// <copyright file="TcpSocketHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class TcpSocketHelperTests
    {
        // ── Connect (sync) ──────────────────────────────────────

        [Fact]
        public void Connect_ToServer_ReturnsConnection()
        {
            using var server = new TcpServer(port: 0);
            var result = TcpSocketHelper.Connect("127.0.0.1", server.Port, timeoutMs: 5000);
            result.Should().NotBeNull();
            result!.Dispose();
        }

        [Fact]
        public void Connect_ToInvalidPort_ReturnsNull()
        {
            // Port 1 is highly unlikely to be open on any system, and we use
            // a very short timeout so the test completes quickly.
            var result = TcpSocketHelper.Connect("127.0.0.1", 1, timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public void Connect_WithNegativeTimeout_ShouldNotThrow()
        {
            using var server = new TcpServer(port: 0);
            // Negative timeout is an edge case; should not throw.
            var result = TcpSocketHelper.Connect("127.0.0.1", server.Port, timeoutMs: -1);
            // May succeed or fail depending on timing; just verify no exception.
        }

        [Fact]
        public void Connect_WithNullHost_ReturnsNull()
        {
            var result = TcpSocketHelper.Connect(null!, 12345, timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public void Connect_WithEmptyHost_ReturnsNull()
        {
            var result = TcpSocketHelper.Connect(string.Empty, 12345, timeoutMs: 500);
            result.Should().BeNull();
        }

        // ── ConnectAsync ────────────────────────────────────────

        [Fact]
        public async Task ConnectAsync_ShouldConnect()
        {
            using var server = new TcpServer(port: 0);
            var result = await TcpSocketHelper.ConnectAsync("127.0.0.1", server.Port, timeoutMs: 5000);
            result.Should().NotBeNull();
            result!.Dispose();
        }

        [Fact]
        public async Task ConnectAsync_WithNullHost_ReturnsNull()
        {
            var result = await TcpSocketHelper.ConnectAsync(null!, 12345, timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public async Task ConnectAsync_WithInvalidPort_ReturnsNull()
        {
            var result = await TcpSocketHelper.ConnectAsync("127.0.0.1", 1, timeoutMs: 500);
            result.Should().BeNull();
        }

        // ── StartServer (sync) ──────────────────────────────────

        [Fact]
        public void StartServer_ShouldReturnRunningServer()
        {
            using var server = TcpSocketHelper.StartServer(0);
            server.Should().NotBeNull();
            server!.IsRunning.Should().BeTrue();
            server.Port.Should().BeGreaterThan(0);
        }

        [Fact]
        public void StartServer_WithPort0_ShouldWork()
        {
            using var server = TcpSocketHelper.StartServer(0);
            server.Should().NotBeNull();
            server!.Port.Should().BeGreaterThan(0);
        }

        // ── StartServerAsync ────────────────────────────────────

        [Fact]
        public async Task StartServerAsync_ShouldStart()
        {
            using var server = await TcpSocketHelper.StartServerAsync(0);
            server.Should().NotBeNull();
            server!.IsRunning.Should().BeTrue();
            server.Port.Should().BeGreaterThan(0);
        }

        // ── Invalid port bounds ─────────────────────────────────

        [Fact]
        public void Connect_NegativePort_ReturnsNull()
        {
            var result = TcpSocketHelper.Connect("127.0.0.1", -1, timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public void Connect_OverMaxPort_ReturnsNull()
        {
            var result = TcpSocketHelper.Connect("127.0.0.1", 65536, timeoutMs: 500);
            result.Should().BeNull();
        }
    }
}
