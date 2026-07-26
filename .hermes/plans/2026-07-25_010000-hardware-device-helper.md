# HardwareDeviceHelper Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add a `HardwareDeviceHelper` to BPlusLib.Foundation that enumerates Windows hardware devices (USB storage, USB HID, COM ports, etc.) and returns detailed information using pure P/Invoke — no WMI, no PowerShell.

**Architecture:** Use SetupAPI (SetupDiGetClassDevs, SetupDiEnumDeviceInterfaces) for device enumeration, combined with DeviceIoControl for USB-specific details. Follow existing project patterns (static helper, null-safe, thread-safe, XML docs).

**Tech Stack:** C# P/Invoke, SetupAPI.dll, Kernel32.dll, cfgmgr32.dll, .NET Framework 4.7+, .NET 6, .NET 8

---

## API Design

### Public Model — HardwareDeviceInfo

```csharp
public sealed class HardwareDeviceInfo
{
    // ── Basic Device Info ─────────────────────────────────────────────
    public string DeviceId { get; init; }           // Device instance ID
    public string DeviceName { get; init; }         // Friendly name
    public string Manufacturer { get; init; }       // Manufacturer
    public string ClassGuid { get; init; }          // Device class GUID
    public string ClassName { get; init; }          // Device class name
    public string LocationInfo { get; init; }       // Location (e.g., "Port_#0001")
    public string EnumeratorName { get; init; }     // Bus enumerator (e.g., "USB")
    public string DeviceDescription { get; init; }  // Device description
    public bool IsConnected { get; init; }          // Device present
    public bool IsRemovable { get; init; }          // Removable device
    public DateTime? InstallDate { get; init; }     // First install date

    // ── USB-Specific Info ─────────────────────────────────────────────
    public int VendorId { get; init; }              // USB VID (0 if not USB)
    public int ProductId { get; init; }             // USB PID (0 if not USB)
    public string SerialNumber { get; init; }       // USB serial number
    public string FirmwareRevision { get; init; }   // USB firmware revision
    public string HardwareRevision { get; init; }   // USB hardware revision
    public UsbSpeed Speed { get; init; }            // USB speed (2.0, 3.0, 3.1...)
    public string UsbVersion { get; init; }         // "2.0", "3.0", "3.1", "3.2"
    public int MaxPowerMilliamps { get; init; }     // Max power in mA
    public string DeviceClass { get; init; }        // USB class (MassStorage, HID, etc.)
    public string DeviceSubClass { get; init; }     // USB subclass
    public string DeviceProtocol { get; init; }     // USB protocol
    public bool IsSelfPowered { get; init; }        // Self-powered device
    public bool IsRemoteWakeCapable { get; init; }  // Can wake the host
}
```

### USB Speed Enum

```csharp
/// <summary>
/// USB connection speed supported by the device.
/// </summary>
public enum UsbSpeed
{
    Unknown = 0,
    LowSpeed = 1,       // 1.5 Mbps (USB 1.0)
    FullSpeed = 2,      // 12 Mbps (USB 1.1)
    HighSpeed = 3,      // 480 Mbps (USB 2.0)
    SuperSpeed = 4,     // 5 Gbps (USB 3.0/3.1 Gen 1)
    SuperSpeedPlus = 5, // 10 Gbps (USB 3.1 Gen 2/3.2 Gen 2)
    // Convenience aliases
    Usb10 = LowSpeed,
    Usb11 = FullSpeed,
    Usb20 = HighSpeed,
    Usb30 = SuperSpeed,
    Usb31 = SuperSpeed,
    Usb32 = SuperSpeedPlus,
}
```

### Public Methods

