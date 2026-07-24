// <copyright file="CustomWindowHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Provides helper methods for custom window chrome with Aero snap support.
    /// Use these methods in your WndProc override to handle WM_NCCALCSIZE and WM_NCHITTEST.
    /// </summary>
    public static class CustomWindowHelper
    {
        // NCHitTest constants
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTSYSMENU = 3;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        // DWM
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int pvAttr, int cbAttr);

        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMNCRP_DISABLED = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int left, top, right, bottom;
        }

        /// <summary>
        /// Handles WM_NCHITTEST — determines which part of the window the cursor is over.
        /// Return this value from your WndProc when msg == 0x0084 (WM_NCHITTEST).
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <param name="screenX">Screen X coordinate from lParam.</param>
        /// <param name="screenY">Screen Y coordinate from lParam.</param>
        /// <param name="borderSize">Resize border width in pixels (default: 8).</param>
        /// <returns>HTCAPTION, HTLEFT, HTTOP, etc.</returns>
        public static int HandleNCHitTest(IntPtr hwnd, int screenX, int screenY, int borderSize = 8)
        {
            if (hwnd == IntPtr.Zero) return HTCLIENT;

            if (!User32.GetWindowRect(hwnd, out RECT rect))
                return HTCLIENT;

            // Determine region
            int x = screenX;
            int y = screenY;

            bool top = y < rect.Top + borderSize;
            bool bottom = y >= rect.Bottom - borderSize;
            bool left = x < rect.Left + borderSize;
            bool right = x >= rect.Right - borderSize;

            if (top && left) return HTTOPLEFT;
            if (top && right) return HTTOPRIGHT;
            if (bottom && left) return HTBOTTOMLEFT;
            if (bottom && right) return HTBOTTOMRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;

            // Caption area (top 30px of the client area)
            int captionHeight = GetCaptionHeight(hwnd);
            if (y < rect.Top + borderSize + captionHeight)
                return HTCAPTION;

            return HTCLIENT;
        }

        /// <summary>
        /// Handles WM_NCCALCSIZE — removes the default title bar for custom chrome.
        /// Call this from WndProc when msg == 0x0083 (WM_NCCALCSIZE) and wParam is TRUE.
        /// Note: internal because RECT is internal. Use overloads for public scenarios.
        /// </summary>
        internal static void HandleNCCalcSize(ref RECT rect, bool removeBorder = true)
        {
            if (!removeBorder) return;

            // Remove standard borders/title bar
            rect.Left += 1;
            rect.Top += 1;
            rect.Right -= 1;
            rect.Bottom -= 1;
        }

        /// <summary>
        /// Applies DWM extended frame for glass/transparency effect behind the window.
        /// </summary>
        public static bool ApplyDwmFrame(IntPtr hwnd, bool extendIntoClientArea = true)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                var margins = new MARGINS
                {
                    left = extendIntoClientArea ? -1 : 0,
                    top = extendIntoClientArea ? -1 : 0,
                    right = extendIntoClientArea ? -1 : 0,
                    bottom = extendIntoClientArea ? -1 : 0,
                };
                return DwmExtendFrameIntoClientArea(hwnd, ref margins) == 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Disables DWM non-client rendering (removes default title bar).
        /// </summary>
        public static bool DisableDwmNcRendering(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                int val = DWMNCRP_DISABLED;
                return DwmSetWindowAttribute(hwnd, DWMWA_NCRENDERING_POLICY, ref val, sizeof(int)) == 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Applies all custom chrome settings at once.
        /// Call in Form_Load to set up custom window chrome.
        /// </summary>
        public static bool EnableCustomChrome(IntPtr hwnd, int borderWidth = 8)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                DisableDwmNcRendering(hwnd);
                ApplyDwmFrame(hwnd, true);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Converts screen coordinates to client coordinates.
        /// </summary>
        public static Point ScreenToClient(IntPtr hwnd, int screenX, int screenY)
        {
            if (hwnd == IntPtr.Zero) return new Point(screenX, screenY);

            var pt = new POINT { X = screenX, Y = screenY };
            User32.ScreenToClient(hwnd, ref pt);
            return new Point(pt.X, pt.Y);
        }

        private static int GetCaptionHeight(IntPtr hwnd)
        {
            try
            {
                // Default caption height is ~30px
                return 30;
            }
            catch { return 30; }
        }
    }
}
