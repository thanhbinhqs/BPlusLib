using System;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Arbitration
{
    /// <summary>
    /// Serializes writes so each frame is atomic. Multiple sessions can write,
    /// but frames are not interleaved.
    /// </summary>
    public sealed class SerializedWriteArbiter : IWriteArbiter
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private int _sequenceCounter;

        public ValueTask<WriteToken> AcquireAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            // Wait for exclusive access to write
            return new ValueTask<WriteToken>(WaitAndCreateTokenAsync(sessionId, cancellationToken));
        }

        public ValueTask ReleaseAsync(WriteToken token, CancellationToken cancellationToken = default)
        {
            _semaphore.Release();
            return default;
        }

        private async Task<WriteToken> WaitAndCreateTokenAsync(Guid sessionId, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct).ConfigureAwait(false);
            int seq = Interlocked.Increment(ref _sequenceCounter);
            return new WriteToken(this, sessionId, seq);
        }
    }
}
