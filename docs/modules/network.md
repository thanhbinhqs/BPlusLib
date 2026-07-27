# Network

Low-level network information via P/Invoke to IPHLPAPI and built-in .NET networking APIs. Provides ICMP ping, TCP/UDP connection enumeration, ARP table reading, DNS lookup, Wake-on-LAN, and VPN detection. No WMI is used.

## Enums

### MibTcpState
TCP connection states as defined by the MIB (RFC 793).

| Value | Description |
|-------|-------------|
| Closed | Closed |
| Listen | Listening |
| SynSent | SYN sent |
| SynRcvd | SYN received |
| Established | Established |
| FinWait1 | FIN WAIT 1 |
| FinWait2 | FIN WAIT 2 |
| CloseWait | Close wait |
| Closing | Closing |
| LastAck | Last ACK |
| TimeWait | Time wait |
| DeleteTcb | Delete TCB |

### ArpEntryState
ARP cache entry states.

| Value | Description |
|-------|-------------|
| Incomplete | Address resolution in progress |
| Reachable | Entry is valid and reachable |
| Stale | Entry is stale |
| Delay | Delay before probing |
| Probe | Actively probing the address |
| Invalid | Entry is invalid |
| Unknown | Unknown state |
| Permanent | Static entry |
| Published | Proxy ARP entry |
| Other | Undefined entry type |
| Dynamic | Added dynamically via ARP |
| Static | Added statically |

## Classes

### PingResult
Represents the result of an ICMP ping operation.

| Property | Returns | Description |
|----------|---------|-------------|
| Success | bool | Whether the ping succeeded |
| RoundtripTimeMs | long | Round-trip time in milliseconds, or -1 if failed |
| Status | string? | Human-readable status description, or null on success |
| IpAddress | string? | The IP address that replied |

### TcpConnectionInfo
Represents a TCP connection entry from the system's connection table.

| Property | Returns | Description |
|----------|---------|-------------|
| LocalAddress | string | Local IP address string |
| LocalPort | int | Local port number |
| RemoteAddress | string | Remote IP address string |
| RemotePort | int | Remote port number |
| State | MibTcpState | TCP connection state |
| OwningPid | int | PID of the process that owns this connection |
| OwningProcessName | string? | Process name, or null if not resolved |

### UdpListenerInfo
Represents a UDP listener entry from the system's listener table.

| Property | Returns | Description |
|----------|---------|-------------|
| LocalAddress | string | Local IP address string |
| LocalPort | int | Local port number |
| OwningPid | int | PID of the process that owns this listener |
| OwningProcessName | string? | Process name, or null if not resolved |

### ArpTableEntry
Represents a single entry in the system's ARP table.

| Property | Returns | Description |
|----------|---------|-------------|
| IpAddress | string | The IP address of the entry |
| MacAddress | string? | The MAC address as colon-separated hex, or null |
| InterfaceIndex | string | The interface index |
| State | ArpEntryState | The state of the ARP entry |

### NetHelper
Provides low-level network information via P/Invoke to IPHLPAPI and built-in .NET networking APIs. No WMI is used anywhere.

| Method | Returns | Description |
|--------|---------|-------------|
| Ping(string host, int timeoutMs, int ttl) | PingResult | Sends an ICMP echo request to the specified host |
| GetTcpConnections() | IReadOnlyList\<TcpConnectionInfo\> | Enumerates all active TCP connections with owning process PID |
| GetUdpListeners() | IReadOnlyList\<UdpListenerInfo\> | Enumerates all UDP listeners with owning process PID |
| GetArpTable() | ArpTableEntry[] | Enumerates the system's ARP table |
| LookupDns(string hostname) | string[] | Performs a DNS lookup and returns all associated IP addresses |
| WakeOnLan(string macAddress, string? broadcastIp) | bool | Sends a Wake-on-LAN magic packet to the specified MAC address |
| IsVpnConnected() | bool | Determines whether a VPN connection is currently active |

## Usage

```csharp
using BPlusLib.Foundation.Network;

// Ping a host
var pingResult = NetHelper.Ping("8.8.8.8", timeoutMs: 3000);
Console.WriteLine($"Reply: {pingResult.Success}, Time: {pingResult.RoundtripTimeMs}ms");

// List TCP connections
var tcpConnections = NetHelper.GetTcpConnections();
foreach (var conn in tcpConnections)
{
    Console.WriteLine($"{conn.State}: {conn.LocalAddress}:{conn.LocalPort} -> {conn.RemoteAddress}:{conn.RemotePort} (PID={conn.OwningPid})");
}

// List UDP listeners
var udpListeners = NetHelper.GetUdpListeners();
foreach (var listener in udpListeners)
{
    Console.WriteLine($"UDP {listener.LocalAddress}:{listener.LocalPort} (PID={listener.OwningPid})");
}

// Read ARP table
var arpEntries = NetHelper.GetArpTable();
foreach (var entry in arpEntries)
{
    Console.WriteLine($"{entry.IpAddress} -> {entry.MacAddress} [{entry.State}]");
}

// DNS lookup
string[] addresses = NetHelper.LookupDns("example.com");

// Wake-on-LAN
NetHelper.WakeOnLan("AA:BB:CC:DD:EE:FF");

// VPN detection
bool vpnActive = NetHelper.IsVpnConnected();
```

## Dependencies
- `iphlpapi.dll` (P/Invoke for `GetExtendedTcpTable`, `GetExtendedUdpTable`, `GetIpNetTable`, `GetIpNetTable2`)
- `kernel32.dll` (P/Invoke for `OpenProcess`, `CloseHandle`, `GetModuleBaseName`)
- `BPlusLib.Foundation.SystemInfo` (for `NetworkInfo.GetAllAdapters` used by VPN detection)
- Windows-only (P/Invoke to Windows networking APIs)
