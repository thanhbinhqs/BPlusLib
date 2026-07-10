// <copyright file="Result.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Represents the result of a void-returning operation that can succeed or fail.
    /// </summary>
    public readonly struct Result : IEquatable<Result>
    {
        private readonly Exception? _error;
        private readonly bool _isSuccess;

        private Result(bool isSuccess, Exception? error)
        {
            _isSuccess = isSuccess;
            _error = error;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static Result Ok() => new(true, null);

        /// <summary>
        /// Creates a failed result from an <see cref="Exception"/>.
        /// </summary>
        /// <param name="error">The exception that caused the failure.</param>
        public static Result Fail(Exception error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));

        /// <summary>
        /// Creates a failed result from an error message.
        /// </summary>
        /// <param name="message">The error message describing the failure.</param>
        public static Result Fail(string message) => new(false, new Exception(message));

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// Gets a value indicating whether the operation failed.
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// Gets the exception that caused the failure, or <c>null</c> if the operation succeeded.
        /// </summary>
        public Exception? Error => IsFailure ? _error : null;

        /// <summary>
        /// Executes one of two actions depending on whether the result is success or failure.
        /// </summary>
        /// <param name="onSuccess">Action to execute on success.</param>
        /// <param name="onFailure">Action to execute on failure with the exception.</param>
        public void Match(Action onSuccess, Action<Exception> onFailure)
        {
            if (IsSuccess)
                onSuccess?.Invoke();
            else
                onFailure?.Invoke(_error!);
        }

        /// <inheritdoc />
        public bool Equals(Result other) => _isSuccess == other._isSuccess && Equals(_error, other._error);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Result other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_isSuccess, _error);

        /// <summary>
        /// Determines whether two <see cref="Result"/> instances are equal.
        /// </summary>
        public static bool operator ==(Result left, Result right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="Result"/> instances are not equal.
        /// </summary>
        public static bool operator !=(Result left, Result right) => !left.Equals(right);
    }

    /// <summary>
    /// Represents the result of an operation that produces a value of type <typeparamref name="T"/>
    /// and can succeed or fail.
    /// </summary>
    /// <typeparam name="T">The type of the value produced on success.</typeparam>
    public readonly struct Result<T> : IEquatable<Result<T>>
    {
        private readonly T? _value;
        private readonly Exception? _error;
        private readonly bool _hasValue;

        private Result(T value)
        {
            _value = value;
            _error = null;
            _hasValue = true;
        }

        private Result(Exception error)
        {
            _value = default;
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _hasValue = false;
        }

        /// <summary>
        /// Creates a successful result containing the specified value.
        /// </summary>
        /// <param name="value">The success value.</param>
        public static Result<T> Ok(T value) => new(value);

        /// <summary>
        /// Creates a failed result from an <see cref="Exception"/>.
        /// </summary>
        /// <param name="error">The exception that caused the failure.</param>
        public static Result<T> Fail(Exception error) => new(error);

        /// <summary>
        /// Creates a failed result from an error message.
        /// </summary>
        /// <param name="message">The error message describing the failure.</param>
        public static Result<T> Fail(string message) => new(new Exception(message));

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool IsSuccess => _hasValue;

        /// <summary>
        /// Gets a value indicating whether the operation failed.
        /// </summary>
        public bool IsFailure => !_hasValue;

        /// <summary>
        /// Gets the exception that caused the failure, or <c>null</c> if the operation succeeded.
        /// </summary>
        public Exception? Error => IsFailure ? _error : null;

        /// <summary>
        /// Gets the success value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the result is in a failed state.</exception>
        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed Result.");

        /// <summary>
        /// Transforms the success value using the specified mapper function.
        /// If the result is in a failed state, the failure is propagated.
        /// </summary>
        /// <typeparam name="TNew">The type of the mapped value.</typeparam>
        /// <param name="mapper">The function to transform the success value.</param>
        /// <returns>A new <see cref="Result{TNew}"/> representing the mapped value or the propagated failure.</returns>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (IsFailure)
                return Result<TNew>.Fail(_error!);

            return Result<TNew>.Ok(mapper(_value!));
        }

        /// <summary>
        /// Chains another operation that returns a <see cref="Result{TNew}"/> based on the success value.
        /// If the result is in a failed state, the failure is propagated.
        /// </summary>
        /// <typeparam name="TNew">The type of the bound value.</typeparam>
        /// <param name="binder">The function to produce a new result from the success value.</param>
        /// <returns>A new <see cref="Result{TNew}"/> from the binder, or the propagated failure.</returns>
        public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
        {
            if (IsFailure)
                return Result<TNew>.Fail(_error!);

            return binder(_value!);
        }

        /// <summary>
        /// Returns the success value if the result succeeded, or the specified default value if it failed.
        /// </summary>
        /// <param name="defaultValue">The default value to return on failure.</param>
        /// <returns>The success value or <paramref name="defaultValue"/>.</returns>
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? OrDefault(T? defaultValue = default) => IsSuccess ? _value! : defaultValue;

        /// <summary>
        /// Returns the success value, or throws the exception that caused the failure.
        /// </summary>
        /// <returns>The success value.</returns>
        /// <exception cref="Exception">The exception that caused the failure.</exception>
        public T OrThrow()
        {
            if (IsFailure)
                throw _error!;

            return _value!;
        }

        /// <summary>
        /// Executes one of two actions depending on whether the result is success or failure.
        /// </summary>
        /// <param name="onSuccess">Action to execute on success with the value.</param>
        /// <param name="onFailure">Action to execute on failure with the exception.</param>
        public void Match(Action<T> onSuccess, Action<Exception> onFailure)
        {
            if (IsSuccess)
                onSuccess?.Invoke(_value!);
            else
                onFailure?.Invoke(_error!);
        }

        /// <inheritdoc />
        public bool Equals(Result<T> other) =>
            _hasValue == other._hasValue &&
            EqualityComparer<T?>.Default.Equals(_value, other._value) &&
            Equals(_error, other._error);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_hasValue, _value, _error);

        /// <summary>
        /// Determines whether two <see cref="Result{T}"/> instances are equal.
        /// </summary>
        public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="Result{T}"/> instances are not equal.
        /// </summary>
        public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
    }
}
