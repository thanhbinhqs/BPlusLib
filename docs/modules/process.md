# Process

Provides process management utilities including extension methods for `System.Diagnostics.Process` (parent process lookup, command line retrieval, elevation check, process tree killing, image path retrieval, async wait) and a pure P/Invoke-based command runner that captures stdout/stderr without WMI dependencies.

## Classes

### ProcessExtensions
Extension methods for `System.Diagnostics.Process` using pure P/Invoke. All methods are thread-safe and gracefully degrade on non-Windows runtimes.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetParentProcessId(process) | int | Gets the parent process ID via NtQueryInformationProcess (returns 0 on failure) |
| GetCommandLine(process) | string? | Gets the full command line by reading PEB→ProcessParameters via ReadProcessMemory |
| IsElevated(process) | bool | Checks if the process is running with elevated (UAC) privileges |
| KillTree(process) | void | Kills the process and all descendants recursively (graceful then force) |
| GetImagePath(process) | string? | Gets the full executable path via QueryFullProcessImageNameW |
| WaitForExitAsync(process, timeoutMs) | Task<bool> | Asynchronously waits for the process to exit using ThreadPool.RegisterWaitForSingleObject |

### CommandRunner
Provides methods to execute external processes synchronously and asynchronously, capturing stdout and stderr using pure P/Invoke (no WMI). Thread-safe.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| RunCommand(fileName, arguments, workingDirectory?, timeoutMs) | CommandRunnerResult | Runs an executable synchronously, capturing output |
| RunCommandAsync(fileName, arguments, workingDirectory?, timeoutMs) | Task\<CommandRunnerResult\> | Async wrapper around RunCommand |

### CommandRunnerResult
Represents the result of executing a command via `CommandRunner`.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| ExitCode | int | Process exit code |
| StandardOutput | string | Full stdout text (UTF-8 decoded) |
| StandardError | string | Full stderr text (UTF-8 decoded) |
| TimedOut | bool | Whether the operation timed out |
| Succeeded | bool | True if exit code is 0 and no timeout |

## Usage

```csharp
using BPlusLib.Foundation.Process;

// Get parent process ID
var proc = System.Diagnostics.Process.GetCurrentProcess();
int parentId = proc.GetParentProcessId();

// Get command line of another process
string? cmdLine = proc.GetCommandLine();

// Check if elevated
bool elevated = proc.IsElevated();

// Kill process tree
proc.KillTree();

// Get image path
string? path = proc.GetImagePath();

// Async wait
bool exited = await proc.WaitForExitAsync(timeoutMs: 5000);

// Run a command
var result = CommandRunner.RunCommand("git", "status");
Console.WriteLine($"Exit: {result.ExitCode}, Output: {result.StandardOutput}");

// Async command
var asyncResult = await CommandRunner.RunCommandAsync("ping", "localhost -n 3");
```

## Dependencies
- `kernel32.dll` — OpenProcess, ReadProcessMemory, QueryFullProcessImageNameW, CreateProcessW, CreatePipe, WaitForSingleObject, TerminateProcess
- `ntdll.dll` — NtQueryInformationProcess, NtQuerySystemInformation
- `advapi32.dll` — OpenProcessToken, GetTokenInformation
- `psapi.dll` — GetModuleFileNameExW (fallback)
