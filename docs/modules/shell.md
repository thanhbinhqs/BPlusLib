# Shell

Windows Shell operations including dark/light theme detection, file association queries, system tray notification icons, shortcut (.lnk) management, file verb execution, recycle bin management, and Open With dialog. All methods use pure P/Invoke and gracefully return false/null on non-Windows.

## Classes

### ThemeHelper
Detects Windows dark/light theme, accent color, and DWM composition.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| IsLightTheme() | bool | True if the system is in light mode (apps) |
| IsDarkTheme() | bool | True if the system is in dark mode (apps) |
| IsAppsLightTheme() | bool | True if apps use light theme |
| IsSystemLightTheme() | bool | True if the taskbar uses light theme |
| GetAccentColor() | uint | Gets accent color as 0xAARRGGBB integer |
| IsCompositionEnabled() | bool | Whether DWM composition is enabled |
| SetWindowDarkMode(hwnd, dark) | bool | Applies dark mode to a window (Win10 20H1+) |

### AssocHelper
File extension association queries via AssocQueryString and registry.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetFileTypeDescription(extension) | string? | Friendly description (e.g. ".txt" → "Text Document") |
| GetAssociatedExecutable(extension) | string? | Executable path for the extension |
| GetOpenCommand(extension) | string? | Open command template |
| GetProgId(extension) | string? | ProgID from registry |
| GetContentType(extension) | string? | MIME content type |
| IsExtensionRegistered(extension) | bool | Whether the extension has a registered association |

### NotifyIcon / NotifyIconHelper
System tray notification icons with balloon tip support.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| NotifyIconHelper.Create(hWnd, callbackMessageId, iconHandle, id, tooltipText) | NotifyIcon | Creates a tray icon instance |
| NotifyIcon.Show() | bool | Adds the icon to the notification area |
| NotifyIcon.Hide() | bool | Removes the icon from the notification area |
| NotifyIcon.Update() | bool | Updates the icon in-place |
| NotifyIcon.ShowBalloonTip(title, text, iconType, timeoutMs) | bool | Shows a balloon tip notification |
| NotifyIcon.SetIcon(iconHandle) | bool | Updates the icon handle |
| NotifyIcon.SetTooltip(tooltip) | bool | Updates the tooltip text |
| NotifyIcon.IsVisible | bool | Whether the icon is visible in the tray |

### ShortcutInfo
Properties of a Windows Shell shortcut (.lnk).

| Property | Type | Description |
|----------|------|-------------|
| TargetPath | string | Target executable or document path |
| Arguments | string? | Command-line arguments |
| WorkingDirectory | string? | Working directory |
| Description | string? | Description/comment |
| IconLocation | string? | Icon file location |
| IconIndex | int | Icon index within the icon file |
| ShowCommand | int | Show command (1=normal, 3=maximized, 7=minimized) |
| Hotkey | string? | Hotkey for the shortcut |

### ShortcutHelper
Read, create, and query Windows Shell shortcuts via IShellLink COM.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Read(shortcutPath) | ShortcutInfo? | Reads properties of a .lnk file |
| Create(shortcutPath, info) | bool | Creates or updates a .lnk file |
| IsShortcut(path) | bool | Whether the path is a .lnk file |
| GetTargetPath(shortcutPath) | string? | Quick method to get just the target path |

### ShellHelper
Win32 Shell operations: file verbs, file associations, recycle bin management.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| ExecuteVerb(filePath, verb, arguments?, runAs?) | bool | Executes a shell verb (open, edit, print, etc.) |
| GetAvailableVerbs(filePath) | IReadOnlyList\<string\> | Lists available verbs for a file |
| GetDefaultProgram(extension) | string? | Default program for a file extension |
| IsDefaultProgramForExtension(programPath, extension) | bool | Whether a program is the default handler |
| SetFileAssociation(extension, programPath, friendlyName?) | bool | Sets file association (requires admin) |
| GetProgId(extension) | string? | ProgID for a file extension |
| OpenWithDialog(filePath) | bool | Opens the "Open with" dialog |
| GetRecycleBinSize() | (long size, long count) | Gets recycle bin size and item count |
| EmptyRecycleBin(noConfirm?, noProgress?, noSound?) | bool | Empties the recycle bin |
| GetFileType(extension) | string? | Gets the file type name |
| GetFileTypeDescription(filePath) | string? | Gets the file type description |

### Enums

| Enum | Values | Description |
|------|--------|-------------|
| BalloonIconType | None, Info, Warning, Error | Balloon tip icon types |

## Usage

```csharp
using BPlusLib.Foundation.Shell;

// Theme detection
if (ThemeHelper.IsDarkTheme())
    ThemeHelper.SetWindowDarkMode(form.Handle, true);

// File associations
string? desc = AssocHelper.GetFileTypeDescription(".txt");
string? exe = AssocHelper.GetAssociatedExecutable(".cs");
string? progId = AssocHelper.GetProgId(".txt");

// Shortcuts
var info = ShortcutHelper.Read(@"C:\MyApp.lnk");
ShortcutHelper.Create(@"C:\MyLink.lnk", new ShortcutInfo
{
    TargetPath = @"C:\MyApp.exe",
    Arguments = "--config prod",
    WorkingDirectory = @"C:\MyApp"
});

// Shell verbs
ShellHelper.ExecuteVerb(@"C:\document.pdf", "print");
var verbs = ShellHelper.GetAvailableVerbs(@"C:\file.txt");

// System tray
var trayIcon = NotifyIconHelper.Create(hWnd, WM_APP + 1, hIcon, tooltipText: "My App");
trayIcon.Show();
trayIcon.ShowBalloonTip("Title", "Hello!", BalloonIconType.Info);

// Recycle bin
var (size, count) = ShellHelper.GetRecycleBinSize();
ShellHelper.EmptyRecycleBin();
```

## Dependencies
- `dwmapi.dll` — DwmGetColorizationColor, DwmIsCompositionEnabled, DwmSetWindowAttribute
- `shlwapi.dll` — AssocQueryStringW
- `shell32.dll` — Shell_NotifyIconW, ShellExecuteExW, SHQueryRecycleBinW, SHEmptyRecycleBinW, SHGetFileInfoW, IShellLink COM
- `ole32.dll` — CoCreateInstance (for IShellLink COM)
- `BPlusLib.Foundation.Native` — Shared P/Invoke declarations
