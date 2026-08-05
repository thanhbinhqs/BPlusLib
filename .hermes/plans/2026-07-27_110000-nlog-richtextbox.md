# NLog Logging + RichTextBox Display Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add NLog-based file logging and RichTextBox log display to BPlusLib.Foundation library.

**Architecture:** Create a new `NLogLogger` class that wraps NLog for file logging, and a `RichTextBoxLogTarget` custom NLog target that renders log entries to a WinForms RichTextBox with color coding by log level.

**Tech Stack:** C# 12, NLog 5.x, net472/net6.0/net8.0, WinForms, xUnit + FluentAssertions

---

## Current Context

- Existing logging module: `src/BPlusLib.Foundation/Logging/` (FileLogger, LogEntry, LoggerFactory)
- WinForms support already enabled in csproj (`<UseWindowsForms>true</UseWindowsForms>`)
- Multi-target: net472, net6.0, net8.0
- No NLog dependency yet

---

## Task 1: Add NLog NuGet package

**Objective:** Add NLog as a dependency

**Files:**
- Modify: `src/BPlusLib.Foundation/BPlusLib.Foundation.csproj`

**Step 1: Add PackageReference**
```xml
<ItemGroup>
  <PackageReference Include="NLog" Version="5.3.4" />
</ItemGroup>
```

**Step 2: Verify restore**
```bash
cd /home/binh/BPlusLib && dotnet restore
```
Expected: no errors

---

## Task 2: Create NLogLogger wrapper

**Objective:** Create a simple NLog wrapper for file logging

**Files:**
- Create: `src/BPlusLib/Foundation/Logging/NLogLogger.cs`

**Complete code:**
```csharp
using System;
using NLog;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// NLog-based file logger with configurable log levels and file rolling.
    /// Wraps NLog for simple usage in BPlusLib applications.
    /// </summary>
    public sealed class NLogLogger : IDisposable
    {
        private readonly Logger _logger;
        private bool _disposed;

        /// <summary>
        /// Creates a new NLogLogger with default configuration (file target).
        /// </summary>
        /// <param name="logFilePath">Path to the log file. Default: "./logs/app.log"</param>
        /// <param name="minLevel">Minimum log level. Default: Info.</param>
        public NLogLogger(
            string logFilePath = "./logs/app.log",
            LogLevel minLevel = null)
        {
            minLevel ??= LogLevel.Info;

            var config = new NLog.Config.LoggingConfiguration();

            var fileTarget = new NLog.Targets.FileTarget("file")
            {
                FileName = logFilePath,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                MaxArchiveFiles = 30,
                AutoFlush = true
            };

            config.AddTarget(fileTarget);
            config.AddRule(minLevel, LogLevel.Fatal, fileTarget);

            LogManager.Configuration = config;
            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>Log a trace message.</summary>
        public void Trace(string message) => _logger.Trace(message);

        /// <summary>Log a debug message.</summary>
        public void Debug(string message) => _logger.Debug(message);

        /// <summary>Log an info message.</summary>
        public void Info(string message) => _logger.Info(message);

        /// <summary>Log a warning message.</summary>
        public void Warn(string message) => _logger.Warn(message);

        /// <summary>Log an error message.</summary>
        public void Error(string message, Exception ex = null)
        {
            if (ex != null)
                _logger.Error(ex, message);
            else
                _logger.Error(message);
        }

        /// <summary>Log a fatal message.</summary>
        public void Fatal(string message, Exception ex = null)
        {
            if (ex != null)
                _logger.Fatal(ex, message);
            else
                _logger.Fatal(message);
        }

        /// <summary>Gets the underlying NLog Logger instance.</summary>
        public Logger UnderlyingLogger => _logger;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            LogManager.Shutdown();
        }
    }
}
```

**Step 3: Verify build**
```bash
dotnet build src/BPlusLib.Foundation/BPlusLib.Foundation.csproj
```

---

## Task 3: Create RichTextBox NLog Target

**Objective:** Create a custom NLog target that renders to WinForms RichTextBox

**Files:**
- Create: `src/BPlusLib/Foundation/Logging/RichTextBoxLogTarget.cs`

