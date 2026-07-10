// <copyright file="TaskExtensionsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Extensions;

namespace BPlusLib.Foundation.Tests.Extensions
{
    [Trait("Category", "Extensions")]
    public sealed class TaskExtensionsTests
    {
        // ── FireAndForget ──────────────────────────────────────────────────

        [Fact]
        public void FireAndForget_ShouldNotThrow()
        {
            Action act = () => Task.CompletedTask.FireAndForget();
            act.Should().NotThrow();
        }

        [Fact]
        public void FireAndForget_FailingTask_ShouldNotThrow()
        {
            Action act = () => Task.FromException(new InvalidOperationException("fail")).FireAndForget();
            act.Should().NotThrow();
        }

        [Fact]
        public void FireAndForget_WithExceptionHandler_ShouldInvokeHandler()
        {
            Exception? captured = null;
            var failing = Task.FromException(new InvalidOperationException("test error"));
            failing.FireAndForget(ex => captured = ex);

            // Allow the fire-and-forget to execute
            Thread.Sleep(100);
            captured.Should().NotBeNull();
            captured!.Message.Should().Be("test error");
        }

        [Fact]
        public void FireAndForget_NullTask_ShouldThrow()
        {
            Task? nullTask = null;
            Action act = () => nullTask!.FireAndForget();
            act.Should().Throw<ArgumentNullException>();
        }

        // ── WithTimeout (generic) ──────────────────────────────────────────

        [Fact]
        public async Task WithTimeout_CompletingTask_ReturnsValue()
        {
            var task = Task.FromResult(42);
            int result = await task.WithTimeout(TimeSpan.FromSeconds(5));
            result.Should().Be(42);
        }

