// <copyright file="UsbDeviceParser.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BPlusLib.Foundation.Hardware
{
    /// <summary>
    /// USB connection speed supported by the device.
    /// </summary>
    public enum UsbSpeed
    {
        /// <summary>Speed unknown or not determined.</summary>
        Unknown = 0,
        /// <summary>USB 1.0 Low Speed — 1.5 Mbps.</summary>
        LowSpeed = 1,
        /// <summary>USB 1.1 Full Speed — 12 Mbps.</summary>
        FullSpeed = 2,
        /// <summary>USB 2.0 High Speed — 480 Mbps.</summary>
        HighSpeed = 3,
        /// <summary>USB 3.0/3.1 Gen 1 SuperSpeed — 5 Gbps.</summary>
        SuperSpeed = 4,
        /// <summary>USB 3.1 Gen 2/3.2 SuperSpeed+ — 10 Gbps.</summary>
        SuperSpeedPlus = 5,
    }

    /// <summary>
    /// Parses USB-specific information from device instance IDs and registry properties.
    /// USB device IDs follow the pattern: USB\VID_XXXX&amp;PID_XXXX\SerialNumber
    /// </summary>
    internal static class UsbDeviceParser
    {
        // Matches USB\VID_XXXX&PID_XXXX\...
        private static readonly Regex UsbIdRegex = new Regex(
            @"USB\\VID_(?<VID>[0-9A-Fa-f]{4})&PID_(?<PID>[0-9A-Fa-f]{4})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Matches USB\VID_XXXX&PID_XXXX\SerialNumber
        private static readonly Regex UsbSerialRegex = new Regex(
            @"USB\\VID_(?<VID>[0-9A-Fa-f]{4})&PID_(?<PID>[0-9A-Fa-f]{4})\\(?<Serial>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Parses speed from bus-reported device descriptor
        // Format: "USB x.xx, Speed = High speed (480Mbit/s)" or "Speed = SuperSpeed (5Gbit/s)"
        private static readonly Regex UsbSpeedRegex = new Regex(
            @"Speed\s*=\s*(?<speed>SuperSpeed\+|SuperSpeed|Low|Full|High)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Parses max power from bus-reported device descriptor
        // Format: "MaxPower = 500mA"
        private static readonly Regex UsbMaxPowerRegex = new Regex(
            @"MaxPower\s*=\s*(?<power>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses VID and PID from a device instance ID.
        /// </summary>
        internal static bool TryParseVidPid(string deviceId, out int vendorId, out int productId)
        {
            vendorId = 0;
            productId = 0;

            if (string.IsNullOrEmpty(deviceId)) return false;

            var match = UsbIdRegex.Match(deviceId);
            if (!match.Success) return false;

            return int.TryParse(match.Groups["VID"].Value, NumberStyles.HexNumber, null, out vendorId)
                && int.TryParse(match.Groups["PID"].Value, NumberStyles.HexNumber, null, out productId);
        }

        /// <summary>
        /// Parses serial number from a USB device instance ID.
        /// </summary>
        internal static string? ParseSerialNumber(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;

            var match = UsbSerialRegex.Match(deviceId);
            if (!match.Success) return null;

            string serial = match.Groups["Serial"].Value;
            return string.IsNullOrEmpty(serial) ? null : serial;
        }

        /// <summary>
        /// Checks if a device instance ID is a USB device.
        /// </summary>
        internal static bool IsUsbDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return false;
            return deviceId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses USB speed from the bus-reported device descriptor string.
        /// The string typically looks like: "USB 2.00, Speed = High speed (480Mbit/s)"
        /// </summary>
        internal static UsbSpeed ParseSpeed(string busReportedDesc)
        {
            if (string.IsNullOrEmpty(busReportedDesc)) return UsbSpeed.Unknown;

            var match = UsbSpeedRegex.Match(busReportedDesc);
            if (!match.Success) return UsbSpeed.Unknown;

            string speed = match.Groups["speed"].Value.ToLowerInvariant();
            return speed switch
            {
                "low" => UsbSpeed.LowSpeed,
                "full" => UsbSpeed.FullSpeed,
                "high" => UsbSpeed.HighSpeed,
                "super" or "superspeed" => UsbSpeed.SuperSpeed,
                "superspeed+" or "superspeedplus" => UsbSpeed.SuperSpeedPlus,
                _ => UsbSpeed.Unknown
            };
        }

        /// <summary>
        /// Gets human-readable USB version string from speed.
        /// </summary>
        internal static string GetUsbVersionString(UsbSpeed speed)
        {
            return speed switch
            {
                UsbSpeed.LowSpeed => "1.0",
                UsbSpeed.FullSpeed => "1.1",
                UsbSpeed.HighSpeed => "2.0",
                UsbSpeed.SuperSpeed => "3.0/3.1 Gen 1",
                UsbSpeed.SuperSpeedPlus => "3.1 Gen 2/3.2",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets max power in mA from a USB device descriptor string.
        /// Format: "MaxPower = 500mA" or "MaxPower = 500"
        /// </summary>
        internal static int ParseMaxPower(string busReportedDesc)
        {
            if (string.IsNullOrEmpty(busReportedDesc)) return 0;

            var match = UsbMaxPowerRegex.Match(busReportedDesc);
            if (match.Success && int.TryParse(match.Groups["power"].Value, out int ma))
                return ma;

            return 0;
        }

        /// <summary>
        /// Gets USB device class name from class code.
        /// </summary>
        internal static string GetDeviceClassName(int classCode)
        {
            return classCode switch
            {
                0x00 => "Use Interface Descriptor",
                0x01 => "Audio",
                0x02 => "CDC (Communications)",
                0x03 => "HID (Human Interface)",
                0x07 => "Printer",
                0x08 => "Mass Storage",
                0x09 => "Hub",
                0x0A => "CDC-Data",
                0x0B => "Smart Card",
                0x0D => "Content Security",
                0x0E => "Video",
                0x0F => "Personal Healthcare",
                0x10 => "Audio/Video",
                0x11 => "Billboard",
                0x12 => "USB Type-C Bridge",
                0xDC => "Diagnostic",
                0xE0 => "Wireless Controller",
                0xEF => "Miscellaneous",
                0xFE => "Application Specific",
                0xFF => "Vendor Specific",
                _ => $"Unknown (0x{classCode:X2})"
            };
        }

        /// <summary>
        /// Gets speed description string for display.
        /// </summary>
        internal static string GetSpeedDescription(UsbSpeed speed)
        {
            return speed switch
            {
                UsbSpeed.LowSpeed => "USB 1.0 Low Speed (1.5 Mbps)",
                UsbSpeed.FullSpeed => "USB 1.1 Full Speed (12 Mbps)",
                UsbSpeed.HighSpeed => "USB 2.0 High Speed (480 Mbps)",
                UsbSpeed.SuperSpeed => "USB 3.0/3.1 Gen 1 SuperSpeed (5 Gbps)",
                UsbSpeed.SuperSpeedPlus => "USB 3.1 Gen 2/3.2 SuperSpeed+ (10 Gbps)",
                _ => "Unknown USB Speed"
            };
        }
    }
}
