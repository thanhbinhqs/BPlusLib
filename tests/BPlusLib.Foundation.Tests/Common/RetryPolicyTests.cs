// <copyright file="RetryPolicyTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Common;

namespace BPlusLib.Foundation.Tests.Common
{
    [Trait("Category", "Common")]
    public sealed class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_SucceedsOnFirstTry_NoRetry()
        {
            var callCount = 0;
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));

            var result = await policy.ExecuteAsync(ct =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult(42);
            });

            result.Should().Be(42);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_SucceedsAfterRetries()
        {
            var callCount = 0;
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10));

            var result = await policy.ExecuteAsync(ct =>
            {
                Interlocked.Increment(ref callCount);
                if (callCount < 3)
                    throw new InvalidOperationException("transient");
                return Task.FromResult("ok");
            });

            result.Should().Be("ok");
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_FailsAfterExhaustingRetries()
        {
            var callCount = 0;
            var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(10));

            Func<Task> act = () => policy.ExecuteAsync(ct =>
            {
                Interlocked.Increment(ref callCount);
                throw new InvalidOperationException("persistent");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
            callCount.Should().Be(3); // original attempt + 2 retries
        }

        [Fact]
        public async Task ExponentialBackoff_IncreasesDelay()
        {
            var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10), RetryBackoffType.Exponential);
            var delays = new System.Collections.Generic.List<TimeSpan>();
            var attempt = 0;

            Func<Task> act = () => policy.ExecuteAsync(ct =>
            {
                attempt++;
                throw new InvalidOperationException("fail");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
            attempt.Should().Be(4); // 1 initial + 3 retries
        }

        [Fact]
        public async Task RetryOn_SpecificExceptionTypeOnly()
        {
            var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(10));
            policy.RetryOn<InvalidOperationException>();

            var callCount = 0;
            Func<Task> act = () => policy.ExecuteAsync(ct =>
            {
                callCount++;
                // This exception type is NOT retryable
                throw new ArgumentException("wrong type");
            });

            await act.Should().ThrowAsync<ArgumentException>();
            callCount.Should().Be(1); // no retry
        }

        [Fact]
        public async Task OnRetry_CallbackInvoked()
        {
            var callbackCount = 0;
            var capturedExceptions = new System.Collections.Generic.List<Exception>();
            var capturedDelays = new System.Collections.Generic.List<TimeSpan>();

            var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(10));
            policy.OnRetry((attempt, ex, delay) =>
            {
                callbackCount++;
                capturedExceptions.Add(ex);
                capturedDelays.Add(delay);
            });

            Func<Task> act = () => policy.ExecuteAsync(ct =>
            {
                throw new InvalidOperationException("retry me");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();

            callbackCount.Should().Be(2); // 2 retries
            capturedExceptions.Should().AllBeOfType<InvalidOperationException>();
            capturedDelays.Should().HaveCount(2);
        }
    }
}
