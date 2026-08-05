using System;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Arbitration
{
    /// <summary>
    /// Only one session can write at a time. Others are blocked or rejected.
    /// </summary>
    public sealed class SingleWriterArbiter : IWriteArbiter
    {
        private long _currentWriterId; // 0 = no writer
        private int _sequenceCounter;

        /// <summary>
        /// If true, non-owner sessions get access denied instead of blocking.
        /// </summary>
        public bool RejectNonOwner { get; set; }

        public ValueTask<WriteToken> AcquireAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            long sessionIdLong = sessionId.GetHashCode();

            if (RejectNonOwner && Interlocked.Read(ref _currentWriterId) != 0 && Interlocked.Read(ref _currentWriterId) != sessionIdLong)
            {
                throw new InvalidOperationException(
                    $"Session {sessionId} cannot write: another session is the active writer.");
            }

            // Spin wait for writer slot
            while (Interlocked.CompareExchange(ref _currentWriterId, sessionIdLong, 0) != 0)
            {
                Thread.SpinWait(100);
            }

            int seq = Interlocked.Increment(ref _sequenceCounter);
            return ValueTask.FromResult(new WriteToken(this, sessionId, seq));
        }

        public ValueTask ReleaseAsync(WriteToken token, CancellationToken cancellationToken = default)
        {
            Interlocked.Exchange(ref _currentWriterId, 0);
            return default;
        }
    }
}
