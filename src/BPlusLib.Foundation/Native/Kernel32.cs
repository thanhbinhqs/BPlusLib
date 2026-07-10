// <copyright file="Kernel32.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for kernel32.dll — process, handle, and system-level operations.
    /// </summary>
    internal static class Kernel32
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary>PROCESS_DUP_HANDLE (0x0040) — Required to duplicate a handle.</summary>
        internal const uint ProcessDuplicateHandle = 0x0040;

        /// <summary>PROCESS_QUERY_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION.</summary>
        internal const uint ProcessQueryInformation = 0x0400 | 0x1000;

        /// <summary>DUPLICATE_SAME_ACCESS — Duplicate handle with same access.</summary>
        internal const uint DuplicateSameAccess = 0x00000002;

        /// <summary>FORMAT_MESSAGE_FROM_SYSTEM.</summary>
        internal const uint FormatMessageFromSystem = 0x00001000;

        /// <summary>FORMAT_MESSAGE_IGNORE_INSERTS.</summary>
        internal const uint FormatMessageIgnoreInserts = 0x00000200;

        /// <summary>Default buffer size for FormatMessageW.</summary>
        internal const int FormatMessageBufferSize = 512;

        /// <summary>Process name in Win32 format.</summary>
        internal const uint ProcessNameWin32 = 0;

        /// <summary>Process name in native (NT) format.</summary>
        internal const uint ProcessNameNative = 1;

        /// <summary>Maximum path length in characters (260).</summary>
        internal const int MaxPathChars = 260;

        /// <summary>Extended maximum path length (32767).</summary>
        internal const int ExtendedMaxPathChars = 32767;

        // =====================================================================
        // Process and handle management
        // =====================================================================

        /// <summary>
        /// Opens an existing process object.
        /// </summary>
        /// <param name="desiredAccess">The access mask for the process.</param>
        /// <param name="inheritHandle">If true, the handle is inheritable.</param>
        /// <param name="processId">The identifier of the process to open.</param>
        /// <returns>An open handle to the process, or IntPtr.Zero on failure.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            int processId);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">The handle to close.</param>
        /// <returns>True if the handle was closed successfully.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Duplicates a handle from one process to another.
        /// </summary>
        /// <param name="sourceProcessHandle">Handle to the source process.</param>
        /// <param name="sourceHandle">The handle to duplicate.</param>
        /// <param name="targetProcessHandle">Handle to the target process.</param>
        /// <param name="targetHandle">Receives the duplicated handle.</param>
        /// <param name="desiredAccess">The access mask for the new handle.</param>
        /// <param name="inheritHandle">If true, the new handle is inheritable.</param>
        /// <param name="options">Duplication options (DUPLICATE_* flags).</param>
        /// <returns>True if the handle was duplicated successfully.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(
            IntPtr sourceProcessHandle,
            IntPtr sourceHandle,
            IntPtr targetProcessHandle,
            out IntPtr targetHandle,
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            uint options);

        /// <summary>Returns a pseudo-handle for the current process.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>Returns the process ID of the calling process.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetCurrentProcessId();

        /// <summary>Returns the thread ID of the calling thread.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetCurrentThreadId();

        // =====================================================================
        // Process information
        // =====================================================================

        /// <summary>
        /// Retrieves the full image path for the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryFullProcessImageName(
            IntPtr handle,
            uint dwFlags,
            StringBuilder lpExeName,
            ref uint lpdwSize);

        /// <summary>
        /// Retrieves the DOS device mapping for a device name.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint QueryDosDevice(
            string? lpDeviceName,
            StringBuilder lpTargetDevice,
            uint bufferLength);

        /// <summary>
        /// Retrieves timing information for the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessTimes(
            IntPtr handle,
            out long lpCreationTime,
            out long lpExitTime,
            out long lpKernelTime,
            out long lpUserTime);

        // =====================================================================
        // Error handling
        // =====================================================================

        /// <summary>Returns the last-error code for the calling thread.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetLastError();

        /// <summary>
        /// Formats a message string from the system's message tables.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int FormatMessageW(
            uint dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            uint dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr arguments);

        /// <summary>Sets the last-error code for the calling thread.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern void SetLastError(int dwErrorCode);
    }
}
