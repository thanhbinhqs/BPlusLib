// <copyright file="LogEntryTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Logging;

namespace BPlusLib.Foundation.Tests.Logging
{
    [Trait("Category", "Logging")]
    public sealed class LogEntryTests
    {
        // ── Constructor ─────────────────────────────────────────────────────

        [Fact]
        public void Constructor_ShouldSetAllProperties()
        {
            var timestamp = new DateTime(2025, 6, 15, 14, 30, 0, 123);
            var exception = new InvalidOperationException("test error");
            var properties = new Dictionary<string, object> { { "key1", "value1" } };

            var entry = new LogEntry(
                timestamp,
                LogLevel.Warning,
                "Test message",
                "MyCategory",
                42,
                exception,
                properties);

            entry.Timestamp.Should().Be(timestamp);
            entry.Level.Should().Be(LogLevel.Warning);
            entry.Message.Should().Be("Test message");
            entry.Category.Should().Be("MyCategory");
            entry.EventId.Should().Be(42);
            entry.Exception.Should().BeSameAs(exception);
            entry.Properties.Should().BeSameAs(properties);
        }

        [Fact]
        public void Constructor_WithNullMessage_ShouldUseEmpty()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Information, null!);
            entry.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void Constructor_WithOnlyRequiredParams_ShouldSetDefaults()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Information, "Hello");

            entry.Category.Should().BeNull();
            entry.EventId.Should().BeNull();
            entry.Exception.Should().BeNull();
            entry.Properties.Should().BeNull();
        }

        // ── ToString ────────────────────────────────────────────────────────

        [Fact]
        public void ToString_ShouldFormatCorrectly()
        {
            var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, 123);
            var entry = new LogEntry(timestamp, LogLevel.Information, "Test message", "MyApp");

            string str = entry.ToString();
            str.Should().Be("2025-06-15 10:30:00.123 [INFO] (MyApp) Test message");
        }

        [Fact]
        public void ToString_WithoutCategory_ShouldOmitCategory()
        {
            var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Warning, "Something odd");

            string str = entry.ToString();
            str.Should().Be("2025-06-15 10:30:00.000 [WARN] Something odd");
        }

        [Fact]
        public void ToString_WithEventId_ShouldIncludeEventId()
        {
            var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Error, "Failed", "App", 1001);

            string str = entry.ToString();
            str.Should().Be("2025-06-15 10:30:00.000 [ERROR] (App) [1001] Failed");
        }

        [Fact]
        public void ToString_WithException_ShouldIncludeException()
        {
            var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, 0);
            var ex = new InvalidOperationException("Something broke");
            var entry = new LogEntry(timestamp, LogLevel.Critical, "Fatal", "App", null, ex);

            string str = entry.ToString();
            str.Should().Contain("InvalidOperationException");
            str.Should().Contain("Something broke");
        }

        [Fact]
        public void ToString_TraceLevel_ShouldUseTraceLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Trace, "detail");
            entry.ToString().Should().Contain("[TRACE]");
        }

        [Fact]
        public void ToString_DebugLevel_ShouldUseDebugLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Debug, "debug");
            entry.ToString().Should().Contain("[DEBUG]");
        }

        [Fact]
        public void ToString_InfoLevel_ShouldUseInfoLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Information, "info");
            entry.ToString().Should().Contain("[INFO]");
        }

        [Fact]
        public void ToString_WarningLevel_ShouldUseWarnLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Warning, "warn");
            entry.ToString().Should().Contain("[WARN]");
        }

        [Fact]
        public void ToString_ErrorLevel_ShouldUseErrorLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Error, "error");
            entry.ToString().Should().Contain("[ERROR]");
        }

        [Fact]
        public void ToString_CriticalLevel_ShouldUseCritLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Critical, "critical");
            entry.ToString().Should().Contain("[CRIT]");
        }

        [Fact]
        public void ToString_NoneLevel_ShouldUseNoneLabel()
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.None, "none");
            entry.ToString().Should().Contain("[NONE]");
        }

        // ── LogLevel enum values ────────────────────────────────────────────

        [Fact]
        public void LogLevel_Values_ShouldBeCorrect()
        {
            ((int)LogLevel.Trace).Should().Be(0);
            ((int)LogLevel.Debug).Should().Be(1);
            ((int)LogLevel.Information).Should().Be(2);
            ((int)LogLevel.Warning).Should().Be(3);
            ((int)LogLevel.Error).Should().Be(4);
            ((int)LogLevel.Critical).Should().Be(5);
            ((int)LogLevel.None).Should().Be(6);
        }
    }
}
