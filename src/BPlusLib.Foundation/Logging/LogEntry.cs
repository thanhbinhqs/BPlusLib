// <copyright file="LogEntry.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Defines the severity level of a log entry.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// The most verbose level, intended for detailed diagnostic tracing.
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Diagnostic messages useful during development and debugging.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Informational messages that track the normal flow of the application.
        /// </summary>
        Information = 2,

        /// <summary>
        /// Indicates a potential problem or something unusual that does not
        /// interrupt normal operation.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Indicates a failure that prevented an operation from completing
        /// but did not crash the application.
        /// </summary>
        Error = 4,

        /// <summary>
        /// Indicates a catastrophic failure that requires immediate attention.
        /// </summary>
        Critical = 5,

        /// <summary>
        /// Special value used to disable all logging. Not a real severity level.
        /// </summary>
        None = 6,
    }

    /// <summary>
    /// Represents a single log entry with structured metadata.
    /// This is an immutable value type.
    /// </summary>
    public readonly struct LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> struct.
        /// </summary>
        /// <param name="timestamp">The time at which the event occurred.</param>
        /// <param name="level">The severity level of the event.</param>
        /// <param name="message">The log message text.</param>
        /// <param name="category">An optional category or source name for the log entry.</param>
        /// <param name="eventId">An optional numeric identifier for the event.</param>
        /// <param name="exception">An optional exception associated with the event.</param>
        /// <param name="properties">An optional dictionary of structured properties.</param>
        public LogEntry(
            DateTime timestamp,
            LogLevel level,
            string message,
            string? category = null,
            int? eventId = null,
            Exception? exception = null,
            IReadOnlyDictionary<string, object>? properties = null)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message ?? string.Empty;
            Category = category;
            EventId = eventId;
            Exception = exception;
            Properties = properties;
        }

        /// <summary>
        /// Gets the time at which the event occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the severity level of the event.
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the log message text. This value is never <see langword="null"/>.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the optional category or source name for the log entry.
        /// </summary>
        public string? Category { get; }

        /// <summary>
        /// Gets the optional numeric identifier for the event.
        /// </summary>
        public int? EventId { get; }

        /// <summary>
        /// Gets the optional exception associated with the event.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets an optional dictionary of structured properties attached to the entry.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Properties { get; }

        /// <summary>
        /// Formats the log entry as a single-line string.
        /// Format: <c>2024-01-15 10:30:00.123 [INFO] (Category) Message</c>
        /// </summary>
        /// <returns>A formatted string representation of the entry.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append(" [");
            sb.Append(Level switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRIT",
                LogLevel.None => "NONE",
                _ => Level.ToString().ToUpperInvariant(),
            });
            sb.Append("]");

            if (Category is not null)
            {
                sb.Append(" (");
                sb.Append(Category);
                sb.Append(")");
            }

            if (EventId.HasValue)
            {
                sb.Append(" [");
                sb.Append(EventId.Value);
                sb.Append("]");
            }

            sb.Append(' ');
            sb.Append(Message);

            if (Exception is not null)
            {
                sb.Append(" | ");
                sb.Append(Exception.GetType().Name);
                sb.Append(": ");
                sb.Append(Exception.Message);
            }

            return sb.ToString();
        }
    }
}
