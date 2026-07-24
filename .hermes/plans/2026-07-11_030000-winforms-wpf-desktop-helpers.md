# Windows Desktop Helpers — Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add 10 practical Windows desktop helpers specifically designed for WinForms/WPF application development, filling the most common gaps in day-to-day .NET desktop work.

**Architecture:** Each helper is a self-contained static class or sealed class in its own namespace under `BPlusLib.Foundation.Windows`. Uses pure P/Invoke (no WMI, no PowerShell). All methods are thread-safe and return `null`/`false` instead of throwing on error. Follows the existing pattern: internal P/Invoke declarations, public API with XML docs, `[SkippableFact]` tests.

**Tech Stack:** C# 12, net472;net6.0;net8.0, xUnit + FluentAssertions, P/Invoke into kernel32/user32/dwmapi/shell32/advapi32.

---

## Rationale — What's Missing for WinForms/WPF

### Already covered by existing 26 modules:
- ✅ Monitor info, DPI queries, screen resolution (`DisplayHelper`, `MonitorHelper`)
- ✅ Dark/light theme detection (`ThemeHelper`)
- ✅ Global hotkeys (`HotkeyHelper`)
- ✅ Input simulation (`InputHelper`)
- ✅ System tray icons (`NotifyIconHelper`)
- ✅ Service management (`ServiceHelper`)
- ✅ Single-process elevation (`UacHelper`)
- ✅ Registry CRUD (`RegistryHelper`)
- ✅ Crash dumps (`CrashDumpHelper`)
- ✅ Named pipes IPC (`PipeServer`/`PipeClient`)

### NOT covered — what desktop developers need daily:
1. **Taskbar progress** — show download/install progress in taskbar button
2. **Window state persistence** — save/restore size/position across sessions
3. **Auto-update** — check GitHub Releases, download, install
4. **Single-instance app** — named mutex pattern
5. **Dark mode for controls** — apply dark mode to WinForms controls
6. **Custom window chrome** — Aero snap, resize, hit-test
7. **Network connectivity monitor** — detect online/offline changes
8. **File association registration** — register app as handler for file types
9. **Auto-start management** — register/unregister startup entry
10. **Global exception handler** — unhandled exception catching + crash report

---

## Task 1: TaskbarProgressHelper

