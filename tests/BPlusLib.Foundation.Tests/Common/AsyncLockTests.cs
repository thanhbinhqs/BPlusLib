// <copyright file="AsyncLockTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class AsyncLockTests
    {
        [Fact]
        public async Task LockAsync_BasicAcquireAndRelease_CompletesSuccessfully()
        {
            var asyncLock = new AsyncLock();
            var releaser = await asyncLock.LockAsync();
            // While locked, no one else can enter
            var wasLocked = true;
            var task = Task.Run(async () =>
            {
                using var innerReleaser = await asyncLock.LockAsync();
                wasLocked = false;
            });

            // Give the inner task a moment — it should be blocked
            await Task.Delay(100);
            wasLocked.Should().BeTrue();

            releaser.Dispose(); // release

            await task;
            wasLocked.Should().BeFalse();
        }

        [Fact]
        public async Task ConcurrentTasks_AreSerialized()
        {
            var asyncLock = new AsyncLock();
            var sharedCounter = 0;
            const int iterations = 50;

            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        using var releaser = await asyncLock.LockAsync();
                        var temp = sharedCounter;
                        await Task.Yield();
                        sharedCounter = temp + 1;
                    }
                });
            }

            await Task.WhenAll(tasks);
            sharedCounter.Should().Be(tasks.Length * iterations);
        }

        [Fact]
        public async Task CancellationToken_CancelsWaitingTask()
        {
            var asyncLock = new AsyncLock();
            var releaser = await asyncLock.LockAsync(); // acquire first

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            Func<Task> act = async () =>
            {
                using var r = await asyncLock.LockAsync(cts.Token);
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
            releaser.Dispose();
        }

        [Fact]
        public async Task Lock_SyncBlocking_Blocks()
        {
            var asyncLock = new AsyncLock();
            var releaser = asyncLock.Lock(); // acquire

            var wasBlocked = true;
            var task = Task.Run(() =>
            {
                using var r = asyncLock.Lock();
                wasBlocked = false;
            });

            var completed = await Task.WhenAny(task, Task.Delay(200));
            completed.Should().NotBe(task, "the sync lock should block");
            wasBlocked.Should().BeTrue();

            releaser.Dispose();

            await task;
            wasBlocked.Should().BeFalse();
        }
    }
}