**Complete code:**
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using NLog;
using NLog.Targets;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Custom NLog target that writes log entries to a WinForms RichTextBox
    /// with color coding by log level.
    /// </summary>
    [Target("RichTextBox")]
    public sealed class RichTextBoxLogTarget : TargetWithLayout
    {
        private readonly RichTextBox _textBox;
        private readonly SynchronizationContext _syncContext;
        private readonly object _lock = new object();
        private int _maxLines = 5000;

        /// <summary>
        /// Maximum number of lines in the RichTextBox. Older lines are trimmed.
        /// </summary>
        public int MaxLines
        {
            get => _maxLines;
            set => _maxLines = value;
        }

        /// <summary>
        /// Creates a new RichTextBoxLogTarget attached to the specified RichTextBox.
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="textBox">The RichTextBox to render log entries to.</param>
        public RichTextBoxLogTarget(RichTextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _syncContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException("RichTextBoxLogTarget must be created on the UI thread.");
            Name = "RichTextBox";
        }

        /// <summary>
        /// Writes a log entry to the RichTextBox with color coding.
        /// </summary>
        protected override void Write(LogEventInfo logEvent)
        {
            string message = Layout.Render(logEvent);
            var color = GetColorForLevel(logEvent.Level);

            if (_syncContext != null)
            {
                _syncContext.Post(_ => AppendText(message, color), null);
            }
            else
            {
                AppendText(message, color);
            }
        }

        private void AppendText(string text, Color color)
        {
            lock (_lock)
            {
                if (_textBox.IsDisposed) return;

                _textBox.SelectionStart = _textBox.TextLength;
                _textBox.SelectionLength = 0;
                _textBox.SelectionColor = color;
                _textBox.AppendText(text + Environment.NewLine);
                _textBox.SelectionColor = _textBox.ForeColor;

                // Trim old lines if exceeding max
                if (_textBox.Lines.Length > _maxLines)
                {
                    int excess = _textBox.Lines.Length - _maxLines;
                    _textBox.Select(0, _textBox.GetFirstCharIndexFromLine(excess));
                    _textBox.SelectedText = string.Empty;
                }

                // Auto-scroll to bottom
                _textBox.SelectionStart = _textBox.TextLength;
                _textBox.ScrollToCaret();
            }
        }

        private static Color GetColorForLevel(LogLevel level)
        {
            if (level == LogLevel.Trace) return Color.Gray;
            if (level == LogLevel.Debug) return Color.LightGray;
            if (level == LogLevel.Info) return Color.White;
            if (level == LogLevel.Warn) return Color.Yellow;
            if (level == LogLevel.Error) return Color.OrangeRed;
            if (level == LogLevel.Fatal) return Color.Red;
            return Color.White;
        }
    }
}
```

**Step 4: Verify build**
```bash
dotnet build src/BPlusLib.Foundation/BPlusLib.Foundation.csproj
```

---

## Task 4: Create RichTextBoxLoggerFactory

**Objective:** Factory to create NLogLogger with RichTextBox target attached

**Files:**
- Create: `src/BPlusLib/Foundation/Logging/RichTextBoxLoggerFactory.cs`

**Complete code:**
```csharp
using System;
using System.Windows.Forms;
using NLog;
using NLog.Config;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Factory for creating NLogLogger instances with optional RichTextBox display.
    /// Provides quick setup for common logging scenarios.
    /// </summary>
    public static class RichTextBoxLoggerFactory
    {
        /// <summary>
        /// Creates an NLogLogger that writes to both a file and a RichTextBox.
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="textBox">RichTextBox to display logs in.</param>
        /// <param name="logFilePath">Path to log file. Default: "./logs/app.log"</param>
        /// <param name="minLevel">Minimum log level. Default: Debug.</param>
        /// <returns>A configured NLogLogger instance.</returns>
        public static NLogLogger CreateWithRichTextBox(
            RichTextBox textBox,
            string logFilePath = "./logs/app.log",
            LogLevel minLevel = null)
        {
            minLevel ??= LogLevel.Debug;

            var config = new LoggingConfiguration();

            // File target
            var fileTarget = new NLog.Targets.FileTarget("file")
            {
                FileName = logFilePath,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                MaxArchiveFiles = 30,
                AutoFlush = true
            };

            // RichTextBox target
            var rtbTarget = new RichTextBoxLogTarget(textBox)
            {
                Layout = "${longdate}|${level:uppercase=true}|${message}"
            };

            config.AddTarget(fileTarget);
            config.AddTarget(rtbTarget);
            config.AddRule(minLevel, LogLevel.Fatal, fileTarget);
            config.AddRule(minLevel, LogLevel.Fatal, rtbTarget);

            LogManager.Configuration = config;

            var logger = new NLogLogger(logFilePath, minLevel);
            return logger;
        }

        /// <summary>
        /// Creates an NLogLogger that writes to file only (no RichTextBox).
        /// </summary>
        /// <param name="logFilePath">Path to log file. Default: "./logs/app.log"</param>
        /// <param name="minLevel">Minimum log level. Default: Debug.</param>
        /// <returns>A configured NLogLogger instance.</returns>
        public static NLogLogger CreateFileOnly(
            string logFilePath = "./logs/app.log",
            LogLevel minLevel = null)
        {
            return new NLogLogger(logFilePath, minLevel ?? LogLevel.Debug);
        }
    }
}
```

**Step 5: Verify build**
```bash
dotnet build src/BPlusLib.Foundation/BPlusLib.Foundation.csproj
```

---

## Task 5: Create NLogLogger Tests

**Objective:** Test NLogLogger file logging

**Files:**
- Create: `tests/BPlusLib.Foundation.Tests/Logging/NLogLoggerTests.cs`

**Complete code:**
```csharp
using System;
using System.IO;
using FluentAssertions;
using BPlusLib.Foundation.Logging;
using NLog;
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
            using var logger = new NLogLogger(logPath, LogLevel.Debug);

            logger.Info("Test message");

            File.Exists(logPath).Should().BeTrue();
            var content = File.ReadAllText(logPath);
            content.Should().Contain("INFO");
            content.Should().Contain("Test message");
        }

        [Fact]
        public void Error_LogsException()
        {
            var logPath = Path.Combine(_tempDir, "error.log");
            using var logger = new NLogLogger(logPath, LogLevel.Debug);

            logger.Error("Something failed", new InvalidOperationException("boom"));

            var content = File.ReadAllText(logPath);
            content.Should().Contain("ERROR");
            content.Should().Contain("Something failed");
            content.Should().Contain("boom");
        }

        [Fact]
        public void MinLevel_FiltersLogs()
        {
            var logPath = Path.Combine(_tempDir, "filtered.log");
            using var logger = new NLogLogger(logPath, LogLevel.Warn);

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
            using var logger = RichTextBoxLoggerFactory.CreateFileOnly(logPath, LogLevel.Debug);

            logger.Info("Factory test");

            File.Exists(logPath).Should().BeTrue();
            File.ReadAllText(logPath).Should().Contain("Factory test");
        }
    }
}
```

**Step 6: Run tests**
```bash
dotnet test --framework net8.0 --filter "FullyQualifiedName~NLogLogger"
```
Expected: 4 passed

---

## Task 6: Update docs + README

**Objective:** Update documentation for the new logging features

**Files:**
- Modify: `docs/modules/logging.md`
- Modify: `README.md`

---

## Task 7: Commit + Push + NuGet

**Objective:** Version bump, build, push to GitHub + nuget.org

```bash
# Bump version
# Build + pack
# Push to GitHub
# Push to nuget.org
```

---

## Files Summary

| Action | File |
|--------|------|
| Create | `src/BPlusLib.Foundation/Logging/NLogLogger.cs` |
| Create | `src/BPlusLib.Foundation/Logging/RichTextBoxLogTarget.cs` |
| Create | `src/BPlusLib.Foundation/Logging/RichTextBoxLoggerFactory.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Logging/NLogLoggerTests.cs` |
| Modify | `src/BPlusLib.Foundation/BPlusLib.Foundation.csproj` |
| Modify | `docs/modules/logging.md` |
| Modify | `README.md` |

---

## Usage Examples

```csharp
// File only
using var logger = RichTextBoxLoggerFactory.CreateFileOnly("./logs/app.log");
logger.Info("Application started");
logger.Error("Failed to load config", ex);

// File + RichTextBox (on UI thread)
using var logger = RichTextBoxLoggerFactory.CreateWithRichTextBox(
    richTextBox1, "./logs/app.log");
logger.Info("Logs appear in the textbox with colors!");
logger.Warn("Warning in yellow");
logger.Error("Error in red");
```

---

## Risks

1. **NLog version conflict** — NLog 5.x works on net472+, low risk
2. **WinForms RichTextBox on non-Windows** — already handled by csproj `UseWindowsForms` condition
3. **Thread safety** — RichTextBoxLogTarget uses SynchronizationContext for UI thread marshalling
