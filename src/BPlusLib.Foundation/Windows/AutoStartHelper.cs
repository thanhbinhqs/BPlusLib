// <copyright file="AutoStartHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Provides methods to register/unregister an application in Windows startup.
    /// All methods use HKCU\Software\Microsoft\Windows\CurrentVersion\Run for per-user
    /// startup entries. No admin rights needed.
    /// </summary>
    public static class AutoStartHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Registers the application to start with Windows (per-user).
        /// </summary>
        /// <param name="appName">Unique application name (used as registry value name).</param>
        /// <param name="executablePath">Path to executable. If null, uses current process path.</param>
        /// <param name="args">Optional command-line arguments.</param>
        /// <returns>True if registered successfully.</returns>
        public static bool Enable(string appName, string? executablePath = null, string? args = null)
        {
            if (string.IsNullOrEmpty(appName)) return false;

            try
            {
                executablePath ??= System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(executablePath)) return false;

                string command = string.IsNullOrEmpty(args)
                    ? $"\"{executablePath}\""
                    : $"\"{executablePath}\" {args}";

                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key is null) return false;

                key.SetValue(appName, command, Microsoft.Win32.RegistryValueKind.String);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Removes the application from Windows startup.
        /// </summary>
        public static bool Disable(string appName)
        {
            return RemoveFromStartup(appName);
        }

        /// <summary>
        /// Checks if the application is registered in startup.
        /// </summary>
        public static bool IsEnabled(string appName)
        {
            if (string.IsNullOrEmpty(appName)) return false;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath);
                if (key is null) return false;
                return key.GetValue(appName) is not null;
            }
            catch { return false; }
        }

        /// <summary>
        /// Gets the command line registered for the application in startup.
        /// </summary>
        public static string? GetCommand(string appName)
        {
            if (string.IsNullOrEmpty(appName)) return null;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath);
                if (key is null) return null;
                return key.GetValue(appName) as string;
            }
            catch { return null; }
        }

        /// <summary>
        /// Registers with arguments (convenience method).
        /// </summary>
        public static bool EnableCurrentUser(string appName, string? args = null)
        {
            return Enable(appName, null, args);
        }

        /// <summary>
        /// Removes the application from startup.
        /// </summary>
        public static bool RemoveFromStartup(string appName)
        {
            if (string.IsNullOrEmpty(appName)) return false;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key is null) return false;

                key.DeleteValue(appName, false);
                return true;
            }
            catch { return false; }
        }
    }
}
