// <copyright file="WinTrustHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// Trust level classification for Authenticode-signed files.
    /// </summary>
    public enum TrustLevel
    {
        /// <summary>Trust status could not be determined.</summary>
        Unknown = 0,
        /// <summary>The file is not trusted (unsigned or signature invalid).</summary>
        Untrusted = 1,
        /// <summary>The file is trusted with a valid signature.</summary>
        Trusted = 2,
        /// <summary>The file is trusted and revocation check passed.</summary>
        TrustedWithRevocation = 3,
    }

    /// <summary>
    /// Information about the Authenticode signature of a file.
    /// </summary>
    public sealed class SignatureInfo
    {
        /// <summary>Whether the file has a digital signature.</summary>
        public bool IsSigned { get; init; }
        /// <summary>Overall trust level.</summary>
        public TrustLevel TrustLevel { get; init; }
        /// <summary>Signer name (organization or individual).</summary>
        public string? SignerName { get; init; }
        /// <summary>Publisher name from the signing certificate.</summary>
        public string? PublisherName { get; init; }
        /// <summary>SHA-1 thumbprint of the signing certificate.</summary>
        public string? Thumbprint { get; init; }
        /// <summary>Timestamp from the countersignature, if available.</summary>
        public DateTime? Timestamp { get; init; }
        /// <summary>Whether this is a Microsoft-signed OS binary.</summary>
        public bool IsOSBinary { get; init; }
        /// <summary>Detailed error code from WinVerifyTrust (0 = trusted).</summary>
        public int ErrorCode { get; init; }
        /// <summary>Human-readable description of the signature state.</summary>
        public string? StatusDescription { get; init; }
    }

    /// <summary>
    /// Provides Authenticode digital signature verification for PE files
    /// using pure P/Invoke (WinVerifyTrust + CryptQueryObject).
    /// All methods are thread-safe and return null on error.
    /// </summary>
    public static class WinTrustHelper
    {
        /// <summary>
        /// Verifies the Authenticode signature of a file and returns detailed
        /// signature information.
        /// </summary>
        public static SignatureInfo Verify(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return CreateUnsigned(filePath, "Path is null or empty.");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return CreateUnsigned(filePath, "Not running on Windows.");

            try
            {
                // Step 1: WinVerifyTrust
                var fileInfo = new WINTRUST_FILE_INFO
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                    pcwszFilePath = filePath,
                    hFile = IntPtr.Zero,
                    pgKnownSubject = IntPtr.Zero,
                };

                IntPtr fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
                try
                {
                    Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

                    var data = new WINTRUST_DATA
                    {
                        cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                        dwUIChoice = WinTrust.WTD_UI_NONE,
                        fdwRevocationChecks = WinTrust.WTD_REVOCATION_NONE,
                        dwUnionChoice = WinTrust.WTD_CHOICE_FILE,
                        pFile = fileInfoPtr,
                        dwStateAction = WinTrust.WTD_STATEACTION_VERIFY,
                        dwProvFlags = WinTrust.WTD_SAFER_FLAG,
                        dwUIContext = 0,
                        pPolicyCallbackData = IntPtr.Zero,
                        pSIPCallbackData = IntPtr.Zero,
                        hWVTStateData = IntPtr.Zero,
                        pwszURLReference = null,
                    };

                    IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_DATA>());
                    try
                    {
                        Marshal.StructureToPtr(data, dataPtr, false);
                        Guid action = WinTrust.WINTRUST_ACTION_GENERIC_VERIFY_V2;
                        int hr = WinTrust.WinVerifyTrust(null, ref action, dataPtr);
                        bool isSigned = hr == 0;

                        // Step 2: Get signer info via CryptQueryObject
                        string? signerName = null;
                        if (isSigned)
                        {
                            signerName = GetSignerName(filePath);
                        }

                        // Step 3: Close state
                        data.dwStateAction = WinTrust.WTD_STATEACTION_CLOSE;
                        Marshal.StructureToPtr(data, dataPtr, false);
                        WinTrust.WinVerifyTrust(null, ref action, dataPtr);

                        return new SignatureInfo
                        {
                            IsSigned = isSigned,
                            TrustLevel = isSigned ? TrustLevel.Trusted : TrustLevel.Untrusted,
                            SignerName = signerName,
                            ErrorCode = hr,
                            StatusDescription = GetStatusDescription(hr),
                        };
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(dataPtr);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(fileInfoPtr);
                }
            }
            catch (DllNotFoundException)
            {
                return CreateUnsigned(filePath, "wintrust.dll not available.");
            }
            catch
            {
                return CreateUnsigned(filePath, "Exception during verification.");
            }
        }

        /// <summary>
        /// Quick check: returns true if the file has a valid Authenticode signature.
        /// </summary>
        public static bool IsSigned(string filePath)
        {
            var info = Verify(filePath);
            return info.IsSigned;
        }

        /// <summary>
        /// Returns the publisher/signer name from the digital signature, if any.
        /// </summary>
        public static string? GetPublisher(string filePath)
        {
            var info = Verify(filePath);
            return info.SignerName;
        }

        private static string? GetSignerName(string filePath)
        {
            IntPtr pFilePath = IntPtr.Zero;
            try
            {
                pFilePath = Marshal.StringToHGlobalUni(filePath);
                if (!Crypt32.CryptQueryObject(
                        Crypt32.CERT_QUERY_OBJECT_FILE,
                        pFilePath,
                        Crypt32.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED,
                        Crypt32.CERT_QUERY_FORMAT_FLAG_ALL,
                        0,
                        out _,
                        out _,
                        out _,
                        out IntPtr hStore,
                        out _,
                        out IntPtr ppvContext))
                    return null;

                try
                {
                    IntPtr pCert = Crypt32.CertFindCertificateInStore(
                        hStore,
                        Crypt32.PKCS_7_ASN_ENCODING | Crypt32.X509_ASN_ENCODING,
                        0,
                        Crypt32.CERT_FIND_ANY,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    if (pCert != IntPtr.Zero)
                    {
                        try
                        {
                            var sb = new StringBuilder(1024);
                            if (Crypt32.CertGetNameStringW(
                                    pCert,
                                    Crypt32.CERT_NAME_SIMPLE_DISPLAY_TYPE,
                                    0,
                                    IntPtr.Zero,
                                    sb,
                                    1024))
                                return sb.ToString();
                        }
                        finally
                        {
                            Crypt32.CertFreeCertificateContext(pCert);
                        }
                    }
                }
                finally
                {
                    Crypt32.CertCloseStore(hStore, 0);
                }
            }
            catch
            {
                // Signer name is optional
            }
            finally
            {
                if (pFilePath != IntPtr.Zero)
                    Marshal.FreeHGlobal(pFilePath);
            }

            return null;
        }

        private static SignatureInfo CreateUnsigned(string filePath, string reason)
        {
            return new SignatureInfo
            {
                IsSigned = false,
                TrustLevel = TrustLevel.Untrusted,
                StatusDescription = reason,
                ErrorCode = -1,
            };
        }

        private static string GetStatusDescription(int hr)
        {
            return hr switch
            {
                0 => "Trusted — signature verified successfully.",
                WinTrust.TRUST_E_SUBJECT_NOT_TRUSTED => "Subject not trusted.",
                WinTrust.TRUST_E_PROVIDER_UNKNOWN => "Trust provider unknown.",
                WinTrust.TRUST_E_ACTION_UNKNOWN => "Action unknown.",
                WinTrust.TRUST_E_SUBJECT_FORM_UNKNOWN => "Subject form unknown (not a signed PE file).",
                WinTrust.CERT_E_UNTRUSTEDROOT => "Certificate chain has an untrusted root.",
                WinTrust.CERT_E_CHAINING => "Certificate chain is invalid.",
                WinTrust.CERT_E_EXPIRED => "Certificate has expired.",
                WinTrust.CRYPT_E_FILE_ERROR => "File error — could not read the file.",
                _ => $"Unknown result (0x{hr:X8}).",
            };
        }
    }
}
