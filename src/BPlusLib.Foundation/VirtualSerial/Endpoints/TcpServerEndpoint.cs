using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// TCP server endpoint — accepts connections and bridges to serial.
    /// </summary>
    public sealed class TcpServerEndpoint : ISerialEndpoint
    {
        private readonly int _listenPort;
        private readonly IPAddress _listenAddress;
        private TcpListener? _listener;
        private Channel<SerialFrame>? _channel;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private long _sequenceNumber;
        private ModemSignals _modemSignals;
        private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new();

        public TcpServerEndpoint(string name, int listenPort,
            IPAddress? listenAddress = null)
        {
            Name = name ?? $"TCP-Server:{listenPort}";
            Id = Guid.NewGuid();
            _listenPort = listenPort;
            _listenAddress = listenAddress ?? IPAddress.Any;
            Settings = SerialSettings.Default;
            _modemSignals = new ModemSignals();
        }

        public Guid Id { get; }
        public EndpointType Type => EndpointType.TcpServer;
        public string Name { get; }
        public bool IsRunning => _isRunning;
        public SerialSettings Settings { get; set; }
        public ModemSignals ModemSignals => _modemSignals;
        public int ClientCount => _clients.Count;

        public event EventHandler<ModemSignalChangedEventArgs>? ModemSignalsChanged;
        public event EventHandler<Guid>? ClientConnected;
        public event EventHandler<Guid>? ClientDisconnected;

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) return ValueTask.CompletedTask;

            _cts = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<SerialFrame>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _listener = new TcpListener(_listenAddress, _listenPort);
            _listener.Start();
            _isRunning = true;

            _ = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);

            return ValueTask.CompletedTask;
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();

            _listener?.Stop();
            _listener = null;

            foreach (var kvp in _clients)
            {
                kvp.Value.Dispose();
                _clients.TryRemove(kvp.Key, out _);
            }

            _channel?.Writer.Complete();
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            // Send to all connected clients
            foreach (var kvp in _clients)
            {
                try
                {
                    var stream = kvp.Value.GetStream();
                    stream.Write(data.Span);
                }
                catch { /* client may have disconnected */ }
            }

            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<SerialFrame> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            if (_channel == null)
                throw new InvalidOperationException("Endpoint is not started.");

            return _channel.Reader.ReadAllAsync(cancellationToken);
        }

        public ValueTask PurgeAsync(PurgeFlags flags = PurgeFlags.All, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SetModemControlAsync(bool? dtr = null, bool? rts = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SetBreakAsync(bool on, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    var clientId = Guid.NewGuid();
                    _clients[clientId] = client;
                    ClientConnected?.Invoke(this, clientId);

                    _ = Task.Run(() => ClientReadLoopAsync(clientId, client, ct), ct);
                }
                catch (OperationCanceledException) { break; }
                catch { /* accept error — continue */ }
            }
        }

        private async Task ClientReadLoopAsync(Guid clientId, TcpClient client, CancellationToken ct)
        {
            var buffer = new byte[4096];

            try
            {
                using var stream = client.GetStream();
                while (!ct.IsCancellationRequested && _isRunning && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (bytesRead == 0) break;

                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);

                    var frame = new SerialFrame
                    {
                        Timestamp = DateTime.UtcNow,
                        Source = Name,
                        SessionId = clientId.GetHashCode(),
                        Direction = FrameDirection.Receive,
                        SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                        Data = data,
                        ModemSignals = _modemSignals
                    };

                    await _channel!.Writer.WriteAsync(frame, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch { /* read error */ }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client.Dispose();
                ClientDisconnected?.Invoke(this, clientId);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
        }
    }
}
