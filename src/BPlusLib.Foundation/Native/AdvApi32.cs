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
    /// P/Invoke declarations for advapi32.dll — Service Control Manager,
    /// Windows Credential Manager, token/privilege operations, and security
    /// descriptor manipulation.
    /// </summary>
    /// <remarks>
    /// Some declarations in this class overlap with private declarations in
    /// <c>SecurityHelper</c>, <c>TokenHelper</c>, <c>PrivilegeHelper</c>,
    /// <c>IntegrityHelper</c>, <c>ExplorerHelper</c>, and <c>ProcessExtensions</c>.
    /// Those private declarations shadow this class — no conflict at compile time.
    /// New code should use <see cref="AdvApi32"/> exclusively.
    /// </remarks>
    internal static class AdvApi32
    {
        // =================================================================
        // Access masks — Service Control Manager
        // =================================================================

        /// <summary>SC_MANAGER_ALL_ACCESS (0xF003F).</summary>
        internal const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
        /// <summary>SC_MANAGER_CONNECT (0x0001).</summary>
        internal const uint SC_MANAGER_CONNECT = 0x0001;
        /// <summary>SC_MANAGER_CREATE_SERVICE (0x0002).</summary>
        internal const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        /// <summary>SC_MANAGER_ENUMERATE_SERVICE (0x0004).</summary>
        internal const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
        /// <summary>SC_MANAGER_LOCK (0x0008).</summary>
        internal const uint SC_MANAGER_LOCK = 0x0008;
        /// <summary>SC_MANAGER_QUERY_LOCK_STATUS (0x0010).</summary>
        internal const uint SC_MANAGER_QUERY_LOCK_STATUS = 0x0010;
        /// <summary>SC_MANAGER_MODIFY_BOOT_CONFIG (0x0020).</summary>
        internal const uint SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020;

        /// <summary>SERVICE_ALL_ACCESS (0xF01FF).</summary>
        internal const uint SERVICE_ALL_ACCESS = 0xF01FF;
        /// <summary>SERVICE_QUERY_CONFIG (0x00001).</summary>
        internal const uint SERVICE_QUERY_CONFIG = 0x00001;
        /// <summary>SERVICE_CHANGE_CONFIG (0x10000).</summary>
        internal const uint SERVICE_CHANGE_CONFIG = 0x10000;
        /// <summary>SERVICE_QUERY_STATUS (0x00004).</summary>
        internal const uint SERVICE_QUERY_STATUS = 0x00004;
        /// <summary>SERVICE_ENUMERATE_DEPENDENTS (0x00008).</summary>
        internal const uint SERVICE_ENUMERATE_DEPENDENTS = 0x00008;
        /// <summary>SERVICE_START (0x00010).</summary>
        internal const uint SERVICE_START = 0x00010;
        /// <summary>SERVICE_STOP (0x00020).</summary>
        internal const uint SERVICE_STOP = 0x00020;
        /// <summary>SERVICE_PAUSE_CONTINUE (0x00040).</summary>
        internal const uint SERVICE_PAUSE_CONTINUE = 0x00040;
        /// <summary>SERVICE_INTERROGATE (0x00080).</summary>
        internal const uint SERVICE_INTERROGATE = 0x00080;
        /// <summary>SERVICE_CREATE (0x00002).</summary>
        internal const uint SERVICE_CREATE = 0x00002;

        // =================================================================
        // Service start types
        // =================================================================

        /// <summary>SERVICE_BOOT_START (0x00).</summary>
        internal const uint SERVICE_BOOT_START = 0x00;
        /// <summary>SERVICE_SYSTEM_START (0x01).</summary>
        internal const uint SERVICE_SYSTEM_START = 0x01;
        /// <summary>SERVICE_AUTO_START (0x02).</summary>
        internal const uint SERVICE_AUTO_START = 0x02;
        /// <summary>SERVICE_DEMAND_START (0x03).</summary>
        internal const uint SERVICE_DEMAND_START = 0x03;
        /// <summary>SERVICE_DISABLED (0x04).</summary>
        internal const uint SERVICE_DISABLED = 0x04;

        // =================================================================
        // Service error control
        // =================================================================

        /// <summary>SERVICE_ERROR_IGNORE (0x00).</summary>
        internal const uint SERVICE_ERROR_IGNORE = 0x00;
        /// <summary>SERVICE_ERROR_NORMAL (0x01).</summary>
        internal const uint SERVICE_ERROR_NORMAL = 0x01;
        /// <summary>SERVICE_ERROR_SEVERE (0x02).</summary>
        internal const uint SERVICE_ERROR_SEVERE = 0x02;
        /// <summary>SERVICE_ERROR_CRITICAL (0x03).</summary>
        internal const uint SERVICE_ERROR_CRITICAL = 0x03;

        // =================================================================
        // Service types
        // =================================================================

        /// <summary>SERVICE_WIN32_OWN_PROCESS (0x10).</summary>
        internal const uint SERVICE_WIN32_OWN_PROCESS = 0x10;
        /// <summary>SERVICE_WIN32_SHARE_PROCESS (0x20).</summary>
        internal const uint SERVICE_WIN32_SHARE_PROCESS = 0x20;
        /// <summary>SERVICE_WIN32 = SERVICE_WIN32_OWN_PROCESS | SERVICE_WIN32_SHARE_PROCESS (0x30).</summary>
        internal const uint SERVICE_WIN32 = 0x30;
        /// <summary>SERVICE_KERNEL_DRIVER (0x01).</summary>
        internal const uint SERVICE_KERNEL_DRIVER = 0x01;
        /// <summary>SERVICE_FILE_SYSTEM_DRIVER (0x02).</summary>
        internal const uint SERVICE_FILE_SYSTEM_DRIVER = 0x02;
        /// <summary>SERVICE_DRIVER = SERVICE_KERNEL_DRIVER | SERVICE_FILE_SYSTEM_DRIVER (0x0B).</summary>
        internal const uint SERVICE_DRIVER = 0x0B;
        /// <summary>SERVICE_INTERACTIVE_PROCESS (0x100).</summary>
        internal const uint SERVICE_INTERACTIVE_PROCESS = 0x100;

        // =================================================================
        // Service states (SERVICE_STATUS.dwCurrentState)
        // =================================================================

        /// <summary>SERVICE_STOPPED (0x01).</summary>
        internal const uint SERVICE_STOPPED = 0x01;
        /// <summary>SERVICE_START_PENDING (0x02).</summary>
        internal const uint SERVICE_START_PENDING = 0x02;
        /// <summary>SERVICE_STOP_PENDING (0x03).</summary>
        internal const uint SERVICE_STOP_PENDING = 0x03;
        /// <summary>SERVICE_RUNNING (0x04).</summary>
        internal const uint SERVICE_RUNNING = 0x04;
        /// <summary>SERVICE_CONTINUE_PENDING (0x05).</summary>
        internal const uint SERVICE_CONTINUE_PENDING = 0x05;
        /// <summary>SERVICE_PAUSE_PENDING (0x06).</summary>
        internal const uint SERVICE_PAUSE_PENDING = 0x06;
        /// <summary>SERVICE_PAUSED (0x07).</summary>
        internal const uint SERVICE_PAUSED = 0x07;

        /// <summary>SERVICE_STATE_ALL (0x03) — enumerates all states.</summary>
        internal const uint SERVICE_STATE_ALL = 0x03;

        // =================================================================
        // Service controls accepted flags
        // =================================================================

        /// <summary>SERVICE_ACCEPT_STOP (0x01).</summary>
        internal const uint SERVICE_ACCEPT_STOP = 0x01;
        /// <summary>SERVICE_ACCEPT_PAUSE_CONTINUE (0x02).</summary>
        internal const uint SERVICE_ACCEPT_PAUSE_CONTINUE = 0x02;
        /// <summary>SERVICE_ACCEPT_SHUTDOWN (0x04).</summary>
        internal const uint SERVICE_ACCEPT_SHUTDOWN = 0x04;
        /// <summary>SERVICE_ACCEPT_PARAMCHANGE (0x08).</summary>
        internal const uint SERVICE_ACCEPT_PARAMCHANGE = 0x08;
        /// <summary>SERVICE_ACCEPT_NETBINDCHANGE (0x10).</summary>
        internal const uint SERVICE_ACCEPT_NETBINDCHANGE = 0x10;
        /// <summary>SERVICE_ACCEPT_HARDWAREPROFILECHANGE (0x20).</summary>
        internal const uint SERVICE_ACCEPT_HARDWAREPROFILECHANGE = 0x20;
        /// <summary>SERVICE_ACCEPT_POWEREVENT (0x40).</summary>
        internal const uint SERVICE_ACCEPT_POWEREVENT = 0x40;
        /// <summary>SERVICE_ACCEPT_SESSIONCHANGE (0x80).</summary>
        internal const uint SERVICE_ACCEPT_SESSIONCHANGE = 0x80;
        /// <summary>SERVICE_ACCEPT_PRESHUTDOWN (0x100).</summary>
        internal const uint SERVICE_ACCEPT_PRESHUTDOWN = 0x100;
        /// <summary>SERVICE_ACCEPT_TIMECHANGE (0x200).</summary>
        internal const uint SERVICE_ACCEPT_TIMECHANGE = 0x200;
        /// <summary>SERVICE_ACCEPT_TRIGGEREVENT (0x400).</summary>
        internal const uint SERVICE_ACCEPT_TRIGGEREVENT = 0x400;
        /// <summary>SERVICE_ACCEPT_USER_LOGOFF (0x800).</summary>
        internal const uint SERVICE_ACCEPT_USER_LOGOFF = 0x800;

        // =================================================================
        // Service control codes
        // =================================================================

        /// <summary>SERVICE_CONTROL_STOP (0x01).</summary>
        internal const uint SERVICE_CONTROL_STOP = 0x01;
        /// <summary>SERVICE_CONTROL_PAUSE (0x02).</summary>
        internal const uint SERVICE_CONTROL_PAUSE = 0x02;
        /// <summary>SERVICE_CONTROL_CONTINUE (0x03).</summary>
        internal const uint SERVICE_CONTROL_CONTINUE = 0x03;
        /// <summary>SERVICE_CONTROL_INTERROGATE (0x04).</summary>
        internal const uint SERVICE_CONTROL_INTERROGATE = 0x04;
        /// <summary>SERVICE_CONTROL_PARAMCHANGE (0x06).</summary>
        internal const uint SERVICE_CONTROL_PARAMCHANGE = 0x06;
        /// <summary>SERVICE_CONTROL_NETBINDADD (0x07).</summary>
        internal const uint SERVICE_CONTROL_NETBINDADD = 0x07;
        /// <summary>SERVICE_CONTROL_NETBINDREMOVE (0x08).</summary>
        internal const uint SERVICE_CONTROL_NETBINDREMOVE = 0x08;
        /// <summary>SERVICE_CONTROL_NETBINDENABLE (0x09).</summary>
        internal const uint SERVICE_CONTROL_NETBINDENABLE = 0x09;
        /// <summary>SERVICE_CONTROL_NETBINDDISABLE (0x0A).</summary>
        internal const uint SERVICE_CONTROL_NETBINDDISABLE = 0x0A;
        /// <summary>SERVICE_CONTROL_POWEREVENT (0x0D).</summary>
        internal const uint SERVICE_CONTROL_POWEREVENT = 0x0D;
        /// <summary>SERVICE_CONTROL_SESSIONCHANGE (0x0E).</summary>
        internal const uint SERVICE_CONTROL_SESSIONCHANGE = 0x0E;
        /// <summary>SERVICE_CONTROL_PRESHUTDOWN (0x0F).</summary>
        internal const uint SERVICE_CONTROL_PRESHUTDOWN = 0x0F;

        /// <summary>SC_ENUM_PROCESS_INFO (0) — info level for EnumServicesStatusExW.</summary>
        internal const uint SC_ENUM_PROCESS_INFO = 0;

        // =================================================================
        // Credential Manager constants
        // =================================================================

        /// <summary>CRED_TYPE_GENERIC (1).</summary>
        internal const uint CRED_TYPE_GENERIC = 1;
        /// <summary>CRED_TYPE_DOMAIN_PASSWORD (2).</summary>
        internal const uint CRED_TYPE_DOMAIN_PASSWORD = 2;
        /// <summary>CRED_TYPE_DOMAIN_CERTIFICATE (3).</summary>
        internal const uint CRED_TYPE_DOMAIN_CERTIFICATE = 3;
        /// <summary>CRED_TYPE_DOMAIN_VISIBLE_PASSWORD (4).</summary>
        internal const uint CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4;
        /// <summary>CRED_TYPE_GENERIC_CERTIFICATE (5).</summary>
        internal const uint CRED_TYPE_GENERIC_CERTIFICATE = 5;
        /// <summary>CRED_TYPE_DOMAIN_EXTENDED (6).</summary>
        internal const uint CRED_TYPE_DOMAIN_EXTENDED = 6;

        /// <summary>CRED_PERSIST_NONE (0) — credential does not persist.</summary>
        internal const uint CRED_PERSIST_NONE = 0;
        /// <summary>CRED_PERSIST_SESSION (1) — credential persists for this logon session.</summary>
        internal const uint CRED_PERSIST_SESSION = 1;
        /// <summary>CRED_PERSIST_LOCAL_MACHINE (2) — credential persists on this machine.</summary>
        internal const uint CRED_PERSIST_LOCAL_MACHINE = 2;
        /// <summary>CRED_PERSIST_ENTERPRISE (3) — credential persists across the enterprise.</summary>
        internal const uint CRED_PERSIST_ENTERPRISE = 3;

        /// <summary>CRED_FLAGS_USERNAME_TARGET (0x01).</summary>
        internal const uint CRED_FLAGS_USERNAME_TARGET = 0x01;

        /// <summary>CRED_ENUMERATE_ALL_CREDENTIALS (0x01).</summary>
        internal const uint CRED_ENUMERATE_ALL_CREDENTIALS = 0x01;

        // =================================================================
        // Token / Security constants
        // =================================================================

        /// <summary>TOKEN_QUERY (0x0008).</summary>
        internal const uint TOKEN_QUERY = 0x0008;
        /// <summary>TOKEN_ADJUST_PRIVILEGES (0x0020).</summary>
        internal const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        /// <summary>TOKEN_ALL_ACCESS (0xF01FF).</summary>
        internal const uint TOKEN_ALL_ACCESS = 0xF01FF;
        /// <summary>TOKEN_DUPLICATE (0x0002).</summary>
        internal const uint TOKEN_DUPLICATE = 0x0002;
        /// <summary>TOKEN_IMPERSONATE (0x0004).</summary>
        internal const uint TOKEN_IMPERSONATE = 0x0004;
        /// <summary>TOKEN_ASSIGN_PRIMARY (0x0001).</summary>
        internal const uint TOKEN_ASSIGN_PRIMARY = 0x0001;

        /// <summary>TokenElevation (20) — Token information class for elevation state.</summary>
        internal const int TokenElevation = 20;
        /// <summary>TokenLinkedToken (19) — Token information class for linked token.</summary>
        internal const int TokenLinkedToken = 19;
        /// <summary>TokenStatistics (10) — Token information class for statistics.</summary>
        internal const int TokenStatistics = 10;
        /// <summary>TokenIntegrityLevel (25) — Token information class for integrity level SID.</summary>
        internal const int TokenIntegrityLevel = 25;

        /// <summary>SE_PRIVILEGE_ENABLED (0x02).</summary>
        internal const uint SE_PRIVILEGE_ENABLED = 0x02;

        /// <summary>SecurityAnonymous (0) — impersonation level.</summary>
        internal const uint SecurityAnonymous = 0;
        /// <summary>SecurityIdentification (1).</summary>
        internal const uint SecurityIdentification = 1;
        /// <summary>SecurityImpersonation (2).</summary>
        internal const uint SecurityImpersonation = 2;
        /// <summary>SecurityDelegation (3).</summary>
        internal const uint SecurityDelegation = 3;

        /// <summary>TokenPrimary (1).</summary>
        internal const uint TokenPrimary = 1;
        /// <summary>TokenImpersonation (2).</summary>
        internal const uint TokenImpersonation = 2;

        /// <summary>SE_SHUTDOWN_NAME privilege.</summary>
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        /// <summary>SE_DEBUG_NAME privilege.</summary>
        internal const string SE_DEBUG_NAME = "SeDebugPrivilege";
        /// <summary>SE_BACKUP_NAME privilege.</summary>
        internal const string SE_BACKUP_NAME = "SeBackupPrivilege";
        /// <summary>SE_RESTORE_NAME privilege.</summary>
        internal const string SE_RESTORE_NAME = "SeRestorePrivilege";
        /// <summary>SE_TAKE_OWNERSHIP_NAME privilege.</summary>
        internal const string SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
        /// <summary>SE_LOAD_DRIVER_NAME privilege.</summary>
        internal const string SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";

        // =================================================================
        // P/Invoke — Service Control Manager
        // =================================================================

        /// <summary>
        /// Opens the service control manager database on the specified machine.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenSCManagerW(
            string? machineName,
            string? databaseName,
            uint desiredAccess);

        /// <summary>
        /// Opens an existing service object.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenServiceW(
            IntPtr hSCManager,
            string lpServiceName,
            uint dwDesiredAccess);

        /// <summary>
        /// Creates a new service object and adds it to the SCM database.
        /// </summary>
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

        /// <summary>
        /// Closes a handle to the service control manager or a service object.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseServiceHandle(IntPtr hSCObject);

        /// <summary>
        /// Starts a service, optionally passing arguments.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool StartServiceW(
            IntPtr hService,
            uint dwNumServiceArgs,
            string?[]? lpServiceArgVectors);

        /// <summary>
        /// Sends a control code to a service.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ControlService(
            IntPtr hService,
            uint dwControl,
            ref SERVICE_STATUS lpServiceStatus);

        /// <summary>
        /// Queries the current status of a service.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceStatus(
            IntPtr hService,
            ref SERVICE_STATUS lpServiceStatus);

        /// <summary>
        /// Queries service configuration parameters.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceConfigW(
            IntPtr hService,
            IntPtr lpServiceConfig,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        /// <summary>
        /// Queries extended service status (includes process ID).
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceStatusEx(
            IntPtr hService,
            uint infoLevel,
            IntPtr lpBuffer,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        /// <summary>
        /// Changes the configuration parameters of a service.
        /// </summary>
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

        /// <summary>
        /// Marks a service for deletion from the SCM database.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteService(IntPtr hService);

        /// <summary>
        /// Enumerates services in the specified SCM database.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumServicesStatusExW(
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

        /// <summary>
        /// Reads a stored credential from the Credential Manager vault.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredReadW(
            string targetName,
            uint type,
            uint flags,
            out IntPtr credential);

        /// <summary>
        /// Writes (creates or updates) a credential in the Credential Manager vault.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredWriteW(
            ref CREDENTIALW credential,
            uint flags);

        /// <summary>
        /// Enumerates credentials in the Credential Manager vault.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredEnumerateW(
            string? filter,
            uint flags,
            out int count,
            out IntPtr credentials);

        /// <summary>
        /// Deletes a credential from the Credential Manager vault.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredDeleteW(
            string targetName,
            uint type,
            uint flags);

        /// <summary>
        /// Frees memory allocated by CredReadW / CredEnumerateW.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredFree(IntPtr buffer);

        // =================================================================
        // P/Invoke — Token / Privilege management
        // =================================================================

        /// <summary>
        /// Opens the access token for a process.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess,
            out IntPtr tokenHandle);

        /// <summary>
        /// Retrieves a specified type of information about an access token.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            int tokenInformationClass,
            IntPtr tokenInformation,
            uint tokenInformationLength,
            out uint returnLength);

        /// <summary>
        /// Retrieves the LUID for a specified privilege name.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValueW(
            string? lpSystemName,
            string lpName,
            out long lpLuid);

        /// <summary>
        /// Enables or disables privileges in the specified access token.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TOKEN_PRIVILEGES newState,
            uint bufferLength,
            IntPtr previousState,
            out uint returnLength);

        /// <summary>
        /// Creates a duplicate of a token handle.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateTokenEx(
            IntPtr existingTokenHandle,
            uint desiredAccess,
            IntPtr tokenAttributes,
            uint impersonationLevel,
            uint tokenType,
            out IntPtr duplicateTokenHandle);

        /// <summary>
        /// Specifies whether a token can be used for impersonation.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetThreadToken(
            IntPtr? threadHandle,
            IntPtr tokenHandle);

        /// <summary>
        /// Opens the thread or process token with the specified access.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenThreadToken(
            IntPtr threadHandle,
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool openAsSelf,
            out IntPtr tokenHandle);

        // =================================================================
        // P/Invoke — SID / Security Descriptor
        // =================================================================

        /// <summary>
        /// Converts a binary SID to a string SID.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertSidToStringSidW(
            IntPtr sid,
            out IntPtr stringSid);

        /// <summary>
        /// Returns the length of a SID in bytes.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int GetLengthSid(IntPtr sid);

        /// <summary>
        /// Converts a string SID to a binary SID.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertStringSidToSidW(
            string stringSid,
            out IntPtr sid);

        /// <summary>
        /// Allocates and initializes a SID from well-known SID identifiers.
        /// The caller must free the SID with FreeSid.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern uint AllocateAndInitializeSid(
            IntPtr pIdentifierAuthority,
            byte nSubAuthorityCount,
            uint dwSubAuthority0,
            uint dwSubAuthority1,
            uint dwSubAuthority2,
            uint dwSubAuthority3,
            uint dwSubAuthority4,
            uint dwSubAuthority5,
            uint dwSubAuthority6,
            uint dwSubAuthority7,
            out IntPtr pSid);

        /// <summary>
        /// Frees a SID allocated by AllocateAndInitializeSid.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern IntPtr FreeSid(IntPtr pSid);

        /// <summary>
        /// Retrieves the security descriptor for a specified object.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetNamedSecurityInfoW(
            string pObjectName,
            uint objectType,
            uint securityInfo,
            out IntPtr pSidOwner,
            out IntPtr pSidGroup,
            out IntPtr pDacl,
            out IntPtr pSacl,
            out IntPtr pSecurityDescriptor);

        // =================================================================
        // P/Invoke — Shutdown / Power
        // =================================================================

        /// <summary>
        /// Initiates a system shutdown.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InitiateSystemShutdownExW(
            string? lpMachineName,
            string? lpMessage,
            uint dwTimeout,
            [MarshalAs(UnmanagedType.Bool)] bool bForceAppsClosed,
            [MarshalAs(UnmanagedType.Bool)] bool bRebootAfterShutdown,
            uint dwReason);

        /// <summary>
        /// Aborts a system shutdown initiated by InitiateSystemShutdownExW.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AbortSystemShutdownW(
            string? lpMachineName);

        // =================================================================
        // P/Invoke — LSA Policy (lookup privilege display name)
        // =================================================================

        /// <summary>
        /// Retrieves the display name for a privilege value.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeDisplayNameW(
            string? lpSystemName,
            string lpName,
            StringBuilder lpDisplayName,
            ref uint cchDisplayName,
            out uint lpLangId);

        // =================================================================
        // Kernel32 forwarders (commonly paired with AdvApi32 calls)
        // =================================================================

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr hMem);
    }

    // =====================================================================
    // Structures — Service Control Manager
    // =====================================================================

    /// <summary>
    /// Contains status information for a service (SERVICE_STATUS).
    /// </summary>
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

    /// <summary>
    /// Contains configuration parameters for a service (QUERY_SERVICE_CONFIGW).
    /// </summary>
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

    /// <summary>
    /// Contains process-specific extended status information for a service.
    /// </summary>
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

    /// <summary>
    /// Contains the service name and extended status for enumeration results.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ENUM_SERVICE_STATUS_PROCESSW
    {
        public IntPtr lpServiceName;
        public IntPtr lpDisplayName;
        public SERVICE_STATUS_PROCESS ServiceStatusProcess;
    }

    // =====================================================================
    // Structures — Credential Manager
    // =====================================================================

    /// <summary>
    /// Describes a credential stored in the Credential Manager (CREDENTIALW).
    /// </summary>
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

    // =====================================================================
    // Structures — Token / Privilege
    // =====================================================================

    /// <summary>
    /// An LUID (Locally Unique Identifier) for a privilege.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    /// <summary>
    /// An LUID and its attributes (enabled/disabled).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    /// <summary>
    /// Specifies a set of privileges and their attributes.
    /// For a single privilege, use this directly.
    /// For multiple, use Marshal.OffsetOf to index the array.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
        // Followed by variable-length array of LUID_AND_ATTRIBUTES
    }

    /// <summary>
    /// Indicates whether a token is elevated (TOKEN_ELEVATION).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_ELEVATION
    {
        public int TokenIsElevated;
    }

    /// <summary>
    /// Contains token statistics (TOKEN_STATISTICS).
    /// </summary>
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

    /// <summary>
    /// Represents the SID identifier authority (SID_IDENTIFIER_AUTHORITY).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 6)]
    internal struct SID_IDENTIFIER_AUTHORITY
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] Value;
    }

    /// <summary>
    /// Well-known SID string constants.
    /// </summary>
    internal static class WellKnownSids
    {
        /// <summary>S-1-1-0 — Everyone.</summary>
        internal const string Everyone = "S-1-1-0";
        /// <summary>S-1-5-18 — Local System.</summary>
        internal const string LocalSystem = "S-1-5-18";
        /// <summary>S-1-5-19 — Local Service.</summary>
        internal const string LocalService = "S-1-5-19";
        /// <summary>S-1-5-20 — Network Service.</summary>
        internal const string NetworkService = "S-1-5-20";
        /// <summary>S-1-5-32-544 — Administrators.</summary>
        internal const string Administrators = "S-1-5-32-544";
        /// <summary>S-1-5-32-545 — Users.</summary>
        internal const string Users = "S-1-5-32-545";
        /// <summary>S-1-16-0 — Untrusted integrity level.</summary>
        internal const string UntrustedIntegrity = "S-1-16-0";
        /// <summary>S-1-16-4096 — Low integrity level.</summary>
        internal const string LowIntegrity = "S-1-16-4096";
        /// <summary>S-1-16-8192 — Medium integrity level.</summary>
        internal const string MediumIntegrity = "S-1-16-8192";
        /// <summary>S-1-16-8448 — Medium-Plus integrity level.</summary>
        internal const string MediumPlusIntegrity = "S-1-16-8448";
        /// <summary>S-1-16-12288 — High integrity level.</summary>
        internal const string HighIntegrity = "S-1-16-12288";
        /// <summary>S-1-16-16384 — System integrity level.</summary>
        internal const string SystemIntegrity = "S-1-16-16384";
        /// <summary>S-1-16-20480 — Protected process integrity level.</summary>
        internal const string ProtectedProcessIntegrity = "S-1-16-20480";
    }
}
