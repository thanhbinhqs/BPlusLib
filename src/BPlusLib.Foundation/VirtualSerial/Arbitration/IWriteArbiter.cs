using System;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.VirtualSerial.Arbitration
{
    /// <summary>
    /// Controls how concurrent writes from multiple sessions are serialized.
    /// </summary>
    public interface IWriteArbiter
    {
        /// <summary>
        /// Acquires the right to write. Returns a token that must be released.
        /// For Serialized policy, this is a no-op (returns default token).
        /// For SingleWriter, blocks until the current writer releases.
        /// For TransactionLock, acquires exclusive lock.
        /// </summary>
        ValueTask<WriteToken> AcquireAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a previously acquired write token.
        /// </summary>
        ValueTask ReleaseAsync(WriteToken token, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Token representing write access. Must be disposed to release.
    /// </summary>
    public readonly struct WriteToken : IAsyncDisposable
    {
        private readonly IWriteArbiter _arbiter;
        private readonly Guid _sessionId;
        private readonly int _sequenceNumber;

        internal WriteToken(IWriteArbiter arbiter, Guid sessionId, int sequenceNumber)
        {
            _arbiter = arbiter;
            _sessionId = sessionId;
            _sequenceNumber = sequenceNumber;
        }

        /// <summary>Session that holds this token.</summary>
        public Guid SessionId => _sessionId;

        /// <summary>Sequence number within the session.</summary>
        public int SequenceNumber => _sequenceNumber;

        public ValueTask DisposeAsync()
        {
            return _arbiter.ReleaseAsync(this);
        }
    }
}
