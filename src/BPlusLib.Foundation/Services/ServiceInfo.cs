// <copyright file="ServiceInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System.ComponentModel;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Services
{
    /// <summary>
    /// Represents the current state of a Windows service,
    /// corresponding to the SERVICE_STATUS.dwCurrentState values.
    /// </summary>
    public enum ServiceState
    {
        /// <summary>The service is not running (SERVICE_STOPPED).</summary>
        Stopped = 0x01,

        /// <summary>The service is starting (SERVICE_START_PENDING).</summary>
        StartPending = 0x02,

        /// <summary>The service is stopping (SERVICE_STOP_PENDING).</summary>
        StopPending = 0x03,

        /// <summary>The service is running (SERVICE_RUNNING).</summary>
        Running = 0x04,

        /// <summary>The service is about to continue (SERVICE_CONTINUE_PENDING).</summary>
        ContinuePending = 0x05,

        /// <summary>The service is about to pause (SERVICE_PAUSE_PENDING).</summary>
        PausePending = 0x06,

        /// <summary>The service is paused (SERVICE_PAUSED).</summary>
        Paused = 0x07,

        /// <summary>The service state is unknown or could not be determined.</summary>
        Unknown = 0xFF,
    }

    /// <summary>
    /// Specifies the start type of a Windows service,
    /// corresponding to the SERVICE_*_START constants in advapi32.
    /// </summary>
    public enum ServiceStartType
    {
        /// <summary>A device driver started by the boot loader (SERVICE_BOOT_START).</summary>
        Boot = 0x00,

        /// <summary>A device driver started by the I/O subsystem (SERVICE_SYSTEM_START).</summary>
        System = 0x01,

        /// <summary>A service that automatically starts when the system starts (SERVICE_AUTO_START).</summary>
        Automatic = 0x02,

        /// <summary>A service that is started only when explicitly requested (SERVICE_DEMAND_START).</summary>
        Manual = 0x03,

        /// <summary>A service that cannot be started (SERVICE_DISABLED).</summary>
        Disabled = 0x04,

        /// <summary>The service start type is unknown or could not be determined.</summary>
        Unknown = 0xFF,
    }

    /// <summary>
    /// Provides detailed information about a Windows service as retrieved from the
    /// Service Control Manager (SCM).
    /// </summary>
    public sealed class ServiceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInfo"/> class.
        /// </summary>
        public ServiceInfo()
        {
        }

        /// <summary>
        /// Gets or sets the short (internal) name of the service.
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the service as shown in the Services MMC snap-in.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the current state of the service.
        /// </summary>
        public ServiceState State { get; set; }

        /// <summary>
        /// Gets or sets the start type of the service.
        /// </summary>
        public ServiceStartType StartType { get; set; }

        /// <summary>
        /// Gets or sets the service type flags (e.g., SERVICE_WIN32_OWN_PROCESS, SERVICE_WIN32_SHARE_PROCESS).
        /// </summary>
        public uint ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the control flags indicating which service control codes the service accepts.
        /// </summary>
        public uint ControlsAccepted { get; set; }

        /// <summary>
        /// Gets or sets the last Win32 exit code for the service.
        /// </summary>
        public uint Win32ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the last service-specific exit code for the service.
        /// </summary>
        public uint ServiceSpecificExitCode { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint value used during a pending start, stop, or pause operation.
        /// </summary>
        public uint CheckPoint { get; set; }

        /// <summary>
        /// Gets or sets the wait hint in milliseconds for a pending start, stop, or pause operation.
        /// </summary>
        public uint WaitHint { get; set; }

        /// <summary>
        /// Gets or sets the process ID of the service process (zero if the service is not running).
        /// </summary>
        public uint ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified path to the service binary executable, if available.
        /// </summary>
        public string? BinaryPathName { get; set; }

        /// <summary>
        /// Gets or sets the name of the account under which the service runs, if available.
        /// </summary>
        public string? ServiceStartName { get; set; }

        /// <summary>
        /// Gets a value indicating whether the service is currently running.
        /// </summary>
        public bool IsRunning => State == ServiceState.Running;

        /// <summary>
        /// Gets a value indicating whether the service is in a pending state
        /// (start pending, stop pending, pause pending, or continue pending).
        /// </summary>
        public bool IsPending =>
            State == ServiceState.StartPending ||
            State == ServiceState.StopPending ||
            State == ServiceState.PausePending ||
            State == ServiceState.ContinuePending;

        /// <summary>
        /// Returns a string representation of the service information.
        /// </summary>
        /// <returns>A string containing the service name, display name, and current state.</returns>
        public override string ToString()
            => $"{(ServiceName ?? string.Empty)} ({(DisplayName ?? string.Empty)}): {State}";
    }
}
