// <copyright file="SafeNativeMethods.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;
using BPlusLib.Foundation.Native.SafeHandles;

namespace BPlusLib.Foundation
{
    /// <summary>
    /// Safe wrappers around Win32 P/Invoke calls that return <see cref="Result{T}"/>
    /// instead of throwing raw exceptions or requiring manual error checking.
    /// </summary>
    internal static class SafeNativeMethods
    {
        /// <summary>
        /// Safely retrieves the bounding rectangle of the specified window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A <see cref="Result{RECT}"/> containing the rectangle on success,
        /// or a failure with <see cref="Win32Exception"/> on error.</returns>
        internal static Result<RECT> GetWindowRectSafe(IntPtr hwnd)
        {
            if (!User32.GetWindowRect(hwnd, out RECT rect))
            {
                int error = Marshal.GetLastWin32Error();
                return Result<RECT>.Fail(new Win32Exception(error));
            }

            return Result<RECT>.Ok(rect);
        }

        /// <summary>
        /// Safely retrieves information about a display monitor.
        /// </summary>
        /// <param name="hmonitor">Handle to the display monitor.</param>
        /// <returns>A <see cref="Result{MONITORINFO}"/> containing monitor information on success,
        /// or a failure with <see cref="Win32Exception"/> on error.</returns>
        internal static Result<MONITORINFO> GetMonitorInfoSafe(IntPtr hmonitor)
        {
            var mi = default(MONITORINFO);
            mi.Init();

            if (!User32.GetMonitorInfoW(hmonitor, ref mi))
            {
                int error = Marshal.GetLastWin32Error();
                return Result<MONITORINFO>.Fail(new Win32Exception(error));
            }

            return Result<MONITORINFO>.Ok(mi);
        }

        /// <summary>
        /// Safely gets the DPI value for a window (Windows 10+).
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A <see cref="Result{Int32}"/> containing the DPI value on success,
        /// or a failure with <see cref="Win32Exception"/> on error (zero return).</returns>
        /// <remarks>Returns 0 on platforms earlier than Windows 10.</remarks>
        internal static Result<int> GetDpiForWindowSafe(IntPtr hwnd)
        {
            int dpi = User32.GetDpiForWindow(hwnd);
            if (dpi == 0)
            {
                int error = Marshal.GetLastWin32Error();
                return Result<int>.Fail(new Win32Exception(error));
            }

            return Result<int>.Ok(dpi);
        }

        /// <summary>
        /// Safely retrieves the text of the specified window's title bar.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A <see cref="Result{String}"/> containing the window text on success,
        /// or a failure with <see cref="Win32Exception"/> on error.</returns>
        internal static Result<string> GetWindowTextSafe(IntPtr hwnd)
        {
            int length = User32.GetWindowTextLengthW(hwnd);
            if (length == 0)
            {
                int error = Marshal.GetLastWin32Error();
                // Zero-length text is valid (empty title bar), but GetWindowTextLengthW
                // returns 0 both for empty strings and errors. Check last error.
                if (error != 0)
                    return Result<string>.Fail(new Win32Exception(error));
                return Result<string>.Ok(string.Empty);
            }

            var sb = new StringBuilder(length + 1);
            int charsCopied = User32.GetWindowTextW(hwnd, sb, sb.Capacity);
            if (charsCopied == 0)
            {
                int error = Marshal.GetLastWin32Error();
                return Result<string>.Fail(new Win32Exception(error));
            }

            return Result<string>.Ok(sb.ToString(0, charsCopied));
        }

        /// <summary>
        /// Safely opens an existing process and wraps the handle in a <see cref="SafeProcessHandle"/>.
        /// </summary>
        /// <param name="access">The desired access mask.</param>
        /// <param name="processId">The process ID to open.</param>
        /// <returns>A <see cref="Result{SafeProcessHandle}"/> containing a safe process handle on success,
        /// or a failure with <see cref="Win32Exception"/> on error.</returns>
        internal static Result<SafeProcessHandle> OpenProcessSafe(uint access, int processId)
        {
            IntPtr rawHandle = Kernel32.OpenProcess(access, false, processId);
            if (rawHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                return Result<SafeProcessHandle>.Fail(new Win32Exception(error));
            }

            return Result<SafeProcessHandle>.Ok(new SafeProcessHandle(rawHandle, true));
        }

        /// <summary>
        /// Safely duplicates a handle from a source process into the current process.
        /// </summary>
        /// <param name="sourceProcess">Safe handle to the source process.</param>
        /// <param name="sourceHandle">The handle value to duplicate.</param>
        /// <returns>A <see cref="Result{T}"/> of <see cref="IntPtr"/> containing the duplicated handle on success,
        /// or a failure with <see cref="Win32Exception"/> on error.</returns>
        internal static Result<IntPtr> DuplicateHandleSafe(SafeProcessHandle sourceProcess, IntPtr sourceHandle)
        {
            IntPtr currentProcess = Kernel32.GetCurrentProcess();

            if (!Kernel32.DuplicateHandle(
                    sourceProcess.DangerousGetHandle(),
                    sourceHandle,
                    currentProcess,
                    out IntPtr duplicatedHandle,
                    0,
                    false,
                    Kernel32.DuplicateSameAccess))
            {
                int error = Marshal.GetLastWin32Error();
                return Result<IntPtr>.Fail(new Win32Exception(error));
            }

            return Result<IntPtr>.Ok(duplicatedHandle);
        }
    }
}
