// <copyright file="MonitorHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Window
{
    /// <summary>
    /// Provides helper methods for working with display monitors.
    /// </summary>
    public static class MonitorHelper
    {
        /// <summary>
        /// Retrieves information about all display monitors.
        /// </summary>
        /// <returns>A read-only list of <see cref="MonitorInfo"/> for all monitors.</returns>
        public static IReadOnlyList<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo>();

            User32.MonitorEnumProc callback = (IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr dwData) =>
            {
                var info = GetMonitorInfoFromHandle(monitor);
                monitors.Add(info);
                return true;
            };

            User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return monitors.AsReadOnly();
        }

        /// <summary>
        /// Retrieves information about the primary display monitor.
        /// </summary>
        /// <returns>A <see cref="MonitorInfo"/> for the primary monitor.</returns>
        public static MonitorInfo GetPrimaryMonitor()
        {
            return GetAllMonitors().First(m => m.IsPrimary);
        }

        /// <summary>
        /// Retrieves information about the display monitor that contains the specified window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A <see cref="MonitorInfo"/> for the monitor containing the window.</returns>
        public static MonitorInfo GetMonitorFrom(IntPtr hwnd)
        {
            IntPtr hMonitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
            return GetMonitorInfoFromHandle(hMonitor);
        }

        /// <summary>
        /// Retrieves information about the display monitor that contains the specified point.
        /// </summary>
        /// <param name="point">The screen-coordinate point.</param>
        /// <returns>A <see cref="MonitorInfo"/> for the monitor containing the point.</returns>
        public static MonitorInfo GetMonitorFrom(Point point)
        {
            var pt = new POINT(point.X, point.Y);
            IntPtr hMonitor = User32.MonitorFromPoint(pt, User32.MONITOR_DEFAULTTONEAREST);
            return GetMonitorInfoFromHandle(hMonitor);
        }

        /// <summary>
        /// Gets the virtual screen rectangle encompassing all monitors.
        /// </summary>
        /// <returns>A <see cref="Rectangle"/> representing the virtual screen bounds.</returns>
        public static Rectangle GetVirtualScreen()
        {
            int x = User32.GetSystemMetrics(User32.SM_XVIRTUALSCREEN);
            int y = User32.GetSystemMetrics(User32.SM_YVIRTUALSCREEN);
            int cx = User32.GetSystemMetrics(User32.SM_CXVIRTUALSCREEN);
            int cy = User32.GetSystemMetrics(User32.SM_CYVIRTUALSCREEN);
            return new Rectangle(x, y, cx, cy);
        }

        /// <summary>
        /// Gets the working area of the primary display monitor.
        /// </summary>
        /// <returns>A <see cref="Rectangle"/> representing the working area.</returns>
        public static Rectangle GetWorkingArea()
        {
            var rect = default(RECT);
            User32.SystemParametersInfoW(User32.SPI_GETWORKAREA, 0, ref rect, 0);
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        /// <summary>
        /// Gets the working area of the monitor containing the specified window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A <see cref="Rectangle"/> representing the working area.</returns>
        public static Rectangle GetWorkingAreaFrom(IntPtr hwnd)
        {
            var monitorInfo = GetMonitorFrom(hwnd);
            return monitorInfo.WorkingArea;
        }

        /// <summary>
        /// Gets the DPI scale for the specified window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A <see cref="DpiScale"/> representing the DPI scaling factors.</returns>
        public static DpiScale GetDpiForWindow(IntPtr hwnd)
        {
            int dpi = User32.GetDpiForWindow(hwnd);
            float scale = dpi / 96f;
            return new DpiScale(scale, scale);
        }

        /// <summary>
        /// Determines whether the primary monitor has a high DPI setting (&gt; 96).
        /// </summary>
        /// <returns><c>true</c> if the primary monitor DPI exceeds 96; otherwise <c>false</c>.</returns>
        public static bool IsHighDpi()
        {
            var primary = GetPrimaryMonitor();
            return primary.Dpi > 96;
        }

        /// <summary>
        /// Retrieves the full <see cref="MonitorInfo"/> for a monitor handle.
        /// </summary>
        private static MonitorInfo GetMonitorInfoFromHandle(IntPtr hMonitor)
        {
            var info = new MONITORINFOEX();
            info.Init();

            if (!User32.GetMonitorInfoW(hMonitor, ref info))
            {
                return new MonitorInfo(
                    string.Empty,
                    Rectangle.Empty,
                    Rectangle.Empty,
                    false,
                    96,
                    string.Empty,
                    hMonitor);
            }

            User32.GetDpiForMonitor(hMonitor, User32.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
            int dpi = (int)dpiX;

            bool isPrimary = (info.dwFlags & 1) != 0; // MONITORINFOF_PRIMARY

            var bounds = Rectangle.FromLTRB(
                info.rcMonitor.Left, info.rcMonitor.Top,
                info.rcMonitor.Right, info.rcMonitor.Bottom);

            var workingArea = Rectangle.FromLTRB(
                info.rcWork.Left, info.rcWork.Top,
                info.rcWork.Right, info.rcWork.Bottom);

            string deviceName = info.szDevice?.TrimEnd('\0') ?? string.Empty;

            return new MonitorInfo(
                deviceName,
                bounds,
                workingArea,
                isPrimary,
                dpi,
                deviceName,
                hMonitor);
        }
    }

    /// <summary>
    /// Represents information about a display monitor.
    /// </summary>
    public readonly struct MonitorInfo
    {
        /// <summary>Gets the friendly name of the monitor.</summary>
        public string Name { get; }

        /// <summary>Gets the bounding rectangle of the monitor in screen coordinates.</summary>
        public Rectangle Bounds { get; }

        /// <summary>Gets the working area rectangle of the monitor (excluding taskbar).</summary>
        public Rectangle WorkingArea { get; }

        /// <summary>Gets a value indicating whether this monitor is the primary display.</summary>
        public bool IsPrimary { get; }

        /// <summary>Gets the DPI value of this monitor.</summary>
        public int Dpi { get; }

        /// <summary>Gets the device identifier string for this monitor.</summary>
        public string DeviceId { get; }

        /// <summary>Gets the native handle of this monitor.</summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorInfo"/> struct.
        /// </summary>
        public MonitorInfo(
            string name,
            Rectangle bounds,
            Rectangle workingArea,
            bool isPrimary,
            int dpi,
            string deviceId,
            IntPtr handle)
        {
            Name = name ?? string.Empty;
            Bounds = bounds;
            WorkingArea = workingArea;
            IsPrimary = isPrimary;
            Dpi = dpi;
            DeviceId = deviceId ?? string.Empty;
            Handle = handle;
        }
    }

    /// <summary>
    /// Represents DPI scaling factors for a display.
    /// </summary>
    public readonly struct DpiScale
    {
        /// <summary>Gets the horizontal DPI scale factor.</summary>
        public float X { get; }

        /// <summary>Gets the vertical DPI scale factor.</summary>
        public float Y { get; }

        /// <summary>
        /// Gets the average DPI scale factor (mean of X and Y).
        /// </summary>
        public float Scale => (X + Y) / 2f;

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiScale"/> struct.
        /// </summary>
        /// <param name="x">The horizontal scale factor.</param>
        /// <param name="y">The vertical scale factor.</param>
        public DpiScale(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
