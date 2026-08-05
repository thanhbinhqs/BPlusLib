# Logging

File-based logging with rolling and archiving support, structured log entries, NLog integration, and cross-thread RichTextBox display. Thread-safe and cross-platform.

## Enums

### LogLevel
Defines the severity level of a log entry.

| Value | Description |
|-------|-------------|
| Trace (0) | Most verbose level, for detailed diagnostic tracing |
| Debug (1) | Diagnostic messages for development and debugging |
| Information (2) | Informational messages tracking normal flow |
| Warning (3) | Potential problems that don't interrupt operation |
| Error (4) | Failures that prevented an operation from completing |
| Critical (5) | Catastrophic failures requiring immediate attention |
| None (6) | Special value to disable all logging |

### RollingPolicy
Specifies how log files are rolled and archived (Flags enum).

| Value | Description |
|-------|-------------|
| None | No rolling—all entries written to a single file |
| Daily | New log file created each day with a date suffix |
| FileSize | When file exceeds max size, it is archived and a new file started |
| Combined | Both Daily and FileSize rolling are active |

## Structs

### LogEntry
Immutable struct representing a single log entry with structured metadata.

| Property | Returns | Description |
|----------|---------|-------------|
| Timestamp | DateTime | The time at which the event occurred |
| Level | LogLevel | The severity level of the event |
| Message | string | The log message text (never null) |
| Category | string? | Optional category or source name |
| EventId | int? | Optional numeric event identifier |
| Exception | Exception? | Optional exception associated with the event |
| Properties | IReadOnlyDictionary\<string, object\>? | Optional structured properties |
| ToString() | string | Formats the entry as `2024-01-15 10:30:00.123 [INFO] (Category) Message` |

## Classes

### FileLogger
Writes log entries to a file with optional rolling and archiving. Thread-safe and works on all .NET-supported platforms. Uses `StreamWriter` with `FileShare.Read` so the file can be read (e.g., by a tail utility) while logging is in progress.

| Method | Returns | Description |
|--------|---------|-------------|
| FileLogger(string basePath, LogLevel minimumLevel, RollingPolicy rollingPolicy, long maxFileSizeBytes, int maxArchiveCount) | FileLogger | Initializes a new file logger instance |
| Log(LogEntry entry) | void | Writes a log entry if its level meets the minimum |
| Log(LogLevel level, string message, ...) | void | Writes a log entry with specified parameters |
| Trace(string message, string? category, int? eventId) | void | Writes a Trace-level entry |
| Debug(string message, string? category, int? eventId) | void | Writes a Debug-level entry |
| Information(string message, string? category, int? eventId) | void | Writes an Information-level entry |
| Warning(string message, string? category, int? eventId) | void | Writes a Warning-level entry |
| Error(string message, string? category, int? eventId, Exception? exception) | void | Writes an Error-level entry |
| Critical(string message, string? category, int? eventId, Exception? exception) | void | Writes a Critical-level entry |
| Flush() | void | Flushes all buffered data synchronously |
| FlushAsync() | Task | Flushes all buffered data asynchronously |
| Dispose() | void | Releases resources, flushing remaining data before closing |

### LoggerFactory
Central point for creating, configuring, and managing FileLogger instances. Loggers are cached by name and shared across callers. Thread-safe.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Configure(LogLevel minimumLevel, string? basePath, RollingPolicy rollingPolicy, long maxFileSizeBytes, int maxArchiveCount) | static void | Configures default settings for all loggers created after this call |
| SetGlobalLogLevel(LogLevel level) | static void | Sets the global minimum log level |
| GlobalMinimumLevel | static LogLevel | Gets the current global minimum log level |
| GetLogger(string name) | static FileLogger | Gets or creates a FileLogger with the specified name |
| GetLogger\<T\>() | static FileLogger | Gets or creates a FileLogger named after the specified type |
| GetLogger(object instance) | static FileLogger | Gets or creates a FileLogger for the type of the specified instance |
| Shutdown() | static void | Flushes and disposes all cached loggers |
| GetLoggerNames() | static IReadOnlyCollection\<string\> | Returns a snapshot of all registered logger names |

### NLogLogger
NLog-based logger wrapper that writes to file with rolling/archiving. Implements `IDisposable`. Supports `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal` log levels with automatic timestamp formatting.

| Method | Returns | Description |
|--------|---------|-------------|
| NLogLogger(string logFilePath, LogLevel minLevel) | NLogLogger | Creates a new NLog-backed file logger |
| Trace(string message) | void | Logs at Trace level |
| Debug(string message) | void | Logs at Debug level |
| Info(string message) | void | Logs at Information level |
| Warn(string message) | void | Logs at Warning level |
| Error(string message, Exception? ex = null) | void | Logs at Error level with optional exception |
| Fatal(string message, Exception? ex = null) | void | Logs at Fatal level with optional exception |
| Dispose() | void | Flushes and shuts down NLog |

**Log format:** `dd/MM/yyyy HH:mm:ss.fff|LEVEL|Message`

