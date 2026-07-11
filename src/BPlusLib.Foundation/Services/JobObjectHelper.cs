// <copyright file="JobObjectHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Services
{
    using JOBOBJECT_BASIC_LIMIT_INFORMATION = Kernel32.JOBOBJECT_BASIC_LIMIT_INFORMATION;
    using JOBOBJECT_EXTENDED_LIMIT_INFORMATION = Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION;
    using IO_COUNTERS = Kernel32.IO_COUNTERS;

    /// <summary>
    /// Provides process group management via Windows Job Objects.
    /// Wraps CreateJobObject, AssignProcessToJobObject, SetInformationJobObject,
    /// QueryInformationJobObject, and TerminateJobObject.
    /// Thread-safe (each instance has its own lock).
    /// </summary>
    public sealed class JobObjectHelper : IDisposable
    {
        private readonly IntPtr _jobHandle;
        private readonly string? _name;
        private bool _disposed;
        private readonly object _lock = new();

        private const int JobObjectExtendedLimitInformation = 9;
        private const int JobObjectBasicLimitInformation = 2;

        /// <summary>
        /// Creates a new job object, optionally with a name.
        /// </summary>
        /// <param name="name">Optional name for the job object (can be used by other processes to open it).</param>
        /// <exception cref="Win32Exception">Thrown if the job object could not be created.</exception>
        public JobObjectHelper(string? name = null)
        {
            _name = name;
            _jobHandle = Kernel32.CreateJobObjectW(IntPtr.Zero, name);
            if (_jobHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>Gets the native handle of the job object.</summary>
        public IntPtr Handle => _jobHandle;

        /// <summary>Gets the optional name of the job object.</summary>
        public string? Name => _name;

        /// <summary>
        /// Assigns a process to this job object.
        /// </summary>
        /// <param name="processHandle">Handle to the process with PROCESS_SET_QUOTA or PROCESS_TERMINATE access.</param>
        /// <returns>True if the process was assigned successfully.</returns>
        public bool AssignProcess(IntPtr processHandle)
        {
            if (_disposed || processHandle == IntPtr.Zero) return false;
            lock (_lock)
            {
                return Kernel32.AssignProcessToJobObject(_jobHandle, processHandle);
            }
        }

        /// <summary>
        /// Assigns a process by its PID to this job object.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <returns>True if the process was assigned successfully.</returns>
        public bool AssignProcessById(int processId)
        {
            if (_disposed || processId <= 0) return false;
            IntPtr hProcess = Kernel32.OpenProcess(
                Kernel32.ProcessCreateProcess | Kernel32.ProcessQueryInformation,
                false, processId);
            if (hProcess == IntPtr.Zero) return false;
            try
            {
                return AssignProcess(hProcess);
            }
            finally
            {
                Kernel32.CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// Sets the KILL_ON_JOB_CLOSE flag. When enabled, all processes in the job
        /// are terminated when the last handle to the job is closed.
        /// </summary>
        public bool SetKillOnClose(bool enabled)
        {
            return SetLimitFlags(
                enabled ? Kernel32.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE : 0u,
                enabled);
        }

        /// <summary>
        /// Sets the maximum number of active processes allowed in the job.
        /// </summary>
        /// <param name="maxProcesses">Maximum concurrent processes (0 = no limit).</param>
        public bool SetActiveProcessLimit(uint maxProcesses)
        {
            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.ActiveProcessLimit = maxProcesses;
            info.BasicLimitInformation.LimitFlags = Kernel32.JOB_OBJECT_LIMIT_ACTIVE_PROCESS;
            return SetExtendedInfo(ref info);
        }

        /// <summary>
        /// Terminates all processes in the job object.
        /// </summary>
        /// <param name="exitCode">The exit code for the terminated processes.</param>
        /// <returns>True if all processes were terminated.</returns>
        public bool Terminate(uint exitCode = 0)
        {
            if (_disposed) return false;
            lock (_lock)
            {
                return Kernel32.TerminateJobObject(_jobHandle, exitCode);
            }
        }

        /// <summary>
        /// Checks whether the current process is already in a job.
        /// </summary>
        public static bool IsCurrentProcessInJob()
        {
            return Kernel32.IsProcessInJob(
                Kernel32.GetCurrentProcess(),
                IntPtr.Zero,
                out bool result) && result;
        }

        private bool SetLimitFlags(uint flags, bool add)
        {
            if (_disposed) return false;
            // Read current limits first
            var info = default(JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
            int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                bool ok = Kernel32.QueryInformationJobObject(
                    _jobHandle, JobObjectExtendedLimitInformation,
                    ptr, (uint)size, out _);
                if (ok)
                    info = Marshal.PtrToStructure<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(ptr);
                else
                    info = default;

                if (add)
                    info.BasicLimitInformation.LimitFlags |= flags;
                else
                    info.BasicLimitInformation.LimitFlags &= ~flags;

                return SetExtendedInfo(ref info);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private bool SetExtendedInfo(ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info)
        {
            if (_disposed) return false;
            lock (_lock)
            {
                int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(info, ptr, false);
                    return Kernel32.SetInformationJobObject(
                        _jobHandle, JobObjectExtendedLimitInformation,
                        ptr, (uint)size);
                }
                finally
                {
                    Marshal.DestroyStructure<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(ptr);
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        /// <summary>Releases the job object handle. All processes remain associated but
        /// KILL_ON_JOB_CLOSE fires if set.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Kernel32.CloseHandle(_jobHandle);
            }
        }
    }
}
