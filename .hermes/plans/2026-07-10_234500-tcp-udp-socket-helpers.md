# TCP/UDP Socket Helpers Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add `TcpSocketHelper`, `UdpSocketHelper`, and supporting types to BPlusLib.Foundation.Networking for inter-process communication via TCP and UDP sockets.

**Architecture:** Three new public classes in `BPlusLib.Foundation.Networking` — stateful wrappers around `System.Net.Sockets.TcpClient`, `TcpListener`, and `UdpClient`. All socket APIs are part of the BCL and cross-platform (no P/Invoke needed). The module follows the existing library patterns: thread-safe, nullable-enabled, exception-safe (never throws), XML-documented.

**Tech Stack:** .NET `System.Net.Sockets` (TcpClient, TcpListener, UdpClient, Socket, NetworkStream), `System.Threading.Tasks` (async patterns), `System.Text` (encoding helpers). All built-in — no NuGet dependencies.

**No P/Invoke needed.** All socket APIs are managed .NET. Tests can run fully on Linux with loopback.

---

## Proposed API

### `TcpConnection` (class, IDisposable)
Stateful wrapper around `TcpClient` + `NetworkStream`.

```csharp
public sealed class TcpConnection : IDisposable
{
    public bool Connected { get; }
    public int Available { get; }
    public EndPoint? LocalEndPoint { get; }
    public EndPoint? RemoteEndPoint { get; }

    public bool Send(byte[] data, int offset = 0, int? count = null);
    public byte[]? Receive(int bufferSize = 4096, int timeoutMs = 5000);
    public string? ReceiveString(int bufferSize = 4096, int timeoutMs = 5000, Encoding? encoding = null);

    public Task<bool> SendAsync(byte[] data, int offset = 0, int? count = null);
    public Task<byte[]?> ReceiveAsync(int bufferSize = 4096, int timeoutMs = 5000);
    public Task<string?> ReceiveStringAsync(int bufferSize = 4096, int timeoutMs = 5000, Encoding? encoding = null);

    public void Close();
    public void Dispose();
}
```

### `TcpServer` (class, IDisposable)
Stateful wrapper around `TcpListener`.

```csharp
public sealed class TcpServer : IDisposable
{
    public int Port { get; }
    public bool IsRunning { get; }

    public TcpConnection? Accept(int timeoutMs = Timeout.Infinite);
    public Task<TcpConnection?> AcceptAsync();

    public void Stop();
    public void Dispose();
}
```

### `UdpEndpoint` (class, IDisposable)
Stateful wrapper around `UdpClient`.

```csharp
public sealed class UdpEndpoint : IDisposable
{
    public int Port { get; }
    public bool EnableBroadcast { get; set; }

    public bool Send(byte[] data, string host, int port);
    public bool Send(byte[] data, EndPoint remoteEndpoint);
    public (byte[]? Data, IPEndPoint? RemoteEndPoint)? Receive(int timeoutMs = 5000);

    public Task<bool> SendAsync(byte[] data, string host, int port);
    public Task<(byte[]? Data, IPEndPoint? RemoteEndPoint)?> ReceiveAsync(int timeoutMs = 5000);

    public bool JoinMulticastGroup(IPAddress multicastAddress);
    public bool DropMulticastGroup(IPAddress multicastAddress);

    public void Close();
    public void Dispose();
}
```

### Static entry-point

```csharp
public static class TcpSocketHelper
{
    public static TcpConnection? Connect(string host, int port, int timeoutMs = 5000);
    public static Task<TcpConnection?> ConnectAsync(string host, int port, int timeoutMs = 5000);
    public static TcpServer? StartServer(int port, IPAddress? address = null);
    public static Task<TcpServer?> StartServerAsync(int port, IPAddress? address = null);
}

public static class UdpSocketHelper
{
    public static UdpEndpoint? CreateEndpoint(int? localPort = null);
    public static bool SendDatagram(byte[] data, string host, int port, int? localPort = null, int timeoutMs = 5000);
    public static byte[]? ReceiveDatagram(int port, int timeoutMs = 5000);
    public static bool Broadcast(byte[] data, int port, int? localPort = null);
}
```

