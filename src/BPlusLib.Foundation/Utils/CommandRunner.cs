// <copyright file="Utils.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace BPlusLib.Foundation
{
    /// <summary>
    /// General-purpose Windows utility methods: IP address resolution,
    /// command execution (cmd / powershell), and system helpers.
    /// </summary>
    /// <remarks>
    /// All methods are thread-safe and never throw — errors are returned
    /// via <see cref="CommandResult"/> or <c>null</c> as appropriate.
    /// </remarks>
    public static class Utils
    {
        // =================================================================
        // IP Address
        // =================================================================

        /// <summary>
        /// Gets the primary local IPv4 address of this machine.
        /// </summary>
        /// <returns>
        /// The primary IPv4 address as a string (e.g. "192.168.1.100"),
        /// or <c>null</c> if no suitable address is found.
        /// </returns>
        /// <remarks>
        /// Uses <see cref="NetworkInterface"/> to find the first operational
        /// network interface with a valid IPv4 unicast address, preferring
        /// interfaces that are up and not loopback.
        /// </remarks>
        public static string? GetLocalIPAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }

                // Fallback: use DNS
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all local IPv4 addresses (non-loopback) on this machine.
        /// </summary>
        /// <returns>A list of IPv4 address strings, or an empty list.</returns>
        public static IReadOnlyList<string> GetAllLocalIPAddresses()
        {
            var results = new List<string>();

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            results.Add(addr.Address.ToString());
                        }
                    }
                }

                // Fallback via DNS
                if (results.Count == 0)
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ip))
                        {
                            results.Add(ip.ToString());
                        }
                    }
                }
            }
            catch
            {
                // Return whatever we collected
            }

            return results;
        }

        /// <summary>
        /// Gets the local machine's host name.
        /// </summary>
        /// <returns>The host name, or <c>null</c> on failure.</returns>
        public static string? GetHostName()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch
            {
                return null;
            }
        }

        // =================================================================
        // Command Execution (cmd.exe)
        // =================================================================

        /// <summary>
        /// Executes a command via <c>cmd.exe /c</c> and returns the output.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workingDirectory">
        /// Optional working directory. If <c>null</c>, inherits the current
        /// process's working directory.
        /// </param>
        /// <param name="timeoutMs">
        /// Timeout in milliseconds before killing the process.
        /// Default: 30,000 (30 seconds). Pass <c>0</c> for no timeout.
        /// </param>
        /// <returns>
        /// A <see cref="CommandResult"/> with stdout, stderr, exit code,
        /// and whether it timed out.
        /// </returns>
        public static CommandResult RunCommand(
            string command,
            string? workingDirectory = null,
            int timeoutMs = 30000)
        {
            return RunProcess("cmd.exe", $"/c \"{command}\"", workingDirectory, timeoutMs);
        }

        /// <summary>
        /// Executes a PowerShell script via <c>powershell.exe -NoProfile -Command</c>.
        /// </summary>
        /// <param name="script">The PowerShell script or command to execute.</param>
        /// <param name="workingDirectory">
        /// Optional working directory. If <c>null</c>, inherits the current
        /// process's working directory.
        /// </param>
        /// <param name="timeoutMs">
        /// Timeout in milliseconds before killing the process.
        /// Default: 60,000 (60 seconds). Pass <c>0</c> for no timeout.
        /// </param>
        /// <returns>
        /// A <see cref="CommandResult"/> with stdout, stderr, exit code,
        /// and whether it timed out.
        /// </returns>
        public static CommandResult RunPowerShell(
            string script,
            string? workingDirectory = null,
            int timeoutMs = 60000)
        {
            return RunProcess("powershell.exe", $"-NoProfile -Command \"{script}\"", workingDirectory, timeoutMs);
        }

        /// <summary>
        /// Executes a PowerShell script via <c>pwsh.exe</c> (PowerShell Core)
        /// if available; falls back to <c>powershell.exe</c>.
        /// </summary>
        public static CommandResult RunPowerShellCore(
            string script,
            string? workingDirectory = null,
            int timeoutMs = 60000)
        {
            try
            {
                // Try pwsh.exe first (PowerShell Core / 7+)
                var result = RunProcess("pwsh.exe", $"-NoProfile -Command \"{script}\"", workingDirectory, timeoutMs);
                if (result.ExitCode != -1) return result;
            }
            catch
            {
                // Fall through
            }

            return RunPowerShell(script, workingDirectory, timeoutMs);
        }

        /// <summary>
        /// Low-level process runner.
        /// </summary>
        private static CommandResult RunProcess(
            string fileName,
            string arguments,
            string? workingDirectory,
            int timeoutMs)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = workingDirectory ?? string.Empty,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                    },
                    EnableRaisingEvents = true,
                };

                using var stdoutWait = new System.Threading.ManualResetEvent(false);
                using var stderrWait = new System.Threading.ManualResetEvent(false);

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdout.AppendLine(e.Data);
                    }
                    else
                    {
                        stdoutWait.Set();
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stderr.AppendLine(e.Data);
                    }
                    else
                    {
                        stderrWait.Set();
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool exited;
                if (timeoutMs > 0)
                {
                    exited = process.WaitForExit(timeoutMs);
                }
                else
                {
                    process.WaitForExit();
                    exited = true;
                }

                // Wait for async output to drain
                if (exited)
                {
                    stdoutWait.WaitOne(5000);
                    stderrWait.WaitOne(5000);
                }
                else
                {
                    // Timeout — kill the process
                    try
                    {
                        if (!process.HasExited)
                        {
#if NET8_0_OR_GREATER
                            process.Kill(entireProcessTree: true);
#else
                            process.Kill();
#endif
                        }
                    }
                    catch
                    {
                        // Best-effort kill
                    }

                    stdoutWait.WaitOne(1000);
                    stderrWait.WaitOne(1000);
                }

                return new CommandResult
                {
                    StandardOutput = stdout.ToString().TrimEnd(),
                    StandardError = stderr.ToString().TrimEnd(),
                    ExitCode = exited ? process.ExitCode : -1,
                    TimedOut = !exited,
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    StandardOutput = stdout.ToString().TrimEnd(),
                    StandardError = stderr.ToString().TrimEnd() + (stderr.Length > 0 ? "\n" : "") + ex.Message,
                    ExitCode = -1,
                    TimedOut = false,
                };
            }
        }
    }

    /// <summary>
    /// Represents the result of executing an external command.
    /// </summary>
    public sealed class CommandResult
    {
        /// <summary>
        /// The standard output text produced by the process.
        /// </summary>
        public string StandardOutput { get; init; } = string.Empty;

        /// <summary>
        /// The standard error text produced by the process.
        /// </summary>
        public string StandardError { get; init; } = string.Empty;

        /// <summary>
        /// The process exit code. <c>-1</c> indicates the process timed out
        /// or could not be started.
        /// </summary>
        public int ExitCode { get; init; } = -1;

        /// <summary>
        /// Whether the process was killed due to exceeding the timeout.
        /// </summary>
        public bool TimedOut { get; init; }

        /// <summary>
        /// Returns a summary of the result.
        /// </summary>
        public override string ToString()
        {
            return $"ExitCode={ExitCode}, TimedOut={TimedOut}, "
                + $"StdOut={StandardOutput.Length} chars, StdErr={StandardError.Length} chars";
        }
    }
}