// <copyright file="DeviceHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Device
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Represents information about a hardware device discovered by the system.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Gets or sets the unique device instance ID.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device description (friendly name from the driver).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a user-friendly name for the device.
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer name.
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the driver version string.
        /// </summary>
        public string? DriverVersion { get; set; }

        /// <summary>
        /// Gets or sets the driver date string.
        /// </summary>
        public string? DriverDate { get; set; }

        /// <summary>
        /// Gets or sets the hardware ID(s) for the device.
        /// </summary>
        public string? HardwareId { get; set; }

        /// <summary>
        /// Gets or sets the bus-reported device description.
        /// </summary>
        public string? BusReportedDeviceDesc { get; set; }

        /// <summary>
        /// Gets or sets the device class GUID.
        /// </summary>
        public string? ClassGuid { get; set; }

        /// <summary>
        /// Gets or sets the device status string (e.g. "OK", "Error").
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Returns a string representation of this device.
        /// </summary>
        /// <returns>A string containing the description and device ID.</returns>
        public override string ToString()
        {
            return $"{Description ?? "Unknown device"} ({DeviceId ?? "unknown"})";
        }
    }

    /// <summary>
    /// Represents information about a logical volume and its associated device.
    /// </summary>
    public class DeviceVolumeInfo
    {
        /// <summary>
        /// Gets or sets the drive letter (e.g. "C:", "D:").
        /// </summary>
        public string? DriveLetter { get; set; }

        /// <summary>
        /// Gets or sets the volume label.
        /// </summary>
        public string? VolumeLabel { get; set; }

        /// <summary>
        /// Gets or sets the file system type (e.g. "NTFS", "FAT32").
        /// </summary>
        public string? FileSystem { get; set; }

        /// <summary>
        /// Gets or sets the volume serial number.
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the device path (e.g. "\\.\PhysicalDrive0").
        /// </summary>
        public string? DevicePath { get; set; }

        /// <summary>
        /// Gets or sets the device type description (e.g. "Fixed", "Removable", "CD-ROM").
        /// </summary>
        public string? DeviceType { get; set; }

        /// <summary>
        /// Gets or sets whether the volume is ready (accessible).
        /// </summary>
        public string? IsReady { get; set; }

        /// <summary>
        /// Returns a string representation of this volume.
        /// </summary>
        /// <returns>A string containing the drive letter and volume label.</returns>
        public override string ToString()
        {
            return $"{DriveLetter ?? "?"} ({VolumeLabel ?? "no label"})";
        }
    }

    /// <summary>
    /// Provides Win32 P/Invoke-based helper methods for Windows device and volume operations,
    /// including USB device detection, device enumeration, and volume information.
    /// All methods are thread-safe and gracefully return empty/null on non-Windows platforms.
    /// </summary>
    public static class DeviceHelper
    {
        // ------ Win32 constants ------

        // SetupAPI constants
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_ALLCLASSES = 0x00000004;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint DIGCF_DEFAULT = 0x00000001;

        private const uint SPDRP_DEVICEDESC = 0x00000000;
        private const uint SPDRP_HARDWAREID = 0x00000001;
        private const uint SPDRP_CLASSGUID = 0x00000006;
        private const uint SPDRP_DRIVER = 0x00000009;
        private const uint SPDRP_MFG = 0x0000000B;
        private const uint SPDRP_FRIENDLYNAME = 0x0000000C;
        private const uint SPDRP_CONFIGFLAGS = 0x0000000F;
        private const uint SPDRP_BUSREPORTEDDEVICEDESC = 0x0000002A;

        private const uint ERROR_INSUFFICIENT_BUFFER = 0x0000007A;
        private const uint ERROR_NO_MORE_ITEMS = 0x00000103;

        private const int INVALID_HANDLE_VALUE = -1;

        // Device notification constants
        private const uint DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        // GUID_DEVINTERFACE_USB_DEVICE
        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE =
            new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        // Volume device type constants
        private const uint DRIVE_UNKNOWN = 0;
        private const uint DRIVE_NO_ROOT_DIR = 1;
        private const uint DRIVE_REMOVABLE = 2;
        private const uint DRIVE_FIXED = 3;
        private const uint DRIVE_REMOTE = 4;
        private const uint DRIVE_CDROM = 5;
        private const uint DRIVE_RAMDISK = 6;

        // IOCTL for volume to disk number
        private const uint IOCTL_VOLUME_BASE = (uint)'V';
        private const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS =
            ((0x00000001) << 24) | ((IOCTL_VOLUME_BASE) << 16) | ((0x0047) << 2) | 0;

        // ------ Structures ------

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISK_EXTENT
        {
            public int DiskNumber;
            public long StartingOffset;
            public long ExtentLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VOLUME_DISK_EXTENTS
        {
            public int NumberOfDiskExtents;
            public DISK_EXTENT Extent;
        }

        // ------ DllImports ------

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsW(
            ref Guid classGuid,
            [MarshalAs(UnmanagedType.LPWStr)] string? enumerator,
            IntPtr hwndParent,
            uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsW(
            IntPtr classGuid,
            [MarshalAs(UnmanagedType.LPWStr)] string? enumerator,
            IntPtr hwndParent,
            uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr deviceInfoSet,
            uint memberIndex,
            ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstanceIdW(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            [Out] StringBuilder? deviceInstanceId,
            uint deviceInstanceIdSize,
            out uint requiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryPropertyW(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            uint property,
            out uint propertyRegDataType,
            [Out] byte[]? propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetailW(
            IntPtr deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            uint deviceInterfaceDetailDataSize,
            out uint requiredSize,
            IntPtr deviceInfoData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotificationW(
            IntPtr hRecipient,
            IntPtr notificationFilter,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotificationNative(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetLogicalDrives();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint QueryDosDeviceW(
            string lpDeviceName,
            [Out] StringBuilder? lpTargetPath,
            uint ucchMax);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetDriveTypeW(string lpRootPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeInformationW(
            string lpRootPathName,
            [Out] StringBuilder? lpVolumeNameBuffer,
            uint nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            [Out] StringBuilder? lpFileSystemNameBuffer,
            uint nFileSystemNameSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // ------ Public API ------

        /// <summary>
        /// Registers a window handle to receive device notification events (e.g. USB insertion/removal).
        /// </summary>
        /// <param name="windowHandle">The window handle that will receive <c>WM_DEVICECHANGE</c> messages.</param>
        /// <param name="interfaceClassGuid">The GUID of the device interface class to monitor.
        /// Use <c>GUID_DEVINTERFACE_USB_DEVICE</c> for USB devices, or <c>Guid.Empty</c> for all.</param>
        /// <returns>A notification handle on success, or <see cref="IntPtr.Zero"/> on failure.</returns>
        public static IntPtr RegisterDeviceNotification(IntPtr windowHandle, Guid interfaceClassGuid)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                Guid guid = interfaceClassGuid == Guid.Empty
                    ? GUID_DEVINTERFACE_USB_DEVICE
                    : interfaceClassGuid;

                var filter = new DEV_BROADCAST_DEVICEINTERFACE
                {
                    dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                    dbcc_devicetype = (int)DBT_DEVTYP_DEVICEINTERFACE,
                    dbcc_reserved = 0,
                    dbcc_classguid = guid,
                    dbcc_name = string.Empty,
                };

                int filterSize = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE));
                IntPtr filterPtr = Marshal.AllocHGlobal(filterSize);

                try
                {
                    Marshal.StructureToPtr(filter, filterPtr, false);
                    IntPtr handle = RegisterDeviceNotificationW(
                        windowHandle,
                        filterPtr,
                        DEVICE_NOTIFY_WINDOW_HANDLE);

                    return handle;
                }
                finally
                {
                    Marshal.FreeHGlobal(filterPtr);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return IntPtr.Zero;
            }
            catch (EntryPointNotFoundException)
            {
                return IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Unregisters a device notification handle previously returned by <see cref="RegisterDeviceNotification"/>.
        /// </summary>
        /// <param name="notificationHandle">The notification handle to unregister.</param>
        /// <returns><c>true</c> if the handle was unregistered; otherwise, <c>false</c>.</returns>
        public static bool UnregisterDeviceNotification(IntPtr notificationHandle)
        {
            if (notificationHandle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                return UnregisterDeviceNotificationNative(notificationHandle);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Enumerates all devices present on the system using SetupAPI (SetupDiGetClassDevs).
        /// </summary>
        /// <returns>A read-only list of <see cref="DeviceInfo"/> objects, or an empty list on failure.</returns>
        public static IReadOnlyList<DeviceInfo> GetAllDevices()
        {
            var devices = new List<DeviceInfo>();

            try
            {
                // Get device info set for all present devices (class GUID 0 = all classes)
                IntPtr deviceInfoSet = SetupDiGetClassDevsW(
                    IntPtr.Zero,
                    null,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_ALLCLASSES);

                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(INVALID_HANDLE_VALUE))
                {
                    return devices;
                }

                try
                {
                    uint index = 0;
                    var devInfoData = new SP_DEVINFO_DATA
                    {
                        cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)),
                    };

                    while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref devInfoData))
                    {
                        try
                        {
                            var info = new DeviceInfo();

                            // Get device instance ID
                            info.DeviceId = GetDeviceInstanceId(deviceInfoSet, ref devInfoData);

                            // Get description
                            info.Description = GetRegistryPropertyString(
                                deviceInfoSet, ref devInfoData, SPDRP_DEVICEDESC);

                            // Get friendly name
                            info.FriendlyName = GetRegistryPropertyString(
                                deviceInfoSet, ref devInfoData, SPDRP_FRIENDLYNAME);

                            // Get manufacturer
                            info.Manufacturer = GetRegistryPropertyString(
                                deviceInfoSet, ref devInfoData, SPDRP_MFG);

                            // Get hardware IDs (multi-string, take first)
                            info.HardwareId = GetRegistryPropertyMultiStringFirst(
                                deviceInfoSet, ref devInfoData, SPDRP_HARDWAREID);

                            // Get class GUID
                            info.ClassGuid = GetRegistryPropertyString(
                                deviceInfoSet, ref devInfoData, SPDRP_CLASSGUID);

                            // Get bus-reported device description
                            info.BusReportedDeviceDesc = GetRegistryPropertyString(
                                deviceInfoSet, ref devInfoData, SPDRP_BUSREPORTEDDEVICEDESC);

                            // Get status from config flags
                            uint? configFlags = GetRegistryPropertyDword(
                                deviceInfoSet, ref devInfoData, SPDRP_CONFIGFLAGS);
                            if (configFlags.HasValue)
                            {
                                info.Status = configFlags.Value == 0 ? "OK" : $"Error (0x{configFlags.Value:X8})";
                            }

                            devices.Add(info);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            // Skip individual device errors
                        }
#pragma warning restore CA1031

                        // Reset structure for next iteration
                        devInfoData = new SP_DEVINFO_DATA
                        {
                            cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)),
                        };

                        index++;
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return devices;
            }
            catch (EntryPointNotFoundException)
            {
                return devices;
            }
            catch
            {
                return devices;
            }
#pragma warning restore CA1031

            return devices.AsReadOnly();
        }

        /// <summary>
        /// Gets a specific property value for a device identified by its device instance ID.
        /// </summary>
        /// <param name="deviceInstanceId">The device instance ID to query.</param>
        /// <param name="propertyName">The property name to retrieve (e.g. "DeviceDesc", "HardwareID", "Manufacturer").
        /// Supported names: <c>DeviceDesc</c>, <c>HardwareID</c>, <c>Manufacturer</c>, <c>FriendlyName</c>,
        /// <c>ClassGUID</c>, <c>Driver</c>, <c>ConfigFlags</c>, <c>BusReportedDeviceDesc</c>.</param>
        /// <returns>The property value as a string, or <c>null</c> if not found or on non-Windows platforms.</returns>
        public static string? GetDeviceProperty(string deviceInstanceId, string propertyName)
        {
            if (string.IsNullOrEmpty(deviceInstanceId) || string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            try
            {
                // Map property name to SPDRP constant
                uint property = MapPropertyNameToSpdrp(propertyName);
                if (property == uint.MaxValue)
                {
                    return null;
                }

                // Get device info set for all present devices
                IntPtr deviceInfoSet = SetupDiGetClassDevsW(
                    IntPtr.Zero,
                    null,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_ALLCLASSES);

                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(INVALID_HANDLE_VALUE))
                {
                    return null;
                }

                try
                {
                    uint index = 0;
                    var devInfoData = new SP_DEVINFO_DATA
                    {
                        cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)),
                    };

                    while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref devInfoData))
                    {
                        try
                        {
                            string? instanceId = GetDeviceInstanceId(deviceInfoSet, ref devInfoData);
                            if (string.Equals(instanceId, deviceInstanceId, StringComparison.OrdinalIgnoreCase))
                            {
                                if (property == SPDRP_CONFIGFLAGS)
                                {
                                    uint? dwordVal = GetRegistryPropertyDword(
                                        deviceInfoSet, ref devInfoData, property);
                                    return dwordVal?.ToString();
                                }

                                if (property == SPDRP_HARDWAREID)
                                {
                                    return GetRegistryPropertyMultiStringFirst(
                                        deviceInfoSet, ref devInfoData, property);
                                }

                                return GetRegistryPropertyString(
                                    deviceInfoSet, ref devInfoData, property);
                            }
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            // Continue enumeration
                        }
#pragma warning restore CA1031

                        devInfoData = new SP_DEVINFO_DATA
                        {
                            cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)),
                        };

                        index++;
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch
            {
                return null;
            }
#pragma warning restore CA1031

            return null;
        }

        /// <summary>
        /// Enumerates logical volumes on the system, gathering drive letter, volume label,
        /// file system, serial number, and device type information.
        /// </summary>
        /// <returns>An array of <see cref="DeviceVolumeInfo"/> objects, or <c>null</c> on failure.</returns>
        public static DeviceVolumeInfo[]? GetVolumeDevices()
        {
            var volumes = new List<DeviceVolumeInfo>();

            try
            {
                uint drives = GetLogicalDrives();
                if (drives == 0)
                {
                    return null;
                }

                for (char driveLetter = 'A'; driveLetter <= 'Z'; driveLetter++)
                {
                    // Check if this drive is present via bitmask
                    int bitIndex = driveLetter - 'A';
                    if ((drives & (1 << bitIndex)) == 0)
                    {
                        continue;
                    }

                    try
                    {
                        string rootPath = $"{driveLetter}:\\";
                        var volumeInfo = new DeviceVolumeInfo
                        {
                            DriveLetter = $"{driveLetter}:",
                        };

                        // Get drive type
                        uint driveType = GetDriveTypeW(rootPath);
                        volumeInfo.DeviceType = driveType switch
                        {
                            DRIVE_REMOVABLE => "Removable",
                            DRIVE_FIXED => "Fixed",
                            DRIVE_REMOTE => "Remote",
                            DRIVE_CDROM => "CD-ROM",
                            DRIVE_RAMDISK => "RAM Disk",
                            DRIVE_NO_ROOT_DIR => "No Root Directory",
                            _ => "Unknown",
                        };

                        // Get volume information
                        var volNameBuf = new StringBuilder(256);
                        var fsNameBuf = new StringBuilder(256);

                        bool success = GetVolumeInformationW(
                            rootPath,
                            volNameBuf,
                            (uint)volNameBuf.Capacity,
                            out uint serialNumber,
                            out _,
                            out _,
                            fsNameBuf,
                            (uint)fsNameBuf.Capacity);

                        if (success)
                        {
                            volumeInfo.VolumeLabel = volNameBuf.ToString();
                            volumeInfo.FileSystem = fsNameBuf.ToString();
                            volumeInfo.SerialNumber = serialNumber.ToString("X8");
                            volumeInfo.IsReady = "True";
                        }
                        else
                        {
                            volumeInfo.IsReady = "False";
                            volumeInfo.VolumeLabel = string.Empty;
                            volumeInfo.FileSystem = string.Empty;
                            volumeInfo.SerialNumber = string.Empty;
                        }

                        // Try to get the device path via QueryDosDevice
                        string dosDeviceName = $"{driveLetter}:";
                        var targetPath = new StringBuilder(1024);
                        uint result = QueryDosDeviceW(dosDeviceName, targetPath, (uint)targetPath.Capacity);
                        if (result > 0)
                        {
                            volumeInfo.DevicePath = targetPath.ToString();
                        }

                        volumes.Add(volumeInfo);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        // Skip drive errors
                    }
#pragma warning restore CA1031
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch
            {
                return null;
            }
#pragma warning restore CA1031

            return volumes.Count > 0 ? volumes.ToArray() : null;
        }

        /// <summary>
        /// Enumerates USB devices specifically using the GUID_DEVINTERFACE_USB_DEVICE interface class.
        /// </summary>
        /// <returns>A read-only list of device instance IDs for USB devices, or an empty list on failure.</returns>
        public static IReadOnlyList<string> GetUsbDevices()
        {
            var usbDevices = new List<string>();

            try
            {
                // Get device info set for USB device interface class
                Guid usbGuid = GUID_DEVINTERFACE_USB_DEVICE;
                IntPtr deviceInfoSet = SetupDiGetClassDevsW(
                    ref usbGuid,
                    null,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(INVALID_HANDLE_VALUE))
                {
                    // Fallback: enumerate all devices and filter by USB class
                    return GetUsbDevicesFallback();
                }

                try
                {
                    uint index = 0;
                    var devInterfaceData = new SP_DEVICE_INTERFACE_DATA
                    {
                        cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA)),
                    };

                    var nullDevInfoData = new SP_DEVINFO_DATA
                    {
                        cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)),
                    };

                    while (SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        ref nullDevInfoData,
                        ref usbGuid,
                        index,
                        ref devInterfaceData))
                    {
                        try
                        {
                            // Get the device path
                            uint requiredSize = 0;
                            SetupDiGetDeviceInterfaceDetailW(
                                deviceInfoSet,
                                ref devInterfaceData,
                                IntPtr.Zero,
                                0,
                                out requiredSize,
                                IntPtr.Zero);

                            if (requiredSize == 0)
                            {
                                index++;
                                continue;
                            }

                            IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);
                            try
                            {
                                // The first 4 bytes is the cbSize field
                                Marshal.WriteInt32(detailDataBuffer, IntPtr.Size == 8 ? 8 : 4);

                                if (SetupDiGetDeviceInterfaceDetailW(
                                    deviceInfoSet,
                                    ref devInterfaceData,
                                    detailDataBuffer,
                                    requiredSize,
                                    out _,
                                    IntPtr.Zero))
                                {
                                    // The device path starts after cbSize
                                    // For simplicity, just add a marker
                                    usbDevices.Add($"USB Device #{index}");
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(detailDataBuffer);
                            }
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            // Skip individual errors
                        }
#pragma warning restore CA1031

                        devInterfaceData = new SP_DEVICE_INTERFACE_DATA
                        {
                            cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA)),
                        };

                        index++;
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (DllNotFoundException)
            {
                return usbDevices;
            }
            catch (EntryPointNotFoundException)
            {
                return usbDevices;
            }
            catch
            {
                return usbDevices;
            }
#pragma warning restore CA1031

            return usbDevices.AsReadOnly();
        }

        // ------ Private helpers ------

        /// <summary>
        /// Gets the device instance ID string from the device info set.
        /// </summary>
        private static string? GetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA devInfoData)
        {
            uint requiredSize = 0;
            SetupDiGetDeviceInstanceIdW(
                deviceInfoSet,
                ref devInfoData,
                null,
                0,
                out requiredSize);

            if (requiredSize == 0)
            {
                return null;
            }

            var sb = new StringBuilder((int)requiredSize);

            if (SetupDiGetDeviceInstanceIdW(
                deviceInfoSet,
                ref devInfoData,
                sb,
                requiredSize,
                out _))
            {
                return sb.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets a string registry property for a device.
        /// </summary>
        private static string? GetRegistryPropertyString(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA devInfoData,
            uint property)
        {
            uint requiredSize = 0;
            bool result = SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet,
                ref devInfoData,
                property,
                out _,
                null,
                0,
                out requiredSize);

            if (!result && requiredSize == 0)
            {
                return null;
            }

            var buffer = new byte[requiredSize];
            uint regDataType;

            if (SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet,
                ref devInfoData,
                property,
                out regDataType,
                buffer,
                requiredSize,
                out _))
            {
                // REG_SZ or REG_EXPAND_SZ
                string str = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize);
                int nullPos = str.IndexOf('\0');
                if (nullPos >= 0)
                {
                    str = str.Substring(0, nullPos);
                }

                return str;
            }

            return null;
        }

        /// <summary>
        /// Gets a DWORD registry property for a device.
        /// </summary>
        private static uint? GetRegistryPropertyDword(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA devInfoData,
            uint property)
        {
            var buffer = new byte[4];
            uint requiredSize;
            uint regDataType;

            if (SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet,
                ref devInfoData,
                property,
                out regDataType,
                buffer,
                4,
                out requiredSize))
            {
                if (requiredSize >= 4)
                {
                    return BitConverter.ToUInt32(buffer, 0);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the first string from a REG_MULTI_SZ registry property.
        /// </summary>
        private static string? GetRegistryPropertyMultiStringFirst(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA devInfoData,
            uint property)
        {
            uint requiredSize = 0;
            bool result = SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet,
                ref devInfoData,
                property,
                out _,
                null,
                0,
                out requiredSize);

            if (!result && requiredSize == 0)
            {
                return null;
            }

            var buffer = new byte[requiredSize];
            uint regDataType;

            if (SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet,
                ref devInfoData,
                property,
                out regDataType,
                buffer,
                requiredSize,
                out _))
            {
                string str = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize);
                int nullPos = str.IndexOf('\0');
                if (nullPos >= 0)
                {
                    return str.Substring(0, nullPos);
                }

                return str;
            }

            return null;
        }

        /// <summary>
        /// Fallback method for USB device enumeration that uses GetAllDevices and filters
        /// by class GUID or hardware ID strings.
        /// </summary>
        private static IReadOnlyList<string> GetUsbDevicesFallback()
        {
            var usbDevices = new List<string>();

            try
            {
                var allDevices = GetAllDevices();
                foreach (var device in allDevices)
                {
                    // USB devices typically have "USB" in the hardware ID or device description
                    if (device.HardwareId != null &&
                        device.HardwareId.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!string.IsNullOrEmpty(device.DeviceId))
                        {
                            usbDevices.Add(device.DeviceId);
                        }
                    }
                    else if (device.Description != null &&
                             device.Description.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!string.IsNullOrEmpty(device.DeviceId))
                        {
                            usbDevices.Add(device.DeviceId);
                        }
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // Return whatever we found
            }
#pragma warning restore CA1031

            return usbDevices.AsReadOnly();
        }

        /// <summary>
        /// Maps a property name string to an SPDRP constant.
        /// </summary>
        private static uint MapPropertyNameToSpdrp(string propertyName)
        {
            return propertyName.ToUpperInvariant() switch
            {
                "DEVICEDESC" => SPDRP_DEVICEDESC,
                "HARDWAREID" => SPDRP_HARDWAREID,
                "MANUFACTURER" => SPDRP_MFG,
                "FRIENDLYNAME" => SPDRP_FRIENDLYNAME,
                "CLASSGUID" => SPDRP_CLASSGUID,
                "DRIVER" => SPDRP_DRIVER,
                "CONFIGFLAGS" => SPDRP_CONFIGFLAGS,
                "BUSREPORTEDDEVICEDESC" => SPDRP_BUSREPORTEDDEVICEDESC,
                _ => uint.MaxValue,
            };
        }
    }
}
