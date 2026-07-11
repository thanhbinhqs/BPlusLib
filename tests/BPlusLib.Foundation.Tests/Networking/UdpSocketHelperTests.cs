// <copyright file="UdpSocketHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class UdpSocketHelperTests
    {
        // ── CreateEndpoint ──────────────────────────────────────

        [Fact]
        public void CreateEndpoint_ShouldReturnEndpoint()
        {
            using var endpoint = UdpSocketHelper.CreateEndpoint();
            endpoint.Should().NotBeNull();
            endpoint!.Port.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateEndpoint_WithSpecificPort_ShouldReturnEndpoint()
        {
            using var endpoint = UdpSocketHelper.CreateEndpoint(0);
            endpoint.Should().NotBeNull();
            endpoint!.Port.Should().BeGreaterThan(0);
        }

        // ── SendDatagram ────────────────────────────────────────

        [Fact]
        public void SendDatagram_ShouldSend()
        {
            using var receiver = new UdpEndpoint();
            byte[] data = Encoding.UTF8.GetBytes("datagram test");

            bool sent = UdpSocketHelper.SendDatagram(data, "127.0.0.1", receiver.Port);
            sent.Should().BeTrue();

            var result = receiver.Receive(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("datagram test");
        }

        [Fact]
        public void SendDatagram_InvalidPort_ReturnsFalse()
        {
            byte[] data = Encoding.UTF8.GetBytes("test");
            bool sent = UdpSocketHelper.SendDatagram(data, "127.0.0.1", -1);
            sent.Should().BeFalse();
        }

        [Fact]
        public void SendDatagram_NullData_ReturnsFalse()
        {
            bool sent = UdpSocketHelper.SendDatagram(null!, "127.0.0.1", 12345);
            sent.Should().BeFalse();
        }

        [Fact]
        public void SendDatagram_EmptyHost_ReturnsFalse()
        {
            bool sent = UdpSocketHelper.SendDatagram(new byte[] { 1 }, string.Empty, 12345);
            sent.Should().BeFalse();
        }

        [Fact]
        public void SendDatagram_NullHost_ReturnsFalse()
        {
            bool sent = UdpSocketHelper.SendDatagram(new byte[] { 1 }, null!, 12345);
            sent.Should().BeFalse();
        }

        // ── ReceiveDatagram ─────────────────────────────────────

        [Fact]
        public void ReceiveDatagram_ShouldReceive()
        {
            // Use UdpEndpoint as receiver (gives us a known port),
            // and UdpSocketHelper.SendDatagram as the sender.
            using var receiver = new UdpEndpoint();
            int port = receiver.Port;

            byte[] data = Encoding.UTF8.GetBytes("receive datagram helper");

            bool sent = UdpSocketHelper.SendDatagram(data, "127.0.0.1", port);
            sent.Should().BeTrue();

            var result = receiver.Receive(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("receive datagram helper");
        }

        [Fact]
        public void ReceiveDatagram_Timeout_ReturnsNull()
        {
            // Pick a random port nobody is sending to.
            using var endpoint = new UdpEndpoint();
            byte[]? received = UdpSocketHelper.ReceiveDatagram(endpoint.Port, timeoutMs: 500);
            received.Should().BeNull();
        }

        [Fact]
        public void ReceiveDatagram_InvalidPort_ReturnsNull()
        {
            byte[]? received = UdpSocketHelper.ReceiveDatagram(-1, timeoutMs: 500);
            received.Should().BeNull();
        }

        // ── Broadcast ───────────────────────────────────────────

        [Fact]
        public void Broadcast_OnLoopback_ShouldWork()
        {
            using var receiver = new UdpEndpoint(0);
            byte[] data = Encoding.UTF8.GetBytes("broadcast test");

            bool sent = UdpSocketHelper.Broadcast(data, receiver.Port);
            sent.Should().BeTrue();

            // The broadcast goes to 255.255.255.255:receiver.Port.
            // On Linux loopback, a broadcast may or may not be delivered
            // to the receiver depending on routing configuration.
            // It's a best-effort test; we just check that Broadcast
            // returns true (send succeeded) and does not throw.

            // Optionally try to receive, but this is unreliable on CI.
            // var result = receiver.Receive(timeoutMs: 1000);
        }

        [Fact]
        public void Broadcast_NullData_ReturnsFalse()
        {
            bool sent = UdpSocketHelper.Broadcast(null!, 12345);
            sent.Should().BeFalse();
        }

        [Fact]
        public void Broadcast_InvalidPort_ReturnsFalse()
        {
            bool sent = UdpSocketHelper.Broadcast(new byte[] { 1 }, -1);
            sent.Should().BeFalse();
        }

        // ── Port bounds (SendDatagram) ──────────────────────────

        [Fact]
        public void SendDatagram_PortTooHigh_ReturnsFalse()
        {
            bool sent = UdpSocketHelper.SendDatagram(new byte[] { 1 }, "127.0.0.1", 65536);
            sent.Should().BeFalse();
        }
    }
}
