// <copyright file="SerialPortOwner.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;

namespace BPlusLib.Foundation.SerialPorts
{
    /// <summary>
    /// Represents the owner (process) of an opened Windows Serial Port (COM Port).
    /// </summary>
    public sealed class SerialPortOwner
    {
        /// <summary>COM port name (e.g. "COM3").</summary>
        public string PortName { get; init; } = string.Empty;

        /// <summary>NT device path (e.g. @"\Device\Serial0").</summary>
        public string DevicePath { get; init; } = string.Empty;

        /// <summary>Owning process ID.</summary>
        public int ProcessId { get; init; }

        /// <summary>Process name (e.g. "notepad.exe").</summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>Full image (executable) path.</summary>
        public string ImagePath { get; init; } = string.Empty;

        /// <summary>Full command line, or null.</summary>
        public string? CommandLine { get; init; }

        /// <summary>Process start time (UTC), or null.</summary>
        public DateTime? StartTime { get; init; }

        /// <summary>Company name from PE version resource, or null.</summary>
        public string? CompanyName { get; init; }

        /// <summary>Product name from PE version resource, or null.</summary>
        public string? ProductName { get; init; }

        /// <summary>File version string (e.g. "10.0.19041.1"), or null.</summary>
        public string? FileVersion { get; init; }

        /// <summary>Product version string (e.g. "10.0.19041.1"), or null.</summary>
        public string? ProductVersion { get; init; }

        /// <summary>Returns a human-readable summary.</summary>
        public override string ToString()
        {
            return $"Port={PortName} (PID={ProcessId}, Name={ProcessName}, Image={ImagePath})";
        }
    }
}