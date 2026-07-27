# Graphics

Display and graphics helpers including screen capture, DPI queries, icon extraction, and a circular progress bar control. Uses Win32 P/Invoke (GDI32, User32, Shcore) for display operations.

## Classes

### ScreenHelper
Provides screen capture and display information retrieval via Win32 P/Invoke. All methods are thread-safe and return default values on non-Windows.

| Method | Returns | Description |
|--------|---------|-------------|
| CaptureScreenRaw() | (byte[] PixelData, int Width, int Height)? | Captures entire virtual screen as BGRA pixels |
| CaptureScreenAsPng() | byte[]? | Captures screen as PNG bytes (.NET Framework only) |
| GetDisplayDevices() | IReadOnlyList&lt;DisplayInfo&gt; | Enumerates all display monitors |
| GetVirtualScreenBounds() | DisplayRect | Gets virtual screen bounds |

### DisplayInfo
Detailed information about a single display monitor.

| Property | Type | Description |
|----------|------|-------------|
| DeviceName | string | Windows device name |
| DeviceString | string | Human-readable device string |
| Bounds | DisplayRect | Display bounds in virtual coordinates |
| WorkingArea | DisplayRect | Working area (excluding taskbar) |
| IsPrimary | bool | Whether this is the primary display |
| DpiX | int | Horizontal DPI |
| DpiY | int | Vertical DPI |
| BitsPerPixel | int | Color depth |
| RefreshRate | int | Refresh rate in Hz |

### DisplayRect
Rectangle struct for display coordinates (avoids System.Drawing dependency).

| Property | Type | Description |
|----------|------|-------------|
| X, Y | int | Upper-left coordinates |
| Width, Height | int | Dimensions |
| Left, Top, Right, Bottom | int | Edge coordinates |
| IsEmpty | bool | True if zero area |

### DisplayHelper
DPI queries, screen resolution changes, high-contrast detection, and scale factor queries.

| Method | Returns | Description |
|--------|---------|-------------|
| GetDpiForWindow(IntPtr hwnd) | int | Gets DPI for a window |
| GetDpiForMonitor(IntPtr hmonitor) | (int DpiX, int DpiY) | Gets DPI for a monitor |
| SetScreenResolution(int w, int h, int bpp, int refresh) | bool | Sets screen resolution (requires admin) |
| IsHighContrastMode() | bool | Detects high contrast mode |
| GetColorDepth() | int | Gets color depth in bits per pixel |
| GetScreenScaleFactor() | double | Gets screen scale factor (e.g., 1.25 for 125%) |

### IconExtractor
Extracts icons from executables, DLLs, and ICO files via Win32 P/Invoke.

| Method | Returns | Description |
|--------|---------|-------------|
| ExtractIconRaw(string filePath, int size) | (byte[] PixelData, int Width, int Height)? | Extracts icon as raw BGRA pixels |
| ExtractIconAsPng(string filePath, int size) | byte[]? | Extracts icon as PNG bytes |
| TryExtractIcon(string filePath, out byte[]?, int size) | bool | Safe icon extraction |
| GetAssociatedIcons(string extension) | IReadOnlyList&lt;string&gt; | Gets icon paths for file extension |

### CircularProgressBar
A circular progress bar control with customizable text, animation, and color options.

| Property | Type | Description |
|----------|------|-------------|
| Value | int | Current progress value (clamped) |
| Minimum / Maximum | int | Range bounds |
| DisplayText | string? | Custom center text |
| ShowPercentage | bool | Auto-display percentage |
| ProgressColor | Color | Ring color |
| ProgressColor2 | Color | Gradient end color |
| TrackColor | Color | Background ring color |
| TextColor | Color | Percentage text color |
| LineWidth | int | Ring line width |
| AnimationEnabled | bool | Enable smooth animation |
| AnimationSpeed | int | Animation tick interval (ms) |
| Percentage | float | Computed percentage |
| ValueChanged | event | Fires when Value changes |

| Method | Returns | Description |
|--------|---------|-------------|
| SetRange(int min, int max) | void | Sets minimum and maximum |

## Usage

```csharp
using BPlusLib.Foundation.Graphics;

// Screen capture
var raw = ScreenHelper.CaptureScreenRaw();
if (raw.HasValue)
{
    byte[] pixels = raw.Value.PixelData;
    int w = raw.Value.Width;
    int h = raw.Value.Height;
}

// Display info
IReadOnlyList<DisplayInfo> displays = ScreenHelper.GetDisplayDevices();
foreach (var display in displays)
{
    Console.WriteLine($"{display.DeviceName}: {display.Bounds.Width}x{display.Bounds.Height}");
}

// DPI queries
int dpi = DisplayHelper.GetDpiForWindow(hwnd);
double scale = DisplayHelper.GetScreenScaleFactor();

// Icon extraction
byte[]? iconPng = IconExtractor.ExtractIconAsPng(@"C:\app.exe", 64);

// Circular progress bar (in designer or code)
var progress = new CircularProgressBar
{
    Value = 75,
    ProgressColor = Color.DodgerBlue,
    AnimationEnabled = true,
    ShowPercentage = true
};
```

## Dependencies
- gdi32.dll (CreateDC, BitBlt, GetDIBits, GetDeviceCaps, DeleteObject, etc.)
- user32.dll (GetSystemMetrics, EnumDisplayDevices, ChangeDisplaySettings, MonitorFromPoint, etc.)
- shcore.dll (GetDpiForMonitor, GetScaleFactorForMonitor)
- shell32.dll (ExtractIconEx, SHGetFileInfo)
- System.Drawing (for CircularProgressBar and PNG encoding on .NET Framework)
