using System;
using System.Buffers;

namespace BPlusLib.Foundation.VirtualSerial.Framing
{
    /// <summary>
    /// No framing — each Feed call produces one frame immediately.
    /// </summary>
    public sealed class RawFramer : IFrameDecoder
    {
        private byte[]? _pending;
        private int _pendingLength;

        public int BufferedBytes => _pendingLength;

        public void Feed(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            // Append to pending buffer
            int newLength = _pendingLength + data.Length;
            if (_pending == null || _pending.Length < newLength)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(newLength);
                if (_pending != null)
                    System.Buffer.BlockCopy(_pending, 0, newBuffer, 0, _pendingLength);
                if (_pending != null) ArrayPool<byte>.Shared.Return(_pending);
                _pending = newBuffer;
            }
            data.CopyTo(_pending.AsSpan(_pendingLength));
            _pendingLength = newLength;
        }

        public bool TryGetFrame(out ReadOnlyMemory<byte> frame)
        {
            if (_pendingLength == 0)
            {
                frame = default;
                return false;
            }

            // Return all buffered data as one frame
            frame = new ReadOnlyMemory<byte>(_pending, 0, _pendingLength);
            _pendingLength = 0;
            return true;
        }

        public void Reset()
        {
            _pendingLength = 0;
        }
    }
}
