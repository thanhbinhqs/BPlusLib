using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Core interface for all serial endpoints. Represents a source or destination
    /// of serial data — physical port, virtual port, TCP socket, etc.
    /// </summary>
    public interface ISerialEndpoint : IAsyncDisposable
    {
        /// <summary>Unique identifier for this endpoint instance.</summary>
        Guid Id { get; }

        /// <summary>The type of endpoint.</summary>
        EndpointType Type { get; }

        /// <summary>Human-readable name (e.g., "COM20", "TCP:192.168.1.100:5000").</summary>
        string Name { get; }

        /// <summary>Whether the endpoint is currently running and able to send/receive.</summary>
        bool IsRunning { get; }

        /// <summary>Serial port settings (baud rate, parity, etc.).</summary>
        SerialSettings Settings { get; set; }

        /// <summary>Current modem signal state.</summary>
        ModemSignals ModemSignals { get; }

        /// <summary>Event raised when modem signals change.</summary>
        event EventHandler<ModemSignalChangedEventArgs>? ModemSignalsChanged;

        /// <summary>Starts the endpoint, opening underlying resources.</summary>
        ValueTask StartAsync(CancellationToken cancellationToken = default);

        /// <summary>Stops the endpoint, closing underlying resources.</summary>
        ValueTask StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Sends data through the endpoint.</summary>
        ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

        /// <summary>Reads all incoming data as an async stream of frames.</summary>
        IAsyncEnumerable<SerialFrame> ReadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Purges pending read/write data.</summary>
        ValueTask PurgeAsync(PurgeFlags flags = PurgeFlags.All, CancellationToken cancellationToken = default);

        /// <summary>Sets the modem control signals (DTR, RTS).</summary>
        ValueTask SetModemControlAsync(bool? dtr = null, bool? rts = null, CancellationToken cancellationToken = default);

        /// <summary>Sends a break signal.</summary>
        ValueTask SetBreakAsync(bool on, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Flags for PurgeComm operations.
    /// </summary>
    [Flags]
    public enum PurgeFlags
    {
        None = 0,
        TxAbort = 1,
        RxAbort = 2,
        TxClear = 4,
        RxClear = 8,
        All = TxAbort | RxAbort | TxClear | RxClear
    }
}
