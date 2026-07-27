# Cisco EWC Module

Cisco Embedded Wireless Controller (EWC) integration via RESTCONF API (RFC 8040), YANG models, and Syslog (RFC 5424). Supports Cisco 9100 series and IOS-XE based WLCs.

## Classes

### CiscoEwcHelper

Static facade for all Cisco EWC operations. All methods are thread-safe and return empty/null on failure.

| Method | Returns | Description |
|--------|---------|-------------|
| `GetDeviceInfoAsync(host, user, pass)` | `CiscoDeviceInfo` | Device info (hostname, model, serial, version) |
| `GetAccessPointsAsync(host, user, pass)` | `List<CiscoApInfo>` | All managed access points |
| `GetClientsAsync(host, user, pass)` | `List<CiscoClientInfo>` | All connected wireless clients |
| `GetSsidsAsync(host, user, pass)` | `List<CiscoSsidInfo>` | All configured SSIDs/WLANs |
| `GetRadioInfoAsync(host, user, pass)` | `List<CiscoRadioInfo>` | RF data: channel, width, power, band per radio |
| `GetRadioInfoForApAsync(host, user, pass, apMac)` | `List<CiscoRadioInfo>` | RF data for a specific AP |
| `GetRfProfilesAsync(host, user, pass)` | `List<CiscoRfProfileInfo>` | RF profile configurations |
| `GetApProfilesAsync(host, user, pass)` | `List<CiscoApProfileInfo>` | AP group/profile configurations |
| `StartSyslogListener(port, callback)` | `SyslogServer` | Real-time syslog listener |
| `Ping(host)` | `bool` | ICMP reachability check |
| `IsReachableAsync(host, user, pass)` | `bool` | RESTCONF reachability check |
| `GetYangDataAsync(host, user, pass, path)` | `string?` | Raw YANG data from any path |
| `GetCapabilitiesAsync(host, user, pass)` | `JObject?` | RESTCONF server capabilities |

### RestConfClient

