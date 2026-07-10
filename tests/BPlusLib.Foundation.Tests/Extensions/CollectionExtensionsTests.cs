// <copyright file="CollectionExtensionsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Extensions;

namespace BPlusLib.Foundation.Tests.Extensions
{
    [Trait("Category", "Extensions")]
    public sealed class CollectionExtensionsTests
    {
        // ── AddRange ──────────────────────────────────────────────────────

        [Fact]
        public void AddRange_AddsItems()
        {
            var list = new List<int> { 1, 2 };
            list.AddRange(new[] { 3, 4, 5 });
            list.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void AddRange_WithNullCollection_ShouldThrow()
        {
            List<int>? nullList = null;
            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.AddRange(nullList!, new[] { 1 });
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddRange_WithNullItems_ShouldThrow()
        {
            var list = new List<int>();
            Action act = () => list.AddRange(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddRange_WithEmptyItems_DoesNotAdd()
        {
            var list = new List<int> { 1 };
            list.AddRange(Array.Empty<int>());
            list.Should().HaveCount(1);
        }

        // ── RemoveWhere ────────────────────────────────────────────────────

        [Fact]
        public void RemoveWhere_RemovesMatching()
        {
            var list = new List<int> { 1, 2, 3, 4, 5, 6 };
            int removed = list.RemoveWhere(x => x % 2 == 0);
            removed.Should().Be(3);
            list.Should().BeEquivalentTo(new[] { 1, 3, 5 });
        }

        [Fact]
        public void RemoveWhere_NoMatch_ReturnsZero()
        {
            var list = new List<int> { 1, 3, 5 };
            int removed = list.RemoveWhere(x => x > 10);
            removed.Should().Be(0);
            list.Should().HaveCount(3);
        }

        [Fact]
        public void RemoveWhere_WithNullCollection_ShouldThrow()
        {
            List<int>? nullList = null;
            Action act = () => nullList!.RemoveWhere(x => true);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RemoveWhere_WithNullPredicate_ShouldThrow()
        {
            var list = new List<int>();
            Action act = () => list.RemoveWhere(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        // ── Batch ──────────────────────────────────────────────────────────

        [Fact]
        public void Batch_SmallerThanBatch_ReturnsOneBatch()
        {
            var source = new[] { 1, 2, 3 };
            var batches = source.Batch(10).ToList();
            batches.Should().HaveCount(1);
            batches[0].Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Batch_LargerThanBatch_Splits()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6, 7 };
            var batches = source.Batch(3).ToList();
            batches.Should().HaveCount(3);
            batches[0].Should().BeEquivalentTo(new[] { 1, 2, 3 });
            batches[1].Should().BeEquivalentTo(new[] { 4, 5, 6 });
            batches[2].Should().BeEquivalentTo(new[] { 7 });
        }

        [Fact]
        public void Batch_Empty_ReturnsEmpty()
        {
            var source = Array.Empty<int>();
            var batches = source.Batch(5).ToList();
            batches.Should().BeEmpty();
        }

        [Fact]
        public void Batch_WithNullSource_ShouldThrow()
        {
            int[]? nullSource = null;
            Action act = () => nullSource!.Batch(5).ToList();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Batch_WithInvalidBatchSize_ShouldThrow()
        {
            var source = new[] { 1 };
            Action act = () => source.Batch(0).ToList();
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // ── DistinctBy ─────────────────────────────────────────────────────

        [Fact]
        public void DistinctBy_RemovesDuplicates()
        {
            var source = new[] { "a", "bb", "ccc", "dd", "eee" };
            var result = BPlusLib.Foundation.Extensions.CollectionExtensions.DistinctBy(source, s => s.Length).ToList();
            result.Should().BeEquivalentTo(new[] { "a", "bb", "ccc" });
        }

        [Fact]
        public void DistinctBy_AllUnique_ReturnsAll()
        {
            var source = new[] { 1, 2, 3 };
            var result = BPlusLib.Foundation.Extensions.CollectionExtensions.DistinctBy(source, x => x).ToList();
            result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void DistinctBy_Empty_ReturnsEmpty()
        {
            var source = Array.Empty<int>();
            var result = BPlusLib.Foundation.Extensions.CollectionExtensions.DistinctBy(source, x => x).ToList();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DistinctBy_WithNullSource_ShouldThrow()
        {
            int[]? nullSource = null;
            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.DistinctBy(nullSource!, x => x).ToList();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DistinctBy_WithNullSelector_ShouldThrow()
        {
            var source = new[] { 1 };
            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.DistinctBy(source, (Func<int, int>)null!).ToList();
            act.Should().Throw<ArgumentNullException>();
        }

        // ── ForEach ────────────────────────────────────────────────────────

        [Fact]
        public void ForEach_WithIndex_ProvidesIndex()
        {
            var source = new[] { "a", "b", "c" };
            var indices = new List<int>();
            var items = new List<string>();

            source.ForEach((item, index) =>
            {
                items.Add(item);
                indices.Add(index);
            });

            items.Should().BeEquivalentTo(new[] { "a", "b", "c" }, opts => opts.WithStrictOrdering());
            indices.Should().BeEquivalentTo(new[] { 0, 1, 2 }, opts => opts.WithStrictOrdering());
        }

        [Fact]
        public void ForEach_Empty_DoesNothing()
        {
            var source = Array.Empty<int>();
            bool invoked = false;
            source.ForEach((_, _) => invoked = true);
            invoked.Should().BeFalse();
        }

        [Fact]
        public void ForEach_WithNullSource_ShouldThrow()
        {
            int[]? nullSource = null;
            Action act = () => nullSource!.ForEach((_, _) => { });
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ForEach_WithNullAction_ShouldThrow()
        {
            var source = new[] { 1 };
            Action act = () => source.ForEach(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        // ── IsNullOrEmpty ──────────────────────────────────────────────────

        [Fact]
        public void IsNullOrEmpty_Null_ReturnsTrue()
        {
            List<int>? nullList = null;
            nullList.IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void IsNullOrEmpty_Empty_ReturnsTrue()
        {
            var empty = new List<int>();
            empty.IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void IsNullOrEmpty_NonEmpty_ReturnsFalse()
        {
            var list = new List<int> { 1 };
            list.IsNullOrEmpty().Should().BeFalse();
        }

        // ── Shuffle ────────────────────────────────────────────────────────

        [Fact]
        public void Shuffle_ShufflesInPlace()
        {
            var original = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var shuffled = new List<int>(original);

            shuffled.Shuffle();
            shuffled.Should().HaveCount(original.Count);
            shuffled.Should().Contain(original);
            // Very unlikely (1/10!) that shuffle produced the same order
            shuffled.Should().NotBeEquivalentTo(original, opts => opts.WithStrictOrdering());
        }

        [Fact]
        public void Shuffle_Empty_DoesNotThrow()
        {
            var empty = new List<int>();
            Action act = () => empty.Shuffle();
            act.Should().NotThrow();
        }

        [Fact]
        public void Shuffle_SingleElement_DoesNotChange()
        {
            var single = new List<int> { 42 };
            single.Shuffle();
            single.Should().BeEquivalentTo(new[] { 42 });
        }

        [Fact]
        public void Shuffle_WithNullList_ShouldThrow()
        {
            List<int>? nullList = null;
            Action act = () => nullList!.Shuffle();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Shuffle_WithCustomRng_UsesIt()
        {
            var list = new List<int> { 1, 2, 3 };
            var rng = new Random(42);
            list.Shuffle(rng);
            list.Should().HaveCount(3);
        }

        // ── ToDictionary ──────────────────────────────────────────────────

        [Fact]
        public void ToDictionary_FromKeyValuePairs()
        {
            var source = new[]
            {
                new KeyValuePair<string, int>("one", 1),
                new KeyValuePair<string, int>("two", 2),
                new KeyValuePair<string, int>("three", 3),
            };
            var dict = BPlusLib.Foundation.Extensions.CollectionExtensions.ToDictionary(source);
            dict.Should().HaveCount(3);
            dict["one"].Should().Be(1);
            dict["two"].Should().Be(2);
            dict["three"].Should().Be(3);
        }

        [Fact]
        public void ToDictionary_Empty_ReturnsEmpty()
        {
            var source = Array.Empty<KeyValuePair<string, int>>();
            var dict = BPlusLib.Foundation.Extensions.CollectionExtensions.ToDictionary(source);
            dict.Should().BeEmpty();
        }

        [Fact]
        public void ToDictionary_WithNullSource_ShouldThrow()
        {
            IEnumerable<KeyValuePair<string, int>>? nullSource = null;
            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.ToDictionary(nullSource!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ToDictionary_DuplicateKeys_ShouldThrow()
        {
            var source = new[]
            {
                new KeyValuePair<string, int>("key", 1),
                new KeyValuePair<string, int>("key", 2),
            };

            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.ToDictionary(source);
            act.Should().Throw<ArgumentException>();
        }

        // ── GetValueOrDefault ──────────────────────────────────────────────

        [Fact]
        public void GetValueOrDefault_ExistingKey_ReturnsValue()
        {
            var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            BPlusLib.Foundation.Extensions.CollectionExtensions.GetValueOrDefault(dict, "a").Should().Be(1);
            BPlusLib.Foundation.Extensions.CollectionExtensions.GetValueOrDefault(dict, "b").Should().Be(2);
        }

        [Fact]
        public void GetValueOrDefault_MissingKey_ReturnsDefault()
        {
            var dict = new Dictionary<string, int> { { "a", 1 } };
            int result = BPlusLib.Foundation.Extensions.CollectionExtensions.GetValueOrDefault(dict, "nonexistent");
            result.Should().Be(default);
        }

        [Fact]
        public void GetValueOrDefault_WithNullDict_ShouldThrow()
        {
            Dictionary<string, int>? nullDict = null;
            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.GetValueOrDefault(nullDict!, "key");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetValueOrDefault_WithNullKey_ShouldThrow()
        {
            var dict = new Dictionary<string, int>();
            Action act = () => BPlusLib.Foundation.Extensions.CollectionExtensions.GetValueOrDefault(dict, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetValueOrDefault_ReferenceTypeMissing_ReturnsNull()
        {
            var dict = new Dictionary<string, string> { { "a", "hello" } };
            string? result = BPlusLib.Foundation.Extensions.CollectionExtensions.GetValueOrDefault(dict, "missing");
            result.Should().BeNull();
        }
    }
}
