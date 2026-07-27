# Registry

Thread-safe Windows registry helper providing safe read/write operations, .reg export/import, and backup/restore functionality. All methods catch exceptions internally and return false/null rather than throwing. Uses `Microsoft.Win32.Registry` and `RegistryKey` classes; the registry hive is parsed from the path prefix (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\...").

## Classes

### RegistryHelper
Static thread-safe registry helper. All methods are graceful on error (return false/null).

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetValue\<T\>(keyPath, valueName) | T? | Reads a registry value as the specified type |
| GetString(keyPath, valueName) | string? | Reads a string value |
| GetDWord(keyPath, valueName) | int? | Reads a DWORD (32-bit integer) value |
| GetQWord(keyPath, valueName) | long? | Reads a QWORD (64-bit integer) value |
| GetBinary(keyPath, valueName) | byte[]? | Reads a binary value |
| GetMultiString(keyPath, valueName) | string[]? | Reads a MULTI_SZ (multi-string) value |
| ValueExists(keyPath, valueName) | bool | Checks if a value exists |
| KeyExists(keyPath) | bool | Checks if a key exists |
| GetSubKeyNames(keyPath) | IReadOnlyList\<string\> | Gets names of all subkeys |
| GetValueNames(keyPath) | IReadOnlyList\<string\> | Gets names of all values |
| TrySetValue\<T\>(keyPath, valueName, value, kind?) | bool | Sets a registry value (creates key if needed) |
| TryDeleteValue(keyPath, valueName) | bool | Deletes a registry value |
| TryDeleteKey(keyPath, recursive?) | bool | Deletes a registry key (optionally recursive) |
| TryExportToReg(keyPath, filePath) | bool | Exports a key tree to a .reg file (UTF-16 LE) |
| TryImportFromReg(filePath) | bool | Imports entries from a .reg file |
| TryBackupKey(keyPath) | RegistryBackup? | Creates a snapshot backup of a key tree |
| TryRestoreKey(backup) | bool | Restores a key tree from backup |

### RegistryHive
Enum representing well-known registry hives.

| Value | Description |
|-------|-------------|
| ClassesRoot | HKEY_CLASSES_ROOT |
| CurrentUser | HKEY_CURRENT_USER |
| LocalMachine | HKEY_LOCAL_MACHINE |
| Users | HKEY_USERS |
| CurrentConfig | HKEY_CURRENT_CONFIG |
| PerformanceData | HKEY_PERFORMANCE_DATA |

### RegistryValueEntry
Represents a single registry value entry.

| Property | Type | Description |
|----------|------|-------------|
| Name | string | Value name (empty string = default value) |
| Data | object? | Value data |
| Kind | RegistryValueKind | Registry value kind |

### RegistryBackup
Represents a snapshot backup of a registry key and its subkeys.

| Property | Type | Description |
|----------|------|-------------|
| KeyPath | string | Full registry key path |
| BackupTime | DateTime | UTC time when backup was taken |
| Values | Dictionary\<string, RegistryValueEntry\> | Captured values keyed by path\ValueName |

## Usage

```csharp
using BPlusLib.Foundation.Registry;

// Read values
string? name = RegistryHelper.GetString(@"HKEY_LOCAL_MACHINE\SOFTWARE\MyApp", "Name");
int? version = RegistryHelper.GetDWord(@"HKEY_CURRENT_USER\Software\MyApp", "Version");

// Check existence
bool exists = RegistryHelper.KeyExists(@"HKEY_LOCAL_MACHINE\SOFTWARE\MyApp");
bool valueExists = RegistryHelper.ValueExists(@"HKEY_CURRENT_USER\Software\MyApp", "Name");

// Write values
RegistryHelper.TrySetValue(@"HKEY_CURRENT_USER\Software\MyApp", "Name", "MyApp");
RegistryHelper.TrySetValue(@"HKEY_CURRENT_USER\Software\MyApp", "Version", 42);

// Backup and restore
var backup = RegistryHelper.TryBackupKey(@"HKEY_CURRENT_USER\Software\MyApp");
if (backup != null)
    RegistryHelper.TryRestoreKey(backup);

// Export/Import .reg files
RegistryHelper.TryExportToReg(@"HKEY_CURRENT_USER\Software\MyApp", @"C:\backup.reg");
RegistryHelper.TryImportFromReg(@"C:\backup.reg");
```

## Dependencies
- `Microsoft.Win32` — Registry and RegistryKey classes (BCL)
- `System.IO` — File operations for .reg export/import
