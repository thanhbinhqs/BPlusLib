# Cisco EWC

Interaction with Cisco EWC (Embedded Wireless Controller) devices via RESTCONF API and syslog. Provides device info, access point enumeration, client tracking, SSID management, and syslog message parsing.

## Classes

### CiscoEwcHelper
Static facade for interacting with Cisco EWC devices via RESTCONF and syslog. All methods are thread-safe and return empty/null on failure.

| Method | Returns | Description |
|--------|---------|-------------|
| GetDeviceInfoAsync(string host, string username, string password, int port, CancellationToken) | Task\<CiscoDeviceInfo\> | Gets device information from a Cisco WLC via RESTCONF |
| GetAccessPointsAsync(string host, string username, string password, int port, CancellationToken) | Task\<List\<CiscoApInfo\>\> | Gets all access points managed by a Cisco WLC |
| GetClientsAsync(string host, string username, string password, int port, CancellationToken) | Task\<List\<CiscoClientInfo\>\> | Gets all wireless clients associated to a Cisco WLC |
| GetSsidsAsync(string host, string username, string password, int port, CancellationToken) | Task\<List\<CiscoSsidInfo\>\> | Gets all SSIDs (WLANs) configured on a Cisco WLC |
| StartSyslogListener(int port, Action\<CiscoSyslogEntry\>? onMessageReceived) | SyslogServer | Creates and starts a syslog listener for Cisco WLC messages |
| Ping(string host, int timeoutMs) | static bool | Pings the specified host to determine reachability |
| IsReachableAsync(string host, string username, string password, int port, CancellationToken) | Task\<bool\> | Checks whether a Cisco WLC is reachable via RESTCONF |
| GetYangDataAsync(string host, string username, string password, string yangPath, int port, CancellationToken) | Task\<string?\> | Gets raw YANG data from a specific RESTCONF path |
| GetCapabilitiesAsync(string host, string username, string password, int port, CancellationToken) | Task\<JObject?\> | Gets the RESTCONF capabilities of the WLC |

### RestConfClient
HTTPS client for Cisco IOS XE / EWC RESTCONF API with Basic authentication. Thread-safe.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| RestConfClient(string host, string username, string password, int port, bool ignoreCertificateErrors) | RestConfClient | Initializes a new RESTCONF client |
| BaseUrl | string | The base URL used by this client |
| GetAsync(string path, CancellationToken) | Task\<JObject?\> | Performs a GET request and deserializes the JSON response |
| PostAsync(string path, string jsonBody, CancellationToken) | Task\<int?\> | Performs a POST request with a JSON body |
| GetRawAsync(string path, CancellationToken) | Task\<string?\> | Performs a GET request and returns the raw response string |
| IsReachableAsync(CancellationToken) | Task\<bool\> | Tests whether the WLC is reachable over HTTPS |
| GetYangModulesAsync(CancellationToken) | Task\<JObject?\> | Retrieves the list of YANG modules supported by the WLC |
| GetCapabilitiesAsync(CancellationToken) | Task\<JObject?\> | Retrieves the RESTCONF server capabilities |
| Dispose() | void | Disposes the underlying HTTP client |

### SyslogServer
Lightweight UDP syslog listener that receives RFC 5424 messages from Cisco WLCs and stores them in a thread-safe concurrent queue.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| SyslogServer(int port) | SyslogServer | Initializes a syslog server on the specified port |
| MessageReceived | event Action\<CiscoSyslogEntry\> | Event raised when a syslog message is received |
| Port | int | The port the server listens on |
| IsListening | bool | Whether the server is currently listening |
| Start() | void | Starts the syslog listener |
| StartAsync(CancellationToken) | Task | Starts the syslog listener asynchronously |
| Stop() | void | Stops the syslog listener |
| TryDequeue(out CiscoSyslogEntry?) | bool | Dequeues a single syslog entry |
| DequeueAll() | CiscoSyslogEntry[] | Dequeues all pending syslog entries |
| PendingCount | int | Number of pending entries in the queue |
| Dispose() | void | Stops and disposes the server |

### YangParser
Parses Cisco IOS XE / EWC YANG model JSON responses into strongly-typed model objects. All methods are static and thread-safe.

| Method | Returns | Description |
|--------|---------|-------------|
| ParseDeviceInfo(JObject? json, string defaultIpAddress) | CiscoDeviceInfo | Parses device/global information from a YANG JSON response |
| ParseAccessPoints(JObject? json) | List\<CiscoApInfo\> | Parses access point operational data |
| ParseClients(JObject? json) | List\<CiscoClientInfo\> | Parses client operational data |
| ParseSsids(JObject? json) | List\<CiscoSsidInfo\> | Parses SSID (WLAN) operational data |

