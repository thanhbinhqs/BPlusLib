// <copyright file="NativeMethods.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation
{
    /// <summary>
    /// Central P/Invoke declarations for all BPlusLib components.
    /// Grouped by subsystem: NT API, kernel32, user32, shell32, etc.
    /// </summary>
    /// <remarks>
    /// This class provides backward compatibility for code referencing NativeMethods.* directly.
    /// New code should use the categorized classes in <see cref="Native"/> instead.
    /// </remarks>
    internal static class NativeMethods
    {
        // =====================================================================
        // NT API constants and helpers
        // =====================================================================
        internal const int SystemExtendedHandleInformation = 64;
        internal const int ObjectNameInformation = 1;
        internal const int ObjectTypeInformation = 2;
        internal const int ProcessBasicInformation = 0;
        internal const int ProcessCommandLineInformation = 63;
        internal const int ProcessDuplicateHandle = 0x0040;
        internal const uint ProcessQueryInformation = 0x0400 | 0x1000;
        internal const uint DuplicateSameAccess = 0x00000002;
        internal const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
        internal const int StatusSuccess = 0;
        internal const int StatusBufferTooSmall = unchecked((int)0xC0000023);
        internal const int StatusBufferOverflow = unchecked((int)0x80000005);
        internal const int InitialHandleBufferSize = 256 * 1024;
        internal const int MaxHandleBufferSize = 16 * 1024 * 1024;
        internal const int MaxObjectNameChars = 1024;
        internal const int MaxCommandLineChars = 32768;
        internal const int MaxPathChars = 260;
        internal const int ExtendedMaxPathChars = 32767;
        internal const uint ProcessNameWin32 = 0;
        internal const uint ProcessNameNative = 1;
        internal static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        internal static bool NtSuccess(int status) => status >= 0;

        // =====================================================================
        // ntdll.dll
        // =====================================================================
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        internal static extern int NtQuerySystemInformation(
            int informationClass, IntPtr buffer, int bufferSize, out int returnedLength);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        internal static extern int NtQueryObject(
            IntPtr handle, int objectInformationClass, IntPtr objectInformation,
            int objectInformationLength, out int returnLength);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        internal static extern int NtQueryInformationProcess(
            IntPtr processHandle, int processInformationClass, IntPtr processInformation,
            int processInformationLength, out int returnLength);

        // =====================================================================
        // kernel32.dll
        // =====================================================================
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(
            IntPtr sourceProcessHandle, IntPtr sourceHandle, IntPtr targetProcessHandle,
            out IntPtr targetHandle, uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint options);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryFullProcessImageName(
            IntPtr handle, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint QueryDosDevice(
            string? lpDeviceName, StringBuilder lpTargetDevice, uint bufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessTimes(
            IntPtr handle, out long lpCreationTime, out long lpExitTime,
            out long lpKernelTime, out long lpUserTime);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetCurrentProcessId();

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetLastError();

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int FormatMessageW(
            uint dwFlags, IntPtr lpSource, int dwMessageId, uint dwLanguageId,
            StringBuilder lpBuffer, int nSize, IntPtr arguments);

        internal const uint FormatMessageFromSystem = 0x00001000;
        internal const uint FormatMessageIgnoreInserts = 0x00000200;
        internal const int FormatMessageBufferSize = 512;

        // =====================================================================
        // user32.dll
        // =====================================================================
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const int HWND_TOPMOST = -1;

        internal const int WH_CBT = 5;
        internal const int HCBT_ACTIVATE = 5;
        internal const int HCBT_CREATEWND = 3;

        internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetSystemMetrics(int nIndex);

        internal const int SM_CXSCREEN = 0;
        internal const int SM_CYSCREEN = 1;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        // =====================================================================
        // shell32.dll
        // =====================================================================
        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);
    }
}
