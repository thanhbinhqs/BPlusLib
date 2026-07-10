// <copyright file="ExplorerHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Explorer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Text;

    /// <summary>
    /// Identifiers for standard Windows known folders.
    /// Maps to KNOWNFOLDERID GUIDs used with SHGetKnownFolderPath.
    /// </summary>
    public enum KnownFolder
    {
        /// <summary>The desktop folder.</summary>
        Desktop,

        /// <summary>The Documents folder.</summary>
        Documents,

        /// <summary>The Downloads folder.</summary>
        Downloads,

        /// <summary>The Pictures folder.</summary>
        Pictures,

        /// <summary>The Music folder.</summary>
        Music,

        /// <summary>The Videos folder.</summary>
        Videos,

        /// <summary>The Recent items folder.</summary>
        Recent,

        /// <summary>The SendTo folder.</summary>
        SendTo,

        /// <summary>The Startup folder.</summary>
        Startup,

        /// <summary>The Programs folder.</summary>
        Programs,

        /// <summary>The AppData (Roaming) folder.</summary>
        AppData,

        /// <summary>The Local AppData folder.</summary>
        LocalAppData,

        /// <summary>The Temp folder (maps to Path.GetTempPath()).</summary>
        Temp,

        /// <summary>The System folder (SYSTEM32).</summary>
        System,

        /// <summary>The Windows folder.</summary>
        Windows,

        /// <summary>The Fonts folder.</summary>
        Fonts,

        /// <summary>The Favorites folder.</summary>
        Favorites,

        /// <summary>The Links folder.</summary>
        Links,

        /// <summary>The SavedGames folder.</summary>
        SavedGames,

        /// <summary>The Screenshots folder.</summary>
        Screenshots,
    }

    /// <summary>
    /// Provides Win32 P/Invoke-based helper methods for Windows Explorer integration.
    /// All methods are thread-safe and gracefully return false/null on non-Windows platforms.
    /// </summary>
    public static class ExplorerHelper
    {
        // ------ KNOWNFOLDERID GUIDs ------

        private static readonly Guid FolderIdDesktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
        private static readonly Guid FolderIdDocuments = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
        private static readonly Guid FolderIdDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        private static readonly Guid FolderIdPictures = new Guid("33E28130-4E1E-42F3-9A84-13F6D42A4134");
        private static readonly Guid FolderIdMusic = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
        private static readonly Guid FolderIdVideos = new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
        private static readonly Guid FolderIdRecent = new Guid("AE50C081-EBD2-438A-8655-8A092E34987A");
        private static readonly Guid FolderIdSendTo = new Guid("8983036C-27C0-404B-8F08-102D10DCFD74");
        private static readonly Guid FolderIdStartup = new Guid("B97D20BB-F46A-4C97-BA10-5E3608430854");
        private static readonly Guid FolderIdPrograms = new Guid("A77F5D77-2E2B-44C3-A6A2-ABA601054A51");
        private static readonly Guid FolderIdAppData = new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D");
        private static readonly Guid FolderIdLocalAppData = new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
        private static readonly Guid FolderIdSystem = new Guid("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7");
        private static readonly Guid FolderIdWindows = new Guid("F38BF404-1D43-42F2-9345-66DE83B0FC3E");
        private static readonly Guid FolderIdFonts = new Guid("FD228CB7-AE11-4AE3-864C-16F3910AB8FE");
        private static readonly Guid FolderIdFavorites = new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD");
        private static readonly Guid FolderIdLinks = new Guid("BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968");
        private static readonly Guid FolderIdSavedGames = new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");
        private static readonly Guid FolderIdScreenshots = new Guid("B7BEDE81-DF94-4682-A7D8-57A52620B86F");

        // ------ Win32 constants ------

        private const uint SHGFI_TYPENAME = 0x00000400;

        private const uint FO_DELETE = 3;
        private const uint FOF_ALLOWUNDO = 0x0040;
        private const uint FOF_NOCONFIRMATION = 0x0010;
        private const uint FOF_SILENT = 0x0004;

        private const uint FILE_SHARE_NONE = 0;
        private const uint GENERIC_READ = 0x80000000;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;

        // ------ Structures ------

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCTW
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

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

        // ------ DllImports ------

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
            uint dwFlags,
            IntPtr hToken,
            out IntPtr ppszPath);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SHGetFileInfoW(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFOW psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHFileOperationW(ref SHFILEOPSTRUCTW lpFileOp);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetCompressedFileSizeW(
            string lpFileName,
            out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int GetSecurityInfo(
            IntPtr hObject,
            uint ObjectType,
            uint SecurityInfo,
            out IntPtr ppsidOwner,
            out IntPtr ppsidGroup,
            out IntPtr ppDacl,
            out IntPtr ppSacl,
            out IntPtr ppSecurityDescriptor);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupAccountSidW(
            string? lpSystemName,
            IntPtr sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder lpReferencedDomainName,
            ref uint cchReferencedDomainName,
            out int peUse);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        // ------ Ole32 for COM (IShellLink resolution) ------

        [DllImport("ole32.dll", SetLastError = true)]
        private static extern int CoCreateInstance(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            IntPtr pUnkOuter,
            uint dwClsContext,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppv);

        // CLSID_ShellLink
        private static readonly Guid CLSID_ShellLink = new Guid("00021401-0000-0000-C000-000000000046");

        // IID_IShellLinkW
        private static readonly Guid IID_IShellLinkW = new Guid("000214F9-0000-0000-C000-000000000046");

        // IID_IPersistFile
        private static readonly Guid IID_IPersistFile = new Guid("0000010B-0000-0000-C000-000000000046");

        // Com interface for getting the path from IShellLink via GetPath
        private const int MAX_PATH = 260;

        private const uint SLGP_SHORTPATH = 0x01;
        private const uint SLGP_UNCPRIORITY = 0x02;
        private const uint SLGP_RAWPATH = 0x04;

        // ------ Public API ------

        /// <summary>
        /// Gets the file system path for a known folder.
        /// </summary>
        /// <param name="folder">The known folder to resolve.</param>
        /// <returns>The full path to the folder, or <c>null</c> on failure or non-Windows platforms.</returns>
        public static string? GetKnownFolderPath(KnownFolder folder)
        {
            // Temp is not a KNOWNFOLDERID; use Path.GetTempPath().
            if (folder == KnownFolder.Temp)
            {
                return Path.GetTempPath();
            }

            Guid folderId = GetFolderId(folder);

            try
            {
                int hr = SHGetKnownFolderPath(folderId, 0, IntPtr.Zero, out IntPtr pPath);
                if (hr != 0)
                {
                    return null;
                }

                try
                {
                    return Marshal.PtrToStringUni(pPath);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pPath);
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
        /// Opens the specified file or folder in Windows Explorer.
        /// </summary>
        /// <param name="path">The file or folder path to open.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        public static bool OpenInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true,
                    });
                    return true;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Win32Exception)
                {
                    return false;
                }
#pragma warning restore CA1031
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
        /// Opens Windows Explorer with the specified file or folder selected.
        /// </summary>
        /// <param name="path">The file or folder path to select.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        public static bool SelectInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true,
                    });
                    return true;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Win32Exception)
                {
                    return false;
                }
