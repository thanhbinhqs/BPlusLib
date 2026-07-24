// <copyright file="WindowManager.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Saved state of a window's position, size, and state.
    /// </summary>
    public sealed class WindowSettings
    {
        /// <summary>X position (left edge).</summary>
        public int X { get; init; }
        /// <summary>Y position (top edge).</summary>
        public int Y { get; init; }
        /// <summary>Width in pixels.</summary>
        public int Width { get; init; }
        /// <summary>Height in pixels.</summary>
        public int Height { get; init; }
        /// <summary>Whether the window was maximized.</summary>
        public bool IsMaximized { get; init; }
        /// <summary>Whether the window was minimized.</summary>
        public bool IsMinimized { get; init; }
    }

    /// <summary>
    /// Provides window state persistence — save/restore position, size, and
    /// maximized/minimized state across sessions using the Windows registry.
    /// All methods are thread-safe and gracefully return false on error.
    /// </summary>
    public static class WindowManager
    {
        private const string BaseKeyPath = @"Software\BPlusLib\WindowManager";
        private static readonly object _lock = new();
        private static readonly Dictionary<string, WindowSettings> _cache = new();

        /// <summary>
        /// Saves explicit window settings to the registry.
        /// </summary>
        /// <param name="hwnd">Window handle (must not be IntPtr.Zero).</param>
        /// <param name="settings">The settings to save.</param>
        /// <param name="key">Unique key (defaults to "hwnd_{hwnd}").</param>
        /// <returns>True on success.</returns>
        public static bool Save(IntPtr hwnd, WindowSettings settings, string? key = null)
        {
            if (hwnd == IntPtr.Zero || settings is null) return false;

            key = key ?? $"hwnd_{hwnd}";
            return SaveToRegistry(key, settings);
        }

        /// <summary>
        /// Loads saved settings from the registry without applying them.
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <param name="key">Unique key (defaults to "hwnd_{hwnd}").</param>
        /// <returns>The saved settings, or null if not found.</returns>
        public static WindowSettings? Restore(IntPtr hwnd, string? key = null)
        {
            key = key ?? $"hwnd_{hwnd}";
            return LoadFromRegistry(key);
        }

        /// <summary>
        /// Deletes saved settings for a key.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <returns>True on success or key not found; false on error.</returns>
        public static bool Delete(string? key = null)
        {
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                string subKeyPath = $"{BaseKeyPath}\\{key}";
                using var subKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKeyPath);
                if (subKey is not null)
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(subKeyPath, false);
                    lock (_lock) _cache.Remove(key);
                }
                return true;
            }
            catch { return false; }
        }

#if FEATURE_WINDOW_MODULE
        /// <summary>
        /// Saves the current window state to the registry.
        /// </summary>
        /// <param name="form">The WinForms Form to save.</param>
        /// <param name="key">Unique key (defaults to form type name).</param>
        /// <returns>True on success.</returns>
        public static bool Save(System.Windows.Forms.Form form, string? key = null)
        {
            if (form is null) return false;

            key = GetKey(key, form);
            var settings = new WindowSettings
            {
                X = form.Left,
                Y = form.Top,
                Width = form.Width,
                Height = form.Height,
                IsMaximized = form.WindowState == System.Windows.Forms.FormWindowState.Maximized,
                IsMinimized = form.WindowState == System.Windows.Forms.FormWindowState.Minimized,
            };

            return SaveToRegistry(key, settings);
        }

        /// <summary>
        /// Restores a window from saved settings.
        /// </summary>
        /// <param name="form">The WinForms Form to restore.</param>
        /// <param name="key">Unique key (must match the key used to save).</param>
        /// <returns>The saved settings, or null if not found.</returns>
        public static WindowSettings? Restore(System.Windows.Forms.Form form, string? key = null)
        {
            if (form is null) return null;

            key = GetKey(key, form);
            var settings = LoadFromRegistry(key);
            if (settings is null) return null;

            // Apply settings
            form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            form.Left = settings.X;
            form.Top = settings.Y;
            form.Width = settings.Width;
            form.Height = settings.Height;

            if (settings.IsMaximized)
                form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            else if (settings.IsMinimized)
                form.WindowState = System.Windows.Forms.FormWindowState.Minimized;

            return settings;
        }

        /// <summary>
        /// Saves multiple forms at once.
        /// </summary>
        /// <param name="forms">The forms to save.</param>
        /// <returns>True if all saves succeeded.</returns>
        public static bool SaveAll(params System.Windows.Forms.Form[] forms)
        {
            bool allSuccess = true;
            foreach (var form in forms)
            {
                if (!Save(form)) allSuccess = false;
            }
            return allSuccess;
        }

        /// <summary>
        /// Restores multiple forms at once.
        /// </summary>
        /// <param name="forms">The forms to restore.</param>
        /// <returns>True if at least one form was restored.</returns>
        public static bool RestoreAll(params System.Windows.Forms.Form[] forms)
        {
            bool anyRestored = false;
            foreach (var form in forms)
            {
                if (Restore(form) is not null) anyRestored = true;
            }
            return anyRestored;
        }

        // --- Internal helpers ---

        private static string GetKey(string? key, System.Windows.Forms.Form form)
        {
            return string.IsNullOrEmpty(key)
                ? form.GetType().FullName ?? form.GetType().Name
                : key;
        }
#endif

        // --- Internal registry methods ---

        private static bool SaveToRegistry(string key, WindowSettings settings)
        {
            try
            {
                string subKeyPath = $"{BaseKeyPath}\\{key}";
                using var subKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subKeyPath);
                if (subKey is null) return false;

                subKey.SetValue("X", settings.X);
                subKey.SetValue("Y", settings.Y);
                subKey.SetValue("Width", settings.Width);
                subKey.SetValue("Height", settings.Height);
                subKey.SetValue("IsMaximized", settings.IsMaximized ? 1 : 0);
                subKey.SetValue("IsMinimized", settings.IsMinimized ? 1 : 0);

                lock (_lock) _cache[key] = settings;
                return true;
            }
            catch { return false; }
        }

        private static WindowSettings? LoadFromRegistry(string key)
        {
            try
            {
                lock (_lock)
                {
                    if (_cache.TryGetValue(key, out var cached))
                        return cached;
                }

                string subKeyPath = $"{BaseKeyPath}\\{key}";
                using var subKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKeyPath);
                if (subKey is null) return null;

                var settings = new WindowSettings
                {
                    X = GetIntValue(subKey, "X", 100),
                    Y = GetIntValue(subKey, "Y", 100),
                    Width = GetIntValue(subKey, "Width", 800),
                    Height = GetIntValue(subKey, "Height", 600),
                    IsMaximized = GetIntValue(subKey, "IsMaximized", 0) == 1,
                    IsMinimized = GetIntValue(subKey, "IsMinimized", 0) == 1,
                };

                lock (_lock) _cache[key] = settings;
                return settings;
            }
            catch { return null; }
        }

        private static int GetIntValue(Microsoft.Win32.RegistryKey key, string valueName, int defaultValue)
        {
            try
            {
                object? val = key.GetValue(valueName);
                return val is int i ? i : defaultValue;
            }
            catch { return defaultValue; }
        }
    }
}
