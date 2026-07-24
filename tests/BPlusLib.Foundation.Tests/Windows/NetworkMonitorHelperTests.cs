using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class NetworkMonitorHelperTests
    {
        [Fact]
        public void IsNetworkAvailable_DoesNotThrow()
        {
            Action act = () => NetworkMonitor.IsNetworkAvailable();
            act.Should().NotThrow();
        }

        [Fact]
        public void IsNetworkAvailable_ReturnsBool()
        {
            var result = NetworkMonitor.IsNetworkAvailable();
            (result == true || result == false).Should().BeTrue();
        }

        [Fact]
        public void GetActiveInterfaceCount_ReturnsNonNegative()
        {
            NetworkMonitor.GetActiveInterfaceCount().Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void CreateMonitor_Succeeds()
        {
            using var monitor = new NetworkMonitor();
            monitor.Should().NotBeNull();
            monitor.IsMonitoring.Should().BeFalse();
        }

        [Fact]
        public void Monitor_StartStop_Succeeds()
        {
            using var monitor = new NetworkMonitor();
            monitor.Start(500).Should().BeTrue();
            monitor.IsMonitoring.Should().BeTrue();
            monitor.Stop().Should().BeTrue();
            monitor.IsMonitoring.Should().BeFalse();
        }

        [Fact]
        public void Monitor_DoubleDispose_NoException()
        {
            var monitor = new NetworkMonitor();
            monitor.Dispose();
            monitor.Dispose(); // Should not throw
        }
    }
}
