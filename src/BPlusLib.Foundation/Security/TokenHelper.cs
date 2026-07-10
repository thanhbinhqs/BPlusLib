// <copyright file="TokenHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// Token access level flags — maps to Win32 TOKEN_* access mask constants.
    /// </summary>
    [Flags]
    public enum TokenAccessLevels
    {
        /// <summary>Required to assign a primary token. (TOKEN_ASSIGN_PRIMARY = 0x0001)</summary>
        AssignPrimary = 0x0001,

        /// <summary>Required to duplicate a token. (TOKEN_DUPLICATE = 0x0002)</summary>
        Duplicate = 0x0002,

        /// <summary>Required to attach an impersonation token. (TOKEN_IMPERSONATE = 0x0004)</summary>
        Impersonate = 0x0004,

        /// <summary>Required to query a token. (TOKEN_QUERY = 0x0008)</summary>
        Query = 0x0008,

        /// <summary>Required to query the source of a token. (TOKEN_QUERY_SOURCE = 0x0010)</summary>
        QuerySource = 0x0010,

        /// <summary>Required to enable/disable privileges or set token attributes. (TOKEN_ADJUST_PRIVILEGES = 0x0020)</summary>
        AdjustPrivileges = 0x0020,

        /// <summary>Required to change default DACL, primary group, or owner. (TOKEN_ADJUST_DEFAULT = 0x0080)</summary>
        AdjustDefault = 0x0080,

        /// <summary>Required to adjust the session ID. (TOKEN_ADJUST_SESSIONID = 0x0100)</summary>
        AdjustSessionId = 0x0100,

        /// <summary>Combines STANDARD_RIGHTS_READ | TOKEN_QUERY. (TOKEN_READ = 0x00020008)</summary>
        Read = 0x00020008,

        /// <summary>Combines STANDARD_RIGHTS_WRITE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_DEFAULT. (TOKEN_WRITE = 0x000200E0)</summary>
        Write = 0x000200E0,

        /// <summary>Combines all possible token access rights. (TOKEN_ALL_ACCESS = 0x000F01FF)</summary>
        AllAccess = 0x000F01FF,
    }

    /// <summary>
    /// Token type — TokenPrimary (1) or TokenImpersonation (2).
    /// </summary>
    public enum TokenType
    {
        /// <summary>Primary token (1).</summary>
        TokenPrimary = 1,

        /// <summary>Impersonation token (2).</summary>
        TokenImpersonation = 2,
    }

    /// <summary>
    /// TOKEN_INFORMATION_CLASS — defines the type of information to retrieve from a token.
    /// </summary>
    public enum TOKEN_INFORMATION_CLASS
    {
        /// <summary>TokenUser (1) — user SID of the token.</summary>
        TokenUser = 1,

        /// <summary>TokenGroups (2) — group SIDs in the token.</summary>
        TokenGroups = 2,

        /// <summary>TokenPrivileges (3) — privileges in the token.</summary>
        TokenPrivileges = 3,

        /// <summary>TokenOwner (4) — default owner SID.</summary>
        TokenOwner = 4,

        /// <summary>TokenPrimaryGroup (5) — default primary group SID.</summary>
        TokenPrimaryGroup = 5,

        /// <summary>TokenDefaultDacl (6) — default DACL.</summary>
        TokenDefaultDacl = 6,

        /// <summary>TokenSource (7) — source of the token.</summary>
        TokenSource = 7,

        /// <summary>TokenType (8) — primary or impersonation.</summary>
        TokenType = 8,

        /// <summary>TokenImpersonationLevel (9) — impersonation level.</summary>
        TokenImpersonationLevel = 9,

        /// <summary>TokenStatistics (10) — token statistics.</summary>
        TokenStatistics = 10,

        /// <summary>TokenSessionId (12) — terminal services session ID.</summary>
        TokenSessionId = 12,

        /// <summary>TokenGroupsAndPrivileges (13) — groups and privileges.</summary>
        TokenGroupsAndPrivileges = 13,

        /// <summary>TokenElevation (20) — whether the token is elevated.</summary>
        TokenElevation = 20,

        /// <summary>TokenIntegrityLevel (25) — integrity level SID.</summary>
        TokenIntegrityLevel = 25,
    }

    /// <summary>
    /// Represents a locally unique identifier (LUID) used by Windows for privileges.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        /// <summary>Low part of the LUID.</summary>
        public int LowPart;

        /// <summary>High part of the LUID.</summary>
        public int HighPart;
    }

    /// <summary>
    /// SID_AND_ATTRIBUTES structure — associates a SID with attribute flags.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SID_AND_ATTRIBUTES
    {
        /// <summary>Pointer to a SID structure.</summary>
        public IntPtr Sid;

        /// <summary>Attributes of the SID (e.g., SE_GROUP_* flags).</summary>
        public uint Attributes;
    }

    /// <summary>
    /// TOKEN_USER structure — contains the user SID of a token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_USER
    {
        /// <summary>SID and attributes for the user.</summary>
        public SID_AND_ATTRIBUTES User;
    }

    /// <summary>
    /// TOKEN_GROUPS structure — contains group SIDs in a token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_GROUPS
    {
        /// <summary>Number of groups in the token.</summary>
        public uint GroupCount;

        /// <summary>First group entry (variable-length array follows).</summary>
        public SID_AND_ATTRIBUTES Groups;
    }

    /// <summary>
    /// TOKEN_SOURCE structure — identifies the source of a token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_SOURCE
    {
        /// <summary>8-byte source name.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] SourceName;

        /// <summary>Source LUID.</summary>
        public LUID SourceIdentifier;
    }

    /// <summary>
    /// TOKEN_STATISTICS structure — contains various statistics about a token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_STATISTICS
    {
        /// <summary>Token ID (LUID).</summary>
        public LUID TokenId;

        /// <summary>Authentication ID (LUID).</summary>
        public LUID AuthenticationId;

        /// <summary>Expiration time (FILETIME).</summary>
        public long ExpirationTime;

        /// <summary>Token type (TokenPrimary or TokenImpersonation).</summary>
        public TokenType TokenType;

        /// <summary>Impersonation level.</summary>
        public SECURITY_IMPERSONATION_LEVEL ImpersonationLevel;

        /// <summary>Dynamic charged.</summary>
        public uint DynamicCharged;

        /// <summary>Dynamic available.</summary>
        public uint DynamicAvailable;

        /// <summary>Group count.</summary>
        public uint GroupCount;

        /// <summary>Privilege count.</summary>
        public uint PrivilegeCount;

        /// <summary>Modified ID (LUID).</summary>
        public LUID ModifiedId;
    }

    /// <summary>
    /// SECURITY_IMPERSONATION_LEVEL enumeration.
    /// </summary>
    internal enum SECURITY_IMPERSONATION_LEVEL
    {
        /// <summary>SecurityAnonymous (0).</summary>
        SecurityAnonymous = 0,

        /// <summary>SecurityIdentification (1).</summary>
        SecurityIdentification = 1,

        /// <summary>SecurityImpersonation (2).</summary>
        SecurityImpersonation = 2,

        /// <summary>SecurityDelegation (3).</summary>
        SecurityDelegation = 3,
    }

    /// <summary>
    /// TOKEN_ELEVATION structure — indicates whether a token is elevated.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_ELEVATION
    {
        /// <summary>Non-zero if the token is elevated.</summary>
        public uint TokenIsElevated;
    }

    /// <summary>
    /// Provides P/Invoke-based access to Windows token information.
    /// All methods are thread-safe and return null on failure rather than throwing.
    /// On non-Windows platforms, all methods gracefully return null/default.
    /// </summary>
    public static class TokenHelper
    {
        // =====================================================================
        // P/Invoke declarations
        // =====================================================================

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            int processId);

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
        private static extern bool ConvertSidToStringSidW(
            IntPtr sid,
            out IntPtr stringSid);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupAccountSidW(
            string? lpSystemName,
            IntPtr sid,
            StringBuilder? lpName,
            ref uint cchName,
            StringBuilder? lpReferencedDomainName,
            ref uint cchReferencedDomainName,
            out int peUse);

        // =====================================================================
        // Constants
        // =====================================================================

        private const uint TokenQuery = 0x0008;
        private const uint TokenQuerySource = 0x0010;
        private const uint TokenAdjustPrivileges = 0x0020;
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;

        // SE_GROUP attributes
        private const uint SE_GROUP_ENABLED = 0x00000004;
        private const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        private const uint SE_GROUP_LOGON_ID = unchecked((uint)0xC0000000);
        private const uint SE_GROUP_MANDATORY = 0x00000001;
        private const uint SE_GROUP_OWNER = 0x00000008;
        private const uint SE_GROUP_RESOURCE = 0x20000000;
        private const uint SE_GROUP_INTEGRITY = 0x00000020;
        private const uint SE_GROUP_INTEGRITY_ENABLED = 0x00000040;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Opens the access token associated with a process.
        /// </summary>
        /// <param name="processId">The process ID to open the token for.</param>
        /// <param name="desiredAccess">The desired token access level (default: Query).</param>
        /// <returns>A handle to the token, or null on failure or non-Windows.</returns>
        public static IntPtr? OpenProcessToken(int processId, TokenAccessLevels desiredAccess = TokenAccessLevels.Query)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return null;

                try
                {
                    if (OpenProcessToken(processHandle, (uint)desiredAccess, out IntPtr tokenHandle))
                        return tokenHandle;

                    return null;
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
        /// Retrieves generic token information as a byte array.
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <param name="infoClass">The type of token information to retrieve.</param>
        /// <param name="data">Receives the raw byte array, or null on failure.</param>
        /// <returns>True if the information was retrieved successfully.</returns>
        public static bool GetTokenInformation(IntPtr tokenHandle, TOKEN_INFORMATION_CLASS infoClass, out byte[]? data)
        {
            data = null;
            try
            {
                // First call: get required buffer size
                int requiredSize = 0;
                GetTokenInformation(tokenHandle, infoClass, IntPtr.Zero, 0, out requiredSize);
                int error = Marshal.GetLastWin32Error();
                if (error != 122 && error != 0) // ERROR_INSUFFICIENT_BUFFER = 122, or success with 0
                    return false;

                if (requiredSize <= 0)
                    return false;

                // Allocate and call again
                IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    if (GetTokenInformation(tokenHandle, infoClass, buffer, requiredSize, out int returnLength))
                    {
                        data = new byte[returnLength];
                        Marshal.Copy(buffer, data, 0, returnLength);
                        return true;
                    }

                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
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
        /// Gets the user SID string from a token.
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <returns>The user SID string (e.g., "S-1-5-21-..."), or null on failure.</returns>
        public static string? GetTokenUser(IntPtr tokenHandle)
        {
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, out byte[]? data) || data == null)
                    return null;

                // Parse the TOKEN_USER structure
                int sidAndAttrSize = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
                if (data.Length < sidAndAttrSize)
                    return null;

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    var tokenUser = Marshal.PtrToStructure<TOKEN_USER>(ptr);
                    return SidToString(tokenUser.User.Sid);
                }
                finally
                {
                    handle.Free();
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
        /// Gets the group SID strings from a token.
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <returns>Array of group SID strings, or null on failure.</returns>
        public static string[]? GetTokenGroups(IntPtr tokenHandle)
        {
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenGroups, out byte[]? data) || data == null)
                    return null;

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    uint groupCount = (uint)Marshal.ReadInt32(ptr);
                    if (groupCount == 0)
                        return Array.Empty<string>();

                    var groups = new string[groupCount];
                    int sidAndAttrSize = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
                    IntPtr groupsPtr = IntPtr.Add(ptr, Marshal.SizeOf<uint>());

                    for (int i = 0; i < groupCount; i++)
                    {
                        IntPtr currentSidAndAttr = IntPtr.Add(groupsPtr, i * sidAndAttrSize);
                        var sidAndAttr = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(currentSidAndAttr);
                        groups[i] = SidToString(sidAndAttr.Sid) ?? string.Empty;
                    }

                    return groups;
                }
                finally
                {
                    handle.Free();
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
        /// Gets the token type (primary or impersonation).
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <returns>The token type, or null on failure.</returns>
        public static TokenType? GetTokenType(IntPtr tokenHandle)
        {
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenType, out byte[]? data) || data == null)
                    return null;

                if (data.Length < 4)
                    return null;

                int rawType = BitConverter.ToInt32(data, 0);
                if (rawType == 1)
                    return TokenType.TokenPrimary;
                if (rawType == 2)
                    return TokenType.TokenImpersonation;

                return null;
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
        /// Gets the terminal services session ID from a token.
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <returns>The session ID, or null on failure.</returns>
        public static int? GetTokenSessionId(IntPtr tokenHandle)
        {
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenSessionId, out byte[]? data) || data == null)
                    return null;

                if (data.Length < 4)
                    return null;

                return BitConverter.ToInt32(data, 0);
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
        /// Gets the source name from a token (e.g., "User32", "Advapi").
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <returns>The source name string, or null on failure.</returns>
        public static string? GetTokenSource(IntPtr tokenHandle)
        {
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenSource, out byte[]? data) || data == null)
                    return null;

                int sourceSize = Marshal.SizeOf<TOKEN_SOURCE>();
                if (data.Length < sourceSize)
                    return null;

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    var source = Marshal.PtrToStructure<TOKEN_SOURCE>(ptr);

                    // Find null terminator in the 8-byte name
                    int len = 0;
                    while (len < source.SourceName.Length && source.SourceName[len] != 0)
                        len++;

                    return Encoding.ASCII.GetString(source.SourceName, 0, len);
                }
                finally
                {
                    handle.Free();
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
        /// Gets token statistics as a debug-friendly string.
        /// </summary>
        /// <param name="tokenHandle">An open token handle.</param>
        /// <returns>A formatted debug string with token statistics, or null on failure.</returns>
        public static string? GetTokenStatistics(IntPtr tokenHandle)
        {
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenStatistics, out byte[]? data) || data == null)
                    return null;

                int statsSize = Marshal.SizeOf<TOKEN_STATISTICS>();
                if (data.Length < statsSize)
                    return null;

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    var stats = Marshal.PtrToStructure<TOKEN_STATISTICS>(ptr);

                    string typeName = stats.TokenType switch
                    {
                        TokenType.TokenPrimary => "Primary",
                        TokenType.TokenImpersonation => "Impersonation",
                        _ => "Unknown",
                    };

                    string impLevel = stats.ImpersonationLevel switch
                    {
                        SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous => "Anonymous",
                        SECURITY_IMPERSONATION_LEVEL.SecurityIdentification => "Identification",
                        SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation => "Impersonation",
                        SECURITY_IMPERSONATION_LEVEL.SecurityDelegation => "Delegation",
                        _ => "Unknown",
                    };

                    return $"TokenId=({stats.TokenId.LowPart}:{stats.TokenId.HighPart}) "
                        + $"AuthId=({stats.AuthenticationId.LowPart}:{stats.AuthenticationId.HighPart}) "
                        + $"Type={typeName} "
                        + $"ImpLevel={impLevel} "
                        + $"Groups={stats.GroupCount} "
                        + $"Privileges={stats.PrivilegeCount} "
                        + $"DynamicCharged={stats.DynamicCharged} "
                        + $"DynamicAvailable={stats.DynamicAvailable}";
                }
                finally
                {
                    handle.Free();
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

        // =====================================================================
        // Internal helpers
        // =====================================================================

        /// <summary>
        /// Opens a process handle with query access.
        /// </summary>
        internal static IntPtr OpenProcessInternal(int processId)
        {
            // First try with PROCESS_QUERY_LIMITED_INFORMATION (Vista+)
            IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (handle != IntPtr.Zero)
                return handle;

            // Fall back to PROCESS_QUERY_INFORMATION
            return OpenProcess(ProcessQueryInformation, false, processId);
        }

        /// <summary>
        /// Converts a SID pointer to a string representation using ConvertSidToStringSidW.
        /// Falls back to manual parsing if the API is unavailable.
        /// </summary>
        internal static string? SidToString(IntPtr sidPtr)
        {
            if (sidPtr == IntPtr.Zero)
                return null;

            try
            {
                if (ConvertSidToStringSidW(sidPtr, out IntPtr strPtr))
                {
                    try
                    {
                        return Marshal.PtrToStringUni(strPtr);
                    }
                    finally
                    {
                        LocalFree(strPtr);
                    }
                }
            }
            catch (EntryPointNotFoundException)
            {
                // Fall through to manual parsing
            }
            catch (DllNotFoundException)
            {
                // Fall through to manual parsing
            }

            return SidToStringManual(sidPtr);
        }

        /// <summary>
        /// Manually parses a SID structure into a string (S-R-I-S-S... format).
        /// </summary>
        internal static string? SidToStringManual(IntPtr sidPtr)
        {
            if (sidPtr == IntPtr.Zero)
                return null;

            try
            {
                byte revision = Marshal.ReadByte(sidPtr);
                byte subAuthorityCount = Marshal.ReadByte(sidPtr + 1);

                // Read identifier authority (6 bytes at offset 2)
                byte[] idAuthority = new byte[6];
                Marshal.Copy(sidPtr + 2, idAuthority, 0, 6);

                // Compute the authority value
                long authority = 0;
                if (idAuthority[0] == 0 && idAuthority[1] == 0)
                {
                    // Authority fits in last 4 bytes
                    authority = ((long)idAuthority[2] << 24)
                              | ((long)idAuthority[3] << 16)
                              | ((long)idAuthority[4] << 8)
                              | idAuthority[5];
                }
                else
                {
                    // Use all 6 bytes
                    for (int i = 0; i < 6; i++)
                        authority = (authority << 8) | idAuthority[i];
                }

                var sb = new StringBuilder();
                sb.Append('S');
                sb.Append('-');
                sb.Append(revision);
                sb.Append('-');
                sb.Append(authority);

                // Read sub-authorities (each 4 bytes, starting at offset 8)
                for (int i = 0; i < subAuthorityCount; i++)
                {
                    int subAuth = Marshal.ReadInt32(sidPtr + 8 + (i * 4));
                    sb.Append('-');
                    sb.Append((uint)subAuth);
                }

                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves a SID to a DOMAIN\USER name using LookupAccountSidW.
        /// </summary>
        internal static string? SidToAccountName(IntPtr sidPtr)
        {
            if (sidPtr == IntPtr.Zero)
                return null;

            try
            {
                uint nameLen = 0;
                uint domainLen = 0;
                int sidType = 0;

                // First call: get required lengths
                LookupAccountSidW(null, sidPtr, null, ref nameLen, null, ref domainLen, out sidType);

                if (nameLen == 0)
                    return SidToString(sidPtr);

                var name = new StringBuilder((int)nameLen);
                var domain = new StringBuilder((int)domainLen);

                if (LookupAccountSidW(null, sidPtr, name, ref nameLen, domain, ref domainLen, out sidType))
                {
                    if (domain.Length > 0)
                        return $"{domain}\\{name}";

                    return name.ToString();
                }

                return SidToString(sidPtr);
            }
            catch (EntryPointNotFoundException)
            {
                return SidToString(sidPtr);
            }
            catch (DllNotFoundException)
            {
                return SidToString(sidPtr);
            }
        }

        /// <summary>
        /// Checks if a SID has a specific attribute flag set.
        /// </summary>
        internal static bool HasAttribute(uint attributes, uint flag)
        {
            return (attributes & flag) == flag;
        }
    }
}
