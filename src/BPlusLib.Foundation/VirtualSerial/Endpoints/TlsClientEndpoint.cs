using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// TLS client endpoint — bridges serial data over encrypted TCP connection.
    /// </summary>
    public sealed class TlsClientEndpoint : ISerialEndpoint
    {
        private readonly string _remoteHost;
        private readonly int _remotePort;
        private readonly TimeSpan _reconnectDelay;
        private TcpClient? _tcpClient;
        private SslStream? _sslStream;
        private Channel<SerialFrame>? _channel;
        private Task? _readTask;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private long _sequenceNumber;
        private ModemSignals _modemSignals;

        public TlsClientEndpoint(string name, string remoteHost, int remotePort,
            TimeSpan? reconnectDelay = null)
        {
            Name = name ?? $"TLS:{remoteHost}:{remotePort}";
            Id = Guid.NewGuid();
            _remoteHost = remoteHost ?? throw new ArgumentNullException(nameof(remoteHost));
            _remotePort = remotePort;
            _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(5);
            Settings = SerialSettings.Default;
            _modemSignals = new ModemSignals();
        }

        public Guid Id { get; }
        public EndpointType Type => EndpointType.TlsClient;
        public string Name { get; }
        public bool IsRunning => _isRunning;
        public SerialSettings Settings { get; set; }
        public ModemSignals ModemSignals => _modemSignals;

        /// <summary>Optional certificate validation callback.</summary>
        public RemoteCertificateValidationCallback? CertificateValidation { get; set; }

        /// <summary>Optional client certificate for mutual TLS.</summary>
        public X509Certificate2? ClientCertificate { get; set; }

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

            _isRunning = true;
            _readTask = Task.Run(() => ConnectAndReadLoopAsync(_cts.Token), _cts.Token);

            return default;
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

            _sslStream?.Dispose();
            _tcpClient?.Dispose();
            _sslStream = null;
            _tcpClient = null;
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (_sslStream == null)
                throw new InvalidOperationException("Not connected.");

            return _sslStream.WriteAsync(data, cancellationToken);
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

        private async Task ConnectAndReadLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];

            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    await _tcpClient.ConnectAsync(_remoteHost, _remotePort, ct).ConfigureAwait(false);

                    _sslStream = new SslStream(
                        _tcpClient.GetStream(),
                        false,
                        CertificateValidation);

                    var sslOptions = new SslClientAuthenticationOptions
                    {
                        TargetHost = _remoteHost,
                        ClientCertificates = ClientCertificate != null
                            ? new X509CertificateCollection { ClientCertificate }
                            : null
                    };

                    await _sslStream.AuthenticateAsClientAsync(sslOptions, ct).ConfigureAwait(false);

                    while (!ct.IsCancellationRequested && _isRunning && _tcpClient.Connected)
                    {
                        int bytesRead = await _sslStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                        if (bytesRead == 0) break;

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
                catch { /* connection error — reconnect */ }

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
