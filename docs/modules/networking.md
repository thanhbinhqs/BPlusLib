# Networking

TCP and UDP socket wrappers, HTTP/FTP client helpers, and embedded HTTP listener. Provides thread-safe, full-duplex TCP/UDP communication with sync/async APIs. All methods handle errors gracefully by returning null or false on failure.

## Classes

### TcpConnection
Thread-safe, full-duplex wrapper around `TcpClient` that provides synchronous and asynchronous send/receive operations. Send and receive paths are independent and can execute concurrently.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Connected | bool | Whether the underlying TcpClient is connected |
| Available | int | Number of bytes available on the socket |
| LocalEndPoint | EndPoint? | The local endpoint |
| RemoteEndPoint | EndPoint? | The remote endpoint |
| Send(byte[] data, int offset, int? count) | bool | Sends data synchronously |
| Receive(int bufferSize, int timeoutMs) | byte[]? | Receives data synchronously |
| ReceiveString(int bufferSize, int timeoutMs, Encoding? encoding) | string? | Receives a string synchronously |
| SendAsync(byte[] data, int offset, int? count) | Task\<bool\> | Sends data asynchronously |
| ReceiveAsync(int bufferSize, int timeoutMs) | Task\<byte[]?\> | Receives data asynchronously |
| ReceiveStringAsync(int bufferSize, int timeoutMs, Encoding? encoding) | Task\<string?\> | Receives a string asynchronously |
| Close() | void | Closes the underlying NetworkStream and TcpClient |
| Dispose() | void | Releases all resources |

### TcpServer
Thread-safe TCP listener wrapper that accepts incoming client connections with optional timeout support. Only one accept operation is active at a time.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| TcpServer(int port, IPAddress? address) | TcpServer | Initializes and starts listening on the specified port |
| TcpServer(IPEndPoint localEndpoint) | TcpServer | Initializes from a specific local endpoint |
| Port | int | The actual port the server is listening on |
| IsRunning | bool | Whether the server is currently accepting connections |
| Accept(int timeoutMs) | TcpConnection? | Accepts a pending TCP connection synchronously with timeout |
| AcceptAsync() | Task\<TcpConnection?\> | Accepts a pending TCP connection asynchronously |
| Stop() | void | Stops the server from accepting new connections |
| Dispose() | void | Releases resources and stops listening |

### TcpSocketHelper
Static helper methods for creating TCP client connections and TCP server listeners.

| Method | Returns | Description |
|--------|---------|-------------|
| Connect(string host, int port, int timeoutMs) | TcpConnection? | Creates a new TCP connection with configurable timeout |
| ConnectAsync(string host, int port, int timeoutMs) | Task\<TcpConnection?\> | Creates a new TCP connection asynchronously with timeout |
| StartServer(int port, IPAddress? address) | TcpServer? | Creates and starts a TCP server |
| StartServerAsync(int port, IPAddress? address) | Task\<TcpServer?\> | Creates and starts a TCP server asynchronously |

### UdpEndpoint
Thread-safe, stateful wrapper around `UdpClient` providing synchronous and asynchronous send, receive, broadcast, and multicast operations. Send and receive paths are independent.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| UdpEndpoint(int? localPort) | UdpEndpoint | Creates a UdpClient bound to an optional local port |
| UdpEndpoint(int localPort, IPAddress? localAddress) | UdpEndpoint | Creates a UdpClient bound to a specific local port and address |
| Port | int | The actual port the endpoint is bound to |
| EnableBroadcast | bool | Whether broadcast packets can be sent |
| SendTimeout | int | Send timeout in milliseconds |
| ReceiveTimeout | int | Receive timeout in milliseconds |
| Send(byte[] data, string host, int port) | bool | Sends a UDP datagram synchronously |
| Send(byte[] data, EndPoint remoteEndpoint) | bool | Sends a UDP datagram to an endpoint |
| Send(byte[] data, int offset, int count, string host, int port) | bool | Sends a segment of a buffer |
| Receive(int timeoutMs) | (byte[]?, IPEndPoint?)? | Receives a UDP datagram with timeout |
| SendAsync(byte[] data, string host, int port) | Task\<bool\> | Sends a UDP datagram asynchronously |
| SendAsync(byte[] data, EndPoint remoteEndpoint) | Task\<bool\> | Sends a UDP datagram to an endpoint asynchronously |
| ReceiveAsync(int timeoutMs) | Task\<(byte[]?, IPEndPoint?)?\> | Receives a UDP datagram asynchronously |
| JoinMulticastGroup(IPAddress multicastAddress) | bool | Joins a multicast group |
| DropMulticastGroup(IPAddress multicastAddress) | bool | Drops multicast group membership |
| Close() | void | Closes the underlying UdpClient |
| Dispose() | void | Releases all resources |

### UdpSocketHelper
Static helper methods for creating UDP endpoints and performing one-shot send, receive, and broadcast operations.

| Method | Returns | Description |
|--------|---------|-------------|
| CreateEndpoint(int? localPort) | UdpEndpoint? | Creates a new UdpEndpoint bound to an optional local port |
| SendDatagram(byte[] data, string host, int port, int? localPort, int timeoutMs) | bool | Sends a one-shot UDP datagram using an ephemeral endpoint |
| ReceiveDatagram(int port, int timeoutMs) | byte[]? | Receives a single UDP datagram on the specified port |
| Broadcast(byte[] data, int port, int? localPort) | bool | Sends a UDP broadcast datagram to all interfaces |

