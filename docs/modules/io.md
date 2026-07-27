# IO

File I/O utilities including path manipulation, checksum computation, file version info reading, and safe file operations with atomic writes and retry logic.

## Classes

### FileVersionInfo
Immutable record representing version information read from a PE file's version resource.

| Property | Returns | Description |
|----------|---------|-------------|
| FileVersion | string? | The file version string |
| ProductVersion | string? | The product version string |
| CompanyName | string? | The company name |
| ProductName | string? | The product name |
| FileDescription | string? | The file description |
| LegalCopyright | string? | The legal copyright |
| LegalTrademarks | string? | The legal trademarks |
| InternalName | string? | The internal name |
| OriginalFilename | string? | The original filename |
| Comments | string? | The comments |
| PrivateBuild | string? | The private build string |
| SpecialBuild | string? | The special build string |
| Language | string? | The language identifier string |

### FileVersionHelper
Provides methods to read version information from PE files using the version.dll API. All methods are thread-safe and gracefully return null on non-Windows platforms.

| Method | Returns | Description |
|--------|---------|-------------|
| GetVersionInfo(string filePath) | FileVersionInfo? | Reads all available version info from a PE file using VerQueryValue |
| GetFileVersion(string filePath) | string? | Quick read: FileVersion string |
| GetProductVersion(string filePath) | string? | Quick read: ProductVersion string |
| GetCompanyName(string filePath) | string? | Quick read: CompanyName |

### ChecksumHelper
Provides file checksum computation using standard cryptographic hash algorithms (MD5, SHA-1, SHA-256, SHA-512) and a pure-managed CRC-32 implementation. All methods use buffered I/O (8 KB buffer), are thread-safe, and never throw—returning "ERROR" on failure.

| Method | Returns | Description |
|--------|---------|-------------|
| ComputeMD5(string path) | string | Computes the MD5 hash of a file |
| ComputeSHA1(string path) | string | Computes the SHA-1 hash of a file |
| ComputeSHA256(string path) | string | Computes the SHA-256 hash of a file |
| ComputeSHA512(string path) | string | Computes the SHA-512 hash of a file |
| ComputeCRC32(string path) | string | Computes a CRC-32 checksum using the standard IEEE polynomial |
| ComputeHash(string path, HashAlgorithmName algorithm) | string | Computes the cryptographic hash using the specified algorithm |
| VerifyHash(string path, string expectedHash, HashAlgorithmName algorithm) | bool | Verifies that a file produces the expected hash |

### SafeFileOps
Thread-safe file I/O operations with atomic-write support, retry logic, and graceful error handling. All methods catch exceptions internally and return false rather than throwing.

| Method | Returns | Description |
|--------|---------|-------------|
| TryWriteAllText(string path, string? contents, Encoding? encoding) | bool | Atomically writes text using a temporary sibling file and File.Replace |
| TryReadAllText(string path, out string? contents, out Exception? error) | bool | Reads all text from a file with shared read access |
| TryCopy(string source, string dest, bool overwrite) | bool | Copies a file with retry logic |
| TryMove(string source, string dest, bool overwrite) | bool | Moves a file or directory with retry logic |
| TryDelete(string path, bool recursive) | bool | Deletes a file or directory |
| IsFileLocked(string path) | bool | Determines whether a file is currently locked by another process |
| GetTempFilePath(string? extension) | string | Creates a unique temporary file path with the specified extension |
| EnsureDirectoryExists(string path) | bool | Ensures that all directories in the path exist |
| TryGetFileHash(string path, HashAlgorithmName algorithm, out string? hash, out Exception? error) | bool | Computes the cryptographic hash of a file |

### PathHelper
Provides safe, validated path operations: combining, sanitizing, normalizing, and querying file-system paths. All methods are thread-safe and never throw.

| Method | Returns | Description |
|--------|---------|-------------|
| SafeCombine(string path1, string path2) | string? | Safely combines two path components after validation |
| HasInvalidPathChars(string path) | bool | Returns true if the path contains invalid path characters |
| HasInvalidFileNameChars(string name) | bool | Returns true if the name contains invalid file name characters |
| SanitizeFileName(string name, char replacement) | string | Replaces every invalid file-name character with the replacement |
| IsAbsolutePath(string path) | bool | Determines whether the path is an absolute (rooted) path |
| GetRelativePath(string fullPath, string basePath) | string? | Computes a relative path from basePath to fullPath |
| NormalizePath(string path) | string | Normalizes a path via Path.GetFullPath and replaces separators |
| PathExists(string path) | bool | Returns true if the path exists as either a file or a directory |
| GetPathSize(string path, bool recursive) | long | Computes the total size (in bytes) of a file or directory |
| GetAvailableFileName(string basePath, string? prefix) | string | Returns a file name that does not currently exist |

## Usage

```csharp
using BPlusLib.Foundation.IO;

// Read file version info
var versionInfo = FileVersionHelper.GetVersionInfo(@"C:\Windows\notepad.exe");
Console.WriteLine($"Version: {versionInfo?.FileVersion}");

// Compute checksums
string md5 = ChecksumHelper.ComputeMD5(@"C:\myfile.txt");
string sha256 = ChecksumHelper.ComputeSHA256(@"C:\myfile.txt");
bool valid = ChecksumHelper.VerifyHash(@"C:\myfile.txt", expectedHash, HashAlgorithmName.SHA256);

// Safe file operations
SafeFileOps.TryWriteAllText("config.json", jsonContent);
SafeFileOps.TryReadAllText("config.json", out var contents, out var error);
SafeFileOps.IsFileLocked(@"C:\locked.dat");

// Path utilities
string safe = PathHelper.SafeCombine("C:\\data", "file.txt");
string sanitized = PathHelper.SanitizeFileName("file:with<bad>chars.txt");
string available = PathHelper.GetAvailableFileName("report.pdf");
```

## Dependencies
- `BPlusLib.Foundation.Native` (for `VersionApi` P/Invoke)
- `kernel32.dll` (P/Invoke for file lock detection)
- No external NuGet packages required
