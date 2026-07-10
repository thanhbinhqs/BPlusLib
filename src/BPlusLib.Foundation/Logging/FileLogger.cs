// <copyright file="FileLogger.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Specifies how log files are rolled and archived.
    /// </summary>
    [Flags]
    public enum RollingPolicy
    {
        /// <summary>
        /// No rolling is performed. All entries are written to a single file.
        /// </summary>
        None = 0,

        /// <summary>
        /// A new log file is created each day with a date suffix in the filename.
        /// </summary>
        Daily = 1,

        /// <summary>
        /// When the current log file exceeds the maximum size, it is archived
        /// and a new file is started.
        /// </summary>
        FileSize = 2,

        /// <summary>
        /// Both <see cref="Daily"/> and <see cref="FileSize"/> rolling are active.
        /// Files are organised by date AND rolled by size within each date.
        /// </summary>
        Combined = Daily | FileSize,
    }

    /// <summary>
    /// Writes log entries to a file with optional rolling and archiving.
    /// Thread-safe and works on all .NET-supported platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This logger uses a <see cref="StreamWriter"/> opened with <see cref="FileShare.Read"/>
    /// so that the file can be read (e.g. by a tail utility) while logging is in progress.
    /// All write operations are serialised through a <see cref="SemaphoreSlim"/> to ensure
    /// thread safety.
    /// </para>
    /// <para>
    /// The <see cref="Log(LogEntry)"/> and convenience methods never throw exceptions;
    /// any I/O errors are silently caught and discarded. Use <see cref="Flush"/> or
    /// <see cref="FlushAsync"/> to ensure pending data is written to disk.
    /// </para>
    /// </remarks>
    public class FileLogger : IDisposable
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const string DateFormat = "yyyy-MM-dd";
        private const string LogLineFormat = "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}]";

        // -----------------------------------------------------------------
        // Fields
        // -----------------------------------------------------------------

        private readonly string _basePath;
        private readonly LogLevel _minimumLevel;
        private readonly RollingPolicy _rollingPolicy;
        private readonly long _maxFileSizeBytes;
        private readonly int _maxArchiveCount;
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        private StreamWriter? _writer;
        private string? _currentFilePath;
        private DateTime _currentDate;
        private long _currentFileLength;
        private bool _disposed;

        // -----------------------------------------------------------------
        // Construction
        // -----------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="basePath">
        /// The directory path or full file path for log files.
        /// If a directory, files are auto-named <c>log-YYYY-MM-DD.txt</c>.
        /// </param>
        /// <param name="minimumLevel">
        /// The minimum <see cref="Logging.LogLevel"/> to log. Entries below this level are ignored.
        /// </param>
        /// <param name="rollingPolicy">
        /// The rolling policy to apply.
        /// </param>
        /// <param name="maxFileSizeBytes">
        /// The maximum file size in bytes before a size-based roll occurs.
        /// Only used when <see cref="RollingPolicy.FileSize"/> or <see cref="RollingPolicy.Combined"/>
        /// is enabled. Defaults to 10 MB.
        /// </param>
        /// <param name="maxArchiveCount">
        /// The maximum number of archived (rolled) files to retain.
        /// Older archives beyond this count are deleted. Defaults to 31.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="basePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="basePath"/> is empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxFileSizeBytes"/> is less than 1024 or
        /// <paramref name="maxArchiveCount"/> is less than 0.
        /// </exception>
        public FileLogger(
            string basePath,
            LogLevel minimumLevel = LogLevel.Information,
            RollingPolicy rollingPolicy = RollingPolicy.None,
            long maxFileSizeBytes = 10 * 1024 * 1024,
            int maxArchiveCount = 31)
        {
            if (basePath is null)
                throw new ArgumentNullException(nameof(basePath));
            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("Path cannot be empty or whitespace.", nameof(basePath));
            if (maxFileSizeBytes < 1024)
                throw new ArgumentOutOfRangeException(nameof(maxFileSizeBytes), maxFileSizeBytes, "Minimum file size is 1024 bytes.");
            if (maxArchiveCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxArchiveCount), maxArchiveCount, "Archive count cannot be negative.");

            _basePath = basePath;
            _minimumLevel = minimumLevel;
            _rollingPolicy = rollingPolicy;
            _maxFileSizeBytes = maxFileSizeBytes;
            _maxArchiveCount = maxArchiveCount;

            // Ensure the directory exists.
            string? dir = Path.GetDirectoryName(GetFullPath());
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            OpenFile(forceNew: false);
        }

        // -----------------------------------------------------------------
        // Public Log Methods
        // -----------------------------------------------------------------

        /// <summary>
        /// Writes a <see cref="LogEntry"/> to the log file if its level meets
        /// the configured minimum. This method never throws.
        /// </summary>
        /// <param name="entry">The log entry to write.</param>
        public void Log(LogEntry entry)
        {
            if (_disposed || entry.Level < _minimumLevel || entry.Level == LogLevel.None)
                return;

            string line = FormatEntry(entry);

            try
            {
                _syncLock.Wait();

                if (_disposed)
                    return;

                CheckRolling();

                if (_writer is null)
                    return;

                _writer.WriteLine(line);
                _currentFileLength += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
            }
            catch
            {
                // Never throw from Log() — silently swallow I/O errors.
            }
            finally
            {
                try { _syncLock.Release(); } catch { }
            }
        }

        /// <summary>
        /// Writes a log entry with the specified parameters.
        /// This method never throws.
        /// </summary>
        /// <param name="level">The severity level of the entry.</param>
        /// <param name="message">The log message text.</param>
        /// <param name="category">An optional category or source name.</param>
        /// <param name="eventId">An optional numeric event identifier.</param>
        /// <param name="exception">An optional exception.</param>
        /// <param name="properties">An optional dictionary of structured properties.</param>
        public void Log(
            LogLevel level,
            string message,
            string? category = null,
            int? eventId = null,
            Exception? exception = null,
            IReadOnlyDictionary<string, object>? properties = null)
        {
            Log(new LogEntry(
                DateTime.Now,
                level,
                message ?? string.Empty,
                category,
                eventId,
                exception,
                properties));
        }

        /// <summary>
        /// Writes a <see cref="LogLevel.Trace"/> entry.
        /// </summary>
        public void Trace(string message, string? category = null, int? eventId = null)
            => Log(new LogEntry(DateTime.Now, LogLevel.Trace, message ?? string.Empty, category, eventId));

        /// <summary>
        /// Writes a <see cref="LogLevel.Debug"/> entry.
        /// </summary>
        public void Debug(string message, string? category = null, int? eventId = null)
            => Log(new LogEntry(DateTime.Now, LogLevel.Debug, message ?? string.Empty, category, eventId));

        /// <summary>
        /// Writes a <see cref="LogLevel.Information"/> entry.
        /// </summary>
        public void Information(string message, string? category = null, int? eventId = null)
            => Log(new LogEntry(DateTime.Now, LogLevel.Information, message ?? string.Empty, category, eventId));

        /// <summary>
        /// Writes a <see cref="LogLevel.Warning"/> entry.
        /// </summary>
        public void Warning(string message, string? category = null, int? eventId = null)
            => Log(new LogEntry(DateTime.Now, LogLevel.Warning, message ?? string.Empty, category, eventId));

        /// <summary>
        /// Writes a <see cref="LogLevel.Error"/> entry.
        /// </summary>
        public void Error(string message, string? category = null, int? eventId = null, Exception? exception = null)
            => Log(new LogEntry(DateTime.Now, LogLevel.Error, message ?? string.Empty, category, eventId, exception));

        /// <summary>
        /// Writes a <see cref="LogLevel.Critical"/> entry.
        /// </summary>
        public void Critical(string message, string? category = null, int? eventId = null, Exception? exception = null)
            => Log(new LogEntry(DateTime.Now, LogLevel.Critical, message ?? string.Empty, category, eventId, exception));

        // -----------------------------------------------------------------
        // Flush
        // -----------------------------------------------------------------

        /// <summary>
        /// Flushes all buffered data to the underlying file synchronously.
        /// This method never throws.
        /// </summary>
        public void Flush()
        {
            if (_disposed)
                return;

            try
            {
                _syncLock.Wait();
                _writer?.Flush();
            }
            catch
            {
                // Silently swallow.
            }
            finally
            {
                try { _syncLock.Release(); } catch { }
            }
        }

        /// <summary>
        /// Flushes all buffered data to the underlying file asynchronously.
        /// This method never throws.
        /// </summary>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public async Task FlushAsync()
        {
            if (_disposed)
                return;

            try
            {
                await _syncLock.WaitAsync().ConfigureAwait(false);

                if (_writer is not null)
                    await _writer.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                // Silently swallow.
            }
            finally
            {
                try { _syncLock.Release(); } catch { }
            }
        }

        // -----------------------------------------------------------------
        // IDisposable
        // -----------------------------------------------------------------

        /// <summary>
        /// Releases all resources used by the <see cref="FileLogger"/>.
        /// Flushes remaining data before closing the file.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the managed and unmanaged resources used by the <see cref="FileLogger"/>.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _syncLock.Wait();
                try
                {
                    CloseWriter();
                }
                finally
                {
                    try { _syncLock.Release(); } catch { }
                }

                _syncLock.Dispose();
            }

            _disposed = true;
        }

        // -----------------------------------------------------------------
        // Rolling Logic
        // -----------------------------------------------------------------

        /// <summary>
        /// Checks whether rolling conditions are met and performs a roll if necessary.
        /// Must be called inside <see cref="_syncLock"/>.
        /// </summary>
        private void CheckRolling()
        {
            bool shouldRoll = false;

            if ((_rollingPolicy & RollingPolicy.Daily) == RollingPolicy.Daily)
            {
                DateTime now = DateTime.Now;
                if (now.Date != _currentDate.Date)
                {
                    shouldRoll = true;
                }
            }

            if (!shouldRoll && (_rollingPolicy & RollingPolicy.FileSize) == RollingPolicy.FileSize)
            {
                if (_currentFileLength >= _maxFileSizeBytes)
                {
                    shouldRoll = true;
                }
            }

            if (shouldRoll)
            {
                CloseWriter();
                ArchiveCurrentFile();
                OpenFile(forceNew: true);
                EnforceArchiveLimit();
            }
        }

        /// <summary>
        /// Archives the current file by renaming it with a numeric suffix.
        /// For daily rolling, the date is embedded in the filename already,
        /// so archiving adds ".1", ".2" etc.
        /// Must be called inside <see cref="_syncLock"/>.
        /// </summary>
        private void ArchiveCurrentFile()
        {
            if (_currentFilePath is null || !File.Exists(_currentFilePath))
                return;

            try
            {
                // Shift existing archives: .n → .n+1
                ShiftArchives(_currentFilePath);

                // Rename current file to .1
                string archivePath = _currentFilePath + ".1";
                if (File.Exists(archivePath))
                    File.Delete(archivePath);

                File.Move(_currentFilePath, archivePath);
            }
            catch
            {
                // Best-effort archiving.
            }
        }

        /// <summary>
        /// Shifts archive suffixes so that .1 becomes .2, .2 becomes .3, etc.
        /// Must be called inside <see cref="_syncLock"/>.
        /// </summary>
        /// <param name="baseFilePath">The base file path (without archive suffix).</param>
        private void ShiftArchives(string baseFilePath)
        {
            for (int i = _maxArchiveCount + 10; i >= 1; i--)
            {
                string oldPath = baseFilePath + "." + i;
                if (File.Exists(oldPath))
                {
                    string newPath = baseFilePath + "." + (i + 1);
                    try
                    {
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(oldPath, newPath);
                    }
                    catch
                    {
                        // Best effort.
                    }
                }
            }
        }

        /// <summary>
        /// Removes archived files beyond <see cref="_maxArchiveCount"/>.
        /// Must be called inside <see cref="_syncLock"/>.
        /// </summary>
        private void EnforceArchiveLimit()
        {
            if (_maxArchiveCount <= 0 || _currentFilePath is null)
                return;

            try
            {
                string? dir = Path.GetDirectoryName(_currentFilePath);
                if (dir is null)
                    return;

                string baseName = Path.GetFileNameWithoutExtension(_currentFilePath);
                string ext = Path.GetExtension(_currentFilePath);

                // Gather all archive files matching our pattern.
                var archiveFiles = Directory.EnumerateFiles(dir, baseName + ext + ".*")
                    .Select(f => new
                    {
                        Path = f,
                        Number = int.TryParse(Path.GetExtension(f)?.TrimStart('.'), out var n) ? n : 0,
                        LastWrite = File.GetLastWriteTimeUtc(f),
                    })
                    .Where(f => f.Number > 0)
                    .OrderByDescending(f => f.Number)
                    .ThenByDescending(f => f.LastWrite)
                    .ToList();

                while (archiveFiles.Count > _maxArchiveCount)
                {
                    var oldest = archiveFiles[archiveFiles.Count - 1];
                    try
                    {
                        File.Delete(oldest.Path);
                    }
                    catch
                    {
                        // Best effort.
                    }

                    archiveFiles.RemoveAt(archiveFiles.Count - 1);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        // -----------------------------------------------------------------
        // File Management
        // -----------------------------------------------------------------

        /// <summary>
        /// Opens or re-opens the log file for writing.
        /// Must be called inside <see cref="_syncLock"/> for the initial open,
        /// but CheckRolling also calls it from within the lock.
        /// </summary>
        /// <param name="forceNew"><see langword="true"/> to truncate/create a new file.</param>
        private void OpenFile(bool forceNew)
        {
            try
            {
                string filePath = ResolveFilePath();

                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileMode = forceNew ? FileMode.Create : FileMode.Append;

                var stream = new FileStream(
                    filePath,
                    fileMode,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: false);

                _writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096)
                {
                    AutoFlush = false,
                };

                _currentFilePath = filePath;
                _currentDate = DateTime.Now.Date;

                // Seed the length from the existing file.
                if (!forceNew && File.Exists(filePath))
                {
                    try
                    {
                        _currentFileLength = new FileInfo(filePath).Length;
                    }
                    catch
                    {
                        _currentFileLength = 0;
                    }
                }
                else
                {
                    _currentFileLength = 0;
                }
            }
            catch
            {
                // If we cannot open the file, degrade gracefully.
                _writer = null;
                _currentFilePath = null;
                _currentFileLength = 0;
            }
        }

        /// <summary>
        /// Closes the current writer and underlying stream.
        /// Must be called inside <see cref="_syncLock"/>.
        /// </summary>
        private void CloseWriter()
        {
            if (_writer is null)
                return;

            try
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
            }
            catch
            {
                // Best-effort close.
            }
            finally
            {
                _writer = null;
            }
        }

        /// <summary>
        /// Resolves the full file path based on the current rolling policy and date.
        /// </summary>
        /// <returns>The resolved file path.</returns>
        private string ResolveFilePath()
        {
            string fullPath = GetFullPath();

            bool hasExtension = !string.IsNullOrEmpty(Path.GetExtension(fullPath));
            bool isDirectory = false;

            try
            {
                isDirectory = Directory.Exists(fullPath)
                    || (!hasExtension && !fullPath.EndsWith(".", StringComparison.Ordinal));
            }
            catch
            {
                isDirectory = !hasExtension;
            }

            if (isDirectory)
            {
                // Treat as directory — auto-name the file.
                string datePart = DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);
                return Path.Combine(fullPath, $"log-{datePart}.txt");
            }

            if ((_rollingPolicy & RollingPolicy.Daily) == RollingPolicy.Daily)
            {
                // Insert the date before the extension.
                string datePart = DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);
                string? dir = Path.GetDirectoryName(fullPath);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                string ext = Path.GetExtension(fullPath);

                string fileName = $"{nameWithoutExt}-{datePart}{ext}";
                return string.IsNullOrEmpty(dir)
                    ? fileName
                    : Path.Combine(dir, fileName);
            }

            return fullPath;
        }

        /// <summary>
        /// Gets the absolute path for <see cref="_basePath"/>.
        /// </summary>
        private string GetFullPath()
        {
            try
            {
                return Path.IsPathRooted(_basePath)
                    ? _basePath
                    : Path.Combine(AppContext.BaseDirectory, _basePath);
            }
            catch
            {
                return _basePath;
            }
        }

        // -----------------------------------------------------------------
        // Formatting
        // -----------------------------------------------------------------

        /// <summary>
        /// Formats a <see cref="LogEntry"/> as a text line to be written to the log file.
        /// </summary>
        private static string FormatEntry(LogEntry entry)
        {
            return entry.ToString();
        }
    }
}
