// <copyright file="FileAssociationHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Represents a file type association to register with Windows.
    /// </summary>
    public sealed class FileAssociation
    {
        /// <summary>The file extension (e.g. ".txt").</summary>
        public string Extension { get; init; } = ".txt";

        /// <summary>The ProgId to register under HKCU\Software\Classes.</summary>
        public string ProgId { get; init; } = string.Empty;

        /// <summary>Human-readable description for the file type.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>Full path to the executable that opens this file type.</summary>
        public string ExecutablePath { get; init; } = string.Empty;

        /// <summary>Optional path to the icon file. Defaults to ExecutablePath.</summary>
        public string IconPath { get; init; } = string.Empty;

        /// <summary>Optional icon index within the icon file.</summary>
        public int IconIndex { get; init; }
    }

    /// <summary>
    /// Provides file extension association registration via HKCU.
    /// Per-user registration (no admin needed).
    /// </summary>
    public static class FileAssociationHelper
    {
        /// <summary>
        /// Registers a file association for the current user.
        /// Creates the ProgId under HKCU\Software\Classes and maps the extension.
        /// </summary>
        /// <param name="association">The file association to register.</param>
        /// <returns>True if registered successfully.</returns>
        public static bool Register(FileAssociation association)
        {
            if (association is null) return false;
            if (string.IsNullOrEmpty(association.Extension) || string.IsNullOrEmpty(association.ProgId))
                return false;
            if (string.IsNullOrEmpty(association.ExecutablePath)) return false;

            try
            {
                // 1. Create ProgId
                string progIdPath = $"Software\\Classes\\{association.ProgId}";
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(progIdPath))
                {
                    if (key is null) return false;
                    key.SetValue("", association.Description);
                }

                // 2. Set default icon
                string iconPath = string.IsNullOrEmpty(association.IconPath)
                    ? association.ExecutablePath
                    : association.IconPath;
                string iconValue = association.IconIndex > 0
                    ? $"{iconPath},{association.IconIndex}"
                    : iconPath;

                using (var iconKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"{progIdPath}\\DefaultIcon"))
                {
                    iconKey?.SetValue("", iconValue);
                }

                // 3. Set shell\open\command
                string command = $"\"{association.ExecutablePath}\" \"%1\"";
                using (var cmdKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"{progIdPath}\\shell\\open\\command"))
                {
                    cmdKey?.SetValue("", command);
                }

                // 4. Map extension to ProgId
                string extPath = $"Software\\Classes\\{association.Extension}";
                using (var extKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(extPath))
                {
                    extKey?.SetValue("", association.ProgId);
                }

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Unregisters a file extension association for the current user.
        /// Removes the extension-to-ProgId mapping.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <param name="progId">The ProgId to remove.</param>
        /// <returns>True if unregistered successfully.</returns>
        public static bool Unregister(string extension, string progId)
        {
            if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(progId)) return false;

            try
            {
                // Remove extension mapping
                string extPath = $"Software\\Classes\\{extension}";
                using (var extKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(extPath, true))
                {
                    extKey?.DeleteValue("", false);
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Checks if a file extension is registered (has a mapping in HKCU).
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>True if the extension has a registered mapping.</returns>
        public static bool IsRegistered(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{extension}");
                return key?.GetValue("") is not null;
            }
            catch { return false; }
        }

        /// <summary>
        /// Gets the file association details for an extension.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".txt").</param>
        /// <returns>The file association, or null if not registered.</returns>
        public static FileAssociation? GetAssociation(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return null;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{extension}");
                string? progId = key?.GetValue("") as string;
                if (string.IsNullOrEmpty(progId)) return null;

                using var progKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{progId}");
                string? description = progKey?.GetValue("") as string;

                using var cmdKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{progId}\\shell\\open\\command");
                string? command = cmdKey?.GetValue("") as string;

                return new FileAssociation
                {
                    Extension = extension,
                    ProgId = progId,
                    Description = description ?? "",
                    ExecutablePath = command ?? "",
                };
            }
            catch { return null; }
        }
    }
}
