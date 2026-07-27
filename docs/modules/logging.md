# Logging

File-based logging with rolling and archiving support, structured log entries, and a centralized logger factory. Thread-safe and cross-platform.

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

## Usage

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

## Dependencies
- `System.Threading.SemaphoreSlim` (built-in, for thread safety)
- No external NuGet packages required
- Fully cross-platform
