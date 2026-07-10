// <copyright file="ClipboardHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Clipboard
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Standard clipboard format identifiers.
    /// </summary>
    public enum ClipboardFormat : ushort
    {
        /// <summary>CF_TEXT — 1</summary>
        CF_TEXT = 1,

        /// <summary>CF_BITMAP — 2</summary>
        CF_BITMAP = 2,

        /// <summary>CF_METAFILEPICT — 3</summary>
        CF_METAFILEPICT = 3,

        /// <summary>CF_SYLK — 4</summary>
        CF_SYLK = 4,

        /// <summary>CF_DIF — 5</summary>
        CF_DIF = 5,

        /// <summary>CF_TIFF — 6</summary>
        CF_TIFF = 6,

        /// <summary>CF_OEMTEXT — 7</summary>
        CF_OEMTEXT = 7,

        /// <summary>CF_DIB — 8</summary>
        CF_DIB = 8,

        /// <summary>CF_PALETTE — 9</summary>
        CF_PALETTE = 9,

        /// <summary>CF_PENDATA — 10</summary>
        CF_PENDATA = 10,

        /// <summary>CF_RIFF — 11</summary>
        CF_RIFF = 11,

        /// <summary>CF_WAVE — 12</summary>
        CF_WAVE = 12,

        /// <summary>CF_UNICODETEXT — 13</summary>
        CF_UNICODETEXT = 13,

        /// <summary>CF_ENHMETAFILE — 14</summary>
        CF_ENHMETAFILE = 14,

        /// <summary>CF_HDROP — 15</summary>
        CF_HDROP = 15,

        /// <summary>CF_LOCALE — 16</summary>
        CF_LOCALE = 16,

        /// <summary>CF_DIBV5 — 17</summary>
        CF_DIBV5 = 17,

        /// <summary>CF_OWNERDISPLAY — 128</summary>
        CF_OWNERDISPLAY = 128,

        /// <summary>CF_DSPTEXT — 129</summary>
        CF_DSPTEXT = 129,

        /// <summary>CF_DSPBITMAP — 130</summary>
        CF_DSPBITMAP = 130,

        /// <summary>CF_DSPMETAFILEPICT — 131</summary>
        CF_DSPMETAFILEPICT = 131,

        /// <summary>CF_DSPENHMETAFILE — 142</summary>
        CF_DSPENHMETAFILE = 142,

        /// <summary>CF_PRIVATEFIRST — 512</summary>
        CF_PRIVATEFIRST = 512,

        /// <summary>CF_PRIVATELAST — 767</summary>
        CF_PRIVATELAST = 767,

        /// <summary>CF_GDIOBJFIRST — 768</summary>
        CF_GDIOBJFIRST = 768,

        /// <summary>CF_GDIOBJLAST — 1023</summary>
        CF_GDIOBJLAST = 1023,
    }

    /// <summary>
    /// Provides Win32 P/Invoke-based clipboard operations.
    /// All methods are thread-safe and gracefully return false/null on non-Windows platforms.
    /// </summary>
    public static class ClipboardHelper
    {
        private static readonly object Lock = new object();

        // ------ Win32 constants ------

        private const uint CF_TEXT = 1;
        private const uint CF_BITMAP = 2;
        private const uint CF_DIB = 8;
        private const uint CF_UNICODETEXT = 13;
        private const uint CF_HDROP = 15;

        private const uint GMEM_MOVABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const uint GMEM_DDESHARE = 0x2000;

        // ------ DllImports ------

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hwndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsClipboardFormatAvailable(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint EnumClipboardFormats(uint uFormat);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetClipboardFormatNameW(uint uFormat, StringBuilder lpszFormatName, int cchMaxCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, IntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalSize(IntPtr hMem);

        // ------ DROPFILES structure ------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DROPFILES
        {
            public int pFiles;
            public int X;
            public int Y;
            public int fNC;
            public int fWide;
        }

        // ------ Public API ------

        /// <summary>
        /// Attempts to set the clipboard text to the specified string.
        /// </summary>
        /// <param name="text">The text to place on the clipboard.</param>
        /// <returns><c>true</c> if the text was successfully set; otherwise, <c>false</c>.</returns>
        public static bool TrySetText(string text)
        {
            if (text is null)
            {
                return false;
            }

            try
            {
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return false;
                    }

                    try
                    {
                        if (!EmptyClipboard())
                        {
                            return false;
                        }

                        // Allocate global memory for the Unicode string.
                        int byteCount = (text.Length + 1) * 2; // +1 for null terminator, *2 for UTF-16
                        IntPtr hGlobal = GlobalAlloc(GMEM_MOVABLE | GMEM_ZEROINIT, (IntPtr)byteCount);
                        if (hGlobal == IntPtr.Zero)
                        {
                            return false;
                        }

                        try
                        {
                            IntPtr pData = GlobalLock(hGlobal);
                            if (pData == IntPtr.Zero)
                            {
                                return false;
                            }

                            try
                            {
                                // Copy the string as UTF-16 into the global memory.
                                byte[] bytes = Encoding.Unicode.GetBytes(text + '\0');
                                Marshal.Copy(bytes, 0, pData, bytes.Length);
                            }
                            finally
                            {
                                GlobalUnlock(hGlobal);
                            }

                            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                            {
                                // SetClipboardData owns the memory on success, free on failure.
                                GlobalFree(hGlobal);
                                return false;
                            }

                            // hGlobal is now owned by the clipboard — do not free.
                            return true;
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            GlobalFree(hGlobal);
                            return false;
                        }
#pragma warning restore CA1031
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Attempts to retrieve text from the clipboard.
        /// </summary>
        /// <returns>The clipboard text, or <c>null</c> if no text is available or an error occurred.</returns>
        public static string? TryGetText()
        {
            try
            {
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return null;
                    }

                    try
                    {
                        IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                        if (hData == IntPtr.Zero)
                        {
                            return null;
                        }

                        IntPtr pData = GlobalLock(hData);
                        if (pData == IntPtr.Zero)
                        {
                            return null;
                        }

                        try
                        {
                            IntPtr size = GlobalSize(hData);
                            int byteCount = size.ToInt32();
                            if (byteCount <= 0)
                            {
                                return null;
                            }

                            byte[] bytes = new byte[byteCount];
                            Marshal.Copy(pData, bytes, 0, byteCount);

                            // Find the null terminator position.
                            int nullPos = 0;
                            while (nullPos < byteCount - 1 && (bytes[nullPos] != 0 || bytes[nullPos + 1] != 0))
                            {
                                nullPos += 2;
                            }

                            return Encoding.Unicode.GetString(bytes, 0, nullPos);
                        }
                        finally
                        {
                            GlobalUnlock(hData);
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch
            {
                return null;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Attempts to set a list of file paths on the clipboard using the CF_HDROP format.
        /// </summary>
        /// <param name="filePaths">The array of file paths to place on the clipboard.</param>
        /// <returns><c>true</c> if the file list was successfully set; otherwise, <c>false</c>.</returns>
        public static bool TrySetFiles(string[] filePaths)
        {
            if (filePaths is null || filePaths.Length == 0)
            {
                return false;
            }

            try
            {
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return false;
                    }

                    try
                    {
                        if (!EmptyClipboard())
                        {
                            return false;
                        }

                        // Build the CF_HDROP data: DROPFILES struct followed by double-null-terminated file strings.
                        int structSize = Marshal.SizeOf(typeof(DROPFILES));
                        var dropFiles = new DROPFILES
                        {
                            pFiles = structSize,
                            X = 0,
                            Y = 0,
                            fNC = 0,
                            fWide = 1, // Unicode
                        };

                        // Calculate total byte count: struct + all file paths (each null-terminated) + final null.
                        int totalBytes = structSize;
                        foreach (string path in filePaths)
                        {
                            totalBytes += (path.Length + 1) * 2; // UTF-16 bytes including null terminator
                        }

                        totalBytes += 2; // Final double-null terminator (already counted one extra above? let's be precise)

                        // Actually: each path adds (path.Length + 1) * 2. The final double-null is an extra 2 bytes.
                        // So totalBytes = structSize + sum((path.Length+1)*2) + 2
                        // But our loop counted sum((path.Length+1)*2) including the +1 for each. The final null
                        // makes the last path have a double null. Let's recompute cleanly:
                        totalBytes = structSize;
                        foreach (string path in filePaths)
                        {
                            totalBytes += (path.Length * 2) + 2; // characters + null terminator
                        }

                        totalBytes += 2; // final null terminator (makes the last file's null a double null)

                        IntPtr hGlobal = GlobalAlloc(GMEM_MOVABLE | GMEM_ZEROINIT, (IntPtr)totalBytes);
                        if (hGlobal == IntPtr.Zero)
                        {
                            return false;
                        }

                        try
                        {
                            IntPtr pData = GlobalLock(hGlobal);
                            if (pData == IntPtr.Zero)
                            {
                                return false;
                            }

                            try
                            {
                                // Write DROPFILES struct.
                                Marshal.StructureToPtr(dropFiles, pData, false);

                                // Write file paths after the struct.
                                IntPtr cursor = pData + structSize;
                                foreach (string path in filePaths)
                                {
                                    byte[] pathBytes = Encoding.Unicode.GetBytes(path + '\0');
                                    Marshal.Copy(pathBytes, 0, cursor, pathBytes.Length);
                                    cursor += pathBytes.Length;
                                }

                                // Write final null terminator (already zero-initialized, but write explicitly).
                                byte[] finalNull = Encoding.Unicode.GetBytes("\0");
                                Marshal.Copy(finalNull, 0, cursor, finalNull.Length);
                            }
                            finally
                            {
                                GlobalUnlock(hGlobal);
                            }

                            if (SetClipboardData(CF_HDROP, hGlobal) == IntPtr.Zero)
                            {
                                GlobalFree(hGlobal);
                                return false;
                            }

                            return true;
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            GlobalFree(hGlobal);
                            return false;
                        }
#pragma warning restore CA1031
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Attempts to retrieve the list of file paths from the clipboard (CF_HDROP format).
        /// </summary>
        /// <returns>An array of file paths, or <c>null</c> if no file list is available or an error occurred.</returns>
        public static string[]? TryGetFiles()
        {
            try
            {
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return null;
                    }

                    try
                    {
                        IntPtr hData = GetClipboardData(CF_HDROP);
                        if (hData == IntPtr.Zero)
                        {
                            return null;
                        }

                        IntPtr pData = GlobalLock(hData);
                        if (pData == IntPtr.Zero)
                        {
                            return null;
                        }

                        try
                        {
                            IntPtr size = GlobalSize(hData);
                            int byteCount = size.ToInt32();
                            if (byteCount <= Marshal.SizeOf(typeof(DROPFILES)))
                            {
                                return null;
                            }

                            // Read DROPFILES struct.
                            DROPFILES dropFiles = Marshal.PtrToStructure<DROPFILES>(pData);
                            int filesOffset = dropFiles.pFiles;

                            // The file paths start at the offset indicated by pFiles (usually struct size).
                            IntPtr filesPtr = pData + filesOffset;
                            int remainingBytes = byteCount - filesOffset;

                            if (remainingBytes <= 0)
                            {
                                return null;
                            }

                            byte[] data = new byte[remainingBytes];
                            Marshal.Copy(filesPtr, data, 0, remainingBytes);

                            // Parse null-terminated Unicode strings.
                            var paths = new List<string>();
                            int pos = 0;
                            while (pos < data.Length - 1)
                            {
                                // Find next null terminator (two zero bytes for Unicode).
                                int end = pos;
                                while (end < data.Length - 1 && (data[end] != 0 || data[end + 1] != 0))
                                {
                                    end += 2;
                                }

                                if (end > pos)
                                {
                                    paths.Add(Encoding.Unicode.GetString(data, pos, end - pos));
                                }

                                // Skip past the null terminator.
                                pos = end + 2;

                                // If we hit a double null (end of list), break.
                                if (pos >= data.Length - 1 || (data[pos] == 0 && data[pos + 1] == 0))
                                {
                                    break;
                                }
                            }

                            return paths.Count > 0 ? paths.ToArray() : null;
                        }
                        finally
                        {
                            GlobalUnlock(hData);
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch
            {
                return null;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Attempts to load an image from the specified path and set it on the clipboard as a bitmap.
        /// Uses System.Drawing.Bitmap on .NET Framework / .NET Core when available.
        /// </summary>
        /// <param name="imagePath">The full path to the image file to load.</param>
        /// <returns><c>true</c> if the image was successfully set on the clipboard; otherwise, <c>false</c>.</returns>
        public static bool TrySetImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return false;
            }

            try
            {
#if NET472
                using (var bitmap = new System.Drawing.Bitmap(imagePath))
                {
                    IntPtr hBitmap = bitmap.GetHbitmap();
                    if (hBitmap == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        lock (Lock)
                        {
                            if (!OpenClipboard(IntPtr.Zero))
                            {
                                return false;
                            }

                            try
                            {
                                if (!EmptyClipboard())
                                {
                                    return false;
                                }

                                if (SetClipboardData(CF_BITMAP, hBitmap) == IntPtr.Zero)
                                {
                                    return false;
                                }

                                // hBitmap is now owned by the clipboard.
                                return true;
                            }
                            finally
                            {
                                CloseClipboard();
                            }
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        // Free the bitmap handle if we failed to set it.
                        NativeMethods.DeleteObject(hBitmap);
                        return false;
                    }
#pragma warning restore CA1031
                }
#else
                // System.Drawing.Bitmap is not available on net6.0+ without
                // System.Drawing.Common package (which is Windows-only).
                // Image clipboard operations are not supported on this platform.
                return false;
#endif
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }
            catch (OutOfMemoryException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Clears all data from the clipboard.
        /// </summary>
        /// <returns><c>true</c> if the clipboard was successfully cleared; otherwise, <c>false</c>.</returns>
        public static bool Clear()
        {
            try
            {
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return false;
                    }

                    try
                    {
                        return EmptyClipboard();
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Checks whether the clipboard currently contains text (CF_UNICODETEXT).
        /// </summary>
        /// <returns><c>true</c> if text is available; otherwise, <c>false</c>.</returns>
        public static bool ContainsText()
        {
            try
            {
                return IsClipboardFormatAvailable(CF_UNICODETEXT);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Checks whether the clipboard currently contains a file list (CF_HDROP).
        /// </summary>
        /// <returns><c>true</c> if a file list is available; otherwise, <c>false</c>.</returns>
        public static bool ContainsFiles()
        {
            try
            {
                return IsClipboardFormatAvailable(CF_HDROP);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Checks whether the clipboard currently contains an image (CF_BITMAP or CF_DIB).
        /// </summary>
        /// <returns><c>true</c> if an image is available; otherwise, <c>false</c>.</returns>
        public static bool ContainsImage()
        {
            try
            {
                return IsClipboardFormatAvailable(CF_BITMAP) ||
                       IsClipboardFormatAvailable(CF_DIB);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Enumerates all clipboard formats currently available.
        /// </summary>
        /// <returns>An array of <see cref="ClipboardFormat"/> values available on the clipboard.</returns>
        public static ClipboardFormat[] GetAvailableFormats()
        {
            try
            {
                var formats = new List<ClipboardFormat>();
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return Array.Empty<ClipboardFormat>();
                    }

                    try
                    {
                        uint format = 0;
                        while (true)
                        {
                            format = EnumClipboardFormats(format);
                            if (format == 0)
                            {
                                break;
                            }

                            if (format <= ushort.MaxValue)
                            {
                                formats.Add((ClipboardFormat)format);
                            }
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }

                return formats.ToArray();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return Array.Empty<ClipboardFormat>();
            }
            catch (EntryPointNotFoundException)
            {
                return Array.Empty<ClipboardFormat>();
            }
            catch
            {
                return Array.Empty<ClipboardFormat>();
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Retrieves human-readable names for all registered clipboard formats,
        /// including both standard and registered custom formats.
        /// </summary>
        /// <returns>A read-only list of format name strings.</returns>
        public static IReadOnlyList<string> GetFormatNames()
        {
            try
            {
                var names = new List<string>();
                lock (Lock)
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return Array.Empty<string>();
                    }

                    try
                    {
                        uint format = 0;
                        while (true)
                        {
                            format = EnumClipboardFormats(format);
                            if (format == 0)
                            {
                                break;
                            }

                            // Try to get the registered name for this format.
                            var sb = new StringBuilder(256);
                            int ret = GetClipboardFormatNameW(format, sb, sb.Capacity);
                            if (ret > 0)
                            {
                                names.Add(sb.ToString());
                            }
                            else
                            {
                                // For standard clipboard formats, use the enum name.
                                if (format <= ushort.MaxValue)
                                {
                                    names.Add(((ClipboardFormat)format).ToString());
                                }
                                else
                                {
                                    names.Add($"0x{format:X4}");
                                }
                            }
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }

                return names.AsReadOnly();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return Array.Empty<string>();
            }
            catch (EntryPointNotFoundException)
            {
                return Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Internal native methods for cleanup operations.
        /// </summary>
        private static class NativeMethods
        {
            [DllImport("gdi32.dll", SetLastError = true)]
            internal static extern bool DeleteObject(IntPtr hObject);
        }
    }
}
