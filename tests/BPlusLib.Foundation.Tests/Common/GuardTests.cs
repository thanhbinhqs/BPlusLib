// <copyright file="GuardTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Common;

namespace BPlusLib.Foundation.Tests.Common
{
    [Trait("Category", "Common")]
    public sealed class GuardTests
    {
        // ── ThrowIfNull (object overload) ────────────────────────────

        [Fact]
        public void ThrowIfNull_WithNullObject_ThrowsArgumentNullException()
        {
            object? value = null;
            Action act = () => Guard.ThrowIfNull(value);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ThrowIfNull_WithNonNullObject_DoesNotThrow()
        {
            object value = new();
            Action act = () => Guard.ThrowIfNull(value);
            act.Should().NotThrow();
        }

        // ── ThrowIfNull<T> (generic overload) ────────────────────────

        [Fact]
        public void ThrowIfNullGeneric_WithNullReference_ThrowsArgumentNullException()
        {
            string? value = null;
            Action act = () => Guard.ThrowIfNull(value);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ThrowIfNullGeneric_WithNonNullReference_DoesNotThrow()
        {
            string value = "hello";
            Action act = () => Guard.ThrowIfNull(value);
            act.Should().NotThrow();
        }

        // ── ThrowIfNullOrEmpty ───────────────────────────────────────

        [Fact]
        public void ThrowIfNullOrEmpty_WithNull_ThrowsArgumentException()
        {
            string? value = null;
            Action act = () => Guard.ThrowIfNullOrEmpty(value);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ThrowIfNullOrEmpty_WithEmpty_ThrowsArgumentException()
        {
            string value = string.Empty;
            Action act = () => Guard.ThrowIfNullOrEmpty(value);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ThrowIfNullOrEmpty_WithValidString_DoesNotThrow()
        {
            string value = "valid";
            Action act = () => Guard.ThrowIfNullOrEmpty(value);
            act.Should().NotThrow();
        }

        // ── ThrowIfNullOrWhiteSpace ──────────────────────────────────

        [Fact]
        public void ThrowIfNullOrWhiteSpace_WithWhitespace_ThrowsArgumentException()
        {
            string value = "   ";
            Action act = () => Guard.ThrowIfNullOrWhiteSpace(value);
            act.Should().Throw<ArgumentException>();
        }

        // ── ThrowIfOutOfRange ────────────────────────────────────────

        [Fact]
        public void ThrowIfOutOfRange_ValueBelowMin_ThrowsArgumentOutOfRangeException()
        {
            Action act = () => Guard.ThrowIfOutOfRange(-1, 0, 10);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ThrowIfOutOfRange_ValueAboveMax_ThrowsArgumentOutOfRangeException()
        {
            Action act = () => Guard.ThrowIfOutOfRange(11, 0, 10);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ThrowIfOutOfRange_ValueInRange_DoesNotThrow()
        {
            Action act = () => Guard.ThrowIfOutOfRange(5, 0, 10);
            act.Should().NotThrow();
        }

        // ── ThrowIfDisposed ──────────────────────────────────────────

        [Fact]
        public void ThrowIfDisposed_WhenTrue_ThrowsObjectDisposedException()
        {
            Action act = () => Guard.ThrowIfDisposed(true);
            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ThrowIfDisposed_WhenFalse_DoesNotThrow()
        {
            Action act = () => Guard.ThrowIfDisposed(false);
            act.Should().NotThrow();
        }

        // ── ThrowIfInvalidOperation ──────────────────────────────────

        [Fact]
        public void ThrowIfInvalidOperation_WhenTrue_ThrowsInvalidOperationException()
        {
            Action act = () => Guard.ThrowIfInvalidOperation(true, "Should fail");
            act.Should().Throw<InvalidOperationException>().WithMessage("Should fail");
        }

        [Fact]
        public void ThrowIfInvalidOperation_WhenFalse_DoesNotThrow()
        {
            Action act = () => Guard.ThrowIfInvalidOperation(false, "Should not throw");
            act.Should().NotThrow();
        }
    }
}
