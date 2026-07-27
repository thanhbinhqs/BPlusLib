# Hardware

Enumerates Windows hardware devices and retrieves detailed information including USB speed, VID/PID, serial numbers, and device classes. Uses pure P/Invoke (SetupAPI) — no WMI, no PowerShell. All methods are thread-safe and return empty lists on failure.

## Classes

### HardwareDeviceHelper
Static helper class for hardware device enumeration and querying.

| Method | Returns | Description |
|--------|---------|-------------|
| GetAllDevices() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Enumerates all hardware devices |
| GetDevicesByClass(Guid classGuid) | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Enumerates devices of a specific class |
| GetDeviceById(string deviceId) | HardwareDeviceInfo? | Gets a specific device by instance ID |
| GetUsbDevices() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Gets all USB devices |
| GetUsbStorageDevices() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Gets USB mass storage devices |
| GetUsbHidDevices() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Gets USB HID devices |
| GetComPorts() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Gets COM/serial ports |
| GetDiskDrives() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Gets disk drives |
| GetNetworkAdapters() | IReadOnlyList&lt;HardwareDeviceInfo&gt; | Gets network adapters |
| IsDevicePresent(string deviceId) | bool | Checks if a device is currently present |

### HardwareDeviceInfo
Represents detailed information about a Windows hardware device, including USB-specific properties.

| Property | Type | Description |
|----------|------|-------------|
| DeviceId | string | Device instance ID |
| DeviceName | string | Friendly device name |
| Manufacturer | string | Manufacturer name |
| ClassGuid | string | Device class GUID |
| ClassName | string | Device class name |
| LocationInfo | string | Device location (e.g., "Port_#0001") |
| EnumeratorName | string | Bus enumerator (e.g., "USB", "PCI") |
| DeviceDescription | string | Device description |
| HardwareId | string | Hardware ID string |
| BusReportedDeviceDesc | string | Bus-reported device descriptor |
| IsConnected | bool | Whether device is connected |
| IsRemovable | bool | Whether device is removable |
| InstallDate | DateTime? | First install date |
| VendorId | int | USB Vendor ID (0 if not USB) |
| ProductId | int | USB Product ID (0 if not USB) |
| SerialNumber | string | USB serial number |
| FirmwareRevision | string | USB firmware revision |
| HardwareRevision | string | USB hardware revision |
| Speed | UsbSpeed | USB connection speed |
| UsbVersion | string | USB version string |
| MaxPowerMilliamps | int | USB max power in mA |
| DeviceClass | string | USB device class name |
| DeviceSubClass | string | USB device subclass |
| DeviceProtocol | string | USB device protocol |
| IsSelfPowered | bool | Whether device is self-powered |
| IsRemoteWakeCapable | bool | Whether device supports remote wake |

### UsbSpeed
Enum defining USB connection speeds.

| Value | Description |
|-------|-------------|
| Unknown | Speed unknown |
| LowSpeed | USB 1.0 — 1.5 Mbps |
| FullSpeed | USB 1.1 — 12 Mbps |
| HighSpeed | USB 2.0 — 480 Mbps |
| SuperSpeed | USB 3.0/3.1 Gen 1 — 5 Gbps |
| SuperSpeedPlus | USB 3.1 Gen 2/3.2 — 10 Gbps |

## Usage

```csharp
using BPlusLib.Foundation.Hardware;

// Enumerate all devices
IReadOnlyList<HardwareDeviceInfo> devices = HardwareDeviceHelper.GetAllDevices();
foreach (var device in devices)
{
    Console.WriteLine($"{device.DeviceName} — {device.ClassName}");
}

// Get USB storage devices
IReadOnlyList<HardwareDeviceInfo> usbStorage = HardwareDeviceHelper.GetUsbStorageDevices();
foreach (var usb in usbStorage)
{
    Console.WriteLine($"{usb.DeviceName} VID={usb.VendorId:X4} PID={usb.ProductId:X4} [{usb.UsbVersion}]");
}

// Get COM ports
IReadOnlyList<HardwareDeviceInfo> ports = HardwareDeviceHelper.GetComPorts();

// Get specific device
HardwareDeviceInfo? device = HardwareDeviceHelper.GetDeviceById("USB\\VID_1234&PID_5678\\SerialNumber");

// Check device presence
bool present = HardwareDeviceHelper.IsDevicePresent("USB\\VID_1234&PID_5678\\Serial");
```

## Dependencies
- setupapi.dll (SetupDiGetClassDevs, SetupDiEnumDeviceInfo, SetupDiGetDeviceRegistryProperty, etc.)
- BPlusLib.Foundation.Native (SetupApi — shared P/Invoke declarations)
