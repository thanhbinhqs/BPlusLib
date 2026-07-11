// <copyright file="Shell32.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for shell32.dll — shell, file info, and icon operations.
    /// </summary>
    internal static class Shell32
    {
        /// <summary>CLSID for ShellLink COM object.</summary>
        internal static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");

        // =====================================================================
        // Command-line parsing
        // =====================================================================

        /// <summary>
        /// Parses a command line string into an argv-style array.
        /// </summary>
        /// <param name="lpCmdLine">The command line string to parse.</param>
        /// <param name="pNumArgs">Receives the number of parsed arguments.</param>
        /// <returns>A pointer to an array of string pointers, or IntPtr.Zero on failure.</returns>
        /// <remarks>Use Marshal.FreeHGlobal on the returned pointer when done.</remarks>
        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
            out int pNumArgs);

        // =====================================================================
        // File information
        // =====================================================================

        /// <summary>
        /// Retrieves information about a file object (icon, display name, type, etc.).
        /// </summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SHGetFileInfoW(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFOW psfi,
            int cbFileInfo,
            uint uFlags);

        /// <summary>SHGFI_LARGEICON = 0x0</summary>
        internal const uint SHGFI_LARGEICON = 0x00000000;
        /// <summary>SHGFI_SMALLICON = 0x1</summary>
        internal const uint SHGFI_SMALLICON = 0x00000001;
        /// <summary>SHGFI_ICON = 0x100</summary>
        internal const uint SHGFI_ICON = 0x00000100;
        /// <summary>SHGFI_DISPLAYNAME = 0x200</summary>
        internal const uint SHGFI_DISPLAYNAME = 0x00000200;
        /// <summary>SHGFI_TYPENAME = 0x400</summary>
        internal const uint SHGFI_TYPENAME = 0x00000400;
        /// <summary>SHGFI_SYSICONINDEX = 0x4000</summary>
        internal const uint SHGFI_SYSICONINDEX = 0x00004000;
        /// <summary>SHGFI_USEFILEATTRIBUTES = 0x10</summary>
        internal const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
        /// <summary>FILE_ATTRIBUTE_NORMAL = 0x80</summary>
        internal const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        // =====================================================================
        // Shell execution
        // =====================================================================

        /// <summary>
        /// Performs an operation on a file (open, print, explore, etc.).
        /// </summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

        // =====================================================================
        // Folder selection
        // =====================================================================

        /// <summary>
        /// Opens a folder and selects the specified items.
        /// </summary>
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            IntPtr[]? apidl,
            uint dwFlags);

        // =====================================================================
        // Icons
        // =====================================================================

        /// <summary>
        /// Extracts icons from an executable, DLL, or icon file.
        /// </summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int ExtractIconExW(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phiconLarge,
            IntPtr[] phiconSmall,
            uint nIcons);

        // =================================================================
        // IShellLink COM interface (shell32.dll)
        // =================================================================

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellLinkW
        {
            void GetPath(
                [Out] StringBuilder pszFile,
                int cchMaxPath,
                out IntPtr pfd,
                uint fFlags);

            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);

            void GetDescription(
                [Out] StringBuilder pszName,
                int cchMaxName);

            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            void GetWorkingDirectory(
                [Out] StringBuilder pszDir,
                int cchMaxPath);

            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            void GetArguments(
                [Out] StringBuilder pszArgs,
                int cchMaxPath);

            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            void GetHotKey(out short pwHotkey);
            void SetHotKey(short wHotkey);

            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);

            void GetIconLocation(
                [Out] StringBuilder pszIconPath,
                int cchIconPath,
                out int piIcon);

            void SetIconLocation(
                [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
                int iIcon);

            void SetRelativePath(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
                int dwReserved);

            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        /// <summary>IPersistFile COM interface for saving/loading .lnk files.</summary>
        [ComImport]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPersistFile
        {
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string pszFile);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        }

        /// <summary>CLSID_ShellLink for CoCreateInstance.</summary>
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink { }

        // =================================================================
        // Shell_NotifyIcon API
        // =================================================================

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Shell_NotifyIconW(
            uint dwMessage,
            ref NOTIFYICONDATAW lpData);

        // Notify Icon messages
        internal const uint NIM_ADD = 0x00000000;
        internal const uint NIM_MODIFY = 0x00000001;
        internal const uint NIM_DELETE = 0x00000002;
        internal const uint NIM_SETFOCUS = 0x00000003;
        internal const uint NIM_SETVERSION = 0x00000004;

        // Notify Icon flags
        internal const uint NIF_MESSAGE = 0x00000001;
        internal const uint NIF_ICON = 0x00000002;
        internal const uint NIF_TIP = 0x00000004;
        internal const uint NIF_STATE = 0x00000008;
        internal const uint NIF_INFO = 0x00000010;
        internal const uint NIF_GUID = 0x00000020;
        internal const uint NIF_SHOWTIP = 0x00000080;

        // NOTIFYICON_VERSION
        internal const uint NOTIFYICON_VERSION = 0x00000003;
        internal const uint NOTIFYICON_VERSION_4 = 0x00000004;

        // NOTIFYICONDATAW size constants
        internal const int NOTIFYICONDATAW_SIZE_V2 = 488; // 32-bit: 504; 64-bit: 552? Use dynamic
        internal const int NOTIFYICONDATAW_SIZE_V2_32 = 504;
        internal const int NOTIFYICONDATAW_SIZE_V2_64 = 552;
        internal const int NOTIFYICONDATAW_SIZE_V4 = 892; // 64-bit: 956

        // Balloon flags
        internal const uint NIIF_NONE = 0x00;
        internal const uint NIIF_INFO = 0x01;
        internal const uint NIIF_WARNING = 0x02;
        internal const uint NIIF_ERROR = 0x03;
        internal const uint NIIF_USER = 0x04;
        internal const uint NIIF_NOSOUND = 0x10;
        internal const uint NIIF_LARGE_ICON = 0x20;
        internal const uint NIIF_RESPECT_QUIET_TIME = 0x80;
    }

    /// <summary>NOTIFYICONDATAW structure for Shell_NotifyIconW.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        // Vista+ balloon data (hBalloonIcon) exists on 64-bit only
        // We stop here for cross-target compatibility
    }
}
