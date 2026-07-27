# SystemInfo

System information module providing CPU, BIOS, OS, memory, battery, network adapter, and disk information. All data is obtained via P/Invoke and registry reads; no WMI dependency. Most classes use lazy singleton instances.

## Classes

### CpuInfo
Detailed CPU information — name, manufacturer, core counts, architecture, clock speed, virtualization detection, and load percentage.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| CpuInfo.Current | CpuInfo | Lazy singleton instance |
| Name | string | Processor name (e.g. "Intel(R) Core(TM) i7-10750H") |
| Manufacturer | string | Manufacturer (e.g. "Intel", "AMD") |
| PhysicalCores | int | Number of physical cores |
| LogicalCores | int | Number of logical processors (including hyper-threading) |
| Architecture | string | Architecture string ("x86", "x64", "ARM64") |
| ProcessorId | int | Processor ID (Family/Model/Stepping encoded) |
| MaxFrequencyMHz | long | Maximum frequency in MHz |
| CurrentFrequencyMHz | long | Current frequency in MHz (may be 0) |
| IsVirtualMachine | bool | Whether running inside a VM |
| CurrentLoadPercentage | float? | Current CPU load (0-100) or null |

### BiosInfo
BIOS/firmware information — manufacturer, version, serial number, release date, UEFI detection.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| BiosInfo.Current | BiosInfo | Lazy singleton instance |
| Manufacturer | string? | BIOS manufacturer |
| Name | string? | BIOS product name |
| Version | string? | BIOS version string |
| SerialNumber | string? | System serial number |
| ReleaseDate | DateTime? | BIOS release date |
| SmbiosVersion | string? | SMBIOS version |
| IsUefi | bool | Whether booted in UEFI mode |

### OperatingSystemInfo
OS information — name, version, edition, architecture, boot time.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| OperatingSystemInfo.Current | OperatingSystemInfo | Lazy singleton instance |
| Name | string | OS display name (e.g. "Windows 10 Pro") |
| Version | string | Version string (e.g. "10.0.19041") |
| Edition | string | Edition (e.g. "Professional") |
| BuildNumber | int | Build number |
| ServicePack | string | Service pack level |
| Architecture | string | Processor architecture |
| InstallDate | DateTime? | OS install date |
| LastBootUpTime | DateTime? | Last boot time |
| IsServer | bool | Whether OS is a server SKU |
| Is64Bit | bool | Whether OS is 64-bit |
| SuiteMask | int | Product suite mask |
| ProductType | byte | Product type byte |
| CSDVersion | string | CSD version string |

### MemoryInfo
System memory (RAM, virtual memory, page file) information.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| MemoryInfo.Current | MemoryInfo | Lazy singleton instance |
| TotalPhysicalBytes | long | Total physical memory |
| AvailablePhysicalBytes | long | Available physical memory |
| UsedPhysicalBytes | long | Used physical memory |
| TotalVirtualBytes | long | Total virtual memory |
| AvailableVirtualBytes | long | Available virtual memory |
| UsedVirtualBytes | long | Used virtual memory |
| TotalPageFileBytes | long | Total page file size |
| AvailablePageFileBytes | long | Available page file space |
| UsedPageFileBytes | long | Used page file space |
| PhysicalUsagePercent | double | Physical memory usage (0-100) |

### BatteryInfo
Battery status and capabilities.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| BatteryInfo.Current | BatteryInfo | Lazy singleton instance |
| IsPresent | bool | Whether a battery is present |
| EstimatedChargePercent | int | Estimated charge (0-100) |
| IsCharging | bool | Whether the battery is charging |
| StatusFlags | BatteryStatusFlags | Raw battery status flags |
| BatteryLifeSeconds | int? | Remaining battery life in seconds |
| BatteryFullLifeSeconds | int? | Full charge lifetime in seconds |
| VoltageMillivolts | double? | Battery voltage in millivolts |
| Chemistry | string? | Battery chemistry (e.g. "LION") |
| DesignCapacityMW | int? | Design capacity in mWh |
| CurrentCapacityMW | int? | Current capacity in mWh |

