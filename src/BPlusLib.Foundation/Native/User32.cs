// <copyright file="User32.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for user32.dll — window, dialog, and display management.
    /// </summary>
    internal static class User32
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary>SWP_NOSIZE — Retains current size.</summary>
        internal const uint SWP_NOSIZE = 0x0001;

        /// <summary>SWP_NOZORDER — Retains current Z order.</summary>
        internal const uint SWP_NOZORDER = 0x0004;

        /// <summary>SWP_NOACTIVATE — Does not activate the window.</summary>
        internal const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>HWND_TOPMOST — Places window above all non-topmost windows.</summary>
        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        /// <summary>WH_CBT — Hook type for CBT (Computer-Based Training).</summary>
        internal const int WH_CBT = 5;

        /// <summary>HCBT_ACTIVATE — CBT hook code for window activation.</summary>
        internal const int HCBT_ACTIVATE = 5;

        /// <summary>HCBT_CREATEWND — CBT hook code for window creation.</summary>
        internal const int HCBT_CREATEWND = 3;

        /// <summary>SM_CXSCREEN — System metric for screen width.</summary>
        internal const int SM_CXSCREEN = 0;

        /// <summary>SM_CYSCREEN — System metric for screen height.</summary>
        internal const int SM_CYSCREEN = 1;

        /// <summary>SM_XVIRTUALSCREEN — Virtual screen origin X.</summary>
        internal const int SM_XVIRTUALSCREEN = 76;

        /// <summary>SM_YVIRTUALSCREEN — Virtual screen origin Y.</summary>
        internal const int SM_YVIRTUALSCREEN = 77;

        /// <summary>SM_CXVIRTUALSCREEN — Virtual screen width.</summary>
        internal const int SM_CXVIRTUALSCREEN = 78;

        /// <summary>SM_CYVIRTUALSCREEN — Virtual screen height.</summary>
        internal const int SM_CYVIRTUALSCREEN = 79;

        // =====================================================================
        // Window message constants
        // =====================================================================

        /// <summary>WM_NCHITTEST — Sent to determine which part of a window contains the cursor.</summary>
        internal const int WM_NCHITTEST = 0x0084;

        /// <summary>WM_GETMINMAXINFO — Sent when the window is about to be moved or resized.</summary>
        internal const int WM_GETMINMAXINFO = 0x0024;

        /// <summary>WM_CLOSE — Sent as a signal that a window or application should terminate.</summary>
        internal const int WM_CLOSE = 0x0010;

        // =====================================================================
        // Hit-test values
        // =====================================================================

        /// <summary>HTCAPTION — In the title bar area.</summary>
        internal const int HTCAPTION = 2;

        /// <summary>HTCLIENT — In the client area.</summary>
        internal const int HTCLIENT = 1;

        /// <summary>HTLEFT — In the left border.</summary>
        internal const int HTLEFT = 10;

        /// <summary>HTRIGHT — In the right border.</summary>
        internal const int HTRIGHT = 11;

        /// <summary>HTTOP — In the top border.</summary>
        internal const int HTTOP = 12;

        /// <summary>HTTOPLEFT — In the top-left corner.</summary>
        internal const int HTTOPLEFT = 13;

        /// <summary>HTTOPRIGHT — In the top-right corner.</summary>
        internal const int HTTOPRIGHT = 14;

        /// <summary>HTBOTTOM — In the bottom border.</summary>
        internal const int HTBOTTOM = 15;

        /// <summary>HTBOTTOMLEFT — In the bottom-left corner.</summary>
        internal const int HTBOTTOMLEFT = 16;

        /// <summary>HTBOTTOMRIGHT — In the bottom-right corner.</summary>
        internal const int HTBOTTOMRIGHT = 17;

        // =====================================================================
        // Delegates
        // =====================================================================

        /// <summary>Callback for hook procedures (SetWindowsHookEx).</summary>
        internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        // =====================================================================
        // Dialog and message box
        // =====================================================================

        /// <summary>
        /// Displays a modal dialog box with message and caption text.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int MessageBox(
            IntPtr hWnd, string text, string caption, uint type);

        // =====================================================================
        // Hooks
        // =====================================================================

        /// <summary>
        /// Installs a hook procedure to be monitored by the system.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);

        /// <summary>
        /// Removes a previously installed hook procedure.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Passes hook information to the next hook procedure in the chain.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        // =====================================================================
        // Window geometry and parent
        // =====================================================================

        /// <summary>
        /// Retrieves the bounding rectangle of the specified window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Retrieves the handle to the parent of the specified window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        /// Changes the size, position, and Z order of a window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// Retrieves the specified system metric or configuration setting.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetSystemMetrics(int nIndex);

        /// <summary>
        /// Retrieves the thread and process IDs for the specified window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(
            IntPtr hWnd, out int lpdwProcessId);

        // =====================================================================
        // Window visibility and state
        // =====================================================================

        /// <summary>Sets the specified window's show state.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>Sets the show state of a window asynchronously.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Determines the visibility state of the specified window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>Determines whether a window is maximized.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsZoomed(IntPtr hWnd);

        /// <summary>Determines whether a window is minimized.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsIconic(IntPtr hWnd);

        /// <summary>Retrieves the handle to the ancestor of the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        /// <summary>GA_PARENT = 1, GA_ROOT = 2, GA_ROOTOWNER = 3.</summary>
        internal const uint GA_PARENT = 1;
        internal const uint GA_ROOT = 2;
        internal const uint GA_ROOTOWNER = 3;

        // =====================================================================
        // Window messages
        // =====================================================================

        /// <summary>Sends a message to a window or windows.</summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>Sends a message with string parameter.</summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessage(
            IntPtr hWnd, uint Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        /// <summary>Posts a message to a window's message queue.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>Retrieves a message from the calling thread's message queue.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMessage(
            out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        /// <summary>Peeks at messages in the calling thread's message queue.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PeekMessage(
            out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        // =====================================================================
        // Window manipulation
        // =====================================================================

        /// <summary>Flashes the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlashWindowEx(ref FLASHWINFO pfwi);

        /// <summary>FLASHW_STOP — Stop flashing.</summary>
        internal const uint FLASHW_STOP = 0;

        /// <summary>FLASHW_CAPTION — Flash the window caption.</summary>
        internal const uint FLASHW_CAPTION = 1;

        /// <summary>FLASHW_TRAY — Flash the taskbar button.</summary>
        internal const uint FLASHW_TRAY = 2;

        /// <summary>FLASHW_ALL — Flash both caption and taskbar button.</summary>
        internal const uint FLASHW_ALL = 3;

        /// <summary>FLASHW_TIMER — Flash continuously until FLASHW_STOP.</summary>
        internal const uint FLASHW_TIMER = 4;

        /// <summary>FLASHW_TIMERNOFG — Flash continuously until window comes to foreground.</summary>
        internal const uint FLASHW_TIMERNOFG = 12;

        /// <summary>Destroys the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(IntPtr hWnd);

        /// <summary>Marks the specified rectangle for repainting.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InvalidateRect(
            IntPtr hWnd, IntPtr lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

        /// <summary>Updates the client area of the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UpdateWindow(IntPtr hWnd);

        /// <summary>Sets the specified window as the foreground window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>Brings the specified window to the top of the Z order.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BringWindowToTop(IntPtr hWnd);

        // =====================================================================
        // Window long / styles
        // =====================================================================

        /// <summary>Changes an attribute of the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowLong(
            IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>Retrieves an attribute of the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>GWL_WNDPROC, GWL_HINSTANCE, GWL_ID, GWL_STYLE, GWL_EXSTYLE.</summary>
        internal const int GWL_WNDPROC = -4;
        internal const int GWL_HINSTANCE = -6;
        internal const int GWL_ID = -12;
        internal const int GWL_STYLE = -16;
        internal const int GWL_EXSTYLE = -20;

        // =====================================================================
        // Client area and coordinate conversion
        // =====================================================================

        /// <summary>Retrieves the client area rectangle.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>Converts screen coordinates to client coordinates.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>Converts client coordinates to screen coordinates.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        // =====================================================================
        // Window text
        // =====================================================================

        /// <summary>
        /// Retrieves the text of the specified window's title bar.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextW(
            IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Retrieves the length of the specified window's title bar text.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowTextLengthW(IntPtr hWnd);

        // =====================================================================
        // Display / DPI
        // =====================================================================

        /// <summary>
        /// Enumerates all display monitors.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(
            IntPtr hdc, IntPtr lprcClip,
            MonitorEnumProc lpfnEnum, IntPtr dwData);

        /// <summary>Callback for EnumDisplayMonitors.</summary>
        /// <returns>True to continue enumeration.</returns>
        internal delegate bool MonitorEnumProc(
            IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr dwData);

        /// <summary>
        /// Retrieves the handle to the display monitor that contains a specified window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        /// Retrieves the handle to the display monitor that contains a specified point.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        /// <summary>
        /// Retrieves information about a display monitor.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfoW(
            IntPtr hMonitor, ref MONITORINFO lpmi);

        /// <summary>
        /// Retrieves information about a display monitor (extended version with device name).
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfoW(
            IntPtr hMonitor, ref MONITORINFOEX lpmi);

        /// <summary>
        /// Gets the DPI value for a window (Windows 10+).
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetDpiForWindow(IntPtr hwnd);

        /// <summary>
        /// Gets the DPI value for a monitor (shcore.dll).
        /// </summary>
        [DllImport("shcore.dll", ExactSpelling = true)]
        internal static extern int GetDpiForMonitor(
            IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        /// <summary>MDT_EFFECTIVE_DPI = 0</summary>
        internal const int MDT_EFFECTIVE_DPI = 0;
        /// <summary>MDT_ANGULAR_DPI = 1</summary>
        internal const int MDT_ANGULAR_DPI = 1;
        /// <summary>MDT_RAW_DPI = 2</summary>
        internal const int MDT_RAW_DPI = 2;

        /// <summary>MONITOR_DEFAULTTONULL — Returns null if no monitor contains the point/window.</summary>
        internal const uint MONITOR_DEFAULTTONULL = 0;
        /// <summary>MONITOR_DEFAULTTOPRIMARY — Returns primary monitor if none found.</summary>
        internal const uint MONITOR_DEFAULTTOPRIMARY = 1;
        /// <summary>MONITOR_DEFAULTTONEAREST — Returns nearest monitor if none found.</summary>
        internal const uint MONITOR_DEFAULTTONEAREST = 2;

        // =====================================================================
        // System parameters
        // =====================================================================

        /// <summary>
        /// Retrieves or sets system-wide parameters.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfoW(
            uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        /// <summary>
        /// Retrieves or sets system-wide parameters (RECT variant).
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfoW(
            uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        /// <summary>SPI_GETWORKAREA — Retrieves the size of the work area on the primary display monitor.</summary>
        internal const uint SPI_GETWORKAREA = 0x0030;
    }
}
