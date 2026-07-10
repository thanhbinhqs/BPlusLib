// <copyright file="LoggerFactory.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Provides a central point for creating, configuring, and managing
    /// <see cref="FileLogger"/> instances. Loggers are cached by name
    /// and shared across callers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All <see cref="FileLogger"/> instances created by the factory inherit
    /// the global minimum log level set via <see cref="Configure"/> or
    /// <see cref="SetGlobalLogLevel"/>. The factory is thread-safe.
    /// </para>
    /// <para>
    /// Call <see cref="Shutdown"/> before application exit to flush and
    /// dispose all cached loggers gracefully.
    /// </para>
    /// </remarks>
    public static class LoggerFactory
    {
        // -----------------------------------------------------------------
        // Fields
        // -----------------------------------------------------------------

        private static readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new();

        private static LogLevel _globalMinimumLevel = LogLevel.Information;
        private static string? _defaultBasePath;
        private static RollingPolicy _defaultRollingPolicy = RollingPolicy.None;
        private static long _defaultMaxFileSizeBytes = 10 * 1024 * 1024;
        private static int _defaultMaxArchiveCount = 31;

        // -----------------------------------------------------------------
        // Configuration
        // -----------------------------------------------------------------

        /// <summary>
        /// Configures the default settings for all loggers created after
        /// this call. Existing loggers are not affected.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level to apply globally.</param>
        /// <param name="basePath">
        /// The default directory or file path for log output. May be <see langword="null"/>
        /// to keep the current default.
        /// </param>
        /// <param name="rollingPolicy">The rolling policy to use by default.</param>
        /// <param name="maxFileSizeBytes">
        /// The maximum file size in bytes before size-based rolling occurs.
        /// Only applies when <paramref name="rollingPolicy"/> includes <see cref="RollingPolicy.FileSize"/>.
        /// </param>
        /// <param name="maxArchiveCount">
        /// The maximum number of archived files to retain.
        /// </param>
        public static void Configure(
            LogLevel minimumLevel = LogLevel.Information,
            string? basePath = null,
            RollingPolicy rollingPolicy = RollingPolicy.None,
            long maxFileSizeBytes = 10 * 1024 * 1024,
            int maxArchiveCount = 31)
        {
            lock (_lock)
            {
                _globalMinimumLevel = minimumLevel;
                _defaultRollingPolicy = rollingPolicy;
                _defaultMaxFileSizeBytes = maxFileSizeBytes;
                _defaultMaxArchiveCount = maxArchiveCount;

                if (basePath is not null)
                {
                    _defaultBasePath = basePath;
                }

                if (_defaultBasePath is null)
                {
                    _defaultBasePath = System.IO.Path.Combine(
                        AppContext.BaseDirectory,
                        "logs");
                }
            }
        }

        /// <summary>
        /// Sets the global minimum log level. All loggers (existing and
        /// future) are affected because each logger independently filters
        /// entries against its own minimum level. However, existing loggers
        /// were created with their own per-instance minimum level, so this
        /// only affects loggers created <em>after</em> this call.
        /// </summary>
        /// <param name="level">The new global minimum log level.</param>
        public static void SetGlobalLogLevel(LogLevel level)
        {
            lock (_lock)
            {
                _globalMinimumLevel = level;
            }
        }

        /// <summary>
        /// Gets the current global minimum log level.
        /// </summary>
        public static LogLevel GlobalMinimumLevel
        {
            get
            {
                lock (_lock)
                {
                    return _globalMinimumLevel;
                }
            }
        }

        // -----------------------------------------------------------------
        // Logger Retrieval
        // -----------------------------------------------------------------

        /// <summary>
        /// Gets or creates a <see cref="FileLogger"/> with the specified name.
        /// If a logger with that name already exists, the cached instance is returned.
        /// </summary>
        /// <param name="name">The name of the logger (used as the log file name or category).</param>
        /// <returns>A <see cref="FileLogger"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
        public static FileLogger GetLogger(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Logger name cannot be empty or whitespace.", nameof(name));

            return _loggers.GetOrAdd(name, CreateLogger);
        }

        /// <summary>
        /// Gets or creates a <see cref="FileLogger"/> named after the specified type.
        /// </summary>
        /// <typeparam name="T">The type whose full name will be used as the logger name.</typeparam>
        /// <returns>A <see cref="FileLogger"/> instance.</returns>
        public static FileLogger GetLogger<T>()
        {
            string name = typeof(T).FullName ?? typeof(T).Name;
            return GetLogger(name);
        }

        /// <summary>
        /// Gets or creates a <see cref="FileLogger"/> for the type of the
        /// specified instance.
        /// </summary>
        /// <param name="instance">The instance whose type will be used as the logger name.</param>
        /// <returns>A <see cref="FileLogger"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
        public static FileLogger GetLogger(object instance)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            return GetLogger(instance.GetType());
        }

        // -----------------------------------------------------------------
        // Shutdown
        // -----------------------------------------------------------------

        /// <summary>
        /// Flushes and disposes all cached loggers and clears the logger cache.
        /// Call this method once on application shutdown.
        /// </summary>
        /// <remarks>
        /// After calling <see cref="Shutdown"/>, the factory can still create new
        /// loggers if <see cref="GetLogger(string)"/> is called again.
        /// </remarks>
        public static void Shutdown()
        {
            List<Exception> exceptions = new();

            foreach (var kvp in _loggers.ToArray())
            {
                try
                {
                    kvp.Value.Flush();
                    kvp.Value.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            _loggers.Clear();

            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "One or more errors occurred during logger shutdown. See inner exceptions for details.",
                    exceptions);
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Returns a snapshot of all currently registered logger names.
        /// </summary>
        /// <returns>A collection of logger names.</returns>
        public static IReadOnlyCollection<string> GetLoggerNames()
        {
            return _loggers.Keys.ToArray();
        }

        // -----------------------------------------------------------------
        // Private Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Creates a new <see cref="FileLogger"/> with the given name.
        /// </summary>
        /// <param name="name">The logger name, used as the log file name.</param>
        /// <returns>A new <see cref="FileLogger"/> instance.</returns>
        private static FileLogger CreateLogger(string name)
        {
            string basePath;
            lock (_lock)
            {
                basePath = _defaultBasePath
                    ?? System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
            }

            // Sanitise the name for use as a filename.
            string sanitised = SanitiseFileName(name);
            string filePath = System.IO.Path.Combine(basePath, sanitised + ".log");

            LogLevel level;
            RollingPolicy policy;
            long maxSize;
            int maxArchives;

            lock (_lock)
            {
                level = _globalMinimumLevel;
                policy = _defaultRollingPolicy;
                maxSize = _defaultMaxFileSizeBytes;
                maxArchives = _defaultMaxArchiveCount;
            }

            return new FileLogger(
                filePath,
                minimumLevel: level,
                rollingPolicy: policy,
                maxFileSizeBytes: maxSize,
                maxArchiveCount: maxArchives);
        }

        /// <summary>
        /// Removes characters that are invalid in file names from the
        /// specified string.
        /// </summary>
        /// <param name="name">The raw name to sanitise.</param>
        /// <returns>A file-system-safe string.</returns>
        private static string SanitiseFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var chars = name.ToCharArray();
            var result = new System.Text.StringBuilder(chars.Length);

            foreach (char c in chars)
            {
                if (Array.IndexOf(invalid, c) < 0)
                    result.Append(c);
                else
                    result.Append('_');
            }

            string sanitised = result.ToString().TrimEnd('.').TrimEnd();

            return string.IsNullOrWhiteSpace(sanitised) ? "Logger" : sanitised;
        }
    }
}