---

## Task Breakdown

### Task 1: Create `TcpConnection` class

**Objective:** Implement the stateful TCP connection wrapper with sync/async send/receive.

**Files:**
- Create: `src/BPlusLib.Foundation/Networking/TcpConnection.cs`
- Test: `tests/BPlusLib.Foundation.Tests/Networking/TcpConnectionTests.cs`

**Key details:**
- Wraps `TcpClient` internally. Gets `NetworkStream` lazily on first send/receive.
- `Send()`: `NetworkStream.Write()` with `timeoutMs` → `NetworkStream.WriteTimeout`.
- `Receive()`: `NetworkStream.Read()` into buffer. Set `ReadTimeout`. Returns null on timeout or disconnect.
- `ReceiveString()`: call Receive() then `Encoding.UTF8.GetString()`.
- `Async`: `NetworkStream.WriteAsync()` / `ReadAsync()`.
- `Connected`: `TcpClient.Connected` (peek by pinging, or just the property).
- `Close()`: calls `NetworkStream.Close()` then `TcpClient.Close()`.
- `Dispose()`: calls Close().
- `Available`: `TcpClient.Available`.
- `LocalEndPoint` / `RemoteEndPoint`: cast `TcpClient.Client.LocalEndPoint` / `RemoteEndPoint`.

**Thread safety:** Use `SemaphoreSlim(1,1)` to serialize send and receive on the same connection. Two separate semaphores — one for send, one for receive — so full-duplex is possible.

**Error handling:** All public methods wrapped in try/catch. Return false/null on failure. Never throw.

**Test strategy:**
- Start a `TcpServer` in test, connect with `TcpConnection`, send data, verify echo/response.
- Test timeout by connecting but not sending, verify Receive returns null after timeout.
- Test async variants.
- Test Dispose, Close, double-Dispose.
- Use port 0 (let OS assign) for the test server to avoid port conflicts.

---

### Task 2: Create `TcpServer` class

**Objective:** Implement the TCP server wrapper that accepts connections.

**Files:**
- Create: `src/BPlusLib.Foundation/Networking/TcpServer.cs`
- Test: `tests/BPlusLib.Foundation.Tests/Networking/TcpServerTests.cs`

**Key details:**
- Wraps `TcpListener` internally. Constructor takes port + optional IPAddress (default IPAddress.Any = `0.0.0.0`).
- `Accept()`: `TcpListener.AcceptTcpClient()` wrapped with timeout. On timeout, return null.
  - Timeout implementation: use `Task.WhenAny(AcceptAsync(), Task.Delay(timeoutMs))`. If delay wins, return null.
- `AcceptAsync()`: `TcpListener.AcceptTcpClientAsync()` → wrap in `Task<TcpConnection?>`.
- `Start()` called in constructor, `Stop()` → `TcpListener.Stop()`.
- `IsRunning`: bool flag set on Start/Stop.
- `Port`: the actual port (for port 0, read `((IPEndPoint)TcpListener.LocalEndpoint).Port`).
- `Dispose()`: calls `Stop()`.

**Error handling:** All wrapped in try/catch. Return null on failure.

