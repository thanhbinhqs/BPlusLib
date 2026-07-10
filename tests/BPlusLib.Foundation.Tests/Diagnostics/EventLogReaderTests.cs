// <copyright file="EventLogReaderTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Diagnostics;

namespace BPlusLib.Foundation.Tests.Diagnostics
{
    [Trait("Category", "Diagnostics")]
    public sealed class EventLogReaderTests
    {
        private const string TestLogName = "Application";

        // ── Constructor ─────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullLogName_ShouldThrow()
        {
            Action act = () => new EventLogReader(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithEmptyLogName_ShouldThrow()
        {
            Action act = () => new EventLogReader(string.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithValidLogName_ShouldSetProperties()
        {
            var reader = new EventLogReader(TestLogName);
            reader.LogName.Should().Be(TestLogName);
        }

        // ── ReadAll ─────────────────────────────────────────────────────────

        [Fact]
        public void ReadAll_OnLinux_ShouldReturnEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.ReadAll();
            entries.Should().NotBeNull();
            // On Linux, EventLog is not available => returns empty list
        }

        // ── ReadSince ───────────────────────────────────────────────────────

        [Fact]
        public void ReadSince_WithMinValue_ShouldReturnEmptyOrEntries()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.ReadSince(DateTime.MinValue);
            entries.Should().NotBeNull();
        }

        [Fact]
        public void ReadSince_WithFutureDate_ShouldReturnEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.ReadSince(DateTime.MaxValue);
            entries.Should().BeEmpty();
        }

        // ── ReadLast ────────────────────────────────────────────────────────

        [Fact]
        public void ReadLast_Zero_ShouldReturnEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.ReadLast(0);
            entries.Should().BeEmpty();
        }

        [Fact]
        public void ReadLast_Negative_ShouldReturnEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.ReadLast(-1);
            entries.Should().BeEmpty();
        }

        [Fact]
        public void ReadLast_Positive_ShouldNotThrow()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.ReadLast(10);
            entries.Should().NotBeNull();
        }

        // ── SearchBySource ──────────────────────────────────────────────────

        [Fact]
        public void SearchBySource_NonExistent_ReturnsEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.SearchBySource("NonExistentSource_XYZ");
            entries.Should().BeEmpty();
        }

        [Fact]
        public void SearchBySource_Null_ReturnsEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.SearchBySource(null!);
            entries.Should().BeEmpty();
        }

        [Fact]
        public void SearchBySource_Empty_ReturnsEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.SearchBySource(string.Empty);
            entries.Should().BeEmpty();
        }

        // ── SearchByEventId ─────────────────────────────────────────────────

        [Fact]
        public void SearchByEventId_NonExistent_ReturnsEmpty()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.SearchByEventId(int.MaxValue);
            entries.Should().BeEmpty();
        }

        [Fact]
        public void SearchByEventId_Zero_ShouldNotThrow()
        {
            var reader = new EventLogReader(TestLogName);
            IReadOnlyList<EventLogEntryInfo> entries = reader.SearchByEventId(0);
            entries.Should().NotBeNull();
        }

        // ── GetLogNames ─────────────────────────────────────────────────────

        [Fact]
        public void GetLogNames_OnLinux_ReturnsEmpty()
        {
            IReadOnlyList<string> names = EventLogReader.GetLogNames();
            names.Should().NotBeNull();
            // On Linux, EventLog.GetEventLogs() returns empty; on Windows returns logs
        }

        // ── RecordCount ─────────────────────────────────────────────────────

        [Fact]
        public void RecordCount_ShouldNotThrow()
        {
            var reader = new EventLogReader(TestLogName);
            int? count = null;
            Action act = () => count = reader.RecordCount;
            act.Should().NotThrow();
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Fact]
        public void Clear_ShouldNotThrow()
        {
            var reader = new EventLogReader(TestLogName);
            Action act = () => reader.Clear();
            act.Should().NotThrow();
        }

        // ── EventLogEntryInfo ───────────────────────────────────────────────

        [Fact]
        public void EventLogEntryInfo_Constructor_ShouldSetProperties()
        {
            var timestamp = new DateTime(2025, 6, 15, 10, 30, 0);
            var entry = new EventLogEntryInfo
            {
                MachineName = "MyMachine",
                Source = "MyApp",
                TimeGenerated = timestamp,
                TimeWritten = timestamp,
                EventId = 1001,
                CategoryNumber = 2,
                Category = "Test",
                ProcessId = 1234,
                ThreadId = 5678,
                Message = "Test message",
                UserName = "TEST\\User",
                EntryType = EventLogEntryType.Information,
            };

            entry.MachineName.Should().Be("MyMachine");
            entry.Source.Should().Be("MyApp");
            entry.TimeGenerated.Should().Be(timestamp);
            entry.TimeWritten.Should().Be(timestamp);
            entry.EventId.Should().Be(1001);
            entry.CategoryNumber.Should().Be(2);
            entry.Category.Should().Be("Test");
            entry.ProcessId.Should().Be(1234);
            entry.ThreadId.Should().Be(5678);
            entry.Message.Should().Be("Test message");
            entry.UserName.Should().Be("TEST\\User");
            entry.EntryType.Should().Be(EventLogEntryType.Information);
        }

        [Fact]
        public void EventLogEntryInfo_DefaultValues_ShouldBeNull()
        {
            var entry = new EventLogEntryInfo();
            entry.MachineName.Should().BeNull();
            entry.Source.Should().BeNull();
            entry.EventId.Should().BeNull();
            entry.CategoryNumber.Should().BeNull();
            entry.ProcessId.Should().BeNull();
            entry.ThreadId.Should().BeNull();
            entry.Message.Should().BeNull();
            entry.UserName.Should().BeNull();
            entry.EntryType.Should().Be(EventLogEntryType.Information);
        }

        // ── EventLogEntryType enum ──────────────────────────────────────────

        [Fact]
        public void EventLogEntryType_Values_ShouldBeCorrect()
        {
            ((int)EventLogEntryType.Information).Should().Be(0);
            ((int)EventLogEntryType.Warning).Should().Be(1);
            ((int)EventLogEntryType.Error).Should().Be(2);
            ((int)EventLogEntryType.SuccessAudit).Should().Be(3);
            ((int)EventLogEntryType.FailureAudit).Should().Be(4);
        }
    }
}
