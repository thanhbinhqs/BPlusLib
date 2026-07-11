// <copyright file="ServiceHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Services
{
    /// <summary>
    /// Provides thread-safe static methods for managing Windows services through the
    /// Service Control Manager (SCM) using P/Invoke into advapi32.dll.
    /// </summary>
    /// <remarks>
    /// All methods gracefully return <see langword="null"/> or <see langword="false"/>
    /// on failure or on non-Windows platforms. No exceptions are thrown.
    /// Thread safety is ensured via a <see cref="SemaphoreSlim"/> guard.
    /// </remarks>
    public static class ServiceHelper
    {
        private static readonly SemaphoreSlim SyncLock = new(1, 1);

        /// <summary>
        /// Default info level for <see cref="AdvApi32.QueryServiceStatusEx"/> (SC_STATUS_PROCESS_INFO).
        /// </summary>
        private const uint SC_STATUS_PROCESS_INFO = 0;

        /// <summary>
        /// Maps a <see cref="ServiceStartType"/> enum value to the corresponding
        /// SERVICE_*_START constant from advapi32.
        /// </summary>
        /// <param name="startType">The managed start type value.</param>
        /// <returns>The native uint constant, or <see cref="AdvApi32.SERVICE_DEMAND_START"/> if the value is Unknown.</returns>
        private static uint StartTypeToNative(ServiceStartType startType) => startType switch
        {
            ServiceStartType.Boot => AdvApi32.SERVICE_BOOT_START,
            ServiceStartType.System => AdvApi32.SERVICE_SYSTEM_START,
            ServiceStartType.Automatic => AdvApi32.SERVICE_AUTO_START,
            ServiceStartType.Manual => AdvApi32.SERVICE_DEMAND_START,
            ServiceStartType.Disabled => AdvApi32.SERVICE_DISABLED,
            _ => AdvApi32.SERVICE_DEMAND_START,
        };

        /// <summary>
        /// Maps a native SERVICE_*_START constant to the corresponding <see cref="ServiceStartType"/>.
        /// </summary>
        /// <param name="native">The native uint value.</param>
        /// <returns>The managed start type value, or <see cref="ServiceStartType.Unknown"/> if unrecognized.</returns>
        private static ServiceStartType NativeToStartType(uint native) => native switch
        {
            AdvApi32.SERVICE_BOOT_START => ServiceStartType.Boot,
            AdvApi32.SERVICE_SYSTEM_START => ServiceStartType.System,
            AdvApi32.SERVICE_AUTO_START => ServiceStartType.Automatic,
            AdvApi32.SERVICE_DEMAND_START => ServiceStartType.Manual,
            AdvApi32.SERVICE_DISABLED => ServiceStartType.Disabled,
            _ => ServiceStartType.Unknown,
        };

        /// <summary>
        /// Maps a native SERVICE_STATUS.dwCurrentState to the corresponding <see cref="ServiceState"/>.
        /// </summary>
        /// <param name="native">The native uint state value.</param>
        /// <returns>The managed state value, or <see cref="ServiceState.Unknown"/> if unrecognized.</returns>
        private static ServiceState NativeToServiceState(uint native) => native switch
        {
            AdvApi32.SERVICE_STOPPED => ServiceState.Stopped,
            AdvApi32.SERVICE_START_PENDING => ServiceState.StartPending,
            AdvApi32.SERVICE_STOP_PENDING => ServiceState.StopPending,
            AdvApi32.SERVICE_RUNNING => ServiceState.Running,
            AdvApi32.SERVICE_CONTINUE_PENDING => ServiceState.ContinuePending,
            AdvApi32.SERVICE_PAUSE_PENDING => ServiceState.PausePending,
            AdvApi32.SERVICE_PAUSED => ServiceState.Paused,
            _ => ServiceState.Unknown,
        };

        /// <summary>
        /// Opens the SCM database on the local machine with the specified access mask.
        /// </summary>
        /// <param name="desiredAccess">Access flags for the SCM (e.g., <see cref="AdvApi32.SC_MANAGER_ALL_ACCESS"/>).</param>
        /// <returns>A handle to the SCM, or <see cref="IntPtr.Zero"/> on failure.</returns>
        private static IntPtr OpenScm(uint desiredAccess)
        {
            try
            {
                return AdvApi32.OpenSCManagerW(null, null, desiredAccess);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Opens an existing service with the specified access mask.
        /// </summary>
        /// <param name="scmHandle">Handle to the SCM database.</param>
        /// <param name="serviceName">Name of the service to open.</param>
        /// <param name="desiredAccess">Access flags for the service.</param>
        /// <returns>A handle to the service, or <see cref="IntPtr.Zero"/> on failure.</returns>
        private static IntPtr OpenService(IntPtr scmHandle, string serviceName, uint desiredAccess)
        {
            try
            {
                return AdvApi32.OpenServiceW(scmHandle, serviceName, desiredAccess);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Safely closes an SCM or service handle.
        /// </summary>
        /// <param name="handle">The handle to close.</param>
        private static void SafeCloseHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                AdvApi32.CloseServiceHandle(handle);
            }
            catch
            {
                // Suppress any finalizer exceptions.
            }
        }

        /// <summary>
        /// Waits for a service to reach the target state, polling with <see cref="QueryServiceStatus"/>
        /// at 250 ms intervals until the timeout expires.
        /// </summary>
        /// <param name="serviceHandle">An open handle to the service with SERVICE_QUERY_STATUS access.</param>
        /// <param name="targetStates">The set of states considered "done" (the wait is satisfied).</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
        /// <returns><see langword="true"/> if the service reached one of the target states within the timeout; otherwise <see langword="false"/>.</returns>
        private static bool WaitForState(IntPtr serviceHandle, HashSet<uint> targetStates, int timeoutMs)
        {
            var status = default(SERVICE_STATUS);
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                try
                {
                    if (AdvApi32.QueryServiceStatus(serviceHandle, ref status))
                    {
                        if (targetStates.Contains(status.dwCurrentState))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }

                Thread.Sleep(250);
                elapsed += 250;
            }

            // Final check after timeout
            try
            {
                if (AdvApi32.QueryServiceStatus(serviceHandle, ref status))
                {
                    return targetStates.Contains(status.dwCurrentState);
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        /// <summary>
        /// Retrieves information about a single service by name.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <returns>
        /// A <see cref="ServiceInfo"/> instance describing the service,
        /// or <see langword="null"/> if the service does not exist or an error occurred.
        /// </returns>
        public static ServiceInfo? GetService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return null;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_QUERY_STATUS | AdvApi32.SERVICE_QUERY_CONFIG);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return null;
                    }

                    try
                    {
                        return PopulateServiceInfo(serviceHandle, serviceName);
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Populates a <see cref="ServiceInfo"/> instance from an open service handle.
        /// </summary>
        private static ServiceInfo? PopulateServiceInfo(IntPtr serviceHandle, string serviceName)
        {
            try
            {
                var info = new ServiceInfo
                {
                    ServiceName = serviceName,
                };

                // Get status (including process ID)
                var statusProc = default(SERVICE_STATUS_PROCESS);
                int size = Marshal.SizeOf<SERVICE_STATUS_PROCESS>();
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    if (AdvApi32.QueryServiceStatusEx(
                            serviceHandle,
                            SC_STATUS_PROCESS_INFO,
                            buffer,
                            (uint)size,
                            out _))
                    {
                        statusProc = Marshal.PtrToStructure<SERVICE_STATUS_PROCESS>(buffer);
                        info.State = NativeToServiceState(statusProc.dwCurrentState);
                        info.ServiceType = statusProc.dwServiceType;
                        info.ControlsAccepted = statusProc.dwControlsAccepted;
                        info.Win32ExitCode = statusProc.dwWin32ExitCode;
                        info.ServiceSpecificExitCode = statusProc.dwServiceSpecificExitCode;
                        info.CheckPoint = statusProc.dwCheckPoint;
                        info.WaitHint = statusProc.dwWaitHint;
                        info.ProcessId = statusProc.dwProcessId;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                // Get configuration (start type, binary path, account name)
                uint bytesNeeded = 0;

                // First call to get required buffer size
                AdvApi32.QueryServiceConfigW(serviceHandle, IntPtr.Zero, 0, out bytesNeeded);
                if (bytesNeeded > 0)
                {
                    IntPtr configBuffer = Marshal.AllocHGlobal((int)bytesNeeded);
                    try
                    {
                        if (AdvApi32.QueryServiceConfigW(
                                serviceHandle,
                                configBuffer,
                                bytesNeeded,
                                out _))
                        {
                            var config = Marshal.PtrToStructure<QUERY_SERVICE_CONFIGW>(configBuffer);
                            info.StartType = NativeToStartType(config.dwStartType);

                            if (config.lpBinaryPathName != IntPtr.Zero)
                            {
                                info.BinaryPathName = Marshal.PtrToStringUni(config.lpBinaryPathName);
                            }

                            if (config.lpServiceStartName != IntPtr.Zero)
                            {
                                info.ServiceStartName = Marshal.PtrToStringUni(config.lpServiceStartName);
                            }

                            if (config.lpDisplayName != IntPtr.Zero)
                            {
                                info.DisplayName = Marshal.PtrToStringUni(config.lpDisplayName);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(configBuffer);
                    }
                }

                // Fall back to display name from status if config query failed
                info.DisplayName ??= serviceName;

                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Starts a service and optionally waits for it to reach the running state.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <param name="waitMs">Maximum time in milliseconds to wait for the service to reach the running state (default 30000).</param>
        /// <returns><see langword="true"/> if the service was started successfully (or was already running); otherwise <see langword="false"/>.</returns>
        public static bool StartService(string serviceName, int waitMs = 30000)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            if (waitMs < 0)
            {
                waitMs = 0;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_START | AdvApi32.SERVICE_QUERY_STATUS);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        // Check if already running
                        var status = default(SERVICE_STATUS);
                        if (AdvApi32.QueryServiceStatus(serviceHandle, ref status))
                        {
                            if (status.dwCurrentState == AdvApi32.SERVICE_RUNNING)
                            {
                                return true;
                            }
                        }

                        // Start the service
                        if (!AdvApi32.StartServiceW(serviceHandle, 0, null))
                        {
                            return false;
                        }

                        // Wait for running state if requested
                        if (waitMs > 0)
                        {
                            return WaitForState(
                                serviceHandle,
                                new HashSet<uint> { AdvApi32.SERVICE_RUNNING },
                                waitMs);
                        }

                        return true;
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Stops a service and optionally waits for it to reach the stopped state.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <param name="waitMs">Maximum time in milliseconds to wait for the service to reach the stopped state (default 30000).</param>
        /// <returns><see langword="true"/> if the service was stopped successfully (or was already stopped); otherwise <see langword="false"/>.</returns>
        public static bool StopService(string serviceName, int waitMs = 30000)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            if (waitMs < 0)
            {
                waitMs = 0;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_STOP | AdvApi32.SERVICE_QUERY_STATUS);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        var status = default(SERVICE_STATUS);

                        // Check if already stopped
                        if (AdvApi32.QueryServiceStatus(serviceHandle, ref status))
                        {
                            if (status.dwCurrentState == AdvApi32.SERVICE_STOPPED)
                            {
                                return true;
                            }
                        }

                        // Send stop control
                        if (!AdvApi32.ControlService(serviceHandle, AdvApi32.SERVICE_CONTROL_STOP, ref status))
                        {
                            return false;
                        }

                        // Wait for stopped state if requested
                        if (waitMs > 0)
                        {
                            return WaitForState(
                                serviceHandle,
                                new HashSet<uint> { AdvApi32.SERVICE_STOPPED },
                                waitMs);
                        }

                        return true;
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Restarts a service by stopping it and then starting it again.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <param name="waitMs">Maximum time in milliseconds to wait for each state transition (default 30000).</param>
        /// <returns><see langword="true"/> if the service was restarted successfully; otherwise <see langword="false"/>.</returns>
        public static bool RestartService(string serviceName, int waitMs = 30000)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            if (waitMs < 0)
            {
                waitMs = 0;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_START | AdvApi32.SERVICE_STOP | AdvApi32.SERVICE_QUERY_STATUS);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        var status = default(SERVICE_STATUS);

                        // Check current state
                        if (!AdvApi32.QueryServiceStatus(serviceHandle, ref status))
                        {
                            return false;
                        }

                        // Stop the service if it is not already stopped
                        if (status.dwCurrentState != AdvApi32.SERVICE_STOPPED)
                        {
                            if (!AdvApi32.ControlService(serviceHandle, AdvApi32.SERVICE_CONTROL_STOP, ref status))
                            {
                                return false;
                            }

                            if (waitMs > 0)
                            {
                                if (!WaitForState(
                                        serviceHandle,
                                        new HashSet<uint> { AdvApi32.SERVICE_STOPPED },
                                        waitMs))
                                {
                                    return false;
                                }
                            }
                        }

                        // Start the service
                        if (!AdvApi32.StartServiceW(serviceHandle, 0, null))
                        {
                            return false;
                        }

                        // Wait for running state if requested
                        if (waitMs > 0)
                        {
                            return WaitForState(
                                serviceHandle,
                                new HashSet<uint> { AdvApi32.SERVICE_RUNNING },
                                waitMs);
                        }

                        return true;
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Pauses a running service.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <returns><see langword="true"/> if the pause control was sent successfully; otherwise <see langword="false"/>.</returns>
        public static bool PauseService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_PAUSE_CONTINUE);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        var status = default(SERVICE_STATUS);
                        return AdvApi32.ControlService(
                            serviceHandle,
                            AdvApi32.SERVICE_CONTROL_PAUSE,
                            ref status);
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Continues a paused service.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <returns><see langword="true"/> if the continue control was sent successfully; otherwise <see langword="false"/>.</returns>
        public static bool ContinueService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_PAUSE_CONTINUE);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        var status = default(SERVICE_STATUS);
                        return AdvApi32.ControlService(
                            serviceHandle,
                            AdvApi32.SERVICE_CONTROL_CONTINUE,
                            ref status);
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Creates a new Windows service entry in the SCM database.
        /// </summary>
        /// <param name="serviceName">The internal name for the service.</param>
        /// <param name="displayName">The display name shown in the Services MMC snap-in.</param>
        /// <param name="binaryPathName">The fully qualified path to the service executable.</param>
        /// <param name="startType">The start type for the service (default <see cref="ServiceStartType.Manual"/>).</param>
        /// <param name="serviceType">The service type flags (default SERVICE_WIN32_OWN_PROCESS).</param>
        /// <param name="errorControl">The error control level for the service (default SERVICE_ERROR_NORMAL).</param>
        /// <param name="loadOrderGroup">Optional load order group name.</param>
        /// <param name="dependencies">Optional null-separated list of dependency service names.</param>
        /// <param name="serviceStartName">Optional account name under which the service runs (<see langword="null"/> for LocalSystem).</param>
        /// <param name="password">Optional password for the service account.</param>
        /// <returns><see langword="true"/> if the service was created successfully; otherwise <see langword="false"/>.</returns>
        public static bool CreateService(
            string serviceName,
            string displayName,
            string binaryPathName,
            ServiceStartType startType = ServiceStartType.Manual,
            uint serviceType = 0x10,  // SERVICE_WIN32_OWN_PROCESS
            uint errorControl = 0x01, // SERVICE_ERROR_NORMAL
            string? loadOrderGroup = null,
            string? dependencies = null,
            string? serviceStartName = null,
            string? password = null)
        {
            if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(binaryPathName))
            {
                return false;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CREATE_SERVICE);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    uint tagId = 0;
                    IntPtr serviceHandle = AdvApi32.CreateServiceW(
                        scmHandle,
                        serviceName,
                        displayName ?? serviceName,
                        AdvApi32.SERVICE_ALL_ACCESS,
                        serviceType,
                        StartTypeToNative(startType),
                        errorControl,
                        binaryPathName,
                        loadOrderGroup,
                        out tagId,
                        dependencies,
                        serviceStartName,
                        password);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    // Close the service handle; the service persists in SCM.
                    SafeCloseHandle(serviceHandle);
                    return true;
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Marks a service for deletion from the SCM database.
        /// </summary>
        /// <param name="serviceName">The internal name of the service to delete.</param>
        /// <returns><see langword="true"/> if the service was marked for deletion successfully; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// The service is not actually removed until all open handles to it are closed
        /// and the service is stopped.
        /// </remarks>
        public static bool DeleteService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_ALL_ACCESS);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_ALL_ACCESS);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        return AdvApi32.DeleteService(serviceHandle);
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Enumerates all services matching the specified state filter.
        /// </summary>
        /// <param name="stateFilter">
        /// The state filter for enumeration.
        /// Use <see cref="ServiceState.Unknown"/> (or any unrecognized value) to enumerate all services.
        /// </param>
        /// <returns>
        /// A list of <see cref="ServiceInfo"/> objects representing the matching services,
        /// or an empty list if enumeration failed.
        /// </returns>
        public static List<ServiceInfo> EnumerateServices(ServiceState stateFilter)
        {
            var results = new List<ServiceInfo>();

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_ENUMERATE_SERVICE);
                if (scmHandle == IntPtr.Zero)
                {
                    return results;
                }

                try
                {
                    uint nativeStateFilter = stateFilter switch
                    {
                        ServiceState.Stopped => AdvApi32.SERVICE_STOPPED,
                        ServiceState.StartPending => AdvApi32.SERVICE_START_PENDING,
                        ServiceState.StopPending => AdvApi32.SERVICE_STOP_PENDING,
                        ServiceState.Running => AdvApi32.SERVICE_RUNNING,
                        ServiceState.ContinuePending => AdvApi32.SERVICE_CONTINUE_PENDING,
                        ServiceState.PausePending => AdvApi32.SERVICE_PAUSE_PENDING,
                        ServiceState.Paused => AdvApi32.SERVICE_PAUSED,
                        _ => AdvApi32.SERVICE_STATE_ALL,
                    };

                    uint resumeHandle = 0;
                    const uint initialBufferSize = 16384; // 16 KB initial buffer
                    uint bufferSize = initialBufferSize;
                    IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);

                    try
                    {
                        while (true)
                        {
                            bool success = AdvApi32.EnumServicesStatusExW(
                                scmHandle,
                                AdvApi32.SC_ENUM_PROCESS_INFO,
                                AdvApi32.SERVICE_WIN32,
                                nativeStateFilter,
                                buffer,
                                bufferSize,
                                out uint bytesNeeded,
                                out uint servicesReturned,
                                ref resumeHandle,
                                null);

                            if (success)
                            {
                                // Parse results
                                int structSize = Marshal.SizeOf<ENUM_SERVICE_STATUS_PROCESSW>();
                                IntPtr current = buffer;

                                for (uint i = 0; i < servicesReturned; i++)
                                {
                                    var entry = Marshal.PtrToStructure<ENUM_SERVICE_STATUS_PROCESSW>(current);
                                    var info = new ServiceInfo
                                    {
                                        ServiceName = entry.lpServiceName != IntPtr.Zero
                                            ? Marshal.PtrToStringUni(entry.lpServiceName)
                                            : null,
                                        DisplayName = entry.lpDisplayName != IntPtr.Zero
                                            ? Marshal.PtrToStringUni(entry.lpDisplayName)
                                            : null,
                                        State = NativeToServiceState(entry.ServiceStatusProcess.dwCurrentState),
                                        ServiceType = entry.ServiceStatusProcess.dwServiceType,
                                        ControlsAccepted = entry.ServiceStatusProcess.dwControlsAccepted,
                                        Win32ExitCode = entry.ServiceStatusProcess.dwWin32ExitCode,
                                        ServiceSpecificExitCode = entry.ServiceStatusProcess.dwServiceSpecificExitCode,
                                        CheckPoint = entry.ServiceStatusProcess.dwCheckPoint,
                                        WaitHint = entry.ServiceStatusProcess.dwWaitHint,
                                        ProcessId = entry.ServiceStatusProcess.dwProcessId,
                                    };

                                    info.DisplayName ??= info.ServiceName;
                                    results.Add(info);
                                    current = IntPtr.Add(current, structSize);
                                }

                                break; // Successfully enumerated
                            }

                            // Check if we need a larger buffer
                            int lastError = Marshal.GetLastWin32Error();
                            if (lastError == 122 /* ERROR_MORE_DATA */ || lastError == 0)
                            {
                                bufferSize = bytesNeeded > 0 ? bytesNeeded : bufferSize * 2;
                                Marshal.FreeHGlobal(buffer);
                                buffer = Marshal.AllocHGlobal((int)bufferSize);
                                continue;
                            }

                            break; // Unexpected error
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                // Return whatever we have so far
            }
            finally
            {
                SyncLock.Release();
            }

            return results;
        }

        /// <summary>
        /// Determines whether the specified service exists in the SCM database.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <returns><see langword="true"/> if the service exists; otherwise <see langword="false"/>.</returns>
        public static bool ServiceExists(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_QUERY_STATUS);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    SafeCloseHandle(serviceHandle);
                    return true;
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }

        /// <summary>
        /// Checks whether a service is currently running.
        /// </summary>
        /// <param name="serviceName">The internal name of the service.</param>
        /// <returns><see langword="true"/> if the service exists and is in the running state; otherwise <see langword="false"/>.</returns>
        public static bool IsServiceRunning(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                return false;
            }

            SyncLock.Wait();
            try
            {
                IntPtr scmHandle = OpenScm(AdvApi32.SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    IntPtr serviceHandle = OpenService(
                        scmHandle,
                        serviceName,
                        AdvApi32.SERVICE_QUERY_STATUS);

                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }

                    try
                    {
                        var status = default(SERVICE_STATUS);
                        if (!AdvApi32.QueryServiceStatus(serviceHandle, ref status))
                        {
                            return false;
                        }

                        return status.dwCurrentState == AdvApi32.SERVICE_RUNNING;
                    }
                    finally
                    {
                        SafeCloseHandle(serviceHandle);
                    }
                }
                finally
                {
                    SafeCloseHandle(scmHandle);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }
    }
}
