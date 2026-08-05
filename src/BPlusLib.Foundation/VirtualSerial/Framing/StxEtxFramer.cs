using System;
using System.Buffers;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// STX/ETX framing: frames start with STX (0x02) and end with ETX (0x03).
    /// Data between STX and ETX is the frame payload.
    /// </summary>
    public sealed class StxEtxFramer : IFrameDecoder
    {
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private byte[] _buffer;
        private int _bufferLength;
        private int _maxFrameLength;
        private bool _inFrame;

        /// <param name="maxFrameLength">Maximum frame length. Default: 65536.</param>
        public StxEtxFramer(int maxFrameLength = 65536)
        {
            _maxFrameLength = maxFrameLength;
            _buffer = ArrayPool<byte>.Shared.Rent(4096);
        }

        public int BufferedBytes => _bufferLength;

        public void Feed(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            int newLength = _bufferLength + data.Length;
            if (newLength > _maxFrameLength)
                throw new InvalidOperationException($"STX/ETX frame exceeds maximum length {_maxFrameLength}.");

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
            // Find STX
            int stxIndex = -1;
            for (int i = 0; i < _bufferLength; i++)
            {
                if (_buffer[i] == STX)
                {
                    stxIndex = i;
                    break;
                }
            }

            if (stxIndex < 0)
            {
                // No STX found — discard all (data before frame start is noise)
                _bufferLength = 0;
                frame = default;
                return false;
            }

            // Find ETX after STX
            for (int i = stxIndex + 1; i < _bufferLength; i++)
            {
                if (_buffer[i] == ETX)
                {
                    // Found complete frame: STX + data + ETX
                    int frameStart = stxIndex;
                    int frameEnd = i + 1; // exclusive
                    int frameLength = frameEnd - frameStart;

                    frame = new ReadOnlyMemory<byte>(_buffer, frameStart, frameLength);

                    // Shift remaining data
                    int remaining = _bufferLength - frameEnd;
                    if (remaining > 0)
                        System.Buffer.BlockCopy(_buffer, frameEnd, _buffer, 0, remaining);
                    _bufferLength = remaining;

                    return true;
                }
            }

            // STX found but no ETX yet — keep buffering
            frame = default;
            return false;
        }

        public void Reset()
        {
            _bufferLength = 0;
            _inFrame = false;
        }
    }
}
