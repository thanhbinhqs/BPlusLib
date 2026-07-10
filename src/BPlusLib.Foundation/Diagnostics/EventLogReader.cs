// <copyright file="EventLogReader.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#if NET472
using System.Diagnostics;
#else
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
#endif

namespace BPlusLib.Foundation.Diagnostics
{
    // =====================================================================
    // Enums
    // =====================================================================

    /// <summary>
    /// Defines the type of event log entry.
    /// </summary>
    public enum EventLogEntryType
    {
        /// <summary>An informational event.</summary>
        Information,

        /// <summary>A warning event.</summary>
        Warning,

        /// <summary>An error event.</summary>
        Error,

        /// <summary>An audit success event (security log).</summary>
        SuccessAudit,

        /// <summary>An audit failure event (security log).</summary>
        FailureAudit,
    }

    // =====================================================================
    // Model class
    // =====================================================================

    /// <summary>
    /// Represents a single entry from an event log with all available
    /// metadata, independent of the underlying retrieval API.
    /// </summary>
    public sealed class EventLogEntryInfo
    {
        /// <summary>
        /// Gets the name of the machine that generated this entry.
        /// </summary>
        public string? MachineName { get; init; }

        /// <summary>
        /// Gets the source (provider) name that wrote the entry.
        /// </summary>
        public string? Source { get; init; }

        /// <summary>
        /// Gets the local time at which the event was generated.
        /// </summary>
        public DateTime TimeGenerated { get; init; }

        /// <summary>
        /// Gets the local time at which the event was written to the log.
        /// </summary>
        public DateTime TimeWritten { get; init; }

        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        public int? EventId { get; init; }

        /// <summary>
        /// Gets the task category number.
        /// </summary>
        public int? CategoryNumber { get; init; }

        /// <summary>
        /// Gets the task category string.
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        /// Gets the process identifier that wrote the entry, if available.
        /// </summary>
        public int? ProcessId { get; init; }

        /// <summary>
        /// Gets the thread identifier that wrote the entry, if available.
        /// </summary>
        public int? ThreadId { get; init; }

        /// <summary>
        /// Gets the event message text.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Gets the user name associated with the entry, if available.
        /// </summary>
        public string? UserName { get; init; }

        /// <summary>
        /// Gets the type (severity classification) of this entry.
        /// </summary>
        public EventLogEntryType EntryType { get; init; }
    }

    // =====================================================================
    // EventLogReader
    // =====================================================================

    /// <summary>
    /// Provides read access to Windows Event Logs. Uses the modern
    /// <c>System.Diagnostics.Eventing.Reader</c> API on .NET 6+ and falls
    /// back to the legacy <c>System.Diagnostics.EventLog</c> API on
    /// .NET Framework 4.7.2.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All methods are thread-safe and wrap failures gracefully, returning
    /// empty lists or <c>null</c> instead of throwing exceptions.
    /// </para>
    /// <para>
    /// On non-Windows platforms, <see cref="GetLogNames"/> may return an
    /// empty list, and all other methods will return empty results
    /// gracefully.
    /// </para>
    /// </remarks>
    public sealed class EventLogReader
    {
        // -----------------------------------------------------------------
        // Fields
        // -----------------------------------------------------------------

        private readonly string _logName;
        private readonly string? _machineName;

        // -----------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogReader"/> class
        /// targeting the specified event log.
        /// </summary>
        /// <param name="logName">The name of the event log (e.g. "Application", "System", "Security").</param>
        /// <param name="machineName">
        /// The name of the remote machine, or <see langword="null"/> for the local machine.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logName"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="logName"/> is empty.</exception>
        public EventLogReader(string logName, string? machineName = null)
        {
#if NET472
            if (logName is null)
                throw new ArgumentNullException(nameof(logName));
#else
            ArgumentNullException.ThrowIfNull(logName);
#endif

            if (logName.Length == 0)
                throw new ArgumentException("Log name cannot be empty.", nameof(logName));

            _logName = logName;
            _machineName = machineName;
        }

        // -----------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------

        /// <summary>
        /// Gets the name of the event log this instance reads from.
        /// </summary>
        public string LogName => _logName;

