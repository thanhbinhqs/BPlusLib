# BPlusLib.Foundation — Phase 1 Implementation Plan

> **For Hermes:** Use delegate_task (subagent) per task group with full context.
> **Plan mode:** This is a plan-only document. Do not implement until explicitly instructed.

**Goal:** Transform BPlusLib into `BPlusLib.Foundation` — an enterprise-grade Windows Foundation Library with Common, Native, Window, and Dialogs modules.

**Architecture:** Flat project with namespace-per-module. All P/Invoke internal. Public API exclusively through safe managed wrappers. Every module depends only on Common + Native.

**Tech Stack:** .NET 8 (primary), .NET Framework 4.7, .NET 6, .NET 10. xUnit + FluentAssertions for tests.

**Current state:** `/home/binh/BPlusLib/` has an existing project with `BPlusLib.csproj` (net481/netstandard2.0/net8.0), existing `SerialPorts/` namespace, `MessageBoxEx.cs`, `Utils.cs`, `NativeMethods.cs`, `NativeStructures.cs`.

---

## Restructure Overview

```
BPlusLib/                              (repo root)
├── BPlusLib.sln → BPlusLib.Foundation.sln
├── src/
│   └── BPlusLib.Foundation/
│       ├── BPlusLib.Foundation.csproj  (new)
│       ├── IsExternalInit.cs           (keep)
│       ├── Common/                     (new module)
│       │   ├── Guard.cs
│       │   ├── Result.cs
│       │   ├── Option.cs
│       │   ├── DisposableBase.cs
│       │   ├── ObservableObject.cs
│       │   ├── AsyncLock.cs
│       │   ├── AsyncCache.cs
│       │   ├── RetryPolicy.cs
│       │   ├── Debounce.cs
│       │   └── ObjectPool.cs
│       ├── Native/                     (refactored from existing NativeMethods.cs)
│       │   ├── SafeNativeMethods.cs    (shared P/Invoke)
│       │   ├── Kernel32.cs
│       │   ├── User32.cs
│       │   ├── NtDll.cs
│       │   ├── Shell32.cs
│       │   ├── SafeHandles/
│       │   │   ├── SafeLibraryHandle.cs
│       │   │   └── SafeProcessHandle.cs
│       │   └── Interop/
│       │       ├── NativeStructures.cs (union of all)
│       │       └── Win32Errors.cs
│       ├── Window/
│       │   ├── DragMoveHelper.cs
│       │   ├── ResizeHelper.cs
│       │   ├── MonitorHelper.cs
│       │   ├── WindowPositionManager.cs
│       │   └── WindowAnimation.cs
│       ├── Dialogs/
│       │   ├── MessageBoxEx.cs         (rewrite existing)
│       │   ├── InputBoxEx.cs
│       │   ├── ProgressDialog.cs
│       │   ├── DialogEnums.cs
│       │   └── DialogParams.cs
│       ├── SerialPorts/                (keep, refactor namespace)
│       │   └── ... (existing files)
│       └── Utils/                      (split from existing Utils.cs)
│           ├── NetworkUtils.cs
│           └── CommandRunner.cs
├── tests/
│   └── BPlusLib.Foundation.Tests/
│       ├── Common/
│       │   ├── GuardTests.cs
│       │   ├── ResultTests.cs
│       │   ├── OptionTests.cs
│       │   ├── DisposableBaseTests.cs
│       │   └── ...
│       ├── Window/
│       │   └── ...
│       └── Dialogs/
│           └── ...
├── README.md
├── LICENSE
└── .gitignore
```

---

## Phase 1 — Task Breakdown (7 Task Groups, ~30 individual tasks)

---

### GROUP A: Project scaffolding

#### Task A1: Create solution + project skeleton

**Objective:** Rename project to `BPlusLib.Foundation` with correct multi-targeting.

**Files:**
- Create: `src/BPlusLib.Foundation/BPlusLib.Foundation.csproj`
- Create: `BPlusLib.Foundation.sln`
- Create: `src/BPlusLib.Foundation/IsExternalInit.cs`

