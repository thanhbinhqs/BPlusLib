// <copyright file="ShortcutHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Shell
{
    /// <summary>
    /// Represents the properties of a Windows Shell shortcut (.lnk).
    /// </summary>
    public sealed class ShortcutInfo
    {
        /// <summary>Target executable or document path.</summary>
        public string TargetPath { get; init; } = string.Empty;

        /// <summary>Command-line arguments passed to the target.</summary>
        public string? Arguments { get; init; }

        /// <summary>Working directory for the target process.</summary>
        public string? WorkingDirectory { get; init; }

        /// <summary>Description/comment for the shortcut.</summary>
        public string? Description { get; init; }

        /// <summary>Icon file location (may include embedded icon index).</summary>
        public string? IconLocation { get; init; }

        /// <summary>Index of the icon within the icon file.</summary>
        public int IconIndex { get; init; }

        /// <summary>Show command: 1=normal, 3=maximized, 7=minimized.</summary>
        public int ShowCommand { get; init; }

        /// <summary>Hotkey for the shortcut.</summary>
        public string? Hotkey { get; init; }
    }

    /// <summary>
    /// Provides methods to read, create, and query Windows Shell shortcuts (.lnk files)
    /// via the IShellLink COM interface. All methods are thread-safe and gracefully
    /// return null/false on non-Windows platforms.
    /// </summary>
    public static class ShortcutHelper
    {
        private const uint SLGP_RAWPATH = 0x00000004;
        private const uint STGM_READ = 0x00000000;

        /// <summary>
        /// Reads the properties of a .lnk shortcut file.
        /// </summary>
        /// <param name="shortcutPath">Full path to the .lnk file.</param>
        /// <returns>A <see cref="ShortcutInfo"/> with the shortcut's properties, or null on failure.</returns>
        public static ShortcutInfo? Read(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath) || !File.Exists(shortcutPath))
                return null;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            try
            {
                // Create ShellLink instance
                Type shellLinkType = Type.GetTypeFromCLSID(Shell32.CLSID_ShellLink)!;
                object? shellLink = Activator.CreateInstance(shellLinkType);
                if (shellLink is null) return null;

                try
                {
                    // Get IPersistFile
                    var persistFile = (Shell32.IPersistFile)shellLink;
                    persistFile.Load(shortcutPath, STGM_READ);

                    // Get IShellLinkW
                    var shellLinkW = (Shell32.IShellLinkW)shellLink;

                    var pathSb = new StringBuilder(260);
                    shellLinkW.GetPath(pathSb, pathSb.Capacity, out _, SLGP_RAWPATH);

                    var descSb = new StringBuilder(1024);
                    shellLinkW.GetDescription(descSb, descSb.Capacity);

                    var dirSb = new StringBuilder(260);
                    shellLinkW.GetWorkingDirectory(dirSb, dirSb.Capacity);

                    var argsSb = new StringBuilder(260);
                    shellLinkW.GetArguments(argsSb, argsSb.Capacity);

                    var iconSb = new StringBuilder(260);
                    shellLinkW.GetIconLocation(iconSb, iconSb.Capacity, out int iconIndex);

                    shellLinkW.GetShowCmd(out int showCmd);
                    shellLinkW.GetHotKey(out short hotkey);

                    return new ShortcutInfo
                    {
                        TargetPath = pathSb.ToString(),
                        Description = descSb.Length > 0 ? descSb.ToString() : null,
                        WorkingDirectory = dirSb.Length > 0 ? dirSb.ToString() : null,
                        Arguments = argsSb.Length > 0 ? argsSb.ToString() : null,
                        IconLocation = iconSb.Length > 0 ? iconSb.ToString() : null,
                        IconIndex = iconIndex,
                        ShowCommand = showCmd,
                        Hotkey = hotkey != 0 ? $"0x{hotkey:X4}" : null,
                    };
                }
                finally
                {
                    if (shellLink is IDisposable disposable)
                        disposable.Dispose();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates or updates a .lnk shortcut file.
        /// </summary>
        /// <param name="shortcutPath">Full path where the .lnk file will be created.</param>
        /// <param name="info">The shortcut properties.</param>
        /// <returns>True if the shortcut was created successfully.</returns>
        public static bool Create(string shortcutPath, ShortcutInfo info)
        {
            if (string.IsNullOrEmpty(shortcutPath) || info is null)
                return false;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                Type shellLinkType = Type.GetTypeFromCLSID(Shell32.CLSID_ShellLink)!;
                object? shellLink = Activator.CreateInstance(shellLinkType);
                if (shellLink is null) return false;

                try
                {
                    var sl = (Shell32.IShellLinkW)shellLink;
                    sl.SetPath(info.TargetPath);
                    if (info.Arguments is not null)
                        sl.SetArguments(info.Arguments);
                    if (info.WorkingDirectory is not null)
                        sl.SetWorkingDirectory(info.WorkingDirectory);
                    if (info.Description is not null)
                        sl.SetDescription(info.Description);
                    if (info.IconLocation is not null)
                        sl.SetIconLocation(info.IconLocation, info.IconIndex);
                    sl.SetShowCmd(info.ShowCommand);

                    var pf = (Shell32.IPersistFile)shellLink;
                    pf.Save(shortcutPath, true);
                    return true;
                }
                finally
                {
                    if (shellLink is IDisposable disposable)
                        disposable.Dispose();
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the specified file is a .lnk shortcut (extension check).
        /// </summary>
        public static bool IsShortcut(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Quick method to get just the target path of a .lnk file.
        /// </summary>
        public static string? GetTargetPath(string shortcutPath)
        {
            var info = Read(shortcutPath);
            return info?.TargetPath;
        }
    }
}
