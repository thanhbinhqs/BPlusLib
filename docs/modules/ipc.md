# IPC

Named pipe inter-process communication (IPC) for Windows, providing server, client, and one-shot transaction helpers using Windows named pipe APIs.

## Classes

### PipeServer
Thread-safe named pipe server based on Windows named pipe APIs. Supports byte-mode pipes with wait-type connections.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| PipeServer(string pipeName, uint maxInstances, uint bufferSize) | PipeServer | Initializes a new pipe server instance |
| PipePath | string | Gets the full pipe path (e.g., `\\.\pipe\MyPipe`) |
| WaitForConnection(int timeoutMs) | bool | Waits for a client to connect to the pipe |
| Read(int maxBytes) | byte[]? | Reads data from the connected pipe |
| Write(byte[] data) | bool | Writes data to the connected pipe |
| Disconnect() | bool | Disconnects the current client for reuse |
| ImpersonateClient() | bool | Impersonates the connected client's security context |
| RevertToSelf() | static bool | Reverts from impersonation back to the original security context |
| Dispose() | void | Disposes the pipe server, closing the underlying handle |

### PipeClient
Named pipe client for connecting to a local Windows named pipe server. Uses CallNamedPipeW for one-shot transactions and CreateFileW for session-oriented communication.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| PipeClient(string pipeName) | PipeClient | Initializes a new pipe client instance |
| PipePath | string | Gets the full pipe path |
| IsConnected | bool | Gets whether the client is currently connected |
| Connect(int timeoutMs) | bool | Connects to the named pipe server |
| Read(int maxBytes) | byte[]? | Reads data from the connected pipe |
| Write(byte[] data) | bool | Writes data to the connected pipe |
| Dispose() | void | Disconnects and disposes the pipe client |

### PipeHelper
Static helper methods for Windows named pipe operations.

| Method | Returns | Description |
|--------|---------|-------------|
| Transact(string pipeName, byte[] request, int timeoutMs) | byte[]? | Performs a one-shot named pipe transaction (send request, receive response) |
| PipeExists(string pipeName) | bool | Checks whether a named pipe of the given name currently exists |

## Usage

```csharp
using BPlusLib.Foundation.IPC;

// Server: create, wait for client, read/write
using var server = new PipeServer("MyAppPipe");
if (server.WaitForConnection(5000))
{
    byte[]? data = server.Read();
    server.Write(new byte[] { 0x01, 0x02 });
    server.Disconnect(); // Ready for next client
}

// Client: connect and communicate
using var client = new PipeClient("MyAppPipe");
if (client.Connect(5000))
{
    client.Write(new byte[] { 0x01, 0x02 });
    byte[]? response = client.Read();
}

// One-shot transaction
byte[]? result = PipeHelper.Transact("MyPipe", requestBytes, timeoutMs: 5000);

// Check if a pipe exists
bool exists = PipeHelper.PipeExists("MyPipe");
```

## Dependencies
- `BPlusLib.Foundation.Native` (for `Kernel32` P/Invoke: `CreateNamedPipeW`, `ConnectNamedPipe`, `ReadFile`, `WriteFile`, `CallNamedPipeW`, `WaitNamedPipeW`, etc.)
- Windows-only (named pipe APIs)
