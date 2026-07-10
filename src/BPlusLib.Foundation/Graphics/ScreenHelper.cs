// <copyright file="ScreenHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Graphics
{
    /// <summary>
    /// Represents a rectangle defined by integer coordinates.
    /// Used in place of <see cref="System.Drawing.Rectangle"/> to avoid
    /// a dependency on System.Drawing on non-Windows targets.
    /// </summary>
    public readonly struct DisplayRect : IEquatable<DisplayRect>
    {
        /// <summary>Initializes a new instance of the <see cref="DisplayRect"/> struct.</summary>
        /// <param name="x">The x-coordinate of the upper-left corner.</param>
        /// <param name="y">The y-coordinate of the upper-left corner.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public DisplayRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Gets the x-coordinate of the upper-left corner.</summary>
        public int X { get; }

        /// <summary>Gets the y-coordinate of the upper-left corner.</summary>
        public int Y { get; }

        /// <summary>Gets the width.</summary>
        public int Width { get; }

        /// <summary>Gets the height.</summary>
        public int Height { get; }

        /// <summary>Gets the x-coordinate of the left edge (same as <see cref="X"/>).</summary>
        public int Left => X;

        /// <summary>Gets the y-coordinate of the top edge (same as <see cref="Y"/>).</summary>
        public int Top => Y;

        /// <summary>Gets the x-coordinate of the right edge.</summary>
        public int Right => X + Width;

        /// <summary>Gets the y-coordinate of the bottom edge.</summary>
        public int Bottom => Y + Height;

        /// <summary>Gets a value indicating whether this rectangle has zero area.</summary>
        public bool IsEmpty => Width <= 0 || Height <= 0;

        /// <inheritdoc/>
        public bool Equals(DisplayRect other) =>
            X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DisplayRect other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        /// <inheritdoc/>
        public override string ToString() =>
            $"{{X={X},Y={Y},Width={Width},Height={Height}}}";

        /// <summary>Equality operator.</summary>
        public static bool operator ==(DisplayRect left, DisplayRect right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(DisplayRect left, DisplayRect right) => !left.Equals(right);

        /// <summary>Returns a rectangle with all values set to zero.</summary>
        public static DisplayRect Empty => default;
    }

    /// <summary>
    /// Provides detailed information about a single display monitor.
    /// </summary>
    public sealed class DisplayInfo
    {
        /// <summary>
        /// Gets or sets the Windows device name (e.g., "\\.\DISPLAY1").
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable device string (e.g., "Generic PnP Monitor").
        /// </summary>
        public string DeviceString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display bounds in virtual-screen coordinates.
        /// </summary>
        public DisplayRect Bounds { get; set; }

        /// <summary>
        /// Gets or sets the working area (excluding taskbar etc.).
        /// </summary>
        public DisplayRect WorkingArea { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the primary display.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the DPI along the X (horizontal) axis.
        /// </summary>
        public int DpiX { get; set; }

        /// <summary>
        /// Gets or sets the DPI along the Y (vertical) axis.
        /// </summary>
        public int DpiY { get; set; }

        /// <summary>
        /// Gets or sets the color depth in bits per pixel.
        /// </summary>
        public int BitsPerPixel { get; set; }

        /// <summary>
        /// Gets or sets the display refresh rate in hertz.
        /// </summary>
        public int RefreshRate { get; set; }

        /// <inheritdoc/>
        public override string ToString() =>
            $"{DeviceName} ({DeviceString}) — {Bounds.Width}x{Bounds.Height} @ {RefreshRate}Hz, {BitsPerPixel}bpp, DPI={DpiX}x{DpiY}, Primary={IsPrimary}";
    }

    /// <summary>
    /// Provides methods for screen capture and display information retrieval
    /// via Win32 P/Invoke (GDI32, User32, Shcore). All methods are thread-safe
    /// and gracefully return default values on non-Windows platforms.
    /// </summary>
    public static class ScreenHelper
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const uint SRCCOPY = 0x00CC0020;
        private const int DIB_RGB_COLORS = 0;

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int SM_CMONITORS = 80;

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        private const int ENUM_CURRENT_SETTINGS = -1;

        // DISPLAY_DEVICE state flags
        private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;
        private const uint DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008;

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        // =====================================================================
        // P/Invoke — GDI32
        // =====================================================================

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDCW(string? pwszDriver, string? pwszDevice, string? pszPort, IntPtr pdm);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern int GetDIBits(
            IntPtr hdc,
            IntPtr hbm,
            uint start,
            uint cLines,
            IntPtr lpvBits,
            ref BITMAPINFOHEADER lpbmi,
            uint usage);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr ho);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        // =====================================================================
        // P/Invoke — User32
        // =====================================================================

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplayDevicesW(
            string? lpDevice,
            uint iDevNum,
            ref DISPLAY_DEVICEW lpDisplayDevice,
            uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplaySettingsW(
            string? lpszDeviceName,
            int iModeNum,
            ref DEVMODEW lpDevMode);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        // =====================================================================
        // P/Invoke — Shcore
        // =====================================================================

        [DllImport("shcore.dll", ExactSpelling = true)]
        private static extern int GetDpiForMonitor(
            IntPtr hmonitor,
            DpiType dpiType,
            out uint dpiX,
            out uint dpiY);

        // =====================================================================
        // Win32 structs
        // =====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            internal int X;
            internal int Y;

            internal POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICEW
        {
            internal uint cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceString;
            internal uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODEW
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            internal string dmDeviceName;
            internal ushort dmSpecVersion;
            internal ushort dmDriverVersion;
            internal ushort dmSize;
            internal ushort dmDriverExtra;
            internal uint dmFields;

            // POINTL dmPosition
            internal int dmPositionX;
            internal int dmPositionY;

            internal uint dmDisplayOrientation;
            internal uint dmDisplayFixedOutput;

            internal short dmColor;
            internal short dmDuplex;
            internal short dmYResolution;
            internal short dmTTOption;
            internal short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            internal string dmFormName;

            internal ushort dmLogPixels;
            internal uint dmBitsPerPel;
            internal uint dmPelsWidth;
            internal uint dmPelsHeight;
            internal uint dmDisplayFlags;
            internal uint dmDisplayFrequency;

            // Remainder omitted — not needed for our queries
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            internal uint biSize;
            internal int biWidth;
            internal int biHeight;
            internal ushort biPlanes;
            internal ushort biBitCount;
            internal uint biCompression;
            internal uint biSizeImage;
            internal int biXPelsPerMeter;
            internal int biYPelsPerMeter;
            internal uint biClrUsed;
            internal uint biImportant;
        }

        private enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }

        // =====================================================================
        // Screen capture
        // =====================================================================

        /// <summary>
        /// Captures the entire virtual screen using GDI and returns raw BGRA pixel data.
        /// Only works on Windows. Returns null on non-Windows or on failure.
        /// </summary>
        /// <returns>A tuple of (pixelData, width, height), or null if capture failed or not on Windows.</returns>
        public static (byte[] PixelData, int Width, int Height)? CaptureScreenRaw()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            IntPtr displayDC = IntPtr.Zero;
            IntPtr memDC = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                var bounds = GetVirtualScreenBounds();
                int width = bounds.Width;
                int height = bounds.Height;

                if (width <= 0 || height <= 0)
                {
                    return null;
                }

                displayDC = CreateDCW("DISPLAY", null, null, IntPtr.Zero);
                if (displayDC == IntPtr.Zero)
                {
                    return null;
                }

                memDC = CreateCompatibleDC(displayDC);
                if (memDC == IntPtr.Zero)
                {
                    return null;
                }

                hBitmap = CreateCompatibleBitmap(displayDC, width, height);
                if (hBitmap == IntPtr.Zero)
                {
                    return null;
                }

                oldBitmap = SelectObject(memDC, hBitmap);

                if (!BitBlt(memDC, 0, 0, width, height, displayDC,
                            bounds.X, bounds.Y, SRCCOPY))
                {
                    return null;
                }

                // Set up BITMAPINFOHEADER for a 32-bit top-down bitmap
                var bmi = new BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // negative = top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0, // BI_RGB
                };

                int stride = width * 4;
                int bufferSize = stride * height;
                byte[] pixelData = new byte[bufferSize];

                GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                try
                {
                    int ret = GetDIBits(
                        memDC,
                        hBitmap,
                        0,
                        (uint)height,
                        handle.AddrOfPinnedObject(),
                        ref bmi,
                        DIB_RGB_COLORS);

                    if (ret == 0)
                    {
                        return null;
                    }

                    return (pixelData, width, height);
                }
                finally
                {
                    handle.Free();
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero)
                {
                    SelectObject(memDC, oldBitmap);
                }

                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }

                if (memDC != IntPtr.Zero)
                {
                    DeleteDC(memDC);
                }

                if (displayDC != IntPtr.Zero)
                {
                    DeleteDC(displayDC);
                }
            }
        }

