// <copyright file="ProcessInformationProvider.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.SerialPorts
{
    internal sealed class ProcessInformationProvider
    {
        internal bool TryGetProcessInformation(
            int processId,
            out string? processName,
            out string? imagePath,
            out DateTime? startTime,
            out string? companyName,
            out string? productName,
            out string? commandLine,
            out string? fileVersion,
            out string? productVersion)
        {
            processName = null; imagePath = null; startTime = null;
            companyName = null; productName = null; commandLine = null;
            fileVersion = null; productVersion = null;

            IntPtr processHandle = IntPtr.Zero;
            try
            {
                processHandle = NativeMethods.OpenProcess(
                    NativeMethods.ProcessQueryInformation | NativeMethods.ProcessDuplicateHandle,
                    false, processId);
                if (processHandle == IntPtr.Zero) return false;

                try { processName = GetProcessNameFromId(processId); } catch { }
                imagePath = GetImagePath(processHandle);
                startTime = GetProcessStartTime(processHandle);
                commandLine = GetCommandLine(processHandle);

                string? resolvedImagePath = imagePath;
                if (resolvedImagePath != null)
                    GetVersionInfo(resolvedImagePath, out companyName, out productName, out fileVersion, out productVersion);

                return true;
            }
            catch { return false; }
            finally
            {
                if (processHandle != IntPtr.Zero) NativeMethods.CloseHandle(processHandle);
            }
        }

        private static string? GetProcessNameFromId(int processId)
        {
            try { var proc = Process.GetProcessById(processId); return proc.ProcessName + ".exe"; }
            catch { return null; }
        }

        private static string? GetImagePath(IntPtr processHandle)
        {
            try
            {
                var sb = new StringBuilder((int)NativeMethods.ExtendedMaxPathChars);
                uint size = (uint)sb.Capacity;
                if (NativeMethods.QueryFullProcessImageName(processHandle, NativeMethods.ProcessNameWin32, sb, ref size))
                    return sb.ToString(0, (int)size);
                return null;
            }
            catch { return null; }
        }

        private static DateTime? GetProcessStartTime(IntPtr processHandle)
        {
            try
            {
                if (NativeMethods.GetProcessTimes(processHandle, out long creationTime, out _, out _, out _))
                    return DateTime.FromFileTimeUtc(creationTime);
                return null;
            }
            catch { return null; }
        }

        private static string? GetCommandLine(IntPtr processHandle)
        {
            try
            {
                int bufferSize = 4096;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    byte[] buffer = new byte[bufferSize];
                    GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        IntPtr bufferPtr = pin.AddrOfPinnedObject();
                        int status = NativeMethods.NtQueryInformationProcess(
                            processHandle, NativeMethods.ProcessCommandLineInformation,
                            bufferPtr, buffer.Length, out int returnLength);
                        if (NativeMethods.NtSuccess(status))
                        {
                            var us = Marshal.PtrToStructure<UnicodeString>(bufferPtr);
                            return Utilities.UnicodeStringToString(us);
                        }
                        if (status == NativeMethods.StatusBufferOverflow ||
                            status == NativeMethods.StatusInfoLengthMismatch ||
                            status == NativeMethods.StatusBufferTooSmall)
                        {
                            if (returnLength > bufferSize && returnLength <= NativeMethods.MaxCommandLineChars)
                            { bufferSize = returnLength; continue; }
                            bufferSize *= 2;
                            if (bufferSize > NativeMethods.MaxCommandLineChars) return null;
                            continue;
                        }
                        return null;
                    }
                    finally { pin.Free(); }
                }
                return null;
            }
            catch { return null; }
        }

        private static void GetVersionInfo(string filePath,
            out string? companyName, out string? productName,
            out string? fileVersion, out string? productVersion)
        {
            companyName = null; productName = null;
            fileVersion = null; productVersion = null;
            try { GetVersionInfoViaApi(filePath, out companyName, out productName, out fileVersion, out productVersion); }
            catch { }
        }

        private static void GetVersionInfoViaApi(string filePath,
            out string? companyName, out string? productName,
            out string? fileVersion, out string? productVersion)
        {
            companyName = null; productName = null;
            fileVersion = null; productVersion = null;
            try
            {
                var info = FileVersionInfo.GetVersionInfo(filePath);
                companyName = info.CompanyName;
                productName = info.ProductName;
                fileVersion = info.FileVersion;
                productVersion = info.ProductVersion;
                if (string.IsNullOrEmpty(companyName)) companyName = null;
                if (string.IsNullOrEmpty(productName)) productName = null;
                if (string.IsNullOrEmpty(fileVersion)) fileVersion = null;
                if (string.IsNullOrEmpty(productVersion)) productVersion = null;
            }
            catch { }
        }
    }
}