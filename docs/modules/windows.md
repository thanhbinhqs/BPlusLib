# Windows

High-level Windows application lifecycle utilities: dark mode styling, self-updating, global exception handling, per-user file associations, auto-start registration, GitHub-based auto-updates, network connectivity monitoring, custom window chrome, single-instance enforcement, window state persistence, and taskbar progress indicators.

## Classes

### DarkModeHelper
Applies Windows dark mode styling to WinForms controls and forms via DWM + SetWindowTheme (Windows 10 1903+).

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| IsDarkModeAvailable() | bool | Whether dark mode is supported (Win10 1903+) |
| ApplyDarkMode(form, recursive?) | bool | Applies dark mode to a form and optionally all children |
| RemoveDarkMode(form, recursive?) | bool | Removes dark mode from a form |
| ApplyDarkMode(control) | bool | Applies dark mode to a single control |
| RemoveDarkMode(control) | bool | Removes dark mode from a single control |
| GetDarkBackColor() | Color | Recommended dark background color |
| GetDarkForeColor() | Color | Recommended dark foreground color |

### AppUpdater
Self-contained self-update helper. Handles check → download → backup → replace → restart.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| UpdateAsync(apiUrl, currentVersion?, progress?, ct?) | Task\<UpdateResult\> | Performs complete self-update |
| CheckForUpdateAsync(apiUrl, currentVersion?) | Task\<AppUpdateInfo?\> | Checks API for updates |
| IsUpdateAvailable(currentVersion, latestVersion) | bool | Compares version strings |
| DownloadAsync(url, targetPath, progress?, ct?) | Task\<bool\> | Downloads a file with progress |
| Extract(zipPath, extractPath) | bool | Extracts a zip file |
| Cleanup() | void | Cleans up temp files |
| GetCurrentVersion() | Version? | Auto-detects current app version |

### GlobalExceptionHandler
Singleton that catches unhandled exceptions across all threads and AppDomains.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Instance | GlobalExceptionHandler | Singleton instance |
| UnhandledException | event\<EventHandler\<CrashReport\>\> | Fires when an unhandled exception is caught |
| IsHandling | bool | Whether the handler is active |
| DumpDirectory | string? | Directory for crash report files |
| Enable() | bool | Subscribes to AppDomain and TaskScheduler exception events |
| Disable() | bool | Unsubscribes from exception events |
| CreateCrashReport(ex) | CrashReport | Creates a crash report from an exception |
| SaveCrashReport(report, path) | bool | Saves a crash report to a file |

### CrashReport
Information about an unhandled exception crash.

| Property | Type | Description |
|----------|------|-------------|
| Timestamp | DateTime | When the crash occurred |
| ExceptionType | string | Exception type name |
| Message | string | Exception message |
| StackTrace | string | Full stack trace |
| InnerException | string? | Inner exception details |
| SystemInfo | Dictionary\<string, string\> | Collected system information |
| DumpPath | string? | Path to minidump file |
| AppVersion | string? | Application version |

### FileAssociationHelper
Per-user file extension association registration via HKCU (no admin needed).

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Register(association) | bool | Registers a file association for the current user |
| Unregister(extension, progId) | bool | Removes a file extension association |
| IsRegistered(extension) | bool | Whether an extension is registered |
| GetAssociation(extension) | FileAssociation? | Gets the association details for an extension |

### FileAssociation
File type association to register with Windows.

| Property | Type | Description |
|----------|------|-------------|
| Extension | string | File extension (e.g. ".txt") |
| ProgId | string | ProgId to register |
| Description | string | Human-readable description |
| ExecutablePath | string | Full path to executable |
| IconPath | string | Optional icon file path |
| IconIndex | int | Optional icon index |

### AutoStartHelper
Registers/unregisters an application in Windows startup via HKCU Run key.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Enable(appName, executablePath?, args?) | bool | Registers app to start with Windows |
| Disable(appName) | bool | Removes app from startup |
| IsEnabled(appName) | bool | Whether app is registered in startup |
| GetCommand(appName) | string? | Gets the command line from startup |
| EnableCurrentUser(appName, args?) | bool | Convenience: registers with current user |
| RemoveFromStartup(appName) | bool | Removes from startup |

### AutoUpdateHelper
Auto-update functionality via GitHub Releases API.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| CheckForUpdateAsync(owner, repo, currentVersion?) | Task\<UpdateInfo?\> | Checks latest GitHub release |
| IsUpdateAvailable(currentVersion, latestVersion) | bool | Compares version strings |
| DownloadUpdateAsync(downloadUrl, targetPath, progress?, ct?) | Task\<bool\> | Downloads update with progress |
| LaunchInstaller(installerPath) | bool | Launches an installer file |

### UpdateInfo
Information about an available update.

| Property | Type | Description |
|----------|------|-------------|
| Version | string | Version string (e.g. "v2.7.0") |
| ReleaseNotes | string | Release notes in markdown |
| PublishedAt | DateTime | When the release was published |
| DownloadUrl | string | Direct download URL |
| FileSize | long | Asset file size in bytes |
| IsNewerThanCurrent | bool | Whether newer than current version |

### NetworkMonitor
Monitors network connectivity changes via polling.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| StatusChanged | event\<EventHandler\<NetworkChangeEventArgs\>\> | Fires when network status changes |
| CurrentStatus | NetworkStatus | Current network status |
| IsConnected | bool | Whether connected to any network |
| IsMonitoring | bool | Whether monitoring is active |
| Start(pollIntervalMs?) | bool | Starts monitoring (default: 2s interval) |
| Stop() | bool | Stops monitoring |
| IsNetworkAvailable() | bool | Static: checks network availability |
| GetActiveInterfaceCount() | int | Static: number of active interfaces |
| GetOperationalInterfaceCount() | int | Static: number of operational interfaces |

