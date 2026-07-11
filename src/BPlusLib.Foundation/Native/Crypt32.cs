// <copyright file="Crypt32.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for crypt32.dll — certificate and cryptographic operations.
    /// </summary>
    internal static class Crypt32
    {
        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptQueryObject(
            uint dwObjectType,
            IntPtr pvObject,
            uint dwExpectedContentTypeFlags,
            uint dwExpectedFormatTypeFlags,
            uint dwFlags,
            out uint pdwMsgAndCertEncodingType,
            out uint pdwContentType,
            out uint pdwFormatType,
            out IntPtr phCertStore,
            out IntPtr phMsg,
            out IntPtr ppvContext);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CertCloseStore(
            IntPtr hCertStore,
            uint dwFlags);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CertFindCertificateInStore(
            IntPtr hCertStore,
            uint dwCertEncodingType,
            uint dwFindFlags,
            uint dwFindType,
            IntPtr pvFindPara,
            IntPtr pPrevCertContext);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CertGetNameStringW(
            IntPtr pCertContext,
            uint dwType,
            uint dwFlags,
            IntPtr pvTypePara,
            StringBuilder pszNameString,
            uint cchNameString);

        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        // Object type for CryptQueryObject
        internal const uint CERT_QUERY_OBJECT_FILE = 1;
        internal const uint CERT_QUERY_OBJECT_BLOB = 2;

        // Content type flags
        internal const uint CERT_QUERY_CONTENT_FLAG_ALL = 0x00000FFE;
        internal const uint CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 0x00000040;

        // Format type flags
        internal const uint CERT_QUERY_FORMAT_FLAG_ALL = 0x0000000E;

        // Cert name types
        internal const uint CERT_NAME_SIMPLE_DISPLAY_TYPE = 4;
        internal const uint CERT_NAME_ISSUER_DISPLAY_TYPE = 5;

        // Encoding types
        internal const uint PKCS_7_ASN_ENCODING = 0x00010000;
        internal const uint X509_ASN_ENCODING = 0x00000001;

        // Find types
        internal const uint CERT_FIND_ANY = 0;
    }
}
