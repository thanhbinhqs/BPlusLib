// <copyright file="NetHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Network;

namespace BPlusLib.Foundation.Tests.Network
{
    [Trait("Category", "Network")]
    public sealed class NetHelperTests
    {
        // ── Ping ───────────────────────────────────────────────────────

        [Fact]
        public void Ping_Localhost_ShouldNotThrow()
        {
            PingResult result = null!;
            Action act = () => result = NetHelper.Ping("127.0.0.1", timeoutMs: 3000);
            act.Should().NotThrow();
            result.Should().NotBeNull();
        }

        [Fact]
        public void Ping_Localhost_ReturnsResultWithExpectedStructure()
        {
            var result = NetHelper.Ping("127.0.0.1", timeoutMs: 3000);
            // On some CI environments (Azure VMs), ICMP may be blocked,
            // but the result should still be populated with valid values.
            result.Should().NotBeNull();
            result.RoundtripTimeMs.Should().BeGreaterOrEqualTo(-1);
            result.Status.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Ping_InvalidHost_ShouldNotThrow()
        {
            PingResult result = null!;
            Action act = () => result = NetHelper.Ping("nonexistent-host-xyz-123.local", timeoutMs: 1000);
            act.Should().NotThrow();
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Ping_NullHost_ShouldThrow()
        {
            Action act = () => NetHelper.Ping(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Ping_EmptyHost_ShouldThrow()
        {
            Action act = () => NetHelper.Ping(string.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Ping_WhitespaceHost_ShouldThrow()
        {
            Action act = () => NetHelper.Ping("   ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void PingResult_ToString_OnSuccess_ShouldContainReply()
        {
            var result = NetHelper.Ping("127.0.0.1", timeoutMs: 1000);
            if (result.Success)
            {
                result.ToString().Should().Contain("Ping reply from");
            }
            else
            {
                result.ToString().Should().Contain("Ping failed");
            }
        }

        // ── TCP connections ─────────────────────────────────────────────

        [Fact]
        public void GetTcpConnections_ShouldNotThrow()
        {
            System.Collections.Generic.IReadOnlyList<TcpConnectionInfo> connections = null!;
            Action act = () => connections = NetHelper.GetTcpConnections();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetTcpConnections_ShouldReturnEmptyOrValidList()
        {
            var connections = NetHelper.GetTcpConnections();
            connections.Should().NotBeNull();
            // On Linux, iphlpapi P/Invoke fails → returns empty list
        }

        [Fact]
        public void GetTcpConnections_ShouldReturnEmptyOnLinux()
        {
            var connections = NetHelper.GetTcpConnections();
            connections.Count.Should().Be(0);
        }

        // ── UDP listeners ──────────────────────────────────────────────

        [Fact]
        public void GetUdpListeners_ShouldNotThrow()
        {
            System.Collections.Generic.IReadOnlyList<UdpListenerInfo> listeners = null!;
            Action act = () => listeners = NetHelper.GetUdpListeners();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetUdpListeners_ShouldReturnEmptyOnLinux()
        {
            var listeners = NetHelper.GetUdpListeners();
            listeners.Count.Should().Be(0);
        }

        // ── ARP table ───────────────────────────────────────────────────

        [Fact]
        public void GetArpTable_ShouldNotThrow()
        {
            ArpTableEntry[] entries = null!;
            Action act = () => entries = NetHelper.GetArpTable();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetArpTable_ShouldReturnEmptyOnLinux()
        {
            var entries = NetHelper.GetArpTable();
            entries.Should().NotBeNull();
            entries.Length.Should().Be(0);
        }

        [Fact]
        public void ArpTableEntry_ToString_ShouldContainIp()
        {
            var entry = new ArpTableEntry("192.168.1.1", "AA:BB:CC:DD:EE:FF", "5", ArpEntryState.Reachable);
            entry.ToString().Should().Contain("192.168.1.1");
            entry.ToString().Should().Contain("AA:BB:CC:DD:EE:FF");
        }

        [Fact]
        public void ArpTableEntry_Constructor_ShouldSetProperties()
        {
            var entry = new ArpTableEntry("10.0.0.1", "00:11:22:33:44:55", "3", ArpEntryState.Permanent);
            entry.IpAddress.Should().Be("10.0.0.1");
            entry.MacAddress.Should().Be("00:11:22:33:44:55");
            entry.InterfaceIndex.Should().Be("3");
            entry.State.Should().Be(ArpEntryState.Permanent);
        }

        // ── DNS lookup ──────────────────────────────────────────────────

        [Fact]
        public void LookupDns_Localhost_ShouldResolve()
        {
            string[] addresses = NetHelper.LookupDns("localhost");
            addresses.Should().NotBeNull();
            addresses.Length.Should().BeGreaterThan(0);
            addresses.Should().Contain(a => a == "127.0.0.1" || a == "::1");
        }

        [Fact]
        public void LookupDns_InvalidHost_ShouldThrow()
        {
            Action act = () => NetHelper.LookupDns("nonexistent-host-xyz-123.local");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void LookupDns_NullHost_ShouldThrow()
        {
            Action act = () => NetHelper.LookupDns(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LookupDns_EmptyHost_ShouldThrow()
        {
            Action act = () => NetHelper.LookupDns(string.Empty);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LookupDns_IpAddress_ShouldReturnSelf()
        {
            string[] addresses = NetHelper.LookupDns("8.8.8.8");
            addresses.Should().NotBeNull();
            addresses.Should().Contain("8.8.8.8");
        }

        // ── VPN detection ──────────────────────────────────────────────

        [Fact]
        public void IsVpnConnected_ShouldNotThrow()
        {
            bool result = false;
            Action act = () => result = NetHelper.IsVpnConnected();
            act.Should().NotThrow();
        }

        [Fact]
        public void IsVpnConnected_ShouldReturnFalseOnLinux()
        {
            // On Linux, NetworkInfo.GetAllAdapters() returns empty → no VPN detected
            bool result = NetHelper.IsVpnConnected();
            result.Should().BeFalse();
        }

        // ── Wake-on-LAN ─────────────────────────────────────────────────

        [Fact]
        public void WakeOnLan_InvalidMac_ShouldReturnFalse()
        {
            bool result = NetHelper.WakeOnLan("invalid-mac-address");
            result.Should().BeFalse();
        }

        [Fact]
        public void WakeOnLan_NullMac_ShouldReturnFalse()
        {
            bool result = NetHelper.WakeOnLan(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void WakeOnLan_EmptyMac_ShouldReturnFalse()
        {
            bool result = NetHelper.WakeOnLan(string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void WakeOnLan_WhitespaceMac_ShouldReturnFalse()
        {
            bool result = NetHelper.WakeOnLan("   ");
            result.Should().BeFalse();
        }

        [Fact]
        public void WakeOnLan_TooShortMac_ShouldReturnFalse()
        {
            bool result = NetHelper.WakeOnLan("AA:BB:CC");
            result.Should().BeFalse();
        }

        [Fact]
        public void WakeOnLan_WrongFormatMac_ShouldReturnFalse()
        {
            bool result = NetHelper.WakeOnLan("GG:HH:II:JJ:KK:LL");
            result.Should().BeFalse();
        }

        [Fact]
        public void WakeOnLan_WellFormedMac_ShouldNotThrow()
        {
            // A well-formed MAC may actually attempt to send a UDP packet.
            // On a typical CI environment, this may fail due to no network.
            // We just verify it doesn't throw.
            bool result = false;
            Action act = () => result = NetHelper.WakeOnLan("00:11:22:33:44:55");
            act.Should().NotThrow();
        }
    }
}
