// <copyright file="EnvironmentHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides environment variable management, PATH manipulation, domain info,
    /// and special folder lookups. All methods are thread-safe.
    /// </summary>
    public static class EnvironmentHelper
    {
        /// <summary>Gets an environment variable (any target).</summary>
        public static string? GetVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (name is null) return null;
            try { return Environment.GetEnvironmentVariable(name, target); }
            catch { return null; }
        }

        /// <summary>Sets an environment variable.</summary>
        public static bool SetVariable(string name, string? value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            try { Environment.SetEnvironmentVariable(name, value, target); return true; }
            catch { return false; }
        }

        /// <summary>Deletes an environment variable.</summary>
        public static bool DeleteVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
            => SetVariable(name, null, target);

        /// <summary>Expands environment-variable strings (e.g., %TEMP%).</summary>
        public static string? ExpandString(string? input)
        {
            if (input is null) return null;
            try { return Environment.ExpandEnvironmentVariables(input); }
            catch { return input; }
        }

        /// <summary>Gets the machine's NetBIOS name.</summary>
        public static string GetMachineName() => Environment.MachineName;

        /// <summary>Returns true if the computer is domain-joined (via NetGetJoinInformation).</summary>
        public static bool IsDomainJoined()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
            try
            {
                int result = NetGetJoinInformation(null, out IntPtr buf, out int status);
                try
                {
                    if (result == 0 && buf != IntPtr.Zero)
                        return status == 3; // NetSetupDomainName
                    return false;
                }
                finally
                {
                    if (buf != IntPtr.Zero) NetApiBufferFree(buf);
                }
            }
            catch { return false; }
        }

        /// <summary>Gets the domain name if the computer is domain-joined.</summary>
        public static string? GetDomainName()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
            try
            {
                int result = NetGetJoinInformation(null, out IntPtr buf, out int status);
                try
                {
                    if (result == 0 && buf != IntPtr.Zero && status == 3)
                        return Marshal.PtrToStringUni(buf);
                    return null;
                }
                finally
                {
                    if (buf != IntPtr.Zero) NetApiBufferFree(buf);
                }
            }
            catch { return null; }
        }

        /// <summary>Adds a directory to the user PATH if not already present.</summary>
        public static bool AddToUserPath(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) return false;
            try
            {
                string current = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                var dirs = new List<string>(current.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries));
                string normalized = Path.GetFullPath(directoryPath);
                if (dirs.Exists(d => d.Equals(normalized, StringComparison.OrdinalIgnoreCase))) return true;
                dirs.Add(normalized);
                Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), dirs), EnvironmentVariableTarget.User);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Removes a directory from the user PATH.</summary>
        public static bool RemoveFromUserPath(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) return false;
            try
            {
                string current = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                var dirs = new List<string>(current.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries));
                string normalized = Path.GetFullPath(directoryPath);
                int removed = dirs.RemoveAll(d => d.Equals(normalized, StringComparison.OrdinalIgnoreCase));
                if (removed == 0) return true;
                Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), dirs), EnvironmentVariableTarget.User);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Gets the user PATH as a list of directories.</summary>
        public static List<string> GetUserPathDirectories() => GetPathDirectories(EnvironmentVariableTarget.User);
        /// <summary>Gets the system PATH as a list of directories.</summary>
        public static List<string> GetSystemPathDirectories() => GetPathDirectories(EnvironmentVariableTarget.Machine);

        private static List<string> GetPathDirectories(EnvironmentVariableTarget target)
        {
            try
            {
                string? path = Environment.GetEnvironmentVariable("PATH", target);
                if (string.IsNullOrEmpty(path)) return new List<string>();
                return new List<string>(path.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries));
            }
            catch { return new List<string>(); }
        }

        // --- P/Invoke for NetGetJoinInformation ---
        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NetGetJoinInformation(
            string? lpServer,
            out IntPtr lpNameBuffer,
            out int lpBufferType);

        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern int NetApiBufferFree(IntPtr buffer);
    }
}