## Models

### CiscoDeviceInfo
| Property | Returns | Description |
|----------|---------|-------------|
| IpAddress | string | IP address of the WLC |
| Hostname | string | Hostname of the WLC |
| Model | string | Hardware model |
| SerialNumber | string | Serial number |
| SoftwareVersion | string | Software version |
| FirmwareVersion | string | Firmware version |
| SystemUptime | string | System uptime |
| LastQueried | DateTimeOffset | When the info was last queried |

### CiscoApInfo
| Property | Returns | Description |
|----------|---------|-------------|
| MacAddress | string | MAC address of the access point |
| Name | string | Name of the AP |
| OperationalStatus | string | Operational status |
| SoftwareVersion | string | Software version |
| Model | string | AP model |
| SerialNumber | string | Serial number |
| Location | string | Physical location |
| IpAddress | string | IP address |
| ClientCount | int | Number of connected clients |
| Channel | int | Radio channel |
| TxPower | double | Transmit power |
| LastQueried | DateTimeOffset | When the info was last queried |

### CiscoClientInfo
| Property | Returns | Description |
|----------|---------|-------------|
| MacAddress | string | Client MAC address |
| IpAddress | string | Client IP address |
| Hostname | string | Client hostname |
| UserName | string | Associated username |
| Ssid | string | SSID name |
| ApMacAddress | string | Associated AP MAC |
| ApName | string | Associated AP name |
| RadioBand | string | Radio band |
| Channel | int | Channel |
| Rssi | int | Signal strength (RSSI) |
| DataRate | double | Data rate |
| Status | string | Client status |
| AuthMethod | string | Authentication method |
| LastQueried | DateTimeOffset | When the info was last queried |

### CiscoSsidInfo
| Property | Returns | Description |
|----------|---------|-------------|
| ProfileName | string | WLAN profile name |
| Ssid | string | SSID name |
| VlanId | int | VLAN ID |
| IsEnabled | bool | Whether the SSID is enabled |
| ClientCount | int | Number of connected clients |
| SecurityMode | string | Security mode |
| AuthType | string | Authentication type |
| RadioPolicy | string | Radio policy |
| WlanId | int | WLAN ID |
| LastQueried | DateTimeOffset | When the info was last queried |

### CiscoSyslogEntry
| Property | Returns | Description |
|----------|---------|-------------|
| Version | int | Syslog version |
| Timestamp | DateTimeOffset | Message timestamp |
| Hostname | string | Source hostname |
| AppName | string | Application name |
| ProcessId | string | Process ID |
| MessageId | string | Message ID |
| Severity | int | Severity level (0-7) |
| SeverityName | string | Human-readable severity name |
| Facility | int | Facility code |
| Message | string | Parsed message text |
| RawMessage | string | Original raw message |
| SourceIp | string | Source IP address |
| SourcePort | int | Source port |
| ReceivedAt | DateTimeOffset | When the message was received |

## Usage

```csharp
using BPlusLib.Foundation.Networking.Cisco;

// Get device information
var deviceInfo = await CiscoEwcHelper.GetDeviceInfoAsync("192.168.1.10", "admin", "password");
Console.WriteLine($"Hostname: {deviceInfo.Hostname}, Model: {deviceInfo.Model}");

// List access points
var aps = await CiscoEwcHelper.GetAccessPointsAsync("192.168.1.10", "admin", "password");
foreach (var ap in aps)
    Console.WriteLine($"AP: {ap.Name} ({ap.IpAddress}) Clients: {ap.ClientCount}");

// List clients
var clients = await CiscoEwcHelper.GetClientsAsync("192.168.1.10", "admin", "password");

// Start syslog listener
var syslog = CiscoEwcHelper.StartSyslogListener(514, entry =>
{
    Console.WriteLine($"[{entry.SeverityName}] {entry.Hostname}: {entry.Message}");
});

// Raw RESTCONF query
var client = new RestConfClient("192.168.1.10", "admin", "password");
var json = await client.GetAsync("/restconf/data/Cisco-IOS-XE-native:native");
```

## Dependencies
- `Newtonsoft.Json` 13.0.3 (for JSON/YANG parsing)
- `System.Net.Http` (for RESTCONF HTTPS client)
- `BPlusLib.Foundation.Networking` (uses `SyslogServer` internally)
