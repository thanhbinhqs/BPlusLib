// <copyright file="TcpServerTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class TcpServerTests
    {
        // ── Constructor / Port ──────────────────────────────────

        [Fact]
        public void Constructor_WithPort0_ShouldAssignPort()
        {
            using var server = new TcpServer(port: 0);
            server.Port.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Constructor_NullAddress_UsesAny()
        {
            using var server = new TcpServer(port: 0, address: null);
            server.Port.Should().BeGreaterThan(0);
            server.IsRunning.Should().BeTrue();
        }

        // ── Accept ──────────────────────────────────────────────

        [Fact]
        public void Accept_WithClient_ReturnsConnection()
        {
            using var server = new TcpServer(port: 0);
            var acceptTask = Task.Run(() => server.Accept(timeoutMs: 5000));

            using var client = new TcpClient();
            client.Connect("127.0.0.1", server.Port);

            using var connection = acceptTask.Result;
            connection.Should().NotBeNull();
            connection!.Connected.Should().BeTrue();
        }

        [Fact]
        public void Accept_Timeout_ReturnsNull()
        {
            using var server = new TcpServer(port: 0);
            // No client connects — accept should time out.
            var result = server.Accept(timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public async Task AcceptAsync_ShouldReturnConnection()
        {
            using var server = new TcpServer(port: 0);
            var acceptTask = server.AcceptAsync();

            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", server.Port);

            using var connection = await acceptTask;
            connection.Should().NotBeNull();
            connection!.Connected.Should().BeTrue();
        }

        // ── Stop / lifecycle ────────────────────────────────────

        [Fact]
        public void Stop_ShouldStopListening()
        {
            using var server = new TcpServer(port: 0);
            server.IsRunning.Should().BeTrue();

            server.Stop();
            server.IsRunning.Should().BeFalse();

            // After stop, accept should return null immediately.
            var result = server.Accept(timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            var server = new TcpServer(port: 0);
            Action dispose = () =>
            {
                server.Dispose();
                server.Dispose(); // second dispose should not throw
            };
            dispose.Should().NotThrow();
        }

        // ── IsRunning ──────────────────────────────────────────

        [Fact]
        public void IsRunning_AfterStart_ShouldBeTrue()
        {
            using var server = new TcpServer(port: 0);
            server.IsRunning.Should().BeTrue();
        }

        [Fact]
        public void IsRunning_AfterStop_ShouldBeFalse()
        {
            using var server = new TcpServer(port: 0);
            server.Stop();
            server.IsRunning.Should().BeFalse();
        }

        // ── Accept after dispose ────────────────────────────────

        [Fact]
        public void Accept_AfterDispose_ThrowsObjectDisposed()
        {
            var server = new TcpServer(port: 0);
            server.Dispose();

            // After Dispose, the SemaphoreSlim is disposed, so Accept throws.
            Action act = () => server.Accept(timeoutMs: 500);
            act.Should().Throw<ObjectDisposedException>();
        }
    }
}