**Step 1:** Write the `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net472;net6.0;net8.0;net10.0</TargetFrameworks>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AssemblyName>BPlusLib.Foundation</AssemblyName>
    <RootNamespace>BPlusLib.Foundation</RootNamespace>
    <Description>BPlusLib.Foundation — Enterprise-grade Windows Foundation Library</Description>
    <Version>2.0.0</Version>
    <Authors>thanhbinhqs</Authors>
    <PackageId>BPlusLib.Foundation</PackageId>
    <PackageProjectUrl>https://github.com/thanhbinhqs/BPlusLib</PackageProjectUrl>
    <RepositoryUrl>https://github.com/thanhbinhqs/BPlusLib.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>windows;foundation;serialport;messagebox;win32;pinvoke</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <Deterministic>true</Deterministic>
    <Features>strict</Features>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net472' or '$(TargetFramework)' == 'net6.0'">
    <PackageReference Include="Microsoft.Bcl.HashCode" Version="1.1.1" />
    <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
    <PackageReference Include="System.Memory" Version="4.5.5" />
  </ItemGroup>
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\LICENSE" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

**Step 2:** Write solution file pointing to the new csproj.

**Step 3:** Write `IsExternalInit.cs` polyfill for net472.

**Step 4:** Build:

```bash
cd /home/binh/BPlusLib
dotnet build src/BPlusLib.Foundation/BPlusLib.Foundation.csproj -f net8.0
```
Expected: Build succeeded, 0 errors.

**Step 5:** Commit:
```bash
git add -A && git commit -m "wip: scaffold BPlusLib.Foundation project"
```

---

#### Task A2: Create test project

**Files:**
- Create: `tests/BPlusLib.Foundation.Tests/BPlusLib.Foundation.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net472;net8.0</TargetFrameworks>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <RootNamespace>BPlusLib.Foundation.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.8.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\BPlusLib.Foundation\BPlusLib.Foundation.csproj" />
  </ItemGroup>
