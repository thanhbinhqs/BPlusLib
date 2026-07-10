// <copyright file="ProcessHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Process
{
    /// <summary>
    /// Represents the result of executing a command via <see cref="CommandRunner"/>.
    /// </summary>
    public sealed class CommandRunnerResult
    {
        /// <summary>
        /// Gets the process exit code.
        /// </summary>
        public int ExitCode { get; internal set; }

        /// <summary>
        /// Gets the full standard output text captured from the process,
        /// decoded as UTF-8.
        /// </summary>
        public string StandardOutput { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the full standard error text captured from the process,
        /// decoded as UTF-8.
        /// </summary>
        public string StandardError { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the operation timed out before the
        /// process exited naturally.
        /// </summary>
        public bool TimedOut { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the process completed successfully
        /// (exit code 0 and did not time out).
        /// </summary>
        public bool Succeeded => ExitCode == 0 && !TimedOut;
    }

    /// <summary>
    /// Provides methods to execute external processes synchronously and
    /// asynchronously, capturing standard output and standard error using
    /// pure P/Invoke (no WMI, no <see cref="System.Diagnostics.Process"/>).
    /// Thread-safe.
    /// </summary>
    public static class CommandRunner
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const uint NormalPriorityClass = 0x00000020;
        private const uint CreateNoWindow = 0x08000000;
        private const uint StartfUseStdHandles = 0x00000100;
        private const int HandleFlagInherit = 0x00000001;
        private const int WaitObject0 = 0x00000000;
        private const int WaitTimeout = 0x00000102;
        private const int PipeBufferSize = 4096;
        private const int TerminateWaitMs = 5000;

        // =====================================================================
        // Win32 structures
        // =====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        // =====================================================================
        // kernel32.dll P/Invoke
        // =====================================================================

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessW(
            string? lpApplicationName,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreatePipe(
            out IntPtr hReadPipe,
            out IntPtr hWritePipe,
            ref SECURITY_ATTRIBUTES lpPipeAttributes,
            int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetHandleInformation(
            IntPtr hObject,
            int dwMask,
            int dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int WaitForSingleObject(
            IntPtr hHandle,
            int dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(
            IntPtr hFile,
            [Out] byte[] lpBuffer,
            int nNumberOfBytesToRead,
            out int lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeProcess(
            IntPtr hProcess,
            out int lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(
            IntPtr hProcess,
            int uExitCode);

        // =====================================================================
        // Public API — synchronous
        // =====================================================================

        /// <summary>
        /// Runs the specified executable with arguments synchronously, capturing
        /// stdout and stderr. Uses <c>CreateProcessW</c> with redirected pipes.
        /// </summary>
        /// <param name="fileName">
        /// The executable file name or path. If <paramref name="fileName"/> does not
        /// contain a directory separator, the system searches <c>%PATH%</c>.
        /// </param>
        /// <param name="arguments">
        /// The command-line arguments passed to the executable.
        /// </param>
        /// <param name="workingDirectory">
        /// Optional working directory for the child process. If <see langword="null"/>,
        /// the current process's working directory is used.
        /// </param>
        /// <param name="timeoutMs">
        /// Time-out in milliseconds. Use <c>System.Threading.Timeout.Infinite</c> (-1)
        /// for no time-out. Default is 30,000 (30 seconds).
        /// </param>
        /// <returns>
        /// A <see cref="CommandRunnerResult"/> containing the exit code, captured
        /// output, and time-out status.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The method is invoked on a non-Windows operating system.
        /// </exception>
        /// <exception cref="Win32Exception">
        /// The underlying <c>CreateProcessW</c> or pipe-creation call failed.
        /// </exception>
        public static CommandRunnerResult RunCommand(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            int timeoutMs = 30000)
        {
#if NET6_0_OR_GREATER
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException(
                    "CommandRunner is only supported on Windows.");
#endif
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            try
            {
                return RunCommandInternal(fileName, arguments, workingDirectory, timeoutMs);
            }
            catch (DllNotFoundException ex)
            {
                throw new PlatformNotSupportedException(
                    "CommandRunner requires Windows kernel32.dll APIs.", ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new PlatformNotSupportedException(
                    "CommandRunner requires Windows kernel32.dll APIs.", ex);
            }
        }

        // =====================================================================
        // Public API — asynchronous
        // =====================================================================

        /// <summary>
        /// Runs the specified executable with arguments asynchronously, capturing
        /// Offloaded to the thread-pool via <see cref="Task.Run(System.Action)"/>.
        /// </summary>
        /// <inheritdoc cref="RunCommand(string, string, string?, int)"/>
        public static Task<CommandRunnerResult> RunCommandAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            int timeoutMs = 30000)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            return Task.Run(() => RunCommand(fileName, arguments, workingDirectory, timeoutMs));
        }

        // =====================================================================
        // Internal implementation
        // =====================================================================

        private static CommandRunnerResult RunCommandInternal(
            string fileName,
            string arguments,
            string? workingDirectory,
            int timeoutMs)
        {
            var result = new CommandRunnerResult();

            // ---- 1. Create stdout & stderr pipes ----
            SECURITY_ATTRIBUTES saAttr = default;
            saAttr.nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
            saAttr.bInheritHandle = 1;
            saAttr.lpSecurityDescriptor = IntPtr.Zero;

            IntPtr stdoutRead = IntPtr.Zero;
            IntPtr stdoutWrite = IntPtr.Zero;
            IntPtr stderrRead = IntPtr.Zero;
            IntPtr stderrWrite = IntPtr.Zero;
            IntPtr processHandle = IntPtr.Zero;
            IntPtr threadHandle = IntPtr.Zero;

            try
            {
                if (!CreatePipe(out stdoutRead, out stdoutWrite, ref saAttr, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to create stdout pipe.");

                if (!CreatePipe(out stderrRead, out stderrWrite, ref saAttr, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to create stderr pipe.");

                // Make read ends non-inheritable so only the parent can read
                if (!SetHandleInformation(stdoutRead, HandleFlagInherit, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to clear inherit flag on stdout read handle.");

                if (!SetHandleInformation(stderrRead, HandleFlagInherit, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to clear inherit flag on stderr read handle.");

                // ---- 2. Set up STARTUPINFO ----
                STARTUPINFO startupInfo = default;
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>();
                startupInfo.dwFlags = (int)StartfUseStdHandles;
                startupInfo.hStdOutput = stdoutWrite;
                startupInfo.hStdError = stderrWrite;
                startupInfo.hStdInput = IntPtr.Zero;

                // ---- 3. Prepare command line ----
                // If fileName contains spaces, quote it
                string quotedFileName = fileName.IndexOf(' ') >= 0
                    ? "\"" + fileName + "\""
                    : fileName;
                var commandLine = new StringBuilder(quotedFileName, 512);
                if (!string.IsNullOrEmpty(arguments))
                {
                    commandLine.Append(' ');
                    commandLine.Append(arguments);
                }

                // ---- 4. Create the process ----
                SECURITY_ATTRIBUTES defaultSa = default;
                defaultSa.nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>();

                if (!CreateProcessW(
                    null,                   // lpApplicationName — search PATH
                    commandLine,
                    ref defaultSa,
                    ref defaultSa,
                    true,                   // bInheritHandles — so child inherits pipe write ends
                    NormalPriorityClass | CreateNoWindow,
                    IntPtr.Zero,            // environment — inherit parent's
                    workingDirectory,
                    ref startupInfo,
                    out PROCESS_INFORMATION procInfo))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error,
                        $"CreateProcess failed for '{fileName}'. Error code: {error}");
                }

                processHandle = procInfo.hProcess;
                threadHandle = procInfo.hThread;
            }
            finally
            {
                // Close the WRITE ends in the parent immediately.
                // The child still has inherited copies, so the pipe stays open
                // until the child exits.
                if (stdoutWrite != IntPtr.Zero) { CloseHandle(stdoutWrite); stdoutWrite = IntPtr.Zero; }
                if (stderrWrite != IntPtr.Zero) { CloseHandle(stderrWrite); stderrWrite = IntPtr.Zero; }
            }

            // ---- 5. Read pipes concurrently while waiting for the process ----
            var stdoutSb = new StringBuilder();
            var stderrSb = new StringBuilder();

            Task stdoutTask = Task.Run(() => ReadPipeToEnd(stdoutRead, stdoutSb));
            Task stderrTask = Task.Run(() => ReadPipeToEnd(stderrRead, stderrSb));

            try
            {
                // ---- 6. Wait for the process ----
                int waitResult = WaitForSingleObject(processHandle, timeoutMs);
                result.TimedOut = waitResult == WaitTimeout;

                if (result.TimedOut)
                {
                    // Kill the hung process
                    try
                    {
                        TerminateProcess(processHandle, 1);
                        WaitForSingleObject(processHandle, TerminateWaitMs);
                    }
                    catch (DllNotFoundException)
                    {
                    }
                    catch (EntryPointNotFoundException)
                    {
                    }
                }
            }
            finally
            {
                // Close the read ends so the background readers unblock
                if (stdoutRead != IntPtr.Zero) { CloseHandle(stdoutRead); stdoutRead = IntPtr.Zero; }
                if (stderrRead != IntPtr.Zero) { CloseHandle(stderrRead); stderrRead = IntPtr.Zero; }

                // Wait for readers to finish
                try
                {
                    Task.WaitAll(new[] { stdoutTask, stderrTask }, 5000);
                }
                catch
                {
                    // Best-effort — if a reader threw, we still have partial data
                }

                result.StandardOutput = stdoutSb.ToString();
                result.StandardError = stderrSb.ToString();
            }

            // ---- 7. Get exit code ----
            if (GetExitCodeProcess(processHandle, out int exitCode))
                result.ExitCode = exitCode;
            else
                result.ExitCode = -1;

            // ---- 8. Clean up process/thread handles ----
            if (threadHandle != IntPtr.Zero) { CloseHandle(threadHandle); threadHandle = IntPtr.Zero; }
            if (processHandle != IntPtr.Zero) { CloseHandle(processHandle); processHandle = IntPtr.Zero; }

            return result;
        }

        /// <summary>
        /// Reads all available data from a pipe handle into a <see cref="StringBuilder"/>.
        /// Runs on a background thread. Blocks until the pipe is closed or broken.
        /// </summary>
        private static void ReadPipeToEnd(IntPtr hPipe, StringBuilder builder)
        {
            var buffer = new byte[PipeBufferSize];
            int bytesRead;

            while (true)
            {
                try
                {
                    if (!ReadFile(hPipe, buffer, buffer.Length, out bytesRead, IntPtr.Zero))
                    {
                        // Pipe closed, broken, or error — stop reading
                        break;
                    }

                    if (bytesRead == 0)
                        break;

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                }
                catch (DllNotFoundException)
                {
                    break;
                }
                catch (EntryPointNotFoundException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
    }
}
