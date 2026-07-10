// <copyright file="CrashDumpHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Diagnostics
{
    // =====================================================================
    // Enums
    // =====================================================================

    /// <summary>
    /// Specifies the type and amount of data to include in a minidump.
    /// These flags correspond directly to the <c>MINIDUMP_TYPE</c> values
    /// defined in <c>dbghelp.h</c>.
    /// </summary>
    [Flags]
    public enum MiniDumpType : uint
    {
        /// <summary>Minimal dump (just the stack and basic info).</summary>
        MiniDumpNormal = 0,

        /// <summary>Includes data segments (default).</summary>
        MiniDumpWithDataSegs = 1,

        /// <summary>Includes full memory contents.</summary>
        MiniDumpWithFullMemory = 2,

        /// <summary>Includes handle data.</summary>
        MiniDumpWithHandleData = 4,

        /// <summary>Filters memory to reduce size.</summary>
        MiniDumpFilterMemory = 8,

        /// <summary>Scans memory for referenced objects.</summary>
        MiniDumpScanMemory = 16,

        /// <summary>Includes unloaded modules.</summary>
        MiniDumpWithUnloadedModules = 32,

        /// <summary>Includes indirectly referenced memory.</summary>
        MiniDumpWithIndirectlyReferencedMemory = 64,

        /// <summary>Filters module paths for privacy.</summary>
        MiniDumpFilterModulePaths = 128,

        /// <summary>Includes per-process thread data.</summary>
        MiniDumpWithProcessThreadData = 256,

        /// <summary>Includes private read-write memory.</summary>
        MiniDumpWithPrivateReadWriteMemory = 512,

        /// <summary>Excludes optional data to reduce size.</summary>
        MiniDumpWithoutOptionalData = 1024,

        /// <summary>Includes full memory info.</summary>
        MiniDumpWithFullMemoryInfo = 2048,

        /// <summary>Includes thread state info.</summary>
        MiniDumpWithThreadInfo = 4096,

        /// <summary>Includes code segments.</summary>
        MiniDumpWithCodeSegs = 8192,

        /// <summary>Excludes auxiliary state to reduce size.</summary>
        MiniDumpWithoutAuxiliaryState = 16384,

        /// <summary>Includes full auxiliary state.</summary>
        MiniDumpWithFullAuxiliaryState = 32768,

        /// <summary>Includes private write-copy memory.</summary>
        MiniDumpWithPrivateWriteCopyMemory = 65536,

        /// <summary>Ignores inaccessible memory (continues instead of failing).</summary>
        MiniDumpIgnoreInaccessibleMemory = 131072,

        /// <summary>Includes token information.</summary>
        MiniDumpWithTokenInformation = 262144,

        /// <summary>Includes module headers.</summary>
        MiniDumpWithModuleHeaders = 524288,

        /// <summary>Filters triage information for debugging.</summary>
        MiniDumpFilterTriage = 1048576,

        /// <summary>Includes AVX XState context.</summary>
        MiniDumpWithAvxXStateContext = 2097152,

        /// <summary>Includes Intel Processor Trace data.</summary>
        MiniDumpWithIptTrace = 4194304,
    }

    // =====================================================================
    // Native structures
    // =====================================================================

    /// <summary>
    /// Contains exception information for a minidump. This is passed to
    /// <c>MiniDumpWriteDump</c> when an exception context should be
    /// included in the dump.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MINIDUMP_EXCEPTION_INFORMATION
    {
        /// <summary>The thread identifier of the thread that caused the exception.</summary>
        internal uint ThreadId;

        /// <summary>Pointer to an EXCEPTION_POINTERS structure.</summary>
        internal IntPtr ExceptionPointers;

        /// <summary>
        /// Set to <see langword="true"/> if the exception context should be
        /// considered client-side rather than server-side.
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        internal bool ClientPointers;
    }

    /// <summary>
    /// Contains user stream information for a minidump. Not used in simple
    /// dumps — pass <see cref="IntPtr.Zero"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MINIDUMP_USER_STREAM_INFORMATION
    {
        /// <summary>Reserved, must be zero.</summary>
        internal uint Reserved;

        /// <summary>Number of user streams.</summary>
        internal uint UserStreamCount;

        /// <summary>Pointer to an array of MINIDUMP_USER_STREAM structures.</summary>
        internal IntPtr UserStreamArray;
    }

    // =====================================================================
    // CrashDumpHelper
    // =====================================================================

    /// <summary>
    /// Provides safe, no-throw access to Windows minidump creation via
    /// <c>dbghelp.dll</c> / <c>MiniDumpWriteDump</c>. All methods return
    /// <see langword="false"/> gracefully on non-Windows platforms or when
    /// the underlying API fails.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class uses P/Invoke to call the <c>dbghelp</c> library. On
    /// non-Windows operating systems all methods return <see langword="false"/>
    /// immediately without attempting to call any native APIs.
    /// </para>
    /// <para>
    /// The calling process must have <c>PROCESS_QUERY_INFORMATION</c> and
    /// <c>PROCESS_VM_READ</c> access rights to the target process. For most
    /// scenarios this means running as Administrator or with SeDebugPrivilege.
    /// </para>
    /// </remarks>
    public static partial class CrashDumpHelper
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        /// <summary>
        /// Required access to read process information for dumping.
        /// </summary>
        private const uint ProcessQueryInformation = 0x0400;

        /// <summary>
        /// Required access to read the virtual memory of the target process.
        /// </summary>
        private const uint ProcessVmRead = 0x0010;

        /// <summary>
        /// Required access to duplicate handles (used in some scenarios).
        /// </summary>
        private const uint ProcessDupHandle = 0x0040;

        /// <summary>
        /// Combined access rights needed to create a minidump.
        /// </summary>
        private const uint ProcessAllAccess = ProcessQueryInformation | ProcessVmRead | ProcessDupHandle;

        // -----------------------------------------------------------------
        // Public methods
        // -----------------------------------------------------------------

        /// <summary>
        /// Attempts to create a minidump of the specified process to the
        /// specified output path.
        /// </summary>
        /// <param name="processId">The ID of the target process.</param>
        /// <param name="outputPath">The full path where the dump file will be written.</param>
        /// <param name="dumpType">The type of minidump to create (default: <see cref="MiniDumpType.MiniDumpWithDataSegs"/>).</param>
        /// <returns><see langword="true"/> if the dump was created successfully; otherwise <see langword="false"/>.</returns>
        public static bool TryCreateMiniDump(int processId, string outputPath, MiniDumpType dumpType = MiniDumpType.MiniDumpWithDataSegs)
        {
            return TryCreateDumpCore(processId, outputPath, (uint)dumpType);
        }

        /// <summary>
        /// Attempts to create a full user-mode dump of the specified process
        /// (equivalent to <see cref="MiniDumpType.MiniDumpWithFullMemory"/>).
        /// </summary>
        /// <param name="processId">The ID of the target process.</param>
        /// <param name="outputPath">The full path where the dump file will be written.</param>
        /// <returns><see langword="true"/> if the dump was created successfully; otherwise <see langword="false"/>.</returns>
        public static bool TryCreateFullDump(int processId, string outputPath)
        {
            return TryCreateDumpCore(processId, outputPath, (uint)MiniDumpType.MiniDumpWithFullMemory);
        }

        /// <summary>
        /// Gets the default folder where Windows stores crash dumps.
        /// On Windows 10 / 11 and Windows Server 2016+, this is typically
        /// <c>%LOCALAPPDATA%\CrashDumps</c> for per-user application dumps,
        /// or <c>%SystemRoot%\Minidump</c> for system minidumps.
        /// </summary>
        /// <returns>
        /// The path to the default crash dump folder, or <see langword="null"/>
        /// if the location could not be determined (e.g. on non-Windows).
        /// </returns>
        public static string? GetDefaultDumpFolder()
        {
            try
            {
                if (!IsWindows())
                    return null;

                // Per-user application crash dumps (Windows Error Reporting).
                string? localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localAppData))
                {
                    string crashDumps = Path.Combine(localAppData, "CrashDumps");
                    return crashDumps;
                }

                // Fallback: system minidump folder.
                string? systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                if (!string.IsNullOrEmpty(systemRoot))
                {
                    return Path.Combine(systemRoot, "Minidump");
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------------------
        // Core implementation
        // -----------------------------------------------------------------

        /// <summary>
        /// Core dump creation logic. Opens the target process, calls
        /// <see cref="MiniDumpWriteDump"/>, and cleans up handles.
        /// </summary>
        private static bool TryCreateDumpCore(int processId, string outputPath, uint dumpType)
        {
            // Validate inputs.
            if (processId <= 0)
                return false;

            if (string.IsNullOrEmpty(outputPath))
                return false;

            // Only supported on Windows.
            if (!IsWindows())
                return false;

            // Ensure the output directory exists.
            try
            {
                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch
            {
                return false;
            }

            IntPtr hProcess = IntPtr.Zero;
            IntPtr hFile = IntPtr.Zero;

            try
            {
                // Open the target process with sufficient rights.
                hProcess = OpenProcess(ProcessAllAccess, false, processId);
                if (hProcess == IntPtr.Zero || hProcess == InvalidHandleValue)
                    return false;

                // Create or truncate the output file.
                hFile = CreateFile(
                    outputPath,
                    GenericWrite,
                    0,                        // dwShareMode = 0 (exclusive)
                    IntPtr.Zero,
                    CreateAlways,
                    FileAttributeNormal,
                    IntPtr.Zero);

                if (hFile == IntPtr.Zero || hFile == InvalidHandleValue)
                    return false;

                // Create the minidump — no exception info, no user streams.
                bool success = MiniDumpWriteDump(
                    hProcess,
                    (uint)processId,
                    hFile,
                    dumpType,
                    IntPtr.Zero,              // ExceptionParam
                    IntPtr.Zero,              // UserStreamParam
                    IntPtr.Zero);             // CallbackParam

                return success;
            }
            catch
            {
                return false;
            }
            finally
            {
                // Clean up handles in reverse order.
                if (hFile != IntPtr.Zero && hFile != InvalidHandleValue)
                    CloseHandle(hFile);

                if (hProcess != IntPtr.Zero && hProcess != InvalidHandleValue)
                    CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> when running on the Windows operating
        /// system where <c>dbghelp.dll</c> and the associated P/Invoke targets
        /// are available.
        /// </summary>
        private static bool IsWindows()
        {
#if NET472 || NET6_0
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            return OperatingSystem.IsWindows();
#endif
        }

        // -----------------------------------------------------------------
        // P/Invoke — kernel32.dll
        // -----------------------------------------------------------------

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        private const uint GenericWrite = 0x40000000;
        private const uint CreateAlways = 2;
        private const uint FileAttributeNormal = 0x80;

        /// <summary>
        /// Opens an existing local process object.
        /// </summary>
        /// <param name="dwDesiredAccess">Access flags (PROCESS_QUERY_INFORMATION | PROCESS_VM_READ).</param>
        /// <param name="bInheritHandle">Whether the handle is inheritable.</param>
        /// <param name="dwProcessId">The PID of the target process.</param>
        /// <returns>A handle to the process, or <see cref="IntPtr.Zero"/> on failure.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            int dwProcessId);

        /// <summary>
        /// Creates or opens a file for writing the dump output.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">The handle to close.</param>
        /// <returns><see langword="true"/> on success.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        // -----------------------------------------------------------------
        // P/Invoke — dbghelp.dll
        // -----------------------------------------------------------------

        /// <summary>
        /// Writes a minidump of the specified process to the specified file.
        /// </summary>
        /// <param name="hProcess">Handle to the target process.</param>
        /// <param name="processId">Process ID of the target process.</param>
        /// <param name="hFile">Handle to the output file.</param>
        /// <param name="dumpType">MINIDUMP_TYPE flags.</param>
        /// <param name="exceptionParam">
        /// Pointer to a <see cref="MINIDUMP_EXCEPTION_INFORMATION"/> structure,
        /// or <see cref="IntPtr.Zero"/>.
        /// </param>
        /// <param name="userStreamParam">
        /// Pointer to a <see cref="MINIDUMP_USER_STREAM_INFORMATION"/> structure,
        /// or <see cref="IntPtr.Zero"/>.
        /// </param>
        /// <param name="callbackParam">
        /// Pointer to a <c>MINIDUMP_CALLBACK_INFORMATION</c> structure,
        /// or <see cref="IntPtr.Zero"/>.
        /// </param>
        /// <returns><see langword="true"/> on success.</returns>
        [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            IntPtr hFile,
            uint dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);
    }
}
