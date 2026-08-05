using System;
using System.Buffers;
using System.Diagnostics;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// Modbus RTU framing: 3.5 character silence interval separates frames.
    /// At 9600 baud, 1 char ≈ 1.04ms, so 3.5 chars ≈ 3.64ms.
    /// </summary>
    public sealed class ModbusRtuFramer : IFrameDecoder
    {
        private readonly int _silenceMs;
        private readonly int _maxFrameLength;
        private byte[] _buffer;
        private int _bufferLength;
        private long _lastByteTicks;

        /// <param name="baudRate">Baud rate to calculate silence interval.</param>
        /// <param name="maxFrameLength">Maximum frame length. Default: 256 (Modbus RTU max).</param>
        public ModbusRtuFramer(int baudRate = 9600, int maxFrameLength = 256)
        {
            // 3.5 characters = 3.5 * (11 bits / baud rate) seconds
            double charTimeSec = 11.0 / baudRate;
            _silenceMs = (int)Math.Ceiling(charTimeSec * 3.5 * 1000);
            if (_silenceMs < 2) _silenceMs = 2; // Minimum 2ms

            _maxFrameLength = maxFrameLength;
            _buffer = ArrayPool<byte>.Shared.Rent(maxFrameLength);
            _lastByteTicks = Stopwatch.GetTimestamp();
        }

        public int BufferedBytes => _bufferLength;

        public void Feed(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            int newLength = _bufferLength + data.Length;
            if (newLength > _maxFrameLength)
                throw new InvalidOperationException($"Modbus RTU frame exceeds maximum length {_maxFrameLength}.");

            if (_buffer.Length < newLength)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(newLength);
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _bufferLength);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
            data.CopyTo(_buffer.AsSpan(_bufferLength));
            _bufferLength = newLength;
            _lastByteTicks = Stopwatch.GetTimestamp();
        }

        public bool TryGetFrame(out ReadOnlyMemory<byte> frame)
        {
            if (_bufferLength == 0)
            {
                frame = default;
                return false;
            }

            // Check if silence interval has elapsed
            long elapsed = Stopwatch.GetTimestamp() - _lastByteTicks;
            long silenceTicks = (long)_silenceMs * Stopwatch.Frequency / 1000;

            if (elapsed < silenceTicks)
            {
                frame = default;
                return false;
            }

            // Silence detected — flush buffer as frame
            frame = new ReadOnlyMemory<byte>(_buffer, 0, _bufferLength);
            _bufferLength = 0;
            return true;
        }

        public void Reset()
        {
            _bufferLength = 0;
            _lastByteTicks = Stopwatch.GetTimestamp();
        }
    }
}
