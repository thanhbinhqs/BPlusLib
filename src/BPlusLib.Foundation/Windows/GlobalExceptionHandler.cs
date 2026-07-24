// <copyright file="GlobalExceptionHandler.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Contains information about an unhandled exception crash.
    /// </summary>
    public sealed class CrashReport
    {
        /// <summary>When the crash occurred.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        /// <summary>The exception type name.</summary>
        public string ExceptionType { get; init; } = string.Empty;
        /// <summary>The exception message.</summary>
        public string Message { get; init; } = string.Empty;
        /// <summary>The full stack trace.</summary>
        public string StackTrace { get; init; } = string.Empty;
        /// <summary>Inner exception details, if any.</summary>
        public string? InnerException { get; init; }
        /// <summary>Collected system information.</summary>
        public Dictionary<string, string> SystemInfo { get; init; } = new();
        /// <summary>Path to the minidump file, if created.</summary>
        public string? DumpPath { get; init; }
        /// <summary>Application version.</summary>
        public string? AppVersion { get; init; }
    }

    /// <summary>
    /// Singleton that catches unhandled exceptions across all threads and AppDomains.
    /// Subscribe to <see cref="UnhandledException"/> to handle crash reports.
    /// </summary>
    public sealed class GlobalExceptionHandler : IDisposable
    {
        private static readonly Lazy<GlobalExceptionHandler> _instance = new(() => new GlobalExceptionHandler(), LazyThreadSafetyMode.ExecutionAndPublication);
        private bool _isHandling;
        private bool _disposed;
        private readonly object _lock = new();

        /// <summary>Singleton instance.</summary>
        public static GlobalExceptionHandler Instance => _instance.Value;

        /// <summary>Fires when an unhandled exception is caught.</summary>
        public event EventHandler<CrashReport>? UnhandledException;

        /// <summary>Whether the handler is currently active.</summary>
        public bool IsHandling => _isHandling;

        /// <summary>
        /// Directory where crash reports are saved. If null, reports are not saved to disk.
        /// </summary>
        public string? DumpDirectory { get; set; }

        private GlobalExceptionHandler() { }

        /// <summary>
        /// Enables the global exception handler. Subscribes to:
        /// - AppDomain.CurrentDomain.UnhandledException
        /// - TaskScheduler.UnobservedTaskException
        /// </summary>
        public bool Enable()
        {
            if (_disposed) return false;

            lock (_lock)
            {
                if (_isHandling) return true;

                try
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                    _isHandling = true;
                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Disables the global exception handler.
        /// </summary>
        public bool Disable()
        {
            lock (_lock)
            {
                if (!_isHandling) return true;

                try
                {
                    AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                    TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                    _isHandling = false;
                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Creates a crash report from an exception.
        /// </summary>
        public static CrashReport CreateCrashReport(Exception ex)
        {
            var report = new CrashReport
            {
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                Message = ex.Message ?? "",
                StackTrace = ex.StackTrace ?? "",
                InnerException = ex.InnerException?.ToString(),
                AppVersion = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(),
            };

            // Collect system info
            try
            {
                report.SystemInfo["OS"] = RuntimeInformation.OSDescription;
                report.SystemInfo["Architecture"] = RuntimeInformation.ProcessArchitecture.ToString();
                report.SystemInfo["FrameworkDescription"] = RuntimeInformation.FrameworkDescription;
                report.SystemInfo["ProcessorCount"] = Environment.ProcessorCount.ToString();
                report.SystemInfo["WorkingSet"] = Environment.WorkingSet.ToString("N0");
                report.SystemInfo["CommandLine"] = Environment.CommandLine;
                report.SystemInfo["Is64BitProcess"] = Environment.Is64BitProcess.ToString();
                report.SystemInfo["TickCount"] = Environment.TickCount.ToString();
            }
            catch { /* system info collection is best-effort */ }

            return report;
        }

        /// <summary>
        /// Saves a crash report to a file.
        /// </summary>
        public static bool SaveCrashReport(CrashReport report, string path)
        {
            if (report is null || string.IsNullOrEmpty(path)) return false;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== CRASH REPORT ===");
                sb.AppendLine($"Timestamp: {report.Timestamp:O}");
                sb.AppendLine($"Exception: {report.ExceptionType}");
                sb.AppendLine($"Message: {report.Message}");
                sb.AppendLine($"AppVersion: {report.AppVersion}");
                sb.AppendLine();
                sb.AppendLine("=== STACK TRACE ===");
                sb.AppendLine(report.StackTrace);

                if (!string.IsNullOrEmpty(report.InnerException))
                {
                    sb.AppendLine();
                    sb.AppendLine("=== INNER EXCEPTION ===");
                    sb.AppendLine(report.InnerException);
                }

                if (report.SystemInfo.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== SYSTEM INFO ===");
                    foreach (var kvp in report.SystemInfo)
                        sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }

                if (!string.IsNullOrEmpty(report.DumpPath))
                {
                    sb.AppendLine();
                    sb.AppendLine($"Dump: {report.DumpPath}");
                }

                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch { return false; }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception?.InnerException ?? e.Exception);
            e.SetObserved();
        }

        private void HandleException(Exception? ex)
        {
            if (ex is null) return;

            try
            {
                var report = CreateCrashReport(ex);

                // Try to save to disk
                if (!string.IsNullOrEmpty(DumpDirectory))
                {
                    try
                    {
                        string reportPath = Path.Combine(DumpDirectory,
                            $"crash_{report.Timestamp:yyyyMMdd_HHmmss}.txt");
                        SaveCrashReport(report, reportPath);
                    }
                    catch { /* save is best-effort */ }
                }

                // Fire event
                UnhandledException?.Invoke(this, report);
            }
            catch { /* handler itself must never throw */ }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Disable();
            }
        }
    }
}
