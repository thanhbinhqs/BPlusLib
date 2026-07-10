// <copyright file="Option.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Represents an optional value that may or may not exist.
    /// Provides a type-safe alternative to null references.
    /// </summary>
    /// <typeparam name="T">The type of the encapsulated value.</typeparam>
    public readonly struct Option<T> : IEquatable<Option<T>>
    {
        private readonly T? _value;
        private readonly bool _hasValue;

        private Option(T value)
        {
            _value = value;
            _hasValue = true;
        }

        /// <summary>
        /// Creates an <see cref="Option{T}"/> containing the specified value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>An option with the value present.</returns>
        public static Option<T> Some(T value) => new(value);

        /// <summary>
        /// Creates an <see cref="Option{T}"/> with no value.
        /// </summary>
        /// <returns>An empty option.</returns>
        public static Option<T> None => default;

        /// <summary>
        /// Gets a value indicating whether this option contains a value.
        /// </summary>
        public bool HasValue => _hasValue;

        /// <summary>
        /// Gets the encapsulated value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the option has no value.</exception>
        public T Value => HasValue
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a None Option.");

        /// <summary>
        /// Returns the encapsulated value if present, or the specified default value if absent.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the option is empty.</param>
        /// <returns>The encapsulated value or <paramref name="defaultValue"/>.</returns>
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? OrDefault(T? defaultValue = default) => HasValue ? _value! : defaultValue;

        /// <summary>
        /// Transforms the encapsulated value using the specified mapper function.
        /// If the option is empty, an empty option of the target type is returned.
        /// </summary>
        /// <typeparam name="TNew">The type of the mapped value.</typeparam>
        /// <param name="mapper">The function to transform the value.</param>
        /// <returns>A new option containing the mapped value, or an empty option.</returns>
        public Option<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (!HasValue)
                return Option<TNew>.None;

            return Option<TNew>.Some(mapper(_value!));
        }

        /// <summary>
        /// Chains another option-returning operation based on the encapsulated value.
        /// If the option is empty, an empty option is returned.
        /// </summary>
        /// <typeparam name="TNew">The type of the bound value.</typeparam>
        /// <param name="binder">The function to produce a new option from the value.</param>
        /// <returns>The option returned by <paramref name="binder"/>, or an empty option.</returns>
        public Option<TNew> Bind<TNew>(Func<T, Option<TNew>> binder)
        {
            if (!HasValue)
                return Option<TNew>.None;

            return binder(_value!);
        }

        /// <summary>
        /// Executes one of two actions depending on whether this option has a value.
        /// </summary>
        /// <param name="onSome">Action to execute with the value when present.</param>
        /// <param name="onNone">Action to execute when the option is empty.</param>
        public void Match(Action<T> onSome, Action onNone)
        {
            if (HasValue)
                onSome?.Invoke(_value!);
            else
                onNone?.Invoke();
        }

        /// <summary>
        /// Converts this option to a <see cref="Result{T}"/>.
        /// If the option has a value, the result will be successful.
        /// Otherwise, the result will contain the provided error.
        /// </summary>
        /// <param name="error">The error to use if the option is empty. Defaults to an <see cref="InvalidOperationException"/>.</param>
        /// <returns>A <see cref="Result{T}"/> representing the option.</returns>
        public Result<T> ToResult(Exception? error = null)
        {
            return HasValue
                ? Result<T>.Ok(_value!)
                : Result<T>.Fail(error ?? new InvalidOperationException("Option has no value."));
        }

        /// <summary>
        /// Filters this option by a predicate.
        /// Returns an empty option if the predicate returns <c>false</c>.
        /// </summary>
        /// <param name="predicate">The predicate to test the value against.</param>
        /// <returns>This option if the predicate matches, otherwise an empty option.</returns>
        public Option<T> Where(Func<T, bool> predicate)
        {
            if (!HasValue)
                return this;

            return predicate(_value!) ? this : None;
        }

        /// <summary>
        /// Explicitly converts a value to an <see cref="Option{T}"/>.
        /// A null reference results in an empty option.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public static explicit operator Option<T>(T? value) =>
            value is null ? None : Some(value);

        /// <inheritdoc />
        public bool Equals(Option<T> other) =>
            _hasValue == other._hasValue &&
            EqualityComparer<T?>.Default.Equals(_value, other._value);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_hasValue, _value);

        /// <summary>
        /// Determines whether two <see cref="Option{T}"/> instances are equal.
        /// </summary>
        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="Option{T}"/> instances are not equal.
        /// </summary>
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
    }
}
