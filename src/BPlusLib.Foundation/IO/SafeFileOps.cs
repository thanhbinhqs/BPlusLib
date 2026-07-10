// <copyright file="SafeFileOps.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BPlusLib.Foundation.IO
{
    /// <summary>
    /// Thread-safe file I/O operations with atomic-write support, retry logic,
    /// and graceful error handling. All methods catch exceptions internally and
    /// return <see langword="false"/> (with <c>out Exception?</c> details where
    /// applicable) rather than throwing.
    /// </summary>
    /// <remarks>
    /// Atomic writes use a temporary sibling file and <see cref="File.Replace(string,string,string?)"/>.
    /// File-lock detection on Windows uses a single P/Invoke (<see cref="NativeMethods"/>
    /// / <c>CreateFile</c>); on non-Windows it returns <see langword="false"/> gracefully.
    /// </remarks>
    public static class SafeFileOps
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const int DefaultRetryCount = 3;
        private const int DefaultRetryDelayMs = 100;
        private const int BufferSize = 4096;
        private const int FileShareNoneRetryMs = 50;

        // -----------------------------------------------------------------
        // Write helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Atomically writes <paramref name="contents"/> to <paramref name="path"/>
        /// using a temporary sibling file and <see cref="File.Replace(string,string,string)"/>.
        /// </summary>
        /// <param name="path">The destination file path.</param>
        /// <param name="contents">The text to write.</param>
        /// <param name="encoding">
        /// The encoding to use, or <see langword="null"/> to use UTF-8 without BOM.
        /// </param>
        /// <returns><see langword="true"/> if the write succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryWriteAllText(string path, string? contents, Encoding? encoding = null)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            encoding ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            // Generate a temp path in the same directory (same volume → atomic rename).
            string? dir = Path.GetDirectoryName(path);
            if (dir is null)
                dir = Directory.GetCurrentDirectory();

            string tempPath;
            try
            {
                tempPath = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".tmp");
            }
            catch
            {
                return false;
            }

            try
            {
                // Write to the temp file first.
                File.WriteAllText(tempPath, contents ?? string.Empty, encoding);

                // Atomically replace the destination.
                File.Replace(tempPath, path, destinationBackupFileName: null);
                return true;
            }
            catch
            {
                // Best-effort cleanup of the temporary file.
                TryDelete(tempPath);
                return false;
            }
        }

        /// <summary>
        /// Attempts to read all text from <paramref name="path"/> with shared read access.
        /// </summary>
        /// <param name="path">The file path to read.</param>
        /// <param name="contents">The file contents, or <see langword="null"/> on failure.</param>
        /// <param name="error">The exception that caused the failure, if any.</param>
        /// <returns><see langword="true"/> if the file was read successfully; otherwise <see langword="false"/>.</returns>
        public static bool TryReadAllText(string path, out string? contents, out Exception? error)
        {
            if (string.IsNullOrEmpty(path))
            {
                contents = null;
                error = new ArgumentException("Path cannot be null or empty.", nameof(path));
                return false;
            }

            try
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    useAsync: false);

                using var reader = new StreamReader(fs, detectEncodingFromByteOrderMarks: true);
                contents = reader.ReadToEnd();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                contents = null;
                error = ex;
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Copy / Move / Delete
        // -----------------------------------------------------------------

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="dest"/> with up to
        /// <see cref="DefaultRetryCount"/> retries and <see cref="DefaultRetryDelayMs"/>
        /// delay between attempts.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="dest">The destination file path.</param>
        /// <param name="overwrite">
        /// <see langword="true"/> to overwrite the destination if it exists.
        /// </param>
        /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
        public static bool TryCopy(string source, string dest, bool overwrite = false)
        {
            return Retry(() =>
            {
                File.Copy(source, dest, overwrite);
            });
        }

        /// <summary>
        /// Moves <paramref name="source"/> to <paramref name="dest"/> with retry logic.
        /// </summary>
        /// <param name="source">The source file or directory path.</param>
        /// <param name="dest">The destination path.</param>
        /// <param name="overwrite">
        /// <see langword="true"/> to overwrite the destination if it exists.
        /// On .NET Framework, <c>File.Move(string,string,bool)</c> is not available;
        /// this falls back to copy + delete.
        /// </param>
        /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
        public static bool TryMove(string source, string dest, bool overwrite = false)
        {
            return Retry(() =>
            {
                if (overwrite)
                {
#if NET472
                    // net472 does not have the 'overwrite' overload.
                    if (File.Exists(dest))
                        File.Delete(dest);
                    File.Move(source, dest);
#else
                    File.Move(source, dest, overwrite: true);
#endif
                }
                else
                {
                    File.Move(source, dest);
                }
            });
        }

        /// <summary>
        /// Deletes the file or directory at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to delete.</param>
        /// <param name="recursive">
        /// If <see langword="true"/> and the path is a directory, deletes all
        /// subdirectories and files recursively.
        /// </param>
        /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
        public static bool TryDelete(string path, bool recursive = false)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else
                {
                    // Path does not exist — consider it a success (idempotent).
                    return true;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Lock detection
        // -----------------------------------------------------------------

        /// <summary>
        /// Determines whether the file at <paramref name="path"/> is currently
        /// locked by another process. On non-Windows platforms this always returns
        /// <see langword="false"/> gracefully.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>
        /// <see langword="true"/> if the file is locked; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsFileLocked(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

#if NET472 || NET6_0
            // Use P/Invoke on Windows for precise detection.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsFileLockedWindows(path);
            }
