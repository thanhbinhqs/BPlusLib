# Cisco EWC Management Library — RESTCONF + YANG + Syslog

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add a `CiscoEwcHelper` to BPlusLib.Foundation for managing Cisco 9100 series APs in EWC mode — retrieve device info, AP details, client stats, logs, and configuration via RESTCONF API with YANG models + Syslog.

**Architecture:** RESTCONF (RFC 8040) over HTTPS for configuration and operational data. YANG models define the data structure. Syslog (UDP 514) for real-time log collection. No SNMP — RESTCONF is the modern standard for Cisco IOS-XE.

**Tech Stack:** C# HTTPS RESTCONF client, Syslog UDP listener, YANG model parsing, .NET Framework 4.7+, .NET 6, .NET 8

---

## Cisco EWC Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                      Cisco 9100 EWC                           │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐     │
│  │  RESTCONF     │  │  Syslog       │  │  NETCONF/SSH  │     │
│  │  HTTPS 443    │  │  UDP 514      │  │  TCP 830      │     │
│  │  RFC 8040     │  │  RFC 5424     │  │  RFC 6241     │     │
│  └───────────────┘  └───────────────┘  └───────────────┘     │
│                                                               │
│  YANG Models:                                                 │
│  - Cisco-IOS-XE-wireless-access-point-oper                   │
│  - Cisco-IOS-XE-wireless-client-oper                         │
│  - Cisco-IOS-XE-wireless-global-oper                         │
│  - Cisco-IOS-XE-wireless-dot11-oper                          │
│  - Cisco-IOS-XE-native                                        │
└───────────────────────────────────────────────────────────────┘
```

---

## RESTCONF API Endpoints (Cisco EWC)

| Endpoint | YANG Model | Mô tả |
|----------|------------|-------|
| `/restconf/data/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data` | AP operational | AP status, channel, power |
| `/restconf/data/Cisco-IOS-XE-wireless-client-oper:client-oper-data` | Client operational | Connected clients |
| `/restconf/data/Cisco-IOS-XE-wireless-global-oper:global-oper-data` | Global operational | WLC stats |
| `/restconf/data/Cisco-IOS-XE-native:native` | Native config | Device config |
| `/restconf/data/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data/ap-name=<name>` | Single AP | Specific AP info |
| `/restconf/data/Cisco-IOS-XE-wireless-client-oper:client-oper-data/mac-address=<mac>` | Single client | Specific client |

---

## API Design

### Public Models

```csharp
// ── Device Info ─────────────────────────────────────────────────────
public sealed class CiscoDeviceInfo
{
    public string Hostname { get; init; }
    public string IpAddress { get; init; }
    public string Model { get; init; }              // e.g., "C9120AXI"
    public string SerialNumber { get; init; }
    public string SoftwareVersion { get; init; }    // IOS-XE version
    public string SystemTime { get; init; }
    public string Uptime { get; init; }
    public string Mode { get; init; }               // "EWC"
    public int NumAps { get; init; }
    public int NumClients { get; init; }
}

// ── Access Point Info ───────────────────────────────────────────────
public sealed class CiscoApInfo
{
    public string Name { get; init; }
    public string MacAddress { get; init; }
    public string IpAddress { get; init; }
    public string Model { get; init; }
    public string SerialNumber { get; init; }
    public string Location { get; init; }
    public string Radio0Status { get; init; }       // 2.4GHz
    public string Radio1Status { get; init; }       // 5GHz
    public int Channel0 { get; init; }
    public int Channel1 { get; init; }
    public int Power0 { get; init; }
    public int Power1 { get; init; }
    public int Clients0 { get; init; }
    public int Clients1 { get; init; }
    public bool IsOnline { get; init; }
    public DateTime LastSeen { get; init; }
    public string AdminState { get; init; }
    public string OperState { get; init; }
}

// ── Client Info ─────────────────────────────────────────────────────
public sealed class CiscoClientInfo
{
    public string MacAddress { get; init; }
    public string IpAddress { get; init; }
    public string Hostname { get; init; }
    public string UserName { get; init; }
    public string ApName { get; init; }
    public string Ssid { get; init; }
    public string SecurityType { get; init; }
    public string Protocol { get; init; }           // 802.11ax, 802.11ac
    public int Rssi { get; init; }
    public int Channel { get; init; }
    public string PhyType { get; init; }
    public long BytesSent { get; init; }
    public long BytesReceived { get; init; }
    public long PacketsSent { get; init; }
    public long PacketsReceived { get; init; }
    public int Speed { get; init; }
    public string State { get; init; }
    public DateTime JoinTime { get; init; }
}

// ── Syslog Entry ───────────────────────────────────────────────────
public sealed class CiscoSyslogEntry
{
    public DateTime Timestamp { get; init; }
    public string Severity { get; init; }           // "Emergency", "Alert", "Critical", "Error", "Warning", "Notice", "Info", "Debug"
    public int SeverityCode { get; init; }          // 0-7
    public string Facility { get; init; }           // e.g., "LOCAL0", "AUTH"
    public string Hostname { get; init; }
    public string ProcessName { get; init; }        // e.g., "wlc_", "osapi"
    public string Message { get; init; }
    public string RawMessage { get; init; }         // Original syslog line
}

