// <copyright file="ThemeHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BPlusLib.Foundation.Shell
{
    /// <summary>
    /// Detects Windows dark/light theme, accent color, and DWM composition.
    /// </summary>
    public static class ThemeHelper
    {
        private const string ThemeRegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>True if the system is in light mode (apps).</summary>
        public static bool IsLightTheme() => !IsDarkTheme();

        /// <summary>True if the system is in dark mode (apps).</summary>
        public static bool IsDarkTheme() => !GetAppsThemeLightValue();

        /// <summary>True if apps use light theme.</summary>
        public static bool IsAppsLightTheme() => GetAppsThemeLightValue();

        /// <summary>True if the system taskbar uses light theme.</summary>
        public static bool IsSystemLightTheme() => GetSystemThemeLightValue();

        /// <summary>Gets the accent color as a 0xAARRGGBB integer, or 0 on failure.</summary>
        public static uint GetAccentColor()
        {
            try
            {
                if (DwmGetColorizationColor(out uint color, out _) == 0)
                    return color;
            }
            catch
            {
                // Non-Windows or unsupported
            }

            return 0;
        }

        /// <summary>Returns true if DWM composition is enabled.</summary>
        public static bool IsCompositionEnabled()
        {
            try
            {
                return DwmIsCompositionEnabled(out bool enabled) == 0 && enabled;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Applies dark mode to a window (Windows 10 20H1+ or 11).</summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="dark">True to enable dark mode, false to disable.</param>
        /// <returns>True if the operation succeeded.</returns>
        public static bool SetWindowDarkMode(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            try
            {
                int attr = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
                int val = dark ? 1 : 0;
                int hr = DwmSetWindowAttribute(hwnd, attr, ref val, Marshal.SizeOf<int>());
                return hr == 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool GetAppsThemeLightValue() => ReadThemeDword("AppsUseLightTheme", true);

        private static bool GetSystemThemeLightValue() => ReadThemeDword("SystemUsesLightTheme", true);

        private static bool ReadThemeDword(string valueName, bool defaultValue)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ThemeRegPath);
                if (key is null)
                    return defaultValue;

                object? val = key.GetValue(valueName);
                return val is int i ? i != 0 : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        // --- DWM P/Invoke ---

        [DllImport("dwmapi.dll", SetLastError = false)]
        private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

        [DllImport("dwmapi.dll", SetLastError = false)]
        private static extern int DwmIsCompositionEnabled(out bool pfEnabled);

        [DllImport("dwmapi.dll", SetLastError = false)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
