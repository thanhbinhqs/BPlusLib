// <copyright file="ThreadHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Threading;

namespace BPlusLib.Foundation.Tests.Threading
{
    [Trait("Category", "Threading")]
    public sealed class ThreadHelperTests
    {
        // ── IsMainThread ─────────────────────────────────────────────────

        [Fact]
        public void IsMainThread_ShouldBeTrue()
        {
            // The xUnit test runner executes this on the main test thread.
            bool isMain = ThreadHelper.IsMainThread();

            isMain.Should().BeTrue("because the test runs on the main thread");
        }

        // ── GetApartmentState ─────────────────────────────────────────────

        [Fact]
        public void GetApartmentState_ShouldReturnOrUnknown()
        {
            ApartmentState state = ThreadHelper.GetApartmentState();

            // On Linux, SetApartmentState is not supported and the getter
            // may return Unknown. On Windows, the test runner thread
            // may have MTA or STA depending on configuration.
            state.Should().BeOneOf(
                ApartmentState.MTA,
                ApartmentState.STA,
                ApartmentState.Unknown);
        }

        // ── IsUIThread ────────────────────────────────────────────────────

        [Fact]
        public void IsUIThread_ShouldBeFalse()
        {
            // In a unit test, there is no Windows Forms or WPF
            // SynchronizationContext installed, so IsUIThread returns false.
            bool isUI = ThreadHelper.IsUIThread();

            isUI.Should().BeFalse("because there is no UI SynchronizationContext in a test");
        }

        // ── RunInSta ──────────────────────────────────────────────────────

        [Fact]
        public void RunInSta_ShouldExecuteAction()
        {
            int executed = 0;

            ThreadHelper.RunInSta(() => { Interlocked.Increment(ref executed); });

            executed.Should().Be(1);
        }

