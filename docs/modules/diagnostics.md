# Diagnostics

Provides diagnostic utilities for reading Windows Event Logs and creating process minidumps. Uses modern .NET APIs on .NET 6+ with fallbacks for .NET Framework 4.7.2.

## Classes

### EventLogReader
Provides read access to Windows Event Logs. Uses System.Diagnostics.Eventing.Reader on .NET 6+ and falls back to System.Diagnostics.EventLog on .NET Framework 4.7.2.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| EventLogReader(string logName, string? machineName) | — | Creates reader targeting specified event log |
| LogName | string | Gets the name of the event log |
| RecordCount | int? | Gets the number of records in the log |
| ReadAll() | IReadOnlyList&lt;EventLogEntryInfo&gt; | Reads all entries from the event log |
| ReadSince(DateTime timestamp) | IReadOnlyList&lt;EventLogEntryInfo&gt; | Reads entries after specified time |
| ReadLast(int count) | IReadOnlyList&lt;EventLogEntryInfo&gt; | Reads last N entries |
| SearchBySource(string source) | IReadOnlyList&lt;EventLogEntryInfo&gt; | Reads entries by event source |
| SearchByEventId(int eventId) | IReadOnlyList&lt;EventLogEntryInfo&gt; | Reads entries by event ID |
| Clear() | void | Clears all entries from the log |
| GetLogNames() | IReadOnlyList&lt;string&gt; | Static — enumerates available event logs |

### EventLogEntryInfo
Represents a single event log entry with full metadata.

| Property | Type | Description |
|----------|------|-------------|
| MachineName | string? | Name of the machine that generated the entry |
| Source | string? | Source (provider) name |
| TimeGenerated | DateTime | Local time event was generated |
| TimeWritten | DateTime | Local time event was written to log |
| EventId | int? | Event identifier |
| CategoryNumber | int? | Task category number |
| Category | string? | Task category string |
| ProcessId | int? | Process ID that wrote the entry |
| ThreadId | int? | Thread ID that wrote the entry |
| Message | string? | Event message text |
| UserName | string? | User name associated with the entry |
| EntryType | EventLogEntryType | Severity classification |

### CrashDumpHelper
Provides safe, no-throw access to Windows minidump creation via dbghelp.dll / MiniDumpWriteDump.

| Method | Returns | Description |
|--------|---------|-------------|
| TryCreateMiniDump(int processId, string outputPath, MiniDumpType) | bool | Creates a minidump of the specified process |
| TryCreateFullDump(int processId, string outputPath) | bool | Creates a full user-mode dump |
| GetDefaultDumpFolder() | string? | Gets default crash dump folder path |

### MiniDumpType
Flags enum specifying minidump type and data to include (corresponds to MINIDUMP_TYPE in dbghelp.h).

| Value | Description |
|-------|-------------|
| MiniDumpNormal | Minimal dump (stack + basic info) |
| MiniDumpWithDataSegs | Includes data segments (default) |
| MiniDumpWithFullMemory | Includes full memory contents |
| MiniDumpWithHandleData | Includes handle data |

### EventLogEntryType
Enum defining event log entry types.

| Value | Description |
|-------|-------------|
| Information | Informational event |
| Warning | Warning event |
| Error | Error event |
| SuccessAudit | Audit success (security log) |
| FailureAudit | Audit failure (security log) |

## Usage

```csharp
using BPlusLib.Foundation.Diagnostics;

// Read event log
var reader = new EventLogReader("Application");
IReadOnlyList<EventLogEntryInfo> entries = reader.ReadLast(100);
foreach (var entry in entries)
{
    Console.WriteLine($"[{entry.EntryType}] {entry.Source}: {entry.Message}");
}

// Search by source
var appErrors = reader.SearchBySource("MyApp");

// List available logs
IReadOnlyList<string> logs = EventLogReader.GetLogNames();

// Create minidump
bool success = CrashDumpHelper.TryCreateMiniDump(
    processId: 1234,
    outputPath: @"C:\Dumps\process.dmp",
    dumpType: MiniDumpType.MiniDumpWithDataSegs);

// Get default dump folder
string? folder = CrashDumpHelper.GetDefaultDumpFolder();
```

## Dependencies
- dbghelp.dll (MiniDumpWriteDump) — for CrashDumpHelper
- kernel32.dll (OpenProcess, CreateFile, CloseHandle) — for CrashDumpHelper
- System.Diagnostics.EventLog (net472) / System.Diagnostics.Eventing.Reader (.NET 6+) — for EventLogReader
