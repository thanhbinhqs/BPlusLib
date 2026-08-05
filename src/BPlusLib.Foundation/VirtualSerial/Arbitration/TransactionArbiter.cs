using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Arbitration
{
    /// <summary>
    /// Transaction-based write arbiter. A session acquires exclusive write access,
    /// sends request, waits for response, then releases.
    /// </summary>
    public sealed class TransactionArbiter : IWriteArbiter
    {
        private long _ownerId; // 0 = no owner
        private Guid _ownerSession = Guid.Empty;
        private int _sequenceCounter;

        /// <summary>Transaction timeout. Default: 5 seconds.</summary>
        public TimeSpan TransactionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Event raised when a transaction is acquired.</summary>
        public event EventHandler<Guid>? TransactionAcquired;

        /// <summary>Event raised when a transaction is released.</summary>
        public event EventHandler<Guid>? TransactionReleased;

        public ValueTask<WriteToken> AcquireAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            long sessionIdLong = sessionId.GetHashCode();

            // If already the owner, re-acquire (nested calls)
            if (Interlocked.Read(ref _ownerId) == sessionIdLong)
            {
                int seq = Interlocked.Increment(ref _sequenceCounter);
                return ValueTask.FromResult(new WriteToken(this, sessionId, seq));
            }

            // Try to become owner
            if (Interlocked.CompareExchange(ref _ownerId, sessionIdLong, 0) == 0)
            {
                _ownerSession = sessionId;
                TransactionAcquired?.Invoke(this, sessionId);
                int seq = Interlocked.Increment(ref _sequenceCounter);
                return ValueTask.FromResult(new WriteToken(this, sessionId, seq));
            }

            // Someone else owns — wait
            return new ValueTask<WriteToken>(WaitForTransactionAsync(sessionId, cancellationToken));
        }

        public ValueTask ReleaseAsync(WriteToken token, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Read(ref _ownerId) == token.SessionId.GetHashCode())
            {
                Interlocked.Exchange(ref _ownerId, 0);
                _ownerSession = Guid.Empty;
                TransactionReleased?.Invoke(this, token.SessionId);
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Forces release of the current transaction owner.
        /// </summary>
        public void ForceRelease()
        {
            var prev = _ownerSession;
            Interlocked.Exchange(ref _ownerId, 0);
            _ownerSession = Guid.Empty;
            if (prev != Guid.Empty)
            {
                TransactionReleased?.Invoke(this, prev);
            }
        }

        private async Task<WriteToken> WaitForTransactionAsync(Guid sessionId, CancellationToken ct)
        {
            using var timeoutCts = new CancellationTokenSource(TransactionTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    if (Interlocked.CompareExchange(ref _ownerId, sessionId.GetHashCode(), 0) == 0)
                    {
                        _ownerSession = sessionId;
                        TransactionAcquired?.Invoke(this, sessionId);
                        int seq = Interlocked.Increment(ref _sequenceCounter);
                        return new WriteToken(this, sessionId, seq);
                    }

                    await Task.Delay(10, linkedCts.Token).ConfigureAwait(false);
                }

                throw new TimeoutException(
                    $"Transaction timeout after {TransactionTimeout.TotalSeconds}s for session {sessionId}.");
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Transaction timeout after {TransactionTimeout.TotalSeconds}s for session {sessionId}.");
            }
        }
    }
}
