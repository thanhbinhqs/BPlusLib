// <copyright file="ProcessExtensions.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Process
{
    /// <summary>
    /// Provides extension methods for <see cref="System.Diagnostics.Process"/>
    /// using pure P/Invoke (no WMI). All methods are thread-safe.
    /// </summary>
    /// <remarks>
    /// Every P/Invoke call is wrapped in <c>try/catch(DllNotFoundException)</c>
    /// so that these extensions degrade gracefully on non-Windows runtimes.
    /// </remarks>
    public static class ProcessExtensions
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const uint ProcessQueryInformation = 0x0400 | 0x1000;
        private const uint ProcessTerminate = 0x0001;
        private const uint ProcessVmRead = 0x0010;
        private const uint ProcessCreateProcess = 0x0080;
        private const uint ProcessSuspendResume = 0x0800;
        private const uint ProcessAllAccess = 0x001F0FFF;

        private const uint TokenQuery = 0x0008;
        private const int TokenElevation = 20;

        private const int ProcessBasicInformation = 0;
        private const int ProcessCommandLineInformation = 0x3C; // 60

        private const int SystemProcessInformation = 5;

        private const uint StatusSuccess = 0;
        private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);

        private const int MaxCommandLineChars = 32768;
        private const int MaxPathChars = 260;
        private const int PipeReadBufferSize = 4096;

        // ProcessParameters offset in PEB (based on pointer size)
        private static readonly int PebProcessParametersOffset =
            IntPtr.Size == 8 ? 0x20 : 0x10;

        // SYSTEM_PROCESS_INFORMATION field offsets
        private static readonly int SpiNextEntryOffset = 0;
        private static readonly int SpiUniqueProcessId =
            IntPtr.Size == 8 ? 0x50 : 0x44;
        private static readonly int SpiInheritedFromUniqueProcessId =
            IntPtr.Size == 8 ? 0x58 : 0x48;
        private static readonly int SpiImageNameLength =
            IntPtr.Size == 8 ? 0x38 : 0x38;
        private static readonly int SpiImageNameBuffer =
            IntPtr.Size == 8 ? 0x40 : 0x3C;

        // =====================================================================
        // Win32 structures (defined in-file per task requirements)
        // =====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_ELEVATION
        {
            public int TokenIsElevated;
        }

        // =====================================================================
        // P/Invoke declarations
        // =====================================================================

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageNameW(
            IntPtr hProcess,
            uint dwFlags,
            StringBuilder lpExeName,
            ref uint lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int WaitForSingleObject(
            IntPtr hHandle,
            int dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(
            IntPtr hProcess,
            int uExitCode);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            IntPtr processInformation,
            int processInformationLength,
            out int returnLength);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        private static extern int NtQuerySystemInformation(
            int informationClass,
            IntPtr buffer,
            int bufferSize,
            out int returnedLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess,
            out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            int tokenInformationClass,
            IntPtr tokenInformation,
            int tokenInformationLength,
            out int returnLength);

        // =====================================================================
        // GetParentProcessId
        // =====================================================================

        /// <summary>
        /// Gets the parent process ID of the specified process using
        /// <c>NtQueryInformationProcess</c> with <c>ProcessBasicInformation</c>.
        /// </summary>
        /// <param name="process">The process to examine.</param>
        /// <returns>
        /// The parent process ID, or <c>0</c> if the information cannot be retrieved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="process"/> is <see langword="null"/>.
        /// </exception>
        public static int GetParentProcessId(this System.Diagnostics.Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return GetParentProcessIdInternal(process.Id);
            }
            catch (DllNotFoundException)
            {
                return 0;
            }
            catch (EntryPointNotFoundException)
            {
                return 0;
            }
        }

        private static int GetParentProcessIdInternal(int processId)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(ProcessQueryInformation, false, processId);
                if (hProcess == IntPtr.Zero)
                    return 0;

                int pbiSize = Marshal.SizeOf<PROCESS_BASIC_INFORMATION>();
                IntPtr pbiPtr = Marshal.AllocHGlobal(pbiSize);
                try
                {
                    int status = NtQueryInformationProcess(
                        hProcess,
                        ProcessBasicInformation,
                        pbiPtr,
                        pbiSize,
                        out _);

                    if (status < 0)
                        return 0;

                    var pbi = Marshal.PtrToStructure<PROCESS_BASIC_INFORMATION>(pbiPtr);
                    return pbi.InheritedFromUniqueProcessId.ToInt32();
                }
                finally
                {
                    Marshal.FreeHGlobal(pbiPtr);
                }
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }

        // =====================================================================
        // GetCommandLine
        // =====================================================================

        /// <summary>
        /// Gets the full command line of the specified process by reading
        /// <c>PEB-&gt;ProcessParameters-&gt;CommandLine</c> via
        /// <c>ReadProcessMemory</c>.
        /// </summary>
        /// <param name="process">The target process.</param>
        /// <returns>
        /// The command-line string, or <see langword="null"/> if it cannot be
        /// retrieved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="process"/> is <see langword="null"/>.
        /// </exception>
        public static string? GetCommandLine(this System.Diagnostics.Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return GetCommandLineInternal(process.Id);
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
        }

        private static string? GetCommandLineInternal(int processId)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(
                    ProcessQueryInformation | ProcessVmRead,
                    false,
                    processId);
                if (hProcess == IntPtr.Zero)
                    return null;

                // 1. Get PEB address via NtQueryInformationProcess
                int pbiSize = Marshal.SizeOf<PROCESS_BASIC_INFORMATION>();
                IntPtr pbiPtr = Marshal.AllocHGlobal(pbiSize);
                try
                {
                    int status = NtQueryInformationProcess(
                        hProcess,
                        ProcessBasicInformation,
                        pbiPtr,
                        pbiSize,
                        out _);

                    if (status < 0)
                        return null;

                    var pbi = Marshal.PtrToStructure<PROCESS_BASIC_INFORMATION>(pbiPtr);
                    IntPtr pebAddress = pbi.PebBaseAddress;
                    if (pebAddress == IntPtr.Zero)
                        return null;

                    // 2. Read the ProcessParameters pointer from PEB
                    int ptrSize = IntPtr.Size;
                    byte[] processParamsBuf = new byte[ptrSize];
                    if (!ReadProcessMemory(
                        hProcess,
                        IntPtr.Add(pebAddress, PebProcessParametersOffset),
                        processParamsBuf,
                        ptrSize,
                        out int bytesRead))
                    {
                        return null;
                    }

                    if (bytesRead != ptrSize)
                        return null;

                    IntPtr processParametersPtr = IntPtr.Size == 8
                        ? new IntPtr(BitConverter.ToInt64(processParamsBuf, 0))
                        : new IntPtr(BitConverter.ToInt32(processParamsBuf, 0));

                    if (processParametersPtr == IntPtr.Zero)
                        return null;

                    // 3. Read the CommandLine UNICODE_STRING from ProcessParameters
                    //    CommandLine offset in RTL_USER_PROCESS_PARAMETERS:
                    //    - On x64: typically 0x70 (varies by Windows version)
                    //    - On x86: typically 0x40
                    //    We read a generous chunk and extract the UNICODE_STRING
                    int usOffset = IntPtr.Size == 8 ? 0x70 : 0x40;
                    int imagePathOffset = IntPtr.Size == 8 ? 0x60 : 0x38;

                    // CommandLine starts right after ImagePathName.
                    // ImagePathName is one UNICODE_STRING (2+2+ptr), then CommandLine.
                    // So: imagePathOffset + sizeof(UNICODE_STRING) = commandLineOffset
                    int unicodeStringSize = Marshal.SizeOf<UNICODE_STRING>();
                    int commandLineOffset = imagePathOffset + unicodeStringSize;

                    byte[] usBuf = new byte[unicodeStringSize];
                    if (!ReadProcessMemory(
                        hProcess,
                        IntPtr.Add(processParametersPtr, commandLineOffset),
                        usBuf,
                        unicodeStringSize,
                        out bytesRead))
                    {
                        return null;
                    }

                    if (bytesRead != unicodeStringSize)
                        return null;

                    var us = new UNICODE_STRING();
                    unsafe
                    {
                        fixed (byte* p = usBuf)
                        {
                            us.Length = (ushort)Marshal.ReadInt16((IntPtr)p);
                            us.MaximumLength = (ushort)Marshal.ReadInt16((IntPtr)(p + 2));
                            us.Buffer = IntPtr.Size == 8
                                ? Marshal.ReadIntPtr((IntPtr)(p + 8))
                                : Marshal.ReadIntPtr((IntPtr)(p + 4));
                        }
                    }

                    if (us.Buffer == IntPtr.Zero || us.Length == 0)
                        return null;

                    // 4. Read the actual command-line string
                    int charCount = us.Length / 2;
                    if (charCount <= 0 || charCount > MaxCommandLineChars)
                        return null;

                    byte[] strBuf = new byte[us.Length];
                    if (!ReadProcessMemory(
                        hProcess,
                        us.Buffer,
                        strBuf,
                        us.Length,
                        out bytesRead))
                    {
                        return null;
                    }

                    return Encoding.Unicode.GetString(strBuf, 0, bytesRead);
                }
                finally
                {
                    Marshal.FreeHGlobal(pbiPtr);
                }
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }

        // =====================================================================
        // IsElevated
        // =====================================================================

        /// <summary>
        /// Determines whether the specified process is running with elevated
        /// privileges (i.e., has a split token with UAC elevation).
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>
        /// <see langword="true"/> if the process is elevated;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="process"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsElevated(this System.Diagnostics.Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return IsElevatedInternal(process.Id);
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        private static bool IsElevatedInternal(int processId)
        {
            IntPtr hProcess = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            IntPtr elevationBuf = IntPtr.Zero;

            try
            {
                hProcess = OpenProcess(ProcessQueryInformation, false, processId);
                if (hProcess == IntPtr.Zero)
                    return false;

                if (!OpenProcessToken(hProcess, TokenQuery, out hToken))
                    return false;

                int elevationSize = Marshal.SizeOf<TOKEN_ELEVATION>();
                elevationBuf = Marshal.AllocHGlobal(elevationSize);

                if (!GetTokenInformation(
                    hToken,
                    TokenElevation,
                    elevationBuf,
                    elevationSize,
                    out int returnLength))
                {
                    return false;
                }

                if (returnLength < elevationSize)
                    return false;

                var elevation = Marshal.PtrToStructure<TOKEN_ELEVATION>(elevationBuf);
                return elevation.TokenIsElevated != 0;
            }
            finally
            {
                if (elevationBuf != IntPtr.Zero)
                    Marshal.FreeHGlobal(elevationBuf);
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }

        // =====================================================================
        // KillTree
        // =====================================================================

        /// <summary>
        /// Kills the process and all its descendants recursively (child processes
        /// first, then the parent). Uses <c>CloseMainWindow</c> for a graceful
        /// shutdown attempt, then falls back to <c>Kill</c> after a 2-second
        /// time-out.
        /// </summary>
        /// <param name="process">The root process to terminate.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="process"/> is <see langword="null"/>.
        /// </exception>
        public static void KillTree(this System.Diagnostics.Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                KillTreeInternal(process.Id);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private static void KillTreeInternal(int rootProcessId)
        {
            // 1. Enumerate all processes via NtQuerySystemInformation
            Dictionary<int, List<int>> parentChildMap = new Dictionary<int, List<int>>();
            List<int> allProcessIds = new List<int>();

            if (!TryEnumerateProcesses(out parentChildMap, out allProcessIds))
                return;

            // 2. Collect all descendant PIDs recursively
            var toKill = new List<int>();
            CollectDescendants(rootProcessId, parentChildMap, toKill, new HashSet<int>());

            // 3. Kill children first, then parent
            foreach (int pid in toKill)
            {
                if (pid == rootProcessId)
                    continue;

                TerminateProcessById(pid, graceful: true);
            }

            // 4. Kill the root process last
            TerminateProcessById(rootProcessId, graceful: true);
        }

        private static bool TryEnumerateProcesses(
            out Dictionary<int, List<int>> parentChildMap,
            out List<int> allProcessIds)
        {
            parentChildMap = new Dictionary<int, List<int>>();
            allProcessIds = new List<int>();

            int bufferSize = 256 * 1024; // Start with 256 KB
            IntPtr buffer = IntPtr.Zero;

            try
            {
                while (true)
                {
                    buffer = Marshal.AllocHGlobal(bufferSize);
                    int status = NtQuerySystemInformation(
                        SystemProcessInformation,
                        buffer,
                        bufferSize,
                        out int returnLength);

                    if (status >= 0)
                        break;

                    Marshal.FreeHGlobal(buffer);
                    buffer = IntPtr.Zero;

                    if (status == StatusInfoLengthMismatch || status == unchecked((int)0xC0000023))
                    {
                        // Buffer too small — double it
                        bufferSize *= 2;
                        if (bufferSize > 16 * 1024 * 1024) // 16 MB cap
                            return false;
                        continue;
                    }

                    return false;
                }

                // Walk the SYSTEM_PROCESS_INFORMATION list
                IntPtr current = buffer;
                while (true)
                {
                    int nextOffset = Marshal.ReadInt32(current + SpiNextEntryOffset);
                    int pid = Marshal.ReadInt32(current + SpiUniqueProcessId);
                    int ppid = Marshal.ReadInt32(current + SpiInheritedFromUniqueProcessId);

                    if (pid > 0)
                    {
                        allProcessIds.Add(pid);

                        if (!parentChildMap.ContainsKey(ppid))
                            parentChildMap[ppid] = new List<int>();

                        if (pid != ppid) // Avoid self-referencing
                            parentChildMap[ppid].Add(pid);
                    }

                    if (nextOffset == 0)
                        break;

                    current = IntPtr.Add(current, nextOffset);
                }

                return true;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        private static void CollectDescendants(
            int parentPid,
            Dictionary<int, List<int>> parentChildMap,
            List<int> result,
            HashSet<int> visited)
        {
            if (!visited.Add(parentPid))
                return;

            if (parentChildMap.TryGetValue(parentPid, out var children))
            {
                // Use a copy to avoid modification during enumeration
                var childrenCopy = new List<int>(children);
                foreach (int childPid in childrenCopy)
                {
                    CollectDescendants(childPid, parentChildMap, result, visited);
                }
            }

            result.Add(parentPid);
        }

        private static void TerminateProcessById(int pid, bool graceful)
        {
            if (pid <= 0)
                return;

            IntPtr hProcess = IntPtr.Zero;
            try
            {
                // Try graceful shutdown first
                if (graceful)
                {
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById(pid);
                        if (proc.HasExited)
                            return;
                        proc.CloseMainWindow();
                        if (proc.WaitForExit(2000))
                            return;
                    }
                    catch
                    {
                        // Fall through to hard kill
                    }
                }

                // Hard kill
                hProcess = OpenProcess(ProcessTerminate, false, pid);
                if (hProcess == IntPtr.Zero)
                    return;

                TerminateProcess(hProcess, 1);
                WaitForSingleObject(hProcess, 5000);
            }
            catch (ArgumentException)
            {
                // Process no longer exists
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }

        // =====================================================================
        // GetImagePath
        // =====================================================================

        /// <summary>
        /// Gets the full image path (executable file name) of the specified
        /// process using <c>QueryFullProcessImageNameW</c> or
        /// <c>GetModuleFileNameEx</c> as a fallback.
        /// </summary>
        /// <param name="process">The process to examine.</param>
        /// <returns>
        /// The full path to the executable, or <see langword="null"/> if it
        /// cannot be retrieved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="process"/> is <see langword="null"/>.
        /// </exception>
        public static string? GetImagePath(this System.Diagnostics.Process process)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return GetImagePathInternal(process.Id);
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
        }

        private static string? GetImagePathInternal(int processId)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(ProcessQueryInformation, false, processId);
                if (hProcess == IntPtr.Zero)
                    return null;

                // Primary: QueryFullProcessImageNameW
                var sb = new StringBuilder(MaxPathChars);
                uint size = (uint)sb.Capacity;
                if (QueryFullProcessImageNameW(hProcess, 0, sb, ref size))
                {
                    return sb.ToString(0, (int)size);
                }

                // Fallback: GetModuleFileNameEx from psapi
                return GetImagePathFallback(hProcess);
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }

        private static string? GetImagePathFallback(IntPtr hProcess)
        {
            try
            {
                var sb = new StringBuilder(MaxPathChars);
                int result = GetModuleFileNameExW(hProcess, IntPtr.Zero, sb, sb.Capacity);
                if (result > 0)
                    return sb.ToString(0, result);

                return null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
        }

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetModuleFileNameExW(
            IntPtr hProcess,
            IntPtr hModule,
            StringBuilder lpFilename,
            int nSize);

        // =====================================================================
        // WaitForExitAsync
        // =====================================================================

        /// <summary>
        /// Waits asynchronously for the process to exit, using
        /// <see cref="ThreadPool.RegisterWaitForSingleObject(WaitHandle, WaitOrTimerCallback, object?, int, bool)"/>.
        /// </summary>
        /// <param name="process">The process to wait for.</param>
        /// <param name="timeoutMs">
        /// Time-out in milliseconds, or <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely. Default is infinite.
        /// </param>
        /// <returns>
        /// A task that completes with <see langword="true"/> if the process
        /// exited within the time-out, or <see langword="false"/> if it timed out.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="process"/> is <see langword="null"/>.
        /// </exception>
        public static Task<bool> WaitForExitAsync(
            this System.Diagnostics.Process process,
            int timeoutMs = Timeout.Infinite)
        {
            if (process is null)
                throw new ArgumentNullException(nameof(process));

            // Already exited
            if (process.HasExited)
                return Task.FromResult(true);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Retrieve the process handle
            IntPtr processHandle;
            try
            {
                // SafeWaitHandle is available on all target frameworks
                processHandle = process.SafeHandle?.DangerousGetHandle() ?? IntPtr.Zero;
                if (processHandle == IntPtr.Zero || processHandle == new IntPtr(-1))
                {
                    processHandle = OpenProcess(ProcessQueryInformation, false, process.Id);
                    if (processHandle == IntPtr.Zero)
                    {
                        // Can't open the process — check if it already exited
                        try
                        {
                            bool exited = process.HasExited;
                            return Task.FromResult(exited);
                        }
                        catch
                        {
                            return Task.FromResult(false);
                        }
                    }
                }
            }
            catch (DllNotFoundException)
            {
                return Task.FromResult(false);
            }
            catch (EntryPointNotFoundException)
            {
                return Task.FromResult(false);
            }

            // Create a wait handle wrapper
            var waitHandle = new SafeProcessWaitHandle(processHandle);

            RegisteredWaitHandle? registeredWait = null;
            registeredWait = ThreadPool.RegisterWaitForSingleObject(
                waitHandle,
                (state, timedOut) =>
                {
                    // Unregister the wait
                    registeredWait?.Unregister(null);
                    waitHandle.Dispose();

                    // If we opened the handle ourselves, close it
                    var tuple = (Tuple<SafeProcessWaitHandle, TaskCompletionSource<bool>>)state!;
                    tuple.Item1.Dispose();

                    // Complete the TCS
                    tuple.Item2.TrySetResult(!timedOut);
                },
                Tuple.Create(waitHandle, tcs),
                timeoutMs,
                executeOnlyOnce: true);

            return tcs.Task;
        }

        /// <summary>
        /// A minimal <see cref="WaitHandle"/> subclass that wraps a process handle
        /// returned by <see cref="OpenProcess"/>. This is needed because
        /// <see cref="ThreadPool.RegisterWaitForSingleObject(WaitHandle, WaitOrTimerCallback, object?, int, bool)"/> requires a
        /// <see cref="WaitHandle"/> whose underlying handle is a valid
        /// synchronization object (process handles are waitable).
        /// </summary>
        private sealed class SafeProcessWaitHandle : WaitHandle
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SafeProcessWaitHandle"/>
            /// class wrapping an existing process handle.
            /// </summary>
            /// <param name="handle">The process handle to wrap.</param>
            public SafeProcessWaitHandle(IntPtr handle)
            {
                SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(
                    handle,
                    ownsHandle: true);
            }
        }
    }
}
