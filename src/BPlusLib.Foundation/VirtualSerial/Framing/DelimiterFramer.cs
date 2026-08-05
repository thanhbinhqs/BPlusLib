using System;
using System.Buffers;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// Splits byte stream on delimiter bytes (e.g., 0x0D 0x0A for CR/LF).
    /// </summary>
    public sealed class DelimiterFramer : IFrameDecoder
    {
        private readonly byte[] _delimiter;
        private byte[] _buffer;
        private int _bufferLength;
        private int _maxFrameLength;

        /// <param name="delimiter">Delimiter bytes to split on (e.g., new byte[] { 0x0D, 0x0A }).</param>
        /// <param name="maxFrameLength">Maximum frame length before error. Default: 65536.</param>
        public DelimiterFramer(byte[] delimiter, int maxFrameLength = 65536)
        {
            _delimiter = delimiter ?? throw new ArgumentNullException(nameof(delimiter));
            _maxFrameLength = maxFrameLength;
            _buffer = ArrayPool<byte>.Shared.Rent(4096);
        }

        public int BufferedBytes => _bufferLength;

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
        }

        public bool TryGetFrame(out ReadOnlyMemory<byte> frame)
        {
            if (_bufferLength < _delimiter.Length)
            {
                frame = default;
                return false;
            }

            // Search for delimiter
            for (int i = 0; i <= _bufferLength - _delimiter.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < _delimiter.Length; j++)
                {
                    if (_buffer[i + j] != _delimiter[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    int frameLength = i + _delimiter.Length;
                    frame = new ReadOnlyMemory<byte>(_buffer, 0, frameLength);

                    // Shift remaining data
                    int remaining = _bufferLength - frameLength;
                    if (remaining > 0)
                        System.Buffer.BlockCopy(_buffer, frameLength, _buffer, 0, remaining);
                    _bufferLength = remaining;

                    return true;
                }
            }

            frame = default;
            return false;
        }

        public void Reset()
        {
            _bufferLength = 0;
        }
    }
}
