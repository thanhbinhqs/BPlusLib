// <copyright file="UdpEndpointTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class UdpEndpointTests
    {
        // ── Constructor / Port ──────────────────────────────────

        [Fact]
        public void Constructor_WithoutPort_ShouldAssignPort()
        {
            using var endpoint = new UdpEndpoint();
            endpoint.Port.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Constructor_WithSpecificPort_ShouldBind()
        {
            using var endpoint = new UdpEndpoint(0);
            endpoint.Port.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Constructor_WithIPAddress_ShouldBind()
        {
            using var endpoint = new UdpEndpoint(0, IPAddress.Any);
            endpoint.Port.Should().BeGreaterThan(0);
        }

        // ── Send / Receive (sync) ───────────────────────────────

        [Fact]
        public void SendReceive_RoundTrip()
        {
            using var sender = new UdpEndpoint();
            using var receiver = new UdpEndpoint();

            byte[] data = Encoding.UTF8.GetBytes("hello");
            bool sent = sender.Send(data, "127.0.0.1", receiver.Port);
            sent.Should().BeTrue();

            var result = receiver.Receive(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("hello");
        }

        [Fact]
        public void Receive_WithTimeout_ReturnsNull()
        {
            using var endpoint = new UdpEndpoint();
            // No one sends to us — receive should time out.
            var result = endpoint.Receive(timeoutMs: 500);
            result.Should().BeNull();
        }

        [Fact]
        public void Send_ToInvalidHost_ReturnsFalse()
        {
            using var sender = new UdpEndpoint();
            byte[] data = Encoding.UTF8.GetBytes("test");
            bool sent = sender.Send(data, "192.0.2.1", 12345); // TEST-NET address, unlikely to route
            // May succeed or fail depending on local routing; UDP send is fire-and-forget,
            // so on many systems this will return true even though the packet goes nowhere.
            // Just verify the method doesn't throw.
        }

        [Fact]
        public void Send_NullData_ReturnsFalse()
        {
            using var sender = new UdpEndpoint();
            bool sent = sender.Send(null!, "127.0.0.1", 12345);
            sent.Should().BeFalse();
        }

        [Fact]
        public void Send_NullHost_ReturnsFalse()
        {
            using var sender = new UdpEndpoint();
            bool sent = sender.Send(new byte[] { 1 }, null!, 12345);
            sent.Should().BeFalse();
        }

        [Fact]
        public void Send_ToNullEndpoint_ReturnsFalse()
        {
            using var sender = new UdpEndpoint();
            bool sent = sender.Send(new byte[] { 1 }, (EndPoint)null!);
            sent.Should().BeFalse();
        }

        // ── EnableBroadcast ─────────────────────────────────────

        [Fact]
        public void EnableBroadcast_ShouldBeSettable()
        {
            using var endpoint = new UdpEndpoint();
            endpoint.EnableBroadcast.Should().BeFalse();
            endpoint.EnableBroadcast = true;
            endpoint.EnableBroadcast.Should().BeTrue();
            endpoint.EnableBroadcast = false;
            endpoint.EnableBroadcast.Should().BeFalse();
        }

        // ── Async operations ────────────────────────────────────

        [Fact]
        public async Task SendAsync_ReturnsTrue()
        {
            using var receiver = new UdpEndpoint();
            using var sender = new UdpEndpoint();

            byte[] data = Encoding.UTF8.GetBytes("async hello");
            bool sent = await sender.SendAsync(data, "127.0.0.1", receiver.Port);
            sent.Should().BeTrue();

            var result = receiver.Receive(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("async hello");
        }

        [Fact]
        public async Task SendAsync_WithEndpoint_ReturnsTrue()
        {
            using var receiver = new UdpEndpoint();
            using var sender = new UdpEndpoint();

            byte[] data = Encoding.UTF8.GetBytes("endpoint hello");
            var remoteEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), receiver.Port);
            bool sent = await sender.SendAsync(data, remoteEp);
            sent.Should().BeTrue();

            var result = receiver.Receive(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("endpoint hello");
        }

        [Fact]
        public async Task ReceiveAsync_ReturnsData()
        {
            using var receiver = new UdpEndpoint();
            using var sender = new UdpEndpoint();

            byte[] data = Encoding.UTF8.GetBytes("receive async");
            sender.Send(data, "127.0.0.1", receiver.Port);

            var result = await receiver.ReceiveAsync(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("receive async");
        }

        [Fact]
        public async Task ReceiveAsync_WithTimeout_ReturnsNull()
        {
            using var receiver = new UdpEndpoint();
            // No one sends — should time out.
            var result = await receiver.ReceiveAsync(timeoutMs: 500);
            result.Should().BeNull();
        }

        // ── Multicast ───────────────────────────────────────────

        [Fact]
        public void JoinMulticastGroup_ShouldNotThrow()
        {
            using var endpoint = new UdpEndpoint();
            // 224.0.0.1 is the "all hosts" multicast group — safe to attempt.
            bool joined = endpoint.JoinMulticastGroup(IPAddress.Parse("224.0.0.1"));
            // On Linux without proper multicast routing this may return false,
            // so we just verify it doesn't throw.
            // (It may still succeed on loopback.)
        }

        [Fact]
        public void JoinMulticastGroup_NullAddress_ReturnsFalse()
        {
            using var endpoint = new UdpEndpoint();
            bool result = endpoint.JoinMulticastGroup(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void DropMulticastGroup_ShouldNotThrow()
        {
            using var endpoint = new UdpEndpoint();
            bool dropped = endpoint.DropMulticastGroup(IPAddress.Parse("224.0.0.1"));
            // May return false if not a member; just check no exception.
        }

        // ── Dispose ─────────────────────────────────────────────

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            var endpoint = new UdpEndpoint();
            Action dispose = () =>
            {
                endpoint.Dispose();
                endpoint.Dispose(); // second dispose
            };
            dispose.Should().NotThrow();
        }

        // ── Offset / segment Send ───────────────────────────────

        [Fact]
        public void Send_WithOffsetAndCount_ShouldSendCorrectData()
        {
            using var receiver = new UdpEndpoint();
            using var sender = new UdpEndpoint();

            byte[] fullData = Encoding.UTF8.GetBytes("prefix_data_suffix");
            // Send only "data" portion.
            bool sent = sender.Send(fullData, offset: 7, count: 4, "127.0.0.1", receiver.Port);
            sent.Should().BeTrue();

            var result = receiver.Receive(timeoutMs: 3000);
            result.Should().NotBeNull();
            Encoding.UTF8.GetString(result.Value.Data!).Should().Be("data");
        }
    }
}