**Objective:** Show progress in the taskbar button (Windows 7+ ITaskbarList3).

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/TaskbarProgressHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/TaskbarProgressHelperTests.cs`

**P/Invoke required (add to `Native/Shell32.cs`):**
```csharp
// ITaskbarList3 COM interface
[ComImport, Guid("ea1afb91-9e28-4b86-90c9-99e552575ef7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITaskbarList3
{
    void HrInit();
    void AddTab(IntPtr hwnd);
    void DeleteTab(IntPtr hwnd);
    void ActivateTab(IntPtr hwnd);
    void SetActiveAlt(IntPtr hwnd);
    void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
    void SetProgressState(IntPtr hwnd, uint state);
}
```

**Public API:**
```csharp
public enum TaskbarProgressState { None, Indeterminate, Normal, Error, Paused }

public static class TaskbarProgressHelper
{
    public static bool SetProgress(IntPtr hwnd, ulong completed, ulong total);
    public static bool SetState(IntPtr hwnd, TaskbarProgressState state);
    public static bool ClearProgress(IntPtr hwnd);
    public static bool SetProgress(Form form, int percent);
    public static bool SetState(Form form, TaskbarProgressState state);
    public static bool ClearProgress(Form form);
}
```

**Tests:** 6 tests — SetProgress/State/Clear with null handles, valid handles, edge cases (0/0).

**Step 1:** Write tests
**Step 2:** Verify failure (methods don't exist)
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 2: WindowManager

**Objective:** Save/restore WinForms/WPF window position, size, and state across sessions using registry.

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/WindowManager.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/WindowManagerTests.cs`

**Public API:**
```csharp
public sealed class WindowSettings
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public FormWindowState State { get; init; } // Normal, Maximized, Minimized
    public bool IsMaximized { get; init; }
}

public static class WindowManager
{
    public static bool Save(Form form, string? key = null);
    public static bool Save(IntPtr hwnd, WindowSettings settings, string? key = null);
    public static WindowSettings? Restore(Form form, string? key = null);
    public static WindowSettings? Restore(IntPtr hwnd, string? key = null);
    public static bool Delete(string? key = null);
    public static bool SaveAll(params Form[] forms);
    public static bool RestoreAll(params Form[] forms);
}
```

**Implementation:** Uses `Microsoft.Win32.Registry.CurrentUser` to store settings per-app (HKCU\Software\{AppName}\WindowState\{key}). Supports multiple forms by key name.

**Tests:** 8 tests — Save/Restore roundtrip, null form, key defaults, multiple forms, Delete.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 3: SingleInstanceHelper

**Objective:** Ensure only one instance of a WinForms/WPF app is running (named mutex pattern).

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/SingleInstanceHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/SingleInstanceHelperTests.cs`

**Public API:**
```csharp
public sealed class SingleInstanceGuard : IDisposable
{
    public static SingleInstanceGuard? TryAcquire(string appName, bool global = false);
    public bool IsNewInstance { get; }
    public void Dispose();
}

// Simplified usage pattern
public static class SingleInstanceHelper
{
    public static bool IsAlreadyRunning(string appName, bool global = false);
    public static SingleInstanceGuard? Acquire(string appName, bool global = false);
}
```

**Implementation:** Uses `System.Threading.Mutex` with `CreateMutexW` for global (cross-user) or local (per-user) mutex.

**Tests:** 5 tests — Acquire succeeds, second acquire fails, dispose releases, IsAlreadyRunning, global vs local.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 4: DarkModeHelper (WinForms Controls)

**Objective:** Apply Windows dark mode to standard WinForms controls (TextBox, ListView, TreeView, ComboBox, etc.).

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/DarkModeHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/DarkModeHelperTests.cs`

**Public API:**
```csharp
public enum DarkModeScope { Controls, Menu, ScrollBar, All }

public static class DarkModeHelper
{
    public static bool ApplyDarkMode(Control control, DarkModeScope scope = DarkModeScope.All);
    public static bool RemoveDarkMode(Control control, DarkModeScope scope = DarkModeScope.All);
    public static bool ApplyDarkMode(Form form, bool recursive = true);
    public static bool RemoveDarkMode(Form form, bool recursive = true);
    public static bool IsDarkModeAvailable(); // Windows 10 1903+
    public static Color GetDarkBackColor();
    public static Color GetDarkForeColor();
}
```

**Implementation:** Uses `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` on control handles. For `TextBox`/`ComboBox`/`ListView`/`TreeView`/`RichTextBox` — uses `WM_THEMECHANGED` + `SetWindowTheme` to apply dark mode. Custom controls get the dark color scheme via `DwmSetWindowAttribute`.

**P/Invoke (add to `Native/User32.cs` or new `Native/UxTheme.cs`):**
```csharp
[DllImport("uxtheme.dll", SetLastError = true, CharSet = CharSet.Unicode)]
internal static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

[DllImport("dwmapi.dll", SetLastError = true)]
internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
internal const int DWMWA_CAPTION_COLOR = 34;
```

**Tests:** 6 tests — IsDarkModeAvailable, ApplyDarkMode/Form, null control, empty form, dark colors valid.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 5: CustomWindowHelper (Aero Snap + Custom Chrome)

**Objective:** Handle WM_NCCALCSIZE, WM_NCHITTEST for custom window chrome with Aero snap support.

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/CustomWindowHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/CustomWindowHelperTests.cs`

**Public API:**
```csharp
public static class CustomWindowHelper
{
    // Handle WM_NCHITTEST for resize borders
    public static int HandleNCHitTest(IntPtr hwnd, int x, int y, int borderSize = 8);

    // Handle WM_NCCALCSIZE to remove default title bar
    public static void HandleNCCalcSize(ref RECT rect, bool removeBorder = true);

    // Apply DWM extended frame for glass/transparency effect
    public static bool ApplyDwmFrame(IntPtr hwnd, bool extendIntoClientArea = true);

    // Enable Aero snap for custom chrome
    public static bool EnableAeroSnap(IntPtr hwnd);

    // Helper to convert screen coordinates to client coordinates
    public static Point ScreenToClient(IntPtr hwnd, int screenX, int screenY);

    // Combined: apply all custom chrome settings at once
    public static bool EnableCustomChrome(IntPtr hwnd, int borderWidth = 8);
}
```

**P/Invoke (add to `Native/User32.cs`):**
```csharp
[DllImport("user32.dll", SetLastError = true)]
internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);
internal const int GWL_STYLE = -16;
internal const int GWL_EXSTYLE = -20;
internal const int WS_THICKFRAME = 0x00040000;
internal const int WS_CAPTION = 0x00C00000;

[DllImport("user32.dll", SetLastError = true)]
internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, [MarshalAs(UnmanagedType.Bool)] bool bMenu, int dwExStyle);