        [Fact]
        public void RunInSta_NullAction_ShouldThrow()
        {
            Action act = () => ThreadHelper.RunInSta(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RunInSta_ShouldRunOnDifferentThread()
        {
            int originalThreadId = Environment.CurrentManagedThreadId;
            int staThreadId = 0;

            ThreadHelper.RunInSta(() =>
            {
                staThreadId = Environment.CurrentManagedThreadId;
            });

            staThreadId.Should().NotBe(originalThreadId);
        }

        // ── RunInSta with result ──────────────────────────────────────────

        [Fact]
        public void RunInSta_WithResult_ReturnsValue()
        {
            int result = ThreadHelper.RunInSta(() => 42);

            result.Should().Be(42);
        }

        [Fact]
        public void RunInSta_WithNullFunc_ShouldThrow()
        {
            Action act = () => ThreadHelper.RunInSta<object>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RunInSta_WithResult_ReturnsNullForReferenceType()
        {
            string? result = ThreadHelper.RunInSta<string>(() => null!);

            result.Should().BeNull();
        }

        // ── RunInStaAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task RunInStaAsync_ShouldComplete()
        {
            int executed = 0;

            await ThreadHelper.RunInStaAsync(() =>
            {
                Interlocked.Increment(ref executed);
            });

            executed.Should().Be(1);
        }

        [Fact]
        public async Task RunInStaAsync_NullAction_ShouldThrow()
        {
            Func<Task> act = () => ThreadHelper.RunInStaAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        // ── RunInStaAsync with result ─────────────────────────────────────

        [Fact]
        public async Task RunInStaAsync_WithResult_ReturnsValue()
        {
            int result = await ThreadHelper.RunInStaAsync(() => 99);

            result.Should().Be(99);
        }

        [Fact]
        public async Task RunInStaAsync_WithNullFunc_ShouldThrow()
        {
            Func<Task> act = () => ThreadHelper.RunInStaAsync<object>(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        // ── RunInMta ──────────────────────────────────────────────────────

        [Fact]
        public void RunInMta_ShouldExecuteAction()
        {
            int executed = 0;

            ThreadHelper.RunInMta(() => { Interlocked.Increment(ref executed); });

            executed.Should().Be(1);
        }

        [Fact]
        public void RunInMta_NullAction_ShouldThrow()
        {
            Action act = () => ThreadHelper.RunInMta(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RunInMta_ShouldRunOnDifferentThread()
        {
            int originalThreadId = Environment.CurrentManagedThreadId;
            int mtaThreadId = 0;

            ThreadHelper.RunInMta(() =>
            {
                mtaThreadId = Environment.CurrentManagedThreadId;
            });

            mtaThreadId.Should().NotBe(originalThreadId);
        }

        // ── DelayExecute ──────────────────────────────────────────────────

        [Fact]
        public void DelayExecute_ShouldExecuteAfterDelay()
        {
            int executed = 0;
            var resetEvent = new ManualResetEventSlim(false);

            ThreadHelper.DelayExecute(10, () =>
            {
                Interlocked.Increment(ref executed);
                resetEvent.Set();
            });

            // Wait for the delayed action to complete
            bool signaled = resetEvent.Wait(TimeSpan.FromSeconds(5));

            signaled.Should().BeTrue("the delayed action should have executed");
            executed.Should().Be(1);
        }

        [Fact]
        public void DelayExecute_NullAction_ShouldThrow()
        {
            Action act = () => ThreadHelper.DelayExecute(10, null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DelayExecute_NegativeDelay_ShouldThrow()
        {
            Action act = () => ThreadHelper.DelayExecute(-1, () => { });

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // ── LockedExecute ─────────────────────────────────────────────────

        [Fact]
        public void LockedExecute_ShouldExecuteSafely()
        {
            var lockObj = new object();
            int counter = 0;

            ThreadHelper.LockedExecute(() =>
            {
                counter++;
            }, lockObj);

            counter.Should().Be(1);
        }

        [Fact]
        public void LockedExecute_NullAction_ShouldThrow()
        {
            var lockObj = new object();

            Action act = () => ThreadHelper.LockedExecute(null!, lockObj);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LockedExecute_NullLockObject_ShouldThrow()
        {
            Action act = () => ThreadHelper.LockedExecute(() => { }, null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LockedExecute_WithResult_ReturnsValue()
        {
            var lockObj = new object();

            int result = ThreadHelper.LockedExecute(() => 42, lockObj);

            result.Should().Be(42);
        }

        [Fact]
        public void LockedExecute_WithResult_NullFunc_ShouldThrow()
        {
            var lockObj = new object();

            Action act = () => ThreadHelper.LockedExecute<object>(null!, lockObj);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LockedExecute_WithResult_NullLockObject_ShouldThrow()
        {
            Action act = () => ThreadHelper.LockedExecute(() => 42, null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LockedExecute_ConcurrentAccess_ShouldBeThreadSafe()
        {
            var lockObj = new object();
            int sharedCounter = 0;
            int iterations = 100;

            Parallel.For(0, iterations, _ =>
            {
                ThreadHelper.LockedExecute(() =>
                {
                    int current = sharedCounter;
                    // Simulate some work
                    Thread.SpinWait(10);
                    sharedCounter = current + 1;
                }, lockObj);
            });

            sharedCounter.Should().Be(iterations);
        }

        // ── SwitchToUiThread ──────────────────────────────────────────────

        [Fact]
        public void SwitchToUiThread_InTest_ShouldThrowBecauseNoUiContext()
        {
            // In a unit test, there's no UI SynchronizationContext,
            // so SwitchToUiThread should throw InvalidOperationException.
            Action act = () => ThreadHelper.SwitchToUiThread(() => { });

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*No UI synchronization context*");
        }

        [Fact]
        public void SwitchToUiThread_NullAction_ShouldThrow()
        {
            Action act = () => ThreadHelper.SwitchToUiThread(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── GetUiSynchronizationContext ───────────────────────────────────

        [Fact]
        public void GetUiSynchronizationContext_ShouldReturnNull()
        {
            var ctx = ThreadHelper.GetUiSynchronizationContext();

            ctx.Should().BeNull("because there is no UI SynchronizationContext in a test");
        }
    }
}
