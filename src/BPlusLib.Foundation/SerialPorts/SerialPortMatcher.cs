// <copyright file="SerialPortMatcher.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;

namespace BPlusLib.Foundation.SerialPorts
{
    internal sealed class SerialPortMatcher
    {
        private readonly DosDeviceMapper _mapper;
        private Dictionary<string, string>? _reverseCache;
        private readonly object _reverseCacheLock = new();

        internal SerialPortMatcher(DosDeviceMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        private IReadOnlyDictionary<string, string> GetReverseMapping()
        {
            if (_reverseCache == null)
            {
                lock (_reverseCacheLock)
                {
                    if (_reverseCache == null)
                        _reverseCache = BuildReverseMapping();
                }
            }
            return _reverseCache;
        }

        internal bool TryMatch(string ntDevicePath, out string? comPortName)
        {
            comPortName = null;
            if (string.IsNullOrEmpty(ntDevicePath)) return false;

            string normalizedPath = ntDevicePath.TrimEnd('\0');
            var reverseMapping = GetReverseMapping();
            if (reverseMapping.TryGetValue(normalizedPath, out comPortName))
                return true;
            return false;
        }

        internal void InvalidateCache() { lock (_reverseCacheLock) _reverseCache = null; }

        private Dictionary<string, string> BuildReverseMapping()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var mapping = _mapper.GetComPortMapping();
            foreach (var kvp in mapping)
            {
                if (!result.ContainsKey(kvp.Value))
                    result[kvp.Value] = kvp.Key;
            }
            return result;
        }
    }
}