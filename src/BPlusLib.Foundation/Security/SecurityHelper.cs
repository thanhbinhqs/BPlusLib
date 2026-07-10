// <copyright file="SecurityHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// High-level security helper that orchestrates TokenHelper, PrivilegeHelper,
    /// and IntegrityHelper to answer common security-related questions about processes.
    /// All methods are thread-safe and gracefully return safe defaults on non-Windows.
    /// </summary>
    public static class SecurityHelper
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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            IntPtr processHandle,
            [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

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

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupAccountSidW(
            string? lpSystemName,
            IntPtr sid,
            System.Text.StringBuilder? lpName,
            ref uint cchName,
            System.Text.StringBuilder? lpReferencedDomainName,
            ref uint cchReferencedDomainName,
            out int peUse);

        // =====================================================================
        // Constants
        // =====================================================================

        private const uint TokenQuery = 0x0008;
        private const uint TokenQuerySource = 0x0010;
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const uint ProcessVmRead = 0x0010;
        private const uint ProcessTerminate = 0x0001;
        private const uint ProcessSuspendResume = 0x0800;
        private const uint ProcessSetInformation = 0x0200;

        // SE_GROUP attributes
        private const uint SE_GROUP_ENABLED = 0x00000004;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Checks if the current process is running with an elevated (administrator) token.
        /// </summary>
        /// <returns>True if the current process is elevated; false otherwise or on non-Windows.</returns>
        public static bool IsCurrentProcessElevated()
        {
            try
            {
                IntPtr currentProcess = TokenHelper.OpenProcessInternal(System.Diagnostics.Process.GetCurrentProcess().Id);
                if (currentProcess == IntPtr.Zero)
                    return false;

                try
                {
                    if (!OpenProcessToken(currentProcess, TokenQuery, out IntPtr tokenHandle))
                        return false;

                    try
                    {
                        return IsTokenElevated(tokenHandle);
                    }
                    finally
                    {
                        CloseHandle(tokenHandle);
                    }
                }
                finally
                {
                    CloseHandle(currentProcess);
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

        /// <summary>
        /// Checks if a specific process has an elevated token.
        /// </summary>
        /// <param name="processId">The process ID to check.</param>
        /// <returns>True if the process is elevated; false otherwise or on non-Windows.</returns>
        public static bool IsProcessElevated(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return false;

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return false;

                    try
                    {
                        return IsTokenElevated(tokenHandle);
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
        /// Checks whether a process is 64-bit (as opposed to 32-bit WOW64).
        /// </summary>
        /// <param name="processId">The process ID to check.</param>
        /// <returns>True if the process is 64-bit; false if 32-bit or if the check fails.</returns>
        public static bool IsProcess64Bit(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return false;

                try
                {
                    // If IsWow64Process succeeds and returns false, the process is native (64-bit)
                    if (IsWow64Process(processHandle, out bool isWow64))
                        return !isWow64;

                    // If IsWow64Process fails (e.g., on pre-Vista), assume 32-bit
                    return false;
                }
                finally
                {
                    CloseHandle(processHandle);
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

        /// <summary>
        /// Gets the owner (user) of a process as a DOMAIN\USER string.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>The owner string (e.g., "WORKGROUP\JohnDoe"), or null on failure.</returns>
        public static string? GetProcessOwner(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return null;

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return null;

                    try
                    {
                        // Get the user SID from the token, then resolve to account name
                        if (!TokenHelper.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, out byte[]? data) || data == null)
                            return null;

                        int sidAndAttrSize = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
                        if (data.Length < sidAndAttrSize)
                            return null;

                        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                        try
                        {
                            IntPtr ptr = handle.AddrOfPinnedObject();
                            var tokenUser = Marshal.PtrToStructure<TOKEN_USER>(ptr);
                            return TokenHelper.SidToAccountName(tokenUser.User.Sid);
                        }
                        finally
                        {
                            handle.Free();
                        }
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
                return null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a process is associated with an interactive user session
        /// (session ID &gt; 0), as opposed to running as a service.
        /// </summary>
        /// <param name="processId">The process ID to check.</param>
        /// <returns>True if the process runs in a user session (not a service).</returns>
        public static bool IsInteractiveUser(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return false;

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return false;

                    try
                    {
                        int? sessionId = TokenHelper.GetTokenSessionId(tokenHandle);
                        return sessionId.HasValue && sessionId.Value > 0;
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
        /// Gets the integrity level of a process as a human-readable string.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>"Low", "Medium", "High", "System", "Untrusted", or "Unknown".</returns>
        public static string? GetProcessIntegrityLevelString(int processId)
        {
            IntegrityLevel level = IntegrityHelper.GetProcessIntegrityLevel(processId);
            return level switch
            {
                IntegrityLevel.Untrusted => "Untrusted",
                IntegrityLevel.Low => "Low",
                IntegrityLevel.Medium => "Medium",
                IntegrityLevel.High => "High",
                IntegrityLevel.System => "System",
                IntegrityLevel.ProtectedProcess => "ProtectedProcess",
                _ => "Unknown",
            };
        }

        /// <summary>
        /// Checks whether a specific access mask can be granted to the current process
        /// for the target process. Attempts to open the process with the given access.
        /// </summary>
        /// <param name="processId">The target process ID.</param>
        /// <param name="access">The desired token access level.</param>
        /// <returns>True if the process can be opened with the specified access.</returns>
        public static bool CanAccessProcess(int processId, TokenAccessLevels access)
        {
            try
            {
                IntPtr handle = OpenProcess((uint)access | ProcessQueryLimitedInformation, false, processId);
                if (handle == IntPtr.Zero)
                {
                    // Try with PROCESS_QUERY_INFORMATION instead
                    handle = OpenProcess((uint)access | ProcessQueryInformation, false, processId);
                }

                if (handle == IntPtr.Zero)
                    return false;

                CloseHandle(handle);
                return true;
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
        /// Gets the SID history of a process — enumerates all group SIDs from the token
        /// and resolves each to a "DOMAIN\USER" or "S-1-..." string.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>A list of group SID strings, or an empty list on failure.</returns>
        public static IReadOnlyList<string> GetProcessSidHistory(int processId)
        {
            var result = new List<string>();
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return result;

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return result;

                    try
                    {
                        string[]? groups = TokenHelper.GetTokenGroups(tokenHandle);
                        if (groups == null)
                            return result;

                        // Resolve each group SID to an account name
                        foreach (string sid in groups)
                        {
                            if (!string.IsNullOrEmpty(sid))
                            {
                                result.Add(sid);
                            }
                        }
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
                return result;
            }
            catch (DllNotFoundException)
            {
                return result;
            }
            catch (PlatformNotSupportedException)
            {
                return result;
            }

            return result;
        }

        /// <summary>
        /// Checks if a process's token contains the BUILTIN\Administrators group
        /// with the SE_GROUP_ENABLED attribute.
        /// </summary>
        /// <param name="processId">The process ID to check.</param>
        /// <returns>True if the Administrators group is present and enabled.</returns>
        public static bool IsProcessInAdminGroup(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return false;

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return false;

                    try
                    {
                        return CheckAdminGroupInToken(tokenHandle);
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
        // Internal helpers
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
        /// Checks whether a token is elevated (has TOKEN_ELEVATION.TokenIsElevated != 0).
        /// </summary>
        private static bool IsTokenElevated(IntPtr tokenHandle)
        {
            // First call: get required buffer size
            int requiredSize = 0;
            GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, IntPtr.Zero, 0, out requiredSize);
            int error = Marshal.GetLastWin32Error();
            if (error != 122 && error != 0)
                return false;

            if (requiredSize < Marshal.SizeOf<TOKEN_ELEVATION>())
                return false;

            IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, buffer, requiredSize, out _))
                    return false;

                var elevation = Marshal.PtrToStructure<TOKEN_ELEVATION>(buffer);
                return elevation.TokenIsElevated != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Checks if the BUILTIN\Administrators group (SID S-1-5-32-544) exists
        /// in the token with SE_GROUP_ENABLED attribute.
        /// </summary>
        private static bool CheckAdminGroupInToken(IntPtr tokenHandle)
        {
            // Build the Administrators SID: S-1-5-32-544
            byte[] adminSid = BuildAdministratorsSid();

            // Get token groups
            if (!TokenHelper.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenGroups, out byte[]? data) || data == null)
                return false;

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                uint groupCount = (uint)Marshal.ReadInt32(ptr);
                if (groupCount == 0)
                    return false;

                int sidAndAttrSize = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
                IntPtr groupsPtr = IntPtr.Add(ptr, Marshal.SizeOf<uint>());

                for (int i = 0; i < groupCount; i++)
                {
                    IntPtr currentSidAndAttr = IntPtr.Add(groupsPtr, i * sidAndAttrSize);
                    var sidAndAttr = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(currentSidAndAttr);

                    if (sidAndAttr.Sid != IntPtr.Zero
                        && (sidAndAttr.Attributes & SE_GROUP_ENABLED) == SE_GROUP_ENABLED
                        && CompareSidToBuiltinSid(sidAndAttr.Sid, adminSid))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Builds the binary representation of the BUILTIN\Administrators SID (S-1-5-32-544).
        /// </summary>
        private static byte[] BuildAdministratorsSid()
        {
            byte[] sid = new byte[16]; // S-1-5-32-544: Revision=1, SubAuthCount=2, Auth=5, SubAuths=32,544
            sid[0] = 1; // Revision
            sid[1] = 2; // SubAuthorityCount
            // IdentifierAuthority = {0,0,0,0,0,5} (SECURITY_NT_AUTHORITY)
            sid[5] = 5;
            // SubAuthority[0] = 32 (SECURITY_BUILTIN_DOMAIN_RID)
            WriteInt32ToBuffer(sid, 8, 32);
            // SubAuthority[1] = 544 (DOMAIN_ALIAS_RID_ADMINS)
            WriteInt32ToBuffer(sid, 12, 544);
            return sid;
        }

        /// <summary>
        /// Writes a little-endian 32-bit integer to a byte buffer at the specified offset.
        /// </summary>
        private static void WriteInt32ToBuffer(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>
        /// Compares a SID pointer against a known binary SID representation.
        /// </summary>
        private static unsafe bool CompareSidToBuiltinSid(IntPtr sidPtr, byte[] expectedSid)
        {
            if (sidPtr == IntPtr.Zero || expectedSid == null)
                return false;

            try
            {
                byte revision = Marshal.ReadByte(sidPtr);
                byte subAuthCount = Marshal.ReadByte(sidPtr + 1);

                int actualSize = 8 + (subAuthCount * 4);
                if (actualSize != expectedSid.Length)
                    return false;

                byte[] actualSid = new byte[actualSize];
                Marshal.Copy(sidPtr, actualSid, 0, actualSize);

                for (int i = 0; i < actualSize; i++)
                {
                    if (actualSid[i] != expectedSid[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
