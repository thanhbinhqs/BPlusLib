# SerialPorts

Identifies which process owns each Windows Serial Port (COM Port) using pure P/Invoke — no WMI, no Handle.exe, no third-party libraries. Enumerates all system handles, resolves COM port ownership, and provides rich process metadata for each port.

## Classes

### SerialPortInspector
Main entry point for serial port inspection. Identifies which process owns each COM port.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetAllOpenedSerialPorts() | IReadOnlyList\<SerialPortOwner\> | Enumerates every serial port currently opened in the system |
| GetSerialPortOwner(portName) | SerialPortOwner? | Gets the owner of a specific COM port by name |

### SerialPortOwner
Represents the owner (process) of an opened Windows Serial Port.

| Property | Type | Description |
|----------|------|-------------|
| PortName | string | COM port name (e.g. "COM3") |
| DevicePath | string | NT device path (e.g. @"\Device\Serial0") |
| ProcessId | int | Owning process ID |
| ProcessName | string | Process name (e.g. "notepad.exe") |
| ImagePath | string | Full executable path |
| CommandLine | string? | Full command line |
| StartTime | DateTime? | Process start time (UTC) |
| CompanyName | string? | Company name from PE version resource |
| ProductName | string? | Product name from PE version resource |
| FileVersion | string? | File version string |
| ProductVersion | string? | Product version string |

### Internal Helper Classes

| Class | Description |
|-------|-------------|
| DosDeviceMapper | Maps COM port names to NT device paths via QueryDosDevice |
| SerialPortMatcher | Matches NT device paths back to COM port names using reverse mapping |
| SystemHandleEnumerator | Enumerates all system handles via NtQuerySystemInformation |
| ObjectNameResolver | Resolves object names from handle values via NtQueryObject |
| ProcessInformationProvider | Retrieves process metadata (name, path, command line, version info) |
| Utilities | Shared marshalling helpers for UnicodeString conversion and Win32 error messages |

## Usage

```csharp
using BPlusLib.Foundation.SerialPorts;

// Get all opened serial ports with their owners
var ports = SerialPortInspector.GetAllOpenedSerialPorts();
foreach (var port in ports)
{
    Console.WriteLine($"Port: {port.PortName}");
    Console.WriteLine($"  Process: {port.ProcessName} (PID={port.ProcessId})");
    Console.WriteLine($"  Path: {port.ImagePath}");
    Console.WriteLine($"  Company: {port.CompanyName}");
}

// Check a specific port
var owner = SerialPortInspector.GetSerialPortOwner("COM3");
if (owner != null)
    Console.WriteLine($"COM3 is owned by {owner.ProcessName} (PID={owner.ProcessId})");
```

## Dependencies
- `ntdll.dll` — NtQuerySystemInformation, NtQueryObject
- `kernel32.dll` — OpenProcess, CloseHandle, DuplicateHandle, QueryDosDevice, QueryFullProcessImageName, GetProcessTimes
- `psapi.dll` — (indirect, via process information)
