// <copyright file="ShellHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Shell
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32;

    /// <summary>
    /// Provides Win32 P/Invoke-based helper methods for Windows Shell operations,
    /// including file verbs, file associations, and recycle bin management.
    /// All methods are thread-safe and gracefully return false/null on non-Windows platforms.
    /// </summary>
    public static class ShellHelper
    {
        // ------ Win32 constants ------

        private const uint SW_SHOWNORMAL = 1;
        private const uint SW_SHOW = 5;
        private const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
        private const uint SEE_MASK_DEFAULT = 0x00000000;

        private const uint SHGFI_DISPLAYNAME = 0x00000200;
        private const uint SHGFI_TYPENAME = 0x00000400;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;

        // ASSOCSTR values for AssocQueryString
        private const uint ASSOCSTR_COMMAND = 1;
        private const uint ASSOCSTR_EXECUTABLE = 2;
        private const uint ASSOCSTR_FRIENDLYDOCNAME = 3;
        private const uint ASSOCSTR_FRIENDLYAPPNAME = 4;
        private const uint ASSOCSTR_NOOPEN = 5;

        // Flags for AssocQueryString
        private const uint ASSOCF_INIT_DEFAULTTOSTAR = 0x00000004;
        private const uint ASSOCF_NONE = 0x00000000;

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        // ------ Structures ------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHELLEXECUTEINFOW
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFOW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        // ------ DllImports ------

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHQueryRecycleBinW(
            string? pszRootPath,
            ref SHQUERYRBINFO pShQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHEmptyRecycleBinW(
            IntPtr hwnd,
            string? pszRootPath,
            uint dwFlags);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int AssocQueryStringW(
            uint flags,
            uint str,
            [MarshalAs(UnmanagedType.LPWStr)] string pszAssoc,
            [MarshalAs(UnmanagedType.LPWStr)] string? pszExtra,
            [Out] StringBuilder? pszOut,
            ref uint pcchOut);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SHGetFileInfoW(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFOW psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        // ------ Public API ------

        /// <summary>
        /// Executes a Shell verb on the specified file path using ShellExecuteExW.
        /// </summary>
        /// <param name="filePath">The file or executable path to act on.</param>
        /// <param name="verb">The shell verb (e.g. "open", "edit", "print"). Defaults to "open".</param>
        /// <param name="arguments">Optional command-line arguments passed to the target.</param>
        /// <param name="runAs">If <c>true</c>, requests elevated privileges ("runas" verb is used instead).</param>
        /// <returns><c>true</c> if the verb was executed successfully; otherwise, <c>false</c>.</returns>
        public static bool ExecuteVerb(string filePath, string verb = "open", string? arguments = null, bool runAs = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            try
            {
                var info = new SHELLEXECUTEINFOW
                {
                    cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFOW)),
                    fMask = runAs ? SEE_MASK_FLAG_NO_UI : SEE_MASK_DEFAULT,
                    hwnd = IntPtr.Zero,
                    lpVerb = runAs ? "runas" : verb,
                    lpFile = filePath,
                    lpParameters = arguments,
                    lpDirectory = null,
                    nShow = (int)(runAs ? SW_SHOW : SW_SHOWNORMAL),
                    hInstApp = IntPtr.Zero,
                    lpIDList = IntPtr.Zero,
                    lpClass = null,
                    hkeyClass = IntPtr.Zero,
                    dwHotKey = 0,
                    hIcon = IntPtr.Zero,
                    hProcess = IntPtr.Zero,
                };

                IntPtr result = ShellExecuteExW(ref info);

                // ShellExecuteEx returns non-zero on success; hInstApp > 32 also indicates success
                return result != IntPtr.Zero;
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
        /// Gets the list of available shell verbs for a file path by reading the registry
        /// under HKEY_CLASSES_ROOT\SystemFileAssociations\{extension}\shell or the extension's ProgID.
        /// </summary>
        /// <param name="filePath">The file path to query verbs for.</param>
        /// <returns>A read-only list of verb names, or an empty list on failure or non-Windows platforms.</returns>
        public static IReadOnlyList<string> GetAvailableVerbs(string filePath)
        {
            var verbs = new List<string>();

            if (string.IsNullOrEmpty(filePath))
            {
                return verbs;
            }

            try
            {
                string extension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(extension))
                {
                    return verbs;
                }

                // Try SystemFileAssociations first (Windows 8+)
                string sysAssocKey = $@"SystemFileAssociations\{extension}\shell";
                verbs.AddRange(ReadVerbsFromRegistryKey(sysAssocKey));

                // If none found, try the extension's ProgID
                if (verbs.Count == 0)
                {
                    string? progId = GetProgId(extension);
                    if (!string.IsNullOrEmpty(progId))
                    {
                        string progIdShellKey = $@"{progId}\shell";
                        verbs.AddRange(ReadVerbsFromRegistryKey(progIdShellKey));
                    }
                }

                // Also try the extension's direct shell key
                if (verbs.Count == 0)
                {
                    string extShellKey = $@"{extension}\shell";
                    verbs.AddRange(ReadVerbsFromRegistryKey(extShellKey));
                }

                // Add standard verbs that might not be listed
                if (verbs.Count == 0)
                {
                    string? progId = GetProgId(extension);
                    if (!string.IsNullOrEmpty(progId))
                    {
                        // Check if there is a shell key under the extension itself
                        string extKey = $@"{extension}\shell";
                        verbs.AddRange(ReadVerbsFromRegistryKey(extKey));
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                // Non-Windows
            }
            catch (EntryPointNotFoundException)
            {
                // Non-Windows
            }
            catch
            {
                // Ignore all other errors
            }
#pragma warning restore CA1031

            return verbs.AsReadOnly();
        }

        /// <summary>
        /// Gets the default program associated with a file extension using AssocQueryString.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The executable path of the default program, or <c>null</c> if not found.</returns>
        public static string? GetDefaultProgram(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                return AssocQueryStringInternal(ASSOCSTR_EXECUTABLE, extension, null);
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
        /// Checks if the specified program is the default handler for the given file extension.
        /// </summary>
        /// <param name="programPath">The full path to the executable.</param>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns><c>true</c> if the program is the default; otherwise, <c>false</c>.</returns>
        public static bool IsDefaultProgramForExtension(string programPath, string extension)
        {
            if (string.IsNullOrEmpty(programPath) || string.IsNullOrEmpty(extension))
            {
                return false;
            }

            try
            {
                string? defaultProgram = GetDefaultProgram(extension);
                if (string.IsNullOrEmpty(defaultProgram))
                {
                    return false;
                }

                return string.Equals(
                    Path.GetFullPath(defaultProgram).TrimEnd('\\', '/'),
                    Path.GetFullPath(programPath).TrimEnd('\\', '/'),
                    StringComparison.OrdinalIgnoreCase);
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
        /// Sets the file association for a given extension. Requires administrator privileges.
        /// Writes to HKEY_CLASSES_ROOT\{extension} and creates the associated ProgID command key.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".myext").</param>
        /// <param name="programPath">The full path to the executable.</param>
        /// <param name="friendlyName">Optional friendly name for the ProgID (e.g. "MyApp.File").</param>
        /// <returns><c>true</c> if the association was set successfully; otherwise, <c>false</c>.</returns>
        public static bool SetFileAssociation(string extension, string programPath, string? friendlyName = null)
        {
            if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(programPath))
            {
                return false;
            }

            try
            {
                // Normalize extension with leading dot
                string ext = extension.StartsWith(".", StringComparison.Ordinal)
                    ? extension
                    : "." + extension;

                string progId = friendlyName ?? "BPlusLib." + ext.TrimStart('.');

                // 1. Set HKEY_CLASSES_ROOT\{extension} default value to ProgID
                using (RegistryKey? extKey = Registry.ClassesRoot.CreateSubKey(ext))
                {
                    if (extKey == null)
                    {
                        return false;
                    }

                    extKey.SetValue(string.Empty, progId);
                }

                // 2. Create HKEY_CLASSES_ROOT\{ProgID}\shell\open\command
                string commandKeyPath = $@"{progId}\shell\open\command";
                using (RegistryKey? cmdKey = Registry.ClassesRoot.CreateSubKey(commandKeyPath))
                {
                    if (cmdKey == null)
                    {
                        return false;
                    }

                    // Use proper command-line quoting
                    cmdKey.SetValue(string.Empty, $"\"{programPath}\" \"%1\"");
                }

                // 3. Optionally set the FriendlyAppName
                if (!string.IsNullOrEmpty(friendlyName))
                {
                    using (RegistryKey? appKey = Registry.ClassesRoot.CreateSubKey(progId))
                    {
                        appKey?.SetValue("FriendlyAppName", friendlyName);
                    }
                }

                return true;
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
            catch (UnauthorizedAccessException)
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
        /// Gets the ProgID associated with a file extension from the registry.
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

                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(ext))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    object? value = key.GetValue(string.Empty);
                    return value?.ToString();
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
        /// Opens the Windows "Open with" dialog for the specified file path.
        /// Uses the "openas" verb via ShellExecuteExW.
        /// </summary>
        /// <param name="filePath">The file path to show the dialog for.</param>
        /// <returns><c>true</c> if the dialog was opened; otherwise, <c>false</c>.</returns>
        public static bool OpenWithDialog(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            try
            {
                var info = new SHELLEXECUTEINFOW
                {
                    cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFOW)),
                    fMask = SEE_MASK_DEFAULT,
                    hwnd = IntPtr.Zero,
                    lpVerb = "openas",
                    lpFile = filePath,
                    lpParameters = null,
                    lpDirectory = null,
                    nShow = (int)SW_SHOWNORMAL,
                    hInstApp = IntPtr.Zero,
                    lpIDList = IntPtr.Zero,
                    lpClass = null,
                    hkeyClass = IntPtr.Zero,
                    dwHotKey = 0,
                    hIcon = IntPtr.Zero,
                    hProcess = IntPtr.Zero,
                };

                IntPtr result = ShellExecuteExW(ref info);
                return result != IntPtr.Zero;
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
        /// Gets the total size of files in the recycle bin using SHQueryRecycleBinW.
        /// </summary>
        /// <returns>The total size in bytes, or <c>null</c> on failure or non-Windows platforms.</returns>
        public static long? GetRecycleBinSize()
        {
            try
            {
                var info = new SHQUERYRBINFO
                {
                    cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)),
                    i64Size = 0,
                    i64NumItems = 0,
                };

                int hr = SHQueryRecycleBinW(null, ref info);
                if (hr != 0)
                {
                    return null;
                }

                return info.i64Size;
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
        /// Empties the recycle bin using SHEmptyRecycleBinW.
        /// </summary>
        /// <returns><c>true</c> if the recycle bin was emptied; otherwise, <c>false</c>.</returns>
        public static bool EmptyRecycleBin()
        {
            try
            {
                int hr = SHEmptyRecycleBinW(
                    IntPtr.Zero,
                    null,
                    SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);

                return hr == 0;
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
        /// Checks if a file path is located in the Recycle Bin by examining its parent directory name.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns><c>true</c> if the file is in the Recycle Bin; otherwise, <c>false</c>.</returns>
        public static bool IsInRecycleBin(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string? directory = Path.GetDirectoryName(fullPath);

                if (string.IsNullOrEmpty(directory))
                {
                    return false;
                }

                // Check parent directory names commonly used for Recycle Bin
                string dirName = Path.GetFileName(directory);

                return string.Equals(dirName, "$Recycle.Bin", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dirName, "RECYCLER", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dirName, "Recycled", StringComparison.OrdinalIgnoreCase);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Gets the file extension for the given path. Wrapper around <c>Path.GetExtension</c>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The extension (including the dot), or <c>null</c> if the path is null or empty.</returns>
        public static string? GetFileExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return Path.GetExtension(path);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                return null;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Gets the human-readable description for a file extension using AssocQueryString
        /// with ASSOCSTR_FRIENDLYDOCNAME.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The description string (e.g. "Text Document"), or <c>null</c> if not found.</returns>
        public static string? GetExtensionDescription(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            try
            {
                return AssocQueryStringInternal(ASSOCSTR_FRIENDLYDOCNAME, extension, null);
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
        /// Checks if the specified path is a shortcut (.lnk file).
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns><c>true</c> if the path ends with .lnk; otherwise, <c>false</c>.</returns>
        public static bool IsShortcut(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetExtension(path),
                    ".lnk",
                    StringComparison.OrdinalIgnoreCase);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Gets the display name (file title without extension) using SHGetFileInfo with SHGFI_DISPLAYNAME.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The display name without extension, or <c>null</c> on failure.</returns>
        public static string? GetFileTitle(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                var shinfo = new SHFILEINFOW();
                IntPtr result = SHGetFileInfoW(
                    path,
                    0,
                    ref shinfo,
                    (uint)Marshal.SizeOf(typeof(SHFILEINFOW)),
                    SHGFI_DISPLAYNAME);

                if (result == IntPtr.Zero)
                {
                    return null;
                }

                return shinfo.szDisplayName;
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

        // ------ Private helpers ------

        /// <summary>
        /// Reads shell verb names from a registry key under HKEY_CLASSES_ROOT.
        /// </summary>
        private static IEnumerable<string> ReadVerbsFromRegistryKey(string subKeyPath)
        {
            var verbs = new List<string>();

            try
            {
                using (RegistryKey? shellKey = Registry.ClassesRoot.OpenSubKey(subKeyPath))
                {
                    if (shellKey == null)
                    {
                        return verbs;
                    }

                    foreach (string? verbName in shellKey.GetSubKeyNames())
                    {
                        if (!string.IsNullOrEmpty(verbName))
                        {
                            verbs.Add(verbName);
                        }
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Ignore registry access errors
            }
#pragma warning restore CA1031

            return verbs;
        }

        /// <summary>
        /// Calls AssocQueryStringW with the given parameters and returns the result string.
        /// </summary>
        private static string? AssocQueryStringInternal(uint assocStr, string extension, string? extra)
        {
            uint length = 0;

            // First call to get the required buffer size
            int hr = AssocQueryStringW(
                ASSOCF_INIT_DEFAULTTOSTAR,
                assocStr,
                extension,
                extra,
                null,
                ref length);

            if (hr != 1 && hr != 0) // 1 = S_FALSE (buffer too small), 0 = S_OK
            {
                // Error or not found
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
                extension,
                extra,
                sb,
                ref length);

            if (hr != 0)
            {
                return null;
            }

            return sb.ToString();
        }
    }
}
