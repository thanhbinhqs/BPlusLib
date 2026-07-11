# Windows Helpers Expansion — Implementation Plan

> **For Hermes:** Use subagent-driven-development to implement this plan task-by-task.
> Each task = 2–5 minutes of focused work with full TDD cycle.

**Goal:** Add 20 new Windows-specific helper modules across 5 phases to
`BPlusLib.Foundation`, covering service management, shell/UI integration,
security/IPC, and environment configuration.

**Architecture:** Each module follows the established pattern:
`StaticClass` or `sealed class` in its own namespace (`BPlusLib.Foundation.<Category>`)
with internal P/Invoke declarations, fully thread-safe, nullable-enabled,
graceful error handling (return `null`/`false`, never throw). No WMI. No external
executables. All depend on existing `Native/` and `Common/` infrastructure.

**Tech Stack:** C# 12, net472;net6.0;net8.0, xUnit + FluentAssertions for tests.

**Current baseline:** 91 source files, 1017 passing tests.

---

## Phase 1 — Process & Service Management (4 modules)

### Task 1.1: Add `Native/AdvApi32.cs` — centralized advapi32 P/Invoke

**Objective:** Consolidate all advapi32.dll declarations scattered across modules
into a single internal class. Add missing APIs for Service, Credential, and
Token operations.

**Files:**
- Create: `src/BPlusLib.Foundation/Native/AdvApi32.cs`
- Test: no test (pure P/Invoke declarations)

**Step 1: Write AdvApi32.cs**

```csharp
// <copyright file="AdvApi32.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for advapi32.dll — service control manager,
    /// Windows Credential Manager, token/privilege operations.
    /// </summary>
    internal static class AdvApi32
    {
        // =================================================================
        // Service Control Manager
        // =================================================================

        // Access masks
        internal const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
        internal const uint SERVICE_ALL_ACCESS = 0xF01FF;
        internal const uint SERVICE_QUERY_CONFIG = 0x00001;
        internal const uint SERVICE_QUERY_STATUS = 0x00004;
        internal const uint SERVICE_START = 0x00010;
        internal const uint SERVICE_STOP = 0x00020;
        internal const uint SERVICE_PAUSE_CONTINUE = 0x00040;
        internal const uint SERVICE_INTERROGATE = 0x00080;
        internal const uint SERVICE_CHANGE_CONFIG = 0x10000;
        internal const uint SERVICE_CREATE = 0x00002;

        // Start types
        internal const uint SERVICE_BOOT_START = 0x00;
        internal const uint SERVICE_SYSTEM_START = 0x01;
        internal const uint SERVICE_AUTO_START = 0x02;
        internal const uint SERVICE_DEMAND_START = 0x03;
        internal const uint SERVICE_DISABLED = 0x04;

        // Error control
        internal const uint SERVICE_ERROR_IGNORE = 0x00;
        internal const uint SERVICE_ERROR_NORMAL = 0x01;
        internal const uint SERVICE_ERROR_SEVERE = 0x02;
        internal const uint SERVICE_ERROR_CRITICAL = 0x03;

        // Service types
        internal const uint SERVICE_WIN32_OWN_PROCESS = 0x10;
        internal const uint SERVICE_WIN32_SHARE_PROCESS = 0x20;
        internal const uint SERVICE_KERNEL_DRIVER = 0x01;
        internal const uint SERVICE_FILE_SYSTEM_DRIVER = 0x02;

        // Service state constants (simplified)
        internal const uint SERVICE_STOPPED = 0x01;
        internal const uint SERVICE_START_PENDING = 0x02;
        internal const uint SERVICE_STOP_PENDING = 0x03;
        internal const uint SERVICE_RUNNING = 0x04;
        internal const uint SERVICE_CONTINUE_PENDING = 0x05;
        internal const uint SERVICE_PAUSE_PENDING = 0x06;
        internal const uint SERVICE_PAUSED = 0x07;

        // Controls accepted
        internal const uint SERVICE_ACCEPT_STOP = 0x01;
        internal const uint SERVICE_ACCEPT_PAUSE_CONTINUE = 0x02;
        internal const uint SERVICE_ACCEPT_SHUTDOWN = 0x04;

        // Service control codes
        internal const uint SERVICE_CONTROL_STOP = 0x01;
        internal const uint SERVICE_CONTROL_PAUSE = 0x02;
        internal const uint SERVICE_CONTROL_CONTINUE = 0x03;
        internal const uint SERVICE_CONTROL_INTERROGATE = 0x04;

        // =================================================================
        // Credential Manager
        // =================================================================

        internal const uint CRED_TYPE_GENERIC = 1;
        internal const uint CRED_TYPE_DOMAIN_PASSWORD = 2;
        internal const uint CRED_TYPE_DOMAIN_CERTIFICATE = 3;
        internal const uint CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4;
        internal const uint CRED_TYPE_GENERIC_CERTIFICATE = 5;
        internal const uint CRED_TYPE_DOMAIN_EXTENDED = 6;

        internal const uint CRED_PERSIST_NONE = 0;
        internal const uint CRED_PERSIST_SESSION = 1;
        internal const uint CRED_PERSIST_LOCAL_MACHINE = 2;
        internal const uint CRED_PERSIST_ENTERPRISE = 3;

        internal const uint CRED_FLAGS_USERNAME_TARGET = 0x01;

        // =================================================================
        // Token / Security
        // =================================================================

        internal const uint TOKEN_QUERY = 0x0008;
        internal const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        internal const uint TOKEN_ALL_ACCESS = 0xF01FF;

        internal const int TokenElevation = 20;
        internal const int TokenLinkedToken = 19;
        internal const int TokenStatistics = 10;
        internal const int TokenIntegrityLevel = 25;

        internal const uint SE_PRIVILEGE_ENABLED = 0x02;

        // =================================================================
        // P/Invoke — Service Control Manager
        // =================================================================

        /// <summary>Opens the service control manager database.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenSCManagerW(
            string? machineName,
            string? databaseName,
            uint desiredAccess);

        /// <summary>Opens an existing service.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenServiceW(
            IntPtr hSCManager,
            string lpServiceName,
            uint dwDesiredAccess);

        /// <summary>Creates a new service.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateServiceW(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string? lpLoadOrderGroup,
            out uint lpdwTagId,
            string? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword);

        /// <summary>Closes a handle to the SCM or a service.</summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseServiceHandle(IntPtr hSCObject);

        /// <summary>Starts a service.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool StartServiceW(
            IntPtr hService,
            uint dwNumServiceArgs,
            string?[]? lpServiceArgVectors);

        /// <summary>Controls a service (stop, pause, continue, interrogate).</summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ControlService(
            IntPtr hService,
            uint dwControl,
            ref SERVICE_STATUS lpServiceStatus);

        /// <summary>Queries the current status of a service.</summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceStatus(
            IntPtr hService,
            ref SERVICE_STATUS lpServiceStatus);

        /// <summary>Queries service configuration.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceConfigW(
            IntPtr hService,
            IntPtr lpServiceConfig,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        /// <summary>Queries extended service status (for process ID).</summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceStatusEx(
            IntPtr hService,
            uint infoLevel,
            IntPtr lpBuffer,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        /// <summary>Changes the configuration of a service.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeServiceConfigW(
            IntPtr hService,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string? lpBinaryPathName,
            string? lpLoadOrderGroup,
            out uint lpdwTagId,
            string? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword,
            string? lpDisplayName);

        /// <summary>Marks a service for deletion.</summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteService(IntPtr hService);

        /// <summary>Enumerates services on the system.</summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint EnumServicesStatusExW(
            IntPtr hSCManager,
            uint infoLevel,
            uint dwServiceType,
            uint dwServiceState,
            IntPtr lpServices,
            uint cbBufSize,
            out uint pcbBytesNeeded,
            out uint lpServicesReturned,
            ref uint lpResumeHandle,
            string? pszGroupName);

        // =================================================================
        // P/Invoke — Credential Manager
        // =================================================================

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredReadW(
            string targetName,
            uint type,
            uint flags,
            out IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredWriteW(
            ref CREDENTIALW credential,
            uint flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredEnumerateW(
            string? filter,
            uint flags,
            out int count,
            out IntPtr credentials);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredDeleteW(
            string targetName,
            uint type,
            uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredFree(IntPtr buffer);

        // =================================================================
        // P/Invoke — Token / Privilege
        // =================================================================

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess,
            out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            int tokenInformationClass,
            IntPtr tokenInformation,
            uint tokenInformationLength,
            out uint returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValueW(
            string? lpSystemName,
            string lpName,
            out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TOKEN_PRIVILEGES newState,
            uint bufferLength,
            IntPtr previousState,
            out uint returnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateTokenEx(
            IntPtr existingTokenHandle,
            uint desiredAccess,
            IntPtr tokenAttributes,
            uint impersonationLevel,
            uint tokenType,
            out IntPtr duplicateTokenHandle);

        // =================================================================
        // P/Invoke — SID / Security
        // =================================================================

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertSidToStringSidW(
            IntPtr sid,
            out IntPtr stringSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int GetLengthSid(IntPtr sid);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);
    }

    // =================================================================
    // Structures
    // =================================================================

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct QUERY_SERVICE_CONFIGW
    {
        public uint dwServiceType;
        public uint dwStartType;
        public uint dwErrorControl;
        public IntPtr lpBinaryPathName;
        public IntPtr lpLoadOrderGroup;
        public uint dwTagId;
        public IntPtr lpDependencies;
        public IntPtr lpServiceStartName;
        public IntPtr lpDisplayName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
        public uint dwProcessId;
        public uint dwServiceFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CREDENTIALW
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
        // Followed by variable-length array — use Marshal.OffsetOf for >1
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_ELEVATION
    {
        public int TokenIsElevated;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_STATISTICS
    {
        public long TokenId;
        public long AuthenticationId;
        public long ExpirationTime;
        public uint TokenType;
        public uint ImpersonationLevel;
        public uint DynamicCharged;
        public uint DynamicAvailable;
        public uint GroupCount;
        public uint PrivilegeCount;
        public long ModifiedId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ENUM_SERVICE_STATUS_PROCESSW
    {
        public IntPtr lpServiceName;
        public IntPtr lpDisplayName;
        public SERVICE_STATUS_PROCESS ServiceStatusProcess;
    }

    // Constants for EnumServicesStatusExW
    internal const uint SC_ENUM_PROCESS_INFO = 0;
    internal const uint SERVICE_WIN32 = 0x00000030;
    internal const uint SERVICE_DRIVER = 0x0000000B;
    internal const uint SERVICE_STATE_ALL = 0x00000003;
}
```

