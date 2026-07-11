// <copyright file="WinTrust.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for wintrust.dll — Authenticode signature verification.
    /// </summary>
    internal static class WinTrust
    {
        // =================================================================
        // WinVerifyTrust
        // =================================================================

        /// <summary>
        /// Verifies the Authenticode signature of a file.
        /// Returns 0 on success (trusted).
        /// </summary>
        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int WinVerifyTrust(
            [MarshalAs(UnmanagedType.LPWStr)] string? pwszFileName,
            ref Guid pgActionID,
            IntPtr pWinTrustData);

        /// <summary>WINTRUST_ACTION_GENERIC_VERIFY_V2</summary>
        internal static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
            new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        // =================================================================
        // Constants
        // =================================================================

        internal const uint WTD_CHOICE_FILE = 1;
        internal const uint WTD_CHOICE_CATALOG = 2;
        internal const uint WTD_CHOICE_BLOB = 3;
        internal const uint WTD_CHOICE_SIGNER = 4;
        internal const uint WTD_CHOICE_CERT = 5;

        internal const uint WTD_UI_NONE = 2;
        internal const uint WTD_UI_ALL = 1;

        internal const uint WTD_REVOCATION_NONE = 0;
        internal const uint WTD_REVOCATION_WHOLECHAIN = 1;

        internal const uint WTD_STATEACTION_IGNORE = 0;
        internal const uint WTD_STATEACTION_VERIFY = 1;
        internal const uint WTD_STATEACTION_CLOSE = 2;
        internal const uint WTD_STATEACTION_AUTO_CACHE = 3;
        internal const uint WTD_STATEACTION_AUTO_CACHE_FLUSH = 4;

        internal const uint WTD_SAFER_FLAG = 0x00000001;
        internal const uint WTD_HASH_ONLY_FLAG = 0x00000002;
        internal const uint WTD_USE_DEFAULT_OSVER_CHECK = 0x00000004;
        internal const uint WTD_LIFETIME_SIGNING_FLAG = 0x00000008;
        internal const uint WTD_CACHE_ONLY_URL_RETRIEVAL = 0x00000010;

        // WinVerifyTrust return values
        internal const int ERROR_SUCCESS = 0;
        internal const int TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004);
        internal const int TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001);
        internal const int TRUST_E_ACTION_UNKNOWN = unchecked((int)0x800B0002);
        internal const int TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003);
        internal const int CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109);
        internal const int CERT_E_CHAINING = unchecked((int)0x800B010A);
        internal const int CERT_E_EXPIRED = unchecked((int)0x800B0101);
        internal const int CRYPT_E_FILE_ERROR = unchecked((int)0x80092003);
        internal const int CRYPT_E_NO_MATCH = unchecked((int)0x80092009);
    }

    /// <summary>
    /// WINTRUST_FILE_INFO structure for WinVerifyTrust.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    /// <summary>
    /// WINTRUST_DATA structure for WinVerifyTrust.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPCallbackData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;  // WINTRUST_FILE_INFO* when dwUnionChoice == WTD_CHOICE_FILE
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
    }
}
