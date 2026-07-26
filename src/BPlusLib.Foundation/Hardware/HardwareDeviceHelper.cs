// <copyright file="HardwareDeviceHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Hardware
{
    /// <summary>
    /// Enumerates Windows hardware devices and retrieves detailed information
    /// including USB speed, VID/PID, serial numbers, and device classes.
    /// Uses pure P/Invoke (SetupAPI) — no WMI, no PowerShell.
    /// All methods are thread-safe and return empty lists on failure.
    /// </summary>
    public static class HardwareDeviceHelper
    {
        // Well-known device class GUIDs
        private static readonly Guid USB_DEVICE_CLASS = new Guid("a5dcbf10-6530-11d2-901f-00c04fb925f3");
        private static readonly Guid HID_DEVICE_CLASS = new Guid("745a17a0-74d3-11d0-b6fe-00a0c90f57da");
        private static readonly Guid DISK_DRIVE_CLASS = new Guid("4d36e967-e325-11ce-bfc1-08002be10318");
        private static readonly Guid NET_CLASS = new Guid("4d36e972-e325-11ce-bfc1-08002be10318");
        private static readonly Guid COMPORT_CLASS = new Guid("4d36e978-e325-11ce-bfc1-08002be10318");

        /// <summary>
        /// Enumerates all hardware devices in the system.
        /// </summary>
        /// <returns>A list of all detected hardware devices, or an empty list on failure.</returns>
        public static IReadOnlyList<HardwareDeviceInfo> GetAllDevices()
        {
            return EnumerateDevices(IntPtr.Zero, SetupApi.DIGCF_ALLCLASSES);
        }

        /// <summary>
        /// Enumerates devices of a specific class.
        /// </summary>
        /// <param name="classGuid">The device class GUID to enumerate.</param>
        /// <returns>A list of devices in the specified class, or an empty list on failure.</returns>
        public static IReadOnlyList<HardwareDeviceInfo> GetDevicesByClass(Guid classGuid)
        {
            IntPtr devInfoSet = SetupApi.SetupDiGetClassDevs(
                ref classGuid, IntPtr.Zero, IntPtr.Zero,
                SetupApi.DIGCF_PRESENT);

            if (devInfoSet == SetupApi.INVALID_HANDLE_VALUE)
                return Array.Empty<HardwareDeviceInfo>();

            try
            {
                return EnumerateFromSet(devInfoSet);
            }
            finally
            {
                SetupApi.SetupDiDestroyDeviceInfoList(devInfoSet);
            }
        }

        /// <summary>
        /// Gets a specific device by its instance ID.
        /// </summary>
        /// <param name="deviceId">The device instance ID to find.</param>
        /// <returns>The device info if found; otherwise null.</returns>
        public static HardwareDeviceInfo? GetDeviceById(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;

            var allDevices = GetAllDevices();
            foreach (var device in allDevices)
            {
                if (string.Equals(device.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                    return device;
            }
            return null;
        }

        /// <summary>Gets all USB devices.</summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetUsbDevices()
        {
            return GetDevicesByClass(USB_DEVICE_CLASS);
        }

        /// <summary>
        /// Gets USB storage devices (mass storage class).
        /// Returns devices with detailed USB speed and power information.
        /// </summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetUsbStorageDevices()
        {
            var usbDevices = GetUsbDevices();
            var result = new List<HardwareDeviceInfo>();
            foreach (var device in usbDevices)
            {
                if (device.DeviceClass.IndexOf("Mass Storage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    device.DeviceDescription.IndexOf("Storage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    device.DeviceDescription.IndexOf("Mass", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(device);
                }
            }
            return result;
        }

        /// <summary>Gets USB HID devices.</summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetUsbHidDevices()
        {
            return GetDevicesByClass(HID_DEVICE_CLASS);
        }

        /// <summary>Gets COM/serial ports.</summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetComPorts()
        {
            return GetDevicesByClass(COMPORT_CLASS);
        }

        /// <summary>Gets disk drives.</summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetDiskDrives()
        {
            return GetDevicesByClass(DISK_DRIVE_CLASS);
        }

        /// <summary>Gets network adapters.</summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetNetworkAdapters()
        {
            return GetDevicesByClass(NET_CLASS);
        }

        /// <summary>
        /// Checks if a device is currently present in the system.
        /// </summary>
        /// <param name="deviceId">The device instance ID to check.</param>
        /// <returns>True if the device is present; false otherwise.</returns>
        public static bool IsDevicePresent(string deviceId)
        {
            return GetDeviceById(deviceId) != null;
        }

        private static IReadOnlyList<HardwareDeviceInfo> EnumerateDevices(IntPtr classGuid, int flags)
        {
            IntPtr devInfoSet = SetupApi.SetupDiGetClassDevs(
                classGuid, IntPtr.Zero, IntPtr.Zero, flags);

            if (devInfoSet == SetupApi.INVALID_HANDLE_VALUE)
                return Array.Empty<HardwareDeviceInfo>();

            try
            {
                return EnumerateFromSet(devInfoSet);
            }
            finally
            {
                SetupApi.SetupDiDestroyDeviceInfoList(devInfoSet);
            }
        }

        private static IReadOnlyList<HardwareDeviceInfo> EnumerateFromSet(IntPtr devInfoSet)
        {
            var devices = new List<HardwareDeviceInfo>();
            var devInfoData = new SetupApi.SP_DEVINFO_DATA();
            devInfoData.cbSize = Marshal.SizeOf(devInfoData);

            int index = 0;
            while (SetupApi.SetupDiEnumDeviceInfo(devInfoSet, index, ref devInfoData))
            {
                try
                {
                    var device = DeviceInfoParser.Parse(devInfoSet, ref devInfoData);

                    // Parse USB-specific info if applicable
                    if (UsbDeviceParser.IsUsbDevice(device.DeviceId))
                    {
                        UsbDeviceParser.TryParseVidPid(device.DeviceId, out int vid, out int pid);
                        UsbSpeed speed = UsbDeviceParser.ParseSpeed(device.BusReportedDeviceDesc);
                        int maxPower = UsbDeviceParser.ParseMaxPower(device.BusReportedDeviceDesc);

                        device = new HardwareDeviceInfo
                        {
                            DeviceId = device.DeviceId,
                            DeviceName = device.DeviceName,
                            Manufacturer = device.Manufacturer,
                            ClassGuid = device.ClassGuid,
                            ClassName = device.ClassName,
                            LocationInfo = device.LocationInfo,
                            EnumeratorName = device.EnumeratorName,
                            DeviceDescription = device.DeviceDescription,
                            HardwareId = device.HardwareId,
                            BusReportedDeviceDesc = device.BusReportedDeviceDesc,
                            VendorId = vid,
                            ProductId = pid,
                            SerialNumber = UsbDeviceParser.ParseSerialNumber(device.DeviceId) ?? string.Empty,
                            FirmwareRevision = device.FirmwareRevision,
                            HardwareRevision = device.HardwareRevision,
                            Speed = speed,
                            UsbVersion = UsbDeviceParser.GetUsbVersionString(speed),
                            MaxPowerMilliamps = maxPower,
                            DeviceClass = device.DeviceClass,
                            DeviceSubClass = device.DeviceSubClass,
                            DeviceProtocol = device.DeviceProtocol,
                            IsConnected = device.IsConnected,
                            IsRemovable = device.IsRemovable,
                        };
                    }

                    devices.Add(device);
                }
                catch
                {
                    // Skip inaccessible devices
                }

                index++;
            }

            return devices;
        }
    }
}