        /// <summary>
        /// Gets the number of records currently in the event log,
        /// or <see langword="null"/> if the count could not be determined.
        /// </summary>
        public int? RecordCount
        {
            get
            {
                try
                {
#if NET472
                    using (var el = new EventLog(_logName, _machineName ?? "."))
                    {
                        return el.Entries.Count;
                    }
#else
                    if (!IsWindows())
                        return null;

                    using (var el = new System.Diagnostics.EventLog(_logName, _machineName ?? "."))
                    {
                        return el.Entries.Count;
                    }
#endif
                }
                catch
                {
                    return null;
                }
            }
        }

        // -----------------------------------------------------------------
        // Public methods
        // -----------------------------------------------------------------

        /// <summary>
        /// Reads all entries from the event log.
        /// </summary>
        /// <returns>A read-only list of event log entries, or an empty list on failure.</returns>
        public IReadOnlyList<EventLogEntryInfo> ReadAll()
        {
            return ReadEntriesCore(null, null, null, null);
        }

        /// <summary>
        /// Reads entries from the event log that were generated after <paramref name="timestamp"/>.
        /// </summary>
        /// <param name="timestamp">The earliest time (local) to include.</param>
        /// <returns>A read-only list of matching entries, or an empty list on failure.</returns>
        public IReadOnlyList<EventLogEntryInfo> ReadSince(DateTime timestamp)
        {
            return ReadEntriesCore(timestamp, null, null, null);
        }

        /// <summary>
        /// Reads the last <paramref name="count"/> entries from the event log.
        /// </summary>
        /// <param name="count">The maximum number of entries to return.</param>
        /// <returns>A read-only list of matching entries, or an empty list on failure.</returns>
        public IReadOnlyList<EventLogEntryInfo> ReadLast(int count)
        {
            if (count <= 0)
                return Array.Empty<EventLogEntryInfo>();

            return ReadEntriesCore(null, count, null, null);
        }

        /// <summary>
        /// Reads entries from the event log that were written by the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The event source (provider name) to filter by.</param>
        /// <returns>A read-only list of matching entries, or an empty list on failure.</returns>
        public IReadOnlyList<EventLogEntryInfo> SearchBySource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return Array.Empty<EventLogEntryInfo>();

            return ReadEntriesCore(null, null, source, null);
        }

        /// <summary>
        /// Reads entries from the event log with the specified <paramref name="eventId"/>.
        /// </summary>
        /// <param name="eventId">The event ID to filter by.</param>
        /// <returns>A read-only list of matching entries, or an empty list on failure.</returns>
        public IReadOnlyList<EventLogEntryInfo> SearchByEventId(int eventId)
        {
            return ReadEntriesCore(null, null, null, eventId);
        }

        /// <summary>
        /// Clears all entries from this event log.
        /// </summary>
        public void Clear()
        {
            try
            {
#if NET472
                using (var el = new EventLog(_logName, _machineName ?? "."))
                {
                    el.Clear();
                }
#else
                if (!IsWindows())
                    return;

                using var session = string.IsNullOrEmpty(_machineName)
                    ? new EventLogSession()
                    : new EventLogSession(_machineName);

                session.ClearLog(_logName);
#endif
            }
            catch
            {
                // Silently ignore — best-effort clear.
            }
        }

