// <copyright file="OptionTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class OptionTests
    {
        // ── Some / None ──────────────────────────────────────────────

        [Fact]
        public void Some_CreatesOptionWithValue()
        {
            var opt = Option<int>.Some(42);
            opt.HasValue.Should().BeTrue();
            opt.Value.Should().Be(42);
        }

        [Fact]
        public void None_CreatesOptionWithoutValue()
        {
            var opt = Option<int>.None;
            opt.HasValue.Should().BeFalse();
        }

        // ── Value access ─────────────────────────────────────────────

        [Fact]
        public void Value_OnNone_ThrowsInvalidOperationException()
        {
            var opt = Option<int>.None;
            Action act = () => { var v = opt.Value; };
            act.Should().Throw<InvalidOperationException>();
        }

        // ── OrDefault ─────────────────────────────────────────────────

        [Fact]
        public void OrDefault_OnSome_ReturnsValue()
        {
            var opt = Option<int>.Some(5);
            opt.OrDefault(-1).Should().Be(5);
        }

        [Fact]
        public void OrDefault_OnNone_ReturnsDefault()
        {
            var opt = Option<int>.None;
            opt.OrDefault(-1).Should().Be(-1);
        }

        // ── Map ───────────────────────────────────────────────────────

        [Fact]
        public void Map_OnSome_TransformsValue()
        {
            var opt = Option<int>.Some(3);
            var mapped = opt.Map(x => x * 2);
            mapped.HasValue.Should().BeTrue();
            mapped.Value.Should().Be(6);
        }

        [Fact]
        public void Map_OnNone_ReturnsNone()
        {
            var opt = Option<int>.None;
            var mapped = opt.Map(x => x.ToString());
            mapped.HasValue.Should().BeFalse();
        }

        // ── Bind ──────────────────────────────────────────────────────

        [Fact]
        public void Bind_OnSome_Chains()
        {
            var opt = Option<int>.Some(2);
            var bound = opt.Bind(x => Option<string>.Some($"num={x}"));
            bound.HasValue.Should().BeTrue();
            bound.Value.Should().Be("num=2");
        }

        [Fact]
        public void Bind_OnNone_ReturnsNone()
        {
            var opt = Option<int>.None;
            var bound = opt.Bind(x => Option<string>.Some("never"));
            bound.HasValue.Should().BeFalse();
        }

        // ── Match ─────────────────────────────────────────────────────

        [Fact]
        public void Match_OnSome_CallsOnSome()
        {
            var opt = Option<int>.Some(10);
            int? captured = null;
            opt.Match(
                onSome: val => captured = val,
                onNone: () => captured = -1);
            captured.Should().Be(10);
        }

        [Fact]
        public void Match_OnNone_CallsOnNone()
        {
            var opt = Option<int>.None;
            int? captured = null;
            opt.Match(
                onSome: val => captured = val,
                onNone: () => captured = -1);
            captured.Should().Be(-1);
        }

        // ── ToResult ──────────────────────────────────────────────────

        [Fact]
        public void ToResult_OnSome_ReturnsSuccess()
        {
            var opt = Option<int>.Some(7);
            var result = opt.ToResult();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(7);
        }

        [Fact]
        public void ToResult_OnNone_ReturnsFailure()
        {
            var opt = Option<int>.None;
            var result = opt.ToResult();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidOperationException>();
        }

        // ── Where ─────────────────────────────────────────────────────

        [Fact]
        public void Where_PredicateTrue_ReturnsSameOption()
        {
            var opt = Option<int>.Some(5);
            var filtered = opt.Where(x => x > 3);
            filtered.HasValue.Should().BeTrue();
            filtered.Value.Should().Be(5);
        }

        [Fact]
        public void Where_PredicateFalse_ReturnsNone()
        {
            var opt = Option<int>.Some(5);
            var filtered = opt.Where(x => x > 10);
            filtered.HasValue.Should().BeFalse();
        }

        [Fact]
        public void Where_OnNone_ReturnsNone()
        {
            var opt = Option<int>.None;
            var filtered = opt.Where(x => true);
            filtered.HasValue.Should().BeFalse();
        }
    }
}
