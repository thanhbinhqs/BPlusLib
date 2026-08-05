using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// TCP client endpoint — bridges serial data to a TCP connection.
    /// </summary>
    public sealed class TcpClientEndpoint : ISerialEndpoint
    {
        private readonly string _remoteHost;
        private readonly int _remotePort;
        private readonly TimeSpan _reconnectDelay;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private Channel<SerialFrame>? _channel;
        private Task? _readTask;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private long _sequenceNumber;
        private ModemSignals _modemSignals;

        public TcpClientEndpoint(string name, string remoteHost, int remotePort,
            TimeSpan? reconnectDelay = null)
        {
            Name = name ?? $"{remoteHost}:{remotePort}";
            Id = Guid.NewGuid();
            _remoteHost = remoteHost ?? throw new ArgumentNullException(nameof(remoteHost));
            _remotePort = remotePort;
            _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(5);
            Settings = SerialSettings.Default;
            _modemSignals = new ModemSignals();
        }

        public Guid Id { get; }
        public EndpointType Type => EndpointType.TcpClient;
        public string Name { get; }
        public bool IsRunning => _isRunning;
        public SerialSettings Settings { get; set; }
        public ModemSignals ModemSignals => _modemSignals;

        public event EventHandler<ModemSignalChangedEventArgs>? ModemSignalsChanged;

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) return ValueTask.CompletedTask;

            _cts = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<SerialFrame>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _isRunning = true;
            _readTask = Task.Run(() => ConnectAndReadLoopAsync(_cts.Token), _cts.Token);

            return ValueTask.CompletedTask;
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();

            if (_readTask != null)
            {
                try { await _readTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }

            _stream?.Dispose();
            _client?.Dispose();
            _stream = null;
            _client = null;
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected.");

            return _stream.WriteAsync(data, cancellationToken);
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

        private async Task ConnectAndReadLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];

            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_remoteHost, _remotePort, ct).ConfigureAwait(false);
                    _stream = _client.GetStream();

                    while (!ct.IsCancellationRequested && _isRunning && _client.Connected)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                        if (bytesRead == 0) break; // Connection closed

                        var data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);

                        var frame = new SerialFrame
                        {
                            Timestamp = DateTime.UtcNow,
                            Source = Name,
                            Direction = FrameDirection.Receive,
                            SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                            Data = data,
                            ModemSignals = _modemSignals
                        };

                        await _channel!.Writer.WriteAsync(frame, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* Connection error — reconnect */ }

                if (_isRunning && !ct.IsCancellationRequested)
                {
                    try { await Task.Delay(_reconnectDelay, ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }

            _channel?.Writer.Complete();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
        }
    }
}