### NetworkAdapterInfo / NetworkInfo
Network adapter information via IPHLPAPI.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| NetworkInfo.GetAllAdapters() | IReadOnlyList\<NetworkAdapterInfo\> | Enumerates all network adapters |
| NetworkAdapterInfo.Name | string | Friendly name |
| NetworkAdapterInfo.Description | string | Adapter description |
| NetworkAdapterInfo.MacAddress | string? | MAC address |
| NetworkAdapterInfo.IpAddresses | IReadOnlyList\<string\> | IP addresses |
| NetworkAdapterInfo.GatewayAddresses | IReadOnlyList\<string\> | Gateway addresses |
| NetworkAdapterInfo.DnsAddresses | IReadOnlyList\<string\> | DNS server addresses |
| NetworkAdapterInfo.IsDhcpEnabled | bool | Whether DHCP is enabled |
| NetworkAdapterInfo.IsUp | bool | Whether adapter is operational |
| NetworkAdapterInfo.AdapterType | string? | Type (Ethernet, Wireless, etc.) |

### DiskInfo / DriveInfoEx
Logical drive information via kernel32.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| DiskInfo.GetAllDrives() | IReadOnlyList\<DriveInfoEx\> | Enumerates all logical drives |
| DiskInfo.GetDrive(driveName) | DriveInfoEx? | Gets info for a specific drive |
| DriveInfoEx.Name | string | Drive name (e.g. "C:") |
| DriveInfoEx.VolumeLabel | string | Volume label |
| DriveInfoEx.FileSystem | string | File system (e.g. "NTFS") |
| DriveInfoEx.DriveType | DriveTypeEx | Drive type |
| DriveInfoEx.TotalBytes | long | Total capacity |
| DriveInfoEx.AvailableBytes | long | Available free space |
| DriveInfoEx.UsedBytes | long | Used space |
| DriveInfoEx.UsagePercent | double | Usage percentage (0-100) |
| DriveInfoEx.SerialNumber | string? | Volume serial number |

### EnvironmentHelper
Environment variable management, PATH manipulation, domain info.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetVariable(name, target?) | string? | Gets an environment variable |
| SetVariable(name, value?, target?) | bool | Sets an environment variable |
| DeleteVariable(name, target?) | bool | Deletes an environment variable |
| ExpandString(input?) | string? | Expands %TEMP% style variables |
| GetMachineName() | string | Machine's NetBIOS name |
| IsDomainJoined() | bool | Whether computer is domain-joined |
| GetDomainName() | string? | Domain name if joined |
| AddToUserPath(directoryPath) | bool | Adds directory to user PATH |
| RemoveFromUserPath(directoryPath) | bool | Removes directory from user PATH |
| GetUserPathDirectories() | List\<string\> | User PATH directories |
| GetSystemPathDirectories() | List\<string\> | System PATH directories |

## Enums

| Enum | Values | Description |
|------|--------|-------------|
| DriveTypeEx | Unknown, NoRootDirectory, Removable, Fixed, Network, CDRom, Ram | Logical drive types |
| BatteryStatusFlags | None, Discharging, AcOffline, Charging, LowBattery, CriticalBattery | Battery status flags |

## Usage

```csharp
using BPlusLib.Foundation.SystemInfo;

// CPU info
var cpu = CpuInfo.Current;
Console.WriteLine($"{cpu.Name}: {cpu.PhysicalCores} cores, {cpu.LogicalCores} threads");
Console.WriteLine($"Load: {cpu.CurrentLoadPercentage:F1}%");

// OS info
var os = OperatingSystemInfo.Current;
Console.WriteLine($"{os.Name} (Build {os.BuildNumber})");

// Memory
var mem = MemoryInfo.Current;
Console.WriteLine($"RAM: {mem.UsedPhysicalBytes / 1024 / 1024}MB / {mem.TotalPhysicalBytes / 1024 / 1024}MB");

// Drives
foreach (var drive in DiskInfo.GetAllDrives())
    Console.WriteLine($"{drive.Name} {drive.FileSystem}: {drive.UsagePercent:F1}% used");

// Network
var adapters = NetworkInfo.GetAllAdapters();
foreach (var adapter in adapters)
    Console.WriteLine($"{adapter.Name}: {adapter.MacAddress}");
```

## Dependencies
- `kernel32.dll` — GetSystemInfo, GetLogicalProcessorInformationEx, GlobalMemoryStatusEx, GetLogicalDrives, GetDriveTypeW, GetVolumeInformationW, GetDiskFreeSpaceExW, GetTickCount64, IsWow64Process, GetNativeSystemInfo
- `ntdll.dll` — NtQuerySystemInformation (for CPU load)
- `iphlpapi.dll` — GetAdaptersAddresses
- `netapi32.dll` — NetGetJoinInformation, NetApiBufferFree
- `BPlusLib.Foundation.Native` — Shared P/Invoke declarations
