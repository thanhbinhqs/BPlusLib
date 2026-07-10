// <copyright file="AsyncLock.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Provides an asynchronous mutual-exclusion lock.
    /// Based on <see cref="SemaphoreSlim"/> initialized to (1, 1).
    /// Recursive acquisition is not supported and will result in a deadlock by design.
    /// </summary>
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// Asynchronously acquires the lock, respecting cancellation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait.</param>
        /// <returns>A <see cref="AsyncLockReleaser"/> that releases the lock when disposed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the lock has been disposed.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the wait is cancelled.</exception>
        public async ValueTask<AsyncLockReleaser> LockAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new AsyncLockReleaser(this);
        }

        /// <summary>
        /// Synchronously acquires the lock, blocking the current thread until the lock is available.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait.</param>
        /// <returns>A <see cref="AsyncLockReleaser"/> that releases the lock when disposed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the lock has been disposed.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the wait is cancelled.</exception>
        public AsyncLockReleaser Lock(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            _semaphore.Wait(cancellationToken);
            return new AsyncLockReleaser(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AsyncLock"/>.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _semaphore.Dispose();
        }

        /// <summary>
        /// Releases the underlying semaphore (called by <see cref="AsyncLockReleaser.Dispose"/>).
        /// </summary>
        internal void Release()
        {
            try
            {
                _semaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // This can happen if the lock was disposed while a releaser was still in flight.
                // Silently ignore — the semaphore is already released or disposed.
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncLock));
        }
    }

    /// <summary>
    /// A disposable handle that releases an <see cref="AsyncLock"/> when disposed.
    /// Use with <c>using</c> or <c>await using</c>.
    /// </summary>
    public readonly struct AsyncLockReleaser : IDisposable
    {
        private readonly AsyncLock _lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLockReleaser"/> struct.
        /// </summary>
        /// <param name="lock">The async lock to release on disposal.</param>
        internal AsyncLockReleaser(AsyncLock @lock)
        {
            _lock = @lock;
        }

        /// <summary>
        /// Releases the associated <see cref="AsyncLock"/>.
        /// </summary>
        public void Dispose()
        {
            _lock?.Release();
        }
    }
}
