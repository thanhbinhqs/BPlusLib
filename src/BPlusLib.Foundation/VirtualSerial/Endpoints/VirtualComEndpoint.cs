using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Placeholder endpoint for virtual COM port via KMDF driver.
    /// Will be implemented when driver client is available.
    /// </summary>
    public sealed class VirtualComEndpoint : ISerialEndpoint
    {
        private readonly string _portName;
        private bool _isRunning;

        public VirtualComEndpoint(string portName)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            Id = Guid.NewGuid();
            Name = portName;
            Settings = SerialSettings.Default;
            ModemSignals = new ModemSignals();
        }

        public Guid Id { get; }
        public EndpointType Type => EndpointType.VirtualSerial;
        public string Name { get; }
        public bool IsRunning => _isRunning;
        public SerialSettings Settings { get; set; }
        public ModemSignals ModemSignals { get; private set; }

        public event EventHandler<ModemSignalChangedEventArgs>? ModemSignalsChanged;

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            _isRunning = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            _isRunning = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "VirtualComEndpoint requires KMDF driver integration. " +
                "Use PhysicalSerialEndpoint, TcpClientEndpoint, or TcpServerEndpoint instead.");
        }

        public IAsyncEnumerable<SerialFrame> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "VirtualComEndpoint requires KMDF driver integration.");
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

        public ValueTask DisposeAsync()
        {
            _isRunning = false;
            return ValueTask.CompletedTask;
        }
    }
}