```csharp
public static class HardwareDeviceHelper
{
    // Enumerate all devices
    public static IReadOnlyList<HardwareDeviceInfo> GetAllDevices();
    
    // Enumerate by class (e.g., USB, HID, Disk)
    public static IReadOnlyList<HardwareDeviceInfo> GetDevicesByClass(Guid classGuid);
    
    // Get specific device by instance ID
    public static HardwareDeviceInfo? GetDeviceById(string deviceId);
    
    // Convenience: Get all USB devices
    public static IReadOnlyList<HardwareDeviceInfo> GetUsbDevices();
    
    // Convenience: Get USB storage devices with detailed info
    public static IReadOnlyList<HardwareDeviceInfo> GetUsbStorageDevices();
    
    // Convenience: Get USB HID devices
    public static IReadOnlyList<HardwareDeviceInfo> GetUsbHidDevices();
    
    // Convenience: Get COM/serial ports
    public static IReadOnlyList<HardwareDeviceInfo> GetComPorts();
    
    // Convenience: Get disk drives
    public static IReadOnlyList<HardwareDeviceInfo> GetDiskDrives();
    
    // Convenience: Get network adapters
    public static IReadOnlyList<HardwareDeviceInfo> GetNetworkAdapters();
    
    // Check if device is present
    public static bool IsDevicePresent(string deviceId);
    
    // Get device registry property
    public static string? GetDeviceProperty(string deviceId, int property);
}
```

---

## File Structure

### New Files

| File | Purpose |
|------|---------|
| `src/Hardware/HardwareDeviceHelper.cs` | Main public API |
| `src/Hardware/DeviceInfoParser.cs` | Parse device info from registry |
| `src/Native/SetupApi.cs` | P/Invoke declarations for SetupAPI |
| `src/Hardware/UsbDeviceParser.cs` | Parse USB-specific info (VID/PID/Serial) |
| `tests/Hardware/HardwareDeviceHelperTests.cs` | Unit tests |

### Modified Files

| File | Change |
|------|--------|
| `src/BPlusLib.Foundation.csproj` | No change needed |

---

## Task Breakdown

### Task 1: Create SetupApi P/Invoke declarations

**Objective:** Declare all required SetupAPI P/Invoke structures and methods.

**Files:**
- Create: `src/Native/SetupApi.cs`

**Step 1: Write SetupApi.cs**

