// <copyright file="DosDeviceMapper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace BPlusLib.SerialPorts
{
    internal sealed class DosDeviceMapper
    {
        private readonly object _cacheLock = new();
        private Dictionary<string, string>? _cache;

        internal IReadOnlyDictionary<string, string> GetComPortMapping()
        {
            if (_cache == null)
            {
                lock (_cacheLock)
                {
                    if (_cache == null)
                        _cache = BuildMapping();
                }
            }
            return _cache;
        }

        internal bool TryResolvePort(string portName, out string? devicePath)
        {
            try
            {
                var mapping = GetComPortMapping();
                if (mapping.TryGetValue(portName, out string? cachedPath))
                { devicePath = cachedPath; return true; }

                var sb = new StringBuilder(NativeMethods.MaxPathChars);
                uint result = NativeMethods.QueryDosDevice(portName, sb, (uint)sb.Capacity);
                if (result > 0 && sb.Length > 0)
                { devicePath = sb.ToString().TrimEnd('\0'); return !string.IsNullOrEmpty(devicePath); }
            }
            catch { }
            devicePath = null;
            return false;
        }

        internal void InvalidateCache() => _cache = null;

        private Dictionary<string, string> BuildMapping()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var nameBuffer = new StringBuilder(NativeMethods.MaxPathChars * 256);
                uint enumResult = NativeMethods.QueryDosDevice(null, nameBuffer, (uint)nameBuffer.Capacity);
                if (enumResult == 0) return result;

                string multiString = nameBuffer.ToString();
                int start = 0;
                while (start < multiString.Length)
                {
                    int end = multiString.IndexOf('\0', start);
                    if (end < 0) break;
                    string name = multiString.Substring(start, end - start);
                    start = end + 1;
                    if (string.IsNullOrEmpty(name)) break;
                    if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                    {
                        var deviceSb = new StringBuilder(NativeMethods.MaxPathChars);
                        uint devResult = NativeMethods.QueryDosDevice(name, deviceSb, (uint)deviceSb.Capacity);
                        if (devResult > 0 && deviceSb.Length > 0)
                        {
                            string devicePath = deviceSb.ToString().TrimEnd('\0');
                            if (!string.IsNullOrEmpty(devicePath))
                                result[name] = devicePath;
                        }
                    }
                }
            }
            catch { }
            return result;
        }
    }
}