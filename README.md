1|# BPlusLib.Foundation
2|
3|**Enterprise-grade Windows Foundation Library — 31 modules, 1,275 tests, pure P/Invoke.**
4|
5|---
6|
7|## Overview
8|
9|BPlusLib.Foundation is a production-ready C# utility library for Windows desktop development. All components use pure P/Invoke — **no WMI, no PowerShell, no external executables**. Cross-platform graceful degradation: every method returns `null`/`false` instead of throwing on non-Windows.
10|
11|| | |
12||---|---|
13|| **Targets** | net472 · net6.0 · net8.0 |
14|| **Version** | 2.10.1 |
15|| **License** | MIT |
16|| **Tests** | 1,286 (1,194 passing, 92 skipped — Windows-only) |
17|| **Author** | [thanhbinhqs](https://github.com/thanhbinhqs) |
18|
19|---
20|
21|## Quick Start
22|
23|```bash
24|dotnet add package BPlusLib.Foundation --source "https://nuget.pkg.github.com/thanhbinhqs/index.json"
25|```
26|
27|```csharp
28|// System info
29|using BPlusLib.Foundation.SystemInfo;
30|var cpu = new CpuInfo();
31|var mem = new MemoryInfo();
32|
33|// Process management
34|using BPlusLib.Foundation.Process;
35|var result = CommandRunner.RunCommand("tasklist", timeoutMs: 10000);
36|
37|// Service management
38|using BPlusLib.Foundation.Services;
39|ServiceHelper.StartService("Spooler");
40|ServiceHelper.StopService("Spooler", waitMs: 15000);
41|
42|// UAC detection
43|using BPlusLib.Foundation.Security;
44|bool elevated = UacHelper.IsElevated();
45|IntegrityLevel level = UacHelper.GetIntegrityLevel();
46|
47|// TCP/UDP communication
48|using BPlusLib.Foundation.Networking;
49|using var server = TcpSocketHelper.StartServer(port: 9000);
50|using var client = TcpSocketHelper.Connect("127.0.0.1", 9000);
51|client?.Send(Encoding.UTF8.GetBytes("Hello"));
52|
53|// Credential Manager
54|using BPlusLib.Foundation.Security;
55|CredentialHelper.Write("myapp:user", "admin", "s3cret");
56|var cred = CredentialHelper.Read("myapp:user");
57|
58|// Named pipe IPC
59|using var pipeServer = new PipeServer("BPlusLibTest");
60|pipeServer.WaitForConnection(5000);
61|pipeServer.Write(Encoding.UTF8.GetBytes("response"));
62|
63|// Auto-update
64|using BPlusLib.Foundation.Windows;
65|await AppUpdater.UpdateAsync("https://example.com/releases/latest.zip");
66|
67|// Cisco EWC (RESTCONF + Syslog)
68|using BPlusLib.Foundation.Networking.Cisco;
69|var device = await CiscoEwcHelper.GetDeviceInfoAsync("192.168.1.1", "admin", "pass");
70|var aps = await CiscoEwcHelper.GetAccessPointsAsync("192.168.1.1", "admin", "pass");
71|var clients = await CiscoEwcHelper.GetClientsAsync("192.168.1.1", "admin", "pass");
72|using var syslog = CiscoEwcHelper.StartSyslogListener(514, entry => Console.WriteLine($"[{entry.Severity}] {entry.Message}"));
73|```
74|
75|---
76|
77|## Modules
78|
79|| # | Module | Description | Docs |
80||---|--------|-------------|------|
81|| 1 | 🔧 [Common](#-common) | `Guard`, `Result<T>`, `Option<T>`, `AsyncLock`, `AsyncCache`, `RetryPolicy`, `Debounce`, `ObjectPool<T>`, `DisposableBase`, `ObservableObject` | [→ docs](docs/modules/common.md) |
82|| 2 | 🪟 [Window](#-window) | `MonitorHelper`, `DragMoveHelper`, `ResizeHelper`, `WindowPositionManager`, `WindowAnimation` | [→ docs](docs/modules/window.md) |
83|| 3 | 💬 [Dialogs](#-dialogs) | `MessageBoxEx`, `InputBoxEx`, `ProgressDialog` | [→ docs](docs/modules/dialogs.md) |
84|| 4 | 🖥️ [System Info](#️-system-info) | `CpuInfo`, `MemoryInfo`, `DiskInfo`, `BatteryInfo`, `NetworkAdapterInfo`, `BiosInfo`, `OperatingSystemInfo` | [→ docs](docs/modules/systeminfo.md) |
85|| 5 | ⚙️ [Process](#️-process) | `CommandRunner`, `ProcessExtensions` | [→ docs](docs/modules/process.md) |
86|| 6 | 🌐 [Network](#-network) | `NetHelper` — Ping, TCP/UDP table, ARP, DNS, WOL, VPN | [→ docs](docs/modules/network.md) |
87|| 7 | 🔌 [Networking](#-networking) | `TcpConnection`, `TcpServer`, `TcpSocketHelper`, `UdpEndpoint`, `UdpSocketHelper`, `NetClientHelper`, `HttpListenerHelper` | [→ docs](docs/modules/networking.md) |
88|| 8 | 🐦 [Cisco EWC](#-cisco-ewc) | `CiscoEwcHelper`, `RestConfClient`, `YangParser`, `SyslogServer` | [→ docs](docs/modules/cisco-ewc.md) |
89|| 9 | 📁 [IO](#-io) | `SafeFileOps`, `PathHelper`, `ChecksumHelper`, `FileVersionHelper` | [→ docs](docs/modules/io.md) |
90|| 10 | 🗄️ [Registry](#️-registry) | `RegistryHelper` — CRUD, export/import .reg, backup/restore | [→ docs](docs/modules/registry.md) |
91|| 11 | 🔒 [Security](#-security) | `UacHelper`, `CredentialHelper`, `WinTrustHelper`, `TokenHelper`, `PrivilegeHelper`, `IntegrityHelper`, `SecurityHelper` | [→ docs](docs/modules/security.md) |
92|| 12 | 📊 [Diagnostics](#️-diagnostics) | `EventLogReader`, `CrashDumpHelper` | [→ docs](docs/modules/diagnostics.md) |
93|| 13 | 📝 [Logging](#️-logging) | `FileLogger`, `LoggerFactory`, `LogEntry`, `NLogLogger`, `RichTextBoxLogTarget` | [→ docs](docs/modules/logging.md) |
94|| 14 | 🧩 [Extensions](#-extensions) | `StringExtensions`, `StreamExtensions`, `TaskExtensions`, `CollectionExtensions` | [→ docs](docs/modules/extensions.md) |
95|| 15 | 📋 [Clipboard](#-clipboard) | `ClipboardHelper` — text, files, bitmap | [→ docs](docs/modules/clipboard.md) |
96|| 16 | 📂 [Explorer](#-explorer) | `ExplorerHelper` — known folders, shortcuts, recycle bin | [→ docs](docs/modules/explorer.md) |
97|| 17 | 🔐 [Cryptography](#-cryptography) | `CryptoHelper` — AES, RSA, HMAC, PBKDF2, X.509 | [→ docs](docs/modules/cryptography.md) |
98|| 18 | 🎵 [Media](#️-media) | `MediaInfoReader` — ID3v1/v2 tags, RIFF/WAV headers | [→ docs](docs/modules/media.md) |
99|| 19 | 🖼️ [Graphics](#️-graphics) | `ScreenHelper`, `IconExtractor`, `DisplayHelper`, `CircularProgressBar` | [→ docs](docs/modules/graphics.md) |
100|| 20 | 🐚 [Shell](#-shell) | `ShellHelper`, `ShortcutHelper`, `NotifyIconHelper`, `AssocHelper`, `ThemeHelper` | [→ docs](docs/modules/shell.md) |
101|| 21 | 📱 [Device](#️-device) | `DeviceHelper` — USB detection, volume info, SetupAPI | [→ docs](docs/modules/device.md) |
102|| 22 | 🔧 [Hardware](#-hardware) | `HardwareDeviceHelper` — USB speed, VID/PID, Serial | [→ docs](docs/modules/hardware.md) |
103|| 23 | 🌍 [Localization](#️-localization) | `LocalizationHelper` — multi-language .resx | [→ docs](docs/modules/localization.md) |
104|| 24 | 🧵 [Threading](#️-threading) | `ThreadHelper` — STA/MTA, COM apartment | [→ docs](docs/modules/threading.md) |
105|| 25 | 🔌 [Serial Ports](#-serial-ports) | `SerialPortInspector` — COM port owner (NtQuerySystemInformation) | [→ docs](docs/modules/serialports.md) |
106|| 26 | ⚙️ [Services](#️-services) | `ServiceHelper`, `JobObjectHelper`, `ConsoleHelper`, `RestartManagerHelper` | [→ docs](docs/modules/services.md) |
107|| 27 | ⌨️ [Input](#️-input) | `InputHelper`, `HotkeyHelper` | [→ docs](docs/modules/input.md) |
108|| 28 | 🔗 [IPC](#-ipc) | `PipeServer`, `PipeClient`, `PipeHelper` | [→ docs](docs/modules/ipc.md) |
109|| 29 | ⚡ [Power](#️-power) | `PowerHelper` — sleep, hibernate, shutdown, restart, lock | [→ docs](docs/modules/power.md) |
110|| 30 | 🪟 [Windows](#️-windows) | `AppUpdater`, `DarkModeHelper`, `SingleInstanceHelper`, `TaskbarProgressHelper`, `AutoStartHelper`, `AutoUpdateHelper`, `GlobalExceptionHandler`, `FileAssociationHelper`, `CustomWindowHelper`, `WindowManager`, `NetworkMonitorHelper` | [→ docs](docs/modules/windows.md) |
111|| 31 | 🔧 [Utils](#-utils) | `EnvironmentHelper`, `FileVersionHelper` | — |
112|
113|---
114|
115|### 🔧 Common
116|Foundational types: `Guard`, `Result<T>`, `Option<T>`, `AsyncLock`, `AsyncCache`, `RetryPolicy`, `Debounce`, `ObjectPool<T>`, `DisposableBase`, `ObservableObject`, and polyfills. [**→ Full Documentation**](docs/modules/common.md)
117|
118|### 🪟 Window
119|`MonitorHelper` — monitor enumeration & info · `DragMoveHelper` — borderless drag-move · `ResizeHelper` — borderless resize · `WindowPositionManager` — save/restore positions · `WindowAnimation` — fade/slide animations. [**→ Full Documentation**](docs/modules/window.md)
120|
121|### 💬 Dialogs
122|`MessageBoxEx` — async message box with WH_CBT hook, dark mode, timeout · `InputBoxEx` — text input dialog · `ProgressDialog` — modal progress bar. [**→ Full Documentation**](docs/modules/dialogs.md)
123|
124|### 🖥️ System Info
125|`OperatingSystemInfo` — name, version, architecture, boot time · `CpuInfo` — cores, speed, virtualization · `MemoryInfo` — physical, virtual, page file · `DiskInfo` — drive info, free space · `NetworkAdapterInfo` — MAC, IP, speed · `BatteryInfo` — charge, state · `BiosInfo` — vendor, version · `EnvironmentHelper` — env vars, PATH, domain · `MemoryHelper` — memory-mapped files, process memory. [**→ Full Documentation**](docs/modules/systeminfo.md)
126|
127|### ⚙️ Process
128|`CommandRunner` — async process execution with stdout/stderr capture · `ProcessExtensions` — elevation, image path, command line, kill tree, wait async. [**→ Full Documentation**](docs/modules/process.md)
129|
130|### 🌐 Network
131|`NetHelper` — Ping, TCP/UDP table, ARP cache, DNS lookup, Wake-on-LAN, VPN detection, routing. [**→ Full Documentation**](docs/modules/network.md)
132|
133|### 🔌 Networking
134|`TcpConnection` — full-duplex TCP wrapper · `TcpServer` — async accept · `TcpSocketHelper` — static connect/start · `UdpEndpoint` — send/receive/broadcast/multicast · `UdpSocketHelper` — static one-shot · `NetClientHelper` — HTTP GET/POST/PUT/DELETE, FTP · `HttpListenerHelper` — embedded localhost HTTP server. [**→ Full Documentation**](docs/modules/networking.md)
135|
136|### 🐦 Cisco EWC
137|`CiscoEwcHelper` — static facade for Cisco EWC via RESTCONF (RFC 8040) · `RestConfClient` — HTTPS client with Basic auth, Newtonsoft.Json · `YangParser` — parse YANG model JSON responses (device info, APs, clients, SSIDs, **RF radio data**, **RF profiles**, **AP profiles**) · `SyslogServer` — UDP syslog listener (RFC 5424) with real-time callbacks. [**→ Full Documentation**](docs/modules/cisco-ewc.md)
138|
139|### 📁 IO
140|`SafeFileOps` — atomic write, retry copy/move, lock detection · `PathHelper` — sanitize, normalize, relative paths · `ChecksumHelper` — MD5/SHA1/SHA256/SHA512/CRC32 · `FileVersionHelper` — PE version resources (VerQueryValue). [**→ Full Documentation**](docs/modules/io.md)
141|
142|### 🗄️ Registry
143|`RegistryHelper` — CRUD values, export/import .reg, backup/restore, typed getters. [**→ Full Documentation**](docs/modules/registry.md)
144|
145|### 🔒 Security
146|`TokenHelper` — token queries, groups, statistics · `PrivilegeHelper` — enable/disable privileges · `IntegrityHelper` — integrity level get/set · `SecurityHelper` — high-level security checks · `UacHelper` — elevation detection, runas, integrity level · `CredentialHelper` — Windows Credential Manager (read/write/enumerate/delete) · `WinTrustHelper` — Authenticode signature verification. [**→ Full Documentation**](docs/modules/security.md)
147|
148|### 📊 Diagnostics
149|`EventLogReader` — Windows Event Log reading · `CrashDumpHelper` — MiniDump/FullDump creation. [**→ Full Documentation**](docs/modules/diagnostics.md)
150|
151|### 📝 Logging
152|`FileLogger` — rolling file log writer · `LoggerFactory` — centralized logger creation · `LogEntry` — log entry model. [**→ Full Documentation**](docs/modules/logging.md)
153|
154|### 🧩 Extensions
155|`StringExtensions` — email validation, truncation, ToTitleCase, slug · `StreamExtensions` — read exact bytes, read-all · `TaskExtensions` — WithTimeout, WithRetry, FireAndForget · `CollectionExtensions` — AddRange, Batch, DistinctBy, Shuffle. [**→ Full Documentation**](docs/modules/extensions.md)
156|
157|### 📋 Clipboard
158|`ClipboardHelper` — text, files, bitmap clipboard operations. [**→ Full Documentation**](docs/modules/clipboard.md)
159|
160|### 📂 Explorer
161|`ExplorerHelper` — known folders, folder picker, shortcut management, recycle bin. [**→ Full Documentation**](docs/modules/explorer.md)
162|
163|### 🔐 Cryptography
164|`CryptoHelper` — AES encrypt/decrypt, RSA key pair, HMAC, PBKDF2, self-signed certificates, X.509. [**→ Full Documentation**](docs/modules/cryptography.md)
165|
166|### 🎵 Media
167|`MediaInfoReader` — ID3v1/v2 tag reading, RIFF/WAV header parsing. [**→ Full Documentation**](docs/modules/media.md)
168|
169|### 🖼️ Graphics
170|`ScreenHelper` — multi-monitor bounds, virtual screen · `IconExtractor` — extract icons from EXE/DLL · `DisplayHelper` — DPI, resolution, color depth, high contrast. [**→ Full Documentation**](docs/modules/graphics.md)
171|
172|### 🐚 Shell
173|`ShellHelper` — file verbs, associations, recycle bin · `ShortcutHelper` — .lnk creation/reading via IShellLink COM · `NotifyIconHelper` — system tray icon (Shell_NotifyIconW) · `AssocHelper` — file extension queries · `ThemeHelper` — dark/light mode, accent color, DWM. [**→ Full Documentation**](docs/modules/shell.md)
174|
175|### 📱 Device
176|`DeviceHelper` — USB detection, volume info, SetupAPI device enumeration. [**→ Full Documentation**](docs/modules/device.md)
177|
178|### 🔧 Hardware
179|`HardwareDeviceHelper` — USB speed detection (Low/Full/High/Super/SuperSpeed+), VID/PID/Serial parsing via SetupAPI · `UsbDeviceParser` — USB descriptor string parsing · `DeviceInfoParser` — device instance ID parsing. [**→ Full Documentation**](docs/modules/hardware.md)
180|
181|### 🌍 Localization
182|`LocalizationHelper` — multi-language .resx loading, culture switching. [**→ Full Documentation**](docs/modules/localization.md)
183|
184|### 🧵 Threading
185|`ThreadHelper` — STA/MTA thread management, COM apartment. [**→ Full Documentation**](docs/modules/threading.md)
186|
187|### 🔌 Serial Ports
188|`SerialPortInspector` — identify which process owns each COM port (NtQuerySystemInformation). Pure P/Invoke. [**→ Full Documentation**](docs/modules/serialports.md)
189|
190|### ⚙️ Services
191|`ServiceHelper` — start/stop/restart/pause/create/delete/query Windows services · `JobObjectHelper` — process group management (kill-on-close, process limits) · `ConsoleHelper` — console window management (show/hide, title, color) · `RestartManagerHelper` — Restart Manager API (lock detection for file updates). [**→ Full Documentation**](docs/modules/services.md)
192|
193|### ⌨️ Input
194|`InputHelper` — keyboard/mouse simulation (SendInput, Unicode text) · `HotkeyHelper` — global hotkey registration (RegisterHotKey). [**→ Full Documentation**](docs/modules/input.md)
195|
196|### 🔗 IPC
197|`PipeServer` / `PipeClient` — named pipe client/server with async support · `PipeHelper` — one-shot transaction. [**→ Full Documentation**](docs/modules/ipc.md)
198|
199|### ⚡ Power
200|`PowerHelper` — sleep, hibernate, shutdown, restart, lock, battery status, prevent sleep. [**→ Full Documentation**](docs/modules/power.md)
201|
202|### 🪟 Windows
203|`AppUpdater` — self-contained auto-updater (lock, download, extract, backup, replace, verify, rollback) · `AutoStartHelper` — registry-based auto-start management · `AutoUpdateHelper` — version checking and update orchestration · `DarkModeHelper` — dark/light mode for WinForms controls · `SingleInstanceHelper` — single-instance application enforcement · `TaskbarProgressHelper` — taskbar progress bar (ITaskbarList3) · `WindowManager` — window state management · `NetworkMonitorHelper` — network connectivity monitoring · `GlobalExceptionHandler` — unhandled exception capture · `FileAssociationHelper` — file type association registration · `CustomWindowHelper` — custom window chrome. [**→ Full Documentation**](docs/modules/windows.md)
204|
205|### 🔧 Utils
206|`EnvironmentHelper` — environment variable management · `FileVersionHelper` — file version info extraction.
207|
208|---
209|
210|## Architecture
211|
212|```
213|BPlusLib.Foundation/
214|├── Common/          ← Guard, Result, Option, AsyncLock, ...
215|├── Native/          ← P/Invoke declarations (15 DLLs)
216|│   ├── Kernel32.cs
217|│   ├── User32.cs
218|│   ├── NtDll.cs
219|│   ├── Shell32.cs
220|│   ├── AdvApi32.cs
221|│   ├── Crypt32.cs
222|│   ├── WinTrust.cs
223|│   ├── PowrProf.cs
224|│   ├── PsApi.cs
225|│   ├── RstrtMgr.cs
226|│   ├── VersionApi.cs
227|│   ├── SetupApi.cs
228|│   └── Interop/
229|├── Window/          ← Monitor, Drag, Resize, Animation
230|├── Dialogs/         ← MessageBoxEx, InputBox, Progress
231|├── SystemInfo/      ← OS, CPU, Memory, Disk, Battery, ...
232|├── Process/         ← CommandRunner, ProcessExtensions
233|├── Network/         ← Ping, TCP/UDP table, ARP, DNS, WOL
234|├── Networking/      ← TCP/UDP sockets, HTTP/FTP client, HTTP server
235|│   └── Cisco/       ← RESTCONF client, YANG parser, Syslog server
236|├── IO/              ← SafeFileOps, Path, Checksum, FileVersion
237|├── Registry/        ← Registry CRUD, export/import
238|├── Security/        ← Token, Privilege, Integrity, UAC, Credential, WinTrust
239|├── Diagnostics/     ← EventLog, CrashDump
240|├── Logging/         ← FileLogger, LoggerFactory, NLogLogger, RichTextBoxLogTarget
241|├── Extensions/      ← String, Stream, Task, Collection
242|├── Clipboard/       ← Text, Files, Bitmap
243|├── Explorer/        ← Known folders, shortcuts, recycle bin
244|├── Cryptography/    ← AES, RSA, HMAC, PBKDF2, X.509
245|├── Media/           ← ID3, WAV
246|├── Graphics/        ← Screen, Icons, DPI
247|├── Shell/           ← Verbs, Associations, Theme, NotifyIcon, Shortcut
248|├── Device/          ← USB, Volume, SetupAPI
249|├── Hardware/        ← USB speed detection, VID/PID parsing
250|├── Localization/    ← Multi-language .resx
251|├── Threading/       ← STA/MTA
252|├── SerialPorts/     ← COM port owner identification
253|├── Services/        ← Service, Job Object, Console, Restart Manager
254|├── Input/           ← SendInput, RegisterHotKey
255|├── IPC/             ← Named pipes
256|├── Power/           ← Sleep, Hibernate, Shutdown
257|├── Windows/         ← AppUpdater, DarkMode, SingleInstance, Taskbar
258|└── Utils/           ← Environment, FileVersion
259|```
260|
261|---
262|
263|## Dependencies
264|
265|| Package | Version | Scope |
266||---------|---------|-------|
267|| Newtonsoft.Json | 13.0.3 | All TFMs (Cisco EWC module) |
268|| Microsoft.Bcl.HashCode | 1.1.1 | net472, net6.0 |
269|| System.Threading.Tasks.Extensions | 4.5.4 | net472, net6.0 |
270|| System.Memory | 4.5.5 | net472, net6.0 |
271|| System.Net.Http | 4.3.4 | net472, net6.0 |
272|| System.Diagnostics.EventLog | 6.0.0 | net6.0, net8.0 |
| NLog | 5.3.4 | All TFMs (logging module) |
273|
274|---
275|
276|## Native Dependencies
277|
278|All P/Invoke — no managed wrappers or external dependencies.
279|
280|| DLL | Modules |
281||-----|---------|
282|| kernel32 | Process, Handle, Console, Job Object, Pipe, Memory, Power |
283|| user32 | Window, Hotkey, Input, Power, Display |
284|| advapi32 | Service, Credential, Token, Privilege, Integrity, Shutdown |
285|| ntdll | SerialPorts (NtQuerySystemInformation) |
286|| shell32 | Explorer, Shell, Shortcut, NotifyIcon, Assoc |
287|| dwmapi | Theme (dark mode), Dialogs |
288|| wintrust | Authenticode signature verification |
289|| crypt32 | Certificate query for WinTrust |
290|| powrprof | Power (SetSuspendState) |
291|| rstrtmgr | Restart Manager API |
292|| psapi | Memory (GetProcessMemoryInfo) |
293|| version | PE file version info |
294|| shcore | Graphics (DPI) |
295|| netapi32 | Environment (domain join) |
296|| shlwapi | Shell (AssocQueryString) |
297|| setupapi | Hardware (device enumeration, USB speed) |
298|
299|---
300|
301|## Building
302|
303|```bash
304|dotnet restore
305|dotnet build
306|dotnet test --framework net8.0
307|dotnet pack -c Release
308|```
309|
310|## NuGet (GitHub Packages)
311|
312|```
313|Source: https://nuget.pkg.github.com/thanhbinhqs/index.json
314|Package: BPlusLib.Foundation
315|Version: 2.8.0
316|```
317|
318|```bash
319|dotnet add package BPlusLib.Foundation --version 2.10.1 --source "https://nuget.pkg.github.com/thanhbinhqs/index.json"
320|```
321|
322|## License
323|
324|MIT — see [LICENSE](LICENSE).
325|