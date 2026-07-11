// <copyright file="RstrtMgr.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for rstrtmgr.dll — Windows Restart Manager API.
    /// </summary>
    internal static class RstrtMgr
    {
        [DllImport("rstrtmgr.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int RmStartSession(
            out uint pSessionHandle,
            uint dwSessionFlags,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder? strSessionKey);

        [DllImport("rstrtmgr.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int RmEndSession(uint dwSessionHandle);

        [DllImport("rstrtmgr.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int RmRegisterResources(
            uint dwSessionHandle,
            uint nFiles,
            string[]? rgsFileNames,
            uint nApplications,
            [In] RM_UNIQUE_PROCESS[]? rgApplications,
            uint nServices,
            string[]? rgsServiceNames);

        [DllImport("rstrtmgr.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int RmGetList(
            uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
            out uint lpdwRebootReasons);

        [DllImport("rstrtmgr.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int RmShutdown(
            uint dwSessionHandle,
            uint lActionFlags,
            RM_WRITE_STATUS_CALLBACK? fnStatus);

        [DllImport("rstrtmgr.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int RmRestart(
            uint dwSessionHandle,
            uint dwRestartFlags,
            RM_WRITE_STATUS_CALLBACK? fnStatus);

        // Delegates

        /// <summary>Callback for shutdown/restart progress.</summary>
        internal delegate void RM_WRITE_STATUS_CALLBACK(uint nPercentComplete);

        // Constants

        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_SEM_TIMEOUT = 121;
        internal const int ERROR_BAD_ARGUMENTS = 160;
        internal const int ERROR_WRITE_FAULT = 29;
        internal const int ERROR_CANCELLED = 1223;
        internal const uint RmForceShutdown = 0x01;
        internal const uint RmShutdownOnlyRegistered = 0x10;

        // Reboot reasons
        internal const uint RmRebootReasonNone = 0x00;
        internal const uint RmRebootReasonDetectedSelf = 0x01;
        internal const uint RmRebootReasonCriticalProcess = 0x02;
        internal const uint RmRebootReasonActiveSession = 0x04;
        internal const uint RmRebootReasonCriticalService = 0x08;
        internal const uint RmRebootReasonNoMoreCritical = 0x10;
        internal const uint RmRebootReasonPermissionDenied = 0x20;
        internal const uint RmRebootReasonSessionMismatch = 0x40;
        internal const uint RmRebootReasonCriticalProcessNeeded = 0x80;
    }

    /// <summary>
    /// Uniquely identifies a process by its PID and start time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public long processStartTime; // FILETIME
    }

    /// <summary>
    /// Information about a process affected by a Restart Manager operation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string strServiceShortName;
        public uint ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }
}
