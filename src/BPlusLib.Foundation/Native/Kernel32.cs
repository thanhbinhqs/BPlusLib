// <copyright file="Kernel32.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for kernel32.dll — process, handle, and system-level operations.
    /// </summary>
    internal static class Kernel32
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary>PROCESS_CREATE_PROCESS (0x0080) — Required to create a process.</summary>
        internal const uint ProcessCreateProcess = 0x0080;

        /// <summary>PROCESS_DUP_HANDLE (0x0040) — Required to duplicate a handle.</summary>
        internal const uint ProcessDuplicateHandle = 0x0040;

        /// <summary>PROCESS_QUERY_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION.</summary>
        internal const uint ProcessQueryInformation = 0x0400 | 0x1000;

        /// <summary>DUPLICATE_SAME_ACCESS — Duplicate handle with same access.</summary>
        internal const uint DuplicateSameAccess = 0x00000002;

        /// <summary>FORMAT_MESSAGE_FROM_SYSTEM.</summary>
        internal const uint FormatMessageFromSystem = 0x00001000;

        /// <summary>FORMAT_MESSAGE_IGNORE_INSERTS.</summary>
        internal const uint FormatMessageIgnoreInserts = 0x00000200;

        /// <summary>Default buffer size for FormatMessageW.</summary>
        internal const int FormatMessageBufferSize = 512;

        /// <summary>Process name in Win32 format.</summary>
        internal const uint ProcessNameWin32 = 0;

        /// <summary>Process name in native (NT) format.</summary>
        internal const uint ProcessNameNative = 1;

        /// <summary>Maximum path length in characters (260).</summary>
        internal const int MaxPathChars = 260;

        /// <summary>Extended maximum path length (32767).</summary>
        internal const int ExtendedMaxPathChars = 32767;

        // =====================================================================
        // Process and handle management
        // =====================================================================

        /// <summary>
        /// Opens an existing process object.
        /// </summary>
        /// <param name="desiredAccess">The access mask for the process.</param>
        /// <param name="inheritHandle">If true, the handle is inheritable.</param>
        /// <param name="processId">The identifier of the process to open.</param>
        /// <returns>An open handle to the process, or IntPtr.Zero on failure.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            int processId);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">The handle to close.</param>
        /// <returns>True if the handle was closed successfully.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Duplicates a handle from one process to another.
        /// </summary>
        /// <param name="sourceProcessHandle">Handle to the source process.</param>
        /// <param name="sourceHandle">The handle to duplicate.</param>
        /// <param name="targetProcessHandle">Handle to the target process.</param>
        /// <param name="targetHandle">Receives the duplicated handle.</param>
        /// <param name="desiredAccess">The access mask for the new handle.</param>
        /// <param name="inheritHandle">If true, the new handle is inheritable.</param>
        /// <param name="options">Duplication options (DUPLICATE_* flags).</param>
        /// <returns>True if the handle was duplicated successfully.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(
            IntPtr sourceProcessHandle,
            IntPtr sourceHandle,
            IntPtr targetProcessHandle,
            out IntPtr targetHandle,
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            uint options);

        /// <summary>Returns a pseudo-handle for the current process.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>Returns the process ID of the calling process.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetCurrentProcessId();

        /// <summary>Returns the thread ID of the calling thread.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetCurrentThreadId();

        // =====================================================================
        // Process information
        // =====================================================================

        /// <summary>
        /// Retrieves the full image path for the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryFullProcessImageName(
            IntPtr handle,
            uint dwFlags,
            StringBuilder lpExeName,
            ref uint lpdwSize);

        /// <summary>
        /// Retrieves the DOS device mapping for a device name.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint QueryDosDevice(
            string? lpDeviceName,
            StringBuilder lpTargetDevice,
            uint bufferLength);

        /// <summary>
        /// Retrieves timing information for the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessTimes(
            IntPtr handle,
            out long lpCreationTime,
            out long lpExitTime,
            out long lpKernelTime,
            out long lpUserTime);

        // =====================================================================
        // Error handling
        // =====================================================================

        /// <summary>Returns the last-error code for the calling thread.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern int GetLastError();

        /// <summary>
        /// Formats a message string from the system's message tables.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int FormatMessageW(
            uint dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            uint dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr arguments);

        // =====================================================================
        // Job Object constants
        // =====================================================================

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

        // =====================================================================
        // Job Object structures
        // =====================================================================

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

        // =====================================================================
        // Job Object P/Invoke
        // =====================================================================

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

        /// <summary>Sets the last-error code for the calling thread.</summary>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern void SetLastError(int dwErrorCode);

        // =================================================================
        // Console API
        // =================================================================

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

        [DllImport("kernel32.dll", ExactSpelling = true)]
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
            IntPtr consoleHandle, ushort attributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetConsoleMode(
            IntPtr consoleHandle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetConsoleMode(
            IntPtr consoleHandle, uint mode);

        /// <summary>Invalid handle sentinel value (-1).</summary>
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // =================================================================
        // Named Pipe API
        // =================================================================

        internal const uint PIPE_ACCESS_DUPLEX = 0x00000003;
        internal const uint PIPE_ACCESS_INBOUND = 0x00000001;
        internal const uint PIPE_ACCESS_OUTBOUND = 0x00000002;
        internal const uint PIPE_ACCESS_OVERLAPPED = 0x40000000; // flag, not exclusive

        internal const uint PIPE_TYPE_BYTE = 0x00000000;
        internal const uint PIPE_TYPE_MESSAGE = 0x00000004;
        internal const uint PIPE_READMODE_BYTE = 0x00000000;
        internal const uint PIPE_READMODE_MESSAGE = 0x00000002;
        internal const uint PIPE_WAIT = 0x00000000;
        internal const uint PIPE_NOWAIT = 0x00000001;
        internal const uint PIPE_UNLIMITED_INSTANCES = 255;

        internal const uint NMPWAIT_USE_DEFAULT_WAIT = 0x00000000;
        internal const uint NMPWAIT_NOWAIT = 0x00000001;
        internal const uint NMPWAIT_WAIT_FOREVER = 0xFFFFFFFF;

        internal const uint ERROR_PIPE_BUSY = 231;
        internal const uint ERROR_NO_DATA = 232;
        internal const uint ERROR_PIPE_NOT_CONNECTED = 233;
        internal const uint ERROR_BROKEN_PIPE = 109;
        internal const uint ERROR_PIPE_LISTENING = 536;
        internal const uint ERROR_PIPE_CONNECTED = 535;
        internal const uint ERROR_SEM_TIMEOUT = 121;

        // File API constants
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint OPEN_EXISTING = 3;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateNamedPipeW(
            string lpName,
            uint dwOpenMode,
            uint dwPipeMode,
            uint nMaxInstances,
            uint nOutBufferSize,
            uint nInBufferSize,
            uint nDefaultTimeOut,
            IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConnectNamedPipe(
            IntPtr hNamedPipe,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DisconnectNamedPipe(IntPtr hNamedPipe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CallNamedPipeW(
            string lpNamedPipeName,
            byte[]? lpInBuffer,
            uint nInBufferSize,
            [Out] byte[]? lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesRead,
            uint nTimeOut);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WaitNamedPipeW(
            string lpNamedPipeName,
            uint nTimeOut);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetNamedPipeHandleState(
            IntPtr hNamedPipe,
            ref uint lpMode,
            IntPtr lpMaxCollectionCount,
            IntPtr lpCollectDataTimeout);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeHandleStateW(
            IntPtr hNamedPipe,
            out uint lpState,
            out uint lpCurInstances,
            out uint lpMaxCollectionCount,
            out uint lpCollectDataTimeout,
            StringBuilder lpUserName,
            uint nMaxUserNameSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImpersonateNamedPipeClient(IntPtr hNamedPipe);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RevertToSelf();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadFile(
            IntPtr hFile,
            [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlushFileBuffers(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        // =================================================================
        // Memory-Mapped File API
        // =================================================================

        internal const uint PAGE_READONLY = 0x02;
        internal const uint PAGE_READWRITE = 0x04;
        internal const uint PAGE_WRITECOPY = 0x08;
        internal const uint PAGE_EXECUTE_READ = 0x20;
        internal const uint PAGE_EXECUTE_READWRITE = 0x40;

        internal const uint FILE_MAP_WRITE = 0x0002;
        internal const uint FILE_MAP_READ = 0x0004;
        internal const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        internal const uint FILE_MAP_COPY = 0x0001;
        internal const uint FILE_MAP_EXECUTE = 0x0020;

        internal const uint SECTION_MAP_WRITE = 0x0002;
        internal const uint SECTION_MAP_READ = 0x0004;
        internal const uint SECTION_QUERY = 0x0001;

        internal const uint SEC_COMMIT = 0x80000000;
        internal const uint SEC_IMAGE = 0x1000000;
        internal const uint SEC_RESERVE = 0x4000000;
        internal const uint SEC_LARGE_PAGES = 0x80000000;

        internal const uint INVALID_FILE_SIZE = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateFileMappingW(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string? lpName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenFileMappingW(
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            IntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessWorkingSetSizeEx(
            IntPtr hProcess,
            out IntPtr lpMinimumWorkingSetSize,
            out IntPtr lpMaximumWorkingSetSize,
            out uint lpFlags);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORY_BASIC_INFORMATION64
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public long RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQuery(
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION64 lpBuffer,
            int dwLength);
    }
}
