// <copyright file="NtDll.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for ntdll.dll — NT kernel-level system information queries.
    /// </summary>
    internal static class NtDll
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary>SystemExtendedHandleInformation class (64).</summary>
        internal const int SystemExtendedHandleInformation = 64;

        /// <summary>ObjectNameInformation class (1).</summary>
        internal const int ObjectNameInformation = 1;

        /// <summary>ObjectTypeInformation class (2).</summary>
        internal const int ObjectTypeInformation = 2;

        /// <summary>ProcessBasicInformation class (0).</summary>
        internal const int ProcessBasicInformation = 0;

        /// <summary>ProcessCommandLineInformation class (63).</summary>
        internal const int ProcessCommandLineInformation = 63;

        /// <summary>STATUS_INFO_LENGTH_MISMATCH.</summary>
        internal const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);

        /// <summary>STATUS_SUCCESS.</summary>
        internal const int StatusSuccess = 0;

        /// <summary>STATUS_BUFFER_TOO_SMALL.</summary>
        internal const int StatusBufferTooSmall = unchecked((int)0xC0000023);

        /// <summary>STATUS_BUFFER_OVERFLOW (warning).</summary>
        internal const int StatusBufferOverflow = unchecked((int)0x80000005);

        /// <summary>Initial handle buffer size (256 KB).</summary>
        internal const int InitialHandleBufferSize = 256 * 1024;

        /// <summary>Maximum handle buffer size (16 MB).</summary>
        internal const int MaxHandleBufferSize = 16 * 1024 * 1024;

        /// <summary>Maximum object name characters (1024).</summary>
        internal const int MaxObjectNameChars = 1024;

        /// <summary>Maximum command line characters (32768).</summary>
        internal const int MaxCommandLineChars = 32768;

        /// <summary>Invalid handle sentinel value (-1).</summary>
        internal static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>
        /// Returns true if the NT status code indicates success (status >= 0).
        /// </summary>
        internal static bool NtSuccess(int status) => status >= 0;

        // =====================================================================
        // P/Invoke
        // =====================================================================

        /// <summary>
        /// Queries system information of the specified class.
        /// </summary>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        internal static extern int NtQuerySystemInformation(
            int informationClass,
            IntPtr buffer,
            int bufferSize,
            out int returnedLength);

        /// <summary>
        /// Queries information about an object handle.
        /// </summary>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        internal static extern int NtQueryObject(
            IntPtr handle,
            int objectInformationClass,
            IntPtr objectInformation,
            int objectInformationLength,
            out int returnLength);

        /// <summary>
        /// Queries information about a process.
        /// </summary>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        internal static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            IntPtr processInformation,
            int processInformationLength,
            out int returnLength);
    }
}