</Project>
```

Build + verify test project restores.

---

#### Task A3: Restructure existing files into module folders

**Files to move:**
- `src/BPlusLib/NativeMethods.cs` → `src/BPlusLib.Foundation/Native/SafeNativeMethods.cs`
- `src/BPlusLib/NativeStructures.cs` → `src/BPlusLib.Foundation/Native/Interop/NativeStructures.cs`
- `src/BPlusLib/SerialPorts/*` → `src/BPlusLib.Foundation/SerialPorts/*`
- `src/BPlusLib/MessageBoxEx.cs` → `src/BPlusLib.Foundation/Dialogs/MessageBoxEx.cs`
- `src/BPlusLib/Utils.cs` → `src/BPlusLib.Foundation/Utils/CommandRunner.cs`
- `src/BPlusLib/IsExternalInit.cs` → `src/BPlusLib.Foundation/IsExternalInit.cs`

Update all namespace declarations:
- `BPlusLib` → `BPlusLib.Foundation`
- `BPlusLib.SerialPorts` → `BPlusLib.Foundation.SerialPorts`

Update internal `NativeMethods` references to point to new location.

Build:
```bash
cd /home/binh/BPlusLib
dotnet build src/BPlusLib.Foundation/BPlusLib.Foundation.csproj -f net8.0
```
Expected: Build succeeded, 0 errors.

---

### GROUP B: Common Module — Core Abstractions

#### Task B1: Guard

**File:** `src/BPlusLib.Foundation/Common/Guard.cs`

```csharp
namespace BPlusLib.Foundation.Common;

public static class Guard
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null);
    public static void ThrowIfNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) where T : class;
    public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null);
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null);
    public static void ThrowIfOutOfRange(int value, int min, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null);
    public static void ThrowIfDisposed(bool isDisposed, [CallerArgumentExpression(nameof(isDisposed))] string? paramName = null);
    public static void ThrowIfInvalidOperation(bool condition, string message);
}
```

**Tests** (`tests/BPlusLib.Foundation.Tests/Common/GuardTests.cs`): 10 test cases covering null, empty, whitespace, out of range, disposed, valid values.

---

#### Task B2: Result\<T\>

**File:** `src/BPlusLib.Foundation/Common/Result.cs`

A discriminated union: success with value, or failure with exception chain.

```csharp
public readonly struct Result<T>
{
    public static Result<T> Ok(T value);
    public static Result<T> Fail(Exception error);
    public static Result<T> Fail(string message);
    public T Value { get; }
    public Exception? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper);
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder);
    public T OrDefault(T defaultValue);
    public T OrThrow();
}

// Non-generic companion for void-returning operations
public readonly struct Result
{
    public static Result Ok();
    public static Result Fail(Exception error);
    public static Result Fail(string message);
}
```

**Tests:** 12 cases — Ok, Fail, Map success, Map failure, Bind, OrDefault, OrThrow, implicit conversion.

---

#### Task B3: Option\<T\>

**File:** `src/BPlusLib.Foundation/Common/Option.cs`

```csharp
public readonly struct Option<T>
{
    public static Option<T> Some(T value);
    public static Option<T> None { get; }
    public bool HasValue { get; }
    public T Value { get; }
    public T OrDefault(T defaultValue);
    public Option<TNew> Map<TNew>(Func<T, TNew> mapper);
    public Option<TNew> Bind<TNew>(Func<T, Option<TNew>> binder);
    public void Match(Action<T> onSome, Action onNone);
    public Result<T> ToResult(string errorMessage);
}
```

**Tests:** 8 cases — Some, None, Map on Some, Map on None, Bind, OrDefault, Match, ToResult.

---

#### Task B4: DisposableBase

**File:** `src/BPlusLib.Foundation/Common/DisposableBase.cs`

Standard Dispose pattern with finalizer guard:

```csharp
public abstract class DisposableBase : IDisposable
{
    protected bool IsDisposed { get; }
    protected abstract void DisposeManaged();
    protected virtual void DisposeUnmanaged() { }
    public void Dispose();  // sealed final
    // non-sealed virtual for subclasses that need custom dispose logic
    protected virtual void Dispose(bool disposing);
}
```

**Tests:** 4 cases — Dispose once, Dispose twice (idempotent), derived class disposes managed resources, derived class with finalizer.

---

#### Task B5: ObservableObject

**File:** `src/BPlusLib.Foundation/Common/ObservableObject.cs`

```csharp
public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null);
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null);
    protected void OnPropertyChanging([CallerMemberName] string? propertyName = null);
}
```

**Tests:** 4 cases — SetProperty fires both events, unchanged value skips events, OnPropertyChanged, OnPropertyChanging.

---

#### Task B6: AsyncLock

**File:** `src/BPlusLib.Foundation/Common/AsyncLock.cs`

```csharp
public sealed class AsyncLock : IDisposable
{
    public AsyncLock();
    public ValueTask<Releaser> LockAsync(CancellationToken ct = default);
    public Releaser Lock();

    public readonly struct Releaser : IDisposable
    {
        public void Dispose();
    }
}
```

**Tests:** 4 cases — basic acquire/release, reentrant, cancellation, concurrent safety.

---

#### Task B7: AsyncCache\<TKey, TValue\>

**File:** `src/BPlusLib.Foundation/Common/AsyncCache.cs`

```csharp
public sealed class AsyncCache<TKey, TValue> where TKey : notnull
{
    public AsyncCache(Func<TKey, CancellationToken, Task<TValue>> factory, TimeSpan? expiry = null);
    public ValueTask<TValue> GetAsync(TKey key, CancellationToken ct = default);
    public void Invalidate(TKey key);
    public void Clear();
    public int Count { get; }
}
```

**Tests:** 6 cases — first call invokes factory, second call returns cached, expiry eviction, Invalidate, Clear, concurrent requests.

---

#### Task B8: RetryPolicy

**File:** `src/BPlusLib.Foundation/Common/RetryPolicy.cs`

```csharp
public sealed class RetryPolicy
{
    public RetryPolicy(int maxRetries, TimeSpan baseDelay, RetryBackoffType backoffType = RetryBackoffType.Exponential);
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}

public enum RetryBackoffType { Constant, Linear, Exponential, Jitter }
```

**Tests:** 6 cases — succeeds first try, succeeds after retries, fails after all retries, exponential backoff, cancellation, jitter.

---

#### Task B9: Debounce + ObjectPool

**Files:**
- `src/BPlusLib.Foundation/Common/Debounce.cs`
- `src/BPlusLib.Foundation/Common/ObjectPool.cs`

**Debounce:**
```csharp
public sealed class Debounce
{
    public Debounce(TimeSpan delay);
    public void Invoke(Action action);
    public Task InvokeAsync(Func<Task> action);
    public void Cancel();
}
```

**ObjectPool:**
```csharp
public sealed class ObjectPool<T> where T : class
{
    public ObjectPool(Func<T> factory, int maxPoolSize = Environment.ProcessorCount * 2);
    public PooledObject<T> Get();
    public void Return(T item);
}

public readonly struct PooledObject<T> : IDisposable where T : class
{
    public T Value { get; }
    public void Dispose();
}
```

**Tests:** Debounce (4), ObjectPool (4).

---

### GROUP C: Native Module — Safe Win32 Wrappers

#### Task C1: Split monolithic NativeMethods into categorized files

**Files:**
- `src/BPlusLib.Foundation/Native/Kernel32.cs` — OpenProcess, CloseHandle, DuplicateHandle, QueryDosDevice, QueryFullProcessImageName, GetProcessTimes, GetCurrentProcess, FormatMessage
- `src/BPlusLib.Foundation/Native/User32.cs` — MessageBox, SetWindowsHookEx, UnhookWindowsHookEx, CallNextHookEx, GetWindowRect, GetParent, SetWindowPos, GetSystemMetrics, GetWindowThreadProcessId, ShowWindow, SendMessage, PostMessage
- `src/BPlusLib.Foundation/Native/NtDll.cs` — NtQuerySystemInformation, NtQueryObject, NtQueryInformationProcess
- `src/BPlusLib.Foundation/Native/Shell32.cs` — CommandLineToArgvW, SHGetFileInfo, ShellExecuteEx, SHOpenFolderAndSelectItems
- `src/BPlusLib.Foundation/Native/Interop/NativeStructures.cs` — all interop structs (keep existing + add RECT, POINT, MONITORINFO, WINDOWPLACEMENT, etc.)
- `src/BPlusLib.Foundation/Native/Interop/Win32Errors.cs` — HRESULT constants, NTSTATUS helpers

All classes remain `internal static`.

**Step 1:** Create each file with proper namespace `BPlusLib.Foundation.Native`.

**Step 2:** Update all references across the codebase.

**Step 3:** Build — 0 errors.

---

#### Task C2: SafeHandle wrappers

**Files:**
- `src/BPlusLib.Foundation/Native/SafeHandles/SafeLibraryHandle.cs`
- `src/BPlusLib.Foundation/Native/SafeHandles/SafeProcessHandle.cs`

```csharp
internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected override bool ReleaseHandle();
}

internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected override bool ReleaseHandle();
}
```

---

#### Task C3: Safe native helper methods

**File:** `src/BPlusLib.Foundation/Native/SafeNativeMethods.cs`

Public/internal helper methods that wrap raw P/Invoke with error handling:

```csharp
internal static class SafeNativeMethods
{
    internal static Result<RECT> GetWindowRect(IntPtr hwnd);
    internal static Result<MONITORINFO> GetMonitorInfo(IntPtr hmonitor);
    internal static Result<string> GetWindowText(IntPtr hwnd);
    internal static Result<int> GetDpiForWindow(IntPtr hwnd);
    internal static Result<IntPtr> OpenProcess(ProcessAccessFlags access, int processId);
    internal static Result<T> DuplicateHandle<T>(SafeProcessHandle sourceProcess, IntPtr sourceHandle) where T : SafeHandle;
}
```

Every method converts Win32 errors to `Result<T>.Fail`.

---

### GROUP D: Window Module

#### Task D1: MonitorHelper

**File:** `src/BPlusLib.Foundation/Window/MonitorHelper.cs`

```csharp
public static class MonitorHelper
{
    public static IReadOnlyList<MonitorInfo> GetAllMonitors();
    public static MonitorInfo GetPrimaryMonitor();
    public static MonitorInfo GetMonitorFrom(IntPtr hwnd);
    public static MonitorInfo GetMonitorFrom(Point point);
    public static Rectangle GetVirtualScreen();
    public static Rectangle GetWorkingArea();
    public static Rectangle GetWorkingAreaFrom(IntPtr hwnd);
    public static DpiScale GetDpiForWindow(IntPtr hwnd);
    public static bool IsHighDpi();
}

public readonly struct MonitorInfo
{
    public string Name { get; }
    public Rectangle Bounds { get; }
    public Rectangle WorkingArea { get; }
    public bool IsPrimary { get; }
    public DpiScale Dpi { get; }
    public string DeviceId { get; }
    public IntPtr Handle { get; }
}

public readonly struct DpiScale
{
    public double X { get; }
    public double Y { get; }
    public double Scale { get; }
}
```

Uses `EnumDisplayMonitors` + `GetMonitorInfo` internally.

**Tests:** 8 cases — GetPrimaryMonitor returns non-empty, GetAllMonitors count > 0, GetMonitorFrom point matches, DPI non-zero, etc.

---

#### Task D2: DragMoveHelper

**File:** `src/BPlusLib.Foundation/Window/DragMoveHelper.cs`

```csharp
public static class DragMoveHelper
{
    public static void Attach(Form form);
    public static void Attach(Form form, Control dragArea);
    public static void Detach(Form form);
}
```

Implementation: Subclass `NativeWindow` to intercept `WM_NCHITTEST` on the drag area, return `HTCAPTION` so Windows handles the move. Also handle `WM_GETMINMAXINFO` for snap prevention during drag.

**Design detail:** When `WM_NCHITTEST` returns `HTCAPTION`, Windows automatically handles the move with full DPI-awareness, snap, and multi-monitor support — no custom mouse handling needed.

---

#### Task D3: ResizeHelper

**File:** `src/BPlusLib.Foundation/Window/ResizeHelper.cs`

```csharp
public static class ResizeHelper
{
    public static void EnableResize(Form form, int borderWidth = 4);
    public static void DisableResize(Form form);
}
```

Implementation: Same `NativeWindow` subclass, intercept `WM_NCHITTEST` along the edges. Return `HTLEFT`, `HTRIGHT`, `HTTOP`, `HTTOPLEFT`, `HTBOTTOMRIGHT`, etc. based on cursor position relative to the form edges.

---

#### Task D4: WindowPositionManager

**File:** `src/BPlusLib.Foundation/Window/WindowPositionManager.cs`

```csharp
public sealed class WindowPositionManager
{
    public WindowPositionManager(string applicationName);
    public void Save(Form form, string key);
    public bool Restore(Form form, string key);
    public void Reset(string key);
}
```

Saves to registry under `HKEY_CURRENT_USER\Software\<appName>\WindowPositions\<key>`: Location, Size, WindowState, Monitor name. On restore, validates the target monitor still exists; if not, falls back to primary monitor.

---

#### Task D5: WindowAnimation

**File:** `src/BPlusLib.Foundation/Window/WindowAnimation.cs`

```csharp
public static class WindowAnimation
{
    public static Task FlashAsync(Form form, int flashCount = 3, CancellationToken ct = default);
    public static Task ShakeAsync(Form form, int intensity = 5, TimeSpan? duration = null, CancellationToken ct = default);
    public static Task FadeInAsync(Form form, TimeSpan? duration = null, int steps = 20, CancellationToken ct = default);
    public static Task FadeOutAsync(Form form, TimeSpan? duration = null, int steps = 20, CancellationToken ct = default);
}
```

Implementation:
- **Flash**: Uses `FlashWindowEx` via user32.
- **Shake**: Timer-based offset animation on form Location.
- **Fade**: Timer-based opacity animation via `Form.Opacity`.

---

### GROUP E: Dialogs Module

#### Task E1: Dialog enums + params (data types first)

**Files:**
- `src/BPlusLib.Foundation/Dialogs/DialogEnums.cs`
- `src/BPlusLib.Foundation/Dialogs/DialogParams.cs`

Extract existing enums from MessageBoxEx into `DialogEnums.cs`:

```csharp
namespace BPlusLib.Foundation.Dialogs;

public enum DialogButton { OK, OKCancel, YesNo, YesNoCancel, AbortRetryIgnore, RetryCancel, CancelTryContinue }
public enum DialogIcon { None, Information, Question, Warning, Error, Shield }
public enum DialogResult { None, OK, Cancel, Abort, Retry, Ignore, Yes, No, TryAgain, Continue }
public enum DarkModeStyle { System, Light, Dark, Inherit }
```

`DialogParams.cs`:

```csharp
public sealed class DialogParams
{
    public string Text { get; init; }
    public string Caption { get; init; }
    public string? Details { get; init; }
    public string? CheckboxText { get; init; }
    public bool? CheckboxState { get; init; }
    public IWin32Window? Owner { get; init; }
    public DialogButton Buttons { get; init; } = DialogButton.OK;
    public DialogIcon Icon { get; init; }
    public DarkModeStyle DarkMode { get; init; }
    public int TimeoutMs { get; init; }
    public DialogResult TimeoutResult { get; init; } = DialogResult.None;
    public bool TopMost { get; init; }
    public IReadOnlyList<DialogCustomButton>? CustomButtons { get; init; }
}

public sealed class DialogCustomButton
{
    public string Text { get; init; }
    public DialogResult Result { get; init; }
    public bool IsDefault { get; init; }
    public bool IsCancel { get; init; }
}
```

---

#### Task E2: MessageBoxEx (rewrite)

**File:** `src/BPlusLib.Foundation/Dialogs/MessageBoxEx.cs`

```csharp
public static class MessageBoxEx
{
    public static Task<DialogResult> ShowAsync(DialogParams parameters, CancellationToken ct = default);
    public static Task<DialogResult> ShowAsync(IWin32Window? owner, string text, string caption,
        DialogButton buttons = DialogButton.OK, DialogIcon icon = DialogIcon.None, CancellationToken ct = default);
}
```

Implementation:
- Use WH_CBT hook for centering (existing approach, refactored).
- Support timeout via `Task.Delay` + `PostMessage(WM_CLOSE)` race.
- Support expandable Details: create a custom form with expander.
- Support custom buttons: build a custom form (advanced path — Phase 2).
- Dark mode: `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` on the dialog HWND.

**Key improvement over existing:** Use `TaskCompletionSource<DialogResult>` pattern so the method is truly async (doesn't block the calling thread). Pump messages via `Application.DoEvents` or use a modal form.

---

#### Task E3: InputBoxEx

**File:** `src/BPlusLib.Foundation/Dialogs/InputBoxEx.cs`

```csharp
public static class InputBoxEx
{
    public static Task<InputBoxResult<string>> ShowAsync(InputBoxParams parameters, CancellationToken ct = default);
}

public sealed class InputBoxParams
{
    public string Title { get; init; }
    public string Label { get; init; }
    public string? DefaultValue { get; init; }
    public string? Placeholder { get; init; }
    public bool UsePasswordMask { get; init; }
    public Func<string, (bool valid, string error)>? Validator { get; init; }
    public IWin32Window? Owner { get; init; }
    public DarkModeStyle DarkMode { get; init; }
}

public readonly struct InputBoxResult<T>
{
    public bool Confirmed { get; }
    public T Value { get; }
}
```

Implementation: Custom form with TextBox + validation. Async pattern via `ShowDialog` on UI thread + `TaskCompletionSource`.

---

#### Task E4: ProgressDialog

**File:** `src/BPlusLib.Foundation/Dialogs/ProgressDialog.cs`

```csharp
public sealed class ProgressDialog : IDisposable
{
    public ProgressDialog(IWin32Window? owner, ProgressDialogParams parameters);
    public Task ShowAsync(CancellationToken ct = default);
    public void Report(int percent, string? statusText = null);
    public CancellationToken CancellationToken { get; }
}

public sealed class ProgressDialogParams
{
    public string Title { get; init; }
    public string? Text { get; init; }
    public bool IsIndeterminate { get; init; }
    public int Maximum { get; init; } = 100;
    public bool ShowCancelButton { get; init; } = true;
    public bool TopMost { get; init; }
}
```

---

### GROUP F: Integration & Migration

#### Task F1: Migrate MessageBoxEx from old to new

- Move existing `BPlusLib.MessageBoxEx` → `BPlusLib.Foundation.Dialogs.MessageBoxEx`
- Update namespace, keep backward-compatible `Show` method (synchronous overload)
- Add new `ShowAsync` method
- Remove old file after confirming new one builds

#### Task F2: Migrate SerialPorts

- Update namespace from `BPlusLib.SerialPorts` → `BPlusLib.Foundation.SerialPorts`
- Update `NativeMethods` references to split module files
- Build, test

#### Task F3: Migrate Utils

- Split `Utils.cs`:
  - `NetworkUtils.cs` → `BPlusLib.Foundation.Utils.NetworkUtils` (IP, hostname)
  - `CommandRunner.cs` → `BPlusLib.Foundation.Utils.CommandRunner` (RunCommand, RunPowerShell)

---

### GROUP G: Tests & Validation

#### Task G1: All Common tests

Write xUnit + FluentAssertions tests for every Common class. Target: 95% coverage.

```bash
dotnet test tests/BPlusLib.Foundation.Tests/ --filter "Category=Common" -v n
```

#### Task G2: Window tests (pure logic)

`MonitorHelper` tests (no HWND needed — use known data).
`DpiScale` math tests.
`WindowPositionManager` save/restore edge cases.

#### Task G3: Dialogs tests (parameter validation)

Test all `DialogParams` validation.
Test `InputBoxParams` validation.
Test `ProgressDialog.Report` thread safety.

---

## Exception Strategy Summary

| Scenario | Exception Type |
|----------|---------------|
| Null argument | `ArgumentNullException` (via Guard) |
| Empty/whitespace string | `ArgumentException` |
| Value out of range | `ArgumentOutOfRangeException` |
| Disposed object access | `ObjectDisposedException` |
| Win32 API failure | `Result<T>.Fail` with `Win32Exception` inner |
| Timeout | `TimeoutException` |
| Operation canceled | `OperationCanceledException` |
| Invalid state | `InvalidOperationException` |

No exceptions are swallowed in library code. Event handlers may catch and log.

---

## Threading Strategy Summary

| Component | Thread Affinity | Notes |
|-----------|----------------|-------|
| MessageBoxEx | UI thread (forms) | Uses `TaskCompletionSource` + `ShowDialog` |
| InputBoxEx | UI thread | Same pattern |
| ProgressDialog | UI thread + background worker | `Progress<T>` pattern |
| DragMoveHelper | UI thread | Window message loop |
| MonitorHelper | Any | Pure data queries |
| WindowAnimation | UI thread | Timer callbacks marshaled via `BeginInvoke` |
| AsyncLock | Any | SemaphoreSlim-based |
| AsyncCache | Any | ConcurrentDictionary + SemaphoreSlim per key |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Win32 P/Invoke subtlety on ARM64 | Wrap every native call; test on ARM64 emulator |
| WH_CBT hook conflicts with other hooks | Install/uninstall around single MessageBox call; never leak hooks |
| DarkMode not supported before Win10 1809 | Runtime check, graceful fallback to light mode |
| .NET Framework 4.7 missing nullable annotations | Use `#nullable disable` in net472-specific code paths |
| Large file rename diff clutters git history | Do moves + content changes in separate commits |

---

## Directory Tree (Final State After Phase 1)

```
/home/binh/BPlusLib/
├── BPlusLib.Foundation.sln
├── src/
│   └── BPlusLib.Foundation/
│       ├── BPlusLib.Foundation.csproj
│       ├── IsExternalInit.cs
│       ├── Common/
│       │   ├── Guard.cs
│       │   ├── Result.cs
│       │   ├── Option.cs
│       │   ├── DisposableBase.cs
│       │   ├── ObservableObject.cs
│       │   ├── AsyncLock.cs
│       │   ├── AsyncCache.cs
│       │   ├── RetryPolicy.cs
│       │   ├── Debounce.cs
│       │   └── ObjectPool.cs
│       ├── Native/
│       │   ├── Kernel32.cs
│       │   ├── User32.cs
│       │   ├── NtDll.cs
│       │   ├── Shell32.cs
│       │   ├── SafeNativeMethods.cs
│       │   ├── SafeHandles/
│       │   │   ├── SafeLibraryHandle.cs
│       │   │   └── SafeProcessHandle.cs
│       │   └── Interop/
│       │       ├── NativeStructures.cs
│       │       └── Win32Errors.cs
│       ├── Window/
│       │   ├── DragMoveHelper.cs
│       │   ├── ResizeHelper.cs
│       │   ├── MonitorHelper.cs
│       │   ├── WindowPositionManager.cs
│       │   └── WindowAnimation.cs
│       ├── Dialogs/
│       │   ├── DialogEnums.cs
│       │   ├── DialogParams.cs
│       │   ├── MessageBoxEx.cs
│       │   ├── InputBoxEx.cs
│       │   └── ProgressDialog.cs
│       ├── SerialPorts/       (migrated, unchanged logic)
│       │   └── ...
│       └── Utils/
│           ├── NetworkUtils.cs
│           └── CommandRunner.cs
├── tests/
│   └── BPlusLib.Foundation.Tests/
│       ├── BPlusLib.Foundation.Tests.csproj
│       ├── Common/
│       │   ├── GuardTests.cs
│       │   ├── ResultTests.cs
│       │   ├── OptionTests.cs
│       │   ├── DisposableBaseTests.cs
│       │   ├── ObservableObjectTests.cs
│       │   ├── AsyncLockTests.cs
│       │   ├── AsyncCacheTests.cs
│       │   ├── RetryPolicyTests.cs
│       │   ├── DebounceTests.cs
│       │   └── ObjectPoolTests.cs
│       ├── Window/
│       │   ├── MonitorHelperTests.cs
│       │   └── WindowPositionManagerTests.cs
│       └── Dialogs/
│           ├── DialogParamsTests.cs
│           └── InputBoxParamsTests.cs
├── README.md
├── LICENSE
└── .gitignore
```

---

## Estimated Effort

| Group | Tasks | Est. Time |
|-------|-------|-----------|
| A: Scaffolding | 3 | 15 min |
| B: Common (9 classes) | 9 | 90 min |
| C: Native (3 tasks) | 3 | 30 min |
| D: Window (5 tasks) | 5 | 60 min |
| E: Dialogs (4 tasks) | 4 | 60 min |
| F: Migration (3 tasks) | 3 | 30 min |
| G: Tests (~30 test files) | 6 | 60 min |
| **Total** | **~33** | **~5 hours** |

---

## Ready to Execute?

This plan is ready. To execute, I recommend:

1. **Group A + G first** (scaffolding + verify with a trivial test)
2. **Group B** one class at a time (TDD: test → code → pass)
3. **Group C** (split + clean up)
4. **Group D + E** (new modules)
5. **Group F** (migration, cleanup)

Shall I proceed with **Group A (scaffolding)** now, or do you want to adjust the plan?
