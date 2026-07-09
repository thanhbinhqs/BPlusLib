// <copyright file="SerialPortInspector.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BPlusLib.SerialPorts
{
    /// <summary>
    /// Identifies which process owns each Windows Serial Port (COM Port)
    /// using pure P/Invoke — no WMI, no Handle.exe, no third-party libraries.
    /// </summary>
    public static class SerialPortInspector
    {
        private static readonly Lazy<DosDeviceMapper> Mapper = new(
            () => new DosDeviceMapper(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<SerialPortMatcher> Matcher = new(
            () => new SerialPortMatcher(Mapper.Value), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<ProcessInformationProvider> ProcessProvider = new(
            () => new ProcessInformationProvider(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Enumerates every serial port currently opened in the system.
        /// </summary>
        /// <returns>Read-only list of <see cref="SerialPortOwner"/> records.</returns>
        public static IReadOnlyList<SerialPortOwner> GetAllOpenedSerialPorts()
        {
            var results = new List<SerialPortOwner>();
            var seenPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mapper = Mapper.Value;
            var matcher = Matcher.Value;
            var processProvider = ProcessProvider.Value;

            using var enumerator = new SystemHandleEnumerator();
            if (!enumerator.EnumerateAllHandles()) return results;

            int currentPid = NativeMethods.GetCurrentProcessId();
            IntPtr currentProcessHandle = NativeMethods.GetCurrentProcess();

            for (int i = 0; i < enumerator.HandleCount; i++)
            {
                SystemExtendedInformationHandleEntry entry;
                try { entry = enumerator.GetEntry(i); }
                catch { continue; }

                if (entry.UniqueProcessId == currentPid) continue;

                IntPtr sourceProcessHandle = IntPtr.Zero;
                IntPtr duplicatedHandle = IntPtr.Zero;
                try
                {
                    sourceProcessHandle = NativeMethods.OpenProcess(
                        NativeMethods.ProcessDuplicateHandle, false, entry.UniqueProcessId);
                    if (sourceProcessHandle == IntPtr.Zero) continue;

                    bool dupSuccess = NativeMethods.DuplicateHandle(
                        sourceProcessHandle, entry.HandleValue, currentProcessHandle,
                        out duplicatedHandle, 0, false, NativeMethods.DuplicateSameAccess);
                    if (!dupSuccess || duplicatedHandle == IntPtr.Zero) continue;

                    using var nameResolver = new ObjectNameResolver();
                    string? objectName = nameResolver.ResolveObjectName(duplicatedHandle);
                    if (objectName == null) continue;

                    if (matcher.TryMatch(objectName, out string? comPortName) && comPortName != null)
                    {
                        string dedupKey = $"{comPortName}:{entry.UniqueProcessId}";
                        if (seenPorts.Contains(dedupKey)) continue;

                        processProvider.TryGetProcessInformation(
                            entry.UniqueProcessId,
                            out string? procName, out string? imagePath, out DateTime? startTime,
                            out string? companyName, out string? productName, out string? commandLine,
                            out string? fileVersion, out string? productVersion);

                        var owner = new SerialPortOwner
                        {
                            PortName = comPortName,
                            DevicePath = objectName,
                            ProcessId = entry.UniqueProcessId,
                            ProcessName = procName ?? $"Unknown ({entry.UniqueProcessId})",
                            ImagePath = imagePath ?? string.Empty,
                            CommandLine = commandLine,
                            StartTime = startTime,
                            CompanyName = companyName,
                            ProductName = productName,
                            FileVersion = fileVersion,
                            ProductVersion = productVersion,
                        };

                        results.Add(owner);
                        seenPorts.Add(dedupKey);
                    }
                }
                catch { }
                finally
                {
                    if (duplicatedHandle != IntPtr.Zero) NativeMethods.CloseHandle(duplicatedHandle);
                    if (sourceProcessHandle != IntPtr.Zero) NativeMethods.CloseHandle(sourceProcessHandle);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the owner of a specific serial port by name.
        /// </summary>
        /// <param name="portName">The COM port name (e.g. "COM3"). Case-insensitive.</param>
        /// <returns>A <see cref="SerialPortOwner"/> if found, otherwise <c>null</c>.</returns>
        public static SerialPortOwner? GetSerialPortOwner(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return null;

            string normalizedName = portName.ToUpperInvariant();
            if (!normalizedName.StartsWith("COM", StringComparison.Ordinal))
                normalizedName = "COM" + normalizedName.TrimStart("COM".ToCharArray());

            var allPorts = GetAllOpenedSerialPorts();
            foreach (var port in allPorts)
            {
                if (string.Equals(port.PortName, normalizedName, StringComparison.OrdinalIgnoreCase))
                    return port;
            }
            return null;
        }
    }
}