### NetClientHelper
HTTP and FTP networking helpers with synchronous and asynchronous APIs. Uses HttpClient on .NET 6+ and HttpWebRequest/WebClient on .NET Framework.

| Method | Returns | Description |
|--------|---------|-------------|
| HttpGet(string url, int timeoutMs, Dictionary\<string, string\>? headers) | string? | Performs a synchronous HTTP GET request |
| HttpPost(string url, string body, string contentType, int timeoutMs, Dictionary\<string, string\>? headers) | string? | Performs a synchronous HTTP POST request |
| HttpDownload(string url, int timeoutMs) | byte[]? | Downloads binary data from a URL |
| TryDownloadFile(string url, string outputPath, int timeoutMs) | bool | Downloads a file from a URL to a local path |
| HttpGetAsync(string url, int timeoutMs) | Task\<string?\> | Performs an asynchronous HTTP GET request |
| HttpPostAsync(string url, string body, string contentType, int timeoutMs) | Task\<string?\> | Performs an asynchronous HTTP POST request |
| HttpDownloadAsync(string url, int timeoutMs) | Task\<byte[]?\> | Downloads binary data asynchronously |
| TryDownloadFileAsync(string url, string outputPath, int timeoutMs) | Task\<bool\> | Downloads a file to a local path asynchronously |
| FtpListDirectory(string url, string? username, string? password, int timeoutMs) | string[]? | Lists the contents of an FTP directory |
| FtpDownloadFile(string url, string outputPath, string? username, string? password) | bool | Downloads a file from an FTP server |
| FtpUploadFile(string url, string localPath, string? username, string? password) | bool | Uploads a file to an FTP server |
| FtpCreateDirectory(string url, string? username, string? password) | bool | Creates a directory on an FTP server |
| FtpDeleteFile(string url, string? username, string? password) | bool | Deletes a file from an FTP server |
| IsNetworkAvailable() | static bool | Checks whether any network interface is available |
| IsInternetAvailable(int timeoutMs) | static bool | Checks internet connectivity by pinging 8.8.8.8 |
| GetPublicIpAddress(int timeoutMs) | static string? | Retrieves the public IP address via external services |
| GetHttpResponseStatusCode(string url, int timeoutMs) | static int? | Gets the HTTP response status code via HEAD request |

### HttpListenerHelper
Basic embedded HTTP server helper using `HttpListener`. Provides simple wrappers for common HTTP server operations.

| Method | Returns | Description |
|--------|---------|-------------|
| Start(string prefix, string? user) | HttpListener? | Starts an HttpListener on the specified prefix |
| Stop(HttpListener listener) | bool | Stops an HttpListener |
| GetRequest(HttpListener listener, int timeoutMs) | HttpListenerContext? | Waits for an incoming HTTP request with timeout |
| SendText(HttpListenerResponse response, string text, string contentType) | void | Sends a plain text response |
| SendJson(HttpListenerResponse response, string json) | void | Sends a JSON response |
| SendBinary(HttpListenerResponse response, byte[] data, string contentType) | void | Sends a binary response |
| GetFreePort() | static int | Finds a free TCP port on localhost |

## Usage

```csharp
using BPlusLib.Foundation.Networking;

// TCP Server
using var server = TcpSocketHelper.StartServer(8080);
var client = server.Accept(timeoutMs: 5000);
client?.Send(new byte[] { 0x01, 0x02 });
byte[]? data = client?.Receive();

// TCP Client
using var connection = TcpSocketHelper.Connect("192.168.1.100", 8080);
connection?.Send(new byte[] { 0x01 });
string? response = connection?.ReceiveString();

// UDP
using var udp = new UdpEndpoint(9000);
udp.Send(new byte[] { 0x01 }, "192.168.1.100", 9001);
var result = udp.Receive(timeoutMs: 3000);

// HTTP
string? html = NetClientHelper.HttpGet("https://example.com");
string? json = NetClientHelper.HttpPost("https://api.example.com/data", "{\"key\":\"value\"}");
bool downloaded = NetClientHelper.TryDownloadFile("https://example.com/file.zip", "/tmp/file.zip");

// FTP
string[]? files = NetClientHelper.FtpListDirectory("ftp://server.com/path/", "user", "pass");
NetClientHelper.FtpUploadFile("ftp://server.com/path/file.zip", "/local/file.zip", "user", "pass");

// Embedded HTTP server
var listener = HttpListenerHelper.Start("http://localhost:8080/");
var ctx = HttpListenerHelper.GetRequest(listener);
HttpListenerHelper.SendJson(ctx.Response, "{\"status\":\"ok\"}");
HttpListenerHelper.Stop(listener);
```

## Dependencies
- `System.Net.Sockets` (built-in)
- `System.Net.Http` (built-in, or `System.Net.Http` NuGet for net472)
- `System.Net` / `FtpWebRequest` (built-in)
- No external NuGet packages required
- Cross-platform for TCP/UDP/HTTP; FTP and HttpListener have limited cross-platform support