HTTPS client for Cisco IOS XE RESTCONF API with Basic authentication.

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAsync(path)` | `JObject?` | GET request, parsed JSON |
| `PostAsync(path, body)` | `int?` | POST request with JSON body |
| `GetRawAsync(path)` | `string?` | GET request, raw JSON string |
| `IsReachableAsync()` | `bool` | Lightweight health check |
| `GetYangModulesAsync()` | `JObject?` | List supported YANG modules |
| `GetCapabilitiesAsync()` | `JObject?` | RESTCONF capabilities |

### YangParser

Parses Cisco IOS XE YANG JSON responses into strongly-typed model objects.

| Method | Returns | Description |
|--------|---------|-------------|
| `ParseDeviceInfo(json)` | `CiscoDeviceInfo` | Parse device info |
| `ParseAccessPoints(json)` | `List<CiscoApInfo>` | Parse AP data |
| `ParseClients(json)` | `List<CiscoClientInfo>` | Parse client data |
| `ParseSsids(json)` | `List<CiscoSsidInfo>` | Parse SSID/WLAN data |
| `ParseRadioInfo(json)` | `List<CiscoRadioInfo>` | Parse RF radio slot data |
| `ParseRfProfiles(json)` | `List<CiscoRfProfileInfo>` | Parse RF profiles |
| `ParseApProfiles(json)` | `List<CiscoApProfileInfo>` | Parse AP group profiles |

### SyslogServer

UDP syslog listener (RFC 5424) with real-time callbacks and thread-safe message buffering.

| Member | Type | Description |
|--------|------|-------------|
| `Start()` | `void` | Start listening |
| `Stop()` | `void` | Stop listening |
| `TryDequeue(out entry)` | `bool` | Dequeue one message |
| `DequeueAll()` | `CiscoSyslogEntry[]` | Dequeue all pending messages |
| `PendingCount` | `int` | Number of unread messages |
| `IsListening` | `bool` | Whether the server is running |
| `MessageReceived` | `event Action<CiscoSyslogEntry>` | Fired on each received message |

---

## Models

### CiscoDeviceInfo

| Property | Type | Description |
|----------|------|-------------|
| `IpAddress` | `string` | WLC IP address |
| `Hostname` | `string` | Device hostname |
| `Model` | `string` | Hardware model |
| `SerialNumber` | `string` | Serial number |
| `SoftwareVersion` | `string` | IOS-XE version |
| `FirmwareVersion` | `string` | Firmware version |
| `SystemUptime` | `string` | System uptime |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoApInfo

| Property | Type | Description |
|----------|------|-------------|
| `MacAddress` | `string` | AP MAC address |
| `Name` | `string` | AP name |
| `OperationalStatus` | `string` | Operational status |
| `SoftwareVersion` | `string` | AP software version |
| `Model` | `string` | AP model |
| `SerialNumber` | `string` | AP serial number |
| `Location` | `string` | Physical location |
| `IpAddress` | `string` | AP IP address |
| `ClientCount` | `int` | Number of connected clients |
| `Channel` | `int` | Operating channel |
| `TxPower` | `double` | Transmit power |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoRadioInfo

| Property | Type | Description |
|----------|------|-------------|
| `ApMacAddress` | `string` | Parent AP MAC address |
| `ApName` | `string` | Parent AP name |
| `SlotId` | `int` | Radio slot index (0=2.4G, 1=5G, 2=6G) |
| `Band` | `string` | Radio band (2.4GHz, 5GHz, 6GHz) |
| `RadioType` | `string` | Radio type (802.11ac, 802.11ax, etc.) |
| `Channel` | `int` | Operating channel number |
| `ChannelWidth` | `int` | Channel width in MHz (20, 40, 80, 160) |
| `ChannelWidthString` | `string` | Channel width string |
| `TxPower` | `double` | Transmit power level |
| `AdminState` | `string` | Admin state (enabled/disabled) |
| `OperState` | `string` | Operational state |
| `NoiseFloor` | `double` | Noise floor level (dBm) |
| `ClientCount` | `int` | Clients on this radio |
| `SsidName` | `string` | Associated SSID name |
| `IsPromiscuous` | `bool` | Promiscuous mode |
| `Bssid` | `string` | BSSID |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoRfProfileInfo

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Profile name |
| `RadioBand` | `string` | Radio band (2.4GHz, 5GHz, 6GHz) |
| `ChannelMode` | `string` | Channel mode (auto/custom) |
| `ChannelList` | `string` | Channel list (comma-separated) |
| `DefaultChannelWidth` | `int` | Default channel width (MHz) |
| `ChannelWidthString` | `string` | Channel width string |
| `TxPowerLevel` | `double` | TX power level |
| `MaxTxPower` | `double` | Maximum TX power |
| `MinTxPower` | `double` | Minimum TX power |
| `MandatoryDataRate` | `double` | Mandatory data rate (Mbps) |
| `MaxDataRate` | `double` | Maximum data rate (Mbps) |
| `ClientMinRssi` | `int` | Client minimum RSSI (dBm) |
| `RrmEnabled` | `bool` | RRM (Radio Resource Management) enabled |
| `CoverageHoleDetection` | `bool` | Coverage hole detection enabled |
| `CoverageHoleRssi` | `int` | Coverage hole RSSI threshold |
| `NeighborReportEnabled` | `bool` | 802.11k neighbor reports |
| `Dot11bEnabled` | `bool` | 802.11b/g enabled |
| `Dot11aEnabled` | `bool` | 802.11a enabled |
| `Description` | `string` | Profile description |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoApProfileInfo

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | AP group name |
| `Description` | `string` | Group description |
| `ApCount` | `int` | Number of APs in group |
| `RfProfile24Ghz` | `string` | 2.4 GHz RF profile name |
| `RfProfile5Ghz` | `string` | 5 GHz RF profile name |
| `RfProfile6Ghz` | `string` | 6 GHz RF profile name |
| `AssociatedWlans` | `string` | Associated WLAN names |
| `Dot11bChannelMode` | `string` | 2.4 GHz channel mode |
| `Dot11aChannelMode` | `string` | 5 GHz channel mode |
| `Dot11bChannelList` | `string` | 2.4 GHz channel list |
| `Dot11aChannelList` | `string` | 5 GHz channel list |
| `Dot11bChannelWidth` | `string` | 2.4 GHz channel width |
| `Dot11aChannelWidth` | `string` | 5 GHz channel width |
| `Dot11bTxPower` | `double` | 2.4 GHz TX power |
| `Dot11aTxPower` | `double` | 5 GHz TX power |
| `FlexConnectVlan` | `string` | FlexConnect VLAN |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoClientInfo

| Property | Type | Description |
|----------|------|-------------|
| `MacAddress` | `string` | Client MAC address |
| `IpAddress` | `string` | Client IP address |
| `Hostname` | `string` | Client hostname |
| `UserName` | `string` | Authenticated username |
| `Ssid` | `string` | Connected SSID |
| `ApMacAddress` | `string` | Serving AP MAC |
| `ApName` | `string` | Serving AP name |
| `RadioBand` | `string` | Radio band |
| `Channel` | `int` | Operating channel |
| `Rssi` | `int` | RSSI (dBm) |
| `DataRate` | `double` | Data rate (Mbps) |
| `Status` | `string` | Client state |
| `AuthMethod` | `string` | Authentication method |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoSsidInfo

| Property | Type | Description |
|----------|------|-------------|
| `ProfileName` | `string` | WLAN profile name |
| `Ssid` | `string` | SSID name |
| `VlanId` | `int` | VLAN ID |
| `IsEnabled` | `bool` | Admin status |
| `ClientCount` | `int` | Connected clients |
| `SecurityMode` | `string` | Security mode |
| `AuthType` | `string` | Authentication type |
| `RadioPolicy` | `string` | Radio policy |
| `WlanId` | `int` | WLAN ID |
| `LastQueried` | `DateTimeOffset` | When data was fetched |

### CiscoSyslogEntry

| Property | Type | Description |
|----------|------|-------------|
| `Version` | `int` | Syslog version |
| `Timestamp` | `DateTimeOffset` | Message timestamp |
| `Hostname` | `string` | Source hostname |
| `AppName` | `string` | Application name |
| `ProcessId` | `string` | Process ID |
| `MessageId` | `string` | Message ID |
| `Severity` | `int` | Severity level (0-7) |
| `Facility` | `int` | Facility code |
| `SeverityName` | `string` | Human-readable severity |
| `Message` | `string` | Log message |
| `RawMessage` | `string` | Raw syslog message |
| `SourceIp` | `string` | Sender IP |
| `SourcePort` | `int` | Sender port |
| `ReceivedAt` | `DateTimeOffset` | When received |

---

## Usage

```csharp
using BPlusLib.Foundation.Networking.Cisco;

