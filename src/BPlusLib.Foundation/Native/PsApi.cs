// <copyright file="PsApi.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for psapi.dll — process status and memory information.
    /// </summary>
    internal static class PsApi
    {
        /// <summary>
        /// Retrieves memory usage information for the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process.</param>
        /// <param name="ppsmemCounters">Receives the process memory counters.</param>
        /// <param name="cb">The size of the PROCESS_MEMORY_COUNTERS structure, in bytes.</param>
        /// <returns>True if the function succeeds.</returns>
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessMemoryInfo(
            IntPtr hProcess,
            out PROCESS_MEMORY_COUNTERS ppsmemCounters,
            uint cb);
    }

    /// <summary>
    /// Contains the memory statistics for a process.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_MEMORY_COUNTERS
    {
        /// <summary>The size of the structure, in bytes.</summary>
        internal uint cb;

        /// <summary>The number of page faults.</summary>
        internal uint PageFaultCount;

        /// <summary>The peak working set size, in bytes.</summary>
        internal IntPtr PeakWorkingSetSize;

        /// <summary>The current working set size, in bytes.</summary>
        internal IntPtr WorkingSetSize;

        /// <summary>The peak paged pool usage, in bytes.</summary>
        internal IntPtr QuotaPeakPagedPoolUsage;

        /// <summary>The current paged pool usage, in bytes.</summary>
        internal IntPtr QuotaPagedPoolUsage;

        /// <summary>The peak non-paged pool usage, in bytes.</summary>
        internal IntPtr QuotaPeakNonPagedPoolUsage;

        /// <summary>The current non-paged pool usage, in bytes.</summary>
        internal IntPtr QuotaNonPagedPoolUsage;

        /// <summary>The current page file usage, in bytes.</summary>
        internal IntPtr PagefileUsage;

        /// <summary>The peak page file usage, in bytes.</summary>
        internal IntPtr PeakPagefileUsage;
    }
}
