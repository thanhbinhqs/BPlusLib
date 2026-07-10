// <copyright file="MemoryInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides information about system memory (RAM, virtual memory,
    /// and page file) using P/Invoke to kernel32 (GlobalMemoryStatusEx).
    /// No WMI dependency.
    /// </summary>
    public sealed class MemoryInfo
    {
        // =====================================================================
        // P/Invoke struct
        // =====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            internal uint dwLength;
            internal uint dwMemoryLoad;
            internal ulong ullTotalPhys;
            internal ulong ullAvailPhys;
            internal ulong ullTotalPageFile;
            internal ulong ullAvailPageFile;
            internal ulong ullTotalVirtual;
            internal ulong ullAvailVirtual;
            internal ulong ullAvailExtendedVirtual;
        }

        // =====================================================================
        // P/Invoke
        // =====================================================================

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        // =====================================================================
        // Lazy singleton
        // =====================================================================

        private static readonly Lazy<MemoryInfo> LazyCurrent =
            new Lazy<MemoryInfo>(() => new MemoryInfo());

        /// <summary>
        /// Gets the singleton <see cref="MemoryInfo"/> instance
        /// representing the current system memory state.
        /// </summary>
        public static MemoryInfo Current => LazyCurrent.Value;

        // =====================================================================
        // Private constructor
        // =====================================================================

        private MemoryInfo()
        {
            try
            {
                var memStatus = default(MEMORYSTATUSEX);
                memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    _totalPhysicalBytes = (long)memStatus.ullTotalPhys;
                    _availablePhysicalBytes = (long)memStatus.ullAvailPhys;
                    _totalVirtualBytes = (long)memStatus.ullTotalVirtual;
                    _availableVirtualBytes = (long)memStatus.ullAvailVirtual;
                    _totalPageFileBytes = (long)memStatus.ullTotalPageFile;
                    _availablePageFileBytes = (long)memStatus.ullAvailPageFile;
                    _memoryLoad = memStatus.dwMemoryLoad;
                }
            }
            catch
            {
                // Non-Windows or unsupported — all values remain 0
            }
        }

        // =====================================================================
        // Backing fields
        // =====================================================================

        private long _totalPhysicalBytes;
        private long _availablePhysicalBytes;
        private long _totalVirtualBytes;
        private long _availableVirtualBytes;
        private long _totalPageFileBytes;
        private long _availablePageFileBytes;
        private uint _memoryLoad;

        // =====================================================================
        // Public properties
        // =====================================================================

        /// <summary>Gets the total physical memory in bytes.</summary>
        public long TotalPhysicalBytes => _totalPhysicalBytes;

        /// <summary>Gets the available physical memory in bytes.</summary>
        public long AvailablePhysicalBytes => _availablePhysicalBytes;

        /// <summary>Gets the used physical memory in bytes (Total - Available).</summary>
        public long UsedPhysicalBytes => _totalPhysicalBytes - _availablePhysicalBytes;

        /// <summary>Gets the total virtual memory in bytes.</summary>
        public long TotalVirtualBytes => _totalVirtualBytes;

        /// <summary>Gets the available virtual memory in bytes.</summary>
        public long AvailableVirtualBytes => _availableVirtualBytes;

        /// <summary>Gets the used virtual memory in bytes (Total - Available).</summary>
        public long UsedVirtualBytes => _totalVirtualBytes - _availableVirtualBytes;

        /// <summary>Gets the total page file size in bytes.</summary>
        public long TotalPageFileBytes => _totalPageFileBytes;

        /// <summary>Gets the available page file space in bytes.</summary>
        public long AvailablePageFileBytes => _availablePageFileBytes;

        /// <summary>Gets the used page file space in bytes (Total - Available).</summary>
        public long UsedPageFileBytes => _totalPageFileBytes - _availablePageFileBytes;

        /// <summary>
        /// Gets the physical memory usage as a percentage (0.0–100.0).
        /// Uses the dwMemoryLoad field from GlobalMemoryStatusEx when available,
        /// otherwise computes as (Used / Total) * 100.
        /// </summary>
        public double PhysicalUsagePercent
        {
            get
            {
                if (_memoryLoad > 0 && _memoryLoad <= 100)
                    return _memoryLoad;

                if (_totalPhysicalBytes > 0)
                {
                    double used = _totalPhysicalBytes - _availablePhysicalBytes;
                    return (used / _totalPhysicalBytes) * 100.0;
                }

                return 0.0;
            }
        }
    }
}
