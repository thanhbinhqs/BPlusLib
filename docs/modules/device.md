# Device

Provides Win32 P/Invoke-based helper methods for Windows device and volume operations, including USB device detection, device enumeration, and volume information. All methods are thread-safe and gracefully return empty/null on non-Windows platforms.

## Classes

### DeviceHelper
Static helper class for Windows device operations using SetupAPI and kernel32 P/Invoke.

| Method | Returns | Description |
|--------|---------|-------------|
| RegisterDeviceNotification(IntPtr windowHandle, Guid interfaceClassGuid) | IntPtr | Registers for device notification events (e.g., USB insertion/removal) |
| UnregisterDeviceNotification(IntPtr notificationHandle) | bool | Unregisters a device notification handle |
| GetAllDevices() | IReadOnlyList&lt;DeviceInfo&gt; | Enumerates all devices present on the system via SetupAPI |
| GetDeviceProperty(string deviceInstanceId, string propertyName) | string? | Gets a specific property for a device by instance ID |
| GetVolumeDevices() | DeviceVolumeInfo[]? | Enumerates logical volumes with drive letter, label, file system info |
| GetUsbDevices() | IReadOnlyList&lt;string&gt; | Enumerates USB devices using GUID_DEVINTERFACE_USB_DEVICE |

### DeviceInfo
Class representing information about a hardware device discovered by the system.

| Property | Type | Description |
|----------|------|-------------|
| DeviceId | string? | Unique device instance ID |
| Description | string? | Device description (friendly name from driver) |
| FriendlyName | string? | User-friendly device name |
| Manufacturer | string? | Manufacturer name |
| DriverVersion | string? | Driver version string |
| DriverDate | string? | Driver date string |
| HardwareId | string? | Hardware ID(s) for the device |
| BusReportedDeviceDesc | string? | Bus-reported device description |
| ClassGuid | string? | Device class GUID |
| Status | string? | Device status (e.g., "OK", "Error") |

### DeviceVolumeInfo
Class representing information about a logical volume and its associated device.

| Property | Type | Description |
|----------|------|-------------|
| DriveLetter | string? | Drive letter (e.g., "C:", "D:") |
| VolumeLabel | string? | Volume label |
| FileSystem | string? | File system type (e.g., "NTFS", "FAT32") |
| SerialNumber | string? | Volume serial number |
| DevicePath | string? | Device path (e.g., "\\.\PhysicalDrive0") |
| DeviceType | string? | Device type ("Fixed", "Removable", "CD-ROM") |
| IsReady | string? | Whether the volume is accessible |

## Usage

```csharp
using BPlusLib.Foundation.Device;

// Enumerate all devices
IReadOnlyList<DeviceInfo> devices = DeviceHelper.GetAllDevices();
foreach (var device in devices)
{
    Console.WriteLine($"{device.FriendlyName} ({device.DeviceId})");
}

// Get volume information
DeviceVolumeInfo[]? volumes = DeviceHelper.GetVolumeDevices();
if (volumes != null)
{
    foreach (var vol in volumes)
    {
        Console.WriteLine($"{vol.DriveLetter} - {vol.VolumeLabel} ({vol.FileSystem})");
    }
}

// Get USB devices
IReadOnlyList<string> usbDevices = DeviceHelper.GetUsbDevices();

// Register for device change notifications
IntPtr handle = DeviceHelper.RegisterDeviceNotification(
    windowHandle, Guid.Empty); // Guid.Empty = all device classes
```

## Dependencies
- setupapi.dll (SetupDiGetClassDevs, SetupDiEnumDeviceInfo, SetupDiGetDeviceRegistryProperty, etc.)
- user32.dll (RegisterDeviceNotification, UnregisterDeviceNotification)
- kernel32.dll (GetLogicalDrives, QueryDosDevice, GetDriveType, GetVolumeInformation, DeviceIoControl)