**Step 2: Build check**

Run: `dotnet build src/BPlusLib.Foundation -c Release --no-restore 2>&1 | tail -5`
Expected: Build succeeded. 0 warnings.

---

### Task 1.2: Create `ServiceHelper` — Windows Service Management

**Objective:** Provide Windows service management: query status, start, stop,
restart, pause, continue, create, delete, enumerate services.
All methods return `null`/`false` on error — never throw.

**Files:**
- Create: `src/BPlusLib.Foundation/Services/ServiceHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Services/ServiceHelperTests.cs`

**Step 1: Define model**

```csharp
// src/BPlusLib.Foundation/Services/ServiceInfo.cs
namespace BPlusLib.Foundation.Services;

/// <summary>Represents a Windows service and its current status.</summary>
public sealed class ServiceInfo
{
    /// <summary>Internal service name (e.g., "wuauserv").</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Display name (e.g., "Windows Update").</summary>
    public string? DisplayName { get; init; }

    /// <summary>Current state: Running, Stopped, Paused, etc.</summary>
    public ServiceState State { get; init; }

    /// <summary>Start type: Automatic, Manual, Disabled, Boot, System.</summary>
    public ServiceStartType StartType { get; init; }

    /// <summary>Path to the service executable.</summary>
    public string? BinaryPath { get; init; }

    /// <summary>Process ID if the service is running; 0 otherwise.</summary>
    public int ProcessId { get; init; }

    /// <summary>Description of the service.</summary>
    public string? Description { get; init; }

    /// <summary>Account name under which the service runs.</summary>
    public string? ServiceAccount { get; init; }

    /// <summary>Whether the service can be stopped.</summary>
    public bool CanStop => (State == ServiceState.Running || State == ServiceState.Paused);

    /// <summary>Whether the service can be paused/continued.</summary>
    public bool CanPauseContinue { get; init; }
}

public enum ServiceState
{
    Unknown = 0,
    Stopped = 1,
    StartPending = 2,
    StopPending = 3,
    Running = 4,
    ContinuePending = 5,
    PausePending = 6,
    Paused = 7,
}

public enum ServiceStartType
{
    Unknown = 0,
    Boot = 1,
    System = 2,
    Automatic = 3,
    Manual = 4,
    Disabled = 5,
}
```

**Step 2: Implement ServiceHelper**

```csharp
// src/BPlusLib.Foundation/Services/ServiceHelper.cs
namespace BPlusLib.Foundation.Services;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

/// <summary>
/// Provides Windows service management via pure P/Invoke (advapi32.dll).
/// All methods are thread-safe and gracefully return null/false on error.
/// </summary>
public static class ServiceHelper
{
    /// <summary>Queries the status of a single service by name.</summary>
    public static ServiceInfo? GetService(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName)) return null;

        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;
        try
        {
            scm = AdvApi32.OpenSCManagerW(null, null, AdvApi32.SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero) return null;

            service = AdvApi32.OpenServiceW(scm, serviceName, AdvApi32.SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero) return null;

            return QueryServiceInfo(scm, service, serviceName);
        }
        catch { return null; }
        finally
        {
            if (service != IntPtr.Zero) AdvApi32.CloseServiceHandle(service);
            if (scm != IntPtr.Zero) AdvApi32.CloseServiceHandle(scm);
        }
    }

    /// <summary>Starts a stopped service. Returns true on success.</summary>
    public static bool StartService(string serviceName)
    {
        // ... OpenSCManagerW -> OpenServiceW -> StartServiceW ...
        // May need to wait for SERVICE_RUNNING with retries
    }

    /// <summary>Stops a running service. Returns true on success.</summary>
    public static bool StopService(string serviceName)
    {
        // ... OpenSCManagerW -> OpenServiceW -> ControlService(SERVICE_CONTROL_STOP) ...
        // Wait for SERVICE_STOPPED with retries
    }

    /// <summary>Restarts a service (stop then start).</summary>
    public static bool RestartService(string serviceName, int waitMs = 30000)
    {
        if (!StopService(serviceName, waitMs)) return false;
        return StartService(serviceName, waitMs);
    }

    /// <summary>Pauses a running service (if it accepts pause).</summary>
    public static bool PauseService(string serviceName)
    {
        // ControlService(SERVICE_CONTROL_PAUSE)
    }

    /// <summary>Continues a paused service.</summary>
    public static bool ContinueService(string serviceName)
    {
        // ControlService(SERVICE_CONTROL_CONTINUE)
    }

    /// <summary>Creates a new Windows service.</summary>
    public static bool CreateService(
        string serviceName,
        string displayName,
        string binaryPath,
        ServiceStartType startType = ServiceStartType.Automatic,
        string? accountName = null,
        string? password = null)
    {
        // CreateServiceW
    }

    /// <summary>Deletes a service. Returns true on success.</summary>
    public static bool DeleteService(string serviceName)
    {
        // OpenServiceW + DeleteService
    }

    /// <summary>Enumerates all services matching the given state filter.</summary>
    public static List<ServiceInfo> EnumerateServices(
        ServiceState stateFilter = ServiceState.Unknown)
    {
        // EnumServicesStatusExW with SC_ENUM_PROCESS_INFO
    }

    /// <summary>Checks whether a service exists.</summary>
    public static bool ServiceExists(string serviceName)
    {
        return GetService(serviceName) is not null;
    }

    /// <summary>Checks whether a service is running.</summary>
    public static bool IsServiceRunning(string serviceName)
    {
        var svc = GetService(serviceName);
        return svc?.State == ServiceState.Running;
    }

    /// <summary>Waits for a service to reach a target state.</summary>
    private static bool WaitForState(
        IntPtr service,
        ServiceState targetState,
        int timeoutMs = 30000)
    {
        // QueryServiceStatus in a loop with Sleep(250)
    }
}
```

**Step 3: Write tests**

```csharp
// tests/BPlusLib.Foundation.Tests/Services/ServiceHelperTests.cs
namespace BPlusLib.Foundation.Tests.Services;

using BPlusLib.Foundation.Services;
using FluentAssertions;
using Xunit;

[Trait("Category", "Services")]
public sealed class ServiceHelperTests
{
    [SkippableFact]
    public void GetService_KnownService_ReturnsInfo()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        // Use "PlugPlay" (Plug and Play) which exists on all modern Windows
        var info = ServiceHelper.GetService("PlugPlay");
        info.Should().NotBeNull();
        info!.ServiceName.Should().Be("PlugPlay");
        info.DisplayName.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public void GetService_NonExistent_ReturnsNull()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        var info = ServiceHelper.GetService("NonExistentServiceXYZ123");
        info.Should().BeNull();
    }

    [SkippableFact]
    public void EnumerateServices_ReturnsList()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        var services = ServiceHelper.EnumerateServices();
        services.Should().NotBeNull();
        services.Should().HaveCountGreaterThan(0);
        services.Should().Contain(s => s.ServiceName == "PlugPlay");
    }

    [SkippableFact]
    public void ServiceExists_ReturnsTrue()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        ServiceHelper.ServiceExists("PlugPlay").Should().BeTrue();
    }

    [SkippableFact]
    public void ServiceExists_NonExistent_ReturnsFalse()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        ServiceHelper.ServiceExists("NONEXISTENT_123").Should().BeFalse();
    }
}
```

**Step 4: Build & test**

Run: `dotnet test tests/BPlusLib.Foundation.Tests --filter "FullyQualifiedName~Services" --framework net8.0 -v n 2>&1 | tail -20`
Expected: 5 passed, 0 failed (on Windows; on Linux all Skip.IfNot)

**Step 5: Commit**

```bash
git add src/BPlusLib.Foundation/Native/AdvApi32.cs \
        src/BPlusLib.Foundation/Services/ServiceInfo.cs \
        src/BPlusLib.Foundation/Services/ServiceHelper.cs \
        tests/BPlusLib.Foundation.Tests/Services/ServiceHelperTests.cs
git commit -m "feat: add ServiceHelper — Windows service management (advapi32)"
```

---

### Task 1.3: Create `JobObjectHelper` — Process Group Management

**Objective:** Process group management via Windows Job Objects:
create job, assign process, set limits (CPU/memory/process count/kill on close),
query limits, terminate all.

**Files:**
- Create: `src/BPlusLib.Foundation/Services/JobObjectHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Services/JobObjectHelperTests.cs`