**NLog features:**
- Rolling files by day (`ArchiveEvery = Day`)
- Date-and-sequence archive numbering
- Auto-flush on every write
- 30-day archive retention

### RichTextBoxLogTarget
Custom NLog target that writes log entries to a WinForms `RichTextBox` with color coding by log level. **Supports cross-thread logging** — can be created on any thread; marshals writes to the UI thread automatically via `SynchronizationContext` or `BeginInvoke` fallback.

| Property/Method | Returns | Description |
|-----------------|---------|-------------|
| RichTextBoxLogTarget(RichTextBox textBox) | — | Creates a target. Can be called from any thread. |
| MaxLines | int | Maximum lines in the RichTextBox (default: 5000). Older lines are trimmed. |
| Dispose() | void | Detaches from the RichTextBox. |

**Color mapping:**

| Level | Color |
|-------|-------|
| Trace | Gray |
| Debug | LightGray |
| Info | White |
| Warn | Yellow |
| Error | OrangeRed |
| Fatal | Red |

**Cross-thread safety:**
- If created on UI thread → captures `SynchronizationContext` immediately
- If created on background thread → uses `Control.BeginInvoke()` fallback
- No locks → avoids UI thread deadlock
- `Dispose()` prevents further writes

### RichTextBoxLoggerFactory
Factory for creating `NLogLogger` instances with optional RichTextBox display. Windows-only (requires WinForms).

| Method | Returns | Description |
|--------|---------|-------------|
| CreateFileOnly(string logFilePath, LogLevel? minLevel) | NLogLogger | Creates a file-only logger |
| CreateWithRichTextBox(RichTextBox textBox, string logFilePath, LogLevel? minLevel) | NLogLogger | Creates a logger that writes to both file and RichTextBox. Cross-thread safe. |
| CreateRichTextBoxOnly(RichTextBox textBox, LogLevel? minLevel) | NLogLogger | Creates a RichTextBox-only logger (no file). Cross-thread safe. |

## Usage

### File-only logging

```csharp
using BPlusLib.Foundation.Logging;

// Configure the factory
LoggerFactory.Configure(
    minimumLevel: LogLevel.Information,
    basePath: "/var/log/myapp",
    rollingPolicy: RollingPolicy.Daily,
    maxFileSizeBytes: 10 * 1024 * 1024);

// Get a logger and write entries
var logger = LoggerFactory.GetLogger("MyService");
logger.Information("Service started");
logger.Warning("Disk space low", category: "Disk");
logger.Error("Connection failed", exception: ex);

// Async flush
await logger.FlushAsync();

// Shutdown on app exit
LoggerFactory.Shutdown();
```

### NLog file logging

```csharp
using BPlusLib.Foundation.Logging;

// Create NLog-backed logger
using var logger = RichTextBoxLoggerFactory.CreateFileOnly("./logs/app.log");
logger.Info("Application started");
logger.Warn("Disk space low");
logger.Error("Connection failed", ex);
```

### Cross-thread RichTextBox logging (WinForms)

```csharp
using BPlusLib.Foundation.Logging;

// Create logger with RichTextBox display — can be called from any thread
var logger = RichTextBoxLoggerFactory.CreateWithRichTextBox(
    richTextBox1,
    "./logs/app.log");

// ✅ Safe from background threads — auto-marshals to UI thread
Task.Run(() =>
{
    logger.Info("Processing...");
    logger.Error("Something failed", ex);
});

// ✅ Safe from UI thread too
logger.Info("Button clicked");

// Dispose when done
logger.Dispose();
```

### Background thread logging patterns

```csharp
// Pattern 1: Inject logger into background work
using var logger = RichTextBoxLoggerFactory.CreateWithRichTextBox(richTextBox1);

await Task.Run(() =>
{
    for (int i = 0; i < 100; i++)
    {
        logger.Info($"Processing item {i}");  // marshals to UI thread
    }
});

// Pattern 2: Multiple threads logging concurrently
using var logger = RichTextBoxLoggerFactory.CreateWithRichTextBox(richTextBox1);
var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
{
    logger.Info($"Thread {i} started");
    Thread.Sleep(100);
    logger.Info($"Thread {i} done");
}));
await Task.WhenAll(tasks);  // all entries appear in RichTextBox

// Pattern 3: Logger in a long-running service
public class MyService
{
    private readonly NLogLogger _logger;

    public MyService(RichTextBox rtb)
    {
        _logger = RichTextBoxLoggerFactory.CreateWithRichTextBox(rtb);
    }

    public void DoWork()
    {
        _logger.Info("Work started");
        // ... background work ...
        _logger.Info("Work completed");
    }

    public void Shutdown() => _logger.Dispose();
}
```

## Dependencies
- `NLog` 5.3.4 — NLog logging framework
- `System.Threading.SemaphoreSlim` (built-in, for thread safety)
- Windows-only: `System.Windows.Forms` (WinForms RichTextBox)
- Cross-platform: FileLogger, LoggerFactory, LogEntry work on all platforms
