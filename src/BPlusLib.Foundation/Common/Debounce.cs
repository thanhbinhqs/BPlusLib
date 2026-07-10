// <copyright file="Debounce.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Provides debouncing for actions and asynchronous operations.
    /// Each invocation resets a timer; the action is only executed after the specified
    /// delay has elapsed without a new invocation.
    /// </summary>
    public sealed class Debounce : IDisposable
    {
        private readonly TimeSpan _delay;
        private readonly object _lock = new();
        private Timer? _timer;
        private Action? _action;
        private Func<Task>? _asyncAction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Debounce"/> class.
        /// </summary>
        /// <param name="delay">The debounce delay. Must be non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="delay"/> is negative.
        /// </exception>
        public Debounce(TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be non-negative.");

            _delay = delay;
        }

        /// <summary>
        /// Invokes the specified action after the debounce delay elapses.
        /// If <see cref="Invoke"/> or <see cref="InvokeAsync"/> is called again before the delay elapses,
        /// the timer is reset and the previous action is replaced.
        /// </summary>
        /// <param name="action">The action to execute after debouncing.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public void Invoke(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            lock (_lock)
            {
                ThrowIfDisposed();
                _action = action;
                _asyncAction = null;
                ResetTimer();
            }
        }

        /// <summary>
        /// Invokes the specified asynchronous operation after the debounce delay elapses.
        /// If <see cref="Invoke"/> or <see cref="InvokeAsync"/> is called again before the delay elapses,
        /// the timer is reset and the previous operation is replaced.
        /// </summary>
        /// <param name="action">The asynchronous operation to execute after debouncing.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public void InvokeAsync(Func<Task> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            lock (_lock)
            {
                ThrowIfDisposed();
                _asyncAction = action;
                _action = null;
                ResetTimer();
            }
        }

        /// <summary>
        /// Cancels any pending debounced operation. The action will not be executed.
        /// </summary>
        public void Cancel()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
                _action = null;
                _asyncAction = null;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Debounce"/> instance.
        /// Any pending operation is cancelled.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _timer?.Dispose();
                _timer = null;
                _action = null;
                _asyncAction = null;
            }
        }

        private void ResetTimer()
        {
            _timer?.Dispose();
            _timer = new Timer(
                callback: OnTimerElapsed,
                state: null,
                dueTime: _delay,
                period: Timeout.InfiniteTimeSpan);
        }

        private void OnTimerElapsed(object? state)
        {
            Action? action;
            Func<Task>? asyncAction;

            lock (_lock)
            {
                if (_disposed)
                    return;

                action = _action;
                asyncAction = _asyncAction;
                _action = null;
                _asyncAction = null;
                _timer?.Dispose();
                _timer = null;
            }

            if (asyncAction != null)
            {
                // Fire the async operation on the thread pool
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await asyncAction().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow exceptions in fire-and-forget to avoid crashing the process
                    }
                });
            }
            else
            {
                action?.Invoke();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
