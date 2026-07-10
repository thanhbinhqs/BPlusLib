// <copyright file="LoggerFactoryTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Logging;

namespace BPlusLib.Foundation.Tests.Logging
{
    [Trait("Category", "Logging")]
    public sealed class LoggerFactoryTests : IDisposable
    {
        private readonly string _tempDir;

        public LoggerFactoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "LoggerFactoryTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            // Shutdown the factory to release all loggers
            try { LoggerFactory.Shutdown(); }
            catch { /* Best effort */ }

            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); }
                catch { /* Best-effort cleanup */ }
            }
        }

        // ── GetLogger ───────────────────────────────────────────────────────

        [Fact]
        public void GetLogger_WithName_ShouldReturnLogger()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            var logger = LoggerFactory.GetLogger("TestLogger");
            logger.Should().NotBeNull();
        }

        [Fact]
        public void GetLogger_WithNullName_ShouldThrow()
        {
            Action act = () => LoggerFactory.GetLogger(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetLogger_WithEmptyName_ShouldThrow()
        {
            Action act = () => LoggerFactory.GetLogger(string.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetLogger_WithWhitespaceName_ShouldThrow()
        {
            Action act = () => LoggerFactory.GetLogger("   ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetLogger_Twice_ShouldReturnSameInstance()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            var logger1 = LoggerFactory.GetLogger("SharedLogger");
            var logger2 = LoggerFactory.GetLogger("SharedLogger");
            logger1.Should().BeSameAs(logger2);
        }

        [Fact]
        public void GetLogger_Generic_ShouldReturnLogger()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            var logger = LoggerFactory.GetLogger<LoggerFactoryTests>();
            logger.Should().NotBeNull();
        }

        [Fact]
        public void GetLogger_WithNullInstance_ShouldThrow()
        {
            object? nullObj = null;
            Action act = () => LoggerFactory.GetLogger(nullObj!);
            act.Should().Throw<ArgumentNullException>();
        }

        // ── Configure ──────────────────────────────────────────────────────

        [Fact]
        public void Configure_ShouldSetDefaults()
        {
            LoggerFactory.Configure(
                minimumLevel: LogLevel.Debug,
                basePath: _tempDir,
                rollingPolicy: RollingPolicy.Daily,
                maxFileSizeBytes: 5 * 1024 * 1024,
                maxArchiveCount: 7);

            LoggerFactory.GlobalMinimumLevel.Should().Be(LogLevel.Debug);
        }

        // ── SetGlobalLogLevel ───────────────────────────────────────────────

        [Fact]
        public void SetGlobalLogLevel_ShouldNotThrow()
        {
            Action act = () => LoggerFactory.SetGlobalLogLevel(LogLevel.Warning);
            act.Should().NotThrow();
        }

        [Fact]
        public void SetGlobalLogLevel_ShouldUpdateGlobalLevel()
        {
            LoggerFactory.SetGlobalLogLevel(LogLevel.Error);
            LoggerFactory.GlobalMinimumLevel.Should().Be(LogLevel.Error);
        }

        // ── GlobalMinimumLevel ──────────────────────────────────────────────

        [Fact]
        public void GlobalMinimumLevel_Default_ShouldBeInformation()
        {
            // Reset by configuring explicitly
            LoggerFactory.Configure();
            LoggerFactory.GlobalMinimumLevel.Should().Be(LogLevel.Information);
        }

        // ── GetLoggerNames ──────────────────────────────────────────────────

        [Fact]
        public void GetLoggerNames_ShouldReturnRegisteredNames()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            LoggerFactory.GetLogger("Name1");
            LoggerFactory.GetLogger("Name2");

            var names = LoggerFactory.GetLoggerNames();
            names.Should().Contain("Name1");
            names.Should().Contain("Name2");
        }

        // ── Shutdown ────────────────────────────────────────────────────────

        [Fact]
        public void Shutdown_ShouldNotThrow()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            LoggerFactory.GetLogger("ShutdownTest");
            Action act = () => LoggerFactory.Shutdown();
            act.Should().NotThrow();
        }

        [Fact]
        public void Shutdown_Twice_ShouldNotThrow()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            LoggerFactory.GetLogger("DualShutdown");
            LoggerFactory.Shutdown();
            Action act = () => LoggerFactory.Shutdown();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetLogger_AfterShutdown_ShouldCreateNewLogger()
        {
            LoggerFactory.Configure(basePath: _tempDir);
            LoggerFactory.GetLogger("BeforeShutdown");
            LoggerFactory.Shutdown();
            var logger = LoggerFactory.GetLogger("AfterShutdown");
            logger.Should().NotBeNull();
        }

        // ── Logger writes to file ───────────────────────────────────────────

        [Fact]
        public void Logger_CreatedByFactory_ShouldWriteToFile()
        {
            LoggerFactory.Configure(basePath: _tempDir, minimumLevel: LogLevel.Trace);
            var logger = LoggerFactory.GetLogger("FactoryWriteTest");
            logger.Information("Factory logger test");
            logger.Flush();

            string logFile = Path.Combine(_tempDir, "FactoryWriteTest.log");
            File.Exists(logFile).Should().BeTrue();
            string content = File.ReadAllText(logFile);
            content.Should().Contain("Factory logger test");
        }
    }
}