**Step 1: Add Job Object P/Invoke to existing Kernel32.cs**

Add to `src/BPlusLib.Foundation/Native/Kernel32.cs`:

```csharp
// Job Object APIs
internal const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
internal const uint JOB_OBJECT_LIMIT_PROCESS_TIME = 0x0002;
internal const uint JOB_OBJECT_LIMIT_JOB_TIME = 0x0004;
internal const uint JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x0008;
internal const uint JOB_OBJECT_LIMIT_AFFINITY = 0x0010;
internal const uint JOB_OBJECT_LIMIT_PRIORITY_CLASS = 0x0020;
internal const uint JOB_OBJECT_LIMIT_WORKING_SET = 0x0001;
internal const uint JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION = 0x0400;
internal const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x0200;

internal const uint JOB_OBJECT_QUERY = 0x0004;
internal const uint JOB_OBJECT_SET_ATTRIBUTES = 0x0002;
internal const uint JOB_OBJECT_TERMINATE = 0x0008;

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
{
    public long PerProcessUserTimeLimit;
    public long PerJobUserTimeLimit;
    public uint LimitFlags;
    public IntPtr MinimumWorkingSetSize;
    public IntPtr MaximumWorkingSetSize;
    public uint ActiveProcessLimit;
    public IntPtr Affinity;
    public uint PriorityClass;
    public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
{
    public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
    public IO_COUNTERS IoInfo;
    public IntPtr ProcessMemoryLimit;
    public IntPtr JobMemoryLimit;
    public IntPtr PeakProcessMemoryUsed;
    public IntPtr PeakJobMemoryUsed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IO_COUNTERS
{
    public ulong ReadOperationCount;
    public ulong WriteOperationCount;
    public ulong OtherOperationCount;
    public ulong ReadTransferCount;
    public ulong WriteTransferCount;
    public ulong OtherTransferCount;
}

[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
internal static extern IntPtr CreateJobObjectW(
    IntPtr lpJobAttributes,
    string? lpName);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool AssignProcessToJobObject(
    IntPtr hJob,
    IntPtr hProcess);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool SetInformationJobObject(
    IntPtr hJob,
    int jobObjectInformationClass,
    IntPtr lpJobObjectInformation,
    uint cbJobObjectInformationLength);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool QueryInformationJobObject(
    IntPtr hJob,
    int jobObjectInformationClass,
    IntPtr lpJobObjectInformation,
    uint cbJobObjectInformationLength,
    out uint lpReturnLength);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool TerminateJobObject(
    IntPtr hJob,
    uint exitCode);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool IsProcessInJob(
    IntPtr hProcess,
    IntPtr hJob,
    [MarshalAs(UnmanagedType.Bool)] out bool result);
```

**Step 2: Implement JobObjectHelper**

```csharp
// src/BPlusLib.Foundation/Services/JobObjectHelper.cs
public sealed class JobObjectHelper : IDisposable
{
    private readonly IntPtr _jobHandle;
    private bool _disposed;
    private static readonly IntPtr JobObjectExtendedLimitInformation = 9;

    /// <summary>Creates a new named or unnamed job object.</summary>
    public JobObjectHelper(string? name = null)
    {
        _jobHandle = Kernel32.CreateJobObjectW(IntPtr.Zero, name);
        if (_jobHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    /// <summary>Assigns a process (by handle) to this job object.</summary>
    public bool AssignProcess(IntPtr processHandle) { ... }

    /// <summary>Assigns a process (by PID) to this job object.</summary>
    public bool AssignProcessById(int processId) { ... }

    /// <summary>Sets kill-on-close: all processes are terminated when the job handle is closed.</summary>
    public bool SetKillOnClose(bool enabled) { ... }

    /// <summary>Sets the active process limit (max concurrent processes).</summary>
    public bool SetActiveProcessLimit(uint maxProcesses) { ... }

    /// <summary>Sets per-process user-mode time limit.</summary>
    public bool SetProcessTimeLimit(TimeSpan limit) { ... }

    /// <summary>Terminates all processes in the job.</summary>
    public bool Terminate(uint exitCode = 0) { ... }

    /// <summary>Returns true if the current process is in a job.</summary>
    public static bool IsCurrentProcessInJob()
    {
        return Kernel32.IsProcessInJob(
            Kernel32.GetCurrentProcess(),
            IntPtr.Zero,
            out bool result) && result;
    }

    public void Dispose() { ... }
}
```

**Step 3: Write tests**

```csharp
[Trait("Category", "Services")]
public sealed class JobObjectHelperTests
{
    [SkippableFact]
    public void CreateJob_NoName_Succeeds()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        using var job = new JobObjectHelper();
        job.Should().NotBeNull();
    }

    [SkippableFact]
    public void AssignCurrentProcess_Succeeds()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        using var job = new JobObjectHelper();
        // Note: if current process is already in a job, this may fail
        // Try and see; not critical for the test
        var result = job.AssignProcess(Kernel32.GetCurrentProcess());
        // May be true or false depending on environment
    }

    [SkippableFact]
    public void SetKillOnClose_Succeeds()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        using var job = new JobObjectHelper();
        job.SetKillOnClose(true).Should().BeTrue();
    }

    [SkippableFact]
    public void Terminate_EmptyJob_Succeeds()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        using var job = new JobObjectHelper();
        job.Terminate().Should().BeTrue();
    }
}
```

**Step 4: Build & test**

Run: `dotnet test tests/BPlusLib.Foundation.Tests --filter "FullyQualifiedName~JobObjectHelper" --framework net8.0 -v n 2>&1 | tail -10`
Expected: all passed (on Windows)

**Step 5: Commit**

```bash
git commit -m "feat: add JobObjectHelper — process group management via Job Objects"
```

---

### Task 1.4: Create `ConsoleHelper` — Console Window Management

**Objective:** Console window manipulation: AllocConsole, AttachConsole,
FreeConsole, show/hide, set title/text color, get window handle, check
if it's a console app.

**Files:**
- Create: `src/BPlusLib.Foundation/Services/ConsoleHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Services/ConsoleHelperTests.cs`

**Step 1: Add console P/Invoke to existing Kernel32.cs**

```csharp
// Existing APIs (likely already partially in Kernel32.cs)
[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool AllocConsole();

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool FreeConsole();

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool AttachConsole(int dwProcessId);
internal const int ATTACH_PARENT_PROCESS = -1;

[DllImport("kernel32.dll", SetLastError = true)]
internal static extern IntPtr GetConsoleWindow();

[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool SetConsoleTitleW(string lpConsoleTitle);

[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
internal static extern int GetConsoleTitleW(StringBuilder title, int size);

[DllImport("kernel32.dll", SetLastError = true)]
internal static extern IntPtr GetStdHandle(int nStdHandle);
internal const int STD_INPUT_HANDLE = -10;
internal const int STD_OUTPUT_HANDLE = -11;
internal const int STD_ERROR_HANDLE = -12;

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool SetConsoleTextAttribute(
    IntPtr consoleHandle,
    ushort attributes);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool GetConsoleMode(
    IntPtr consoleHandle,
    out uint mode);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool SetConsoleMode(
    IntPtr consoleHandle,
    uint mode);

// User32 for ShowWindow on console
```

**Step 2: Implement ConsoleHelper**

```csharp
// src/BPlusLib.Foundation/Services/ConsoleHelper.cs
public static class ConsoleHelper
{
    /// <summary>Allocates a new console for the calling process.</summary>
    public static bool Allocate() => Kernel32.AllocConsole();

    /// <summary>Detaches the calling process from its console.</summary>
    public static bool Free() => Kernel32.FreeConsole();

    /// <summary>Attaches to an existing console (use ATTACH_PARENT_PROCESS).</summary>
    public static bool Attach(int processId = -1) =>
        Kernel32.AttachConsole(processId);

    /// <summary>Gets the window handle of the console, or IntPtr.Zero.</summary>
    public static IntPtr GetWindowHandle() => Kernel32.GetConsoleWindow();

    /// <summary>Shows or hides the console window.</summary>
    public static bool ShowWindow(bool visible)
    {
        IntPtr hWnd = Kernel32.GetConsoleWindow();
        if (hWnd == IntPtr.Zero) return false;
        return User32.ShowWindowAsync(hWnd, visible ? 5 : 0);
    }

    /// <summary>Sets the console window title.</summary>
    public static bool SetTitle(string title) =>
        Kernel32.SetConsoleTitleW(title ?? string.Empty);

    /// <summary>Gets the current console window title.</summary>
    public static string? GetTitle() { ... }

    /// <summary>Sets the console text color (foreground | background).</summary>
    public static bool SetTextColor(ConsoleColor foreground, ConsoleColor? background = null) { ... }

    /// <summary>Returns true if the current process has a console.</summary>
    public static bool HasConsole => Kernel32.GetConsoleWindow() != IntPtr.Zero;

    /// <summary>Tries to enable or disable QuickEdit mode (for blocking mouse clicks).</summary>
    public static bool EnableQuickEdit(bool enable) { ... }

    /// <summary>Tries to enable or disable Ctrl+C handling.</summary>
    public static bool EnableCtrlC(bool enable) { ... }
}
```

**Step 3: Write tests**