        /// <summary>
        /// Enumerates the names of all event logs available on the system.
        /// </summary>
        /// <returns>A read-only list of log names, or an empty list on failure.</returns>
        public static IReadOnlyList<string> GetLogNames()
        {
            try
            {
#if NET472
                return EventLog.GetEventLogs()
                    .Select(el => el.Log)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList()
                    .AsReadOnly();
#else
                if (!IsWindows())
                    return Array.Empty<string>();

                return EventLogSession.GlobalSession.GetLogNames()
                    .ToList()
                    .AsReadOnly();
#endif
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        // -----------------------------------------------------------------
        // Core read logic
        // -----------------------------------------------------------------

        /// <summary>
        /// Unified entry-reading method. Filters are applied post-retrieval
        /// for simplicity and cross-platform consistency.
        /// </summary>
        private IReadOnlyList<EventLogEntryInfo> ReadEntriesCore(
            DateTime? since,
            int? lastCount,
            string? sourceFilter,
            int? eventIdFilter)
        {
            try
            {
#if NET472
                return ReadEntriesLegacy(since, lastCount, sourceFilter, eventIdFilter);
#else
                if (!IsWindows())
                    return Array.Empty<EventLogEntryInfo>();

                return ReadEntriesModern(since, lastCount, sourceFilter, eventIdFilter);
#endif
            }
            catch
            {
                return Array.Empty<EventLogEntryInfo>();
            }
        }

#if NET472
        // =================================================================
        // .NET Framework 4.7.2 path — System.Diagnostics.EventLog
        // =================================================================

        private IReadOnlyList<EventLogEntryInfo> ReadEntriesLegacy(
            DateTime? since,
            int? lastCount,
            string? sourceFilter,
            int? eventIdFilter)
        {
            var results = new List<EventLogEntryInfo>();

            using (var el = new EventLog(_logName, _machineName ?? "."))
            {
                int totalEntries = el.Entries.Count;
                if (totalEntries == 0)
                    return Array.Empty<EventLogEntryInfo>();

                // Determine range — entries are indexed oldest-first.
                int startIndex = 0;
                int endIndex = totalEntries - 1;

                if (lastCount.HasValue && lastCount.Value > 0 && lastCount.Value < totalEntries)
                {
                    startIndex = totalEntries - lastCount.Value;
                }

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var entry = el.Entries[i];

                    // Apply filters
                    if (since.HasValue && entry.TimeGenerated <= since.Value)
                        continue;

                    if (sourceFilter != null &&
                        !string.Equals(entry.Source, sourceFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (eventIdFilter.HasValue && entry.InstanceId != eventIdFilter.Value)
                        continue;

                    results.Add(new EventLogEntryInfo
                    {
                        MachineName = entry.MachineName,
                        Source = entry.Source,
                        TimeGenerated = entry.TimeGenerated,
                        TimeWritten = entry.TimeWritten,
                        EventId = (int)entry.InstanceId,
                        CategoryNumber = entry.CategoryNumber,
                        Category = entry.Category,
                        ProcessId = null,   // Not available in legacy API
                        ThreadId = null,    // Not available in legacy API
                        Message = entry.Message,
                        UserName = entry.UserName,
                        EntryType = MapEntryType(entry.EntryType),
                    });
                }
            }

            return results.AsReadOnly();
        }

        /// <summary>
        /// Maps the .NET Framework <see cref="System.Diagnostics.EventLogEntryType"/>
        /// to our library-neutral <see cref="EventLogEntryType"/>.
        /// </summary>
        private static EventLogEntryType MapEntryType(System.Diagnostics.EventLogEntryType type)
        {
            return type switch
            {
                System.Diagnostics.EventLogEntryType.Warning => EventLogEntryType.Warning,
                System.Diagnostics.EventLogEntryType.Error => EventLogEntryType.Error,
                System.Diagnostics.EventLogEntryType.SuccessAudit => EventLogEntryType.SuccessAudit,
                System.Diagnostics.EventLogEntryType.FailureAudit => EventLogEntryType.FailureAudit,
                _ => EventLogEntryType.Information,
            };
        }

#else
        // =================================================================
        // Modern path (.NET 6+) — System.Diagnostics.Eventing.Reader
        // =================================================================

        /// <summary>
        /// Keywords for audit success/failure in event records.
        /// </summary>
        private const ulong AuditSuccessKeyword = 0x8020000000000000;
        private const ulong AuditFailureKeyword = 0x8010000000000000;

        private IReadOnlyList<EventLogEntryInfo> ReadEntriesModern(
            DateTime? since,
            int? lastCount,
            string? sourceFilter,
            int? eventIdFilter)
        {
            var results = new List<EventLogEntryInfo>();

            // Build query in reverse chronological order so that "last N"
            // reads can stop early.
            var query = string.IsNullOrEmpty(_machineName)
                ? new EventLogQuery(_logName, PathType.LogName) { ReverseDirection = true }
                : new EventLogQuery(_logName, PathType.LogName, _machineName) { ReverseDirection = true };

            // Use fully qualified name to disambiguate from our own class.
            using var sysReader = new System.Diagnostics.Eventing.Reader.EventLogReader(query);
            int count = 0;

            for (var record = sysReader.ReadEvent(); record != null; record = sysReader.ReadEvent())
            {
                using (record)
                {
                    // Convert inside a per-record try/catch so a single
                    // malformed entry does not lose the entire result set.
                    EventLogEntryInfo? entry;
                    try
                    {
                        entry = ConvertRecord(record);
                    }
                    catch
                    {
                        continue;
                    }

                    // Post-filters
                    if (sourceFilter != null &&
                        !string.Equals(entry.Source, sourceFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (eventIdFilter.HasValue && entry.EventId != eventIdFilter.Value)
                        continue;

                    // For "since", stop when we pass the threshold (records
                    // arrive newest-first, so once TimeGenerated <= since we
                    // can abort — all remaining entries are even older).
                    if (since.HasValue && entry.TimeGenerated <= since.Value)
                    {
                        break;
                    }

                    results.Add(entry);
                    count++;

                    if (lastCount.HasValue && count >= lastCount.Value)
                        break;
                }
            }

            // Results are newest-first; reverse to chronological order
            // except for "last N" which should stay newest-first.
            if (lastCount.HasValue)
            {
                return results.AsReadOnly();
            }

            results.Reverse();
            return results.AsReadOnly();
        }

        /// <summary>
        /// Converts an <see cref="EventRecord"/> to our library-neutral
        /// <see cref="EventLogEntryInfo"/>.
        /// </summary>
        private static EventLogEntryInfo ConvertRecord(EventRecord record)
        {
            var timeCreated = record.TimeCreated ?? DateTime.MinValue;

            string? userName = null;
            try
            {
                if (record.UserId is SecurityIdentifier sid)
                {
                    userName = sid.Translate(typeof(NTAccount)).Value;
                }
            }
            catch
            {
                // Best-effort — some SIDs cannot be resolved.
            }

            string? message = null;
            try
            {
                message = record.FormatDescription();
            }
            catch
            {
                // FormatDescription can throw if the provider is not
                // installed on the current machine.
            }

            return new EventLogEntryInfo
            {
                MachineName = record.MachineName,
                Source = record.ProviderName,
                TimeGenerated = timeCreated,
                TimeWritten = timeCreated,   // EventLogRecord does not separate TimeWritten
                EventId = record.Id,
                CategoryNumber = record.Task,
                Category = record.OpcodeDisplayName,
                ProcessId = record.ProcessId,
                ThreadId = record.ThreadId,
                Message = message,
                UserName = userName,
                EntryType = MapLevelToEntryType(record.Level, (ulong?)record.Keywords),
            };
        }

        /// <summary>
        /// Maps an event <paramref name="level"/> byte and <paramref name="keywords"/>
        /// mask to our library-neutral <see cref="EventLogEntryType"/>.
        /// </summary>
        private static EventLogEntryType MapLevelToEntryType(byte? level, ulong? keywords)
        {
            // Check audit keywords first — these take precedence over level.
            if (keywords.HasValue)
            {
                if ((keywords.Value & AuditSuccessKeyword) == AuditSuccessKeyword)
                    return EventLogEntryType.SuccessAudit;

                if ((keywords.Value & AuditFailureKeyword) == AuditFailureKeyword)
                    return EventLogEntryType.FailureAudit;
            }

            return level switch
            {
                2 /* StandardEventLevel.Error */ => EventLogEntryType.Error,
                3 /* StandardEventLevel.Warning */ => EventLogEntryType.Warning,
                1 /* StandardEventLevel.Critical */ => EventLogEntryType.Error,
                _ => EventLogEntryType.Information,
            };
        }
#endif

        // -----------------------------------------------------------------
        // Platform check
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns <see langword="true"/> when running on the Windows operating
        /// system where the Event Log APIs are available.
        /// </summary>
        private static bool IsWindows()
        {
#if NET472 || NET6_0
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            return OperatingSystem.IsWindows();
#endif
        }
    }
}