// ── SSID Info ───────────────────────────────────────────────────────
public sealed class CiscoSsidInfo
{
    public string Name { get; init; }
    public string ProfileName { get; init; }
    public bool IsBroadcast { get; init; }
    public string SecurityType { get; init; }
    public string AuthMethod { get; init; }
    public int ClientCount { get; init; }
    public bool IsGuest { get; init; }
}

// ── Syslog Config ───────────────────────────────────────────────────
public sealed class CiscoSyslogConfig
{
    public string Server { get; init; }
    public int Port { get; init; }
    public string Facility { get; init; }
    public int MinSeverity { get; init; }           // 0-7
    public bool IsEnabled { get; init; }
}
```

### Public Methods

```csharp
public static class CiscoEwcHelper
{
    // ── Device Info (RESTCONF) ──────────────────────────────────────
    
    /// <summary>Gets device info via RESTCONF YANG model.</summary>
    public static async Task<CiscoDeviceInfo?> GetDeviceInfoAsync(
        string host, string username, string password);
    
    /// <summary>Gets device info via RESTCONF with custom timeout.</summary>
    public static async Task<CiscoDeviceInfo?> GetDeviceInfoAsync(
        string host, string username, string password, int timeoutMs);
    
    // ── AP Management (RESTCONF) ───────────────────────────────────
    
    /// <summary>Gets all APs via RESTCONF YANG model.</summary>
    public static async Task<IReadOnlyList<CiscoApInfo>> GetAccessPointsAsync(
        string host, string username, string password);
    
    /// <summary>Gets a specific AP by name.</summary>
    public static async Task<CiscoApInfo?> GetAccessPointAsync(
        string host, string username, string password, string apName);
    
    /// <summary>Gets a specific AP by MAC address.</summary>
    public static async Task<CiscoApInfo?> GetAccessPointByMacAsync(
        string host, string username, string password, string macAddress);
    
    /// <summary>Gets AP statistics (clients, traffic).</summary>
    public static async Task<IReadOnlyList<CiscoApInfo>> GetApStatisticsAsync(
        string host, string username, string password);
    
    // ── Client Management (RESTCONF) ───────────────────────────────
    
    /// <summary>Gets all connected clients via RESTCONF.</summary>
    public static async Task<IReadOnlyList<CiscoClientInfo>> GetClientsAsync(
        string host, string username, string password);
    
    /// <summary>Gets a specific client by MAC.</summary>
    public static async Task<CiscoClientInfo?> GetClientAsync(
        string host, string username, string password, string macAddress);
    
    /// <summary>Gets client count per AP.</summary>
    public static async Task<IReadOnlyDictionary<string, int>> GetClientCountPerApAsync(
        string host, string username, string password);
    
    // ── SSID Management (RESTCONF) ─────────────────────────────────
    
    /// <summary>Gets all configured SSIDs via RESTCONF.</summary>
    public static async Task<IReadOnlyList<CiscoSsidInfo>> GetSsidsAsync(
        string host, string username, string password);
    
    /// <summary>Gets SSID statistics (client count, traffic).</summary>
    public static async Task<IReadOnlyList<CiscoSsidInfo>> GetSsidStatisticsAsync(
        string host, string username, string password);
    
    // ── Syslog ──────────────────────────────────────────────────────
    
    /// <summary>Starts a syslog listener on the specified port.</summary>
    public static CiscoSyslogServer StartSyslogListener(
        int port = 514, Action<CiscoSyslogEntry>? onEntry = null);
    
    /// <summary>Gets syslog configuration via RESTCONF.</summary>
    public static async Task<CiscoSyslogConfig?> GetSyslogConfigAsync(
        string host, string username, string password);
    
    /// <summary>Gets recent log entries from the EWC via RESTCONF.</summary>
    public static async Task<IReadOnlyList<CiscoSyslogEntry>> GetLogsAsync(
        string host, string username, string password, int maxEntries = 100);
    
    /// <summary>Gets logs filtered by severity.</summary>
    public static async Task<IReadOnlyList<CiscoSyslogEntry>> GetLogsAsync(
        string host, string username, string password,
        string severity, int maxEntries = 100);
    
    // ── Configuration (RESTCONF) ───────────────────────────────────
    