// ── Device Info ─────────────────────────────────────
var device = await CiscoEwcHelper.GetDeviceInfoAsync("192.168.1.1", "admin", "pass");
Console.WriteLine($"{device.Hostname} — {device.Model} — {device.SoftwareVersion}");

// ── Access Points ───────────────────────────────────
var aps = await CiscoEwcHelper.GetAccessPointsAsync("192.168.1.1", "admin", "pass");
foreach (var ap in aps)
    Console.WriteLine($"{ap.Name} — {ap.IpAddress} — {ap.ClientCount} clients");

// ── RF / Radio Info (channel, width, power) ─────────
var radios = await CiscoEwcHelper.GetRadioInfoAsync("192.168.1.1", "admin", "pass");
foreach (var r in radios)
    Console.WriteLine($"{r.ApName} [{r.Band}] Ch{r.Channel} {r.ChannelWidth}MHz Power:{r.TxPower}dBm");

// ── RF Profiles ─────────────────────────────────────
var rfProfiles = await CiscoEwcHelper.GetRfProfilesAsync("192.168.1.1", "admin", "pass");
foreach (var p in rfProfiles)
    Console.WriteLine($"{p.Name} — {p.RadioBand} — Ch: {p.ChannelList} — Width: {p.DefaultChannelWidth}MHz — Power: {p.TxPowerLevel}");

// ── AP Profiles (AP Groups) ─────────────────────────
var apProfiles = await CiscoEwcHelper.GetApProfilesAsync("192.168.1.1", "admin", "pass");
foreach (var p in apProfiles)
    Console.WriteLine($"{p.Name} — 2.4G:{p.RfProfile24Ghz} 5G:{p.RfProfile5Ghz} — WLANs: {p.AssociatedWlans}");

// ── Radio Info for Specific AP ──────────────────────
var apRadios = await CiscoEwcHelper.GetRadioInfoForApAsync("192.168.1.1", "admin", "pass", "aa:bb:cc:dd:ee:ff");

// ── Clients ─────────────────────────────────────────
var clients = await CiscoEwcHelper.GetClientsAsync("192.168.1.1", "admin", "pass");
foreach (var c in clients)
    Console.WriteLine($"{c.UserName} — {c.IpAddress} — {c.Ssid} — RSSI:{c.Rssi}dBm");

// ── SSIDs ───────────────────────────────────────────
var ssids = await CiscoEwcHelper.GetSsidsAsync("192.168.1.1", "admin", "pass");

// ── Raw YANG Data ───────────────────────────────────
var raw = await CiscoEwcHelper.GetYangDataAsync("192.168.1.1", "admin", "pass",
    "/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data");

// ── Syslog (real-time) ──────────────────────────────
using var syslog = CiscoEwcHelper.StartSyslogListener(514, entry =>
    Console.WriteLine($"[{entry.SeverityName}] {entry.Hostname}: {entry.Message}"));

// ── Diagnostics ─────────────────────────────────────
bool ok = await CiscoEwcHelper.IsReachableAsync("192.168.1.1", "admin", "pass");
var caps = await CiscoEwcHelper.GetCapabilitiesAsync("192.168.1.1", "admin", "pass");
```

---

## Dependencies

| Package | Version |
|---------|---------|
| Newtonsoft.Json | 13.0.3 |

## YANG Models

| Model | Path | Description |
|-------|------|-------------|
| `Cisco-IOS-XE-native` | `/restconf/data/Cisco-IOS-XE-native:native` | Device info |
| `Cisco-IOS-XE-wireless-access-point-oper` | `/restconf/data/...access-point-oper-data` | AP + radio slot data |
| `Cisco-IOS-XE-wireless-client-oper` | `/restconf/data/...client-oper-data` | Client data |
| `Cisco-IOS-XE-wireless-wlan-global-oper` | `/restconf/data/...wlan-global-oper-data` | SSID/WLAN data |
| `Cisco-IOS-XE-wireless-rf-cfg` | `/restconf/data/...rf-profiles` | RF profile config |
| `Cisco-IOS-XE-wireless-ap-cfg` | `/restconf/data/...ap-cfg` | AP group config |
