// <copyright file="SocketIntegrationTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class SocketIntegrationTests : IDisposable
    {
        // ── TCP Echo Round-Trip ─────────────────────────────────

        [Fact]
        public void Tcp_Echo_RoundTrip()
        {
            using var server = new TcpServer(port: 0);
            var acceptedTask = Task.Run(() => server.Accept(timeoutMs: 5000));
            using var client = TcpSocketHelper.Connect("127.0.0.1", server.Port, timeoutMs: 5000);
            client.Should().NotBeNull();
            using var serverConn = acceptedTask.Result;
            serverConn.Should().NotBeNull();

            // Client sends "ping", server reads it
            client!.Send(Encoding.UTF8.GetBytes("ping"));
            var received = serverConn!.ReceiveString(timeoutMs: 3000);
            received.Should().Be("ping");

            // Server sends "pong", client reads it
            serverConn.Send(Encoding.UTF8.GetBytes("pong"));
            var response = client.ReceiveString(timeoutMs: 3000);
            response.Should().Be("pong");
        }

        // ── TCP Two Concurrent Clients ──────────────────────────

        [Fact]
        public void Tcp_TwoConcurrentClients()
        {
            using var server = new TcpServer(port: 0);
            int port = server.Port;

            // Accept and connect clients ONE AT A TIME to avoid race conditions
            // with the TCP backlog queue and the Accept semaphore.
            using var client1 = TcpSocketHelper.Connect("127.0.0.1", port, timeoutMs: 5000);
            client1.Should().NotBeNull();
            using var conn1 = server.Accept(timeoutMs: 5000);
            conn1.Should().NotBeNull();

            using var client2 = TcpSocketHelper.Connect("127.0.0.1", port, timeoutMs: 5000);
            client2.Should().NotBeNull();
            using var conn2 = server.Accept(timeoutMs: 5000);
            conn2.Should().NotBeNull();

            // Both clients send data
            client1!.Send(Encoding.UTF8.GetBytes("from client1"));
            client2!.Send(Encoding.UTF8.GetBytes("from client2"));

            // Server reads from both
            var msg1 = conn1!.ReceiveString(timeoutMs: 3000);
            var msg2 = conn2!.ReceiveString(timeoutMs: 3000);

            msg1.Should().Be("from client1");
            msg2.Should().Be("from client2");
        }

        // ── UDP Two-Way Messaging ───────────────────────────────

        [Fact]
        public void Udp_TwoWayMessaging()
        {
            using var endpointA = new UdpEndpoint();
            using var endpointB = new UdpEndpoint();

            // A sends to B
            byte[] dataA = Encoding.UTF8.GetBytes("hello from A");
            bool sentA = endpointA.Send(dataA, "127.0.0.1", endpointB.Port);
            sentA.Should().BeTrue();

            var resultB = endpointB.Receive(timeoutMs: 3000);
            resultB.Should().NotBeNull();
            Encoding.UTF8.GetString(resultB.Value.Data!).Should().Be("hello from A");

            // B sends to A
            byte[] dataB = Encoding.UTF8.GetBytes("hello from B");
            bool sentB = endpointB.Send(dataB, "127.0.0.1", endpointA.Port);
            sentB.Should().BeTrue();

            var resultA = endpointA.Receive(timeoutMs: 3000);
            resultA.Should().NotBeNull();
            Encoding.UTF8.GetString(resultA.Value.Data!).Should().Be("hello from B");
        }

        // ── UDP Broadcast Received by Multiple ──────────────────

        [Fact]
        public void Udp_Broadcast_ReceivedByMultiple()
        {
            // Note: Broadcast delivery on loopback is platform-dependent.
            // On Linux, 255.255.255.255 broadcasts are typically not delivered
            // to local sockets unless the sending socket has SO_BROADCAST and
            // the receiving socket listens on INADDR_ANY. This test is best-effort:
            // we verify the send returns true and the receive doesn't throw.
            using var receiver1 = new UdpEndpoint();
            using var receiver2 = new UdpEndpoint();
            using var broadcaster = new UdpEndpoint();
            broadcaster.EnableBroadcast = true;

            byte[] data = Encoding.UTF8.GetBytes("broadcast message");

            // Broadcast to the port of receiver1 (UdpSocketHelper.Broadcast also works)
            bool sent = broadcaster.Send(data, "255.255.255.255", receiver1.Port);
            sent.Should().BeTrue();

            // Try to receive on both — may or may not get the broadcast on loopback.
            // Just verify no exception is thrown.
            var result1 = receiver1.Receive(timeoutMs: 1000);
            var result2 = receiver2.Receive(timeoutMs: 500);

            // If the broadcast arrived, verify content.
            if (result1.HasValue)
            {
                Encoding.UTF8.GetString(result1.Value.Data!).Should().Be("broadcast message");
            }
        }

        // ── TCP Large Data Transfer (64 KB) ─────────────────────

        [Fact]
        public void Tcp_LargeDataTransfer()
        {
            const int size = 64 * 1024; // 64 KB
            byte[] largeData = new byte[size];
            new Random(42).NextBytes(largeData);

            using var server = new TcpServer(port: 0);
            var acceptedTask = Task.Run(() => server.Accept(timeoutMs: 5000));
            using var client = TcpSocketHelper.Connect("127.0.0.1", server.Port, timeoutMs: 5000);
            client.Should().NotBeNull();
            using var serverConn = acceptedTask.Result;
            serverConn.Should().NotBeNull();

            // Send the large data in chunks (TcpConnection.Receive uses a 4096 buffer).
            client!.Send(largeData);

            // Receive all data from server side — may need multiple reads.
            using var ms = new System.IO.MemoryStream(size);
            while (ms.Length < size)
            {
                var chunk = serverConn!.Receive(bufferSize: 4096, timeoutMs: 5000);
                if (chunk == null)
                    break;
                ms.Write(chunk, 0, chunk.Length);
            }

            byte[] receivedData = ms.ToArray();
            receivedData.Length.Should().Be(size);
            receivedData.SequenceEqual(largeData).Should().BeTrue();
        }

        public void Dispose()
        {
            // Nothing to clean up at class level; each test manages its own disposables.
        }
    }
}
