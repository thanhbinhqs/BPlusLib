// <copyright file="AsyncCacheTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class AsyncCacheTests
    {
        [Fact]
        public async Task GetAsync_FirstCall_InvokesFactory()
        {
            var callCount = 0;
            var cache = new AsyncCache<string, string>(
                (key, _) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult($"value:{key}");
                });

            var result = await cache.GetAsync("key1");

            result.Should().Be("value:key1");
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAsync_SecondCall_ReturnsCachedValue()
        {
            var callCount = 0;
            var cache = new AsyncCache<string, string>(
                (key, _) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult($"value:{key}");
                });

            var first = await cache.GetAsync("key1");
            var second = await cache.GetAsync("key1");

            first.Should().Be("value:key1");
            second.Should().Be("value:key1");
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task Expiry_EvictsAfterTimeout()
        {
            var callCount = 0;
            var cache = new AsyncCache<string, string>(
                (key, _) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult($"value:{key}");
                },
                expiry: TimeSpan.FromMilliseconds(100));

            var first = await cache.GetAsync("key1");
            first.Should().Be("value:key1");
            callCount.Should().Be(1);

            // Wait for expiry
            await Task.Delay(250);

            var second = await cache.GetAsync("key1");
            second.Should().Be("value:key1");
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task Invalidate_RemovesSpecificKey()
        {
            var callCount = 0;
            var cache = new AsyncCache<string, string>(
                (key, _) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult($"value:{key}");
                });

            await cache.GetAsync("key1");
            await cache.GetAsync("key2");

            cache.Invalidate("key1");

            var result = await cache.GetAsync("key1");
            result.Should().Be("value:key1");
            callCount.Should().Be(3); // key1 factory called twice
        }

        [Fact]
        public async Task Clear_RemovesAll()
        {
            var callCount = 0;
            var cache = new AsyncCache<string, string>(
                (key, _) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult($"v:{key}");
                });

            await cache.GetAsync("a");
            await cache.GetAsync("b");

            cache.Clear();

            await cache.GetAsync("a");
            await cache.GetAsync("b");

            callCount.Should().Be(4);
        }

        [Fact]
        public async Task ConcurrentRequests_OnlyInvokeFactoryOnce()
        {
            var callCount = 0;
            var cache = new AsyncCache<string, string>(
                async (key, ct) =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(200, ct);
                    return $"v:{key}";
                });

            var task1 = cache.GetAsync("same");
            var task2 = cache.GetAsync("same");

            var results = await Task.WhenAll(task1, task2);

            results[0].Should().Be("v:same");
            results[1].Should().Be("v:same");
            callCount.Should().Be(1);
        }
    }
}