        [Fact]
        public async Task WithTimeout_SlowTask_ThrowsTimeoutException()
        {
            var slow = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ => 42);
            Func<Task<int>> act = () => slow.WithTimeout(TimeSpan.FromMilliseconds(1));
            await act.Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public async Task WithTimeout_FailingTask_PropagatesException()
        {
            var failing = Task.FromException<int>(new InvalidOperationException("fail"));
            Func<Task<int>> act = () => failing.WithTimeout(TimeSpan.FromSeconds(5));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task WithTimeout_NullTask_ShouldThrow()
        {
            Task<int>? nullTask = null;
            Func<Task<int>> act = () => nullTask!.WithTimeout(TimeSpan.FromSeconds(1));
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        // ── WithTimeout (non-generic) ──────────────────────────────────────

        [Fact]
        public async Task WithTimeout_NonGeneric_Completes()
        {
            var task = Task.CompletedTask;
            Func<Task> act = () => task.WithTimeout(TimeSpan.FromSeconds(5));
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task WithTimeout_NonGeneric_Slow_ThrowsTimeoutException()
        {
            var slow = Task.Delay(TimeSpan.FromSeconds(30));
            Func<Task> act = () => slow.WithTimeout(TimeSpan.FromMilliseconds(1));
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // ── WithRetry ──────────────────────────────────────────────────────

        [Fact]
        public async Task WithRetry_SuccessOnFirstAttempt_DoesNotRetry()
        {
            int attempts = 0;
            Func<Task<int>> factory = () =>
            {
                attempts++;
                return Task.FromResult(42);
            };

            int result = await factory.WithRetry(maxRetries: 3);
            result.Should().Be(42);
            attempts.Should().Be(1);
        }

        [Fact]
        public async Task WithRetry_AlwaysFails_Retries()
        {
            int attempts = 0;
            Func<Task<int>> factory = () =>
            {
                attempts++;
                return Task.FromException<int>(new InvalidOperationException("fail"));
            };

            Func<Task<int>> act = () => factory.WithRetry(maxRetries: 3);
            await act.Should().ThrowAsync<InvalidOperationException>();
            attempts.Should().Be(4); // 1 initial + 3 retries
        }

        [Fact]
        public async Task WithRetry_SucceedsAfterRetries_ReturnsResult()
        {
            int attempts = 0;
            Func<Task<int>> factory = () =>
            {
                attempts++;
                if (attempts < 3)
                    return Task.FromException<int>(new InvalidOperationException("fail"));
                return Task.FromResult(42);
            };

            int result = await factory.WithRetry(maxRetries: 3);
            result.Should().Be(42);
            attempts.Should().Be(3);
        }

        [Fact]
        public async Task WithRetry_NullFactory_ShouldThrow()
        {
            Func<Task<int>>? nullFactory = null;
            Func<Task<int>> act = () => nullFactory!.WithRetry();
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WithRetry_NegativeMaxRetries_ShouldThrow()
        {
            Func<Task<int>> factory = () => Task.FromResult(0);
            Func<Task<int>> act = () => factory.WithRetry(maxRetries: -1);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task WithRetry_ZeroRetries_DoesNotRetry()
        {
            int attempts = 0;
            Func<Task<int>> factory = () =>
            {
                attempts++;
                return Task.FromException<int>(new InvalidOperationException("fail"));
            };

            Func<Task<int>> act = () => factory.WithRetry(maxRetries: 0);
            await act.Should().ThrowAsync<InvalidOperationException>();
            attempts.Should().Be(1);
        }

        // ── WithCancellation ───────────────────────────────────────────────

        [Fact]
        public async Task WithCancellation_Completes_ReturnsValue()
        {
            var task = Task.FromResult(99);
            int result = await task.WithCancellation(CancellationToken.None);
            result.Should().Be(99);
        }

        [Fact]
        public async Task WithCancellation_Cancelled_Throws()
        {
            var neverCompletes = Task.Delay(Timeout.Infinite, CancellationToken.None);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task<int>> act = () => neverCompletes.ContinueWith(_ => 0).WithCancellation(cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task WithCancellation_NullTask_ShouldThrow()
        {
            Task<int>? nullTask = null;
            Func<Task<int>> act = () => nullTask!.WithCancellation(CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        // ── Memoize ────────────────────────────────────────────────────────

        [Fact]
        public void Memoize_ReturnsSameTask()
        {
            int invocationCount = 0;
            Func<Task<int>> factory = () =>
            {
                invocationCount++;
                return Task.FromResult(42);
            };

            Lazy<Task<int>> memoized = factory.Memoize();
            Task<int> t1 = memoized.Value;
            Task<int> t2 = memoized.Value;

            t1.Should().BeSameAs(t2);
            invocationCount.Should().Be(1);
        }

        [Fact]
        public async Task Memoize_Result_IsCorrect()
        {
            Func<Task<string>> factory = () => Task.FromResult("memoized");
            Lazy<Task<string>> memoized = factory.Memoize();
            string result = await memoized.Value;
            result.Should().Be("memoized");
        }

        [Fact]
        public void Memoize_NullFactory_ShouldThrow()
        {
            Func<Task<int>>? nullFactory = null;
            Action act = () => nullFactory!.Memoize();
            act.Should().Throw<ArgumentNullException>();
        }

        // ── SuppressException ──────────────────────────────────────────────

        [Fact]
        public async Task SuppressException_FailingTask_ReturnsDefault()
        {
            var failing = Task.FromException<string>(new InvalidOperationException("fail"));
            string? result = await failing.SuppressException();
            result.Should().BeNull();
        }

        [Fact]
        public async Task SuppressException_CompletingTask_ReturnsValue()
        {
            var task = Task.FromResult("success");
            string? result = await task.SuppressException();
            result.Should().Be("success");
        }

        [Fact]
        public async Task SuppressException_CancelledTask_ReturnsDefault()
        {
            var cancelled = Task.FromCanceled<string>(new CancellationToken(true));
            string? result = await cancelled.SuppressException();
            result.Should().BeNull();
        }

        [Fact]
        public async Task SuppressException_NullTask_ShouldThrow()
        {
            Task<string>? nullTask = null;
            Func<Task<string?>> act = () => nullTask!.SuppressException();
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