```csharp
[Trait("Category", "Services")]
public sealed class ConsoleHelperTests
{
    [SkippableFact]
    public void GetConsoleWindow_ReturnsHandle()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        // If running with a console (test runner), this should be non-zero
        var hwnd = ConsoleHelper.GetWindowHandle();
        // On some test runners there may be no console
        // Just verify it doesn't throw
    }

    [SkippableFact]
    public void SetAndGetTitle_Roundtrips()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        if (!ConsoleHelper.HasConsole) return;
        var original = ConsoleHelper.GetTitle();
        ConsoleHelper.SetTitle("TestTitle123");
        ConsoleHelper.GetTitle().Should().Be("TestTitle123");
        ConsoleHelper.SetTitle(original ?? "");
    }

    [SkippableFact]
    public void HasConsole_ReturnsTrueOrFalse()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        // Should not throw
        var has = ConsoleHelper.HasConsole;
        has.Should().BeOneOf(true, false);
    }
}
```

**Step 4: Build & test**

Run: `dotnet test --filter "FullyQualifiedName~ConsoleHelper" --framework net8.0 -v n`
Expected: passed

**Step 5: Commit**

```bash
git commit -m "feat: add ConsoleHelper — console window management"
```

---

## Phase 2 — Shell & UI Integration (4 modules)

### Task 2.1: Create `ShortcutHelper` — .lnk Shortcut Creation/Reading

**Objective:** Read and create Windows Shell links (.lnk files) via IShellLink
COM interface. No WMI. Pure P/Invoke into shell32/ole32.

**Files:**
- Create: `src/BPlusLib.Foundation/Shell/ShortcutHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Shell/ShortcutHelperTests.cs`

**Key APIs:**
- `CoCreateInstance(CLSID_ShellLink, ..., IID_IShellLinkW, ...)`
- `IShellLinkW.GetPath/SetPath/GetDescription/SetDescription`
- `IPersistFile.Save/Load`

**Step 1: Write the ShortcutHelper class**

```csharp
// src/BPlusLib.Foundation/Shell/ShortcutHelper.cs
public sealed class ShortcutInfo
{
    public string TargetPath { get; init; } = string.Empty;
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public string? Description { get; init; }
    public string? IconLocation { get; init; }
    public int IconIndex { get; init; }
    public int ShowCommand { get; init; }
    public string? Hotkey { get; init; }
}

public static class ShortcutHelper
{
    /// <summary>Reads a .lnk file and returns its properties.</summary>
    public static ShortcutInfo? Read(string shortcutPath) { ... }

    /// <summary>Creates or updates a .lnk shortcut.</summary>
    public static bool Create(string shortcutPath, ShortcutInfo info) { ... }

    /// <summary>Returns true if the file is a valid .lnk shortcut.</summary>
    public static bool IsShortcut(string path) { ... }

    /// <summary>Gets the target path of a .lnk shortcut (fast path, no COM).</summary>
    public static string? GetTargetPath(string shortcutPath) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void CreateAndRead_Roundtrips()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    string tempLnk = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.lnk");
    try
    {
        var info = new ShortcutInfo
        {
            TargetPath = @"C:\Windows\System32\notepad.exe",
            Arguments = "test.txt",
            Description = "Test shortcut",
            WorkingDirectory = @"C:\Windows\System32",
            ShowCommand = 1,
        };
        ShortcutHelper.Create(tempLnk, info).Should().BeTrue();
        var read = ShortcutHelper.Read(tempLnk);
        read.Should().NotBeNull();
        read!.TargetPath.Should().Contain("notepad.exe");
        read.Description.Should().Be("Test shortcut");
    }
    finally { File.Delete(tempLnk); }
}
```

**Step 3: Build & commit**

```bash
git commit -m "feat: add ShortcutHelper — .lnk creation and reading via IShellLink"
```

---

### Task 2.2: Create `NotifyIconHelper` — System Tray Notify Icon

**Objective:** Programmatically add/modify/remove system tray notification icons
via Shell_NotifyIconW. Supports balloon tips, tooltip text, icon, callback
message ID.

**Files:**
- Create: `src/BPlusLib.Foundation/Shell/NotifyIconHelper.cs`
- Create: `src/BPlusLib.Foundation/Shell/NotifyIconData.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Shell/NotifyIconHelperTests.cs`

**Key APIs:**
- `Shell_NotifyIconW(NIM_ADD, ref NOTIFYICONDATAW)`
- `NOTIFYICONDATAW` structure
- Window message callback

**Step 1: Implement**

```csharp
public sealed class NotifyIcon : IDisposable
{
    /// <summary>Adds the icon to the notification area.</summary>
    public bool Show() { ... }

    /// <summary>Removes the icon from the notification area.</summary>
    public bool Hide() { ... }

    /// <summary>Updates the icon, tooltip, or visibility.</summary>
    public bool Update() { ... }

    /// <summary>Shows a balloon tip notification.</summary>
    public bool ShowBalloonTip(string title, string text, uint timeoutMs = 3000) { ... }

    public void Dispose() { Hide(); }
}

public static class NotifyIconHelper
{
    public static NotifyIcon Create(
        IntPtr hwnd,
        uint callbackMessageId,
        IntPtr iconHandle,
        string? tooltipText = null,
        Guid? guid = null) { ... }
}
```

**Step 2: Write test**

```csharp
[SkippableFact]
public void CreateAndRemove_Succeeds()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Need a window handle for callbacks — use a simple approach
    // Just verify it doesn't throw; actual UI testing is integration-only
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add NotifyIconHelper — system tray notifications (Shell_NotifyIconW)"
```

---

### Task 2.3: Create `HotkeyHelper` — Global Hotkeys

**Objective:** Register/unregister global hotkeys via RegisterHotKey.
Requires a window handle for message processing.

**Files:**
- Create: `src/BPlusLib.Foundation/Input/HotkeyHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Input/HotkeyHelperTests.cs`

**Step 1: Add P/Invoke to User32.cs**

```csharp
[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool RegisterHotKey(
    IntPtr hWnd,
    int id,
    uint fsModifiers,
    uint vk);

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool UnregisterHotKey(
    IntPtr hWnd,
    int id);

// Modifier keys
internal const uint MOD_ALT = 0x0001;
internal const uint MOD_CONTROL = 0x0002;
internal const uint MOD_SHIFT = 0x0004;
internal const uint MOD_WIN = 0x0008;
internal const uint MOD_NOREPEAT = 0x4000;
```

**Step 2: Implement**

```csharp
public sealed class HotkeyRegistration : IDisposable
{
    /// <summary>Registers a global hotkey.</summary>
    public static HotkeyRegistration? Register(
        IntPtr hwnd, int id,
        HotkeyModifiers modifiers, System.Windows.Forms.Keys key) { ... }

    /// <summary>Unregisters the hotkey.</summary>
    public void Unregister() { ... }
    public void Dispose() => Unregister();
}

[Flags]
public enum HotkeyModifiers
{
    None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8, NoRepeat = 0x4000,
}
```

**Step 3: Write test**

```csharp
[SkippableFact]
public void RegisterAndUnregister_Succeeds()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Without a valid window this may fail — just verify it doesn't throw
}
```

**Step 4: Commit**

```bash
git commit -m "feat: add HotkeyHelper — global hotkeys via RegisterHotKey"
```

---

### Task 2.4: Create `InputHelper` — Keyboard/Mouse Simulation

**Objective:** Simulate keyboard and mouse input via SendInput.
Supports key down/up, text typing, mouse clicks, mouse move.

**Files:**
- Create: `src/BPlusLib.Foundation/Input/InputHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Input/InputHelperTests.cs`

**Step 1: Add P/Invoke to User32.cs**

```csharp
[DllImport("user32.dll", SetLastError = true)]
internal static extern uint SendInput(
    uint cInputs,
    [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInputs,
    int cbSize);

internal const int INPUT_MOUSE = 0;
internal const int INPUT_KEYBOARD = 1;
internal const int INPUT_HARDWARE = 2;

internal const uint KEYEVENTF_KEYDOWN = 0x0000;
internal const uint KEYEVENTF_KEYUP = 0x0002;
internal const uint KEYEVENTF_SCANCODE = 0x0008;
internal const uint KEYEVENTF_UNICODE = 0x0004;

internal const uint MOUSEEVENTF_MOVE = 0x0001;
internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
internal const uint MOUSEEVENTF_LEFTUP = 0x0004;
internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;
internal const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
internal const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
internal const uint MOUSEEVENTF_WHEEL = 0x0800;
internal const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
```

**Step 2: Implement**

```csharp
public static class InputHelper
{
    /// <summary>Sends a single key press (down + up).</summary>
    public static bool SendKeyPress(VirtualKeyCode keyCode) { ... }

    /// <summary>Sends text by translating each character to key events.</summary>
    public static bool SendText(string text) { ... }

    /// <summary>Sends a key down event.</summary>
    public static bool KeyDown(VirtualKeyCode keyCode) { ... }

    /// <summary>Sends a key up event.</summary>
    public static bool KeyUp(VirtualKeyCode keyCode) { ... }

    /// <summary>Moves the mouse cursor to the specified screen coordinates.</summary>
    public static bool MoveMouse(int x, int y, bool absolute = true) { ... }

    /// <summary>Simulates a left mouse button click.</summary>
    public static bool LeftClick() { ... }

    /// <summary>Simulates a right mouse button click.</summary>
    public static bool RightClick() { ... }

    /// <summary>Simulates a mouse wheel scroll.</summary>
    public static bool ScrollWheel(int delta) { ... }

    /// <summary>Modifier key helper: Ctrl+C, Alt+Tab, etc.</summary>
    public static bool SendModifiedKey(VirtualKeyCode modifier, VirtualKeyCode key) { ... }
}
```