#pragma warning restore CA1031
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
        /// Shows the Windows file properties dialog for the specified file or folder.
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        /// <returns><c>true</c> if the properties dialog was opened; otherwise, <c>false</c>.</returns>
        public static bool ShowFileProperties(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                var info = new SHELLEXECUTEINFOW
                {
                    cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFOW)),
                    fMask = 0,
                    hwnd = IntPtr.Zero,
                    lpVerb = "properties",
                    lpFile = path,
                    lpParameters = null,
                    lpDirectory = null,
                    nShow = 1, // SW_SHOWNORMAL
                    hInstApp = IntPtr.Zero,
                    lpIDList = IntPtr.Zero,
                    lpClass = null,
                    hkeyClass = IntPtr.Zero,
                    dwHotKey = 0,
                    hIcon = IntPtr.Zero,
                    hProcess = IntPtr.Zero,
                };

                IntPtr result = ShellExecuteExW(ref info);
                return result != IntPtr.Zero &&
                       (int)result > 32; // ShellExecuteEx returns > 32 on success
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
        /// Alias for <see cref="SelectInExplorer(string)"/>.
        /// Opens Windows Explorer with the specified file selected.
        /// </summary>
        /// <param name="path">The file or folder path to select.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        public static bool ShowFileInExplorer(string path)
        {
            return SelectInExplorer(path);
        }

        /// <summary>
        /// Gets the actual size on disk for a file using GetCompressedFileSizeW.
        /// Returns the physical size allocated on disk (may differ from logical size).
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>The size on disk in bytes, or <c>null</c> on failure.</returns>
        public static long? GetFileSizeOnDisk(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                uint sizeHigh;
                uint sizeLow = GetCompressedFileSizeW(path, out sizeHigh);
                if (sizeLow == 0xFFFFFFFF && Marshal.GetLastWin32Error() != 0)
                {
                    return null;
                }

                return ((long)sizeHigh << 32) | sizeLow;
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
        /// Checks if the specified file is currently in use (locked by another process).
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns><c>true</c> if the file is in use; otherwise, <c>false</c>.</returns>
        public static bool IsFileInUse(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                IntPtr hFile = CreateFileW(
                    path,
                    GENERIC_READ,
                    FILE_SHARE_NONE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hFile.ToInt64() == INVALID_HANDLE_VALUE)
                {
                    // If access is denied, the file is likely in use.
                    int error = Marshal.GetLastWin32Error();
                    return error == 32; // ERROR_SHARING_VIOLATION
                }

                CloseHandle(hFile);
                return false;
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
        /// Gets the file type description (e.g., "Text Document", "Application") for the specified file.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>The file type description, or <c>null</c> on failure.</returns>
        public static string? GetFileTypeDescription(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                var shinfo = default(SHFILEINFOW);
                IntPtr ret = SHGetFileInfoW(
                    path,
                    0,
                    ref shinfo,
                    (uint)Marshal.SizeOf(typeof(SHFILEINFOW)),
                    SHGFI_TYPENAME);

                if (ret == IntPtr.Zero)
                {
                    return null;
                }

                return shinfo.szTypeName;
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
        /// Gets the owner of the specified file using GetSecurityInfo and LookupAccountSid.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>The owner name in DOMAIN\USER format, or <c>null</c> on failure.</returns>
        public static string? GetFileOwner(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                // Use GetSecurityInfo on the file.
                // We need to open the file with READ_CONTROL to query its security.
                // SE_FILE_OBJECT = 1, OWNER_SECURITY_INFORMATION = 1
                const uint SE_FILE_OBJECT = 1;
                const uint OWNER_SECURITY_INFORMATION = 1;

                // Open the file with READ_CONTROL access.
                const uint READ_CONTROL = 0x00020000;
                IntPtr hFile = CreateFileW(
                    path,
                    READ_CONTROL,
                    0,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hFile.ToInt64() == INVALID_HANDLE_VALUE)
                {
                    return null;
                }

                try
                {
                    int hr = GetSecurityInfo(
                        hFile,
                        SE_FILE_OBJECT,
                        OWNER_SECURITY_INFORMATION,
                        out IntPtr pSidOwner,
                        out IntPtr pSidGroup,
                        out IntPtr ppDacl,
                        out IntPtr ppSacl,
                        out IntPtr ppSecurityDescriptor);

                    if (hr != 0)
                    {
                        return null;
                    }

                    // Look up the account name for the SID.
                    uint nameLen = 0;
                    uint domainLen = 0;
                    int sidType;

                    // First call to get buffer sizes.
                    LookupAccountSidW(
                        null,
                        pSidOwner,
                        null,
                        ref nameLen,
                        null,
                        ref domainLen,
                        out sidType);

                    if (nameLen == 0)
                    {
                        return null;
                    }

                    var nameBuilder = new StringBuilder((int)nameLen);
                    var domainBuilder = new StringBuilder((int)domainLen);

                    if (!LookupAccountSidW(
                        null,
                        pSidOwner,
                        nameBuilder,
                        ref nameLen,
                        domainBuilder,
                        ref domainLen,
                        out sidType))
                    {
                        return null;
                    }

                    string ownerName = nameBuilder.ToString();
                    string domainName = domainBuilder.ToString();

                    if (string.IsNullOrEmpty(domainName))
                    {
                        return ownerName;
                    }

                    return $"{domainName}\\{ownerName}";
                }
                finally
                {
                    CloseHandle(hFile);
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
        /// Gets a list of recently opened files by reading the Windows Recent folder.
        /// </summary>
        /// <param name="maxCount">The maximum number of recent files to return (default: 20).</param>
        /// <returns>A read-only list of shortcut file paths to recent items.</returns>
        public static IReadOnlyList<string> GetRecentFiles(int maxCount = 20)
        {
            try
            {
                string? recentPath = GetKnownFolderPath(KnownFolder.Recent);
                if (string.IsNullOrEmpty(recentPath) || !Directory.Exists(recentPath))
                {
                    return Array.Empty<string>();
                }

                var files = new List<string>();
                try
                {
                    foreach (string lnkFile in Directory.EnumerateFiles(recentPath, "*.lnk"))
                    {
                        if (files.Count >= maxCount)
                        {
                            break;
                        }

                        string? target = ResolveShortcut(lnkFile);
                        if (!string.IsNullOrEmpty(target) && File.Exists(target))
                        {
                            files.Add(target);
                        }
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (DirectoryNotFoundException)
                {
                    // Recent folder might not exist.
                }
                catch (IOException)
                {
                    // I/O errors while enumerating.
                }
#pragma warning restore CA1031

                return files.AsReadOnly();
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
        /// Resolves a Windows shortcut (.lnk) file to its target path using the IShellLink COM interface.
        /// </summary>
        /// <param name="shortcutPath">The full path to the .lnk file.</param>
        /// <returns>The resolved target path, or <c>null</c> on failure.</returns>
        public static string? ResolveShortcut(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath) || !File.Exists(shortcutPath))
            {
                return null;
            }

            try
            {
                // Create IShellLink instance via COM.
                int hr = CoCreateInstance(CLSID_ShellLink, IntPtr.Zero, 1 /*CLSCTX_INPROC_SERVER*/, IID_IShellLinkW, out IntPtr pShellLink);
                if (hr != 0 || pShellLink == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    // Query for IPersistFile.
                    IntPtr pPersistFile;
                    // Create a non-readonly local copy for ref parameter
                    Guid iidPersistFile = IID_IPersistFile;
                    hr = Marshal.QueryInterface(pShellLink, ref iidPersistFile, out pPersistFile);
                    if (hr != 0 || pPersistFile == IntPtr.Zero)
                    {
                        return null;
                    }

                    try
                    {
                        // IPersistFile::Load
                        // IPersistFile vtable: IUnknown (3 slots) + Load (slot 4), Save (slot 5), ...
                        // Load is at vtable offset 4 (0-indexed).
                        var loadMethod = Marshal.GetDelegateForFunctionPointer<LoadDelegate>(
                            Marshal.ReadIntPtr(Marshal.ReadIntPtr(pPersistFile), 4 * IntPtr.Size));

                        hr = loadMethod(pPersistFile, shortcutPath, 0); // STGM_READ = 0
                        if (hr != 0)
                        {
                            return null;
                        }

                        // Resolve the shortcut to ensure the target is up-to-date.
                        var resolveMethod = Marshal.GetDelegateForFunctionPointer<ResolveDelegate>(
                            Marshal.ReadIntPtr(Marshal.ReadIntPtr(pShellLink), 18 * IntPtr.Size));

                        hr = resolveMethod(pShellLink, IntPtr.Zero, 0x0001 /*SLR_NO_UI*/ | 0x0004 /*SLR_UPDATE*/);
                        if (hr != 0)
                        {
                            // Continue anyway — we may still get the path from the cached PIDL.
                        }

                        // Get the PIDL (ITEMIDLIST) from the shell link.
                        var getIdListMethod = Marshal.GetDelegateForFunctionPointer<GetIDListDelegate>(
                            Marshal.ReadIntPtr(Marshal.ReadIntPtr(pShellLink), 3 * IntPtr.Size));
                        hr = getIdListMethod(pShellLink, out IntPtr pidl);
                        if (hr != 0 || pidl == IntPtr.Zero)
                        {
                            return null;
                        }

                        try
                        {
                            // Convert PIDL to path.
                            var sb = new StringBuilder(MAX_PATH);
                            if (!SHGetPathFromIDListW(pidl, sb))
                            {
                                return null;
                            }

                            string targetPath = sb.ToString();
                            return string.IsNullOrEmpty(targetPath) ? null : targetPath;
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pidl);
                        }
                    }
                    finally
                    {
                        Marshal.Release(pPersistFile);
                    }
                }
                finally
                {
                    Marshal.Release(pShellLink);
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
        /// Moves the specified file or folder to the Windows Recycle Bin.
        /// </summary>
        /// <param name="path">The full path to the file or folder to recycle.</param>
        /// <returns><c>true</c> if the item was moved to the recycle bin; otherwise, <c>false</c>.</returns>
        public static bool TryRecycle(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                var op = new SHFILEOPSTRUCTW
                {
                    hwnd = IntPtr.Zero,
                    wFunc = FO_DELETE,
                    pFrom = path + '\0', // SHFILEOPSTRUCT requires double-null-terminated string
                    pTo = null,
                    fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT),
                    fAnyOperationsAborted = 0,
                    hNameMappings = IntPtr.Zero,
                    lpszProgressTitle = null,
                };

                int result = SHFileOperationW(ref op);
                return result == 0;
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

        // ------ COM delegates for manual vtable calling ------

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate int LoadDelegate(IntPtr thisPtr, [MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ResolveDelegate(IntPtr thisPtr, IntPtr hwnd, uint fFlags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetIDListDelegate(IntPtr thisPtr, out IntPtr ppidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SHGetPathFromIDListW(IntPtr pidl, StringBuilder pszPath);

        // ------ Private helpers ------

        /// <summary>
        /// Maps a <see cref="KnownFolder"/> enum value to its KNOWNFOLDERID GUID.
        /// </summary>
        private static Guid GetFolderId(KnownFolder folder)
        {
            return folder switch
            {
                KnownFolder.Desktop => FolderIdDesktop,
                KnownFolder.Documents => FolderIdDocuments,
                KnownFolder.Downloads => FolderIdDownloads,
                KnownFolder.Pictures => FolderIdPictures,
                KnownFolder.Music => FolderIdMusic,
                KnownFolder.Videos => FolderIdVideos,
                KnownFolder.Recent => FolderIdRecent,
                KnownFolder.SendTo => FolderIdSendTo,
                KnownFolder.Startup => FolderIdStartup,
                KnownFolder.Programs => FolderIdPrograms,
                KnownFolder.AppData => FolderIdAppData,
                KnownFolder.LocalAppData => FolderIdLocalAppData,
                KnownFolder.System => FolderIdSystem,
                KnownFolder.Windows => FolderIdWindows,
                KnownFolder.Fonts => FolderIdFonts,
                KnownFolder.Favorites => FolderIdFavorites,
                KnownFolder.Links => FolderIdLinks,
                KnownFolder.SavedGames => FolderIdSavedGames,
                KnownFolder.Screenshots => FolderIdScreenshots,
                _ => FolderIdDesktop,
            };
        }
    }
}
