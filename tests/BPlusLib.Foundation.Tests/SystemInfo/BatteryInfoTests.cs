// <copyright file="BatteryInfoTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Tests.SystemInfo
{
    [Trait("Category", "SystemInfo")]
    public sealed class BatteryInfoTests
    {
        [Fact]
        public void Current_ShouldNotThrow()
        {
            BatteryInfo instance = null!;
            Action act = () => instance = BatteryInfo.Current;
            act.Should().NotThrow();
        }

        [Fact]
        public void EstimatedChargePercent_ShouldBeBetween0And100()
        {
            var battery = BatteryInfo.Current;
            battery.EstimatedChargePercent.Should().BeInRange(0, 100);
        }

        [Fact]
        public void IsPresent_ShouldBeBool()
        {
            var battery = BatteryInfo.Current;
            // On Linux, GetSystemPowerStatus fails → IsPresent = false
            battery.IsPresent.Should().BeFalse();
        }

        [Fact]
        public void IsCharging_ShouldBeBool()
        {
            var battery = BatteryInfo.Current;
            battery.IsCharging.Should().BeFalse();
        }

        [Fact]
        public void StatusFlags_ShouldBeNoneOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.StatusFlags.Should().Be(BatteryStatusFlags.None);
        }

        [Fact]
        public void BatteryLifeSeconds_ShouldBeNullOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.BatteryLifeSeconds.Should().BeNull();
        }

        [Fact]
        public void BatteryFullLifeSeconds_ShouldBeNullOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.BatteryFullLifeSeconds.Should().BeNull();
        }

        [Fact]
        public void VoltageMillivolts_ShouldBeNullOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.VoltageMillivolts.Should().BeNull();
        }

        [Fact]
        public void Chemistry_ShouldBeNullOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.Chemistry.Should().BeNull();
        }

        [Fact]
        public void DesignCapacityMW_ShouldBeNullOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.DesignCapacityMW.Should().BeNull();
        }

        [Fact]
        public void CurrentCapacityMW_ShouldBeNullOnNonWindows()
        {
            var battery = BatteryInfo.Current;
            battery.CurrentCapacityMW.Should().BeNull();
        }
    }
}
