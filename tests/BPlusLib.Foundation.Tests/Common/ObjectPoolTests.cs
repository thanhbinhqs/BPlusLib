// <copyright file="ObjectPoolTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class ObjectPoolTests
    {
        [Fact]
        public void Get_ReturnsItemFromPool()
        {
            var pool = new ObjectPool<string>(() => "new", maxPoolSize: 5);
            using var pooled = pool.Get();
            pooled.Item.Should().NotBeNull();
        }

        [Fact]
        public void Return_PutsItemBack()
        {
            var pool = new ObjectPool<string>(() => "new", maxPoolSize: 5);
            string? firstItem;

            using (var pooled = pool.Get())
            {
                firstItem = pooled.Item;
            }
            // Item was returned to pool via Dispose

            using var second = pool.Get();
            second.Item.Should().BeSameAs(firstItem);
        }

        [Fact]
        public void PoolReuses_ReturnedItems()
        {
            var pool = new ObjectPool<System.Text.StringBuilder>(() => new System.Text.StringBuilder(), maxPoolSize: 3);

            System.Text.StringBuilder retrieved;
            using (var pooled = pool.Get())
            {
                pooled.Item.Append("hello");
                retrieved = pooled.Item;
            }
            // Returned

            using var pooled2 = pool.Get();
            pooled2.Item.Should().BeSameAs(retrieved);
            // The returned item should be cleared state; StringBuilders retain state
            // but the identity check is what matters
        }

        [Fact]
        public void PoolCreates_NewItemsWhenEmpty()
        {
            var pool = new ObjectPool<string>(() => Guid.NewGuid().ToString(), maxPoolSize: 2);

            using var item1 = pool.Get();
            using var item2 = pool.Get();
            // Pool is now empty (max was 2, both taken)
            item1.Item.Should().NotBe(item2.Item);

            using var item3 = pool.Get();
            // Pool empty so creates new
            item3.Item.Should().NotBeNull();
        }
    }
}
