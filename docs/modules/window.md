# Window

WinForms window management utilities including window animations (flash, shake, fade), position persistence via registry, edge-based resizing for borderless forms, drag-move for custom title bars, and display monitor enumeration. Most Window module classes require the `FEATURE_WINDOW_MODULE` compile flag.

## Classes

### WindowAnimation
Provides async window animation utilities: flash, shake, fade-in, and fade-out. All methods support cancellation.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| FlashAsync(form, flashCount?, ct?) | Task | Flashes the form's caption and taskbar button |
| ShakeAsync(form, intensity?, duration?, ct?) | Task | Shakes the form horizontally |
| FadeInAsync(form, duration?, steps?, ct?) | Task | Fades the form in (opacity 0→1) |
| FadeOutAsync(form, duration?, steps?, ct?) | Task | Fades the form out (opacity 1→0) |

### WindowPositionManager
Manages window position persistence using the Windows registry. Saves/restores form location, size, window state, and target monitor.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| WindowPositionManager(applicationName) | Constructor | Creates instance with registry namespace |
| ApplicationName | string | Application name for registry keys |
| Save(form, key) | void | Saves current position, size, state, and monitor |
| Restore(form, key) | bool | Restores previously saved state (validates monitor exists) |
| Reset(key) | void | Removes saved window position from registry |

### ResizeHelper
Enables edge-based resizing on forms with no standard window border.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| EnableResize(form, borderWidth?) | void | Enables resizing by dragging edges |
| DisableResize(form) | void | Disables edge-based resizing |

### DragMoveHelper
Enables form dragging by emulating title-bar drag behavior for custom/borderless forms.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Attach(form) | void | Attaches drag-move to the entire form surface |
| Attach(form, dragArea) | void | Attaches drag-move restricted to a specific control |
| Detach(form) | void | Detaches drag-move behavior |

### MonitorHelper
Display monitor enumeration and DPI queries.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetAllMonitors() | IReadOnlyList\<MonitorInfo\> | Gets info for all display monitors |
| GetPrimaryMonitor() | MonitorInfo | Gets the primary monitor |
| GetMonitorFrom(hwnd) | MonitorInfo | Gets the monitor containing a window |
| GetMonitorFrom(point) | MonitorInfo | Gets the monitor containing a point |
| GetVirtualScreen() | Rectangle | Virtual screen rectangle (all monitors) |
| GetWorkingArea() | Rectangle | Working area of primary monitor |
| GetWorkingAreaFrom(hwnd) | Rectangle | Working area of the monitor with a window |
| GetDpiForWindow(hwnd) | DpiScale | DPI scale for a window |
| IsHighDpi() | bool | Whether primary monitor has high DPI (>96) |

### MonitorInfo
Information about a display monitor (readonly struct).

| Property | Type | Description |
|----------|------|-------------|
| Name | string | Friendly name |
| Bounds | Rectangle | Bounding rectangle in screen coordinates |
| WorkingArea | Rectangle | Working area (excluding taskbar) |
| IsPrimary | bool | Whether this is the primary display |
| Dpi | int | DPI value |
| DeviceId | string | Device identifier |
| Handle | IntPtr | Native monitor handle |

### DpiScale
DPI scaling factors (readonly struct).

| Property | Type | Description |
|----------|------|-------------|
| X | float | Horizontal DPI scale factor |
| Y | float | Vertical DPI scale factor |
| Scale | float | Average of X and Y |

## Usage

```csharp
using BPlusLib.Foundation.Window;

// Window animations (async, cancellable)
var cts = new CancellationTokenSource();
await WindowAnimation.FadeInAsync(form, TimeSpan.FromMilliseconds(300), ct: cts.Token);
await WindowAnimation.ShakeAsync(form, intensity: 10);
await WindowAnimation.FlashAsync(form, flashCount: 5);
await WindowAnimation.FadeOutAsync(form);

// Position persistence
var posMgr = new WindowPositionManager("MyApp");
posMgr.Save(form, "MainWindow");
// Later...
posMgr.Restore(form, "MainWindow");

// Borderless form: drag + resize
DragMoveHelper.Attach(form); // Entire form draggable
DragMoveHelper.Attach(form, titleBarPanel); // Only title bar
ResizeHelper.EnableResize(form, borderWidth: 6);

// Monitor info
var monitors = MonitorHelper.GetAllMonitors();
foreach (var m in monitors)
    Console.WriteLine($"{m.Name}: {m.Bounds}, DPI={m.Dpi}");

var primary = MonitorHelper.GetPrimaryMonitor();
bool highDpi = MonitorHelper.IsHighDpi();
```

## Dependencies
- `user32.dll` — FlashWindowEx, MonitorFromWindow, MonitorFromPoint, GetMonitorInfoW, GetDpiForMonitor, GetDpiForWindow, GetSystemMetrics, SystemParametersInfoW, GetWindowRect, ScreenToClient
- `dwmapi.dll` — (indirect, via native interop)
- `Microsoft.Win32` — Registry (for position persistence)
- `BPlusLib.Foundation.Common` — Guard utilities
- `BPlusLib.Foundation.Native` — Shared P/Invoke declarations
