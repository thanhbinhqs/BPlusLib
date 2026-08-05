using System;
using System.Buffers;
using System.Diagnostics;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// Splits byte stream on idle timeout. If no bytes arrive within the timeout,
    /// the buffered data is flushed as a frame.
    /// </summary>
    public sealed class IdleTimeoutFramer : IFrameDecoder
    {
        private readonly int _idleTimeoutMs;
        private readonly int _maxFrameLength;
        private byte[] _buffer;
        private int _bufferLength;
        private long _lastFeedTicks;

        /// <param name="idleTimeoutMs">Idle timeout in milliseconds before flushing.</param>
        /// <param name="maxFrameLength">Maximum frame length. Default: 65536.</param>
        public IdleTimeoutFramer(int idleTimeoutMs = 10, int maxFrameLength = 65536)
        {
            _idleTimeoutMs = idleTimeoutMs;
            _maxFrameLength = maxFrameLength;
            _buffer = ArrayPool<byte>.Shared.Rent(4096);
            _lastFeedTicks = Stopwatch.GetTimestamp();
        }

        public int BufferedBytes => _bufferLength;

        /// <summary>Whether the idle timeout has elapsed since last Feed.</summary>
        public bool IsIdle => _bufferLength > 0 &&
            (Stopwatch.GetTimestamp() - _lastFeedTicks) > (long)_idleTimeoutMs * Stopwatch.Frequency / 1000;

        public void Feed(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            int newLength = _bufferLength + data.Length;
            if (newLength > _maxFrameLength)
                throw new InvalidOperationException($"Frame exceeds maximum length {_maxFrameLength}.");

            if (_buffer.Length < newLength)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(newLength);
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _bufferLength);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
            data.CopyTo(_buffer.AsSpan(_bufferLength));
            _bufferLength = newLength;
            _lastFeedTicks = Stopwatch.GetTimestamp();
        }

        public bool TryGetFrame(out ReadOnlyMemory<byte> frame)
        {
            if (_bufferLength == 0 || !IsIdle)
            {
                frame = default;
                return false;
            }

            frame = new ReadOnlyMemory<byte>(_buffer, 0, _bufferLength);
            _bufferLength = 0;
            return true;
        }

        public void Reset()
        {
            _bufferLength = 0;
            _lastFeedTicks = Stopwatch.GetTimestamp();
        }
    }
}