#if NETFRAMEWORK
        /// <summary>
        /// Captures the entire virtual screen and returns it as a PNG byte array.
        /// Uses System.Drawing on .NET Framework. Returns null on non-Windows or on failure.
        /// </summary>
        /// <returns>PNG-encoded image bytes, or null on failure.</returns>
        public static byte[]? CaptureScreenAsPng()
        {
            var raw = CaptureScreenRaw();
            if (raw == null)
            {
                return null;
            }

            var (pixelData, width, height) = raw.Value;

            try
            {
                using var bitmap = new System.Drawing.Bitmap(
                    width,
                    height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                var bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                try
                {
                    int srcStride = width * 4;
                    int dstStride = bitmapData.Stride;
                    if (srcStride == dstStride)
                    {
                        Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Marshal.Copy(
                                pixelData,
                                y * srcStride,
                                bitmapData.Scan0 + (y * dstStride),
                                srcStride);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                using var ms = new System.IO.MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
#endif

        // =====================================================================
        // Display information
        // =====================================================================

        /// <summary>
        /// Gets the bounding rectangle of the entire virtual screen
        /// (all monitors combined via GetSystemMetrics).
        /// </summary>
        /// <returns>A <see cref="DisplayRect"/> spanning all displays, or <see cref="DisplayRect.Empty"/> on failure.</returns>
        public static DisplayRect GetVirtualScreenBounds()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return DisplayRect.Empty;
            }

            try
            {
                int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
                int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
                int cx = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                int cy = GetSystemMetrics(SM_CYVIRTUALSCREEN);
                return new DisplayRect(x, y, cx, cy);
            }
            catch
            {
                return DisplayRect.Empty;
            }
        }

        /// <summary>
        /// Gets the number of display monitors on the desktop.
        /// </summary>
        /// <returns>Monitor count, or 0 on non-Windows or on failure.</returns>
        public static int GetMonitorCount()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return 0;
            }

            try
            {
                return GetSystemMetrics(SM_CMONITORS);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Enumerates all displays and returns their information.
        /// Thread-safe; on non-Windows returns an empty list.
        /// </summary>
        /// <returns>A read-only list of <see cref="DisplayInfo"/> instances.</returns>
        public static IReadOnlyList<DisplayInfo> GetAllDisplays()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Array.Empty<DisplayInfo>();
            }

            var result = new List<DisplayInfo>();
            var dd = default(DISPLAY_DEVICEW);
            dd.cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>();

            uint devNum = 0;
            while (EnumDisplayDevicesW(null, devNum, ref dd, 0))
            {
                // Skip mirroring drivers
                if ((dd.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) != 0)
                {
                    devNum++;
                    dd.cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>();
                    continue;
                }

                // Only process outputs attached to the desktop
                if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                {
                    var info = new DisplayInfo
                    {
                        DeviceName = dd.DeviceName ?? string.Empty,
                        DeviceString = dd.DeviceString ?? string.Empty,
                        IsPrimary = (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                    };

                    // Get current settings via EnumDisplaySettingsW
                    var dm = default(DEVMODEW);
                    dm.dmSize = (ushort)Marshal.SizeOf<DEVMODEW>();

                    if (EnumDisplaySettingsW(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
                    {
                        info.Bounds = new DisplayRect(
                            dm.dmPositionX,
                            dm.dmPositionY,
                            (int)dm.dmPelsWidth,
                            (int)dm.dmPelsHeight);
                        info.BitsPerPixel = (int)dm.dmBitsPerPel;
                        info.RefreshRate = (int)dm.dmDisplayFrequency;
                    }

                    // Get DPI via GetDpiForMonitor (Win 8.1+)
                    var dpi = GetDpiFromPoint(
                        dm.dmPositionX + (int)(dm.dmPelsWidth / 2),
                        dm.dmPositionY + (int)(dm.dmPelsHeight / 2));
                    info.DpiX = dpi.DpiX;
                    info.DpiY = dpi.DpiY;

                    // Working area — approximate as bounds for now
                    info.WorkingArea = info.Bounds;

                    result.Add(info);
                }

                devNum++;
                dd.cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>();
            }

            return result.AsReadOnly();
        }

        /// <summary>
        /// Gets information about the primary display, or null if not found.
        /// </summary>
        /// <returns>A <see cref="DisplayInfo"/> for the primary monitor, or null.</returns>
        public static DisplayInfo? GetPrimaryDisplay()
        {
            var displays = GetAllDisplays();
            foreach (var d in displays)
            {
                if (d.IsPrimary)
                {
                    return d;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the display's DPI from an HMONITOR obtained via MonitorFromPoint.
        /// Falls back to GDI GetDeviceCaps if GetDpiForMonitor is not available.
        /// </summary>
        private static (int DpiX, int DpiY) GetDpiFromPoint(int x, int y)
        {
            try
            {
                var pt = new POINT(x, y);
                IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                if (hMonitor != IntPtr.Zero)
                {
                    if (GetDpiForMonitor(hMonitor, DpiType.Effective, out uint dpiX, out uint dpiY) == 0)
                    {
                        return ((int)dpiX, (int)dpiY);
                    }
                }
            }
            catch
            {
                // shcore.dll not available (pre-Win 8.1) — fall through to GDI
            }

            // Fallback: use GDI device caps
            return GetGdiDpi();
        }

        /// <summary>
        /// Gets DPI from a display DC via GDI GetDeviceCaps.
        /// </summary>
        private static (int DpiX, int DpiY) GetGdiDpi()
        {
            try
            {
                IntPtr hdc = CreateDCW("DISPLAY", null, null, IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                        int dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
                        return (dpiX > 0 ? dpiX : 96, dpiY > 0 ? dpiY : 96);
                    }
                    finally
                    {
                        DeleteDC(hdc);
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return (96, 96);
        }

        /// <summary>
        /// Gets DPI for a specific point on the screen.
        /// </summary>
        internal static (int DpiX, int DpiY) GetDpiForScreenPoint(int x, int y)
        {
            return GetDpiFromPoint(x, y);
        }
    }
}
