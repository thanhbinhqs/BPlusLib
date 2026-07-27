# Clipboard

Provides Win32 P/Invoke-based clipboard operations for reading and writing text and file lists. All methods are thread-safe and gracefully return false/null on non-Windows platforms.

## Classes

### ClipboardHelper
Static helper class that provides clipboard access via Win32 APIs (user32.dll, kernel32.dll). Uses thread-safe locking and handles non-Windows platforms gracefully.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| TrySetText(string text) | bool | Sets the clipboard text to the specified Unicode string |
| TryGetText() | string? | Retrieves Unicode text from the clipboard |
| TrySetFiles(string[] filePaths) | bool | Sets a list of file paths on the clipboard using CF_HDROP format |
| TryGetFiles() | string[]? | Retrieves file paths from the clipboard (CF_HDROP format) |

### ClipboardFormat
Enum defining standard Windows clipboard format identifiers (CF_TEXT, CF_BITMAP, CF_UNICODETEXT, CF_HDROP, etc.).

| Value | Description |
|-------|-------------|
| CF_TEXT | 1 — ANSI text |
| CF_BITMAP | 2 — Bitmap image |
| CF_UNICODETEXT | 13 — Unicode text |
| CF_HDROP | 15 — File list (drag and drop) |

## Usage

```csharp
using BPlusLib.Foundation.Clipboard;

// Set text on clipboard
bool success = ClipboardHelper.TrySetText("Hello, World!");

// Get text from clipboard
string? text = ClipboardHelper.TryGetText();

// Set files on clipboard
string[] files = { @"C:\file1.txt", @"C:\file2.pdf" };
ClipboardHelper.TrySetFiles(files);

// Get files from clipboard
string[]? clipboardFiles = ClipboardHelper.TryGetFiles();
```

## Dependencies
- user32.dll (OpenClipboard, CloseClipboard, SetClipboardData, GetClipboardData, etc.)
- kernel32.dll (GlobalAlloc, GlobalLock, GlobalUnlock, GlobalFree, GlobalSize)
