// <copyright file="TaskExtensions.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="Task"/> and
    /// <see cref="Task{TResult}"/> to simplify common asynchronous
    /// patterns such as fire-and-forget, timeouts, retries, cancellation,
    /// memoization, and exception suppression.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Fires the task and forgets about it. The task runs in the
        /// background; any exceptions are silently swallowed unless an
        /// <paramref name="onException"/> callback is provided.
        /// </summary>
        /// <param name="task">The task to execute in a fire-and-forget manner.</param>
        /// <param name="onException">
        /// An optional callback invoked when the task faults. If
        /// <see langword="null"/>, exceptions are silently ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task"/> is <see langword="null"/>.
        /// </exception>
        public static void FireAndForget(this Task task, Action<Exception>? onException = null)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // Using an async void lambda so the exception handling is
            // fire-and-forget by design.
            _ = FireAndForgetInternal(task, onException);
        }

        /// <summary>
        /// Wraps the task with a timeout. If the task does not complete
        /// within the specified <paramref name="timeout"/>, a
        /// <see cref="TimeoutException"/> is thrown.
        /// </summary>
        /// <typeparam name="T">The result type of the task.</typeparam>
        /// <param name="task">The task to apply a timeout to.</param>
        /// <param name="timeout">The maximum time to wait for completion.</param>
        /// <returns>The result of the task if it completes within the timeout.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The task did not complete within the specified timeout.
        /// </exception>
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            using (var cts = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
                if (completedTask == task)
                {
                    cts.Cancel(); // cancel the delay timer
                    return await task.ConfigureAwait(false); // propagate original result/exception
                }

                throw new TimeoutException($"The operation did not complete within {timeout}.");
            }
        }

        /// <summary>
        /// Wraps the task with a timeout. If the task does not complete
        /// within the specified <paramref name="timeout"/>, a
        /// <see cref="TimeoutException"/> is thrown.
        /// </summary>
        /// <param name="task">The task to apply a timeout to.</param>
        /// <param name="timeout">The maximum time to wait for completion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The task did not complete within the specified timeout.
        /// </exception>
        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            using (var cts = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
                if (completedTask == task)
                {
                    cts.Cancel();
                    await task.ConfigureAwait(false); // propagate original exception
                    return;
                }

                throw new TimeoutException($"The operation did not complete within {timeout}.");
            }
        }

        /// <summary>
        /// Wraps an asynchronous operation with automatic retry logic.
        /// On failure, the operation is retried up to <paramref name="maxRetries"/>
        /// times with an optional delay between attempts.
        /// </summary>
        /// <typeparam name="T">The result type of the task.</typeparam>
        /// <param name="taskFactory">
        /// A factory function that produces the task to retry. The factory is
        /// called again on each retry.
        /// </param>
        /// <param name="maxRetries">
        /// The maximum number of retry attempts (excluding the initial try).
        /// Defaults to 3.
        /// </param>
        /// <param name="delay">
        /// The delay between retries. If <see langword="null"/>, no delay
        /// is inserted between attempts.
        /// </param>
        /// <returns>The result of the task if it eventually succeeds.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="taskFactory"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxRetries"/> is negative.
        /// </exception>
        /// <remarks>
        /// The last exception thrown by the task is re-thrown if all retries
        /// are exhausted.
        /// </remarks>
        public static async Task<T> WithRetry<T>(
            this Func<Task<T>> taskFactory,
            int maxRetries = 3,
            TimeSpan? delay = null)
        {
            if (taskFactory == null)
            {
                throw new ArgumentNullException(nameof(taskFactory));
            }

            if (maxRetries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "maxRetries must be non-negative.");
            }

            int attempts = 0;
            while (true)
            {
                try
                {
                    var task = taskFactory();
                    if (task == null)
                    {
                        throw new InvalidOperationException("The task factory returned a null task.");
                    }

                    return await task.ConfigureAwait(false);
                }
                catch (Exception) when (attempts < maxRetries)
                {
                    attempts++;
                    if (delay.HasValue)
                    {
                        await Task.Delay(delay.Value).ConfigureAwait(false);
                    }
                }
                // If attempts >= maxRetries, the exception propagates.
            }
        }

        /// <summary>
        /// Wraps the task so it can be cancelled via a
        /// <see cref="CancellationToken"/>. If the token is signalled before
        /// the task completes, the task is cancelled.
        /// </summary>
        /// <typeparam name="T">The result type of the task.</typeparam>
        /// <param name="task">The task to observe cancellation on.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The result of the task if it completes before cancellation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The operation was cancelled.
        /// </exception>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!ct.CanBeCanceled)
            {
                return await task.ConfigureAwait(false);
            }

            // Use Task.WhenAny to race the original task against a
            // cancellation-triggered task.
            var tcs = new TaskCompletionSource<object?>();
            using (ct.Register(state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), tcs))
            {
                var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completedTask == task)
                {
                    return await task.ConfigureAwait(false);
                }

                // Cancellation was requested; throw.
                throw new OperationCanceledException(ct);
            }
        }

        /// <summary>
        /// Memoizes (caches) the result of an asynchronous factory function,
        /// returning a <see cref="Lazy{T}"/> that ensures the factory is
        /// invoked at most once, even under concurrent access.
        /// </summary>
        /// <typeparam name="T">The result type of the task.</typeparam>
        /// <param name="factory">
        /// The asynchronous factory function to memoize.
        /// </param>
        /// <returns>
        /// A <c>Lazy&lt;Task&lt;T&gt;&gt;</c> that wraps the factory. The factory
        /// is invoked on the first access to <c>Value</c>; subsequent
        /// accesses return the same task.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <see langword="null"/>.
        /// </exception>
        public static Lazy<Task<T>> Memoize<T>(this Func<Task<T>> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            // LazyThreadSafetyMode.ExecutionAndPublication ensures the
            // factory is called at most once, even from multiple threads.
            return new Lazy<Task<T>>(() => factory(), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Suppresses any exception thrown by the task, returning
        /// <see langword="default"/> (<see langword="null"/> for reference
        /// types) on failure.
        /// </summary>
        /// <typeparam name="T">The result type of the task. Must be a reference type.</typeparam>
        /// <param name="task">The task to suppress exceptions for.</param>
        /// <returns>
        /// The task result if it succeeds; <see langword="default"/> if the
        /// task faults or is cancelled.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="task"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<T?> SuppressException<T>(this Task<T> task)
            where T : class
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                return default;
            }
        }

        private static async Task FireAndForgetInternal(Task task, Action<Exception>? onException)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex) when (onException != null)
            {
                onException(ex);
            }
            catch
            {
                // Silently swallow if no handler is provided.
            }
        }
    }
}
