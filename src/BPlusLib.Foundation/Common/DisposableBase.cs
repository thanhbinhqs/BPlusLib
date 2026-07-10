// <copyright file="DisposableBase.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Provides a thread-safe base implementation of the standard <see cref="IDisposable"/> pattern.
    /// Derived classes implement <see cref="DisposeManaged"/> to release managed resources
    /// and may override <see cref="DisposeUnmanaged"/> to release unmanaged resources.
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
        private int _isDisposed; // 0 = false, 1 = true; used with Interlocked.Exchange for thread safety

        /// <summary>
        /// Finalizer. Releases unmanaged resources without relying on the garbage collector
        /// to call <see cref="Dispose()"/> explicitly.
        /// </summary>
        ~DisposableBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// Thread-safe — uses <see cref="Interlocked"/> semantics.
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref _isDisposed) == 1;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// managed resources. This method is called by <see cref="Dispose(bool)"/> when
        /// the <c>disposing</c> parameter is <c>true</c>.
        /// </summary>
        protected abstract void DisposeManaged();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources. This method is called by <see cref="Dispose(bool)"/> regardless
        /// of the <c>disposing</c> parameter value.
        /// Override this method when the derived class owns unmanaged resources.
        /// </summary>
        protected virtual void DisposeUnmanaged()
        {
        }

        /// <summary>
        /// Releases the managed and optionally unmanaged resources used by this instance.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources (called from the finalizer).
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (disposing)
            {
                DisposeManaged();
            }

            DisposeUnmanaged();
        }

        /// <summary>
        /// Releases all resources used by this instance. Thread-safe; multiple calls are idempotent.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if this instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the object is disposed.</exception>
        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) == 1)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
