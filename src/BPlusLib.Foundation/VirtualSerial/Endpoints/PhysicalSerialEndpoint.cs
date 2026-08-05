using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Serial endpoint wrapping System.IO.Ports.SerialPort for physical COM ports.
    /// Supports cross-thread reading via Channel.
    /// </summary>
    public sealed class PhysicalSerialEndpoint : ISerialEndpoint
    {
        private readonly string _portName;
        private SerialPort? _port;
        private Channel<SerialFrame>? _channel;
        private Task? _readTask;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private long _sequenceNumber;
        private ModemSignals _modemSignals;

        public PhysicalSerialEndpoint(string portName, SerialSettings? settings = null)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            Id = Guid.NewGuid();
            Name = portName;
            Settings = settings ?? SerialSettings.Default;
            _modemSignals = new ModemSignals();
        }

        public Guid Id { get; }
        public EndpointType Type => EndpointType.PhysicalSerial;
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

            _port = new SerialPort(_portName)
            {
                BaudRate = Settings.BaudRate,
                DataBits = Settings.DataBits,
                Parity = Settings.Parity switch
                {
                    ParityMode.Odd => Parity.Odd,
                    ParityMode.Even => Parity.Even,
                    ParityMode.Mark => Parity.Mark,
                    ParityMode.Space => Parity.Space,
                    _ => Parity.None
                },
                StopBits = Settings.StopBits switch
                {
                    StopBitsMode.OnePointFive => StopBits.OnePointFive,
                    StopBitsMode.Two => StopBits.Two,
                    _ => Parity.None == Parity.None ? System.IO.Ports.StopBits.One : System.IO.Ports.StopBits.One
                },
                Handshake = Settings.Handshake switch
                {
                    HandshakeMode.XOnXOff => Handshake.XOnXOff,
                    HandshakeMode.RequestToSend => Handshake.RequestToSend,
                    HandshakeMode.RequestToSendXOnXOff => Handshake.RequestToSendXOnXOff,
                    _ => Handshake.None
                },
                ReadTimeout = Settings.ReadTimeoutMs < 0 ? Timeout.Infinite : Settings.ReadTimeoutMs,
                WriteTimeout = Settings.WriteTimeoutMs < 0 ? Timeout.Infinite : Settings.WriteTimeoutMs,
                ReadBufferSize = Settings.RxBufferSize,
                WriteBufferSize = Settings.TxBufferSize,
                DtrEnable = Settings.DtrEnable,
                RtsEnable = Settings.RtsEnable
            };

            _port.Open();
            _isRunning = true;

            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token), _cts.Token);

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

            try { _port?.Close(); }
            catch { /* ignore close errors */ }

            _port?.Dispose();
            _port = null;
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (!_isRunning || _port == null)
                throw new InvalidOperationException("Endpoint is not running.");
            byte[] bytes = data.ToArray();
            _port.Write(bytes, 0, bytes.Length);
            return default;
        }

        public IAsyncEnumerable<SerialFrame> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            if (_channel == null)
                throw new InvalidOperationException("Endpoint is not started.");

            return _channel.Reader.ReadAllAsync(cancellationToken);
        }

        public ValueTask PurgeAsync(PurgeFlags flags = PurgeFlags.All, CancellationToken cancellationToken = default)
        {
            if (_port?.IsOpen != true) return default;

            if (flags.HasFlag(PurgeFlags.RxClear)) _port.DiscardInBuffer();
            if (flags.HasFlag(PurgeFlags.TxClear)) _port.DiscardOutBuffer();

            return default;
        }

        public ValueTask SetModemControlAsync(bool? dtr = null, bool? rts = null, CancellationToken cancellationToken = default)
        {
            if (_port?.IsOpen != true) return default;

            if (dtr.HasValue) _port.DtrEnable = dtr.Value;
            if (rts.HasValue) _port.RtsEnable = rts.Value;

            var previous = _modemSignals;
            _modemSignals = _modemSignals with { Dtr = _port.DtrEnable, Rts = _port.RtsEnable };
            _modemSignals.NotifyChanged(previous);
            ModemSignalsChanged?.Invoke(this, new ModemSignalChangedEventArgs(previous, _modemSignals));

            return default;
        }

        public ValueTask SetBreakAsync(bool on, CancellationToken cancellationToken = default)
        {
            if (_port?.IsOpen != true) return default;

            if (on) _port.BreakState = true;
            else _port.BreakState = false;

            return default;
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];

            try
            {
                while (!ct.IsCancellationRequested && _isRunning && _port?.IsOpen == true)
                {
                    try
                    {
                        int bytesRead = _port.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            var frame = new SerialFrame
                            {
                                Timestamp = DateTime.UtcNow,
                                Source = Name,
                                Direction = FrameDirection.Receive,
                                SequenceNumber = Interlocked.Increment(ref _sequenceNumber),
                                Data = new byte[bytesRead],
                                ModemSignals = _modemSignals
                            };
                            Array.Copy(buffer, frame.Data.ToArray(), bytesRead);
                            frame = frame with { Data = frame.Data.ToArray() };

                            await _channel!.Writer.WriteAsync(frame, ct).ConfigureAwait(false);
                        }
                    }
                    catch (TimeoutException)
                    {
                        // Read timeout — continue loop
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                _isRunning = false;
            }
            finally
            {
                _channel?.Writer.Complete();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
        }
    }
}