### CustomWindowHelper
Custom window chrome with Aero snap support.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| HandleNCHitTest(hwnd, screenX, screenY, borderSize?) | int | Handles WM_NCHITTEST for custom chrome |
| ApplyDwmFrame(hwnd, extendIntoClientArea?) | bool | Applies DWM extended frame for glass effect |
| DisableDwmNcRendering(hwnd) | bool | Disables DWM non-client rendering |
| EnableCustomChrome(hwnd, borderWidth?) | bool | Applies all custom chrome settings |
| ScreenToClient(hwnd, screenX, screenY) | Point | Converts screen coordinates to client coordinates |

### SingleInstanceHelper
Named-mutex-based single-instance guard.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| IsAlreadyRunning(appName, global?) | bool | Probes if another instance is running |
| Acquire(appName, global?) | SingleInstanceGuard? | Tries to acquire the mutex (null if already running) |

### SingleInstanceGuard
Disposable guard holding the single-instance mutex.

| Property | Type | Description |
|----------|------|-------------|
| IsNewInstance | bool | Whether this is the first/only instance |

### WindowManager
Window state persistence via Windows registry.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Save(hwnd, settings, key?) | bool | Saves explicit window settings |
| Restore(hwnd, key?) | WindowSettings? | Loads saved settings from registry |
| Delete(key?) | bool | Deletes saved settings |
| Save(form, key?) | bool | Saves WinForms form state (FEATURE_WINDOW_MODULE) |
| Restore(form, key?) | WindowSettings? | Restores WinForms form state |
| SaveAll(forms) | bool | Saves multiple forms |
| RestoreAll(forms) | bool | Restores multiple forms |

### WindowSettings
Saved state of a window's position, size, and state.

| Property | Type | Description |
|----------|------|-------------|
| X | int | X position |
| Y | int | Y position |
| Width | int | Width in pixels |
| Height | int | Height in pixels |
| IsMaximized | bool | Whether window was maximized |
| IsMinimized | bool | Whether window was minimized |

### TaskbarProgressHelper
Shows progress in the Windows taskbar button (Windows 7+).

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| SetProgress(hwnd, completed, total) | bool | Sets progress value |
| SetState(hwnd, state) | bool | Sets progress state |
| ClearProgress(hwnd) | bool | Clears the progress indicator |
| SetProgress(form, percent) | bool | WinForms convenience: set by percentage |
| SetState(form, state) | bool | WinForms convenience: set state |
| ClearProgress(form) | bool | WinForms convenience: clear progress |

## Enums

| Enum | Values | Description |
|------|--------|-------------|
| NetworkStatus | Unknown, Connected, Disconnected, ConnectedAsLAN, ConnectedAsWiFi | Network connectivity status |
| TaskbarProgressState | None, Indeterminate, Normal, Error, Paused | Taskbar progress states |

## Usage

```csharp
using BPlusLib.Foundation.Windows;

// Dark mode
if (DarkModeHelper.IsDarkModeAvailable())
    DarkModeHelper.ApplyDarkMode(form);

// Auto-start
AutoStartHelper.Enable("MyApp", args: "--minimized");
bool isAutoStart = AutoStartHelper.IsEnabled("MyApp");

// Single instance
using var guard = SingleInstanceHelper.Acquire("MyApp");
if (guard == null)
{
    Console.WriteLine("Another instance is already running.");
    return;
}

// Global exception handling
GlobalExceptionHandler.Instance.DumpDirectory = @"C:\Crashes";
GlobalExceptionHandler.Instance.UnhandledException += (s, report) =>
    Console.WriteLine($"Crash: {report.ExceptionType}: {report.Message}");
GlobalExceptionHandler.Instance.Enable();

// Window state persistence
WindowManager.Save(form, "MainWindow");
var settings = WindowManager.Restore(form, "MainWindow");

// Taskbar progress
TaskbarProgressHelper.SetProgress(form, 75); // 75%
TaskbarProgressHelper.SetState(form, TaskbarProgressState.Normal);
TaskbarProgressHelper.ClearProgress(form);

// Network monitoring
using var monitor = new NetworkMonitor();
monitor.StatusChanged += (s, e) =>
    Console.WriteLine($"Network: {e.Status} (interfaces: {e.InterfaceCount})");
monitor.Start();

// File association (per-user)
FileAssociationHelper.Register(new FileAssociation
{
    Extension = ".myapp",
    ProgId = "MyApp.Document",
    Description = "MyApp Document",
    ExecutablePath = @"C:\MyApp.exe"
});

// GitHub auto-update
var update = await AutoUpdateHelper.CheckForUpdateAsync("owner", "repo");
if (update?.IsNewerThanCurrent == true)
    await AutoUpdateHelper.DownloadUpdateAsync(update.DownloadUrl, @"C:\update.zip");
```

## Dependencies
- `dwmapi.dll` — DwmSetWindowAttribute, DwmExtendFrameIntoClientArea
- `uxtheme.dll` — SetWindowTheme
- `user32.dll` — SystemParametersInfo, GetWindowRect, ScreenToClient, GetDesktopWindow
- `shell32.dll` — ITaskbarList3 COM
- `ole32.dll` — CoCreateInstance (for COM interfaces)
- `System.Net.Http` — HttpClient (for auto-update)
- `System.Text.Json` — JSON deserialization (for GitHub API)
- `System.IO.Compression` — ZipFile extraction (for self-update)
- `Microsoft.Win32` — Registry operations
- `BPlusLib.Foundation.Native` — Shared P/Invoke declarations