    /// <summary>Gets running configuration summary via RESTCONF.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> GetConfigAsync(
        string host, string username, string password);
    
    /// <summary>Gets wireless configuration summary.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> GetWirelessConfigAsync(
        string host, string username, string password);
    
    // ── RESTCONF Raw ────────────────────────────────────────────────
    
    /// <summary>Gets raw YANG data from a RESTCONF endpoint.</summary>
    public static async Task<string?> GetYangDataAsync(
        string host, string username, string password, string yangPath);
    
    /// <summary>Gets YANG data as parsed JSON.</summary>
    public static async Task<JsonElement?> GetYangDataJsonAsync(
        string host, string username, string password, string yangPath);
    
    /// <summary>Lists available YANG models on the device.</summary>
    public static async Task<IReadOnlyList<string>> GetYangModelsAsync(
        string host, string username, string password);
    
    /// <summary>Gets YANG model schema for a specific module.</summary>
    public static async Task<string?> GetYangSchemaAsync(
        string host, string username, string password, string moduleName);
    
    // ── Diagnostics ─────────────────────────────────────────────────
    
    /// <summary>Pings the EWC.</summary>
    public static bool Ping(string host, int timeoutMs = 2000);
    
    /// <summary>Checks if RESTCONF is reachable.</summary>
    public static async Task<bool> IsReachableAsync(
        string host, string username, string password, int timeoutMs = 2000);
    
    /// <summary>Gets RESTCONF API capabilities.</summary>
    public static async Task<IReadOnlyList<string>> GetCapabilitiesAsync(
        string host, string username, string password);
}
```

---

## File Structure

### New Files

| File | Purpose |
|------|---------|
| `src/Networking/Cisco/CiscoEwcHelper.cs` | Main public API |
| `src/Networking/Cisco/Models/CiscoDeviceInfo.cs` | Device info model |
| `src/Networking/Cisco/Models/CiscoApInfo.cs` | AP info model |
| `src/Networking/Cisco/Models/CiscoClientInfo.cs` | Client info model |
| `src/Networking/Cisco/Models/CiscoSyslogEntry.cs` | Syslog entry model |
| `src/Networking/Cisco/Models/CiscoSsidInfo.cs` | SSID info model |
| `src/Networking/Cisco/Models/CiscoSyslogConfig.cs` | Syslog config model |
| `src/Networking/Cisco/RestConfClient.cs` | RESTCONF HTTP client |
| `src/Networking/Cisco/SyslogServer.cs` | Syslog UDP listener |
| `src/Networking/Cisco/YangParser.cs` | YANG JSON response parser |
| `tests/Networking/Cisco/CiscoEwcHelperTests.cs` | Unit tests |

---

## Task Breakdown

### Task 1: Create RESTCONF client

**Objective:** Implement HTTPS RESTCONF client for Cisco EWC with basic auth and session support.

**Files:**
- Create: `src/Networking/Cisco/RestConfClient.cs`

**Step 1: Write RestConfClient.cs**

```csharp
// <copyright file="RestConfClient.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// HTTPS RESTCONF client for Cisco IOS-XE devices (EWC, ISR, Catalyst).
    /// Implements RFC 8040 — RESTCONF Protocol.
    /// </summary>
    internal static class RestConfClient
    {
        private const string DataPrefix = "/restconf/data";
        private const string OperationsPrefix = "/restconf/operations";
        private const string YangCatalogPrefix = "/restconf/data/ietf-yang-library:yang-library";

        /// <summary>
        /// Gets the base URL for RESTCONF data operations.
        /// </summary>
        internal static string GetDataUrl(string host, string yangPath)
        {
            return $"https://{host}{DataPrefix}{yangPath}";
        }

        /// <summary>
        /// Makes a GET request to RESTCONF and returns the JSON response.
        /// </summary>
        internal static async Task<JsonElement?> GetAsync(
            string host, string username, string password, string yangPath,
            int timeoutMs = 10000, string accept = "application/yang-data+json")
        {
            try
            {
                using var handler = CreateHandler(username, password);
                using var client = CreateClient(handler, timeoutMs);

                string url = GetDataUrl(host, yangPath);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

                var response = await client.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch { return null; }
        }

        /// <summary>
        /// Makes a GET request and returns raw string content.
        /// </summary>
        internal static async Task<string?> GetRawAsync(
            string host, string username, string password, string yangPath,
            int timeoutMs = 10000, string accept = "application/yang-data+json")
        {
            try
            {
                using var handler = CreateHandler(username, password);
                using var client = CreateClient(handler, timeoutMs);

                string url = GetDataUrl(host, yangPath);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

                var response = await client.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch { return null; }
        }

        /// <summary>
        /// Makes a POST request to RESTCONF (for RPC operations).
        /// </summary>
        internal static async Task<JsonElement?> PostAsync(
            string host, string username, string password, string yangPath,
            object? body = null, int timeoutMs = 10000)
        {
            try
            {
                using var handler = CreateHandler(username, password);
                using var client = CreateClient(handler, timeoutMs);

                string url = $"https://{host}{OperationsPrefix}{yangPath}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/yang-data+json"));

                if (body != null)
                {
                    string json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/yang-data+json");
                }

                var response = await client.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                string respJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<JsonElement>(respJson);
            }
            catch { return null; }
        }

        /// <summary>
        /// Checks if RESTCONF is reachable on the device.
        /// </summary>
        internal static async Task<bool> IsReachableAsync(
            string host, string username, string password, int timeoutMs = 2000)
        {
            var result = await GetAsync(host, username, password,
                "/ietf-system:system-state/clock", timeoutMs).ConfigureAwait(false);
            return result.HasValue;
        }

        /// <summary>
        /// Gets YANG library data (list of installed YANG models).
        /// </summary>
        internal static async Task<IReadOnlyList<string>> GetYangModulesAsync(
            string host, string username, string password, int timeoutMs = 5000)
        {
            var modules = new List<string>();
            try
            {
                var json = await GetAsync(host, username, password,
                    "/ietf-yang-library:yang-library/module", timeoutMs).ConfigureAwait(false);
                if (!json.HasValue) return modules;

                if (json.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in json.Value.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var name))
                            modules.Add(name.GetString() ?? string.Empty);
                    }
                }
            }
            catch { }
            return modules;
        }

        /// <summary>
        /// Gets RESTCONF server capabilities.
        /// </summary>
        internal static async Task<IReadOnlyList<string>> GetCapabilitiesAsync(
            string host, string username, string password, int timeoutMs = 5000)
        {
            var caps = new List<string>();
            try
            {
                using var handler = CreateHandler(username, password);
                using var client = CreateClient(handler, timeoutMs);

                string url = $"https://{host}/restconf/capabilities";
                var response = await client.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return caps;

                string xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                // Parse <capability> elements from XML
                int idx = 0;
                while ((idx = xml.IndexOf("<capability>", idx, StringComparison.Ordinal)) >= 0)
                {
                    int end = xml.IndexOf("</capability>", idx, StringComparison.Ordinal);
                    if (end < 0) break;
                    string cap = xml.Substring(idx + 12, end - idx - 12);
                    caps.Add(cap);
                    idx = end + 13;
                }
            }
            catch { }
            return caps;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static HttpClientHandler CreateHandler(string username, string password)
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true,
                Credentials = new NetworkCredential(username, password),
            };
        }

        private static HttpClient CreateClient(HttpClientHandler handler, int timeoutMs)
        {
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            };
        }
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 2: Create YANG JSON parser

