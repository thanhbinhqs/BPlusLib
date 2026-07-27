# Threading

Cross-platform thread management and synchronization helpers. All methods are pure .NET (no P/Invoke) and work on Linux, macOS, and Windows. Includes STA/MTA thread creation, UI-thread detection, marshalling, delayed execution, and thread-safe locked execution wrappers.

## Classes

### ThreadHelper
Static thread-management and synchronization utilities.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetApartmentState() | ApartmentState | Gets the current thread's apartment state |
| IsUIThread() | bool | Whether the current thread is a UI thread (WinForms/WPF) |
| RunInSta(action) | void | Runs an action on a new STA thread (blocks) |
| RunInSta\<T\>(func) | T? | Runs a function on a new STA thread and returns its result |
| RunInStaAsync(action) | Task | Async wrapper: runs action on STA thread |
| RunInStaAsync\<T\>(func) | Task\<T?\> | Async wrapper: runs function on STA thread |
| RunInMta(action) | void | Runs an action on a new MTA thread (blocks) |
| IsMainThread() | bool | Whether the current thread is the main thread |
| GetUiSynchronizationContext() | SynchronizationContext? | Gets the UI SynchronizationContext if available |
| SwitchToUiThread(action) | void | Marshals an action to the UI thread |
| DelayExecute(delayMs, action) | void | Executes an action after a delay on the thread pool |
| LockedExecute\<T\>(func, lockObject) | T? | Executes a function inside a lock |
| LockedExecute(action, lockObject) | void | Executes an action inside a lock |

## Usage

```csharp
using BPlusLib.Foundation.Threading;

// STA thread for COM interop
ThreadHelper.RunInSta(() =>
{
    // COM operations requiring STA
    var clipboard = System.Windows.Forms.Clipboard.GetText();
});

// Async STA
string? text = await ThreadHelper.RunInStaAsync(() =>
{
    return System.Windows.Forms.Clipboard.GetText();
});

// UI thread marshalling
ThreadHelper.SwitchToUiThread(() =>
{
    label.Text = "Updated from background thread";
});

// Delayed execution
ThreadHelper.DelayExecute(1000, () =>
{
    Console.WriteLine("This runs after 1 second");
});

// Thread-safe locked execution
var result = ThreadHelper.LockedExecute(() =>
{
    return sharedCounter++;
}, lockObject);

// Check if on UI thread
if (ThreadHelper.IsUIThread())
    // Direct UI update
else
    ThreadHelper.SwitchToUiThread(() => UpdateUI());

// Main thread check
if (ThreadHelper.IsMainThread())
    Console.WriteLine("Running on the main thread");
```

## Dependencies
- None (pure .NET, no P/Invoke)
- `System.Threading` — Thread, SynchronizationContext, ApartmentState
- `System.Threading.Tasks` — Task, TaskScheduler
