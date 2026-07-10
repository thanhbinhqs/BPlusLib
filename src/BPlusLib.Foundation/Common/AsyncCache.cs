// <copyright file="AsyncCache.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// A thread-safe, lazy-evaluated cache for asynchronous values, keyed by <typeparamref name="TKey"/>.
    /// Values are computed on demand via a factory delegate and optionally expire after a specified duration.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache. Must not be null.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public sealed class AsyncCache<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, Task<TValue>> _cache = new();
        private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _keyLocks = new();
        private readonly ConcurrentDictionary<TKey, CancellationTokenSource> _expiryCts = new();
        private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;
        private readonly TimeSpan? _expiry;
        private readonly CancellationTokenSource _cleanupCts = new();
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="factory">
        /// A delegate that asynchronously produces a value for a given key and cancellation token.
        /// </param>
        /// <param name="expiry">
        /// An optional duration after which a cached value is automatically invalidated.
        /// If <c>null</c>, values never expire once cached.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <c>null</c>.</exception>
        public AsyncCache(Func<TKey, CancellationToken, Task<TValue>> factory, TimeSpan? expiry = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _expiry = expiry;
        }

        /// <summary>
        /// Gets or creates the value associated with the specified key.
        /// If the value is already cached, the existing task is returned.
        /// The factory is invoked at most once per key unless the key is invalidated.
        /// </summary>
        /// <param name="key">The key to look up or create.</param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel waiting for the factory.
        /// Note: if the factory is already running, this token does not cancel the factory itself.
        /// </param>
        /// <returns>A task that resolves to the cached or newly created value.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled while waiting.</exception>
        public async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Fast path — value already cached
            if (_cache.TryGetValue(key, out var existingTask))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await existingTask.ConfigureAwait(false);
            }

            // Slow path — acquire per-key lock to ensure single factory invocation
            var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await keyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();

                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out existingTask))
                    return await existingTask.ConfigureAwait(false);

                // Create and cache the task
                var task = CreateValueAsync(key, cancellationToken);
                _cache[key] = task;
                return await task.ConfigureAwait(false);
            }
            finally
            {
                keyLock.Release();
            }
        }

        /// <summary>
        /// Removes the cached value for the specified key, if present.
        /// The next call to <see cref="GetAsync"/> for this key will invoke the factory again.
        /// </summary>
        /// <param name="key">The key to invalidate.</param>
        public void Invalidate(TKey key)
        {
            _cache.TryRemove(key, out _);

            if (_expiry.HasValue && _expiryCts.TryRemove(key, out var expiryCts))
            {
                CancelCts(expiryCts);
            }
        }

        /// <summary>
        /// Removes all cached values. The next calls to <see cref="GetAsync"/> will re-invoke the factory.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();

            if (_expiry.HasValue)
            {
                foreach (var kvp in _expiryCts)
                {
                    if (_expiryCts.TryRemove(kvp.Key, out var expiryCts))
                        CancelCts(expiryCts);
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the cache, cancelling pending expiry timers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try { _cleanupCts.Cancel(); } catch (ObjectDisposedException) { }
            _cleanupCts.Dispose();

            // Cancel all expiry timers
            foreach (var kvp in _expiryCts)
            {
                if (_expiryCts.TryRemove(kvp.Key, out var expiryCts))
                    CancelCts(expiryCts);
            }

            // Dispose all per-key semaphores
            foreach (var kvp in _keyLocks)
            {
                if (_keyLocks.TryRemove(kvp.Key, out var sem))
                    sem.Dispose();
            }

            _cache.Clear();
        }

        private async Task<TValue> CreateValueAsync(TKey key, CancellationToken cancellationToken)
        {
            try
            {
                TValue value = await _factory(key, cancellationToken).ConfigureAwait(false);

                if (_expiry.HasValue)
                {
                    ScheduleExpiry(key);
                }

                return value;
            }
            catch (OperationCanceledException)
            {
                _cache.TryRemove(key, out _);
                throw;
            }
            catch
            {
                // On failure, remove the failed task so the next caller retries the factory
                _cache.TryRemove(key, out _);
                throw;
            }
        }

        private void ScheduleExpiry(TKey key)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cleanupCts.Token);
            _expiryCts[key] = cts;
            var expiry = _expiry!.Value;

            // Fire-and-forget: delay then invalidate
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(expiry, cts.Token).ConfigureAwait(false);
                    Invalidate(key);
                }
                catch (OperationCanceledException)
                {
                    // Expiry cancelled (key invalidated manually or cache disposed)
                }
                finally
                {
                    cts.Dispose();
                }
            }, CancellationToken.None);
        }

        private static void CancelCts(CancellationTokenSource cts)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
            cts.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
