// <copyright file="Guard.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Defensive argument validation guard methods.
    /// Provides a consistent exception-throwing contract for all public APIs.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
        /// </summary>
        public static void ThrowIfNull<T>(
            [NotNull] T? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            where T : class
        {
            if (argument is null)
                ThrowArgumentNull(paramName);
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
        /// </summary>
        public static void ThrowIfNull(
            [NotNull] object? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
                ThrowArgumentNull(paramName);
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if <paramref name="argument"/> is null or empty.
        /// </summary>
        public static void ThrowIfNullOrEmpty(
            [NotNull] string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (string.IsNullOrEmpty(argument))
                ThrowArgumentException("Value cannot be null or empty.", paramName);
#pragma warning disable CS8777 // Parameter must have non-null value when exiting — guaranteed by throw above
        }
#pragma warning restore CS8777

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if <paramref name="argument"/> is null, empty, or whitespace.
        /// </summary>
        public static void ThrowIfNullOrWhiteSpace(
            [NotNull] string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(argument))
                ThrowArgumentException("Value cannot be null, empty, or consist only of whitespace.", paramName);
#pragma warning disable CS8777 // Parameter must have non-null value when exiting — guaranteed by throw above
        }
#pragma warning restore CS8777

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is outside [<paramref name="min"/>, <paramref name="max"/>].
        /// </summary>
        public static void ThrowIfOutOfRange(
            int value,
            int min,
            int max,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < min || value > max)
                ThrowArgumentOutOfRange(paramName, value, $"Value must be between {min} and {max}.");
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if <paramref name="isDisposed"/> is true.
        /// </summary>
        public static void ThrowIfDisposed(
            bool isDisposed,
            [CallerArgumentExpression(nameof(isDisposed))] string? paramName = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException(paramName ?? "object");
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="condition"/> is true.
        /// </summary>
        public static void ThrowIfInvalidOperation(
            bool condition,
            string message)
        {
            if (condition)
                throw new InvalidOperationException(message);
        }

        [DoesNotReturn]
        private static void ThrowArgumentNull(string? paramName) =>
            throw new ArgumentNullException(paramName);

        [DoesNotReturn]
        private static void ThrowArgumentException(string message, string? paramName) =>
            throw new ArgumentException(message, paramName);

        [DoesNotReturn]
        private static void ThrowArgumentOutOfRange(string? paramName, object actualValue, string message) =>
            throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }
}