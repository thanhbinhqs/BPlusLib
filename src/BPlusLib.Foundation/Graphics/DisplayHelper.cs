// <copyright file="DisplayHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Graphics
{
    /// <summary>
    /// Provides display-related helper methods via Win32 P/Invoke —
    /// DPI queries, screen resolution changes, high-contrast detection,
    /// color depth, and scale factor. All methods are thread-safe and
    /// gracefully return safe defaults on non-Windows platforms.
    /// </summary>
    public static class DisplayHelper
    {
        // =====================================================================
        // Constants
        // =====================================================================

        // DEVMODE fields
        private const uint DM_PELSWIDTH = 0x00080000;
        private const uint DM_PELSHEIGHT = 0x00100000;
        private const uint DM_BITSPERPEL = 0x00040000;
        private const uint DM_DISPLAYFREQUENCY = 0x00400000;

        // ChangeDisplaySettings flags
        private const int CDS_UPDATEREGISTRY = 0x00000001;
        private const int CDS_TEST = 0x00000002;
        private const int CDS_FULLSCREEN = 0x00000004;
        private const int CDS_GLOBAL = 0x00000008;
        private const int CDS_SET_PRIMARY = 0x00000010;
        private const int CDS_RESET = 0x40000000;

        // ChangeDisplaySettings return values
        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const int DISP_CHANGE_RESTART = 1;
        private const int DISP_CHANGE_FAILED = -1;
        private const int DISP_CHANGE_BADMODE = -2;
        private const int DISP_CHANGE_NOTUPDATED = -3;
        private const int DISP_CHANGE_BADFLAGS = -4;
        private const int DISP_CHANGE_BADPARAM = -5;
        private const int DISP_CHANGE_BADDUALVIEW = -6;

        // SystemParametersInfo
        private const uint SPI_GETHIGHCONTRAST = 0x0042;
        private const uint SPIF_SENDCHANGE = 0x0002;

        // GetDeviceCaps indices
        private const int BITSPIXEL = 12;
        private const int PLANES = 14;
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
        private const int HORZRES = 8;
        private const int VERTRES = 10;

        // DpiType enum for GetDpiForMonitor
        private enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }

        // ScaleFactor type for GetScaleFactorForMonitor
        private enum ScaleFactorType
        {
            Invalid = 0,
            Scale100 = 100,
            Scale125 = 125,
            Scale140 = 140,
            Scale150 = 150,
            Scale175 = 175,
            Scale200 = 200,
            Scale225 = 225,
            Scale250 = 250,
            Scale300 = 300,
            Scale350 = 350,
            Scale400 = 400,
            Scale450 = 450,
            Scale500 = 500,
        }

        // =====================================================================
        // P/Invoke — User32
        // =====================================================================

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int ChangeDisplaySettingsW(ref DEVMODEW lpDevMode, uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ChangeDisplaySettingsW(IntPtr lpDevMode, uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplaySettingsW(
            string? lpszDeviceName,
            int iModeNum,
            ref DEVMODEW lpDevMode);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoW(
            uint uiAction,
            uint uiParam,
            IntPtr pvParam,
            uint fWinIni);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetDpiForWindowNative(IntPtr hwnd);

        // =====================================================================
        // P/Invoke — GDI32
        // =====================================================================

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDCW(string? pwszDriver, string? pwszDevice, string? pszPort, IntPtr pdm);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        // =====================================================================
        // P/Invoke — Shcore
        // =====================================================================

        [DllImport("shcore.dll", ExactSpelling = true)]
        private static extern int GetDpiForMonitor(
            IntPtr hmonitor,
            DpiType dpiType,
            out uint dpiX,
            out uint dpiY);

        [DllImport("shcore.dll", ExactSpelling = true)]
        private static extern int GetScaleFactorForMonitor(
            IntPtr hmonitor,
            out ScaleFactorType pScale);

        // =====================================================================
        // Win32 structs
        // =====================================================================

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
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIGHCONTRASTW
        {
            internal uint cbSize;
            internal uint dwFlags;
            // lpszDefaultScheme omitted — not needed
        }

        // HC flags
        private const uint HCF_HIGHCONTRASTON = 0x00000001;

        // =====================================================================
        // DPI queries
        // =====================================================================

        /// <summary>
        /// Gets the DPI for the specified window.
        /// Uses GetDpiForWindow (Windows 10 Anniversary Update+, 1607).
        /// Falls back to GDI device caps on older systems.
        /// </summary>
        /// <param name="hwnd">Handle to the window (HWND).</param>
        /// <returns>The DPI value (e.g., 96, 120, 144), or 96 on failure or non-Windows.</returns>
        public static int GetDpiForWindow(IntPtr hwnd)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return 96;
            }

            try
            {
                int dpi = GetDpiForWindowNative(hwnd);
                if (dpi > 0)
                {
                    return dpi;
                }
            }
            catch
            {
                // Fallback: GDI device caps
            }

            return GetGdiDpi().X;
        }

        /// <summary>
        /// Gets the DPI for a specific monitor handle.
        /// Uses GetDpiForMonitor from shcore.dll (Windows 8.1+).
        /// </summary>
        /// <param name="hmonitor">Handle to the monitor (HMONITOR).</param>
        /// <returns>A tuple (DpiX, DpiY), or (96, 96) on failure or non-Windows.</returns>
        public static (int DpiX, int DpiY) GetDpiForMonitor(IntPtr hmonitor)
        {
            if (hmonitor == IntPtr.Zero || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (96, 96);
            }

            try
            {
                if (GetDpiForMonitor(hmonitor, DpiType.Effective, out uint dpiX, out uint dpiY) == 0)
                {
                    return ((int)dpiX, (int)dpiY);
                }
            }
            catch
            {
                // shcore not available
            }

            return (96, 96);
        }

        // =====================================================================
        // Screen resolution
        // =====================================================================

        /// <summary>
        /// Sets the screen resolution for the primary display.
        /// Requires administrator privileges to succeed.
        /// </summary>
        /// <param name="width">Desired width in pixels.</param>
        /// <param name="height">Desired height in pixels.</param>
        /// <param name="bitsPerPixel">Color depth (default: 32).</param>
        /// <param name="refreshRate">Refresh rate in Hz (0 = default).</param>
        /// <returns>true if the resolution was changed successfully; otherwise, false.</returns>
        public static bool SetScreenResolution(
            int width,
            int height,
            int bitsPerPixel = 32,
            int refreshRate = 0)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            if (width <= 0 || height <= 0 || bitsPerPixel <= 0)
            {
                return false;
            }

            try
            {
                var dm = default(DEVMODEW);
                dm.dmSize = (ushort)Marshal.SizeOf<DEVMODEW>();
                dm.dmDeviceName = string.Empty;

                // Get current settings to fill in defaults
                if (!EnumDisplaySettingsW(null, -1, ref dm))
                {
                    return false;
                }

                // Override with requested values
                dm.dmPelsWidth = (uint)width;
                dm.dmPelsHeight = (uint)height;
                dm.dmBitsPerPel = (uint)bitsPerPixel;

                if (refreshRate > 0)
                {
                    dm.dmDisplayFrequency = (uint)refreshRate;
                }

                // Set the fields that are being modified
                dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL;
                if (refreshRate > 0)
                {
                    dm.dmFields |= DM_DISPLAYFREQUENCY;
                }

                int result = ChangeDisplaySettingsW(ref dm, CDS_TEST);
                if (result != DISP_CHANGE_SUCCESSFUL)
                {
                    return false;
                }

                result = ChangeDisplaySettingsW(ref dm, CDS_UPDATEREGISTRY);
                return result == DISP_CHANGE_SUCCESSFUL;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================================
        // High contrast
        // =====================================================================

        /// <summary>
        /// Determines whether the system is running in high-contrast mode.
        /// Uses SystemParametersInfoW with SPI_GETHIGHCONTRAST.
        /// </summary>
        /// <returns>true if high contrast is enabled; false on non-Windows or on failure.</returns>
        public static bool IsHighContrastMode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            try
            {
                var hc = new HIGHCONTRASTW
                {
                    cbSize = (uint)Marshal.SizeOf<HIGHCONTRASTW>(),
                };

                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<HIGHCONTRASTW>());
                try
                {
                    Marshal.StructureToPtr(hc, ptr, false);

                    if (SystemParametersInfoW(SPI_GETHIGHCONTRAST, hc.cbSize, ptr, 0))
                    {
                        hc = Marshal.PtrToStructure<HIGHCONTRASTW>(ptr);
                        return (hc.dwFlags & HCF_HIGHCONTRASTON) != 0;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        // =====================================================================
        // Color depth
        // =====================================================================

        /// <summary>
        /// Gets the current color depth (bits per pixel) for the primary display.
        /// Uses GetDeviceCaps with BITSPIXEL and PLANES.
        /// </summary>
        /// <returns>Color depth in bits per pixel (e.g., 32), or 0 on non-Windows.</returns>
        public static int GetColorDepth()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return 0;
            }

            IntPtr hdc = CreateDCW("DISPLAY", null, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                return 0;
            }

            try
            {
                int bitsPixel = GetDeviceCaps(hdc, BITSPIXEL);
                int planes = GetDeviceCaps(hdc, PLANES);
                return bitsPixel * planes;
            }
            catch
            {
                return 0;
            }
            finally
            {
                DeleteDC(hdc);
            }
        }

        // =====================================================================
        // Screen scale factor
        // =====================================================================

        /// <summary>
        /// Gets the current screen scale factor (e.g., 1.0 for 100%, 1.25 for 125%).
        /// Uses GetScaleFactorForMonitor from shcore.dll (Windows 8.1+),
        /// with fallback to GetDpiForMonitor / 96.0.
        /// </summary>
        /// <returns>The scale factor, or 1.0 on non-Windows or on failure.</returns>
        public static double GetScreenScaleFactor()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return 1.0;
            }

            try
            {
                // Try GetScaleFactorForMonitor first (Windows 8.1+)
                // We need an HMONITOR. Use MonitorFromPoint for the primary display center.
                var pt = default(POINT);
                pt.X = 0;
                pt.Y = 0;

                IntPtr hMonitor = MonitorFromPoint(pt, 2); // MONITOR_DEFAULTTONEAREST
                if (hMonitor != IntPtr.Zero)
                {
                    if (GetScaleFactorForMonitor(hMonitor, out ScaleFactorType scale) == 0)
                    {
                        if (scale >= ScaleFactorType.Scale100)
                        {
                            return (int)scale / 100.0;
                        }
                    }
                }
            }
            catch
            {
                // Fall through to DPI-based calculation
            }

            // Fallback: DPI-based calculation
            try
            {
                var dpi = GetGdiDpi();
                return dpi.X / 96.0;
            }
            catch
            {
                return 1.0;
            }
        }

        // =====================================================================
        // Internal helpers
        // =====================================================================

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            internal int X;
            internal int Y;
        }

        /// <summary>
        /// Gets DPI from the primary display via GDI GetDeviceCaps.
        /// </summary>
        private static (int X, int Y) GetGdiDpi()
        {
            IntPtr hdc = CreateDCW("DISPLAY", null, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                return (96, 96);
            }

            try
            {
                int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                int dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
                return (dpiX > 0 ? dpiX : 96, dpiY > 0 ? dpiY : 96);
            }
            catch
            {
                return (96, 96);
            }
            finally
            {
                DeleteDC(hdc);
            }
        }
    }
}
