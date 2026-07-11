// <copyright file="AssocHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace BPlusLib.Foundation.Shell
{
    /// <summary>
    /// Provides file extension association queries via AssocQueryString and registry.
    /// All methods are thread-safe and gracefully return null/false on non-Windows platforms.
    /// </summary>
    public static class AssocHelper
    {
        // ASSOCSTR values
        private const uint ASSOCSTR_COMMAND = 1;
        private const uint ASSOCSTR_EXECUTABLE = 2;
        private const uint ASSOCSTR_FRIENDLYDOCNAME = 3;
        private const uint ASSOCSTR_FRIENDLYAPPNAME = 4;
        private const uint ASSOCSTR_CONTENTTYPE = 7;

        // Flags for AssocQueryString
        private const uint ASSOCF_INIT_DEFAULTTOSTAR = 0x00000004;
        private const uint ASSOCF_NONE = 0x00000000;

        /// <summary>
        /// Gets the friendly file type description for an extension (e.g., ".txt" → "Text Document").
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The description string, or <c>null</c> if not found.</returns>
        public static string? GetFileTypeDescription(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                return AssocQueryStringInternal(ASSOCSTR_FRIENDLYDOCNAME, extension, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the executable associated with a file extension (e.g., ".txt" → "C:\Windows\system32\NOTEPAD.EXE").
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The full path to the executable, or <c>null</c> if not found.</returns>
        public static string? GetAssociatedExecutable(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                return AssocQueryStringInternal(ASSOCSTR_EXECUTABLE, extension, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the open command template for an extension (e.g., ".txt" → @"%SystemRoot%\system32\NOTEPAD.EXE %1").
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The command template string, or <c>null</c> if not found.</returns>
        public static string? GetOpenCommand(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                return AssocQueryStringInternal(ASSOCSTR_COMMAND, extension, "open");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the ProgID for an extension from the registry (e.g., ".txt" → "txtfile").
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The ProgID string, or <c>null</c> if not found.</returns>
        public static string? GetProgId(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                string ext = extension.StartsWith(".", StringComparison.Ordinal)
                    ? extension
                    : "." + extension;

                using (RegistryKey? key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    object? value = key.GetValue(string.Empty);
                    return value?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the content type (MIME) for an extension (e.g., ".txt" → "text/plain").
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The content type string, or <c>null</c> if not found.</returns>
        public static string? GetContentType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                return AssocQueryStringInternal(ASSOCSTR_CONTENTTYPE, extension, null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if the extension has a registered association.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns><c>true</c> if the extension has a registered association; otherwise <c>false</c>.</returns>
        public static bool IsExtensionRegistered(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            try
            {
                // Check registry first
                string? progId = GetProgId(extension);
                if (!string.IsNullOrEmpty(progId))
                {
                    return true;
                }

                // Fall back to AssocQueryString
                string? executable = GetAssociatedExecutable(extension);
                return !string.IsNullOrEmpty(executable);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calls AssocQueryStringW with the given parameters and returns the result string.
        /// </summary>
        private static string? AssocQueryStringInternal(uint assocStr, string extension, string? extra)
        {
            string ext = extension.StartsWith(".", StringComparison.Ordinal)
                ? extension
                : "." + extension;

            uint length = 0;

            // First call to get the required buffer size
            int hr = AssocQueryStringW(
                ASSOCF_INIT_DEFAULTTOSTAR,
                assocStr,
                ext,
                extra,
                null,
                ref length);

            if (hr != 1 && hr != 0) // 1 = S_FALSE (buffer too small), 0 = S_OK
            {
                return null;
            }

            if (length == 0)
            {
                return null;
            }

            var sb = new StringBuilder((int)length);

            hr = AssocQueryStringW(
                ASSOCF_INIT_DEFAULTTOSTAR,
                assocStr,
                ext,
                extra,
                sb,
                ref length);

            if (hr != 0)
            {
                return null;
            }

            return sb.ToString();
        }

        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int AssocQueryStringW(
            uint flags,
            uint str,
            [MarshalAs(UnmanagedType.LPWStr)] string pszAssoc,
            [MarshalAs(UnmanagedType.LPWStr)] string? pszExtra,
            [Out] StringBuilder? pszOut,
            ref uint pcchOut);
    }
}
