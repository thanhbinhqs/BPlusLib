// <copyright file="DarkModeHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Drawing;
using System.Runtime.InteropServices;
#if FEATURE_WINDOW_MODULE
using System.Windows.Forms;
#endif
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Applies Windows dark mode styling to WinForms controls and forms.
    /// Uses DWM + SetWindowTheme for Windows 10 1903+ dark mode support.
    /// </summary>
    public static class DarkModeHelper
    {
        // DWM
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int pvAttr, int cbAttr);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 34;

        // UxTheme
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string? appName, string? idList);

        // SystemParametersInfo
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint action, uint param, ref HIGHCONTRAST pStruct, uint winIni);
        private const uint SPI_GETHIGHCONTRAST = 0x0042;

        [StructLayout(LayoutKind.Sequential)]
        private struct HIGHCONTRAST
        {
            public int cbSize;
            public int dwFlags;
            public IntPtr lpszDefaultScheme;
        }

        private static readonly Color DarkBack = Color.FromArgb(32, 32, 32);
        private static readonly Color DarkFore = Color.FromArgb(240, 240, 240);
        private static readonly Color DarkControlBack = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkControlFore = Color.FromArgb(240, 240, 240);

        /// <summary>
        /// Checks if Windows dark mode is available (Windows 10 1903+).
        /// </summary>
        public static bool IsDarkModeAvailable()
        {
            try
            {
                // Check if DWMWA_USE_IMMERSIVE_DARK_MODE is supported
                IntPtr testHwnd = GetProcessWindow();
                if (testHwnd == IntPtr.Zero) return false;
                int val = 0;
                int hr = DwmSetWindowAttribute(testHwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
                return hr == 0 || hr == unchecked((int)0x80070005); // E_ACCESSDENIED means supported but need admin
            }
            catch { return false; }
        }

#if FEATURE_WINDOW_MODULE
        /// <summary>
        /// Applies dark mode to a form and optionally all child controls.
        /// </summary>
        public static bool ApplyDarkMode(Form form, bool recursive = true)
        {
            if (form is null) return false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

            try
            {
                // Apply to form handle
                ApplyToHwnd(form.Handle);

                // Set form colors
                form.BackColor = DarkBack;
                form.ForeColor = DarkFore;

                if (recursive)
                {
                    foreach (Control ctrl in form.Controls)
                    {
                        ApplyToControl(ctrl);
                    }
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Removes dark mode from a form.
        /// </summary>
        public static bool RemoveDarkMode(Form form, bool recursive = true)
        {
            if (form is null) return false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

            try
            {
                RemoveFromHwnd(form.Handle);
                form.BackColor = SystemColors.Control;
                form.ForeColor = SystemColors.ControlText;

                if (recursive)
                {
                    foreach (Control ctrl in form.Controls)
                    {
                        RemoveFromControl(ctrl);
                    }
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Applies dark mode to a single control.
        /// </summary>
        public static bool ApplyDarkMode(Control control)
        {
            if (control is null) return false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
            try
            {
                ApplyToControl(control);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Removes dark mode from a single control.
        /// </summary>
        public static bool RemoveDarkMode(Control control)
        {
            if (control is null) return false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
            try
            {
                RemoveFromControl(control);
                return true;
            }
            catch { return false; }
        }
#endif

        /// <summary>Gets the recommended dark background color.</summary>
        public static Color GetDarkBackColor() => DarkBack;

        /// <summary>Gets the recommended dark foreground color.</summary>
        public static Color GetDarkForeColor() => DarkFore;

#if FEATURE_WINDOW_MODULE
        private static void ApplyToControl(Control ctrl)
        {
            try
            {
                // Apply SetWindowTheme for native controls
                if (ctrl.IsHandleCreated)
                {
                    SetWindowTheme(ctrl.Handle, "DarkMode_Explorer", null);
                    ApplyToHwnd(ctrl.Handle);
                }

                // Set control colors
                if (ctrl is TextBox || ctrl is RichTextBox)
                {
                    ctrl.BackColor = DarkControlBack;
                    ctrl.ForeColor = DarkControlFore;
                }
                else if (ctrl is ListView || ctrl is TreeView || ctrl is DataGridView)
                {
                    ctrl.BackColor = DarkControlBack;
                    ctrl.ForeColor = DarkControlFore;
                }
                else if (ctrl is Button || ctrl is CheckBox || ctrl is RadioButton || ctrl is Label || ctrl is GroupBox)
                {
                    ctrl.BackColor = DarkBack;
                    ctrl.ForeColor = DarkFore;
                }
                else if (ctrl is ComboBox || ctrl is NumericUpDown || ctrl is TrackBar)
                {
                    ctrl.BackColor = DarkControlBack;
                    ctrl.ForeColor = DarkControlFore;
                }
                else if (ctrl is Panel || ctrl is TabControl || ctrl is TabPage)
                {
                    ctrl.BackColor = DarkBack;
                    ctrl.ForeColor = DarkFore;
                }
                else
                {
                    // Generic: just set colors
                    ctrl.BackColor = DarkBack;
                    ctrl.ForeColor = DarkFore;
                }
            }
            catch { /* best effort */ }
        }

        private static void RemoveFromControl(Control ctrl)
        {
            try
            {
                if (ctrl.IsHandleCreated)
                {
                    SetWindowTheme(ctrl.Handle, null, null);
                    RemoveFromHwnd(ctrl.Handle);
                }

                ctrl.BackColor = SystemColors.Control;
                ctrl.ForeColor = SystemColors.ControlText;
            }
            catch { /* best effort */ }
        }
#endif

        private static void ApplyToHwnd(IntPtr hwnd)
        {
            int val = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
        }

        private static void RemoveFromHwnd(IntPtr hwnd)
        {
            int val = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
        }

        private static IntPtr GetProcessWindow()
        {
            // Use desktop window handle for DWM API capability check
            IntPtr hwnd = User32.GetDesktopWindow();
            return hwnd != IntPtr.Zero ? hwnd : IntPtr.Zero;
        }
    }
}