**Test strategy:**
- Create TcpServer on port 0, connect via TcpClient, verify Accept returns a TcpConnection.
- Connect two clients, verify both accepted.
- Test Stop then Accept returns null.
- Test Accept timeout (call Accept with 100ms timeout, don't connect, verify returns null).

---

### Task 3: Create `TcpSocketHelper` static class

**Objective:** Provide static convenience methods for quick TCP operations.

**Files:**
- Create: `src/BPlusLib.Foundation/Networking/TcpSocketHelper.cs`
- Test: `tests/BPlusLib.Foundation.Tests/Networking/TcpSocketHelperTests.cs`

**Key details:**
- `Connect()`: creates `TcpClient`, calls `client.ConnectAsync(host, port).Wait(timeoutMs)`.
- `ConnectAsync()`: `await client.ConnectAsync(host, port)` with cancellation token from timeout.
- `StartServer()`: creates `TcpServer`, calls start.
- `StartServerAsync()`: `Task.Run` or passed through.

**Error handling:** All wrapped. On connect timeout, close the TcpClient and return null.

**Test strategy:**
- Connect to localhost on a known invalid port → returns null.
- Start server on port 0, Connect to it → returns TcpConnection.
- ConnectAsync and StartServerAsync.

---

### Task 4: Create `UdpEndpoint` class

**Objective:** Implement stateful UDP wrapper with send/receive, broadcast, multicast.

**Files:**
- Create: `src/BPlusLib.Foundation/Networking/UdpEndpoint.cs`
- Test: `tests/BPlusLib.Foundation.Tests/Networking/UdpEndpointTests.cs`

**Key details:**
- Wraps `UdpClient` internally.
- Constructor takes optional local port. If null, let OS assign.
- `Send(byte[], string host, int port)`: `UdpClient.Send(data, data.Length, host, port)`.
- `Send(byte[], EndPoint)`: `UdpClient.Send(data, data.Length, remoteEndpoint)`.
- `Receive(int timeoutMs)`: `UdpClient.Receive()` with `UdpClient.Client.ReceiveTimeout` set. Returns tuple of data + sender endpoint. Null on timeout.
- `SendAsync`: `UdpClient.SendAsync()`.
- `ReceiveAsync`: `UdpClient.ReceiveAsync()` with timeout via `Task.WhenAny`.
- `JoinMulticastGroup(IPAddress)`: `UdpClient.JoinMulticastGroup()`.
- `DropMulticastGroup(IPAddress)`: `UdpClient.DropMulticastGroup()`.
- `EnableBroadcast`: `UdpClient.EnableBroadcast`.
- `Close()` / `Dispose()`: `UdpClient.Close()`.

**Thread safety:** `SemaphoreSlim(1,1)` for send. Separate semaphore for receive.

**Test strategy:**
- Create two UdpEndpoints on different ports, send from A to B, verify B receives.
- Test timeout (receive on unoccupied port, timeout, verify null).
- Test EnableBroadcast.
- Test async variants.
- Test multicast (use 224.0.0.0/24 range which is safe for local testing).

---

### Task 5: Create `UdpSocketHelper` static class

**Objective:** Provide static convenience methods for quick UDP operations.

**Files:**
- Create: `src/BPlusLib.Foundation/Networking/UdpSocketHelper.cs`
- Test: `tests/BPlusLib.Foundation.Tests/Networking/UdpSocketHelperTests.cs`

**Key details:**
- `CreateEndpoint(int? localPort)`: creates UdpEndpoint.
- `SendDatagram(data, host, port, localPort, timeout)`: creates ephemeral endpoint, sends, closes.
- `ReceiveDatagram(port, timeout)`: creates endpoint on port, receives once, closes.
- `Broadcast(data, port, localPort)`: creates endpoint with EnableBroadcast=true, sends to 255.255.255.255:port.

**Test strategy:**
- SendDatagram to a receiving endpoint and verify delivery.
- ReceiveDatagram on an occupied port.
- Broadcast on localhost.

---

### Task 6: Write integration test

**Objective:** End-to-end test demonstrating TCP client-server round-trip.

**Files:**
- Modify: `tests/BPlusLib.Foundation.Tests/Networking/SocketIntegrationTests.cs` (new file)

**Test scenarios:**
1. TCP: start server → connect → send "hello" → receive → verify "hello" received
2. TCP: concurrent clients (2 clients, server accepts both)
3. UDP: two-way messaging (A sends to B, B sends to A)
4. UDP: broadcast (send to broadcast, receive on listener)

**Test pattern (shared for all test files):**
```csharp
// Arrange
using var server = TcpSocketHelper.StartServer(port: 0);
int actualPort = server!.Port;

// Act
using var client = TcpSocketHelper.Connect("127.0.0.1", actualPort);
client.Should().NotBeNull();
client!.Send(Encoding.UTF8.GetBytes("ping"));

// Accept on server
using var accepted = server.Accept(timeoutMs: 3000);
accepted.Should().NotBeNull();
var received = accepted!.ReceiveString(timeoutMs: 3000);

// Assert
received.Should().Be("ping");
```

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `src/BPlusLib.Foundation/Networking/TcpConnection.cs` |
| Create | `src/BPlusLib.Foundation/Networking/TcpServer.cs` |
| Create | `src/BPlusLib.Foundation/Networking/TcpSocketHelper.cs` |
| Create | `src/BPlusLib.Foundation/Networking/UdpEndpoint.cs` |
| Create | `src/BPlusLib.Foundation/Networking/UdpSocketHelper.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Networking/TcpConnectionTests.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Networking/TcpServerTests.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Networking/TcpSocketHelperTests.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Networking/UdpEndpointTests.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Networking/UdpSocketHelperTests.cs` |
| Create | `tests/BPlusLib.Foundation.Tests/Networking/SocketIntegrationTests.cs` |

No modifications to existing files. No new NuGet packages.

---

## Verification

1. **Build**: `dotnet build src/BPlusLib.Foundation/BPlusLib.Foundation.csproj -c Release` → 0 errors, 0 warnings
2. **Tests**: `dotnet test tests/BPlusLib.Foundation.Tests/BPlusLib.Foundation.Tests.csproj --framework net8.0` → all ~1000 tests pass (846 existing + ~50 new)
3. **Pack**: `dotnet pack src/BPlusLib.Foundation/BPlusLib.Foundation.csproj -c Release -o packages` → produces `BPlusLib.Foundation.2.5.0.nupkg`
4. **Push**: `git push origin main && dotnet nuget push packages/BPlusLib.Foundation.2.5.0.nupkg -k $GITHUB_TOKEN -s https://nuget.pkg.github.com/thanhbinhqs/index.json`

---

## Risks & Tradeoffs

1. **Port conflicts**: Tests use port 0 (OS-assigned) for servers, which eliminates this risk. The static convenience methods (SendDatagram, ReceiveDatagram) also bind ephemerally.

2. **Firewall**: Loopback (127.0.0.1) connections are never blocked by Windows firewall. All tests use loopback.

3. **Async on net472**: `UdpClient.ReceiveAsync()` and `TcpListener.AcceptTcpClientAsync()` are available on net472 since .NET Framework 4.5. The `SendAsync` on `UdpClient` is also available. No polyfill needed.

4. **Thread safety with SemaphoreSlim**: `SemaphoreSlim(1,1)` ensures sequential access. `WaitAsync()` is not available on net472 → use `Wait()` in `Task.Run()` or a helper.

5. **Timeout on Accept**: `TcpListener` has no native Accept timeout. We implement via `Task.WhenAny(acceptTask, Task.Delay(timeout))` which works on all targets.

---

## Open Questions

- Should `TcpConnection.Receive()` return `byte[]?` (null = timeout/disconnect) or `(byte[] Data, bool Success)?`? → Decision: `byte[]?` is simpler. The caller checks for null to detect timeout/disconnect. This matches the existing library pattern (nullable returns = failure).

- Should socket helpers support SSL/TLS? → No (YAGNI). This is IPC, not internet. The user said "giao tiếp giữa các phần mềm" (inter-process communication). TLS can be added later if needed.

- Should we support Unix Domain Sockets? → No (YAGNI). Not available on net472. Can be added when net472 is dropped.

- Port range for tests? → Use port 0 (OS assigns). For UDP tests that need to bind to a specific receive port, use a high ephemeral port (>49152) or port 0 and read the actual port from the endpoint.
