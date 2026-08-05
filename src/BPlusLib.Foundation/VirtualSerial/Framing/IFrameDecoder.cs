using System;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// Decodes a byte stream into frames. Feed raw bytes, then call TryGetFrame
    /// to extract complete frames.
    /// </summary>
    public interface IFrameDecoder
    {
        /// <summary>Feed raw bytes into the decoder.</summary>
        void Feed(ReadOnlySpan<byte> data);

        /// <summary>
        /// Try to extract a complete frame.
        /// Returns true if a frame was decoded; the frame data is in <paramref name="frame"/>.
        /// </summary>
        bool TryGetFrame(out ReadOnlyMemory<byte> frame);

        /// <summary>Reset the decoder state (clear internal buffer).</summary>
        void Reset();

        /// <summary>Number of bytes currently buffered (not yet decoded into a frame).</summary>
        int BufferedBytes { get; }
    }
}