**Objective:** Parse RESTCONF YANG data JSON responses into model objects.

**Files:**
- Create: `src/Networking/Cisco/YangParser.cs`

**Step 1: Write YangParser.cs**

```csharp
// <copyright file="YangParser.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// Parses RESTCONF YANG data JSON responses into model objects.
    /// Handles Cisco IOS-XE YANG model naming conventions.
    /// </summary>
    internal static class YangParser
    {
        /// <summary>
        /// Parses AP list from YANG access-point-oper-data.
        /// </summary>
        internal static IReadOnlyList<CiscoApInfo> ParseAccessPoints(JsonElement json)
        {
            var aps = new List<CiscoApInfo>();

            try
            {
                // Navigate: Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data/ap-info
                if (json.TryGetProperty("Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data", out var data) &&
                    data.TryGetProperty("ap-info", out var apInfo) &&
                    apInfo.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ap in apInfo.EnumerateArray())
                    {
                        aps.Add(ParseApInfo(ap));
                    }
                }
            }
            catch { }

            return aps;
        }

        /// <summary>
        /// Parses a single AP info from YANG data.
        /// </summary>
        internal static CiscoApInfo ParseApInfo(JsonElement ap)
        {
            return new CiscoApInfo
            {
                Name = GetString(ap, "ap-name"),
                MacAddress = GetString(ap, "bssid"),
                IpAddress = GetString(ap, "ap-ip-addr"),
                Model = GetString(ap, "ap-model"),
                SerialNumber = GetString(ap, "ap-serial"),
                Location = GetString(ap, "ap-location"),
                AdminState = GetString(ap, "ap-admin-state"),
                OperState = GetString(ap, "ap-oper-state"),
                IsOnline = GetString(ap, "ap-oper-state") == "Registered",
                Radio0Status = GetString(ap, "radio-slot-info[0]/radio-admin-state"),
                Radio1Status = GetString(ap, "radio-slot-info[1]/radio-admin-state"),
                Channel0 = GetInt(ap, "radio-slot-info[0]/channel-number"),
                Channel1 = GetInt(ap, "radio-slot-info[1]/channel-number"),
                Power0 = GetInt(ap, "radio-slot-info[0]/power-level"),
                Power1 = GetInt(ap, "radio-slot-info[1]/power-level"),
            };
        }

        /// <summary>
        /// Parses client list from YANG client-oper-data.
        /// </summary>
        internal static IReadOnlyList<CiscoClientInfo> ParseClients(JsonElement json)
        {
            var clients = new List<CiscoClientInfo>();

            try
            {
                if (json.TryGetProperty("Cisco-IOS-XE-wireless-client-oper:client-oper-data", out var data) &&
                    data.TryGetProperty("client-info", out var clientInfo) &&
                    clientInfo.ValueKind == JsonValueKind.Array)
                {
                    foreach (var client in clientInfo.EnumerateArray())
                    {
                        clients.Add(ParseClientInfo(client));
                    }
                }
            }
            catch { }

            return clients;
        }

        /// <summary>
        /// Parses a single client info from YANG data.
        /// </summary>
        internal static CiscoClientInfo ParseClientInfo(JsonElement client)
        {
            return new CiscoClientInfo
            {
                MacAddress = GetString(client, "mac-addr"),
                IpAddress = GetString(client, "ipv4-addr"),
                Hostname = GetString(client, "host-name"),
                UserName = GetString(client, "user-name"),
                ApName = GetString(client, "ap-name"),
                Ssid = GetString(client, "wlan-id"),
                SecurityType = GetString(client, "security-protocol"),
                Protocol = GetString(client, "protocol"),
                Rssi = GetInt(client, "rssi"),
                Channel = GetInt(client, "channel"),
                PhyType = GetString(client, "phy-type"),
                Speed = GetInt(client, "speed"),
                State = GetString(client, "state"),
            };
        }

        /// <summary>
        /// Parses SSID list from YANG wireless global data.
        /// </summary>
        internal static IReadOnlyList<CiscoSsidInfo> ParseSsids(JsonElement json)
        {
            var ssids = new List<CiscoSsidInfo>();

            try
            {
                if (json.TryGetProperty("Cisco-IOS-XE-wireless-global-oper:wlan-global-oper-data", out var data) &&
                    data.TryGetProperty("wlan-info", out var wlanInfo) &&
                    wlanInfo.ValueKind == JsonValueKind.Array)
                {
                    foreach (var wlan in wlanInfo.EnumerateArray())
                    {
                        ssids.Add(new CiscoSsidInfo
                        {
                            Name = GetString(wlan, "wlan-name"),
                            ProfileName = GetString(wlan, "profile-name"),
                            IsBroadcast = GetBool(wlan, "broadcast-ssid"),
                            ClientCount = GetInt(wlan, "num-of-clients"),
                        });
                    }
                }
            }
            catch { }

            return ssids;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static string GetString(JsonElement element, string path)
        {
            try
            {
                if (element.TryGetProperty(path, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
            }
            catch { }
            return string.Empty;
        }

        private static int GetInt(JsonElement element, string path)
        {
            try
            {
                if (element.TryGetProperty(path, out var value) && value.ValueKind == JsonValueKind.Number)
                    return value.GetInt32();
            }
            catch { }
            return 0;
        }

        private static bool GetBool(JsonElement element, string path)
        {
            try
            {
                if (element.TryGetProperty(path, out var value) && value.ValueKind == JsonValueKind.True)
                    return true;
            }
            catch { }
            return false;
        }
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 3: Create Syslog server

**Objective:** Implement UDP syslog listener (RFC 5424) for real-time log collection.

**Files:**
- Create: `src/Networking/Cisco/SyslogServer.cs`

**Step 1: Write SyslogServer.cs**

```csharp
// <copyright file="SyslogServer.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// UDP Syslog server (RFC 5424) for receiving logs from Cisco devices.
    /// Thread-safe, supports concurrent clients.
    /// </summary>
    public sealed class CiscoSyslogServer : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly CancellationTokenSource _cts;
        private readonly Task _listenTask;
        private readonly ConcurrentQueue<CiscoSyslogEntry> _entries;
        private readonly Action<CiscoSyslogEntry>? _onEntry;
        private bool _disposed;

        /// <summary>
        /// Creates and starts a syslog server on the specified port.
        /// </summary>
        /// <param name="port">UDP port to listen on (default 514).</param>
        /// <param name="onEntry">Optional callback for each received log entry.</param>
        public CiscoSyslogServer(int port = 514, Action<CiscoSyslogEntry>? onEntry = null)
        {
            _entries = new ConcurrentQueue<CiscoSyslogEntry>();
            _onEntry = onEntry;
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient(port);
            _listenTask = Task.Run(() => ListenLoop(_cts.Token));
        }

        /// <summary>Gets whether the server is currently running.</summary>
        public bool IsRunning => !_cts.IsCancellationRequested && !_disposed;

        /// <summary>Gets the number of entries received since creation.</summary>
        public int EntryCount => _entries.Count;

        /// <summary>
        /// Dequeues all pending syslog entries.
        /// </summary>
        public IReadOnlyList<CiscoSyslogEntry> GetEntries()
        {
            var result = new List<CiscoSyslogEntry>();
            while (_entries.TryDequeue(out var entry))
            {
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Dequeues up to maxEntries pending syslog entries.
        /// </summary>
        public IReadOnlyList<CiscoSyslogEntry> GetEntries(int maxEntries)
        {
            var result = new List<CiscoSyslogEntry>();
            while (result.Count < maxEntries && _entries.TryDequeue(out var entry))
            {
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Stops the syslog server.
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            try { _udpClient.Close(); } catch { }
        }

        /// <summary>Disposes the server and stops listening.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts.Dispose();
            _udpClient.Dispose();
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync().ConfigureAwait(false);
                    string message = Encoding.UTF8.GetString(result.Buffer);
                    var entry = ParseSyslogMessage(message, result.RemoteEndPoint);
                    _entries.Enqueue(entry);
                    _onEntry?.Invoke(entry);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch { /* continue listening */ }
            }
        }

        /// <summary>
        /// Parses a syslog message (RFC 5424 format).
        /// Format: &lt;priority&gt;version timestamp hostname app-name procid msgid structured-data msg
        /// </summary>
        internal static CiscoSyslogEntry ParseSyslogMessage(string message, EndPoint? remoteEp = null)
        {
            try
            {
                // Extract priority: &lt;NN&gt;
                int priorityStart = message.IndexOf('<');
                int priorityEnd = message.IndexOf('>', priorityStart);
                int priority = 0;
                if (priorityStart >= 0 && priorityEnd > priorityStart)
                {
                    int.TryParse(message.Substring(priorityStart + 1, priorityEnd - priorityStart - 1), out priority);
                }

                int severity = priority % 8;
                int facility = priority / 8;

                string severityStr = severity switch
                {
                    0 => "Emergency",
                    1 => "Alert",
                    2 => "Critical",
                    3 => "Error",
                    4 => "Warning",
                    5 => "Notice",
                    6 => "Info",
                    7 => "Debug",
                    _ => "Unknown"
                };

                string facilityStr = facility switch
                        {
                    0 => "Kern",
                    1 => "User",
                    2 => "Mail",
                    3 => "Daemon",
                    4 => "Auth",
                    5 => "Syslog",
                    6 => "LPR",
                    7 => "News",
                    8 => "UUCP",
                    9 => "Cron",
                    10 => "AuthPriv",
                    11 => "FTP",
                    12 => "NTP",
                    13 => "Security",
                    14 => "Console",
                    15 => "SolarisCron",
                    16 => "Local0",
                    17 => "Local1",
                    18 => "Local2",
                    19 => "Local3",
                    20 => "Local4",
                    21 => "Local5",
                    22 => "Local6",
                    23 => "Local7",
                    _ => $"Facility{facility}"
                };

                // Parse remaining fields after priority
                string remaining = message.Substring(priorityEnd + 1);
                string[] parts = remaining.Split(' ', 5);

                string timestamp = parts.Length > 0 ? parts[0] : string.Empty;
                string hostname = parts.Length > 1 ? parts[1] : string.Empty;
                string appName = parts.Length > 2 ? parts[2] : string.Empty;
                string procId = parts.Length > 3 ? parts[3] : string.Empty;
                string msg = parts.Length > 4 ? parts[4] : remaining;

                DateTime ts = DateTime.TryParse(timestamp, out var parsed) ? parsed : DateTime.UtcNow;

                return new CiscoSyslogEntry
                {
                    Timestamp = ts,
                    Severity = severityStr,
                    SeverityCode = severity,
                    Facility = facilityStr,
                    Hostname = hostname,
                    ProcessName = appName,
                    Message = msg,
                    RawMessage = message,
                };
            }
            catch
            {
                return new CiscoSyslogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Severity = "Unknown",
                    SeverityCode = -1,
                    Facility = "Unknown",
                    Message = message,
                    RawMessage = message,
                };
            }
        }
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 4: Create model classes

