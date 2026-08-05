using System;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Immutable data frame representing a chunk of serial data with metadata.
    /// </summary>
    public readonly record struct SerialFrame
    {
        /// <summary>Timestamp when the frame was received or sent.</summary>
        public DateTime Timestamp { get; init; }

        /// <summary>Source endpoint name or identifier.</summary>
        public required string Source { get; init; }

        /// <summary>Route identifier if applicable.</summary>
        public string? RouteId { get; init; }

        /// <summary>Session identifier for multi-open scenarios.</summary>
        public long SessionId { get; init; }

        /// <summary>Data direction.</summary>
        public FrameDirection Direction { get; init; }

        /// <summary>The frame payload.</summary>
        public ReadOnlyMemory<byte> Data { get; init; }

        /// <summary>Frame sequence number within the route.</summary>
        public long SequenceNumber { get; init; }

        /// <summary>Whether this frame represents a break signal.</summary>
        public bool IsBreak { get; init; }

        /// <summary>Modem signals at the time of this frame.</summary>
        public ModemSignals ModemSignals { get; init; }

        /// <summary>Error flags associated with this frame.</summary>
        public SerialError Errors { get; init; }

        /// <summary>Number of bytes in the frame.</summary>
        public int Length => Data.Length;
    }

    /// <summary>
    /// Direction of data flow.
    /// </summary>
    public enum FrameDirection
    {
        /// <summary>Data received from device/network.</summary>
        Receive,

        /// <summary>Data sent to device/network.</summary>
        Transmit
    }

    /// <summary>
    /// Serial error flags.
    /// </summary>
    [Flags]
    public enum SerialError
    {
        None = 0,
        Overrun = 1,
        Parity = 2,
        Framing = 4,
        Break = 8,
        BufferOverrun = 16
    }
}
