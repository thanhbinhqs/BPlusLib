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

| # | Module | Description | Docs |
|---|--------|-------------|------|
| 1 | 🔧 [Common](#-common) | `Guard`, `Result<T>`, `Option<T>`, `AsyncLock`, `AsyncCache`, `RetryPolicy`, `Debounce`, `ObjectPool<T>`, `DisposableBase`, `ObservableObject` | [→ docs](docs/modules/common.md) |
| 2 | 🪟 [Window](#-window) | `MonitorHelper`, `DragMoveHelper`, `ResizeHelper`, `WindowPositionManager`, `WindowAnimation` | [→ docs](docs/modules/window.md) |
| 3 | 💬 [Dialogs](#-dialogs) | `MessageBoxEx`, `InputBoxEx`, `ProgressDialog` | [→ docs](docs/modules/dialogs.md) |
| 4 | 🖥️ [System Info](#️-system-info) | `CpuInfo`, `MemoryInfo`, `DiskInfo`, `BatteryInfo`, `NetworkAdapterInfo`, `BiosInfo`, `OperatingSystemInfo` | [→ docs](docs/modules/systeminfo.md) |
| 5 | ⚙️ [Process](#️-process) | `CommandRunner`, `ProcessExtensions` | [→ docs](docs/modules/process.md) |
| 6 | 🌐 [Network](#-network) | `NetHelper` — Ping, TCP/UDP table, ARP, DNS, WOL, VPN | [→ docs](docs/modules/network.md) |
| 7 | 🔌 [Networking](#-networking) | `TcpConnection`, `TcpServer`, `TcpSocketHelper`, `UdpEndpoint`, `UdpSocketHelper`, `NetClientHelper`, `HttpListenerHelper` | [→ docs](docs/modules/networking.md) |
| 8 | 🐦 [Cisco EWC](#-cisco-ewc) | `CiscoEwcHelper`, `RestConfClient`, `YangParser`, `SyslogServer` | [→ docs](docs/modules/cisco-ewc.md) |
| 9 | 📁 [IO](#-io) | `SafeFileOps`, `PathHelper`, `ChecksumHelper`, `FileVersionHelper` | [→ docs](docs/modules/io.md) |
| 10 | 🗄️ [Registry](#️-registry) | `RegistryHelper` — CRUD, export/import .reg, backup/restore | [→ docs](docs/modules/registry.md) |
| 11 | 🔒 [Security](#-security) | `UacHelper`, `CredentialHelper`, `WinTrustHelper`, `TokenHelper`, `PrivilegeHelper`, `IntegrityHelper`, `SecurityHelper` | [→ docs](docs/modules/security.md) |
| 12 | 📊 [Diagnostics](#️-diagnostics) | `EventLogReader`, `CrashDumpHelper` | [→ docs](docs/modules/diagnostics.md) |
| 13 | 📝 [Logging](#️-logging) | `FileLogger`, `LoggerFactory`, `LogEntry` | [→ docs](docs/modules/logging.md) |
| 14 | 🧩 [Extensions](#-extensions) | `StringExtensions`, `StreamExtensions`, `TaskExtensions`, `CollectionExtensions` | [→ docs](docs/modules/extensions.md) |
| 15 | 📋 [Clipboard](#-clipboard) | `ClipboardHelper` — text, files, bitmap | [→ docs](docs/modules/clipboard.md) |
| 16 | 📂 [Explorer](#-explorer) | `ExplorerHelper` — known folders, shortcuts, recycle bin | [→ docs](docs/modules/explorer.md) |
| 17 | 🔐 [Cryptography](#-cryptography) | `CryptoHelper` — AES, RSA, HMAC, PBKDF2, X.509 | [→ docs](docs/modules/cryptography.md) |
| 18 | 🎵 [Media](#️-media) | `MediaInfoReader` — ID3v1/v2 tags, RIFF/WAV headers | [→ docs](docs/modules/media.md) |
| 19 | 🖼️ [Graphics](#️-graphics) | `ScreenHelper`, `IconExtractor`, `DisplayHelper`, `CircularProgressBar` | [→ docs](docs/modules/graphics.md) |
| 20 | 🐚 [Shell](#-shell) | `ShellHelper`, `ShortcutHelper`, `NotifyIconHelper`, `AssocHelper`, `ThemeHelper` | [→ docs](docs/modules/shell.md) |
| 21 | 📱 [Device](#️-device) | `DeviceHelper` — USB detection, volume info, SetupAPI | [→ docs](docs/modules/device.md) |
| 22 | 🔧 [Hardware](#-hardware) | `HardwareDeviceHelper` — USB speed, VID/PID, Serial | [→ docs](docs/modules/hardware.md) |
| 23 | 🌍 [Localization](#️-localization) | `LocalizationHelper` — multi-language .resx | [→ docs](docs/modules/localization.md) |
| 24 | 🧵 [Threading](#️-threading) | `ThreadHelper` — STA/MTA, COM apartment | [→ docs](docs/modules/threading.md) |
| 25 | 🔌 [Serial Ports](#-serial-ports) | `SerialPortInspector` — COM port owner (NtQuerySystemInformation) | [→ docs](docs/modules/serialports.md) |
| 26 | ⚙️ [Services](#️-services) | `ServiceHelper`, `JobObjectHelper`, `ConsoleHelper`, `RestartManagerHelper` | [→ docs](docs/modules/services.md) |
| 27 | ⌨️ [Input](#️-input) | `InputHelper`, `HotkeyHelper` | [→ docs](docs/modules/input.md) |
| 28 | 🔗 [IPC](#-ipc) | `PipeServer`, `PipeClient`, `PipeHelper` | [→ docs](docs/modules/ipc.md) |
| 29 | ⚡ [Power](#️-power) | `PowerHelper` — sleep, hibernate, shutdown, restart, lock | [→ docs](docs/modules/power.md) |
| 30 | 🪟 [Windows](#️-windows) | `AppUpdater`, `DarkModeHelper`, `SingleInstanceHelper`, `TaskbarProgressHelper`, `AutoStartHelper`, `AutoUpdateHelper`, `GlobalExceptionHandler`, `FileAssociationHelper`, `CustomWindowHelper`, `WindowManager`, `NetworkMonitorHelper` | [→ docs](docs/modules/windows.md) |
| 31 | 🔧 [Utils](#-utils) | `EnvironmentHelper`, `FileVersionHelper` | — |

---

### 🔧 Common
Foundational types: `Guard`, `Result<T>`, `Option<T>`, `AsyncLock`, `AsyncCache`, `RetryPolicy`, `Debounce`, `ObjectPool<T>`, `DisposableBase`, `ObservableObject`, and polyfills. [**→ Full Documentation**](docs/modules/common.md)

### 🪟 Window
`MonitorHelper` — monitor enumeration & info · `DragMoveHelper` — borderless drag-move · `ResizeHelper` — borderless resize · `WindowPositionManager` — save/restore positions · `WindowAnimation` — fade/slide animations. [**→ Full Documentation**](docs/modules/window.md)

### 💬 Dialogs
`MessageBoxEx` — async message box with WH_CBT hook, dark mode, timeout · `InputBoxEx` — text input dialog · `ProgressDialog` — modal progress bar. [**→ Full Documentation**](docs/modules/dialogs.md)

### 🖥️ System Info
`OperatingSystemInfo` — name, version, architecture, boot time · `CpuInfo` — cores, speed, virtualization · `MemoryInfo` — physical, virtual, page file · `DiskInfo` — drive info, free space · `NetworkAdapterInfo` — MAC, IP, speed · `BatteryInfo` — charge, state · `BiosInfo` — vendor, version · `EnvironmentHelper` — env vars, PATH, domain · `MemoryHelper` — memory-mapped files, process memory. [**→ Full Documentation**](docs/modules/systeminfo.md)

### ⚙️ Process
`CommandRunner` — async process execution with stdout/stderr capture · `ProcessExtensions` — elevation, image path, command line, kill tree, wait async. [**→ Full Documentation**](docs/modules/process.md)

### 🌐 Network
`NetHelper` — Ping, TCP/UDP table, ARP cache, DNS lookup, Wake-on-LAN, VPN detection, routing. [**→ Full Documentation**](docs/modules/network.md)

### 🔌 Networking
`TcpConnection` — full-duplex TCP wrapper · `TcpServer` — async accept · `TcpSocketHelper` — static connect/start · `UdpEndpoint` — send/receive/broadcast/multicast · `UdpSocketHelper` — static one-shot · `NetClientHelper` — HTTP GET/POST/PUT/DELETE, FTP · `HttpListenerHelper` — embedded localhost HTTP server. [**→ Full Documentation**](docs/modules/networking.md)

### 🐦 Cisco EWC
`CiscoEwcHelper` — static facade for Cisco EWC via RESTCONF (RFC 8040) · `RestConfClient` — HTTPS client with Basic auth, Newtonsoft.Json · `YangParser` — parse YANG model JSON responses (device info, APs, clients, SSIDs, **RF radio data**, **RF profiles**, **AP profiles**) · `SyslogServer` — UDP syslog listener (RFC 5424) with real-time callbacks. [**→ Full Documentation**](docs/modules/cisco-ewc.md)

### 📁 IO
`SafeFileOps` — atomic write, retry copy/move, lock detection · `PathHelper` — sanitize, normalize, relative paths · `ChecksumHelper` — MD5/SHA1/SHA256/SHA512/CRC32 · `FileVersionHelper` — PE version resources (VerQueryValue). [**→ Full Documentation**](docs/modules/io.md)

### 🗄️ Registry
`RegistryHelper` — CRUD values, export/import .reg, backup/restore, typed getters. [**→ Full Documentation**](docs/modules/registry.md)

### 🔒 Security
`TokenHelper` — token queries, groups, statistics · `PrivilegeHelper` — enable/disable privileges · `IntegrityHelper` — integrity level get/set · `SecurityHelper` — high-level security checks · `UacHelper` — elevation detection, runas, integrity level · `CredentialHelper` — Windows Credential Manager (read/write/enumerate/delete) · `WinTrustHelper` — Authenticode signature verification. [**→ Full Documentation**](docs/modules/security.md)

### 📊 Diagnostics
`EventLogReader` — Windows Event Log reading · `CrashDumpHelper` — MiniDump/FullDump creation. [**→ Full Documentation**](docs/modules/diagnostics.md)

### 📝 Logging
`FileLogger` — rolling file log writer · `LoggerFactory` — centralized logger creation · `LogEntry` — log entry model. [**→ Full Documentation**](docs/modules/logging.md)

### 🧩 Extensions
`StringExtensions` — email validation, truncation, ToTitleCase, slug · `StreamExtensions` — read exact bytes, read-all · `TaskExtensions` — WithTimeout, WithRetry, FireAndForget · `CollectionExtensions` — AddRange, Batch, DistinctBy, Shuffle. [**→ Full Documentation**](docs/modules/extensions.md)

### 📋 Clipboard
`ClipboardHelper` — text, files, bitmap clipboard operations. [**→ Full Documentation**](docs/modules/clipboard.md)

### 📂 Explorer
`ExplorerHelper` — known folders, folder picker, shortcut management, recycle bin. [**→ Full Documentation**](docs/modules/explorer.md)

### 🔐 Cryptography
`CryptoHelper` — AES encrypt/decrypt, RSA key pair, HMAC, PBKDF2, self-signed certificates, X.509. [**→ Full Documentation**](docs/modules/cryptography.md)

### 🎵 Media
`MediaInfoReader` — ID3v1/v2 tag reading, RIFF/WAV header parsing. [**→ Full Documentation**](docs/modules/media.md)

### 🖼️ Graphics
`ScreenHelper` — multi-monitor bounds, virtual screen · `IconExtractor` — extract icons from EXE/DLL · `DisplayHelper` — DPI, resolution, color depth, high contrast. [**→ Full Documentation**](docs/modules/graphics.md)

### 🐚 Shell
`ShellHelper` — file verbs, associations, recycle bin · `ShortcutHelper` — .lnk creation/reading via IShellLink COM · `NotifyIconHelper` — system tray icon (Shell_NotifyIconW) · `AssocHelper` — file extension queries · `ThemeHelper` — dark/light mode, accent color, DWM. [**→ Full Documentation**](docs/modules/shell.md)

### 📱 Device
`DeviceHelper` — USB detection, volume info, SetupAPI device enumeration. [**→ Full Documentation**](docs/modules/device.md)

### 🔧 Hardware
`HardwareDeviceHelper` — USB speed detection (Low/Full/High/Super/SuperSpeed+), VID/PID/Serial parsing via SetupAPI · `UsbDeviceParser` — USB descriptor string parsing · `DeviceInfoParser` — device instance ID parsing. [**→ Full Documentation**](docs/modules/hardware.md)

### 🌍 Localization
`LocalizationHelper` — multi-language .resx loading, culture switching. [**→ Full Documentation**](docs/modules/localization.md)

### 🧵 Threading
`ThreadHelper` — STA/MTA thread management, COM apartment. [**→ Full Documentation**](docs/modules/threading.md)

### 🔌 Serial Ports
`SerialPortInspector` — identify which process owns each COM port (NtQuerySystemInformation). Pure P/Invoke. [**→ Full Documentation**](docs/modules/serialports.md)

### ⚙️ Services
`ServiceHelper` — start/stop/restart/pause/create/delete/query Windows services · `JobObjectHelper` — process group management (kill-on-close, process limits) · `ConsoleHelper` — console window management (show/hide, title, color) · `RestartManagerHelper` — Restart Manager API (lock detection for file updates). [**→ Full Documentation**](docs/modules/services.md)

### ⌨️ Input
`InputHelper` — keyboard/mouse simulation (SendInput, Unicode text) · `HotkeyHelper` — global hotkey registration (RegisterHotKey). [**→ Full Documentation**](docs/modules/input.md)

### 🔗 IPC
`PipeServer` / `PipeClient` — named pipe client/server with async support · `PipeHelper` — one-shot transaction. [**→ Full Documentation**](docs/modules/ipc.md)

### ⚡ Power
`PowerHelper` — sleep, hibernate, shutdown, restart, lock, battery status, prevent sleep. [**→ Full Documentation**](docs/modules/power.md)

### 🪟 Windows
`AppUpdater` — self-contained auto-updater (lock, download, extract, backup, replace, verify, rollback) · `AutoStartHelper` — registry-based auto-start management · `AutoUpdateHelper` — version checking and update orchestration · `DarkModeHelper` — dark/light mode for WinForms controls · `SingleInstanceHelper` — single-instance application enforcement · `TaskbarProgressHelper` — taskbar progress bar (ITaskbarList3) · `WindowManager` — window state management · `NetworkMonitorHelper` — network connectivity monitoring · `GlobalExceptionHandler` — unhandled exception capture · `FileAssociationHelper` — file type association registration · `CustomWindowHelper` — custom window chrome. [**→ Full Documentation**](docs/modules/windows.md)

### 🔧 Utils
`EnvironmentHelper` — environment variable management · `FileVersionHelper` — file version info extraction.

---

## Architecture

```
BPlusLib.Foundation/
├── Common/          ← Guard, Result, Option, AsyncLock, ...
├── Native/          ← P/Invoke declarations (15 DLLs)
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
│   ├── SetupApi.cs
│   └── Interop/
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
