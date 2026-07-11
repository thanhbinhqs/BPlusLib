// <copyright file="RestartManagerHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Services
{
    /// <summary>
    /// Provides information about a process affected by the Restart Manager.
    /// </summary>
    public interface IRmProcessInfo
    {
        /// <summary>Gets the process ID.</summary>
        int ProcessId { get; }

        /// <summary>Gets the application name, if available.</summary>
        string? AppName { get; }

        /// <summary>Gets the service short name, if available.</summary>
        string? ServiceName { get; }

        /// <summary>Gets whether the process can be restarted.</summary>
        bool IsRestartable { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IRmProcessInfo"/>.
    /// </summary>
    internal sealed class RmProcessInfo : IRmProcessInfo
    {
        public int ProcessId { get; set; }
        public string? AppName { get; set; }
        public string? ServiceName { get; set; }
        public bool IsRestartable { get; set; }

        internal static RmProcessInfo FromNative(in RM_PROCESS_INFO native)
        {
            return new RmProcessInfo
            {
                ProcessId = native.Process.dwProcessId,
                AppName = string.IsNullOrEmpty(native.strAppName) ? null : native.strAppName,
                ServiceName = string.IsNullOrEmpty(native.strServiceShortName) ? null : native.strServiceShortName,
                IsRestartable = native.bRestartable,
            };
        }
    }

    /// <summary>
    /// Manages a Restart Manager session for shutting down and restarting applications
    /// that hold locks on specified files or processes.
    /// </summary>
    public sealed class RestartManagerSession : IDisposable
    {
        private uint _sessionHandle;
        private bool _disposed;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartManagerSession"/> class.
        /// Starts a new Restart Manager session.
        /// </summary>
        /// <exception cref="Win32Exception">Thrown if the session could not be started.</exception>
        public RestartManagerSession()
        {
            int result = RstrtMgr.RmStartSession(out _sessionHandle, 0, null);
            if (result != RstrtMgr.ERROR_SUCCESS)
            {
                throw new Win32Exception(result, "Failed to start Restart Manager session.");
            }
        }

        /// <summary>
        /// Registers one or more file paths with the Restart Manager session.
        /// </summary>
        /// <param name="filePaths">The file paths to register.</param>
        /// <returns>True if all files were registered successfully.</returns>
        public bool RegisterFiles(params string[] filePaths)
        {
            if (filePaths is null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }

            lock (_lock)
            {
                CheckDisposed();

                if (filePaths.Length == 0)
                {
                    return true;
                }

                int result = RstrtMgr.RmRegisterResources(
                    _sessionHandle,
                    (uint)filePaths.Length,
                    filePaths,
                    0,
                    null,
                    0,
                    null);

                return result == RstrtMgr.ERROR_SUCCESS;
            }
        }

        /// <summary>
        /// Registers one or more process IDs with the Restart Manager session.
        /// </summary>
        /// <param name="processIds">The process IDs to register.</param>
        /// <returns>True if all processes were registered successfully.</returns>
        public bool RegisterProcesses(params int[] processIds)
        {
            if (processIds is null)
            {
                throw new ArgumentNullException(nameof(processIds));
            }

            lock (_lock)
            {
                CheckDisposed();

                if (processIds.Length == 0)
                {
                    return true;
                }

                RM_UNIQUE_PROCESS[] processes = new RM_UNIQUE_PROCESS[processIds.Length];
                for (int i = 0; i < processIds.Length; i++)
                {
                    processes[i] = new RM_UNIQUE_PROCESS
                    {
                        dwProcessId = processIds[i],
                        processStartTime = 0, // let the RM resolve the start time
                    };
                }

                int result = RstrtMgr.RmRegisterResources(
                    _sessionHandle,
                    0,
                    null,
                    (uint)processes.Length,
                    processes,
                    0,
                    null);

                return result == RstrtMgr.ERROR_SUCCESS;
            }
        }

        /// <summary>
        /// Retrieves the list of processes that are currently affected (i.e., have locks
        /// on the registered resources).
        /// </summary>
        /// <returns>A list of <see cref="IRmProcessInfo"/> describing affected processes.</returns>
        public List<IRmProcessInfo> GetProcesses()
        {
            lock (_lock)
            {
                CheckDisposed();

                uint procInfoCount = 0;
                uint rebootReasons;

                // First call: get the required buffer size
                int result = RstrtMgr.RmGetList(
                    _sessionHandle,
                    out uint procInfoNeeded,
                    ref procInfoCount,
                    null,
                    out rebootReasons);

                if (result != RstrtMgr.ERROR_SUCCESS && result != 234 /* ERROR_MORE_DATA */)
                {
                    return new List<IRmProcessInfo>();
                }

                if (procInfoNeeded == 0)
                {
                    return new List<IRmProcessInfo>();
                }

                // Second call: get the actual data
                procInfoCount = procInfoNeeded;
                RM_PROCESS_INFO[] procInfoArray = new RM_PROCESS_INFO[procInfoCount];

                result = RstrtMgr.RmGetList(
                    _sessionHandle,
                    out procInfoNeeded,
                    ref procInfoCount,
                    procInfoArray,
                    out rebootReasons);

                if (result != RstrtMgr.ERROR_SUCCESS)
                {
                    return new List<IRmProcessInfo>();
                }

                var list = new List<IRmProcessInfo>((int)procInfoCount);
                for (int i = 0; i < procInfoCount; i++)
                {
                    list.Add(RmProcessInfo.FromNative(procInfoArray[i]));
                }

                return list;
            }
        }

        /// <summary>
        /// Shuts down all processes that have locks on the registered resources.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000).</param>
        /// <returns>True if all processes were shut down successfully.</returns>
        public bool ShutdownProcesses(int timeoutMs = 30000)
        {
            lock (_lock)
            {
                CheckDisposed();

                uint flags = RstrtMgr.RmShutdownOnlyRegistered;

                int result = RstrtMgr.RmShutdown(_sessionHandle, flags, null);

                if (result == RstrtMgr.ERROR_SEM_TIMEOUT)
                {
                    // Retry with force shutdown if the first attempt timed out
                    result = RstrtMgr.RmShutdown(_sessionHandle, flags | RstrtMgr.RmForceShutdown, null);
                }

                return result == RstrtMgr.ERROR_SUCCESS;
            }
        }

        /// <summary>
        /// Restarts all processes that were shut down by the Restart Manager and are marked as restartable.
        /// </summary>
        /// <returns>True if all processes were restarted successfully.</returns>
        public bool RestartProcesses()
        {
            lock (_lock)
            {
                CheckDisposed();

                int result = RstrtMgr.RmRestart(_sessionHandle, 0, null);
                return result == RstrtMgr.ERROR_SUCCESS;
            }
        }

        /// <summary>
        /// Ends the Restart Manager session and releases all resources.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                if (_sessionHandle != 0)
                {
                    RstrtMgr.RmEndSession(_sessionHandle);
                    _sessionHandle = 0;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
