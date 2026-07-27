# BPlusLib.Foundation

**Enterprise-grade Windows Foundation Library — 31 modules, 1,275 tests, pure P/Invoke.**

---

## Overview

BPlusLib.Foundation is a production-ready C# utility library for Windows desktop development. All components use pure P/Invoke — **no WMI, no PowerShell, no external executables**. Cross-platform graceful degradation: every method returns `null`/`false` instead of throwing on non-Windows.

| | |
|---|---|
| **Targets** | net472 · net6.0 · net8.0 |
| **Version** | 2.8.0 |
| **License** | MIT |
| **Tests** | 1,275 (1,183 passing, 92 skipped — Windows-only) |
| **Author** | [thanhbinhqs](https://github.com/thanhbinhqs) |

---

## Quick Start

```bash
dotnet add package BPlusLib.Foundation --source "https://nuget.pkg.github.com/thanhbinhqs/index.json"
```

```csharp
// System info
using BPlusLib.Foundation.SystemInfo;
var cpu = new CpuInfo();
var mem = new MemoryInfo();

// Process management
using BPlusLib.Foundation.Process;
var result = CommandRunner.RunCommand("tasklist", timeoutMs: 10000);

// Service management
using BPlusLib.Foundation.Services;
ServiceHelper.StartService("Spooler");
ServiceHelper.StopService("Spooler", waitMs: 15000);

// UAC detection
using BPlusLib.Foundation.Security;
bool elevated = UacHelper.IsElevated();
IntegrityLevel level = UacHelper.GetIntegrityLevel();

// TCP/UDP communication
using BPlusLib.Foundation.Networking;
using var server = TcpSocketHelper.StartServer(port: 9000);
using var client = TcpSocketHelper.Connect("127.0.0.1", 9000);
client?.Send(Encoding.UTF8.GetBytes("Hello"));

// Credential Manager
using BPlusLib.Foundation.Security;
CredentialHelper.Write("myapp:user", "admin", "s3cret");
var cred = CredentialHelper.Read("myapp:user");

// Named pipe IPC
using var pipeServer = new PipeServer("BPlusLibTest");
pipeServer.WaitForConnection(5000);
pipeServer.Write(Encoding.UTF8.GetBytes("response"));

// Auto-update
using BPlusLib.Foundation.Windows;
await AppUpdater.UpdateAsync("https://example.com/releases/latest.zip");

// Cisco EWC (RESTCONF + Syslog)
using BPlusLib.Foundation.Networking.Cisco;
var device = await CiscoEwcHelper.GetDeviceInfoAsync("192.168.1.1", "admin", "pass");
var aps = await CiscoEwcHelper.GetAccessPointsAsync("192.168.1.1", "admin", "pass");
var clients = await CiscoEwcHelper.GetClientsAsync("192.168.1.1", "admin", "pass");
using var syslog = CiscoEwcHelper.StartSyslogListener(514, entry => Console.WriteLine($"[{entry.Severity}] {entry.Message}"));
```

---

## Modules

### 🔧 Common
Foundational types: `Guard`, `Result<T>`, `Option<T>`, `AsyncLock`, `AsyncCache`, `RetryPolicy`, `Debounce`, `ObjectPool<T>`, `DisposableBase`, `ObservableObject`, and polyfills.

### 🪟 Window
`MonitorHelper` — monitor enumeration & info · `DragMoveHelper` — borderless drag-move · `ResizeHelper` — borderless resize · `WindowPositionManager` — save/restore positions · `WindowAnimation` — fade/slide animations.

### 💬 Dialogs
`MessageBoxEx` — async message box with WH_CBT hook, dark mode, timeout · `InputBoxEx` — text input dialog · `ProgressDialog` — modal progress bar.

### 🖥️ System Info
`OperatingSystemInfo` — name, version, architecture, boot time · `CpuInfo` — cores, speed, virtualization · `MemoryInfo` — physical, virtual, page file · `DiskInfo` — drive info, free space · `NetworkAdapterInfo` — MAC, IP, speed · `BatteryInfo` — charge, state · `BiosInfo` — vendor, version · `EnvironmentHelper` — env vars, PATH, domain · `MemoryHelper` — memory-mapped files, process memory.

### ⚙️ Process
`CommandRunner` — async process execution with stdout/stderr capture · `ProcessExtensions` — elevation, image path, command line, kill tree, wait async.

### 🌐 Network
`NetHelper` — Ping, TCP/UDP table, ARP cache, DNS lookup, Wake-on-LAN, VPN detection, routing.

### 🔌 Networking
`TcpConnection` — full-duplex TCP wrapper · `TcpServer` — async accept · `TcpSocketHelper` — static connect/start · `UdpEndpoint` — send/receive/broadcast/multicast · `UdpSocketHelper` — static one-shot · `NetClientHelper` — HTTP GET/POST/PUT/DELETE, FTP · `HttpListenerHelper` — embedded localhost HTTP server.

### 🐦 Cisco EWC
`CiscoEwcHelper` — static facade for Cisco EWC via RESTCONF (RFC 8040) · `RestConfClient` — HTTPS client with Basic auth, Newtonsoft.Json · `YangParser` — parse YANG model JSON responses (device info, APs, clients, SSIDs) · `SyslogServer` — UDP syslog listener (RFC 5424) with real-time callbacks.

```csharp
using BPlusLib.Foundation.Networking.Cisco;

// Device info
var device = await CiscoEwcHelper.GetDeviceInfoAsync("192.168.1.1", "admin", "pass");

// Access points
var aps = await CiscoEwcHelper.GetAccessPointsAsync("192.168.1.1", "admin", "pass");

// Connected clients
var clients = await CiscoEwcHelper.GetClientsAsync("192.168.1.1", "admin", "pass");

// Configured SSIDs
var ssids = await CiscoEwcHelper.GetSsidsAsync("192.168.1.1", "admin", "pass");

// Raw YANG data
var raw = await CiscoEwcHelper.GetYangDataAsync("192.168.1.1", "admin", "pass",
    "/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data");

// Syslog (real-time)
using var syslog = CiscoEwcHelper.StartSyslogListener(514, entry =>
    Console.WriteLine($"[{entry.Severity}] {entry.Hostname}: {entry.Message}"));

// Diagnostics
bool ok = await CiscoEwcHelper.IsReachableAsync("192.168.1.1", "admin", "pass");
var caps = await CiscoEwcHelper.GetCapabilitiesAsync("192.168.1.1", "admin", "pass");
```

### 📁 IO
`SafeFileOps` — atomic write, retry copy/move, lock detection · `PathHelper` — sanitize, normalize, relative paths · `ChecksumHelper` — MD5/SHA1/SHA256/SHA512/CRC32 · `FileVersionHelper` — PE version resources (VerQueryValue).

### 🗄️ Registry
`RegistryHelper` — CRUD values, export/import .reg, backup/restore, typed getters.

### 🔒 Security
`TokenHelper` — token queries, groups, statistics · `PrivilegeHelper` — enable/disable privileges · `IntegrityHelper` — integrity level get/set · `SecurityHelper` — high-level security checks · `UacHelper` — elevation detection, runas, integrity level · `CredentialHelper` — Windows Credential Manager (read/write/enumerate/delete) · `WinTrustHelper` — Authenticode signature verification.

### 📊 Diagnostics
`EventLogReader` — Windows Event Log reading · `CrashDumpHelper` — MiniDump/FullDump creation.

### 📝 Logging
`FileLogger` — rolling file log writer · `LoggerFactory` — centralized logger creation · `LogEntry` — log entry model.

### 🧩 Extensions
`StringExtensions` — email validation, truncation, ToTitleCase, slug · `StreamExtensions` — read exact bytes, read-all · `TaskExtensions` — WithTimeout, WithRetry, FireAndForget · `CollectionExtensions` — AddRange, Batch, DistinctBy, Shuffle.

### 📋 Clipboard
`ClipboardHelper` — text, files, bitmap clipboard operations.

### 📂 Explorer
`ExplorerHelper` — known folders, folder picker, shortcut management, recycle bin.

### 🔐 Cryptography
`CryptoHelper` — AES encrypt/decrypt, RSA key pair, HMAC, PBKDF2, self-signed certificates, X.509.

### 🎵 Media
`MediaInfoReader` — ID3v1/v2 tag reading, RIFF/WAV header parsing.

### 🖼️ Graphics
`ScreenHelper` — multi-monitor bounds, virtual screen · `IconExtractor` — extract icons from EXE/DLL · `DisplayHelper` — DPI, resolution, color depth, high contrast.

### 🐚 Shell
`ShellHelper` — file verbs, associations, recycle bin · `ShortcutHelper` — .lnk creation/reading via IShellLink COM · `NotifyIconHelper` — system tray icon (Shell_NotifyIconW) · `AssocHelper` — file extension queries · `ThemeHelper` — dark/light mode, accent color, DWM.

### 📱 Device
`DeviceHelper` — USB detection, volume info, SetupAPI device enumeration.

### 🔧 Hardware
`HardwareDeviceHelper` — USB speed detection (Low/Full/High/Super/SuperSpeed+), VID/PID/Serial parsing via SetupAPI · `UsbDeviceParser` — USB descriptor string parsing · `DeviceInfoParser` — device instance ID parsing.

```csharp
using BPlusLib.Foundation.Hardware;

// Get all USB devices
var devices = HardwareDeviceHelper.GetUsbDevices();
foreach (var dev in devices)
    Console.WriteLine($"{dev.FriendlyName} — {dev.UsbSpeed} — VID:{dev.VendorId} PID:{dev.ProductId}");
```

### 🌍 Localization
`LocalizationHelper` — multi-language .resx loading, culture switching.

### 🧵 Threading
`ThreadHelper` — STA/MTA thread management, COM apartment.

### 🔌 Serial Ports
`SerialPortInspector` — identify which process owns each COM port (NtQuerySystemInformation). Pure P/Invoke.

### ⚙️ Services
`ServiceHelper` — start/stop/restart/pause/create/delete/query Windows services · `JobObjectHelper` — process group management (kill-on-close, process limits) · `ConsoleHelper` — console window management (show/hide, title, color) · `RestartManagerHelper` — Restart Manager API (lock detection for file updates).

### ⌨️ Input
`InputHelper` — keyboard/mouse simulation (SendInput, Unicode text) · `HotkeyHelper` — global hotkey registration (RegisterHotKey).

### 🔗 IPC
`PipeServer` / `PipeClient` — named pipe client/server with async support · `PipeHelper` — one-shot transaction.

### ⚡ Power
`PowerHelper` — sleep, hibernate, shutdown, restart, lock, battery status, prevent sleep.

### 🪟 Windows
`AppUpdater` — self-contained auto-updater (lock, download, extract, backup, replace, verify, rollback) · `AutoStartHelper` — registry-based auto-start management · `AutoUpdateHelper` — version checking and update orchestration · `DarkModeHelper` — dark/light mode for WinForms controls · `SingleInstanceHelper` — single-instance application enforcement · `TaskbarProgressHelper` — taskbar progress bar (ITaskbarList3) · `WindowManager` — window state management · `NetworkMonitorHelper` — network connectivity monitoring · `GlobalExceptionHandler` — unhandled exception capture · `FileAssociationHelper` — file type association registration · `CustomWindowHelper` — custom window chrome.

```csharp
using BPlusLib.Foundation.Windows;

// Auto-update (self-contained)
await AppUpdater.UpdateAsync("https://example.com/releases/latest.zip");

// Single instance
var single = SingleInstance.Create("MyApp");
if (!single.TryAcquire()) return;

// Dark mode for WinForms
DarkModeHelper.EnableDarkMode(myForm);
```

### 🔧 Utils
`EnvironmentHelper` — environment variable management · `FileVersionHelper` — file version info extraction.

---

## Architecture

```
BPlusLib.Foundation/
├── Common/          ← Guard, Result, Option, AsyncLock, ...
├── Native/          ← P/Invoke declarations (14 DLLs)
│   ├── Kernel32.cs
│   ├── User32.cs
│   ├── NtDll.cs
│   ├── Shell32.cs
│   ├── AdvApi32.cs
│   ├── Crypt32.cs
│   ├── WinTrust.cs
│   ├── PowrProf.cs
│   ├── PsApi.cs
│   ├── RstrtMgr.cs
│   ├── VersionApi.cs
│   └── SetupApi.cs
├── Window/          ← Monitor, Drag, Resize, Animation
├── Dialogs/         ← MessageBoxEx, InputBox, Progress
├── SystemInfo/      ← OS, CPU, Memory, Disk, Battery, ...
├── Process/         ← CommandRunner, ProcessExtensions
├── Network/         ← Ping, TCP/UDP table, ARP, DNS, WOL
├── Networking/      ← TCP/UDP sockets, HTTP/FTP client, HTTP server
│   └── Cisco/       ← RESTCONF client, YANG parser, Syslog server
├── IO/              ← SafeFileOps, Path, Checksum, FileVersion
├── Registry/        ← Registry CRUD, export/import
├── Security/        ← Token, Privilege, Integrity, UAC, Credential, WinTrust
├── Diagnostics/     ← EventLog, CrashDump
├── Logging/         ← FileLogger, LoggerFactory
├── Extensions/      ← String, Stream, Task, Collection
├── Clipboard/       ← Text, Files, Bitmap
├── Explorer/        ← Known folders, shortcuts, recycle bin
├── Cryptography/    ← AES, RSA, HMAC, PBKDF2, X.509
├── Media/           ← ID3, WAV
├── Graphics/        ← Screen, Icons, DPI
├── Shell/           ← Verbs, Associations, Theme, NotifyIcon, Shortcut
├── Device/          ← USB, Volume, SetupAPI
├── Hardware/        ← USB speed detection, VID/PID parsing
├── Localization/    ← Multi-language .resx
├── Threading/       ← STA/MTA
├── SerialPorts/     ← COM port owner identification
├── Services/        ← Service, Job Object, Console, Restart Manager
├── Input/           ← SendInput, RegisterHotKey
├── IPC/             ← Named pipes
├── Power/           ← Sleep, Hibernate, Shutdown
├── Windows/         ← AppUpdater, DarkMode, SingleInstance, Taskbar
└── Utils/           ← Environment, FileVersion
```

---

## Dependencies

| Package | Version | Scope |
|---------|---------|-------|
| Newtonsoft.Json | 13.0.3 | All TFMs (Cisco EWC module) |
| Microsoft.Bcl.HashCode | 1.1.1 | net472, net6.0 |
| System.Threading.Tasks.Extensions | 4.5.4 | net472, net6.0 |
| System.Memory | 4.5.5 | net472, net6.0 |
| System.Net.Http | 4.3.4 | net472, net6.0 |
| System.Diagnostics.EventLog | 6.0.0 | net6.0, net8.0 |

---

## Native Dependencies

All P/Invoke — no managed wrappers or external dependencies.

| DLL | Modules |
|-----|---------|
| kernel32 | Process, Handle, Console, Job Object, Pipe, Memory, Power |
| user32 | Window, Hotkey, Input, Power, Display |
| advapi32 | Service, Credential, Token, Privilege, Integrity, Shutdown |
| ntdll | SerialPorts (NtQuerySystemInformation) |
| shell32 | Explorer, Shell, Shortcut, NotifyIcon, Assoc |
| dwmapi | Theme (dark mode), Dialogs |
| wintrust | Authenticode signature verification |
| crypt32 | Certificate query for WinTrust |
| powrprof | Power (SetSuspendState) |
| rstrtmgr | Restart Manager API |
| psapi | Memory (GetProcessMemoryInfo) |
| version | PE file version info |
| shcore | Graphics (DPI) |
| netapi32 | Environment (domain join) |
| shlwapi | Shell (AssocQueryString) |
| setupapi | Hardware (device enumeration, USB speed) |

---

## Building

```bash
dotnet restore
dotnet build
dotnet test --framework net8.0
dotnet pack -c Release
```

## NuGet (GitHub Packages)

```
Source: https://nuget.pkg.github.com/thanhbinhqs/index.json
Package: BPlusLib.Foundation
Version: 2.8.0
```

```bash
dotnet add package BPlusLib.Foundation --version 2.8.0 --source "https://nuget.pkg.github.com/thanhbinhqs/index.json"
```

## License

MIT — see [LICENSE](LICENSE).
