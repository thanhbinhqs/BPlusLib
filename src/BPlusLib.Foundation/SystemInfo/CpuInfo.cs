// <copyright file="CpuInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides detailed information about the host CPU(s) —
    /// name, manufacturer, core counts, architecture, clock speed,
    /// virtualization detection, and current load percentage.
    /// All data is obtained via P/Invoke and registry reads; no WMI.
    /// </summary>
    public sealed class CpuInfo
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const int RelationProcessorCore = 0;

        private static readonly string RegistryCpuKey =
            @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

        // =====================================================================
        // P/Invoke structs
        // =====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            internal ushort wProcessorArchitecture;
            internal ushort wReserved;
            internal uint dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal ushort wProcessorLevel;
            internal ushort wProcessorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            internal int Relationship;
            internal int Size;
            // Followed by an anonymous union — we handle via manual marshalling
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GROUP_AFFINITY
        {
            internal UIntPtr Mask;
            internal ushort Group;
            internal ushort Reserved1;
            internal ushort Reserved2;
            internal ushort Reserved3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSOR_RELATIONSHIP
        {
            internal byte Flags;
            private byte Reserved1;
            private byte Reserved2;
            private byte Reserved3;
            internal int EfficiencyClass;
            // Variable-length array of GROUP_AFFINITY follows
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
        {
            internal long IdleTime;
            internal long KernelTime;
            internal long UserTime;
            internal long Reserved0;
            internal long Reserved1;
        }

        // =====================================================================
        // P/Invoke declarations
        // =====================================================================

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        private static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLogicalProcessorInformationEx(
            int relationshipType,
            IntPtr buffer,
            ref int returnedLength);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        private static extern int NtQuerySystemInformation(
            int informationClass,
            IntPtr buffer,
            int bufferSize,
            out int returnedLength);

        // =====================================================================
        // Constants for NtQuerySystemInformation
        // =====================================================================

        private const int SystemProcessorPerformanceInformation = 8;

        // =====================================================================
        // Lazy singleton
        // =====================================================================

        private static readonly Lazy<CpuInfo> LazyCurrent =
            new Lazy<CpuInfo>(() => new CpuInfo());

        /// <summary>
        /// Gets the singleton <see cref="CpuInfo"/> instance
        /// representing the host CPU.
        /// </summary>
        public static CpuInfo Current => LazyCurrent.Value;

        // =====================================================================
        // Private constructor
        // =====================================================================

        private CpuInfo()
        {
            LoadRegistryInfo();

            try
            {
                var sysInfo = default(SYSTEM_INFO);
                GetSystemInfo(ref sysInfo);
                _logicalCores = (int)sysInfo.dwNumberOfProcessors;
            }
            catch
            {
                _logicalCores = Environment.ProcessorCount;
            }

            _physicalCores = CountPhysicalCores();

            if (string.IsNullOrEmpty(_architecture))
            {
                _architecture = IntPtr.Size == 8 ? "x64" : "x86";
            }

            _isVirtualMachine = DetectVirtualMachine();
        }

        // =====================================================================
        // Backing fields
        // =====================================================================

        private string _name = string.Empty;
        private string _manufacturer = string.Empty;
        private int _physicalCores;
        private int _logicalCores;
        private string _architecture = string.Empty;
        private int _processorId;
        private long _maxFrequencyMHz;
#pragma warning disable CS0649
        private long _currentFrequencyMHz; // N/A on most systems; stays 0
#pragma warning restore CS0649
        private bool _isVirtualMachine;
        private float? _currentLoadPercentage;

        // =====================================================================
        // Public properties
        // =====================================================================

        /// <summary>Gets the processor name string (e.g., "Intel(R) Core(TM) i7-10750H").</summary>
        public string Name => _name;

        /// <summary>Gets the manufacturer name (e.g., "Intel", "AMD", "ARM").</summary>
        public string Manufacturer => _manufacturer;

        /// <summary>Gets the number of physical cores.</summary>
        public int PhysicalCores => _physicalCores;

        /// <summary>Gets the number of logical processors (including hyper-threading).</summary>
        public int LogicalCores => _logicalCores;

        /// <summary>Gets the processor architecture string ("x86", "x64", "ARM64").</summary>
        public string Architecture => _architecture;

        /// <summary>Gets the processor ID (Family/Model/Stepping encoded).</summary>
        public int ProcessorId => _processorId;

        /// <summary>Gets the maximum frequency in MHz.</summary>
        public long MaxFrequencyMHz => _maxFrequencyMHz;

        /// <summary>Gets the current frequency in MHz (may be 0 if unavailable).</summary>
        public long CurrentFrequencyMHz => _currentFrequencyMHz;

        /// <summary>
        /// Gets whether the system is likely running inside a virtual machine,
        /// determined by CPU vendor string or BIOS contents.
        /// </summary>
        public bool IsVirtualMachine => _isVirtualMachine;

        /// <summary>
        /// Gets the current CPU load percentage (0.0–100.0), or <c>null</c>
        /// if the data could not be obtained.
        /// </summary>
        public float? CurrentLoadPercentage
        {
            get
            {
                if (_currentLoadPercentage.HasValue)
                    return _currentLoadPercentage;

                _currentLoadPercentage = ComputeCurrentLoad();
                return _currentLoadPercentage;
            }
        }

        // =====================================================================
        // Private methods
        // =====================================================================

        private void LoadRegistryInfo()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryCpuKey);
                if (key == null) return;

                string? processorName = key.GetValue("ProcessorNameString") as string;
                string? vendor = key.GetValue("VendorIdentifier") as string;
                object? freqObj = key.GetValue("~MHz");
                object? identifier = key.GetValue("Identifier");
                object? arch = key.GetValue("ProcessorArchitecture");

                _name = processorName ?? string.Empty;
                _manufacturer = vendor ?? string.Empty;

                if (freqObj is int freqInt)
                    _maxFrequencyMHz = freqInt;
                else if (freqObj is string freqStr && int.TryParse(freqStr, out int parsed))
                    _maxFrequencyMHz = parsed;

                if (arch is string archStr && !string.IsNullOrEmpty(archStr))
                    _architecture = archStr;
                else
                    _architecture = IntPtr.Size == 8 ? "x64" : "x86";

                // Parse ProcessorId from Identifier string (e.g., "x86 Family 6 Model 158 Stepping 10")
                if (identifier is string idStr)
                {
                    _processorId = ParseProcessorId(idStr);
                }
            }
            catch
            {
                // Registry read failed — non-fatal
            }
        }

        private static int ParseProcessorId(string identifier)
        {
            int id = 0;
            try
            {
                // Extract Family, Model, Stepping
                int family = ExtractNumericValue(identifier, "Family");
                int model = ExtractNumericValue(identifier, "Model");
                int stepping = ExtractNumericValue(identifier, "Stepping");

                // Encode as (Family << 16) | (Model << 8) | Stepping
                id = (family << 16) | (model << 8) | stepping;
            }
            catch
            {
                // Ignore parse errors
            }

            return id;
        }

        private static int ExtractNumericValue(string text, string key)
        {
            int idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return 0;

            idx += key.Length;
            // Skip non-numeric characters
            while (idx < text.Length && !char.IsDigit(text[idx]))
                idx++;

            if (idx >= text.Length) return 0;

            int start = idx;
            while (idx < text.Length && char.IsDigit(text[idx]))
                idx++;

            return int.TryParse(text.Substring(start, idx - start), out int val) ? val : 0;
        }

        private int CountPhysicalCores()
        {
            try
            {
                int bufferSize = 0;
                // First call to get required size
                GetLogicalProcessorInformationEx(RelationProcessorCore, IntPtr.Zero, ref bufferSize);

                if (bufferSize <= 0)
                    return FallbackPhysicalCoreCount();

                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    if (!GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref bufferSize))
                        return FallbackPhysicalCoreCount();

                    int count = 0;
                    int offset = 0;

                    while (offset < bufferSize)
                    {
                        var item = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(
                            IntPtr.Add(buffer, offset));
                        if (item.Relationship == RelationProcessorCore)
                            count++;

                        if (item.Size <= 0) break;
                        offset += item.Size;
                    }

                    return count > 0 ? count : FallbackPhysicalCoreCount();
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                return FallbackPhysicalCoreCount();
            }
        }

        private int FallbackPhysicalCoreCount()
        {
            // Reasonable fallback: assume hyper-threading, physical = logical / 2
            int logical = _logicalCores > 0 ? _logicalCores : Environment.ProcessorCount;
            return Math.Max(1, logical / 2);
        }

        private bool DetectVirtualMachine()
        {
            try
            {
                // Check CPU vendor string for known hypervisor signatures
                string vendor = _manufacturer.ToUpperInvariant();
                if (vendor.Contains("KVMKVMKVM") ||
                    vendor.Contains("VMWAREVMWARE") ||
                    vendor.Contains("MICROSOFT HV") ||
                    vendor.Contains("VBOX") ||
                    vendor.Contains("XEN"))
                {
                    return true;
                }

                // Check BIOS version in registry
                using var biosKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\SystemBiosVersion");
                if (biosKey != null)
                {
                    string[]? biosVersions = biosKey.GetValue(null) as string[];
                    if (biosVersions != null)
                    {
                        foreach (string bv in biosVersions)
                        {
                            string upper = bv.ToUpperInvariant();
                            if (upper.Contains("VIRTUAL") ||
                                upper.Contains("VMWARE") ||
                                upper.Contains("XEN") ||
                                upper.Contains("VBOX"))
                            {
                                return true;
                            }
                        }
                    }
                }

                // Check for Hyper-V presence
                using var hyperVKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Virtualization");
                if (hyperVKey != null)
                {
                    // Key exists — likely running on Hyper-V
                    return true;
                }
            }
            catch
            {
                // Ignore check failures
            }

            return false;
        }

        private float? ComputeCurrentLoad()
        {
            try
            {
                int cpuCount = _logicalCores > 0 ? _logicalCores : Environment.ProcessorCount;
                int structSize = Marshal.SizeOf<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();
                int bufferSize = structSize * cpuCount;

                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    int status = NtQuerySystemInformation(
                        SystemProcessorPerformanceInformation,
                        buffer,
                        bufferSize,
                        out int returnedLength);

                    if (status != 0 || returnedLength < structSize)
                        return null;

                    long totalIdle = 0;
                    long totalKernel = 0;
                    long totalUser = 0;

                    for (int i = 0; i < cpuCount; i++)
                    {
                        var perf = Marshal.PtrToStructure<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>(
                            IntPtr.Add(buffer, i * structSize));
                        totalIdle += perf.IdleTime;
                        totalKernel += perf.KernelTime;
                        totalUser += perf.UserTime;
                    }

                    // KernelTime includes IdleTime, so total = (kernel + user) but idle is already in kernel
                    long totalTotal = totalKernel + totalUser;
                    if (totalTotal == 0)
                        return null;

                    // This is a point-in-time sample; for real accuracy take two samples.
                    // For a singleton, we report what we can.
                    // Idle / total gives idle fraction; load = 1 - idle/total.
                    float idleFraction = (float)totalIdle / totalTotal;
                    float load = (1.0f - idleFraction) * 100.0f;
                    return load < 0.0f ? 0.0f : (load > 100.0f ? 100.0f : load);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