// NCHitTest constants
internal const int HTCLIENT = 1;
internal const int HTCAPTION = 2;
internal const int HTSYSMENU = 3;
internal const int HTLEFT = 10;
internal const int HTRIGHT = 11;
internal const int HTTOP = 12;
internal const int HTTOPLEFT = 13;
internal const int HTTOPRIGHT = 14;
internal const int HTBOTTOM = 15;
internal const int HTBOTTOMLEFT = 16;
internal const int HTBOTTOMRIGHT = 17;
```

**Tests:** 7 tests — HandleNCHitTest borders, corners, caption, client area; AdjustWindowRect; EnableCustomChrome.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 6: NetworkMonitorHelper

**Objective:** Monitor network connectivity changes in real-time.

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/NetworkMonitorHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/NetworkMonitorHelperTests.cs`

**Public API:**
```csharp
public enum NetworkStatus { Unknown, Connected, Disconnected, ConnectedAsLAN, ConnectedAsWiFi }

public sealed class NetworkChangeEventArgs : EventArgs
{
    public NetworkStatus Status { get; }
    public string? AdapterName { get; }
    public bool IsConnected { get; }
}

public sealed class NetworkMonitor : IDisposable
{
    public event EventHandler<NetworkChangeEventArgs>? StatusChanged;
    public NetworkStatus CurrentStatus { get; }
    public bool IsConnected { get; }
    public bool IsMonitoring { get; }

    public NetworkMonitor();
    public bool Start();
    public bool Stop();
    public void Dispose();
}
```

**P/Invoke (add to `Native/Kernel32.cs`):**
```csharp
[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
internal static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool UnregisterDeviceNotification(IntPtr hHandle);

[DllImport("kernel32.dll", SetLastError = true)]
internal static extern uint GetAdaptersInfo(IntPtr pAdapterInfo, ref uint pOutBufLen);

internal const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
internal const uint DBT_DEVICEARRIVAL = 0x8000;
internal const uint DBT_DEVICEREMOVECOMPLETE = 0x8004;
internal const uint DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
```

Also need `IPAdaptersInfo` struct and `GetAdaptersInfo` from `iphlpapi.dll`.

**Tests:** 5 tests — CurrentStatus valid, Start/Stop, IsConnected bool, event registration, Dispose safe.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 7: AutoUpdateHelper

**Objective:** Check GitHub Releases for updates, download, and optionally install.

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/AutoUpdateHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/AutoUpdateHelperTests.cs`

**Public API:**
```csharp
public sealed class UpdateInfo
{
    public string Version { get; init; } = string.Empty;
    public string ReleaseNotes { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public bool IsNewerThanCurrent { get; init; }
}

public static class AutoUpdateHelper
{
    public static async Task<UpdateInfo?> CheckForUpdateAsync(
        string owner, string repo, Version? currentVersion = null);

    public static async Task<bool> DownloadUpdateAsync(
        string downloadUrl, string targetPath,
        IProgress<double>? progress = null, CancellationToken ct = default);

    public static bool IsUpdateAvailable(
        string currentVersion, string latestVersion);

