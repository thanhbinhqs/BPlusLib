# Security

Comprehensive Windows security module providing Authenticode signature verification, credential manager access, UAC detection, integrity level management, privilege enumeration/control, token inspection, and high-level security queries. All methods use pure P/Invoke (no WMI), are thread-safe, and gracefully return safe defaults on non-Windows.

## Classes

### WinTrustHelper
Provides Authenticode digital signature verification for PE files using WinVerifyTrust + CryptQueryObject.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Verify(filePath) | SignatureInfo | Verifies Authenticode signature and returns detailed info |
| IsSigned(filePath) | bool | Quick check: whether the file has a valid Authenticode signature |
| GetPublisher(filePath) | string? | Returns the publisher/signer name from the digital signature |

### SignatureInfo
Detailed Authenticode signature information.

| Property | Type | Description |
|----------|------|-------------|
| IsSigned | bool | Whether the file has a digital signature |
| TrustLevel | TrustLevel | Overall trust level |
| SignerName | string? | Signer name (organization or individual) |
| PublisherName | string? | Publisher name from signing certificate |
| Thumbprint | string? | SHA-1 thumbprint of signing certificate |
| Timestamp | DateTime? | Timestamp from countersignature |
| IsOSBinary | bool | Whether this is a Microsoft-signed OS binary |
| ErrorCode | int | Detailed error code from WinVerifyTrust (0 = trusted) |
| StatusDescription | string? | Human-readable description of signature state |

### CredentialHelper
Read/write/enumerate/delete access to Windows Credential Manager via pure P/Invoke.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Read(targetName, type) | CredentialEntry? | Reads a stored credential by target name |
| Write(targetName, userName, password, type, persist, comment) | bool | Creates or updates a credential |
| Enumerate(filter?) | List\<CredentialEntry\> | Enumerates all stored credentials matching optional filter |
| Delete(targetName, type) | bool | Deletes a stored credential |

### UacHelper
Detects UAC state, process elevation, integrity level, and launches processes with elevated privileges (runas).

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| IsElevated() | bool | Whether the current process is running elevated |
| GetIntegrityLevel() | IntegrityLevel | Gets the integrity level of the current process |
| IsStandardUser() | bool | Whether the current process is standard user (not elevated, not SYSTEM) |
| IsUacEnabled() | bool | Whether UAC is enabled on the system |
| GetConsentPromptBehavior() | int | UAC consent prompt behavior for admins |
| RunElevated(arguments?) | bool | Restarts current exe with elevated privileges |
| RunAsAdmin(executablePath, arguments?) | bool | Launches a specific exe with elevated privileges |

### IntegrityHelper
Queries and sets integrity level (Mandatory Integrity Control) of processes.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| CurrentProcessIntegrityLevel | IntegrityLevel | Gets integrity level of current process |
| GetProcessIntegrityLevel(processId) | IntegrityLevel | Gets integrity level of a specific process |
| SetProcessIntegrityLevel(level) | bool | Sets the integrity level of the current process |

### SecurityHelper
High-level security helper orchestrating TokenHelper, PrivilegeHelper, and IntegrityHelper.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| IsCurrentProcessElevated() | bool | Whether the current process has an elevated token |
| IsProcessElevated(processId) | bool | Whether a specific process has an elevated token |
| IsProcess64Bit(processId) | bool | Whether a process is 64-bit |
| GetProcessOwner(processId) | string? | Gets the owner (DOMAIN\USER) of a process |
| IsInteractiveUser(processId) | bool | Whether a process runs in a user session |
| GetProcessIntegrityLevelString(processId) | string? | Integrity level as human-readable string |
| CanAccessProcess(processId, access) | bool | Whether the current process can access the target |
| GetProcessSidHistory(processId) | IReadOnlyList\<string\> | Group SIDs from the process token |
| IsProcessInAdminGroup(processId) | bool | Whether BUILTIN\Administrators is enabled in token |

### PrivilegeHelper
Enumerates, enables, disables, and queries process privileges.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| GetCurrentProcessPrivileges() | IReadOnlyList\<PrivilegeEntry\> | Enumerates all privileges of current process |
| GetProcessPrivileges(processId) | IReadOnlyList\<PrivilegeEntry\> | Enumerates all privileges of a specific process |
| EnablePrivilege(privilegeName) | bool | Enables a privilege for the current process |
| DisablePrivilege(privilegeName) | bool | Disables a privilege |
| RemovePrivilege(privilegeName) | bool | Removes a privilege entirely |
| HasPrivilege(privilegeName) | bool | Checks if a privilege is present and enabled |
| GetAllWellKnownPrivileges() | IReadOnlyList\<string\> | List of all well-known privilege names |

