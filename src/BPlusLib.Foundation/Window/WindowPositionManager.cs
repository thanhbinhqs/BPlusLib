// <copyright file="WindowPositionManager.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BPlusLib.Foundation.Common;
using Microsoft.Win32;

namespace BPlusLib.Foundation.Window
{
    /// <summary>
    /// Manages window position persistence using the Windows registry.
    /// Saves and restores form location, size, window state, and target monitor
    /// for one or more application windows.
    /// </summary>
    public sealed class WindowPositionManager
    {
        private const string RegistryBasePath = "Software";
        private const string SubKeyName = "WindowPositions";

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowPositionManager"/> class.
        /// </summary>
        /// <param name="applicationName">The application name used as the registry key namespace.</param>
        public WindowPositionManager(string applicationName)
        {
            Guard.ThrowIfNullOrWhiteSpace(applicationName);
            ApplicationName = applicationName;
        }

        /// <summary>
        /// Gets the application name used as the registry key namespace.
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// Saves the current position, size, state, and monitor of the specified form.
        /// </summary>
        /// <param name="form">The form whose state to save.</param>
        /// <param name="key">A unique key identifying this window (e.g., "MainWindow").</param>
        public void Save(Form form, string key)
        {
            Guard.ThrowIfNull(form);
            Guard.ThrowIfNullOrWhiteSpace(key);

            using var keyRoot = Registry.CurrentUser.CreateSubKey(GetWindowKey(key));
            if (keyRoot == null)
                return;

            var bounds = form.Bounds;
            keyRoot.SetValue("X", bounds.X, RegistryValueKind.DWord);
            keyRoot.SetValue("Y", bounds.Y, RegistryValueKind.DWord);
            keyRoot.SetValue("Width", bounds.Width, RegistryValueKind.DWord);
            keyRoot.SetValue("Height", bounds.Height, RegistryValueKind.DWord);
            keyRoot.SetValue("WindowState", (int)form.WindowState, RegistryValueKind.DWord);

            // Determine the current monitor
            string monitorName = string.Empty;
            if (form.IsHandleCreated)
            {
                var monitorInfo = MonitorHelper.GetMonitorFrom(form.Handle);
                monitorName = monitorInfo.Name;
            }

            keyRoot.SetValue("MonitorName", monitorName ?? string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Restores a previously saved position and state for the specified form.
        /// Validates that the target monitor still exists; falls back to the primary
        /// monitor if it does not.
        /// </summary>
        /// <param name="form">The form whose state to restore.</param>
        /// <param name="key">The unique key used when saving.</param>
        /// <returns><c>true</c> if restore data was found and applied; otherwise <c>false</c>.</returns>
        public bool Restore(Form form, string key)
        {
            Guard.ThrowIfNull(form);
            Guard.ThrowIfNullOrWhiteSpace(key);

            using var keyRoot = Registry.CurrentUser.OpenSubKey(GetWindowKey(key));
            if (keyRoot == null)
                return false;

            // Read saved values
            int x = (int)(keyRoot.GetValue("X", -1) ?? -1);
            int y = (int)(keyRoot.GetValue("Y", -1) ?? -1);
            int width = (int)(keyRoot.GetValue("Width", -1) ?? -1);
            int height = (int)(keyRoot.GetValue("Height", -1) ?? -1);
            int windowState = (int)(keyRoot.GetValue("WindowState", (int)FormWindowState.Normal) ?? (int)FormWindowState.Normal);
            string? monitorName = keyRoot.GetValue("MonitorName", string.Empty) as string;

            if (x < 0 || y < 0 || width <= 0 || height <= 0)
                return false;

            // Validate the saved monitor exists; fall back to primary if not
            var allMonitors = MonitorHelper.GetAllMonitors();
            var targetMonitor = allMonitors.FirstOrDefault(m =>
                string.Equals(m.Name, monitorName, StringComparison.OrdinalIgnoreCase));

            if (targetMonitor.Name == null && allMonitors.Count > 0)
            {
                targetMonitor = MonitorHelper.GetPrimaryMonitor();
            }

            // Clamp the window bounds to the target monitor's working area
            var bounds = new Rectangle(x, y, width, height);
            bounds = ClampToMonitor(bounds, targetMonitor);

            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = bounds;

            // Restore window state (but don't minimize on restore)
            if (windowState == (int)FormWindowState.Maximized)
            {
                form.WindowState = FormWindowState.Maximized;
            }
            else
            {
                form.WindowState = FormWindowState.Normal;
            }

            return true;
        }

        /// <summary>
        /// Removes a previously saved window position from the registry.
        /// </summary>
        /// <param name="key">The unique key identifying the window state to remove.</param>
        public void Reset(string key)
        {
            Guard.ThrowIfNullOrWhiteSpace(key);

            try
            {
                using var appKey = Registry.CurrentUser.OpenSubKey(GetAppKey(), writable: true);
                if (appKey != null)
                {
                    appKey.DeleteSubKeyTree(key, throwOnMissingSubKey: false);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Registry access denied — best-effort
            }
        }

        private string GetAppKey() => $@"{RegistryBasePath}\{ApplicationName}";

        private string GetWindowKey(string windowKey) => $@"{GetAppKey()}\{SubKeyName}\{windowKey}";

        private static Rectangle ClampToMonitor(Rectangle bounds, MonitorInfo monitor)
        {
            // Ensure the window is at least partially visible on the target monitor
            var wa = monitor.WorkingArea;

            // Clamp size to monitor working area
            int newWidth = Math.Min(bounds.Width, wa.Width);
            int newHeight = Math.Min(bounds.Height, wa.Height);

            // Clamp position so the title bar is visible
            int newX = bounds.X;
            int newY = bounds.Y;

            // If the window is completely off-screen, center it
            bool completelyOff = (newX + newWidth < wa.Left) ||
                                 (newX > wa.Right) ||
                                 (newY + newHeight < wa.Top) ||
                                 (newY > wa.Bottom);

            if (completelyOff)
            {
                newX = wa.Left + (wa.Width - newWidth) / 2;
                newY = wa.Top + (wa.Height - newHeight) / 2;
            }
            else
            {
                // Ensure at least a portion of the title bar is visible
                newX = Math.Max(newX, wa.Left - newWidth + 100);
                newX = Math.Min(newX, wa.Right - 100);
                newY = Math.Max(newY, wa.Top - newHeight + 50);
                newY = Math.Min(newY, wa.Bottom - 50);
            }

            return new Rectangle(newX, newY, newWidth, newHeight);
        }
    }
}

#endif
