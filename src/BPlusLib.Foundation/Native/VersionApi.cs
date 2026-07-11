// <copyright file="VersionApi.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for version.dll APIs used to read PE file version info.
    /// </summary>
    internal static class VersionApi
    {
        [DllImport("version.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetFileVersionInfoSizeExW(
            uint dwFlags,
            string lpFilename,
            out uint lpdwHandle);

        [DllImport("version.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetFileVersionInfoExW(
            uint dwFlags,
            string lpFilename,
            uint dwHandle,
            uint dwLen,
            IntPtr lpData);

        [DllImport("version.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VerQueryValueW(
            IntPtr pBlock,
            string lpSubBlock,
            out IntPtr lplpBuffer,
            out uint puLen);

        internal const uint FILE_VER_GET_NEUTRAL = 0x02;
        internal const uint FILE_VER_GET_LOCALISED = 0x01;
        internal const uint FILE_VER_GET_PREFERRED_LOCALE = 0x04;
    }
}
