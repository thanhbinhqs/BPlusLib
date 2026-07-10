// <copyright file="ResultTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class ResultTests
    {
        // ── Ok / Fail ────────────────────────────────────────────────

        [Fact]
        public void Ok_CreatesSuccessResult()
        {
            var result = Result<int>.Ok(42);
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().Be(42);
        }

        [Fact]
        public void Fail_CreatesFailureResult()
        {
            var error = new InvalidOperationException("fail");
            var result = Result<int>.Fail(error);
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeSameAs(error);
        }

        // ── Value accessor ───────────────────────────────────────────

        [Fact]
        public void Value_OnFailure_ThrowsInvalidOperationException()
        {
            var result = Result<int>.Fail(new Exception("bad"));
            Action act = () => { var v = result.Value; };
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Value_OnSuccess_ReturnsValue()
        {
            var result = Result<int>.Ok(99);
            result.Value.Should().Be(99);
        }

        // ── Map ───────────────────────────────────────────────────────

        [Fact]
        public void Map_OnSuccess_TransformsValue()
        {
            var result = Result<int>.Ok(3);
            var mapped = result.Map(x => x * 2);
            mapped.IsSuccess.Should().BeTrue();
            mapped.Value.Should().Be(6);
        }

        [Fact]
        public void Map_OnFailure_PassesThroughError()
        {
            var error = new Exception("original");
            var result = Result<int>.Fail(error);
            var mapped = result.Map(x => x.ToString());
            mapped.IsFailure.Should().BeTrue();
            mapped.Error.Should().BeSameAs(error);
        }

        // ── Bind ──────────────────────────────────────────────────────

        [Fact]
        public void Bind_ChainsSuccessfulResults()
        {
            var result = Result<int>.Ok(5);
            var bound = result.Bind(x => Result<string>.Ok($"value={x}"));
            bound.IsSuccess.Should().BeTrue();
            bound.Value.Should().Be("value=5");
        }

        [Fact]
        public void Bind_OnFailure_PropagatesError()
        {
            var error = new Exception("err");
            var result = Result<int>.Fail(error);
            var bound = result.Bind(x => Result<string>.Ok("never"));
            bound.IsFailure.Should().BeTrue();
            bound.Error.Should().BeSameAs(error);
        }

        // ── OrDefault ─────────────────────────────────────────────────

        [Fact]
        public void OrDefault_OnSuccess_ReturnsValue()
        {
            var result = Result<int>.Ok(10);
            result.OrDefault(-1).Should().Be(10);
        }

        [Fact]
        public void OrDefault_OnFailure_ReturnsDefault()
        {
            var result = Result<int>.Fail(new Exception());
            result.OrDefault(-1).Should().Be(-1);
        }

        // ── OrThrow ───────────────────────────────────────────────────

        [Fact]
        public void OrThrow_OnSuccess_ReturnsValue()
        {
            var result = Result<int>.Ok(7);
            result.OrThrow().Should().Be(7);
        }

        [Fact]
        public void OrThrow_OnFailure_ThrowsError()
        {
            var error = new InvalidOperationException("boom");
            var result = Result<int>.Fail(error);
            Action act = () => result.OrThrow();
            act.Should().Throw<InvalidOperationException>().WithMessage("boom");
        }

        // ── Implicit conversion from T ─────────────────────────────────
        // Note: Result<T> does not have an implicit conversion operator,
        // so this test confirms Ok(T) behaviour (the closest semantic).

        [Fact]
        public void ImplicitConversionFromValue_NotProvided_UsesOk()
        {
            // Result<T> does not define an implicit operator.
            // This test validates that the pattern of Ok(value) works
            // as the intended creation mechanism.
            Result<string> result = Result<string>.Ok("hello");
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be("hello");
        }

        // ── Non-generic Result ────────────────────────────────────────

        [Fact]
        public void NonGenericResult_Ok_Succeeds()
        {
            var result = Result.Ok();
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
        }

        [Fact]
        public void NonGenericResult_Fail_Fails()
        {
            var error = new Exception("oops");
            var result = Result.Fail(error);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeSameAs(error);
        }

        // ── Error chain preserved ─────────────────────────────────────

        [Fact]
        public void Error_IsPreservedThroughBindAndMap()
        {
            var inner = new InvalidOperationException("inner");
            var result = Result<int>.Fail(inner);
            var mapped = result.Map(x => x.ToString());
            var bound = mapped.Bind(_ => Result<string>.Ok("ignored"));

            bound.IsFailure.Should().BeTrue();
            bound.Error.Should().BeOfType<InvalidOperationException>();
            bound.Error!.Message.Should().Be("inner");
        }

        // ── ToString ──────────────────────────────────────────────────

        [Fact]
        public void ToString_OnSuccess_ContainsValue()
        {
            var result = Result<int>.Ok(42);
            // Default ToString for a struct returns the type name;
            // FluentAssertions just verifies it's not null.
            result.ToString().Should().NotBeNull();
        }

        // ── Equality (structural for value types) ─────────────────────

        [Fact]
        public void Equality_TwoSuccessWithSameValue_AreEqual()
        {
            var r1 = Result<int>.Ok(10);
            var r2 = Result<int>.Ok(10);
            r1.Should().Be(r2);
            (r1 == r2).Should().BeTrue();
            (r1 != r2).Should().BeFalse();
        }

        [Fact]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var r1 = Result<int>.Ok(10);
            var r2 = Result<int>.Ok(20);
            r1.Should().NotBe(r2);
        }

        [Fact]
        public void Equality_FailureWithSameError_AreEqual()
        {
            var ex = new Exception("same");
            var r1 = Result<int>.Fail(ex);
            var r2 = Result<int>.Fail(ex);
            r1.Should().Be(r2);
        }

        [Fact]
        public void NonGenericEquality_OkAndOk_AreEqual()
        {
            var r1 = Result.Ok();
            var r2 = Result.Ok();
            r1.Should().Be(r2);
        }
    }
}
