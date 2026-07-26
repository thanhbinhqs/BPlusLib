// <copyright file="HardwareDeviceInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;

namespace BPlusLib.Foundation.Hardware
{
    /// <summary>
    /// Represents detailed information about a Windows hardware device,
    /// including USB-specific properties for plug-and-play devices.
    /// </summary>
    public sealed class HardwareDeviceInfo
    {
        // ── Basic Device Info ─────────────────────────────────────────────

        /// <summary>Gets the device instance ID (e.g., "USB\VID_1234&amp;PID_5678\Serial").</summary>
        public string DeviceId { get; init; } = string.Empty;

        /// <summary>Gets the friendly device name (e.g., "Kingston DataTraveler").</summary>
        public string DeviceName { get; init; } = string.Empty;

        /// <summary>Gets the manufacturer name.</summary>
        public string Manufacturer { get; init; } = string.Empty;

        /// <summary>Gets the device class GUID.</summary>
        public string ClassGuid { get; init; } = string.Empty;

        /// <summary>Gets the device class name (e.g., "Disk drives", "USB").</summary>
        public string ClassName { get; init; } = string.Empty;

        /// <summary>Gets the device location info (e.g., "Port_#0001").</summary>
        public string LocationInfo { get; init; } = string.Empty;

        /// <summary>Gets the bus enumerator name (e.g., "USB", "PCI").</summary>
        public string EnumeratorName { get; init; } = string.Empty;

        /// <summary>Gets the device description.</summary>
        public string DeviceDescription { get; init; } = string.Empty;

        /// <summary>Gets the hardware ID string.</summary>
        public string HardwareId { get; init; } = string.Empty;

        /// <summary>Gets the bus-reported device descriptor string.</summary>
        public string BusReportedDeviceDesc { get; init; } = string.Empty;

        /// <summary>Gets whether the device is currently connected.</summary>
        public bool IsConnected { get; init; }

        /// <summary>Gets whether the device is removable.</summary>
        public bool IsRemovable { get; init; }

        /// <summary>Gets the first install date, if available.</summary>
        public DateTime? InstallDate { get; init; }

        // ── USB-Specific Info ─────────────────────────────────────────────

        /// <summary>Gets the USB Vendor ID (VID), or 0 if not a USB device.</summary>
        public int VendorId { get; init; }

        /// <summary>Gets the USB Product ID (PID), or 0 if not a USB device.</summary>
        public int ProductId { get; init; }

        /// <summary>Gets the USB serial number, if available.</summary>
        public string SerialNumber { get; init; } = string.Empty;

        /// <summary>Gets the USB firmware revision, if available.</summary>
        public string FirmwareRevision { get; init; } = string.Empty;

        /// <summary>Gets the USB hardware revision, if available.</summary>
        public string HardwareRevision { get; init; } = string.Empty;

        /// <summary>Gets the USB connection speed.</summary>
        public UsbSpeed Speed { get; init; }

        /// <summary>Gets the USB version string (e.g., "2.0", "3.0/3.1 Gen 1").</summary>
        public string UsbVersion { get; init; } = string.Empty;

        /// <summary>Gets the USB max power in milliamps.</summary>
        public int MaxPowerMilliamps { get; init; }

        /// <summary>Gets the USB device class name (e.g., "Mass Storage", "HID").</summary>
        public string DeviceClass { get; init; } = string.Empty;

        /// <summary>Gets the USB device subclass.</summary>
        public string DeviceSubClass { get; init; } = string.Empty;

        /// <summary>Gets the USB device protocol.</summary>
        public string DeviceProtocol { get; init; } = string.Empty;

        /// <summary>Gets whether the device is self-powered.</summary>
        public bool IsSelfPowered { get; init; }

        /// <summary>Gets whether the device can wake the host remotely.</summary>
        public bool IsRemoteWakeCapable { get; init; }

        /// <summary>Returns a human-readable summary of this device.</summary>
        public override string ToString()
        {
            if (VendorId > 0 && ProductId > 0)
            {
                string speedStr = Speed != UsbSpeed.Unknown ? $" [{UsbVersion}]" : "";
                return $"{DeviceName} (VID={VendorId:X4}, PID={ProductId:X4}{speedStr}) — {ClassName}";
            }
            return $"{DeviceName} — {ClassName}";
        }
    }
}
