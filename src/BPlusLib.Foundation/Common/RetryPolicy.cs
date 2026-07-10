// <copyright file="RetryPolicy.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Defines the backoff strategy to use between retry attempts.
    /// </summary>
    public enum RetryBackoffType
    {
        /// <summary>
        /// The delay between retries is constant (equal to the base delay).
        /// </summary>
        Constant,

        /// <summary>
        /// The delay increases linearly: baseDelay × attempt.
        /// </summary>
        Linear,

        /// <summary>
        /// The delay increases exponentially: baseDelay × 2^(attempt-1).
        /// </summary>
        Exponential,

        /// <summary>
        /// Exponential backoff with random jitter applied (50–100% of the calculated delay).
        /// </summary>
        Jitter,
    }

    /// <summary>
    /// Provides configurable retry logic for asynchronous operations with support for
    /// exception filtering, backoff strategies, and retry callbacks.
    /// </summary>
    public sealed class RetryPolicy
    {
        private static readonly Random _random = new();

        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;
        private readonly RetryBackoffType _backoffType;
        private readonly List<Type> _retryableExceptions = new();
        private Action<int, Exception, TimeSpan>? _onRetry;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts. Must be non-negative.</param>
        /// <param name="baseDelay">The base delay between retries. Must be non-negative.</param>
        /// <param name="backoffType">The backoff strategy to use between attempts.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxRetries"/> is negative, or if <paramref name="baseDelay"/> is negative.
        /// </exception>
        public RetryPolicy(int maxRetries, TimeSpan baseDelay, RetryBackoffType backoffType = RetryBackoffType.Exponential)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "maxRetries must be non-negative.");
            if (baseDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(baseDelay), "baseDelay must be non-negative.");

            _maxRetries = maxRetries;
            _baseDelay = baseDelay;
            _backoffType = backoffType;
        }

        /// <summary>
        /// Configures the policy to retry when an exception of type <typeparamref name="TException"/>
        /// (or a derived type) is thrown.
        /// By default, all exceptions are retried.
        /// </summary>
        /// <typeparam name="TException">The type of exception to retry on.</typeparam>
        /// <returns>This <see cref="RetryPolicy"/> instance for fluent chaining.</returns>
        public RetryPolicy RetryOn<TException>()
            where TException : Exception
        {
            _retryableExceptions.Add(typeof(TException));
            return this;
        }

        /// <summary>
        /// Registers a callback that is invoked before each retry attempt.
        /// </summary>
        /// <param name="callback">
        /// A delegate receiving the retry attempt number (1-based), the exception that triggered the retry,
        /// and the delay before the next attempt.
        /// </param>
        /// <returns>This <see cref="RetryPolicy"/> instance for fluent chaining.</returns>
        public RetryPolicy OnRetry(Action<int, Exception, TimeSpan> callback)
        {
            _onRetry = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>
        /// Executes the specified asynchronous operation with retry logic.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            var attempt = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await operation(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt))
                {
                    attempt++;
                    var delay = CalculateDelay(attempt);
                    _onRetry?.Invoke(attempt, ex, delay);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Executes the specified asynchronous void-returning operation with retry logic.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            var attempt = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await operation(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt))
                {
                    attempt++;
                    var delay = CalculateDelay(attempt);
                    _onRetry?.Invoke(attempt, ex, delay);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private bool ShouldRetry(Exception ex, int currentAttempts)
        {
            if (currentAttempts >= _maxRetries)
                return false;

            // If no specific exception types were registered, retry all exceptions
            if (_retryableExceptions.Count == 0)
                return true;

            foreach (var exceptionType in _retryableExceptions)
            {
                if (exceptionType.IsInstanceOfType(ex))
                    return true;
            }

            return false;
        }

        private TimeSpan CalculateDelay(int attempt)
        {
            var baseMs = _baseDelay.TotalMilliseconds;
            double delayMs;

            switch (_backoffType)
            {
                case RetryBackoffType.Constant:
                    delayMs = baseMs;
                    break;

                case RetryBackoffType.Linear:
                    delayMs = baseMs * attempt;
                    break;

                case RetryBackoffType.Exponential:
                    delayMs = baseMs * Math.Pow(2, attempt - 1);
                    break;

                case RetryBackoffType.Jitter:
                    delayMs = baseMs * Math.Pow(2, attempt - 1);
                    lock (_random)
                    {
                        // Apply random jitter: 50% to 100% of the calculated delay
                        delayMs *= 0.5 + (_random.NextDouble() * 0.5);
                    }

                    break;

                default:
                    delayMs = baseMs;
                    break;
            }

            // Clamp to avoid overflow and excessive delays (max ~24 days)
            if (delayMs > int.MaxValue)
                delayMs = int.MaxValue;

            if (delayMs < 0)
                delayMs = 0;

            return TimeSpan.FromMilliseconds(delayMs);
        }
    }
}