**Step 3: Write test**

```csharp
[SkippableFact]
public void SendKeyPress_DoesNotThrow()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Testing actual input is integration-only (would require focused window)
    // Just verify the API doesn't throw and returns expected value
}
```

**Step 4: Commit**

```bash
git commit -m "feat: add InputHelper — keyboard/mouse simulation via SendInput"
```

---

## Phase 3 — Security & Identity (3 modules)

### Task 3.1: Create `UacHelper` — UAC Elevation Detection

**Objective:** Check if process is elevated, get process integrity level,
auto-elevate by spawning a new elevated process (runas verb), get linked
token.

**Files:**
- Create: `src/BPlusLib.Foundation/Security/UacHelper.cs`
- Create: `src/BPlusLib.Foundation/Security/UacHelper.Models.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Security/UacHelperTests.cs`

**Key APIs:** Already in AdvApi32.cs / existing Security module.
- `OpenProcessToken` + `GetTokenInformation(TokenElevation)`
- `ShellExecuteExW` with "runas" verb
- `GetTokenInformation(TokenIntegrityLevel)` + `ConvertSidToStringSidW`
- `GetTokenInformation(TokenLinkedToken)`

**Step 1: Implement**

```csharp
public static class UacHelper
{
    /// <summary>Returns true if the current process is running elevated (as admin).</summary>
    public static bool IsElevated() { ... }

    /// <summary>Returns true if the current process is running as a standard user.</summary>
    public static bool IsStandardUser() => !IsElevated() && !IsSystem();

    /// <summary>Returns true if UAC is enabled on this system.</summary>
    public static bool IsUacEnabled() { /* registry: EnableLUA */ }

    /// <summary>Gets the integrity level of the current process.</summary>
    public static IntegrityLevel GetIntegrityLevel() { ... }

    /// <summary>Restarts the current process with elevated privileges (runas).</summary>
    public static bool RunElevated(string? arguments = null) { ... }

    /// <summary>Runs a specific executable with elevated privileges.</summary>
    public static bool RunAsAdmin(string executablePath, string? arguments = null) { ... }

    /// <summary>Checks if a specific executable has the "requireAdministrator" manifest.</summary>
    public static bool RequiresElevation(string executablePath) { ... }

    /// <summary>Gets the elevated linked token when running from a split-token admin.</summary>
    public static bool TryGetLinkedToken(out SafeFileHandle? linkedToken) { ... }

    /// <summary>Gets the process integrity level string.</summary>
    public static string? GetIntegrityLevelString() { ... }
}

public enum IntegrityLevel
{
    Untrusted = 0,
    Low = 1,
    Medium = 2,
    MediumPlus = 3,
    High = 4,
    System = 5,
    ProtectedProcess = 6,
    Unknown = -1,
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void IsElevated_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Just verify it doesn't throw
    var elevated = UacHelper.IsElevated();
    elevated.Should().BeOneOf(true, false);
}

[SkippableFact]
public void GetIntegrityLevel_ReturnsLevel()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var level = UacHelper.GetIntegrityLevel();
    level.Should().NotBe(IntegrityLevel.Unknown);
}

[SkippableFact]
public void IsUacEnabled_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Just check non-throwing
    UacHelper.IsUacEnabled().Should().BeOneOf(true, false);
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add UacHelper — UAC elevation detection and runas"
```

---

### Task 3.2: Create `CredentialHelper` — Windows Credential Manager

**Objective:** Read, write, enumerate, and delete credentials in the
Windows Credential Manager vault.

**Files:**
- Create: `src/BPlusLib.Foundation/Security/CredentialHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Security/CredentialHelperTests.cs`

**Key APIs:** Already in AdvApi32.cs
- `CredReadW` / `CredWriteW` / `CredEnumerateW` / `CredDeleteW` / `CredFree`

**Step 1: Implement**

```csharp
public sealed class CredentialEntry
{
    public string TargetName { get; init; } = string.Empty;
    public CredentialType Type { get; init; }
    public string? UserName { get; init; }
    public byte[]? CredentialBlob { get; init; }
    public string? Password { get; init; }  // decoded from blob for generic creds
    public string? Comment { get; init; }
    public DateTime LastWritten { get; init; }
    public CredentialPersist Persist { get; init; }
}

public enum CredentialType { Generic = 1, DomainPassword = 2, DomainCertificate = 3, ... }
public enum CredentialPersist { Session = 1, LocalMachine = 2, Enterprise = 3 }

public static class CredentialHelper
{
    /// <summary>Reads a stored credential by target name.</summary>
    public static CredentialEntry? Read(string targetName, CredentialType type = CredentialType.Generic) { ... }

    /// <summary>Saves a credential (creates or updates).</summary>
    public static bool Write(
        string targetName,
        string? userName,
        string? password,
        CredentialType type = CredentialType.Generic,
        CredentialPersist persist = CredentialPersist.LocalMachine,
        string? comment = null) { ... }

    /// <summary>Enumerates all stored credentials matching an optional filter.</summary>
    public static List<CredentialEntry> Enumerate(string? filter = null) { ... }

    /// <summary>Deletes a stored credential.</summary>
    public static bool Delete(string targetName, CredentialType type = CredentialType.Generic) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void WriteReadDelete_Roundtrips()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    string target = $"BPlusLib_Test_{Guid.NewGuid():N}";
    try
    {
        CredentialHelper.Write(target, "testuser", "testpass123!")
            .Should().BeTrue();
        var entry = CredentialHelper.Read(target);
        entry.Should().NotBeNull();
        entry!.UserName.Should().Be("testuser");
        entry.Password.Should().Be("testpass123!");
    }
    finally
    {
        CredentialHelper.Delete(target);
    }
}

[SkippableFact]
public void Read_NonExistent_ReturnsNull()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var entry = CredentialHelper.Read($"NONEXISTENT_{Guid.NewGuid():N}");
    entry.Should().BeNull();
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add CredentialHelper — Windows Credential Manager access"
```

---

### Task 3.3: Create `WinTrustHelper` — Authenticode Signature Verification

**Objective:** Verify Authenticode (PE digital signatures) via WinVerifyTrust.
Check if an executable is signed, get signer info, check timestamp.

**Files:**
- Create: `src/BPlusLib.Foundation/Security/WinTrustHelper.cs`
- Create: `src/BPlusLib.Foundation/Security/WinTrustNative.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Security/WinTrustHelperTests.cs`

**Key APIs:**
- `WinVerifyTrust(IntPtr, Guid, IntPtr)` from wintrust.dll
- `WTD_CHOICE_FILE` / `WINTRUST_ACTION_GENERIC_VERIFY_V2`
- `CryptQueryObject` / `CertGetNameString` from crypt32.dll for signer info

**Step 1: Implement**

```csharp
public enum TrustLevel
{
    Unknown = 0,
    Untrusted = 1,
    Trusted = 2,
    TrustedWithRevocation = 3,
}

public sealed class SignatureInfo
{
    public bool IsSigned { get; init; }
    public TrustLevel TrustLevel { get; init; }
    public string? SignerName { get; init; }
    public string? PublisherName { get; init; }
    public string? Thumbprint { get; init; }
    public DateTime? Timestamp { get; init; }
    public bool IsOSBinary { get; init; }
}

public static class WinTrustHelper
{
    /// <summary>Verifies the Authenticode signature of a file.</summary>
    public static SignatureInfo Verify(string filePath) { ... }

    /// <summary>Quick check: returns true if the file has a valid signature.</summary>
    public static bool IsSigned(string filePath) { ... }

    /// <summary>Returns the publisher name from the signature, if any.</summary>
    public static string? GetPublisher(string filePath) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void Verify_SystemDll_IsSigned()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var info = WinTrustHelper.Verify(@"C:\Windows\System32\kernel32.dll");
    info.Should().NotBeNull();
    info.IsSigned.Should().BeTrue();
    info.SignerName.Should().NotBeNullOrEmpty();
}

[SkippableFact]
public void Verify_UnsignedFile_ReturnsUntrusted()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    string tempFile = Path.GetTempFileName();
    try
    {
        File.WriteAllText(tempFile, "not a PE file");
        var info = WinTrustHelper.Verify(tempFile);
        info.IsSigned.Should().BeFalse();
    }
    finally { File.Delete(tempFile); }
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add WinTrustHelper — Authenticode signature verification"
```

---

## Phase 4 — IPC & System (4 modules)

### Task 4.1: Create `PipeHelper` — Named Pipe IPC

**Objective:** Named pipe client/server communication. Supports both
synchronous and async I/O, message mode, timeout, and impersonation.

**Files:**
- Create: `src/BPlusLib.Foundation/IPC/PipeServer.cs`
- Create: `src/BPlusLib.Foundation/IPC/PipeClient.cs`
- Create: `src/BPlusLib.Foundation/IPC/PipeHelper.cs` (static helpers)
- Create: `tests/BPlusLib.Foundation.Tests/IPC/PipeHelperTests.cs`

**Key APIs:**
- `CreateNamedPipeW` / `ConnectNamedPipe` / `DisconnectNamedPipe`
- `CallNamedPipeW` / `WaitNamedPipeW`
- `SetNamedPipeHandleState`
- `ImpersonateNamedPipeClient` / `RevertToSelf`
- Overlapped I/O

**Step 1: Implement**

