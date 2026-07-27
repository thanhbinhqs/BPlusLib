# Dialogs

Extended dialog components for Windows Forms applications, including enhanced message boxes, input dialogs, and progress dialogs with dark mode support, DPI awareness, and async operation.

> **Note:** This module requires the `FEATURE_WINDOW_MODULE` compilation symbol and System.Windows.Forms.

## Classes

### MessageBoxEx
Extended message box with centering, dark mode, timeout, and async support. Uses WH_CBT hook for centering and DWM for dark mode.

| Method | Returns | Description |
|--------|---------|-------------|
| ShowAsync(DialogParams, CancellationToken) | Task&lt;DialogResult&gt; | Shows message box asynchronously with full parameters |
| ShowAsync(IWin32Window?, string text, string caption, DialogButton, DialogIcon, CancellationToken) | Task&lt;DialogResult&gt; | Shows message box with simple parameters |
| Show(IntPtr parentHandle, string text, string caption, DialogButton, DialogIcon) | DialogResult | Shows message box synchronously (backward-compatible) |
| Show(string text, string caption, DialogButton, DialogIcon) | DialogResult | Shows message box synchronously without parent |

### InputBoxEx
Displays a modal input dialog with label, text box, and OK/Cancel buttons. Supports custom validation and dark mode.

| Method | Returns | Description |
|--------|---------|-------------|
| ShowAsync(InputBoxParams, CancellationToken) | Task&lt;InputBoxResult&lt;string&gt;&gt; | Shows input dialog asynchronously |

### ProgressDialog
Modal progress dialog with a progress bar, status text, and optional cancel button. Thread-safe: Report() can be called from any thread.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| ProgressDialog(IWin32Window?, ProgressDialogParams) | — | Creates a progress dialog |
| CancellationToken | CancellationToken | Gets token signalled when user cancels |
| ShowAsync(CancellationToken) | Task | Shows dialog asynchronously |
| Report(int percent, string? statusText) | void | Reports progress (0-100) with optional status text |
| Dispose() | void | Closes the dialog |

### DialogParams
Parameters for displaying a message box dialog.

| Property | Type | Description |
|----------|------|-------------|
| Text | string | Message text |
| Caption | string | Dialog title |
| Details | string? | Detailed text (expandable area) |
| CheckboxText | string? | Checkbox label (shows checkbox if set) |
| CheckboxState | bool | Initial checkbox state |
| Owner | IWin32Window? | Owner window |
| Buttons | DialogButton | Button configuration |
| Icon | DialogIcon | Icon to display |
| DarkMode | DarkModeStyle | Dark mode style |
| TimeoutMs | int | Auto-close timeout (ms) |
| TimeoutResult | DialogResult | Result on timeout |
| TopMost | bool | Whether dialog is topmost |
| CustomButtons | List&lt;DialogCustomButton&gt;? | Custom buttons |

### InputBoxParams
Parameters for displaying an input box dialog.

| Property | Type | Description |
|----------|------|-------------|
| Title | string | Dialog title |
| Label | string | Label above input field |
| DefaultValue | string? | Pre-filled value |
| Placeholder | string? | Placeholder text |
| UsePasswordMask | bool | Password input mode |
| Validator | Func&lt;string, string?&gt;? | Validation function (returns null on success) |
| Owner | IWin32Window? | Owner window |
| DarkMode | DarkModeStyle | Dark mode style |

### InputBoxResult&lt;T&gt;
Result of an input box dialog.

| Property | Type | Description |
|----------|------|-------------|
| Confirmed | bool | Whether user clicked OK |
| Value | T? | The entered value |

### ProgressDialogParams
Parameters for a progress dialog.

| Property | Type | Description |
|----------|------|-------------|
| Title | string | Dialog title |
| Text | string | Status text above progress bar |
| IsIndeterminate | bool | Indeterminate mode |
| Maximum | int | Max progress value (default: 100) |
| ShowCancelButton | bool | Show cancel button |
| TopMost | bool | Topmost window |
| DarkMode | DarkModeStyle | Dark mode style |

### ProgressReport
Progress update data for ProgressDialog.

| Property | Type | Description |
|----------|------|-------------|
| Percent | int | Progress percentage (0-100) |
| StatusText | string? | Status text |
| IsIndeterminate | bool | Indeterminate mode |

### DialogCustomButton
Custom button definition for dialogs.

| Property | Type | Description |
|----------|------|-------------|
| Text | string | Button text |
| Result | DialogResult | Result when clicked |
| IsDefault | bool | Default (Enter) button |
| IsCancel | bool | Cancel (Esc) button |

## Enums

### DialogButton
OK, OKCancel, YesNo, YesNoCancel, AbortRetryIgnore, RetryCancel, CancelTryContinue

### DialogIcon
None, Information, Question, Warning, Error, Shield

### DialogResult
None, OK, Cancel, Abort, Retry, Ignore, Yes, No, TryAgain, Continue

### DarkModeStyle
System, Light, Dark, Inherit

## Usage

```csharp
using BPlusLib.Foundation.Dialogs;

// Show async message box
var result = await MessageBoxEx.ShowAsync(
    owner: mainWindow,
    text: "Are you sure?",
    caption: "Confirm",
    buttons: DialogButton.YesNo,
    icon: DialogIcon.Question);

// Show input box
var input = await InputBoxEx.ShowAsync(new InputBoxParams
{
    Title = "Enter Value",
    Label = "Please enter your name:",
    Placeholder = "John Doe",
    Validator = string.IsNullOrEmpty ? "Name is required" : null
});

if (input.Confirmed)
{
    Console.WriteLine($"User entered: {input.Value}");
}

// Show progress dialog
using var progress = new ProgressDialog(mainWindow, new ProgressDialogParams
{
    Title = "Processing",
    Text = "Please wait...",
    ShowCancelButton = true
});

var progressTask = progress.ShowAsync();

// Report progress from background work
for (int i = 0; i <= 100; i += 10)
{
    progress.Report(i, $"Processing {i}%...");
    await Task.Delay(100);
}

progress.Dispose();
await progressTask;
```

## Dependencies
- System.Windows.Forms (for dialog forms)
- BPlusLib.Foundation.Native (User32, Kernel32 P/Invoke)
- BPlusLib.Foundation.Window (MonitorHelper, IWin32Window)
- BPlusLib.Foundation.Common (Guard)
