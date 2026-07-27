# Explorer

Provides Win32 P/Invoke-based helper methods for Windows Explorer integration, including known folder resolution, file operations, shortcut resolution, and Recycle Bin support. All methods are thread-safe and gracefully return false/null on non-Windows platforms.

## Classes

### ExplorerHelper
Static helper class for Windows Explorer operations.

| Method | Returns | Description |
|--------|---------|-------------|
| GetKnownFolderPath(KnownFolder folder) | string? | Gets file system path for a known folder |
| OpenInExplorer(string path) | bool | Opens file or folder in Windows Explorer |
| SelectInExplorer(string path) | bool | Opens Explorer with file/folder selected |
| ShowFileProperties(string path) | bool | Shows Windows file properties dialog |
| ShowFileInExplorer(string path) | bool | Alias for SelectInExplorer |
| GetFileSizeOnDisk(string path) | long? | Gets actual size on disk (compressed size) |
| IsFileInUse(string path) | bool | Checks if file is locked by another process |
| GetFileTypeDescription(string path) | string? | Gets file type description (e.g., "Text Document") |
| GetFileOwner(string path) | string? | Gets file owner in DOMAIN\USER format |
| GetRecentFiles(int maxCount) | IReadOnlyList&lt;string&gt; | Gets recently opened files from Recent folder |
| ResolveShortcut(string shortcutPath) | string? | Resolves .lnk shortcut to target path |
| TryRecycle(string path) | bool | Moves file/folder to Recycle Bin |

### KnownFolder
Enum mapping to KNOWNFOLDERID GUIDs for standard Windows folders.

| Value | Description |
|-------|-------------|
| Desktop | The desktop folder |
| Documents | The Documents folder |
| Downloads | The Downloads folder |
| Pictures | The Pictures folder |
| Music | The Music folder |
| Videos | The Videos folder |
| Recent | The Recent items folder |
| SendTo | The SendTo folder |
| Startup | The Startup folder |
| Programs | The Programs folder |
| AppData | The AppData (Roaming) folder |
| LocalAppData | The Local AppData folder |
| Temp | The Temp folder |
| System | The System folder (SYSTEM32) |
| Windows | The Windows folder |
| Fonts | The Fonts folder |
| Favorites | The Favorites folder |
| Links | The Links folder |
| SavedGames | The SavedGames folder |
| Screenshots | The Screenshots folder |

## Usage

```csharp
using BPlusLib.Foundation.Explorer;

// Get known folder paths
string? desktop = ExplorerHelper.GetKnownFolderPath(KnownFolder.Desktop);
string? downloads = ExplorerHelper.GetKnownFolderPath(KnownFolder.Downloads);
string? appData = ExplorerHelper.GetKnownFolderPath(KnownFolder.LocalAppData);

// Open file in Explorer
ExplorerHelper.OpenInExplorer(@"C:\MyFolder");
ExplorerHelper.SelectInExplorer(@"C:\MyFile.txt");

// Show properties dialog
ExplorerHelper.ShowFileProperties(@"C:\MyFile.txt");

// Check if file is locked
bool inUse = ExplorerHelper.IsFileInUse(@"C:\locked.dat");

// Get file owner
string? owner = ExplorerHelper.GetFileOwner(@"C:\MyFile.txt");

// Get recent files
IReadOnlyList<string> recent = ExplorerHelper.GetRecentFiles(20);

// Resolve shortcut
string? target = ExplorerHelper.ResolveShortcut(@"C:\MyShortcut.lnk");

// Move to Recycle Bin
bool recycled = ExplorerHelper.TryRecycle(@"C:\OldFile.txt");
```

## Dependencies
- shell32.dll (SHGetKnownFolderPath, SHGetFileInfo, SHFileOperation, ShellExecuteEx)
- kernel32.dll (GetCompressedFileSize, CreateFile, CloseHandle)
- advapi32.dll (GetSecurityInfo, LookupAccountSid)
- ole32.dll (CoCreateInstance for IShellLink)