### TokenHelper
P/Invoke-based access to Windows token information.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| OpenProcessToken(processId, desiredAccess) | IntPtr? | Opens the access token for a process |
| GetTokenInformation(tokenHandle, infoClass, out data) | bool | Retrieves generic token info as byte array |
| GetTokenUser(tokenHandle) | string? | Gets the user SID string from a token |
| GetTokenGroups(tokenHandle) | string[]? | Gets the group SID strings from a token |
| GetTokenSessionId(tokenHandle) | int? | Gets the terminal services session ID |
| SidToString(sidPtr) | string? | Converts a SID pointer to string |
| SidToAccountName(sidPtr) | string? | Resolves a SID to DOMAIN\USER |

## Enums

| Enum | Values | Description |
|------|--------|-------------|
| TrustLevel | Unknown, Untrusted, Trusted, TrustedWithRevocation | Authenticode trust classification |
| IntegrityLevel | Untrusted, Low, Medium, MediumPlus, High, System, ProtectedProcess, Unknown | Windows integrity levels (MIC) |
| CredentialType | Generic, DomainPassword, DomainCertificate, DomainVisiblePassword, GenericCertificate, DomainExtended | Credential Manager types |
| CredentialPersistence | Session, LocalMachine, Enterprise | Credential persistence settings |
| PrivilegeAttributes | Disabled, EnabledByDefault, Enabled, Removed, UsedForAccess | Privilege state flags |
| TokenAccessLevels | AssignPrimary, Duplicate, Impersonate, Query, QuerySource, AdjustPrivileges, AdjustDefault, AdjustSessionId, Read, Write, AllAccess | Token access mask flags |
| TokenType | TokenPrimary, TokenImpersonation | Token type |
| TOKEN_INFORMATION_CLASS | TokenUser, TokenGroups, TokenPrivileges, TokenOwner, TokenPrimaryGroup, TokenDefaultDacl, TokenSource, TokenType, TokenImpersonationLevel, TokenStatistics, TokenSessionId, TokenGroupsAndPrivileges, TokenElevation, TokenIntegrityLevel | Token information classes |

## Usage

```csharp
using BPlusLib.Foundation.Security;

// Verify Authenticode signature
var sig = WinTrustHelper.Verify(@"C:\MyApp.exe");
if (sig.IsSigned)
    Console.WriteLine($"Signed by {sig.SignerName}, trust: {sig.TrustLevel}");

// Credential Manager
CredentialHelper.Write("myapp:user123", "user123", "secret", CredentialType.Generic);
var cred = CredentialHelper.Read("myapp:user123");
Console.WriteLine($"Password: {cred?.Password}");
CredentialHelper.Delete("myapp:user123");

// UAC
if (UacHelper.IsUacEnabled() && !UacHelper.IsElevated())
    UacHelper.RunElevated();

// Integrity level
var level = IntegrityHelper.GetProcessIntegrityLevel(processId);

// Privileges
bool hasDebug = PrivilegeHelper.HasPrivilege("SeDebugPrivilege");
PrivilegeHelper.EnablePrivilege("SeDebugPrivilege");
var all = PrivilegeHelper.GetCurrentProcessPrivileges();

// Security queries
string? owner = SecurityHelper.GetProcessOwner(processId);
bool elevated = SecurityHelper.IsProcessElevated(processId);
bool interactive = SecurityHelper.IsInteractiveUser(processId);
```

## Dependencies
- `kernel32.dll` — OpenProcess, CloseHandle, GetCurrentProcess
- `advapi32.dll` — OpenProcessToken, GetTokenInformation, SetTokenInformation, AdjustTokenPrivileges, LookupPrivilegeValueW, LookupPrivilegeNameW, LookupPrivilegeDisplayNameW, ConvertSidToStringSidW, LookupAccountSidW
- `wintrust.dll` — WinVerifyTrust
- `crypt32.dll` — CryptQueryObject, CertFindCertificateInStore, CertGetNameStringW
- `shell32.dll` — ShellExecuteExW (for runas)
- `BPlusLib.Foundation.Native` — Shared P/Invoke declarations
