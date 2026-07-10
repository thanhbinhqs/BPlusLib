// <copyright file="Utilities.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.SerialPorts
{
    internal static class Utilities
    {
        internal static readonly int HandleEntrySize = Marshal.SizeOf<SystemExtendedInformationHandleEntry>();
        internal static readonly int HandleInfoHeaderSize = Marshal.SizeOf<SystemExtendedHandleInformation>();

        internal static string GetWin32ErrorMessage(int errorCode)
        {
            var sb = new StringBuilder(512);
            int result = NativeMethods.FormatMessageW(
                NativeMethods.FormatMessageFromSystem | NativeMethods.FormatMessageIgnoreInserts,
                IntPtr.Zero, errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
            if (result > 0)
                return sb.ToString(0, result).TrimEnd();
            return $"Unknown error (0x{errorCode:X8})";
        }

        internal static string? UnicodeStringToString(UnicodeString us)
        {
            if (us.Buffer == IntPtr.Zero || us.Length == 0)
                return null;
            return Marshal.PtrToStringUni(us.Buffer, us.Length / 2);
        }

        internal static int GetHandleInfoHeaderSize()
        {
            return IntPtr.Size + 4;
        }

        internal static string? ReadUnicodeStringFromPtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            try
            {
                var us = Marshal.PtrToStructure<UnicodeString>(ptr);
                return UnicodeStringToString(us);
            }
            catch { return null; }
        }
    }
}