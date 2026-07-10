// <copyright file="IntegrityHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// Defines Windows integrity levels (also known as Mandatory Integrity Control or MIC).
    /// </summary>
    public enum IntegrityLevel
    {
        /// <summary>Untrusted integrity level (SID S-1-16-0).</summary>
        Untrusted = 0,

        /// <summary>Low integrity level (SID S-1-16-4096 = 0x1000).</summary>
        Low = 0x1000,

        /// <summary>Medium integrity level (SID S-1-16-8192 = 0x2000).</summary>
        Medium = 0x2000,

        /// <summary>High integrity level (SID S-1-16-12288 = 0x3000). Typically for elevated processes.</summary>
        High = 0x3000,

        /// <summary>System integrity level (SID S-1-16-16384 = 0x4000).</summary>
        System = 0x4000,

        /// <summary>Protected process integrity level (SID S-1-16-20480 = 0x5000).</summary>
        ProtectedProcess = 0x5000,

        /// <summary>Unknown integrity level.</summary>
        Unknown = -1,
    }

    /// <summary>
    /// TOKEN_MANDATORY_LABEL structure — contains the mandatory integrity label for a token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_MANDATORY_LABEL
    {
        /// <summary>SID and attributes for the integrity label.</summary>
        public SID_AND_ATTRIBUTES Label;
    }

    /// <summary>
    /// Provides methods to query and set the integrity level (Mandatory Integrity Control)
    /// of processes using pure P/Invoke.
    /// Thread-safe; gracefully returns Unknown/default on non-Windows.
    /// </summary>
    public static class IntegrityHelper
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
        private static extern bool SetTokenInformation(
            IntPtr tokenHandle,
            TOKEN_INFORMATION_CLASS tokenInformationClass,
            IntPtr tokenInformation,
            int tokenInformationLength);

        // =====================================================================
        // Constants
        // =====================================================================

        private const uint TokenQuery = 0x0008;
        private const uint TokenAdjustDefault = 0x0080;
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;

        // SID attributes for integrity labels
        private const uint SE_GROUP_INTEGRITY = 0x00000020;
        private const uint SE_GROUP_INTEGRITY_ENABLED = 0x00000040;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Gets the integrity level of the current process.
        /// </summary>
        public static IntegrityLevel CurrentProcessIntegrityLevel
        {
            get
            {
                try
                {
                    IntPtr currentProcess = GetCurrentProcess();
                    if (currentProcess == IntPtr.Zero)
                        return IntegrityLevel.Unknown;

                    if (!OpenProcessToken(currentProcess, TokenQuery, out IntPtr tokenHandle))
                        return IntegrityLevel.Unknown;

                    try
                    {
                        return GetIntegrityLevelFromToken(tokenHandle);
                    }
                    finally
                    {
                        CloseHandle(tokenHandle);
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    return IntegrityLevel.Unknown;
                }
                catch (DllNotFoundException)
                {
                    return IntegrityLevel.Unknown;
                }
                catch (PlatformNotSupportedException)
                {
                    return IntegrityLevel.Unknown;
                }
            }
        }

        /// <summary>
        /// Gets the integrity level of a specific process.
        /// </summary>
        /// <param name="processId">The process ID to query.</param>
        /// <returns>The integrity level, or <see cref="IntegrityLevel.Unknown"/> on failure.</returns>
        public static IntegrityLevel GetProcessIntegrityLevel(int processId)
        {
            try
            {
                IntPtr processHandle = OpenProcessInternal(processId);
                if (processHandle == IntPtr.Zero)
                    return IntegrityLevel.Unknown;

                try
                {
                    if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
                        return IntegrityLevel.Unknown;

                    try
                    {
                        return GetIntegrityLevelFromToken(tokenHandle);
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
                return IntegrityLevel.Unknown;
            }
            catch (DllNotFoundException)
            {
                return IntegrityLevel.Unknown;
            }
            catch (PlatformNotSupportedException)
            {
                return IntegrityLevel.Unknown;
            }
        }

        /// <summary>
        /// Sets the integrity level of the current process.
        /// Requires SeIncreaseQuotaPrivilege to raise the integrity level.
        /// </summary>
        /// <param name="level">The target integrity level.</param>
        /// <returns>True if the integrity level was set successfully; false otherwise.</returns>
        public static bool SetProcessIntegrityLevel(IntegrityLevel level)
        {
            if (level == IntegrityLevel.Unknown)
                return false;

            try
            {
                IntPtr currentProcess = GetCurrentProcess();
                if (currentProcess == IntPtr.Zero)
                    return false;

                // Need TOKEN_ADJUST_DEFAULT to set token information
                if (!OpenProcessToken(currentProcess, TokenQuery | TokenAdjustDefault, out IntPtr tokenHandle))
                    return false;

                try
                {
                    return SetIntegrityLevelOnToken(tokenHandle, level);
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
        /// Reads the integrity level from a TOKEN_MANDATORY_LABEL structure.
        /// </summary>
        private static IntegrityLevel GetIntegrityLevelFromToken(IntPtr tokenHandle)
        {
            // First call: get required buffer size
            int requiredSize = 0;
            GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, IntPtr.Zero, 0, out requiredSize);
            int error = Marshal.GetLastWin32Error();
            if (error != 122 && error != 0)
                return IntegrityLevel.Unknown;

            if (requiredSize <= 0)
                return IntegrityLevel.Unknown;

            IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, buffer, requiredSize, out _))
                    return IntegrityLevel.Unknown;

                // The buffer contains a TOKEN_MANDATORY_LABEL structure
                // Label.Sid points to a SID; the sub-authority of the SID is the integrity level
                var label = Marshal.PtrToStructure<TOKEN_MANDATORY_LABEL>(buffer);
                if (label.Label.Sid == IntPtr.Zero)
                    return IntegrityLevel.Unknown;

                return GetIntegrityLevelFromSid(label.Label.Sid);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Extracts the integrity level from a SID pointer.
        /// The integrity level is the last sub-authority of the SID.
        /// Integrity SIDs are of the form S-1-16-{Level} where Level = 0x1000 * n.
        /// </summary>
        private static IntegrityLevel GetIntegrityLevelFromSid(IntPtr sidPtr)
        {
            try
            {
                if (sidPtr == IntPtr.Zero)
                    return IntegrityLevel.Unknown;

                byte subAuthorityCount = Marshal.ReadByte(sidPtr + 1);
                if (subAuthorityCount == 0)
                    return IntegrityLevel.Unknown;

                // The last sub-authority is the integrity level value
                int lastSubAuthOffset = 8 + ((subAuthorityCount - 1) * 4);
                int rawLevel = Marshal.ReadInt32(sidPtr + lastSubAuthOffset);

                return rawLevel switch
                {
                    0 => IntegrityLevel.Untrusted,
                    0x1000 => IntegrityLevel.Low,
                    0x2000 => IntegrityLevel.Medium,
                    0x3000 => IntegrityLevel.High,
                    0x4000 => IntegrityLevel.System,
                    0x5000 => IntegrityLevel.ProtectedProcess,
                    _ => (IntegrityLevel)(-1),
                };
            }
            catch
            {
                return IntegrityLevel.Unknown;
            }
        }

        /// <summary>
        /// Sets the integrity level on a token by constructing a TOKEN_MANDATORY_LABEL
        /// with the appropriate integrity SID.
        /// </summary>
        private static bool SetIntegrityLevelOnToken(IntPtr tokenHandle, IntegrityLevel level)
        {
            // Build mandatory integrity SID: S-1-16-{Level}
            // SID structure:
            //   byte[0]: Revision (1)
            //   byte[1]: SubAuthorityCount (1)
            //   byte[2-7]: IdentifierAuthority (16 = 0x000000000010)
            //   byte[8-11]: SubAuthority[0] = integrity level

            int sidSize = 8 + 4; // 8 bytes header + 1 sub-authority (4 bytes)
            IntPtr sidBuffer = Marshal.AllocHGlobal(sidSize);
            try
            {
                Marshal.WriteByte(sidBuffer, 0, 1); // Revision = 1
                Marshal.WriteByte(sidBuffer, 1, 1); // SubAuthorityCount = 1

                // IdentifierAuthority for SECURITY_MANDATORY_LABEL_AUTHORITY {0,0,0,0,0,16}
                byte[] authority = { 0, 0, 0, 0, 0, 16 };
                Marshal.Copy(authority, 0, sidBuffer + 2, 6);

                // Sub-authority[0] = integrity level value
                Marshal.WriteInt32(sidBuffer + 8, (int)level);

                // Build TOKEN_MANDATORY_LABEL
                var label = new TOKEN_MANDATORY_LABEL
                {
                    Label = new SID_AND_ATTRIBUTES
                    {
                        Sid = sidBuffer,
                        Attributes = SE_GROUP_INTEGRITY | SE_GROUP_INTEGRITY_ENABLED,
                    },
                };

                int labelSize = Marshal.SizeOf<TOKEN_MANDATORY_LABEL>();
                IntPtr labelBuffer = Marshal.AllocHGlobal(labelSize);
                try
                {
                    Marshal.StructureToPtr(label, labelBuffer, false);

                    return SetTokenInformation(
                        tokenHandle,
                        TOKEN_INFORMATION_CLASS.TokenIntegrityLevel,
                        labelBuffer,
                        labelSize);
                }
                finally
                {
                    Marshal.DestroyStructure(labelBuffer, typeof(TOKEN_MANDATORY_LABEL));
                    Marshal.FreeHGlobal(labelBuffer);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(sidBuffer);
            }
        }
    }
}
