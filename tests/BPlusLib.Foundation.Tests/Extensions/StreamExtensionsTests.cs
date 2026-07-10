// <copyright file="StreamExtensionsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Extensions;

namespace BPlusLib.Foundation.Tests.Extensions
{
    [Trait("Category", "Extensions")]
    public sealed class StreamExtensionsTests
    {
        // ── ReadAllBytes ────────────────────────────────────────────────────

        [Fact]
        public void ReadAllBytes_WithMemoryStream()
        {
            byte[] data = { 1, 2, 3, 4, 5 };
            using var ms = new MemoryStream(data);
            byte[] result = ms.ReadAllBytes();
            result.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void ReadAllBytes_WithNullStream_ShouldThrow()
        {
            Stream? nullStream = null;
            Action act = () => nullStream!.ReadAllBytes();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ReadAllBytes_EmptyStream_ReturnsEmpty()
        {
            using var ms = new MemoryStream();
            byte[] result = ms.ReadAllBytes();
            result.Should().BeEmpty();
        }

        // ── ReadAllText ─────────────────────────────────────────────────────

        [Fact]
        public void ReadAllText_ReturnsContent()
        {
            string expected = "Hello World";
            byte[] bytes = Encoding.UTF8.GetBytes(expected);
            using var ms = new MemoryStream(bytes);
            string result = ms.ReadAllText();
            result.Should().Be(expected);
        }

        [Fact]
        public void ReadAllText_WithEncoding_ReturnsContent()
        {
            string expected = "Hëllö";
            byte[] bytes = Encoding.UTF8.GetBytes(expected);
            using var ms = new MemoryStream(bytes);
            string result = ms.ReadAllText(Encoding.UTF8);
            result.Should().Be(expected);
        }

        [Fact]
        public void ReadAllText_WithNullStream_ShouldThrow()
        {
            Stream? nullStream = null;
            Action act = () => nullStream!.ReadAllText();
            act.Should().Throw<ArgumentNullException>();
        }

        // ── CopyToAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task CopyToAsync_WithProgress_ReportsProgress()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello World, this is a test for progress reporting");
            using var source = new MemoryStream(data);
            using var dest = new MemoryStream();
            long lastProgress = 0;

            // Use synchronous IProgress to avoid threading issues with Progress<T>
            var progress = new SynchronousProgress<long>(value => lastProgress = value);
            await source.CopyToAsync(dest, progress);

            lastProgress.Should().Be(data.Length);
            dest.ToArray().Should().BeEquivalentTo(data);
        }

        private sealed class SynchronousProgress<T> : IProgress<T>
        {
            private readonly Action<T> _handler;
            public SynchronousProgress(Action<T> handler) => _handler = handler;
            public void Report(T value) => _handler(value);
        }

        [Fact]
        public async Task CopyToAsync_WithCancellation_Cancels()
        {
            byte[] data = new byte[100_000];
            new Random(42).NextBytes(data);
            using var source = new MemoryStream(data);
            using var dest = new MemoryStream();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => source.CopyToAsync(dest, null, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task CopyToAsync_NullSource_ShouldThrow()
        {
            using var dest = new MemoryStream();
            Func<Task> act = () => BPlusLib.Foundation.Extensions.StreamExtensions.CopyToAsync(null!, dest);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CopyToAsync_NullDestination_ShouldThrow()
        {
            using var source = new MemoryStream();
            Func<Task> act = () => BPlusLib.Foundation.Extensions.StreamExtensions.CopyToAsync(source, null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CopyToAsync_ZeroBufferSize_ShouldThrow()
        {
            using var source = new MemoryStream();
            using var dest = new MemoryStream();
            Func<Task> act = () => source.CopyToAsync(dest, null, CancellationToken.None, 0);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        // ── Drain ──────────────────────────────────────────────────────────

        [Fact]
        public void Drain_DoesNotThrow()
        {
            byte[] data = new byte[1000];
            new Random(42).NextBytes(data);
            using var ms = new MemoryStream(data);
            Action act = () => ms.Drain();
            act.Should().NotThrow();
            ms.Position.Should().Be(ms.Length);
        }

        [Fact]
        public void Drain_WithNullStream_ShouldThrow()
        {
            Stream? nullStream = null;
            Action act = () => nullStream!.Drain();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Drain_EmptyStream_DoesNotThrow()
        {
            using var ms = new MemoryStream();
            Action act = () => ms.Drain();
            act.Should().NotThrow();
        }

        // ── WriteText ───────────────────────────────────────────────────────

        [Fact]
        public void WriteText_WritesContent()
        {
            using var ms = new MemoryStream();
            ms.WriteText("Hello World");
            ms.Position = 0;
            string result = Encoding.UTF8.GetString(ms.ToArray());
            result.Should().Be("Hello World");
        }

        [Fact]
        public void WriteText_WithEncoding_WritesContent()
        {
            using var ms = new MemoryStream();
            ms.WriteText("Hëllö", Encoding.UTF8);
            ms.Position = 0;
            string result = Encoding.UTF8.GetString(ms.ToArray());
            result.Should().Be("Hëllö");
        }

        [Fact]
        public void WriteText_WithNullStream_ShouldThrow()
        {
            Stream? nullStream = null;
            Action act = () => nullStream!.WriteText("test");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteText_NullText_WritesEmpty()
        {
            using var ms = new MemoryStream();
            ms.WriteText(null!);
            ms.Length.Should().Be(0);
        }

        // ── ReadExact ───────────────────────────────────────────────────────

        [Fact]
        public void ReadExact_ReadsExactCount()
        {
            byte[] data = { 1, 2, 3, 4, 5 };
            using var ms = new MemoryStream(data);
            byte[] result = ms.ReadExact(3);
            result.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        }

        [Fact]
        public void ReadExact_ShortStream_Throws()
        {
            byte[] data = { 1, 2 };
            using var ms = new MemoryStream(data);
            Action act = () => ms.ReadExact(10);
            act.Should().Throw<EndOfStreamException>();
        }

        [Fact]
        public void ReadExact_Zero_ReturnsEmpty()
        {
            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            byte[] result = ms.ReadExact(0);
            result.Should().BeEmpty();
        }

        [Fact]
        public void ReadExact_WithNullStream_ShouldThrow()
        {
            Stream? nullStream = null;
            Action act = () => nullStream!.ReadExact(1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ReadExact_NegativeCount_ShouldThrow()
        {
            using var ms = new MemoryStream();
            Action act = () => ms.ReadExact(-1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // ── TryRead ─────────────────────────────────────────────────────────

        [Fact]
        public void TryRead_ShortStream_ReturnsActualCount()
        {
            byte[] data = { 1, 2 };
            using var ms = new MemoryStream(data);
            byte[] buffer = new byte[10];
            int read = ms.TryRead(buffer, 0, 10);
            read.Should().Be(2);
        }

        [Fact]
        public void TryRead_ExactFit_ReturnsCount()
        {
            byte[] data = { 1, 2, 3 };
            using var ms = new MemoryStream(data);
            byte[] buffer = new byte[3];
            int read = ms.TryRead(buffer, 0, 3);
            read.Should().Be(3);
        }

        [Fact]
        public void TryRead_WithNullStream_ShouldThrow()
        {
            Stream? nullStream = null;
            byte[] buffer = new byte[1];
            Action act = () => nullStream!.TryRead(buffer, 0, 1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryRead_WithNullBuffer_ShouldThrow()
        {
            using var ms = new MemoryStream();
            byte[]? nullBuffer = null;
            Action act = () => ms.TryRead(nullBuffer!, 0, 1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryRead_NegativeOffset_ShouldThrow()
        {
            using var ms = new MemoryStream();
            byte[] buffer = new byte[1];
            Action act = () => ms.TryRead(buffer, -1, 1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TryRead_OffsetAndCountExceedBuffer_ShouldThrow()
        {
            using var ms = new MemoryStream();
            byte[] buffer = new byte[1];
            Action act = () => ms.TryRead(buffer, 0, 2);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
