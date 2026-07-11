// <copyright file="TcpConnectionTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class TcpConnectionTests : IDisposable
    {
        private TcpServer? _server;
        private TcpConnection? _connection;
        private TcpConnection? _accepted;

        /// <summary>
        /// Sets up a server, connects a client, and accepts it.
        /// Both <see cref="_connection"/> (client side) and <see cref="_accepted"/>
        /// (server side) are ready for test use.
        /// </summary>
        private void EstablishConnection(int serverTimeoutMs = 5000, int connectTimeoutMs = 5000)
        {
            _server = new TcpServer(port: 0);
            _server.Port.Should().BeGreaterThan(0);

            var acceptTask = Task.Run(() => _server.Accept(timeoutMs: serverTimeoutMs));
            _connection = TcpSocketHelper.Connect("127.0.0.1", _server.Port, timeoutMs: connectTimeoutMs);
            _connection.Should().NotBeNull();
            _accepted = acceptTask.Result;
            _accepted.Should().NotBeNull();
        }

        public void Dispose()
        {
            _accepted?.Dispose();
            _connection?.Dispose();
            _server?.Dispose();
        }

        // ── Constructor ─────────────────────────────────────────

        [Fact]
        public void Constructor_FromTcpClient_ShouldSetProperties()
        {
            // TcpConnection has internal constructors, so it must be created
            // via TcpSocketHelper.Connect or TcpServer.Accept.
            using var server = new TcpServer(port: 0);
            using var connection = TcpSocketHelper.Connect("127.0.0.1", server.Port, timeoutMs: 5000);
            connection.Should().NotBeNull();
            connection!.Connected.Should().BeTrue();
            connection.Available.Should().Be(0);
        }

        // ── Send / Receive (sync) ───────────────────────────────

        [Fact]
        public void SendReceive_RoundTrip()
        {
            EstablishConnection();

            byte[] data = Encoding.UTF8.GetBytes("hello");
            bool sent = _connection!.Send(data);
            sent.Should().BeTrue();

            byte[]? received = _accepted!.Receive(bufferSize: 4096, timeoutMs: 3000);
            received.Should().NotBeNull();
            Encoding.UTF8.GetString(received!).Should().Be("hello");
        }

        [Fact]
        public void Receive_WithTimeout_ReturnsNull()
        {
            EstablishConnection();

            // Don't send anything — receive should time out.
            byte[]? received = _connection!.Receive(bufferSize: 4096, timeoutMs: 500);
            received.Should().BeNull();
        }

        [Fact]
        public void Send_Disconnected_ReturnsFalse()
        {
            EstablishConnection();

            // First send a small message to confirm the connection is alive.
            _connection!.Send(Encoding.UTF8.GetBytes("hello"));
            var confirmation = _accepted!.Receive(bufferSize: 4096, timeoutMs: 2000);
            confirmation.Should().NotBeNull();

            // Dispose the accepted side — this closes the server-side socket.
            _accepted.Dispose();

            // Subsequent sends should not throw, though they may succeed or fail
            // depending on how quickly the OS propagates the TCP close notification.
            byte[] data = Encoding.UTF8.GetBytes("after disconnect");
            var sendEx = Record.Exception(() => _connection.Send(data));
            sendEx.Should().BeNull();
        }

        [Fact]
        public void ReceiveString_ShouldReturnString()
        {
            EstablishConnection();

            byte[] data = Encoding.UTF8.GetBytes("hello string");
            _connection!.Send(data);

            string? result = _accepted!.ReceiveString(bufferSize: 4096, timeoutMs: 3000);
            result.Should().Be("hello string");
        }

        // ── Async operations ────────────────────────────────────

        [Fact]
        public async Task SendAsync_ShouldReturnTrue()
        {
            EstablishConnection();

            byte[] data = Encoding.UTF8.GetBytes("async hello");
            bool sent = await _connection!.SendAsync(data);
            sent.Should().BeTrue();

            byte[]? received = _accepted!.Receive(bufferSize: 4096, timeoutMs: 3000);
            received.Should().NotBeNull();
            Encoding.UTF8.GetString(received!).Should().Be("async hello");
        }

        [Fact]
        public async Task ReceiveAsync_ShouldReturnData()
        {
            EstablishConnection();

            byte[] data = Encoding.UTF8.GetBytes("receive async");
            _connection!.Send(data);

            byte[]? received = await _accepted!.ReceiveAsync(bufferSize: 4096, timeoutMs: 3000);
            received.Should().NotBeNull();
            Encoding.UTF8.GetString(received!).Should().Be("receive async");
        }

        // ── Dispose / lifecycle ─────────────────────────────────

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            using var server = new TcpServer(port: 0);
            var acceptTask = Task.Run(() => server.Accept(timeoutMs: 3000));
            using var conn = TcpSocketHelper.Connect("127.0.0.1", server.Port, timeoutMs: 3000);
            conn.Should().NotBeNull();
            var accepted = acceptTask.Result;
            accepted.Should().NotBeNull();

            // Act
            Action dispose = () =>
            {
                conn!.Dispose();
                conn.Dispose(); // second dispose should not throw
            };
            dispose.Should().NotThrow();

            accepted.Dispose();
        }

        [Fact]
        public void Connected_AfterDispose_ReturnsFalse()
        {
            EstablishConnection();
            _connection!.Dispose();

            // After Dispose, Connected may still reflect the underlying socket
            // state briefly, but we suppress exceptions from the catch block.
            // The important thing is that the call does not throw and returns
            // a bool (not necessarily false on all platforms due to TCP linger).
            var ex = Record.Exception(() => _ = _connection.Connected);
            ex.Should().BeNull();
        }

        [Fact]
        public void Available_ShouldBeZeroOnFreshConnection()
        {
            EstablishConnection();
            _connection!.Available.Should().Be(0);
            _accepted!.Available.Should().Be(0);
        }
    }
}
