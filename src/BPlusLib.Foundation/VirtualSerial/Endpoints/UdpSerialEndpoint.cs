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
    /// UDP endpoint — bridges serial data to UDP datagrams.
    /// </summary>
    public sealed class UdpSerialEndpoint : ISerialEndpoint
    {
        private readonly string _remoteHost;
        private readonly int _remotePort;
        private readonly int _localPort;
        private UdpClient? _udpClient;
        private Channel<SerialFrame>? _channel;
        private Task? _receiveTask;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private long _sequenceNumber;
        private ModemSignals _modemSignals;

        public UdpSerialEndpoint(string name, string remoteHost, int remotePort,
            int localPort = 0)
        {
            Name = name ?? $"UDP:{remoteHost}:{remotePort}";
            Id = Guid.NewGuid();
            _remoteHost = remoteHost ?? throw new ArgumentNullException(nameof(remoteHost));
            _remotePort = remotePort;
            _localPort = localPort;
            Settings = SerialSettings.Default;
            _modemSignals = new ModemSignals();
        }

        public Guid Id { get; }
        public EndpointType Type => EndpointType.Udp;
        public string Name { get; }
        public bool IsRunning => _isRunning;
        public SerialSettings Settings { get; set; }
        public ModemSignals ModemSignals => _modemSignals;

        public event EventHandler<ModemSignalChangedEventArgs>? ModemSignalsChanged;

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) return default;

            _cts = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<SerialFrame>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _udpClient = new UdpClient(_localPort);
            _isRunning = true;

            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);

            return default;
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();

            if (_receiveTask != null)
            {
                try { await _receiveTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }

            _udpClient?.Dispose();
            _udpClient = null;
            _channel?.Writer.Complete();
        }

        public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (_udpClient == null)
                throw new InvalidOperationException("Endpoint is not running.");

            var endpoint = new IPEndPoint(IPAddress.Parse(_remoteHost), _remotePort);
            await _udpClient.SendAsync(data, endpoint).ConfigureAwait(false);
        }

        public IAsyncEnumerable<SerialFrame> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            if (_channel == null)
                throw new InvalidOperationException("Endpoint is not started.");

            return _channel.Reader.ReadAllAsync(cancellationToken);
        }

        public ValueTask PurgeAsync(PurgeFlags flags = PurgeFlags.All, CancellationToken cancellationToken = default)
        {
            return default;
        }

        public ValueTask SetModemControlAsync(bool? dtr = null, bool? rts = null, CancellationToken cancellationToken = default)
        {
            return default;
        }

        public ValueTask SetBreakAsync(bool on, CancellationToken cancellationToken = default)
        {
            return default;
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync(ct).ConfigureAwait(false);

                    var frame = new SerialFrame
                    {
                        Timestamp = DateTime.UtcNow,
                        Source = Name,
                        Direction = FrameDirection.Receive,
                        SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                        Data = result.Buffer,
                        ModemSignals = _modemSignals
                    };

                    await _channel!.Writer.WriteAsync(frame, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch { /* receive error — continue */ }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
        }
    }
}