```csharp
// <copyright file="SetupApi.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for SetupAPI.dll — Windows device installation functions.
    /// </summary>
    internal static class SetupApi
    {
        // DIGCF flags
        internal const int DIGCF_ALLCLASSES = 0x00000004;
        internal const int DIGCF_PRESENT = 0x00000002;
        internal const int DIGCF_DEVICEINTERFACE = 0x00000010;

        // SPDRP flags (SetupDiGetDeviceRegistryProperty)
        internal const int SPDRP_DEVICEDESC = 0x00000000;
        internal const int SPDRP_HARDWAREID = 0x00000001;
        internal const int SPDRP_FRIENDLYNAME = 0x0000000C;
        internal const int SPDRP_MFG = 0x0000000B;
        internal const int SPDRP_CLASS = 0x00000007;
        internal const int SPDRP_CLASSGUID = 0x00000008;
        internal const int SPDRP_DRIVER = 0x00000009;
        internal const int SPDRP_CONFIGFLAGS = 0x0000000A;
        internal const int SPDRP_LOCATION_INFORMATION = 0x0000000D;
        internal const int SPDRP_ENUMERATOR_NAME = 0x0000001A;
        internal const int SPDRP_INSTALL_DATE = 0x00000012;
        internal const int SPDRP_BUSREPORTEDDEVDESC = 0x00000029; // Bus-reported device descriptor

        // DI_FLAGS
        internal const int DIGCF_DEFAULT = 0x00000001;
        internal const int DIGCF_PRESENT = 0x00000002;
        internal const int DIGCF_ALLCLASSES = 0x00000004;
        internal const int DIGCF_PROFILE = 0x00000008;
        internal const int DIGCF_DEVICEINTERFACE = 0x00000010;

        // INVALID_HANDLE_VALUE
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>Retrieves a device information set.</summary>
        [DllImport("newdev.dll", SetLastError = true)]
        internal static extern bool DiShowUpdateDriver(
            IntPtr hwndParent,
            string INFPath,
            string? SourceMediaPath,
            uint Flags,
            out bool NeedReboot);

        /// <summary>Retrieves a device information set for all devices of a class.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            int Flags);

        /// <summary>Retrieves a device information set for all devices.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            int Flags);

        /// <summary>Retrieves a device information element.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            int MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        /// <summary>Retrieves a device registry property.</summary>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            int Property,
            out int PropertyRegDataType,
            IntPtr PropertyBuffer,
            int PropertyBufferSize,
            out int RequiredSize);

        /// <summary>Frees a device information set.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        /// <summary>Device information set element.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        /// <summary>Device interface data.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        /// <summary>Device interface detail data.</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        /// <summary>SetupDiGetDeviceInterfaceDetail.</summary>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            out int RequiredSize,
            ref SP_DEVINFO_DATA DeviceInfoData);
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 2: Create DeviceInfoParser helper

**Objective:** Parse device properties from SetupAPI into HardwareDeviceInfo model.

**Files:**
- Create: `src/Hardware/DeviceInfoParser.cs`

**Step 1: Write DeviceInfoParser.cs**

```csharp
// <copyright file="DeviceInfoParser.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Hardware
{
    /// <summary>
    /// Parses device information from SetupAPI handles into HardwareDeviceInfo objects.
    /// </summary>
    internal static class DeviceInfoParser
    {
        /// <summary>
        /// Extracts a string registry property from a device info element.
        /// </summary>
        internal static string GetStringProperty(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo, int property)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceRegistryProperty(
                    devInfoSet, ref devInfo, property, out _, IntPtr.Zero, 0, out requiredSize);

                if (requiredSize <= 0) return string.Empty;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    if (SetupApi.SetupDiGetDeviceRegistryProperty(
                        devInfoSet, ref devInfo, property, out _, buffer, requiredSize, out _))
                    {
                        return Marshal.PtrToStringUni(buffer) ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Extracts an integer registry property from a device info element.
        /// </summary>
        internal static int GetIntProperty(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo, int property)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceRegistryProperty(
                    devInfoSet, ref devInfo, property, out _, IntPtr.Zero, 0, out requiredSize);

                if (requiredSize <= 0) return 0;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    if (SetupApi.SetupDiGetDeviceRegistryProperty(
                        devInfoSet, ref devInfo, property, out _, buffer, requiredSize, out _))
                    {
                        return Marshal.ReadInt32(buffer);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Parses a HardwareDeviceInfo from a device info element.
        /// </summary>
        internal static HardwareDeviceInfo Parse(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo)
        {
            string deviceId = GetStringProperty(devInfoSet, ref devInfo, 0); // SPDRP_DEVICEDESC as fallback
            string friendlyName = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_FRIENDLYNAME);
            string manufacturer = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_MFG);
            string className = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_CLASS);
            string classGuid = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_CLASSGUID);
            string locationInfo = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_LOCATION_INFORMATION);
            string enumeratorName = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_ENUMERATOR_NAME);
            string description = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_DEVICEDESC);
            int configFlags = GetIntProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_CONFIGFLAGS);

            // Get device instance ID
            string deviceInstanceId = GetDeviceInstanceId(devInfoSet, ref devInfo);

            return new HardwareDeviceInfo
            {
                DeviceId = deviceInstanceId,
                DeviceName = friendlyName,
                Manufacturer = manufacturer,
                ClassGuid = classGuid,
                ClassName = className,
                LocationInfo = locationInfo,
                EnumeratorName = enumeratorName,
                DeviceDescription = description,
                IsConnected = true,
                IsRemovable = (configFlags & 0x10) != 0, // CONFIGFLAG_REMOVABLE
            };
        }

        private static string GetDeviceInstanceId(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceInstanceId(
                    devInfoSet, ref devInfo, null, 0, out requiredSize);

                if (requiredSize <= 0) return string.Empty;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize * 2);
                try
                {
                    if (SetupApi.SetupDiGetDeviceInstanceId(
                        devInfoSet, ref devInfo, buffer, requiredSize, out _))
                    {
                        return Marshal.PtrToStringUni(buffer) ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 3: Create UsbDeviceParser helper

**Objective:** Parse USB-specific information (VID/PID/Serial/Speed) from device instance ID and registry.

**Files:**
- Create: `src/Hardware/UsbDeviceParser.cs`

**Step 1: Write UsbDeviceParser.cs**

```csharp
// <copyright file="UsbDeviceParser.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Hardware
{
    /// <summary>
    /// Parses USB-specific information from device instance IDs and registry properties.
    /// USB device IDs follow the pattern: USB\VID_XXXX&PID_XXXX\SerialNumber
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

        // DEVPKEY_Device_BusReportedDeviceDesc — contains USB descriptor info
        // Format: "USB x.xx, Speed = High speed, ..."
        private static readonly Regex UsbSpeedRegex = new Regex(
            @"Speed\s*=\s*(?<speed>Low|Full|High|Super(?:Speed\+?)?)\s*speed",
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
        /// The string typically looks like: "USB x.xx, Speed = High speed"
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
        /// Format: "MaxPower = xxxmA" or "MaxPower = xxx"
        /// </summary>
        internal static int ParseMaxPower(string busReportedDesc)
        {
            if (string.IsNullOrEmpty(busReportedDesc)) return 0;

            var match = Regex.Match(busReportedDesc, @"MaxPower\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int ma))
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
        /// Parses USB version from hardware ID string.
        /// Hardware IDs look like: USB\VID_1234&PID_5678&REV_0100
        /// or: USB\VID_1234&PID_5678&MI_00
        /// </summary>
        internal static string ParseHardwareId(string hardwareId)
        {
            if (string.IsNullOrEmpty(hardwareId)) return string.Empty;

            // Extract revision as proxy for USB version
            var match = Regex.Match(hardwareId, @"REV_(?<rev>[0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string rev = match.Groups["rev"].Value;
                // Rough mapping: 01xx = USB 1.x, 02xx = USB 2.0, 03xx = USB 3.x
                if (rev.StartsWith("03", StringComparison.OrdinalIgnoreCase))
                    return "3.x";
                if (rev.StartsWith("02", StringComparison.OrdinalIgnoreCase))
                    return "2.0";
                if (rev.StartsWith("01", StringComparison.OrdinalIgnoreCase))
                    return "1.x";
            }

            return string.Empty;
        }
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 4: Add SetupDiGetDeviceInstanceId to SetupApi

**Objective:** Add missing P/Invoke for SetupDiGetDeviceInstanceId.

**Files:**
- Modify: `src/Native/SetupApi.cs`

**Step 1: Add P/Invoke declaration**

Add to SetupApi.cs before the closing brace:

```csharp
/// <summary>Retrieves the device instance ID for a device information element.</summary>
[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
internal static extern bool SetupDiGetDeviceInstanceId(
    IntPtr DeviceInfoSet,
    ref SP_DEVINFO_DATA DeviceInfoData,
    IntPtr DeviceInstanceId,
    int DeviceInstanceIdSize,
    out int RequiredSize);
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 5: Create HardwareDeviceHelper main API

**Objective:** Implement the public static helper with all enumeration methods.

**Files:**
- Create: `src/Hardware/HardwareDeviceHelper.cs`

**Step 1: Write HardwareDeviceHelper.cs**

```csharp
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
    /// Enumerates Windows hardware devices and retrieves detailed information.
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
        public static IReadOnlyList<HardwareDeviceInfo> GetAllDevices()
        {
            return EnumerateDevices(IntPtr.Zero, SetupApi.DIGCF_ALLCLASSES);
        }

        /// <summary>
        /// Enumerates devices of a specific class.
        /// </summary>
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

        /// <summary>Gets USB storage devices (mass storage class).</summary>
        public static IReadOnlyList<HardwareDeviceInfo> GetUsbStorageDevices()
        {
            var usbDevices = GetUsbDevices();
            var result = new List<HardwareDeviceInfo>();
            foreach (var device in usbDevices)
            {
                if (device.ClassName.Contains("Disk", StringComparison.OrdinalIgnoreCase) ||
                    device.DeviceDescription.Contains("Storage", StringComparison.OrdinalIgnoreCase) ||
                    device.DeviceDescription.Contains("Mass", StringComparison.OrdinalIgnoreCase))
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

                        // Get bus-reported device descriptor for speed/power
                        string busDesc = DeviceInfoParser.GetStringProperty(
                            devInfoSet, ref devInfoData, SPDRP_BUSREPORTEDDEVDESC);
                        UsbSpeed speed = UsbDeviceParser.ParseSpeed(busDesc);
                        int maxPower = UsbDeviceParser.ParseMaxPower(busDesc);

                        // Create new instance with USB info
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
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 6: Create unit tests

**Objective:** Write comprehensive tests for HardwareDeviceHelper.

**Files:**
- Create: `tests/BPlusLib.Foundation.Tests/Hardware/HardwareDeviceHelperTests.cs`

**Step 1: Write test file**

```csharp
// <copyright file="HardwareDeviceHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.Hardware;

namespace BPlusLib.Foundation.Tests.Hardware
{
    [Trait("Category", "Hardware")]
    public sealed class HardwareDeviceHelperTests
    {
        // ── GetAllDevices ─────────────────────────────────────────────

        [SkippableFact]
        public void GetAllDevices_ReturnsNonEmpty()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetAllDevices();
            devices.Should().NotBeEmpty();
        }

        [SkippableFact]
        public void GetAllDevices_HasDeviceIds()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetAllDevices();
            devices.Should().AllSatisfy(d =>
            {
                d.DeviceId.Should().NotBeNullOrEmpty();
                d.DeviceName.Should().NotBeNull();
            });
        }

        // ── GetDevicesByClass ─────────────────────────────────────────

        [SkippableFact]
        public void GetUsbDevices_ReturnsList()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetUsbDevices();
            devices.Should().NotBeNull();
        }

        [SkippableFact]
        public void GetComPorts_ReturnsList()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetComPorts();
            devices.Should().NotBeNull();
        }

        [SkippableFact]
        public void GetDiskDrives_ReturnsNonEmpty()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetDiskDrives();
            devices.Should().NotBeEmpty();
        }

        [SkippableFact]
        public void GetNetworkAdapters_ReturnsNonEmpty()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetNetworkAdapters();
            devices.Should().NotBeEmpty();
        }

        // ── GetDeviceById ─────────────────────────────────────────────

        [SkippableFact]
        public void GetDeviceById_NullReturnsNull()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            HardwareDeviceHelper.GetDeviceById(null!).Should().BeNull();
            HardwareDeviceHelper.GetDeviceById("").Should().BeNull();
        }

        [SkippableFact]
        public void GetDeviceById_NonexistentReturnsNull()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            HardwareDeviceHelper.GetDeviceById("ROOT\\NONEXISTENT\\0000").Should().BeNull();
        }

        // ── IsDevicePresent ───────────────────────────────────────────

        [SkippableFact]
        public void IsDevicePresent_NullReturnsFalse()
        {
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));

            HardwareDeviceHelper.IsDevicePresent(null!).Should().BeFalse();
        }

        // ── UsbDeviceParser ───────────────────────────────────────────

        [Fact]
        public void UsbDeviceParser_TryParseVidPid_ValidId()
        {
            string deviceId = @"USB\VID_1234&PID_5678\SerialNumber123";
            bool result = Hardware.UsbDeviceParser.TryParseVidPid(deviceId, out int vid, out int pid);
            result.Should().BeTrue();
            vid.Should().Be(0x1234);
            pid.Should().Be(0x5678);
        }

        [Fact]
        public void UsbDeviceParser_TryParseVidPid_InvalidId()
        {
            bool result = Hardware.UsbDeviceParser.TryParseVidPid(@"PCI\VEN_1234", out _, out _);
            result.Should().BeFalse();
        }

        [Fact]
        public void UsbDeviceParser_ParseSerialNumber_ValidId()
        {
            string deviceId = @"USB\VID_1234&PID_5678\ABCD1234";
            string? serial = Hardware.UsbDeviceParser.ParseSerialNumber(deviceId);
            serial.Should().Be("ABCD1234");
        }

        [Fact]
        public void UsbDeviceParser_IsUsbDevice_True()
        {
            Hardware.UsbDeviceParser.IsUsbDevice(@"USB\VID_1234&PID_5678").Should().BeTrue();
        }

        [Fact]
        public void UsbDeviceParser_IsUsbDevice_False()
        {
            Hardware.UsbDeviceParser.IsUsbDevice(@"PCI\VEN_1234").Should().BeFalse();
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_HighSpeed()
        {
            string desc = "USB 2.00, Speed = High speed (480Mbit/s), ...";
            Hardware.UsbDeviceParser.ParseSpeed(desc).Should().Be(Hardware.UsbSpeed.HighSpeed);
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_SuperSpeed()
        {
            string desc = "USB 3.00, Speed = SuperSpeed (5Gbit/s), ...";
            Hardware.UsbDeviceParser.ParseSpeed(desc).Should().Be(Hardware.UsbSpeed.SuperSpeed);
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_SuperSpeedPlus()
        {
            string desc = "USB 3.10, Speed = SuperSpeed+ (10Gbit/s), ...";
            Hardware.UsbDeviceParser.ParseSpeed(desc).Should().Be(Hardware.UsbSpeed.SuperSpeedPlus);
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_EmptyReturnsUnknown()
        {
            Hardware.UsbDeviceParser.ParseSpeed("").Should().Be(Hardware.UsbSpeed.Unknown);
            Hardware.UsbDeviceParser.ParseSpeed(null!).Should().Be(Hardware.UsbSpeed.Unknown);
        }

        [Fact]
        public void UsbDeviceParser_GetUsbVersionString_Works()
        {
            Hardware.UsbDeviceParser.GetUsbVersionString(Hardware.UsbSpeed.HighSpeed).Should().Be("2.0");
            Hardware.UsbDeviceParser.GetUsbVersionString(Hardware.UsbSpeed.SuperSpeed).Should().Be("3.0/3.1 Gen 1");
            Hardware.UsbDeviceParser.GetUsbVersionString(Hardware.UsbSpeed.SuperSpeedPlus).Should().Be("3.1 Gen 2/3.2");
        }

        [Fact]
        public void UsbDeviceParser_GetDeviceClassName_Works()
        {
            Hardware.UsbDeviceParser.GetDeviceClassName(0x08).Should().Be("Mass Storage");
            Hardware.UsbDeviceParser.GetDeviceClassName(0x03).Should().Be("HID (Human Interface)");
            Hardware.UsbDeviceParser.GetDeviceClassName(0x09).Should().Be("Hub");
        }

        [Fact]
        public void UsbDeviceParser_ParseMaxPower_Works()
        {
            string desc = "..., MaxPower = 500mA, ...";
            Hardware.UsbDeviceParser.ParseMaxPower(desc).Should().Be(500);
        }
    }
}
```

**Step 2: Verify compilation and tests**

Run: `dotnet test --framework net8.0 --filter "FullyQualifiedName~Hardware" -v q`
Expected: Tests pass (skipped on Linux, pass on Windows)

---

### Task 7: Run full build + test suite

**Objective:** Verify all changes compile and existing tests still pass.

**Step 1: Build all targets**

Run: `dotnet build src/BPlusLib.Foundation -v q`
Expected: Build succeeded, 0 errors

**Step 2: Run all tests**

Run: `dotnet test --framework net8.0 --no-restore -v q`
Expected: All tests pass

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add HardwareDeviceHelper for USB/HID/disk/network enumeration"
```

---

## Verification

After implementation, verify:

1. `dotnet build` — 0 errors across net472, net6.0, net8.0
2. `dotnet test --framework net8.0` — All existing tests pass
3. Hardware tests are skipped on Linux, pass on Windows
4. XML documentation is complete for all public members
5. All methods are thread-safe and return empty collections on failure

---

## Risks

| Risk | Mitigation |
|------|------------|
| SetupAPI P/Invoke signature errors | Use well-documented signatures from Microsoft docs |
| Device enumeration may be slow on systems with many devices | Cache results, document performance characteristics |
| Access denied for some devices | Gracefully skip inaccessible devices |
| USB VID/PID parsing may fail for non-standard IDs | Return 0 for unparsable values |