#else
            // net8.0+: this overload is available on all platforms.
            if (OperatingSystem.IsWindows())
            {
                return IsFileLockedWindows(path);
            }
#endif

            // Non-Windows: try File.Open with FileShare.None.
            return IsFileLockedFallback(path);
        }

        /// <summary>
        /// Windows-specific P/Invoke detection. Attempts to open the file
        /// with <see cref="FileShare.None"/>; if access is denied the file is locked.
        /// </summary>
        private static bool IsFileLockedWindows(string path)
        {
            const uint GenericRead = 0x80000000;
            const uint FileShareNone = 0;
            const uint OpenExisting = 3;
            const uint FileAttributeNormal = 0x80;

            IntPtr handle = CreateFile(
                path,
                GenericRead,
                FileShareNone,
                IntPtr.Zero,
                OpenExisting,
                FileAttributeNormal,
                IntPtr.Zero);

            if (handle == InvalidHandleValue)
            {
                // ERROR_SHARING_VIOLATION (32) or ERROR_LOCK_VIOLATION (33)
                int lastError = Marshal.GetLastWin32Error();
                return lastError == 32 || lastError == 33;
            }

            CloseHandle(handle);
            return false;
        }

        /// <summary>
        /// Fallback lock test — only used on non-Windows or when P/Invoke fails.
        /// </summary>
        private static bool IsFileLockedFallback(string path)
        {
            try
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    1,
                    FileOptions.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Temp path
        // -----------------------------------------------------------------

        /// <summary>
        /// Creates a unique temporary file path with the specified extension.
        /// </summary>
        /// <param name="extension">
        /// The file extension (default <c>".tmp"</c>). A leading dot is added if
        /// not present.
        /// </param>
        /// <returns>
        /// A full path to a non-existent temporary file, or <see cref="string.Empty"/>
        /// on failure.
        /// </returns>
        public static string GetTempFilePath(string? extension = ".tmp")
        {
            try
            {
                string temp = Path.GetTempFileName();
                string target = Path.ChangeExtension(temp, NormalizeExtension(extension));
                File.Move(temp, target);
                return target;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Ensures that all directories in <paramref name="path"/> exist.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        /// <returns>
        /// <see langword="true"/> if the directory exists or was created;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Hash computation
        // -----------------------------------------------------------------

        /// <summary>
        /// Computes the cryptographic hash of the file at <paramref name="path"/>
        /// using the specified <paramref name="algorithm"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="algorithm">The hash algorithm to use (e.g. <see cref="HashAlgorithmName.SHA256"/>).</param>
        /// <param name="hash">The lowercase hex-encoded hash string, or <see langword="null"/> on failure.</param>
        /// <param name="error">The exception that caused the failure, if any.</param>
        /// <returns><see langword="true"/> if the hash was computed; otherwise <see langword="false"/>.</returns>
        public static bool TryGetFileHash(string path, HashAlgorithmName algorithm, out string? hash, out Exception? error)
        {
            if (string.IsNullOrEmpty(path))
            {
                hash = null;
                error = new ArgumentException("Path cannot be null or empty.", nameof(path));
                return false;
            }

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
                using var hasher = IncrementalHash.CreateHash(algorithm);

                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
#if NETFRAMEWORK || NET6_0
                    hasher.AppendData(buffer, 0, bytesRead);
#else
                    hasher.AppendData(buffer.AsSpan(0, bytesRead));
#endif
                }

                byte[] hashBytes = hasher.GetHashAndReset();
                hash = ConvertToHexLower(hashBytes);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                hash = null;
                error = ex;
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Executes <paramref name="action"/> with up to <see cref="DefaultRetryCount"/>
        /// attempts and <see cref="DefaultRetryDelayMs"/> delay between retries.
        /// </summary>
        private static bool Retry(Action action)
        {
            for (int attempt = 0; attempt < DefaultRetryCount; attempt++)
            {
                try
                {
                    action();
                    return true;
                }
                catch
                {
                    if (attempt < DefaultRetryCount - 1)
                        Thread.Sleep(DefaultRetryDelayMs);
                }
            }

            return false;
        }

        /// <summary>
        /// Ensures the extension has a leading dot and is not empty.
        /// </summary>
        private static string NormalizeExtension(string? extension)
        {
            if (string.IsNullOrEmpty(extension))
                return ".tmp";

            return extension![0] == '.' ? extension : "." + extension;
        }

        /// <summary>
        /// Converts a byte array to a lowercase hex string without allocations
        /// from string concatenation.
        /// </summary>
        private static string ConvertToHexLower(byte[] bytes)
        {
#if NET472
            // Use StringBuilder on .NET Framework for compatibility.
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
#else
            return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
        }

        // -----------------------------------------------------------------
        // P/Invoke for Windows file-lock detection
        // -----------------------------------------------------------------

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
