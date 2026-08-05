# VirtualSerial Module

Virtual serial port routing platform for Windows — endpoint abstractions, routing engine, frame decoders, write arbitration, and modem signal mapping.

## Overview

The VirtualSerial module provides a complete abstraction layer for virtual serial port communication. It enables:

- **Endpoint abstraction** — unified interface for physical, virtual, TCP, UDP, TLS endpoints
- **Routing engine** — flexible data routing between endpoints
- **Frame decoders** — 7 framing strategies for different protocols
- **Write arbitration** — 3 policies for concurrent write handling
- **Modem signal mapping** — configurable signal mapping for pairs
- **Configuration** — JSON-based configuration with validation

## Classes

### ISerialEndpoint

Core interface for all serial endpoints.

| Property/Method | Returns | Description |
|-----------------|---------|-------------|
| Id | Guid | Unique endpoint identifier |
| Type | EndpointType | Endpoint type enum |
| Name | string | Human-readable name |
| IsRunning | bool | Whether endpoint is active |
| Settings | SerialSettings | Baud rate, parity, etc. |
| ModemSignals | ModemSignals | Current modem state |
| StartAsync() | ValueTask | Start the endpoint |
| StopAsync() | ValueTask | Stop the endpoint |
| SendAsync(data) | ValueTask | Send data |
| ReadAllAsync() | IAsyncEnumerable<SerialFrame> | Read incoming frames |
| PurgeAsync(flags) | ValueTask | Clear buffers |
| SetModemControlAsync(dtr, rts) | ValueTask | Set DTR/RTS |
| SetBreakAsync(on) | ValueTask | Set break signal |

### Endpoint Implementations

| Class | Type | Description |
|-------|------|-------------|
| PhysicalSerialEndpoint | PhysicalSerial | System.IO.Ports wrapper |
| TcpClientEndpoint | TcpClient | TCP client with reconnect |
| TcpServerEndpoint | TcpServer | TCP server with multi-client |
| UdpSerialEndpoint | Udp | UDP datagram bridge |
| TlsClientEndpoint | TlsClient | TLS-encrypted TCP client |
| VirtualComEndpoint | VirtualSerial | Placeholder for KMDF driver |

### IFrameDecoder

Decodes byte streams into frames.

| Class | Description |
|-------|-------------|
| RawFramer | No framing (pass-through) |
| DelimiterFramer | Split on delimiter bytes |
| FixedLengthFramer | Fixed N bytes per frame |
| IdleTimeoutFramer | Gap-based framing |
| ModbusRtuFramer | 3.5 char silence interval |
| StxEtxFramer | STX (0x02) / ETX (0x03) boundaries |

### IWriteArbiter

Controls concurrent write behavior.

| Class | Policy |
|-------|--------|
| SerializedWriteArbiter | Atomic frames, no interleaving |
| SingleWriterArbiter | One writer at a time |
| TransactionArbiter | Acquire/send/release lifecycle |

### RouteEngine

Manages routes between endpoints.

| Method | Description |
|--------|-------------|
| AddEndpoint() | Register an endpoint |
| AddRoute() | Define a route |
| StartRouteAsync() | Start routing data |
| StopRouteAsync() | Stop routing |
| GetStatistics() | Get route statistics |

### ModemSignalMapper

Maps modem control signals between endpoints.

| Mapping | Default |
|---------|---------|
| RTS → CTS | ✅ |
| DTR → DSR | ✅ |
| DTR → DCD | ✅ |
| Ring Indicator | Manual |

## Usage

### Create endpoints and route between them

```csharp
using BPlusLib.Foundation.VirtualSerial;
using BPlusLib.Foundation.VirtualSerial.Endpoints;
using BPlusLib.Foundation.VirtualSerial.Routing;

// Create endpoints
var com20 = VirtualSerialHelper.CreateTcpClient("COM20", "192.168.1.100", 5000);
var com21 = VirtualSerialHelper.CreateTcpServer("COM21", 5001);

// Create route engine
using var engine = VirtualSerialHelper.CreateRouteEngine();
engine.AddEndpoint(com20);
engine.AddEndpoint(com21);

// Define route
var route = new SerialRoute
{
    Type = RouteType.Pair,
    Sources = new[] { com20.Id },
    Destinations = new[] { com21.Id }
};
engine.AddRoute(route);

// Start routing
await engine.StartRouteAsync(route.Id);
```

### Frame decoding

```csharp
using BPlusLib.Foundation.VirtualSerial.Framing;

// CR/LF delimiter
var framer = VirtualSerialHelper.CreateCrLfFramer();
framer.Feed(data);
while (framer.TryGetFrame(out var frame))
{
    Console.WriteLine($"Frame: {frame.Length} bytes");
}

// Modbus RTU (3.5 char silence)
var modbus = VirtualSerialHelper.CreateModbusRtuFramer(9600);
modbus.Feed(data);
while (modbus.TryGetFrame(out var frame))
{
    // Process Modbus frame
}
```

### Write arbitration

```csharp
using BPlusLib.Foundation.VirtualSerial.Arbitration;

// Serialized writes (atomic frames)
var arbiter = VirtualSerialHelper.CreateSerializedArbiter();
using var token = await arbiter.AcquireAsync(sessionId);
await endpoint.SendAsync(data);
// Token auto-releases on dispose
```

### Configuration

```csharp
using BPlusLib.Foundation.VirtualSerial.Configuration;

// Load from file
var config = VirtualSerialHelper.LoadConfig("config.json");

// Create default
var config = VirtualSerialHelper.CreateDefaultConfig();
VirtualSerialHelper.SaveConfig(config, "config.json");
```

### Modem signal mapping

```csharp
using BPlusLib.Foundation.VirtualSerial.Modem;

var mapper = VirtualSerialHelper.CreatePairModemMapper();
var forB = mapper.MapToDestination(signalsFromA, currentB);
// B.RTS → A.CTS, B.DTR → A.DSR, B.DTR → A.DCD
```

## Dependencies

| Package | Version |
|---------|---------|
| Newtonsoft.Json | 13.0.3 |
| System.IO.Ports | 8.0.0 |

## Targets

- net472
- net6.0
- net8.0

## Related Modules

- **Networking** — TcpConnection, TcpServer, UdpEndpoint (reused by endpoints)
- **IPC** — PipeServer/PipeClient (future driver communication)
- **Common** — Guard, AsyncLock, Result<T>
