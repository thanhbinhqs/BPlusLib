// <copyright file="PathHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.IO
{
    /// <summary>
    /// Provides safe, validated path operations: combining, sanitising,
    /// normalising, and querying file-system paths. All methods are
    /// thread-safe and never throw—exceptions are caught internally and
    /// returned as <see langword="null"/> or empty results where appropriate.
    /// </summary>
    public static class PathHelper
    {
        // -----------------------------------------------------------------
        // Combining & validation
        // -----------------------------------------------------------------

        /// <summary>
        /// Safely combines two path components after validating that neither
        /// contains invalid path characters.
        /// </summary>
        /// <param name="path1">The first path component.</param>
        /// <param name="path2">The second path component.</param>
        /// <returns>
        /// The combined path, or <see langword="null"/> if either component is
        /// <see langword="null"/> or contains invalid characters.
        /// </returns>
        public static string? SafeCombine(string path1, string path2)
        {
            if (path1 is null || path2 is null)
                return null;

            if (HasInvalidPathChars(path1) || HasInvalidPathChars(path2))
                return null;

            try
            {
                return Path.Combine(path1, path2);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="path"/> contains
        /// any character from <see cref="Path.GetInvalidPathChars()"/>.
        /// </summary>
        /// <param name="path">The path string to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if invalid characters are present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool HasInvalidPathChars(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            char[] invalid = Path.GetInvalidPathChars();

            // Per-index: avoid LINQ in hot paths.
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="name"/> contains
        /// any character from <see cref="Path.GetInvalidFileNameChars()"/>.
        /// </summary>
        /// <param name="name">The file name to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if invalid characters are present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool HasInvalidFileNameChars(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            char[] invalid = Path.GetInvalidFileNameChars();

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces every invalid file-name character in <paramref name="name"/>
        /// with <paramref name="replacement"/>.
        /// </summary>
        /// <param name="name">The file name to sanitise.</param>
        /// <param name="replacement">
        /// The replacement character (default <c>'_'</c>).
        /// </param>
        /// <returns>
        /// The sanitised name, or <see cref="string.Empty"/> if <paramref name="name"/>
        /// is <see langword="null"/> or empty after replacement.
        /// </returns>
        public static string SanitizeFileName(string name, char replacement = '_')
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            char[] invalid = Path.GetInvalidFileNameChars();
            var result = new StringBuilder(name.Length);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool isInvalid = false;

                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                    {
                        isInvalid = true;
                        break;
                    }
                }

                result.Append(isInvalid ? replacement : c);
            }

            return result.ToString();
        }

        // -----------------------------------------------------------------
        // Path inspection
        // -----------------------------------------------------------------

        /// <summary>
        /// Determines whether <paramref name="path"/> is an absolute (rooted) path.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> is rooted;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                return Path.IsPathRooted(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Computes a relative path from <paramref name="basePath"/> to
        /// <paramref name="fullPath"/>. Supports both <c>/</c> and <c>\</c>
        /// directory separators.
        /// </summary>
        /// <param name="fullPath">The absolute target path.</param>
        /// <param name="basePath">The absolute base (anchor) path.</param>
        /// <returns>
        /// The relative path, or <see langword="null"/> if the operation fails
        /// (e.g. different drive roots on Windows).
        /// </returns>
        public static string? GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
                return null;

            try
            {
                string full = NormalizePath(fullPath);
                string baseNorm = NormalizePath(basePath);

#if NET472
                // .NET Framework does not have Path.GetRelativePath; use URI.
                return GetRelativePathLegacy(full, baseNorm);
#else
                return Path.GetRelativePath(baseNorm, full);
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Normalises a path via <see cref="Path.GetFullPath(string)"/> and
        /// replaces forward slashes with backslashes on Windows.
        /// </summary>
        /// <param name="path">The path to normalise.</param>
        /// <returns>
        /// The full, normalised path, or <see cref="string.Empty"/> on failure.
        /// </returns>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            try
            {
                string full = Path.GetFullPath(path);

                if (Path.DirectorySeparatorChar == '\\')
                {
                    full = full.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                }

                return full;
            }
            catch
            {
                return string.Empty;
            }
        }

        // -----------------------------------------------------------------
        // Existence & size
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns <see langword="true"/> if the specified <paramref name="path"/>
        /// exists as either a file or a directory.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>
        /// <see langword="true"/> if the path exists; otherwise <see langword="false"/>.
        /// </returns>
        public static bool PathExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                return File.Exists(path) || Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Computes the total size (in bytes) of the file or directory at
        /// <paramref name="path"/>. If <paramref name="path"/> is a directory
        /// and <paramref name="recursive"/> is <see langword="true"/>, all
        /// nested files are included.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="recursive">
        /// If <see langword="true"/> and the path is a directory, sizes are
        /// summed recursively.
        /// </param>
        /// <returns>
        /// The total size in bytes, or <c>-1</c> if the path does not exist
        /// or an error occurs.
        /// </returns>
        public static long GetPathSize(string path, bool recursive = true)
        {
            if (string.IsNullOrEmpty(path))
                return -1;

            try
            {
                if (File.Exists(path))
                {
                    return new FileInfo(path).Length;
                }

                if (Directory.Exists(path))
                {
                    return GetDirectorySize(path, recursive);
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        // -----------------------------------------------------------------
        // Available file name
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns a file name that does not currently exist. If
        /// <paramref name="basePath"/> exists, a numeric suffix is appended
        /// (e.g. <c>"file (1).txt"</c>, <c>"file (2).txt"</c>).
        /// </summary>
        /// <param name="basePath">The desired file path.</param>
        /// <param name="prefix">
        /// An optional prefix to prepend before the base name (e.g. <c>"Copy - "</c>).
        /// May be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An available file path, or <see cref="string.Empty"/> on failure.
        /// </returns>
        public static string GetAvailableFileName(string basePath, string? prefix = null)
        {
            if (string.IsNullOrEmpty(basePath))
                return string.Empty;

            try
            {
                if (!File.Exists(basePath) && !Directory.Exists(basePath))
                    return basePath;

                string? dir = Path.GetDirectoryName(basePath);
                string name = Path.GetFileNameWithoutExtension(basePath);
                string ext = Path.GetExtension(basePath);

                // Apply optional prefix.
                string baseName = string.IsNullOrEmpty(prefix) ? name : prefix + name;

                // Build candidate: "{dir}/{baseName} ({n}){ext}" or "{dir}/{baseName}{ext}" for n=0.
                for (int i = 1; i < 10000; i++)
                {
                    string candidate = Path.Combine(
                        dir ?? ".",
                        $"{baseName} ({i}){ext}");

                    if (!File.Exists(candidate) && !Directory.Exists(candidate))
                        return candidate;
                }

                // Fallback: GUID suffix.
                return Path.Combine(
                    dir ?? ".",
                    $"{baseName} ({Guid.NewGuid():N}){ext}");
            }
            catch
            {
                return string.Empty;
            }
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Recursively computes the total size of a directory tree.
        /// Uses <see cref="Directory.EnumerateFiles(string, string, SearchOption)"/>
        /// to avoid loading all paths into memory at once.
        /// </summary>
        private static long GetDirectorySize(string directoryPath, bool recursive)
        {
            long total = 0;

            SearchOption option = recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            try
            {
                foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", option))
                {
                    try
                    {
                        total += new FileInfo(filePath).Length;
                    }
                    catch
                    {
                        // Skip files we cannot stat.
                    }
                }
            }
            catch
            {
                // Return partial results if enumeration fails mid-way.
            }

            return total;
        }

        /// <summary>
        /// Legacy relative-path computation for .NET Framework 4.7.2.
        /// Uses <see cref="Uri"/> to compute the difference.
        /// </summary>
        private static string? GetRelativePathLegacy(string fullPath, string basePath)
        {
            // Ensure trailing separators for directory detection.
            string baseWithSep = basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? basePath
                : basePath + Path.DirectorySeparatorChar;

            var baseUri = new Uri(baseWithSep);
            var fullUri = new Uri(fullPath);

            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            // Replace URI separators (/) with platform separators.
            if (Path.DirectorySeparatorChar != '/')
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

            return relativePath;
        }
    }
}
