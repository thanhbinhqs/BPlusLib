using System;
using System.Buffers;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// Splits byte stream into fixed-length frames.
    /// </summary>
    public sealed class FixedLengthFramer : IFrameDecoder
    {
        private readonly int _frameLength;
        private byte[] _buffer;
        private int _bufferLength;

        public FixedLengthFramer(int frameLength)
        {
            if (frameLength <= 0) throw new ArgumentOutOfRangeException(nameof(frameLength));
            _frameLength = frameLength;
            _buffer = ArrayPool<byte>.Shared.Rent(frameLength * 2);
        }

        public int BufferedBytes => _bufferLength;

        public void Feed(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            int newLength = _bufferLength + data.Length;
            if (_buffer.Length < newLength)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(newLength);
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _bufferLength);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
            data.CopyTo(_buffer.AsSpan(_bufferLength));
            _bufferLength = newLength;
        }

        public bool TryGetFrame(out ReadOnlyMemory<byte> frame)
        {
            if (_bufferLength < _frameLength)
            {
                frame = default;
                return false;
            }

            frame = new ReadOnlyMemory<byte>(_buffer, 0, _frameLength);

            int remaining = _bufferLength - _frameLength;
            if (remaining > 0)
                System.Buffer.BlockCopy(_buffer, _frameLength, _buffer, 0, remaining);
            _bufferLength = remaining;

            return true;
        }

        public void Reset()
        {
            _bufferLength = 0;
        }
    }
}
