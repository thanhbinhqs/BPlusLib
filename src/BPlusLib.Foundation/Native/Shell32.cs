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
    }
}
