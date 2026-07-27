# Input

Keyboard and mouse input simulation via SendInput, and global hotkey registration. All methods are thread-safe and gracefully return false on failure.

## Classes

### InputHelper
Provides keyboard and mouse input simulation via SendInput. Subject to UIPI (User Interface Privilege Isolation)—a lower-integrity process cannot send input to a higher-integrity window.

| Method | Returns | Description |
|--------|---------|-------------|
| SendKeyPress(VirtualKeyCode keyCode) | bool | Sends a single key press (down followed by up) |
| SendModifiedKey(VirtualKeyCode modifier, VirtualKeyCode key) | bool | Sends a modifier+key combination (e.g., Ctrl+C, Alt+Tab) |
| KeyDown(VirtualKeyCode keyCode) | bool | Sends a key-down event for the specified key |
| KeyUp(VirtualKeyCode keyCode) | bool | Sends a key-up event for the specified key |
| SendText(string text) | bool | Types text by sending Unicode key events for each character |
| MoveMouse(int x, int y, bool relative) | bool | Moves the mouse cursor (absolute or relative) |
| LeftClick() | bool | Simulates a left mouse button click |
| RightClick() | bool | Simulates a right mouse button click |
| MiddleClick() | bool | Simulates a middle mouse button click |
| ScrollWheel(int delta) | bool | Simulates a mouse wheel scroll (positive=up, negative=down) |

### HotkeyModifiers
Modifier keys for hotkey registration (Flags enum).

| Value | Description |
|-------|-------------|
| None | No modifier |
| Alt | Alt key |
| Control | Ctrl key |
| Shift | Shift key |
| Win | Windows logo key |
| NoRepeat | No repeat when holding |

### HotkeyRegistration
Represents a registered global hotkey. Disposing unregisters it. The specified window receives WM_HOTKEY (0x0312) messages when the hotkey is pressed.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Register(IntPtr hWnd, int id, HotkeyModifiers modifiers, byte virtualKey) | HotkeyRegistration? | Registers a global hotkey |
| Unregister() | bool | Unregisters the hotkey |
| WindowHandle | IntPtr | Gets the window handle associated with this registration |
| Id | int | Gets the hotkey identifier |
| Dispose() | void | Unregisters the hotkey |

## Usage

```csharp
using BPlusLib.Foundation.Input;

// Simulate keyboard input
InputHelper.SendKeyPress(VirtualKeyCode.VK_RETURN);
InputHelper.SendModifiedKey(VirtualKeyCode.VK_CONTROL, VirtualKeyCode.VK_C); // Ctrl+C
InputHelper.SendText("Hello World");

// Simulate mouse input
InputHelper.LeftClick();
InputHelper.MoveMouse(500, 300, relative: false);
InputHelper.ScrollWheel(120); // Scroll up

// Register a global hotkey (Ctrl+Shift+L)
var hotkey = HotkeyRegistration.Register(hWnd, 1, HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x4C);
// ... handle WM_HOTKEY messages in your window procedure ...
hotkey?.Dispose(); // Unregisters the hotkey
```

## Dependencies
- `BPlusLib.Foundation.Native` (for `User32` P/Invoke: `SendInput`, `RegisterHotKey`, `UnregisterHotKey`, and input structs)
- Windows-only (SendInput / RegisterHotKey APIs)
