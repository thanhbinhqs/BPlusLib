# Services

Windows service management, Restart Manager integration, console management, and Job Object process group control. All methods use pure P/Invoke into advapi32/kernel32 and are thread-safe.

## Classes

### ServiceHelper
Thread-safe static methods for managing Windows services through the Service Control Manager (SCM). Gracefully returns null/false on failure.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetService(serviceName) | ServiceInfo? | Retrieves detailed information about a service |
| StartService(serviceName, waitMs?) | bool | Starts a service and optionally waits for running state |
| StopService(serviceName, waitMs?) | bool | Stops a service and optionally waits for stopped state |
| PauseService(serviceName, waitMs?) | bool | Pauses a service |
| ResumeService(serviceName, waitMs?) | bool | Resumes a paused service |
| RestartService(serviceName, stopWaitMs?, startWaitMs?) | bool | Stops then starts a service |
| IsServiceRunning(serviceName) | bool | Checks if a service is currently running |
| SetStartType(serviceName, startType) | bool | Changes the service start type |
| EnumerateServices() | IReadOnlyList\<ServiceInfo\> | Lists all installed services |

### ServiceInfo
Detailed information about a Windows service.

| Property | Type | Description |
|----------|------|-------------|
| ServiceName | string? | Short (internal) name |
| DisplayName | string? | Display name |
| State | ServiceState | Current state (Running, Stopped, etc.) |
| StartType | ServiceStartType | Start type (Automatic, Manual, Disabled) |
| ServiceType | uint | Service type flags |
| ControlsAccepted | uint | Accepted control codes |
| Win32ExitCode | uint | Last Win32 exit code |
| ServiceSpecificExitCode | uint | Service-specific exit code |
| CheckPoint | uint | Checkpoint during pending operations |
| WaitHint | uint | Wait hint in milliseconds |
| ProcessId | uint | Service process ID (0 if not running) |
| BinaryPathName | string? | Path to service binary |
| ServiceStartName | string? | Account the service runs under |
| IsRunning | bool | Whether the service is running |
| IsPending | bool | Whether the service is in a pending state |

### RestartManagerSession
Manages a Restart Manager session for shutting down and restarting applications that hold file locks.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| RegisterFiles(filePaths) | bool | Registers file paths with the session |
| RegisterProcesses(processIds) | bool | Registers process IDs with the session |
| GetProcesses() | List\<IRmProcessInfo\> | Gets processes affected by registered resources |
| ShutdownProcesses(timeoutMs?) | bool | Shuts down all affected processes |
| RestartProcesses() | bool | Restarts all shut-down processes |
| Dispose() | void | Ends the session and releases resources |

### IRmProcessInfo
Interface for Restart Manager process information.

| Property | Type | Description |
|----------|------|-------------|
| ProcessId | int | Process ID |
| AppName | string? | Application name |
| ServiceName | string? | Service short name |
| IsRestartable | bool | Whether the process can be restarted |

### ConsoleHelper
Windows console management via pure P/Invoke.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Allocate() | bool | Allocates a new console |
| Free() | bool | Detaches from the console |
| Attach(processId?) | bool | Attaches to a process console (-1 for parent) |
| GetWindowHandle() | IntPtr | Gets the console window handle |
| SetWindowVisible(visible) | bool | Shows or hides the console window |
| SetTitle(title) | bool | Sets the console window title |
| GetTitle() | string? | Gets the current console title |
| HasConsole | bool | Whether the process has an attached console |
| EnableQuickEdit(enable) | bool | Enables/disables QuickEdit mode |
| EnableCtrlC(enable) | bool | Enables/disables Ctrl+C handling |
| SetTextColor(foreground, background?) | bool | Sets the console text color |

### JobObjectHelper
Process group management via Windows Job Objects.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Handle | IntPtr | Native handle of the job object |
| Name | string? | Optional name |
| AssignProcess(processHandle) | bool | Assigns a process to the job |
| AssignProcessById(processId) | bool | Assigns a process by PID |
| SetKillOnClose(enabled) | bool | Sets KILL_ON_JOB_CLOSE flag |
| SetActiveProcessLimit(maxProcesses) | bool | Sets max concurrent processes |
| Terminate(exitCode?) | bool | Terminates all processes in the job |
| IsCurrentProcessInJob() | bool | Static: checks if current process is in a job |

## Enums

| Enum | Values | Description |
|------|--------|-------------|
| ServiceState | Stopped, StartPending, StopPending, Running, ContinuePending, PausePending, Paused, Unknown | Service current state |
| ServiceStartType | Boot, System, Automatic, Manual, Disabled, Unknown | Service start type |

## Usage

```csharp
using BPlusLib.Foundation.Services;

// Query a service
var svc = ServiceHelper.GetService("wuauserv");
if (svc != null)
    Console.WriteLine($"{svc.DisplayName}: {svc.State} (PID={svc.ProcessId})");

// Start/stop services
ServiceHelper.StartService("MyService");
ServiceHelper.StopService("MyService", waitMs: 10000);

// Restart Manager: close file locks
using var rm = new RestartManagerSession();
rm.RegisterFiles(@"C:\locked-file.dll");
var procs = rm.GetProcesses();
rm.ShutdownProcesses();
// ... replace file ...
rm.RestartProcesses();

// Console management
ConsoleHelper.Attach(-1); // Attach to parent console
ConsoleHelper.SetTitle("My App");
ConsoleHelper.SetTextColor(ConsoleHelper.ConsoleColor.Cyan);

// Job Object: group processes
using var job = new JobObjectHelper("MyGroup");
job.SetKillOnClose(true);
job.AssignProcessById(childPid);
```

## Dependencies
- `advapi32.dll` — OpenSCManagerW, OpenServiceW, StartServiceW, ControlService, QueryServiceStatus, QueryServiceStatusEx, QueryServiceConfigW, CloseServiceHandle
- `kernel32.dll` — CreateJobObjectW, AssignProcessToJobObject, SetInformationJobObject, QueryInformationJobObject, TerminateJobObject, AllocConsole, FreeConsole, AttachConsole, GetConsoleWindow, SetConsoleTitleW, GetConsoleTitleW, SetConsoleTextAttribute, GetConsoleMode, SetConsoleMode
- `user32.dll` — ShowWindowAsync
- `rstrtmgr.dll` — RmStartSession, RmRegisterResources, RmGetList, RmShutdown, RmRestart, RmEndSession
- `BPlusLib.Foundation.Native` — Shared P/Invoke declarations
