// <copyright file="PrivilegeHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// Privilege attributes flags — maps to Win32 SE_PRIVILEGE_* constants.
    /// </summary>
    [Flags]
    public enum PrivilegeAttributes : uint
    {
        /// <summary>Privilege is disabled (0).</summary>
        Disabled = 0,

        /// <summary>Privilege is enabled by default (1).</summary>
        EnabledByDefault = 1,

        /// <summary>Privilege is enabled (2).</summary>
        Enabled = 2,

        /// <summary>Privilege is removed (4).</summary>
        Removed = 4,

        /// <summary>Used for access check (0x80000000).</summary>
        UsedForAccess = 0x80000000,
    }

    /// <summary>
    /// Represents a single privilege entry with its name, display name, and current state.
    /// </summary>
    public sealed class PrivilegeEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivilegeEntry"/> class.
        /// </summary>
        internal PrivilegeEntry(string name, string displayName, PrivilegeAttributes attributes)
        {
            Name = name ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Attributes = attributes;
        }

        /// <summary>Gets the privilege name (e.g., "SeDebugPrivilege").</summary>
        public string Name { get; }

        /// <summary>Gets the human-readable display name (e.g., "Debug programs").</summary>
        public string DisplayName { get; }

        /// <summary>Gets the current privilege attributes.</summary>
        public PrivilegeAttributes Attributes { get; }

        /// <summary>Gets whether this privilege is currently enabled.</summary>
        public bool Enabled => (Attributes & PrivilegeAttributes.Enabled) == PrivilegeAttributes.Enabled;

        /// <summary>Gets whether this privilege is enabled by default.</summary>
        public bool EnabledByDefault => (Attributes & PrivilegeAttributes.EnabledByDefault) == PrivilegeAttributes.EnabledByDefault;

        /// <summary>Gets whether this privilege has been removed.</summary>
        public bool Removed => (Attributes & PrivilegeAttributes.Removed) == PrivilegeAttributes.Removed;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} [{(Enabled ? "Enabled" : Removed ? "Removed" : "Disabled")}] \"{DisplayName}\"";
        }
    }

    /// <summary>
    /// LUID_AND_ATTRIBUTES structure — associates a privilege LUID with attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID_AND_ATTRIBUTES
    {
        /// <summary>LUID of the privilege.</summary>
        public LUID Luid;

        /// <summary>Attributes of the privilege.</summary>
        public PrivilegeAttributes Attributes;
    }

    /// <summary>
    /// TOKEN_PRIVILEGES structure — contains a variable-length array of privileges.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        /// <summary>Number of privileges in the array.</summary>
        public uint PrivilegeCount;

        /// <summary>First privilege entry (variable-length array follows).</summary>
        public LUID_AND_ATTRIBUTES Privileges;
    }

    /// <summary>
    /// Provides methods to enumerate, enable, disable, and query process privileges
    /// using pure P/Invoke (no WMI).
    /// All methods are thread-safe and gracefully return empty/default on non-Windows.
    /// </summary>
    public static class PrivilegeHelper
    {
        // =====================================================================
        // P/Invoke declarations
        // =====================================================================

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            int processId);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess,
            out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            TOKEN_INFORMATION_CLASS tokenInformationClass,
            IntPtr tokenInformation,
            int tokenInformationLength,
            out int returnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            IntPtr newState,
            int bufferLength,
            IntPtr previousState,
            out int returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValueW(
            string? lpSystemName,
            string lpName,
            out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeNameW(
            string? lpSystemName,
            ref LUID lpLuid,
            StringBuilder? lpName,
            ref uint cchName);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeDisplayNameW(
            string? lpSystemName,
            string lpName,
            StringBuilder? lpDisplayName,
            ref uint cchDisplayName,
            out int lpLanguageId);

        // =====================================================================
        // Constants
        // =====================================================================

        private const uint TokenQuery = 0x0008;
        private const uint TokenAdjustPrivileges = 0x0020;
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Enumerates all privileges of the current process.
        /// </summary>
        /// <returns>A read-only list of privilege entries (empty on failure or non-Windows).</returns>
        public static IReadOnlyList<PrivilegeEntry> GetCurrentProcessPrivileges()
        {
            try
            {
                IntPtr currentProcess = GetCurrentProcess();
                if (currentProcess == IntPtr.Zero)
                    return Array.Empty<PrivilegeEntry>();

                if (!OpenProcessToken(currentProcess, TokenQuery, out IntPtr tokenHandle))
                    return Array.Empty<PrivilegeEntry>();

                try
                {
                    return GetPrivilegesFromToken(tokenHandle);
                }
                finally
                {
                    CloseHandle(tokenHandle);
                }
            }
            catch (EntryPointNotFoundException)
            {
                return Array.Empty<PrivilegeEntry>();
            }
            catch (DllNotFoundException)
            {
                return Array.Empty<PrivilegeEntry>();
            }
            catch (PlatformNotSupportedException)
            {
                return Array.Empty<PrivilegeEntry>();
            }
        }

        /// <summary>
        /// Enumerates all privileges of a specific process.
        /// </summary>
        /// <param name="processId">The process ID to query.</param>
        /// <returns>A read-only list of privilege entries (empty on failure or non-Windows).</returns>
        public static IReadOnlyList<PrivilegeEntry> GetProcessPrivileges(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return Array.Empty<PrivilegeEntry>();

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return Array.Empty<PrivilegeEntry>();

                    try
                    {
                        return GetPrivilegesFromToken(tokenHandle);
                    }
                    finally
                    {
                        CloseHandle(tokenHandle);
                    }
                }
                finally
                {
                    CloseHandle(processHandle);
                }
            }
            catch (EntryPointNotFoundException)
            {
                return Array.Empty<PrivilegeEntry>();
            }
            catch (DllNotFoundException)
            {
                return Array.Empty<PrivilegeEntry>();
            }
            catch (PlatformNotSupportedException)
            {
                return Array.Empty<PrivilegeEntry>();
            }
        }

        /// <summary>
        /// Enables a specific privilege for the current process.
        /// </summary>
        /// <param name="privilegeName">The privilege name (e.g., "SeDebugPrivilege").</param>
        /// <returns>True if the privilege was enabled successfully; false otherwise.</returns>
        public static bool EnablePrivilege(string privilegeName)
        {
            return SetPrivilegeState(privilegeName, PrivilegeAttributes.Enabled);
        }

        /// <summary>
        /// Disables a specific privilege for the current process.
        /// </summary>
        /// <param name="privilegeName">The privilege name (e.g., "SeDebugPrivilege").</param>
        /// <returns>True if the privilege was disabled successfully; false otherwise.</returns>
        public static bool DisablePrivilege(string privilegeName)
        {
            return SetPrivilegeState(privilegeName, PrivilegeAttributes.Disabled);
        }

        /// <summary>
        /// Removes a specific privilege from the current process entirely.
        /// </summary>
        /// <param name="privilegeName">The privilege name (e.g., "SeDebugPrivilege").</param>
        /// <returns>True if the privilege was removed successfully; false otherwise.</returns>
        public static bool RemovePrivilege(string privilegeName)
        {
            return SetPrivilegeState(privilegeName, PrivilegeAttributes.Removed);
        }

        /// <summary>
        /// Checks if the current process has a specific privilege enabled.
        /// </summary>
        /// <param name="privilegeName">The privilege name (e.g., "SeDebugPrivilege").</param>
        /// <returns>True if the privilege is present and enabled.</returns>
        public static bool HasPrivilege(string privilegeName)
        {
            try
            {
                var privileges = GetCurrentProcessPrivileges();
                foreach (var priv in privileges)
                {
                    if (string.Equals(priv.Name, privilegeName, StringComparison.OrdinalIgnoreCase))
                        return priv.Enabled;
                }

                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a list of all well-known privilege names.
        /// </summary>
        /// <returns>A read-only list of well-known privilege name strings.</returns>
        public static IReadOnlyList<string> GetAllWellKnownPrivileges()
        {
            return s_wellKnownPrivileges;
        }

        // =====================================================================
        // Internal implementation
        // =====================================================================

        /// <summary>
        /// Opens a process handle with query access.
        /// </summary>
        private static IntPtr OpenProcessInternal(int processId)
        {
            IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (handle != IntPtr.Zero)
                return handle;

            return OpenProcess(ProcessQueryInformation, false, processId);
        }

        /// <summary>
        /// Enumerates privileges from an open token handle.
        /// </summary>
        private static List<PrivilegeEntry> GetPrivilegesFromToken(IntPtr tokenHandle)
        {
            var result = new List<PrivilegeEntry>();

            // First call: get required buffer size
            int requiredSize = 0;
            GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, 0, out requiredSize);
            int error = Marshal.GetLastWin32Error();
            if (error != 122 && error != 0)
                return result;

            if (requiredSize <= 0)
                return result;

            IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, requiredSize, out int returnLength))
                    return result;

                uint privilegeCount = (uint)Marshal.ReadInt32(buffer);
                if (privilegeCount == 0)
                    return result;

                int luidAndAttrSize = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
                IntPtr privilegesPtr = IntPtr.Add(buffer, Marshal.SizeOf<uint>());

                for (int i = 0; i < privilegeCount; i++)
                {
                    IntPtr currentPtr = IntPtr.Add(privilegesPtr, i * luidAndAttrSize);
                    var luidAndAttr = Marshal.PtrToStructure<LUID_AND_ATTRIBUTES>(currentPtr);

                    string name = LuidToPrivilegeName(luidAndAttr.Luid);
                    string displayName = GetDisplayNameFromPrivilegeName(name);

                    result.Add(new PrivilegeEntry(name, displayName, luidAndAttr.Attributes));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return result;
        }

        /// <summary>
        /// Converts a LUID to a privilege name.
        /// </summary>
        private static string LuidToPrivilegeName(LUID luid)
        {
            try
            {
                uint nameLen = 0;
                LookupPrivilegeNameW(null, ref luid, null, ref nameLen);
                if (nameLen == 0)
                    return $"Unknown-LUID-{luid.LowPart:x8}-{luid.HighPart:x8}";

                var sb = new StringBuilder((int)nameLen);
                if (LookupPrivilegeNameW(null, ref luid, sb, ref nameLen))
                    return sb.ToString();

                return $"Unknown-LUID-{luid.LowPart:x8}-{luid.HighPart:x8}";
            }
            catch (EntryPointNotFoundException)
            {
                return $"Unknown-LUID-{luid.LowPart:x8}-{luid.HighPart:x8}";
            }
            catch (DllNotFoundException)
            {
                return $"Unknown-LUID-{luid.LowPart:x8}-{luid.HighPart:x8}";
            }
        }

        /// <summary>
        /// Gets the display name for a privilege name.
        /// </summary>
        private static string GetDisplayNameFromPrivilegeName(string privilegeName)
        {
            if (string.IsNullOrEmpty(privilegeName))
                return string.Empty;

            try
            {
                uint displayLen = 0;
                int langId = 0;
                LookupPrivilegeDisplayNameW(null, privilegeName, null, ref displayLen, out langId);
                if (displayLen == 0)
                    return privilegeName;

                var sb = new StringBuilder((int)displayLen);
                if (LookupPrivilegeDisplayNameW(null, privilegeName, sb, ref displayLen, out langId))
                    return sb.ToString();

                return privilegeName;
            }
            catch (EntryPointNotFoundException)
            {
                return privilegeName;
            }
            catch (DllNotFoundException)
            {
                return privilegeName;
            }
        }

        /// <summary>
        /// Sets the state of a privilege (enable, disable, or remove).
        /// </summary>
        private static bool SetPrivilegeState(string privilegeName, PrivilegeAttributes newState)
        {
            if (string.IsNullOrEmpty(privilegeName))
                return false;

            try
            {
                IntPtr currentProcess = GetCurrentProcess();
                if (currentProcess == IntPtr.Zero)
                    return false;

                // Need both TOKEN_QUERY and TOKEN_ADJUST_PRIVILEGES
                if (!OpenProcessToken(currentProcess, TokenQuery | TokenAdjustPrivileges, out IntPtr tokenHandle))
                    return false;

                try
                {
                    // Convert privilege name to LUID
                    if (!LookupPrivilegeValueW(null, privilegeName, out LUID luid))
                        return false;

                    // Build TOKEN_PRIVILEGES structure
                    int luidAndAttrSize = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
                    int tpSize = Marshal.SizeOf<uint>() + luidAndAttrSize;
                    IntPtr buffer = Marshal.AllocHGlobal(tpSize);
                    try
                    {
                        Marshal.WriteInt32(buffer, 1); // PrivilegeCount = 1

                        IntPtr privPtr = IntPtr.Add(buffer, Marshal.SizeOf<uint>());
                        var luidAndAttr = new LUID_AND_ATTRIBUTES
                        {
                            Luid = luid,
                            Attributes = newState,
                        };
                        Marshal.StructureToPtr(luidAndAttr, privPtr, false);

                        bool success = AdjustTokenPrivileges(
                            tokenHandle,
                            false,
                            buffer,
                            tpSize,
                            IntPtr.Zero,
                            out _);

                        // ERROR_NOT_ALL_ASSIGNED (1300) can occur, meaning some privileges
                        // weren't adjusted. AdjustTokenPrivileges returns TRUE even if
                        // privileges weren't all adjusted, so check last error.
                        if (success && Marshal.GetLastWin32Error() == 0)
                            return true;

                        return false;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                finally
                {
                    CloseHandle(tokenHandle);
                }
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
        }

        // =====================================================================
        // Well-known privileges
        // =====================================================================

        private static readonly string[] s_wellKnownPrivileges =
        {
            "SeCreateTokenPrivilege",
            "SeAssignPrimaryTokenPrivilege",
            "SeLockMemoryPrivilege",
            "SeIncreaseQuotaPrivilege",
            "SeUnsolicitedInputPrivilege",
            "SeMachineAccountPrivilege",
            "SeTcbPrivilege",
            "SeSecurityPrivilege",
            "SeTakeOwnershipPrivilege",
            "SeLoadDriverPrivilege",
            "SeSystemProfilePrivilege",
            "SeSystemtimePrivilege",
            "SeProfileSingleProcessPrivilege",
            "SeIncreaseBasePriorityPrivilege",
            "SeCreatePagefilePrivilege",
            "SeCreatePermanentPrivilege",
            "SeBackupPrivilege",
            "SeRestorePrivilege",
            "SeShutdownPrivilege",
            "SeDebugPrivilege",
            "SeAuditPrivilege",
            "SeSystemEnvironmentPrivilege",
            "SeChangeNotifyPrivilege",
            "SeRemoteShutdownPrivilege",
            "SeUndockPrivilege",
            "SeSyncAgentPrivilege",
            "SeEnableDelegationPrivilege",
            "SeManageVolumePrivilege",
            "SeImpersonatePrivilege",
            "SeCreateGlobalPrivilege",
            "SeTrustedCredManAccessPrivilege",
            "SeRelabelPrivilege",
            "SeIncreaseWorkingSetPrivilege",
            "SeTimeZonePrivilege",
            "SeCreateSymbolicLinkPrivilege",
            "SeDelegateSessionUserImpersonatePrivilege",
        };
    }
}