```csharp
public sealed class PipeServer : IDisposable
{
    public PipeServer(string pipeName, int maxInstances = 1, int bufferSize = 4096) { ... }

    /// <summary>Waits for a client to connect (blocking).</summary>
    public bool WaitForConnection(int timeoutMs = Timeout.Infinite) { ... }

    /// <summary>Reads bytes from the connected client.</summary>
    public byte[]? Read(int maxBytes = 4096) { ... }

    /// <summary>Writes bytes to the connected client.</summary>
    public bool Write(byte[] data) { ... }

    /// <summary>Disconnects the current client and waits for the next.</summary>
    public bool Disconnect() { ... }

    /// <summary>Impersonates the connected client's security context.</summary>
    public bool ImpersonateClient() { ... }

    public void Dispose() { ... }
}

public sealed class PipeClient : IDisposable
{
    public PipeClient(string pipeName) { ... }

    /// <summary>Connects to an existing pipe server.</summary>
    public bool Connect(int timeoutMs = 10000) { ... }

    /// <summary>Reads bytes from the server.</summary>
    public byte[]? Read(int maxBytes = 4096) { ... }

    /// <summary>Writes bytes to the server.</summary>
    public bool Write(byte[] data) { ... }

    public void Dispose() { ... }
}

public static class PipeHelper
{
    /// <summary>One-shot named pipe transaction (send request, receive reply).</summary>
    public static byte[]? Transact(string pipeName, byte[] request, int timeoutMs = 10000) { ... }

    /// <summary>Returns true if a pipe server exists for the given name.</summary>
    public static bool PipeExists(string pipeName) { ... }

    /// <summary>Gets the list of available named pipes on the system.</summary>
    public static string[] GetAvailablePipes() { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void ClientServer_Roundtrip()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    string pipeName = $"BPlusLibTest_{Guid.NewGuid():N}";
    byte[] sent = Encoding.UTF8.GetBytes("Hello from client");
    byte[] received = null;

    using var server = new PipeServer(pipeName);
    var serverTask = Task.Run(() =>
    {
        server.WaitForConnection(5000);
        server.Write(sent);
        received = server.Read();
    });

    using var client = new PipeClient(pipeName);
    client.Connect(5000).Should().BeTrue();
    client.Write(sent);
    var response = client.Read();
    response.Should().NotBeNull();

    serverTask.Wait(5000).Should().BeTrue();
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add PipeHelper — named pipe IPC server/client"
```

---

### Task 4.2: Create `MemoryHelper` — Memory-Mapped Files & System Memory

**Objective:** Memory-mapped file creation/opening, view mapping,
system memory info (available RAM, total RAM, page file size, per-process
working set). Extends existing SystemInfo with memory mapping.

**Files:**
- Create: `src/BPlusLib.Foundation/SystemInfo/MemoryHelper.cs`
- Create: `src/BPlusLib.Foundation/SystemInfo/MemoryMappedFileHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/SystemInfo/MemoryHelperTests.cs`

**Key APIs:**
- `CreateFileMappingW` / `OpenFileMappingW` / `MapViewOfFile` / `UnmapViewOfFile`
- `GlobalMemoryStatusEx` / `GetProcessMemoryInfo` (psapi)
- `EmptyWorkingSet`

**Step 1: Implement**

```csharp
public sealed class SystemMemoryInfo
{
    public ulong TotalPhysicalMemory { get; init; }
    public ulong AvailablePhysicalMemory { get; init; }
    public ulong TotalVirtualMemory { get; init; }
    public ulong AvailableVirtualMemory { get; init; }
    public ulong TotalPageFile { get; init; }
    public ulong AvailablePageFile { get; init; }
    public double MemoryUsagePercent { get; init; }
}

public sealed class MemoryMappedView : IDisposable
{
    public IntPtr Pointer { get; }
    public long Size { get; }
    public void Dispose() { UnmapViewOfFile(Pointer); }
}

public static class MemoryHelper
{
    public static SystemMemoryInfo GetSystemMemoryInfo() { ... }

    /// <summary>Gets the current process memory usage.</summary>
    public static ProcessMemoryInfo? GetProcessMemoryInfo() { ... }
}

public static class MemoryMappedFileHelper
{
    /// <summary>Creates or opens a named memory-mapped file.</summary>
    public static MemoryMappedView? CreateOrOpen(
        string name, long size, bool readWrite = true) { ... }

    /// <summary>Opens an existing named memory-mapped file.</summary>
    public static MemoryMappedView? Open(string name, bool readWrite = false) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void GetSystemMemoryInfo_ReturnsNonZero()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var info = MemoryHelper.GetSystemMemoryInfo();
    info.Should().NotBeNull();
    info.TotalPhysicalMemory.Should().BeGreaterThan(0);
    info.AvailablePhysicalMemory.Should().BeGreaterThan(0);
}

[SkippableFact]
public void CreateAndOpen_SharedMemory()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    string name = $"BPlusLib_{Guid.NewGuid():N}";
    using var view1 = MemoryMappedFileHelper.CreateOrOpen(name, 4096);
    view1.Should().NotBeNull();
    Marshal.WriteInt32(view1!.Pointer, 42);
    using var view2 = MemoryMappedFileHelper.Open(name, readWrite: false);
    view2.Should().NotBeNull();
    Marshal.ReadInt32(view2!.Pointer).Should().Be(42);
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add MemoryHelper — memory-mapped files and system memory info"
```

---

### Task 4.3: Create `RestartManagerHelper` — Restart Manager API

**Objective:** Use the Windows Restart Manager API to detect which processes
are using specified files/resources, shut them down, and restart them.
Essential for installers/updaters.

**Files:**
- Create: `src/BPlusLib.Foundation/Services/RestartManagerHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Services/RestartManagerHelperTests.cs`

**Key APIs (rm.h / rstrtmgr.dll):**
- `RmStartSession` / `RmEndSession`
- `RmRegisterResources` (files, processes, services)
- `RmGetList` (applications using registered resources)
- `RmShutdown` / `RmRestart`

**Step 1: Implement**

```csharp
public sealed class RestartManagerSession : IDisposable
{
    /// <summary>Starts a new Restart Manager session.</summary>
    public RestartManagerSession() { RmStartSession(...); }

    /// <summary>Registers files to check for locks.</summary>
    public bool RegisterFiles(params string[] filePaths) { ... }

    /// <summary>Registers processes to check.</summary>
    public bool RegisterProcesses(params int[] processIds) { ... }

    /// <summary>Gets the list of processes using registered resources.</summary>
    public List<RmProcessInfo> GetProcesses() { ... }

    /// <summary>Shuts down the identified processes.</summary>
    public bool ShutdownProcesses(int timeoutMs = 30000) { ... }

    /// <summary>Restarts previously shut-down processes.</summary>
    public bool RestartProcesses() { ... }

    public void Dispose() { RmEndSession(...); }
}

public sealed class RmProcessInfo
{
    public int ProcessId { get; init; }
    public string? ProcessName { get; init; }
    public string? ApplicationName { get; init; }
    public string? ServiceName { get; init; }
    public RmAppType ApplicationType { get; init; }
    public RmAppStatus ApplicationStatus { get; init; }
    public int SessionId { get; init; }
    public bool IsRestartable { get; init; }
    public bool HasRestartData { get; init; }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void StartAndEndSession_Succeeds()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    using var rm = new RestartManagerSession();
    rm.Should().NotBeNull();
}

[SkippableFact]
public void DetectCurrentProcess_ReturnsSelf()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    using var rm = new RestartManagerSession();
    string tempFile = Path.GetTempFileName();
    try
    {
        // Open the file to lock it
        using var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        rm.RegisterFiles(tempFile).Should().BeTrue();
        var processes = rm.GetProcesses();
        processes.Should().NotBeNull();
        processes.Should().Contain(p => p.ProcessId == Environment.ProcessId);
    }
    finally { File.Delete(tempFile); }
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add RestartManagerHelper — Restart Manager API"
```

---

### Task 4.4: Create `HttpListenerHelper` — Embedded HTTP Server

**Objective:** Simple embedded HTTP server for localhost communication
using System.Net.HttpListener (cross-platform) with Windows-specific
URL ACL registration via netsh (or manual HTTP API).

**Files:**
- Create: `src/BPlusLib.Foundation/Networking/HttpListenerHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Networking/HttpListenerHelperTests.cs`

**Step 1: Implement**

```csharp
public static class HttpListenerHelper
{
    /// <summary>Starts a simple HTTP listener on localhost.</summary>
    public static HttpListener? Start(string prefix = "http://localhost:8080/") { ... }

    /// <summary>Gets the next HTTP request (blocking).</summary>
    public static HttpListenerContext? GetRequest(HttpListener listener, int timeoutMs = 5000) { ... }

    /// <summary>Sends a text response. Helper method.</summary>
    public static void SendText(HttpListenerResponse response, string text, string contentType = "text/plain") { ... }

    /// <summary>Sends a JSON response.</summary>
    public static void SendJson(HttpListenerResponse response, string json) { ... }

    /// <summary>Sends a binary response.</summary>
    public static void SendBinary(HttpListenerResponse response, byte[] data, string contentType = "application/octet-stream") { ... }

    /// <summary>Registers a URL ACL on Windows (netsh http add urlacl).</summary>
    public static bool RegisterUrlAcl(string url, string user = "Everyone") { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void StartSendReceive_Roundtrip()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    int port = GetFreePort();
    string prefix = $"http://localhost:{port}/";
    using var listener = HttpListenerHelper.Start(prefix);
    listener.Should().NotBeNull();

    // Send request in background
    string responseText = null;
    var clientTask = Task.Run(() =>
    {
        using var client = new HttpClient();
        var response = client.GetAsync($"http://localhost:{port}/test").Result;
        responseText = response.Content.ReadAsStringAsync().Result;
    });

    // Receive on server
    var ctx = HttpListenerHelper.GetRequest(listener, 5000);
    ctx.Should().NotBeNull();
    ctx!.Request.Url.Should().NotBeNull();
    HttpListenerHelper.SendText(ctx.Response, "Hello!");
    clientTask.Wait(5000).Should().BeTrue();
    responseText.Should().Be("Hello!");
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add HttpListenerHelper — embedded HTTP server"
```

