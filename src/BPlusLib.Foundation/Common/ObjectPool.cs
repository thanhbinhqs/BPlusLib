// <copyright file="ObjectPool.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// A reusable handle that returns an object to its pool when disposed.
    /// Use with <c>using</c> to ensure the item is returned automatically.
    /// </summary>
    /// <typeparam name="T">The type of the pooled object. Must be a reference type.</typeparam>
    public readonly struct PooledObject<T> : IDisposable
        where T : class
    {
        private readonly ObjectPool<T>? _pool;

        /// <summary>
        /// Gets the pooled item.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledObject{T}"/> struct.
        /// </summary>
        /// <param name="pool">The pool to return the item to on disposal.</param>
        /// <param name="item">The pooled item.</param>
        internal PooledObject(ObjectPool<T> pool, T item)
        {
            _pool = pool;
            Item = item;
        }

        /// <summary>
        /// Returns the item to the pool.
        /// </summary>
        public void Dispose()
        {
            _pool?.Return(Item);
        }
    }

    /// <summary>
    /// A thread-safe object pool that reuses instances of type <typeparamref name="T"/>.
    /// Items are stored in a <see cref="ConcurrentBag{T}"/> for efficient lock-free access.
    /// When the pool is full, excess items are disposed (if they implement <see cref="IDisposable"/>).
    /// </summary>
    /// <typeparam name="T">The type of objects to pool. Must be a reference type.</typeparam>
    public sealed class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly ConcurrentBag<T> _items = new();
        private readonly Func<T> _factory;
        private readonly int _maxPoolSize;
        private int _count;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="factory">A delegate that creates new instances of <typeparamref name="T"/>.</param>
        /// <param name="maxPoolSize">The maximum number of items to keep in the pool. Must be greater than 0.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxPoolSize"/> is less than or equal to 0.
        /// </exception>
        public ObjectPool(Func<T> factory, int maxPoolSize)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            if (maxPoolSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "maxPoolSize must be greater than 0.");

            _maxPoolSize = maxPoolSize;
        }

        /// <summary>
        /// Gets an item from the pool, or creates a new one if the pool is empty.
        /// The item is wrapped in a <see cref="PooledObject{T}"/> that returns it to the pool
        /// when disposed.
        /// </summary>
        /// <returns>A <see cref="PooledObject{T}"/> containing the pooled item.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public PooledObject<T> Get()
        {
            ThrowIfDisposed();

            if (_items.TryTake(out var item))
            {
                Interlocked.Decrement(ref _count);
                return new PooledObject<T>(this, item);
            }

            return new PooledObject<T>(this, _factory());
        }

        /// <summary>
        /// Returns an item to the pool. If the pool has reached its maximum size,
        /// the item is disposed (if it implements <see cref="IDisposable"/>) and discarded.
        /// </summary>
        /// <param name="item">The item to return to the pool. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is <c>null</c>.</exception>
        public void Return(T item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (_disposed)
            {
                DisposeIfPossible(item);
                return;
            }

            if (Interlocked.Increment(ref _count) <= _maxPoolSize)
            {
                _items.Add(item);
            }
            else
            {
                // Pool is full — dispose and discard
                Interlocked.Decrement(ref _count);
                DisposeIfPossible(item);
            }
        }

        /// <summary>
        /// Releases all resources used by the pool and disposes any remaining items.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            while (_items.TryTake(out var item))
            {
                DisposeIfPossible(item);
            }
        }

        private static void DisposeIfPossible(T item)
        {
            if (item is IDisposable disposable)
                disposable.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