    public static bool LaunchInstaller(string installerPath);
}
```

**Implementation:** Uses `HttpClient` to query `https://api.github.com/repos/{owner}/{repo}/releases/latest`. Parses JSON response. Downloads asset via redirect URL.

**Tests:** 5 tests — CheckForUpdate null owner, IsUpdateAvailable comparison, DownloadUpdate null URL, LaunchInstaller null path, Version comparison edge cases.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 8: FileAssociationHelper

**Objective:** Register application as handler for file types (context menu, default program).

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/FileAssociationHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/FileAssociationHelperTests.cs`

**Public API:**
```csharp
public sealed class FileAssociation
{
    public string Extension { get; init; } = ".txt";
    public string ProgId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public int IconIndex { get; init; }
}

public static class FileAssociationHelper
{
    public static bool Register(FileAssociation association);
    public static bool Unregister(string extension, string progId);
    public static bool IsRegistered(string extension);
    public static FileAssociation? GetAssociation(string extension);
    public static bool SetDefault(string extension, string progId);
}
```

**Implementation:** Uses `Registry.ClassesRoot` to create ProgId, shell\open\command, DefaultIcon entries. Registers in HKCU\Software\Classes (per-user, no admin needed).

**Tests:** 6 tests — Register/Unregister roundtrip, IsRegistered, GetAssociation, SetDefault, null extension.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 9: AutoStartHelper

**Objective:** Register/unregister application in Windows startup (HKCU\Software\Microsoft\Windows\CurrentVersion\Run).

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/AutoStartHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/AutoStartHelperTests.cs`

**Public API:**
```csharp
public static class AutoStartHelper
{
    public static bool Enable(string appName, string? executablePath = null, string? args = null);
    public static bool Disable(string appName);
    public static bool IsEnabled(string appName);
    public static string? GetCommand(string appName);
    public static bool EnableCurrentUser(string appName, string? args = null);
    public static bool EnableAllUsers(string appName, string? args = null);
    public static bool RemoveFromStartup(string appName);
}
```

**Implementation:** Uses `Registry.CurrentUser\Software\Microsoft\Windows\CurrentVersion\Run` for per-user, `Registry.LocalMachine\...` for all-users.

**Tests:** 6 tests — Enable/Disable roundtrip, IsEnabled, GetCommand, RemoveFromStartup, null/empty args.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Task 10: GlobalExceptionHandler

**Objective:** Catch unhandled exceptions across all threads and AppDomains, generate crash reports.

**Files:**
- Create: `src/BPlusLib.Foundation/Windows/GlobalExceptionHandler.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Windows/GlobalExceptionHandlerTests.cs`

**Public API:**
```csharp
public sealed class CrashReport
{
    public DateTime Timestamp { get; init; }
    public string ExceptionType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string StackTrace { get; init; } = string.Empty;
    public string? InnerException { get; init; }
    public Dictionary<string, string> SystemInfo { get; init; } = new();
    public string? DumpPath { get; init; }
}

public sealed class GlobalExceptionHandler : IDisposable
{
    public event EventHandler<CrashReport>? UnhandledException;

    public static GlobalExceptionHandler Instance { get; }
    public bool IsHandling { get; }
    public string? DumpDirectory { get; set; }

    public bool Enable();
    public bool Disable();
    public void Dispose();

    public static CrashReport CreateCrashReport(Exception ex);
    public static bool SaveCrashReport(CrashReport report, string path);
}
```

**Implementation:**
- `AppDomain.CurrentDomain.UnhandledException`
- `TaskScheduler.UnobservedTaskException`
- `Application.ThreadException` (WinForms)
- On crash: collect exception info, system info (OS, CPU, RAM, .NET version), optionally create minidump via `CrashDumpHelper`, save report to file.

**Tests:** 5 tests — Enable/Disable, IsHandling, CreateCrashReport valid, SaveCrashReport, Instance singleton.

**Step 1:** Write tests
**Step 2:** Verify failure
**Step 3:** Implement
**Step 4:** Verify pass
**Step 5:** Commit

---

## Summary

| # | Module | LOC est. | Tests | Priority |
|---|--------|----------|-------|----------|
| 1 | TaskbarProgressHelper | ~200 | 6 | 🔴 High |
| 2 | WindowManager | ~250 | 8 | 🔴 High |
| 3 | SingleInstanceHelper | ~120 | 5 | 🔴 High |
| 4 | DarkModeHelper | ~300 | 6 | 🟡 Medium |
| 5 | CustomWindowHelper | ~350 | 7 | 🟡 Medium |
| 6 | NetworkMonitorHelper | ~250 | 5 | 🟡 Medium |
| 7 | AutoUpdateHelper | ~200 | 5 | 🟡 Medium |
| 8 | FileAssociationHelper | ~250 | 6 | 🟢 Low |
| 9 | AutoStartHelper | ~150 | 6 | 🟢 Low |
| 10 | GlobalExceptionHandler | ~250 | 5 | 🔴 High |
| **Total** | **10 modules** | **~2,320** | **59** | |

**New native DLLs needed:** `dwmapi.dll` (DWM — already have), `uxtheme.dll` (SetWindowTheme)

**Files likely to change:**
- Modify: `src/BPlusLib.Foundation/Native/Shell32.cs` (ITaskbarList3 COM)
- Modify: `src/BPlusLib.Foundation/Native/User32.cs` (NCHITTEST, NCCALCSIZE, GetWindowLong, etc.)
- Modify: `src/BPlusLib.Foundation/Native/Kernel32.cs` (RegisterDeviceNotification, GetAdaptersInfo)
- Create: 10 new files under `src/BPlusLib.Foundation/Windows/`
- Create: 10 new test files under `tests/BPlusLib.Foundation.Tests/Windows/`

**Risks:**
1. `RegisterDeviceNotification` needs a window handle — NetworkMonitorHelper creates a hidden Form.
2. DarkModeHelper varies by Windows version — fallback to default colors.
3. AutoUpdateHelper depends on GitHub API — handle rate limiting gracefully.
4. FileAssociationHelper per-user registration may not persist on domain-locked machines.

**Open questions:**
1. Should we add `System.Windows.Forms` conditional compilation (like existing `FEATURE_WINDOW_MODULE`) for WinForms-specific helpers?
2. For WPF helpers (WindowManager WPF overload, WpfDarkMode) — should we create a separate `BPlusLib.Foundation.Wpf` package or keep them in the same package?