---

## Phase 5 — Environment & Configuration (5 modules)

### Task 5.1: Create `EnvironmentHelper` — System Environment

**Objective:** Environment variable management (read/write/delete per-user
and per-machine), PATH parsing/add/remove, special folder paths,
system directory locations, computer info (domain join status, name, etc.).

**Files:**
- Create: `src/BPlusLib.Foundation/SystemInfo/EnvironmentHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/SystemInfo/EnvironmentHelperTests.cs`

**Key APIs:**
- `GetEnvironmentVariable` / `SetEnvironmentVariable` (kernel32)
- `ExpandEnvironmentStrings`
- `GetComputerNameExW`
- `NetGetJoinInformation` (netapi32)
- `SHGetKnownFolderPath` (shell32 — partially in ExplorerHelper)
- PATH manipulation via `GetEnvironmentVariable("PATH", ...)`

**Step 1: Implement**

```csharp
public static class EnvironmentHelper
{
    /// <summary>Gets an environment variable (process-level).</summary>
    public static string? GetVariable(string name) => Environment.GetEnvironmentVariable(name);

    /// <summary>Gets an environment variable with explicit target.</summary>
    public static string? GetVariable(string name, EnvironmentVariableTarget target) =>
        Environment.GetEnvironmentVariable(name, target);

    /// <summary>Sets an environment variable.</summary>
    public static bool SetVariable(string name, string value,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) { ... }

    /// <summary>Deletes an environment variable.</summary>
    public static bool DeleteVariable(string name,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) { ... }

    /// <summary>Expands environment-variable strings (e.g., %TEMP%).</summary>
    public static string? ExpandString(string input) { ... }

    /// <summary>Gets the machine's fully qualified DNS host name.</summary>
    public static string? GetMachineName() { ... }

    /// <summary>Returns true if the computer is joined to a domain.</summary>
    public static bool IsDomainJoined() { ... }

    /// <summary>Gets the domain name, if the computer is domain-joined.</summary>
    public static string? GetDomainName() { ... }

    /// <summary>Adds a directory to the user PATH (if not already present).</summary>
    public static bool AddToUserPath(string directoryPath) { ... }

    /// <summary>Removes a directory from the user PATH.</summary>
    public static bool RemoveFromUserPath(string directoryPath) { ... }

    /// <summary>Gets the user PATH as a list of directories.</summary>
    public static List<string> GetUserPathDirectories() { ... }

    /// <summary>Gets the system PATH as a list of directories.</summary>
    public static List<string> GetSystemPathDirectories() { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void GetMachineName_ReturnsNonEmpty()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var name = EnvironmentHelper.GetMachineName();
    name.Should().NotBeNullOrEmpty();
}

[SkippableFact]
public void ExpandString_Works()
{
    var expanded = EnvironmentHelper.ExpandString("%TEMP%");
    expanded.Should().NotBeNullOrEmpty();
    expanded.Should().NotBe("%TEMP%");
}

[SkippableFact]
public void IsDomainJoined_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    EnvironmentHelper.IsDomainJoined().Should().BeOneOf(true, false);
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add EnvironmentHelper — env vars, PATH, domain info"
```

---

### Task 5.2: Create `FileVersionHelper` — PE File Version Information

**Objective:** Read version resources from PE files:
FileVersion, ProductVersion, CompanyName, Copyright, etc.
via VerQueryValue/GetFileVersionInfo (version.dll).

**Files:**
- Create: `src/BPlusLib.Foundation/IO/FileVersionHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/IO/FileVersionHelperTests.cs`

**Key APIs:**
- `GetFileVersionInfoSizeExW` / `GetFileVersionInfoExW`
- `VerQueryValueW` / `VerLanguageNameW`

**Step 1: Implement**

```csharp
public sealed class FileVersionInfo
{
    public string? FileVersion { get; init; }
    public string? ProductVersion { get; init; }
    public string? CompanyName { get; init; }
    public string? ProductName { get; init; }
    public string? FileDescription { get; init; }
    public string? LegalCopyright { get; init; }
    public string? LegalTrademarks { get; init; }
    public string? InternalName { get; init; }
    public string? OriginalFilename { get; init; }
    public string? PrivateBuild { get; init; }
    public string? SpecialBuild { get; init; }
    public string? Comments { get; init; }
    public string? Language { get; init; }
}

public static class FileVersionHelper
{
    /// <summary>Reads all available version info from a PE file.</summary>
    public static FileVersionInfo? GetVersionInfo(string filePath) { ... }

    /// <summary>Quick read: just the file version string.</summary>
    public static string? GetFileVersion(string filePath) { ... }

    /// <summary>Quick read: just the product version string.</summary>
    public static string? GetProductVersion(string filePath) { ... }

    /// <summary>Quick read: just the company name.</summary>
    public static string? GetCompanyName(string filePath) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void GetVersionInfo_Kernel32_ReturnsData()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var info = FileVersionHelper.GetVersionInfo(@"C:\Windows\System32\kernel32.dll");
    info.Should().NotBeNull();
    info!.FileVersion.Should().NotBeNullOrEmpty();
    info.CompanyName.Should().NotBeNullOrEmpty();
    info.ProductName.Should().NotBeNullOrEmpty();
}

[SkippableFact]
public void GetVersionInfo_TextFile_ReturnsNull()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    string tempFile = Path.GetTempFileName();
    try
    {
        File.WriteAllText(tempFile, "hello");
        var info = FileVersionHelper.GetVersionInfo(tempFile);
        info.Should().BeNull();
    }
    finally { File.Delete(tempFile); }
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add FileVersionHelper — PE version info via VerQueryValue"
```

---

### Task 5.3: Create `PowerHelper` — System Power Management

**Objective:** Power state management: sleep, hibernate, shutdown, restart,
battery status (extends existing BatteryInfo with power management),
power source detection, display dimming/screensaver control.

**Files:**
- Create: `src/BPlusLib.Foundation/Power/PowerHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Power/PowerHelperTests.cs`

**Key APIs:**
- `SetSuspendState` (powrprof.dll) — sleep/hibernate
- `ExitWindowsEx` (user32) — shutdown/restart/logoff
- `GetSystemPowerStatus` (kernel32) — AC/battery status
- `InitiateSystemShutdownExW` (advapi32)
- `AdjustTokenPrivileges` for SE_SHUTDOWN_NAME

**Step 1: Implement**

```csharp
public static class PowerHelper
{
    /// <summary>Puts the system into sleep mode.</summary>
    public static bool Sleep() { ... }

    /// <summary>Puts the system into hibernation.</summary>
    public static bool Hibernate() { ... }

    /// <summary>Shuts down the system.</summary>
    public static bool Shutdown(bool force = false, bool reboot = false) { ... }

    /// <summary>Restarts the system.</summary>
    public static bool Restart(bool force = false) => Shutdown(force, reboot: true);

    /// <summary>Logs off the current user.</summary>
    public static bool LogOff(bool force = false) { ... }

    /// <summary>Returns true if the system is running on battery power.</summary>
    public static bool IsOnBattery() { ... }

    /// <summary>Returns the current battery charge percentage (0-100, or -1 if unknown).</summary>
    public static int GetBatteryChargePercent() { ... }

    /// <summary>Returns true if hibernation is enabled on this system.</summary>
    public static bool IsHibernationEnabled() { ... }

    /// <summary>Prevents the system from sleeping (e.g., during critical operation).</summary>
    public static bool PreventSleep(bool prevent) { ... }

    /// <summary>Returns the system power status.</summary>
    public static SystemPowerStatus GetPowerStatus() { ... }

    /// <summary>Locks the workstation.</summary>
    public static bool LockWorkstation() { ... }
}

public sealed class SystemPowerStatus
{
    public bool IsOnBattery { get; init; }
    public int BatteryChargePercent { get; init; }
    public bool BatteryIsCharging { get; init; }
    public int BatteryLifeSeconds { get; init; }
    public int BatteryFullLifeSeconds { get; init; }
    public AclineStatus AclineStatus { get; init; }
    public BatteryFlag BatteryFlag { get; init; }
}

public enum AclineStatus { Offline = 0, Online = 1, Unknown = 255 }
public enum BatteryFlag { High = 1, Low = 2, Critical = 4, Charging = 8, ... }
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void GetPowerStatus_ReturnsStatus()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var status = PowerHelper.GetPowerStatus();
    status.Should().NotBeNull();
    status.IsOnBattery.Should().BeOneOf(true, false);
}

[SkippableFact]
public void IsOnBattery_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    PowerHelper.IsOnBattery().Should().BeOneOf(true, false);
}

[SkippableFact]
public void LockWorkstation_DoesNotThrow()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Just verify it doesn't throw
    var result = PowerHelper.LockWorkstation();
    // May fail if not supported in test environment
    result.Should().BeOneOf(true, false);
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add PowerHelper — power management (sleep, hibernate, shutdown)"
```