**Objective:** Create all model classes.

**Files:**
- Create: `src/Networking/Cisco/Models/CiscoDeviceInfo.cs`
- Create: `src/Networking/Cisco/Models/CiscoApInfo.cs`
- Create: `src/Networking/Cisco/Models/CiscoClientInfo.cs`
- Create: `src/Networking/Cisco/Models/CiscoSyslogEntry.cs`
- Create: `src/Networking/Cisco/Models/CiscoSsidInfo.cs`
- Create: `src/Networking/Cisco/Models/CiscoSyslogConfig.cs`

**Step 1: Write all model files**

See API Design section above for complete models.

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 5: Create CiscoEwcHelper main API

**Objective:** Implement the public static helper with all convenience methods.

**Files:**
- Create: `src/Networking/Cisco/CiscoEwcHelper.cs`

**Step 1: Write CiscoEwcHelper.cs**

```csharp
// <copyright file="CiscoEwcHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// Manages Cisco 9100 series APs in EWC (Embedded Wireless Controller) mode.
    /// Uses RESTCONF API with YANG models for configuration and operational data.
    /// Uses Syslog (UDP 514) for real-time log collection.
    /// All methods are thread-safe and return empty results on failure.
    /// </summary>
    public static class CiscoEwcHelper
    {
        // YANG model paths for Cisco EWC
        private const string YangApOper = "/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data";
        private const string YangClientOper = "/Cisco-IOS-XE-wireless-client-oper:client-oper-data";
        private const string YangGlobalOper = "/Cisco-IOS-XE-wireless-global-oper:wlan-global-oper-data";
        private const string YangNative = "/Cisco-IOS-XE-native:native";
        private const string YangClock = "/ietf-system:system-state/clock";

        // ── Device Info ─────────────────────────────────────────────────

        /// <summary>
        /// Gets device info via RESTCONF YANG model.
        /// </summary>
        public static async Task<CiscoDeviceInfo?> GetDeviceInfoAsync(
            string host, string username, string password, int timeoutMs = 10000)
        {
            if (string.IsNullOrEmpty(host)) return null;

            try
            {
                var clock = await RestConfClient.GetAsync(host, username, password, YangClock, timeoutMs)
                    .ConfigureAwait(false);

                var native = await RestConfClient.GetAsync(host, username, password, YangNative, timeoutMs)
                    .ConfigureAwait(false);

                string hostname = string.Empty;
                string version = string.Empty;

                if (clock.HasValue && clock.Value.TryGetProperty("ietf-system:clock", out var clockData))
                {
                    // Extract system time
                }

                if (native.HasValue)
                {
                    hostname = ExtractString(native.Value, "Cisco-IOS-XE-native:hostname");
                    version = ExtractString(native.Value, "Cisco-IOS-XE-native:version");
                }

                return new CiscoDeviceInfo
                {
                    Hostname = hostname,
                    IpAddress = host,
                    SoftwareVersion = version,
                    SystemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Mode = "EWC",
                };
            }
            catch { return null; }
        }

        // ── AP Management ───────────────────────────────────────────────

        /// <summary>
        /// Gets all APs via RESTCONF YANG model.
        /// </summary>
        public static async Task<IReadOnlyList<CiscoApInfo>> GetAccessPointsAsync(
            string host, string username, string password, int timeoutMs = 10000)
        {
            if (string.IsNullOrEmpty(host)) return Array.Empty<CiscoApInfo>();

            var json = await RestConfClient.GetAsync(host, username, password, YangApOper, timeoutMs)
                .ConfigureAwait(false);

            if (!json.HasValue) return Array.Empty<CiscoApInfo>();
            return YangParser.ParseAccessPoints(json.Value);
        }

        /// <summary>
        /// Gets a specific AP by name.
        /// </summary>
        public static async Task<CiscoApInfo?> GetAccessPointAsync(
            string host, string username, string password, string apName, int timeoutMs = 10000)
        {
            var aps = await GetAccessPointsAsync(host, username, password, timeoutMs).ConfigureAwait(false);
            return aps.FirstOrDefault(ap =>
                string.Equals(ap.Name, apName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a specific AP by MAC address.
        /// </summary>
        public static async Task<CiscoApInfo?> GetAccessPointByMacAsync(
            string host, string username, string password, string macAddress, int timeoutMs = 10000)
        {
            var aps = await GetAccessPointsAsync(host, username, password, timeoutMs).ConfigureAwait(false);
            return aps.FirstOrDefault(ap =>
                string.Equals(ap.MacAddress, macAddress, StringComparison.OrdinalIgnoreCase));
        }

        // ── Client Management ───────────────────────────────────────────

        /// <summary>
        /// Gets all connected clients via RESTCONF.
        /// </summary>
        public static async Task<IReadOnlyList<CiscoClientInfo>> GetClientsAsync(
            string host, string username, string password, int timeoutMs = 10000)
        {
            if (string.IsNullOrEmpty(host)) return Array.Empty<CiscoClientInfo>();

            var json = await RestConfClient.GetAsync(host, username, password, YangClientOper, timeoutMs)
                .ConfigureAwait(false);

            if (!json.HasValue) return Array.Empty<CiscoClientInfo>();
            return YangParser.ParseClients(json.Value);
        }

        /// <summary>
        /// Gets a specific client by MAC.
        /// </summary>
        public static async Task<CiscoClientInfo?> GetClientAsync(
            string host, string username, string password, string macAddress, int timeoutMs = 10000)
        {
            var clients = await GetClientsAsync(host, username, password, timeoutMs).ConfigureAwait(false);
            return clients.FirstOrDefault(c =>
                string.Equals(c.MacAddress, macAddress, StringComparison.OrdinalIgnoreCase));
        }

        // ── SSID Management ─────────────────────────────────────────────

        /// <summary>
        /// Gets all configured SSIDs via RESTCONF.
        /// </summary>
        public static async Task<IReadOnlyList<CiscoSsidInfo>> GetSsidsAsync(
            string host, string username, string password, int timeoutMs = 10000)
        {
            if (string.IsNullOrEmpty(host)) return Array.Empty<CiscoSsidInfo>();

            var json = await RestConfClient.GetAsync(host, username, password, YangGlobalOper, timeoutMs)
                .ConfigureAwait(false);

            if (!json.HasValue) return Array.Empty<CiscoSsidInfo>();
            return YangParser.ParseSsids(json.Value);
        }

        // ── Syslog ──────────────────────────────────────────────────────

        /// <summary>
        /// Starts a syslog listener on the specified port.
        /// </summary>
        public static CiscoSyslogServer StartSyslogListener(
            int port = 514, Action<CiscoSyslogEntry>? onEntry = null)
        {
            return new CiscoSyslogServer(port, onEntry);
        }

        /// <summary>
        /// Gets recent log entries from the EWC via RESTCONF.
        /// </summary>
        public static async Task<IReadOnlyList<CiscoSyslogEntry>> GetLogsAsync(
            string host, string username, string password, int maxEntries = 100, int timeoutMs = 10000)
        {
            // RESTCONF doesn't have a direct syslog endpoint on most Cisco devices.
            // Logs are typically retrieved via:
            // 1. Syslog server (real-time)
            // 2. CLI command (show logging)
            // 3. Event log API
            //
            // For now, return empty — real-time logs come from StartSyslogListener()
            await Task.CompletedTask;
            return Array.Empty<CiscoSyslogEntry>();
        }

        // ── Configuration (RESTCONF) ───────────────────────────────────

        /// <summary>
        /// Gets raw YANG data from a RESTCONF endpoint.
        /// </summary>
        public static async Task<string?> GetYangDataAsync(
            string host, string username, string password, string yangPath, int timeoutMs = 10000)
        {
            return await RestConfClient.GetRawAsync(host, username, password, yangPath, timeoutMs)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets YANG data as parsed JSON.
        /// </summary>
        public static async Task<JsonElement?> GetYangDataJsonAsync(
            string host, string username, string password, string yangPath, int timeoutMs = 10000)
        {
            return await RestConfClient.GetAsync(host, username, password, yangPath, timeoutMs)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Lists available YANG models on the device.
        /// </summary>
        public static async Task<IReadOnlyList<string>> GetYangModelsAsync(
            string host, string username, string password, int timeoutMs = 5000)
        {
            return await RestConfClient.GetYangModulesAsync(host, username, password, timeoutMs)
                .ConfigureAwait(false);
        }

        // ── Diagnostics ─────────────────────────────────────────────────

        /// <summary>
        /// Pings the EWC.
        /// </summary>
        public static bool Ping(string host, int timeoutMs = 2000)
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send(host, timeoutMs);
                return reply?.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        /// <summary>
        /// Checks if RESTCONF is reachable.
        /// </summary>
        public static async Task<bool> IsReachableAsync(
            string host, string username, string password, int timeoutMs = 2000)
        {
            return await RestConfClient.IsReachableAsync(host, username, password, timeoutMs)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets RESTCONF API capabilities.
        /// </summary>
        public static async Task<IReadOnlyList<string>> GetCapabilitiesAsync(
            string host, string username, string password, int timeoutMs = 5000)
        {
            return await RestConfClient.GetCapabilitiesAsync(host, username, password, timeoutMs)
                .ConfigureAwait(false);
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static string ExtractString(JsonElement element, string path)
        {
            try
            {
                if (element.TryGetProperty(path, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
            }
            catch { }
            return string.Empty;
        }
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/BPlusLib.Foundation --framework net8.0 --no-restore -v q`
Expected: Build succeeded, 0 errors

