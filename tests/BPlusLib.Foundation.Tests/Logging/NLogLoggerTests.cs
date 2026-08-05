using System;
using System.IO;
using FluentAssertions;
using BPlusLib.Foundation.Logging;
using Xunit;

namespace BPlusLib.Foundation.Tests.Logging
{
    public class NLogLoggerTests : IDisposable
    {
        private readonly string _tempDir;

        public NLogLoggerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "BPlusLib_NLogTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void Info_LogsToFile()
        {
            var logPath = Path.Combine(_tempDir, "test.log");
            using var logger = new NLogLogger(logPath, NLog.LogLevel.Debug);
            logger.Info("Test message");
            File.Exists(logPath).Should().BeTrue();
            File.ReadAllText(logPath).Should().Contain("INFO").And.Contain("Test message");
        }

        [Fact]
        public void Error_LogsException()
        {
            var logPath = Path.Combine(_tempDir, "error.log");
            using var logger = new NLogLogger(logPath, NLog.LogLevel.Debug);
            logger.Error("Something failed", new InvalidOperationException("boom"));
            var content = File.ReadAllText(logPath);
            content.Should().Contain("ERROR").And.Contain("Something failed").And.Contain("boom");
        }

        [Fact]
        public void MinLevel_FiltersLogs()
        {
            var logPath = Path.Combine(_tempDir, "filtered.log");
            using var logger = new NLogLogger(logPath, NLog.LogLevel.Warn);
            logger.Debug("should not appear");
            logger.Info("should not appear");
            logger.Warn("should appear");
            logger.Error("should appear");
            var content = File.ReadAllText(logPath);
            content.Should().NotContain("should not appear");
            content.Should().Contain("should appear");
        }

        [Fact]
        public void Factory_CreateFileOnly()
        {
            var logPath = Path.Combine(_tempDir, "factory.log");
            using var logger = RichTextBoxLoggerFactory.CreateFileOnly(logPath, NLog.LogLevel.Debug);
            logger.Info("Factory test");
            File.Exists(logPath).Should().BeTrue();
            File.ReadAllText(logPath).Should().Contain("Factory test");
        }
    }
}