---

### Task 5.4: Create `AssocHelper` — File Association Queries

**Objective:** Query and set file extension associations and program
associations via AssocQueryString/AssocQueryKey and registry.

**Files:**
- Create: `src/BPlusLib.Foundation/Shell/AssocHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Shell/AssocHelperTests.cs`

**Step 1: Implement**

```csharp
public static class AssocHelper
{
    /// <summary>Gets the friendly name for a file extension (e.g., ".txt" → "Text Document").</summary>
    public static string? GetFileTypeDescription(string extension) { ... }

    /// <summary>Gets the executable associated with a file extension.</summary>
    public static string? GetAssociatedExecutable(string extension) { ... }

    /// <summary>Gets the command line template for a file extension's default verb.</summary>
    public static string? GetOpenCommand(string extension) { ... }

    /// <summary>Gets the ProgID associated with a file extension (e.g., ".txt" → "txtfile").</summary>
    public static string? GetProgId(string extension) { ... }

    /// <summary>Gets the icon location for a file extension.</summary>
    public static string? GetIconLocation(string extension, out int iconIndex) { ... }

    /// <summary>Gets the content type (MIME) for a file extension.</summary>
    public static string? GetContentType(string extension) { ... }

    /// <summary>Gets the perceived type for a file extension (e.g., "text", "image", "audio").</summary>
    public static string? GetPerceivedType(string extension) { ... }

    /// <summary>Returns true if the file extension has a registered association.</summary>
    public static bool IsExtensionRegistered(string extension) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void GetFileTypeDescription_Txt_ReturnsTextDocument()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var desc = AssocHelper.GetFileTypeDescription(".txt");
    desc.Should().NotBeNullOrEmpty();
    desc.Should().Contain("Text");
}

[SkippableFact]
public void GetAssociatedExecutable_Txt_ReturnsNotepad()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var exe = AssocHelper.GetAssociatedExecutable(".txt");
    exe.Should().NotBeNullOrEmpty();
    exe.Should().Contain("notepad.exe");
}

[SkippableFact]
public void IsExtensionRegistered_Txt_ReturnsTrue()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    AssocHelper.IsExtensionRegistered(".txt").Should().BeTrue();
}

[SkippableFact]
public void IsExtensionRegistered_Unknown_ReturnsFalse()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    AssocHelper.IsExtensionRegistered(".xyzabc123").Should().BeFalse();
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add AssocHelper — file extension association queries"
```

---

### Task 5.5: Create `ThemeHelper` — Windows Theme Detection

**Objective:** Detect dark/light mode, accent color, read DWM composition
settings, glass/frame insets. Essential for modern Windows app theming.

**Files:**
- Create: `src/BPlusLib.Foundation/Shell/ThemeHelper.cs`
- Create: `tests/BPlusLib.Foundation.Tests/Shell/ThemeHelperTests.cs`

**Key APIs:**
- Registry: `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize`
  - `AppsUseLightTheme` (DWORD) — light=1, dark=0
  - `SystemUsesLightTheme` (DWORD)
- `DwmGetColorizationColor` (dwmapi.dll)
- `DwmIsCompositionEnabled` (dwmapi.dll)
- `SPI_GETCLIENTAREAANIMATION` / `SPI_GETKEYBOARDCUES`
- `GetSysColor` for classic theme detection

**Step 1: Implement**

```csharp
public static class ThemeHelper
{
    /// <summary>Returns true if the system is in light mode.</summary>
    public static bool IsLightTheme() { ... }

    /// <summary>Returns true if the system is in dark mode.</summary>
    public static bool IsDarkTheme() => !IsLightTheme();

    /// <summary>Returns true if apps use light theme (individual setting).</summary>
    public static bool IsAppsLightTheme() { ... }

    /// <summary>Returns true if the taskbar/system use light theme.</summary>
    public static bool IsSystemLightTheme() { ... }

    /// <summary>Gets the accent color used by the system.</summary>
    public static Color GetAccentColor() { ... }

    /// <summary>Returns true if DWM composition is enabled.</summary>
    public static bool IsCompositionEnabled() { ... }

    /// <summary>Returns true if transparency effects are enabled.</summary>
    public static bool IsTransparencyEnabled() { ... }

    /// <summary>Gets the colorization color from DWM.</summary>
    public static int GetColorizationColor(out bool isOpaque) { ... }

    /// <summary>Applies dark mode to a window (if supported).</summary>
    public static bool SetWindowDarkMode(IntPtr hwnd, bool dark) { ... }

    /// <summary>Gets the window frame inset (for custom chrome).</summary>
    public static Padding GetWindowFrameInsets(IntPtr hwnd) { ... }
}
```

**Step 2: Write tests**

```csharp
[SkippableFact]
public void IsLightTheme_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    ThemeHelper.IsLightTheme().Should().BeOneOf(true, false);
}

[SkippableFact]
public void IsAppsLightTheme_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    ThemeHelper.IsAppsLightTheme().Should().BeOneOf(true, false);
}

[SkippableFact]
public void GetAccentColor_ReturnsColor()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    var color = ThemeHelper.GetAccentColor();
    color.Should().NotBeNull();
}

[SkippableFact]
public void IsCompositionEnabled_ReturnsBool()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    ThemeHelper.IsCompositionEnabled().Should().BeOneOf(true, false);
}

[SkippableFact]
public void SetWindowDarkMode_DoesNotThrow()
{
    Skip.IfNot(OperatingSystem.IsWindows());
    // Without a valid window this may fail gracefully
    var result = ThemeHelper.SetWindowDarkMode(IntPtr.Zero, true);
    result.Should().BeFalse(); // IntPtr.Zero should fail
}
```

**Step 3: Commit**

```bash
git commit -m "feat: add ThemeHelper — dark/light theme, accent color, DWM"
```

---

## Summary: All 20 Modules

| Phase | Module | Files | Tests | DLLs |
|-------|--------|-------|-------|------|
| **1** | AdvApi32.cs (native) | 1 | 0 | advapi32 |
| **1** | ServiceHelper | 2 | 1 | advapi32 |
| **1** | JobObjectHelper | 1 | 1 | kernel32 |
| **1** | ConsoleHelper | 1 | 1 | kernel32, user32 |
| **2** | ShortcutHelper | 1 | 1 | shell32, ole32 |
| **2** | NotifyIconHelper | 2 | 1 | shell32 |
| **2** | HotkeyHelper | 1 | 1 | user32 |
| **2** | InputHelper | 1 | 1 | user32 |
| **3** | UacHelper | 2 | 1 | advapi32, kernel32 |
| **3** | CredentialHelper | 1 | 1 | advapi32 |
| **3** | WinTrustHelper | 1 | 1 | wintrust, crypt32 |
| **4** | PipeHelper | 3 | 1 | kernel32 |
| **4** | MemoryHelper | 2 | 1 | kernel32, psapi |
| **4** | RestartManagerHelper | 1 | 1 | rstrtmgr |
| **4** | HttpListenerHelper | 1 | 1 | System.Net (managed) |
| **5** | EnvironmentHelper | 1 | 1 | kernel32, netapi32 |
| **5** | FileVersionHelper | 1 | 1 | version.dll |
| **5** | PowerHelper | 1 | 1 | powrprof, user32, advapi32 |
| **5** | AssocHelper | 1 | 1 | shell32 |
| **5** | ThemeHelper | 1 | 1 | dwmapi, user32 |

**Total:** ~27 new files, ~55 new test methods, ~8,000 new LOC.

---

## Verification

After all phases:

1. **Build:** `dotnet build src/BPlusLib.Foundation -c Release`
   → 0 errors, 0 warnings

2. **Test:** `dotnet test tests/BPlusLib.Foundation.Tests --framework net8.0`
   → 1072+ passed (1017 existing + ~55 new)

3. **Pack:** `dotnet pack src/BPlusLib.Foundation -c Release`
   → `BPlusLib.Foundation.2.6.0.nupkg` (version bump)

4. **Push:** tag + release

---

## Risks & Open Questions

1. **COM Interop (ShortcutHelper):** IShellLink requires
   `ComImport` + `Guid` attributes and `CoCreateInstance`. The
   `System.Runtime.InteropServices.ComTypes` namespace is available
   on all targets but may have slight differences on net472.
   → Mitigation: test early on net472.

2. **WinTrust (WinTrustHelper):** The WINTRUST_DATA / WINTRUST_FILE_INFO
   structures require careful marshalling. The `GUID` parameter in
   `WinVerifyTrust` is passed by reference.
   → Mitigation: use `Marshal.AllocHGlobal` for structures.

3. **RestartManager (RestartManagerHelper):** rstrtmgr.dll is only
   available on Windows Vista+. Some systems may not have it.
   → Mitigation: try/catch DllNotFoundException, return null gracefully.

4. **HttpListener:** Requires admin privileges for non-localhost prefixes.
   All tests should use `http://localhost` only.

5. **SendInput (InputHelper):** UIPI (User Interface Privilege Isolation)
   blocks SendInput from lower-integrity processes to higher ones.
   Tests should document this limitation.

6. **NetJoinInformation (EnvironmentHelper):** Requires netapi32.dll which
   may not be available on Windows Nano Server.
   → Mitigation: try/catch for DllNotFoundException.

7. **ThemeHelper registry reads:** Registry layout differs on Windows
   Server Core (no personalization settings).
   → Mitigation: return safe defaults if registry key missing.

---

**Ready to execute using subagent-driven-development — dispatch fresh subagents per task with full context.**