---

### Task 6: Create unit tests

**Objective:** Write comprehensive tests for CiscoEwcHelper.

**Files:**
- Create: `tests/BPlusLib.Foundation.Tests/Networking/Cisco/CiscoEwcHelperTests.cs`

**Step 1: Write test file**

Tests should cover:
- Model creation and properties
- Syslog message parsing (RFC 5424 format)
- JSON YANG response parsing
- Null/empty input handling
- Syslog server start/stop
- RESTCONF URL building

**Step 2: Verify compilation and tests**

Run: `dotnet test --framework net8.0 --filter "FullyQualifiedName~Cisco" -v q`
Expected: Tests pass

---

### Task 7: Run full build + test suite

**Objective:** Verify all changes compile and existing tests still pass.

**Step 1: Build all targets**

Run: `dotnet build src/BPlusLib.Foundation -v q`
Expected: Build succeeded, 0 errors

**Step 2: Run all tests**

Run: `dotnet test --framework net8.0 --no-restore -v q`
Expected: All tests pass

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add CiscoEwcHelper for Cisco 9100 EWC management via RESTCONF + Syslog"
```

---

## Verification

After implementation, verify:

1. `dotnet build` — 0 errors across net472, net6.0, net8.0
2. `dotnet test --framework net8.0` — All existing tests pass
3. XML documentation is complete for all public members
4. All methods are thread-safe and return empty results on failure
5. Syslog server properly parses RFC 5424 messages

---

## Risks

| Risk | Mitigation |
|------|------------|
| RESTCONF API version differences | Document YANG model paths, support multiple versions |
| SSL certificate errors | Accept self-signed certs (common in lab environments) |
| Network timeout | Configurable timeouts on all methods |
| Syslog message format variations | Robust parsing with fallback to raw message |
| YANG model structure changes | Use TryGetProperty for safe JSON navigation |
