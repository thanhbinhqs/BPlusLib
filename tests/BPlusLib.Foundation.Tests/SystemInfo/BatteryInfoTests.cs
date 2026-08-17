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
            ((object)battery.IsPresent).Should().BeOfType<bool>();
        }

        [Fact]
        public void IsCharging_ShouldBeBool()
        {
            var battery = BatteryInfo.Current;
            ((object)battery.IsCharging).Should().BeOfType<bool>();
        }

        [Fact]
        public void StatusFlags_ShouldBeDefinedFlags()
        {
            var battery = BatteryInfo.Current;
            var validMask = BatteryStatusFlags.None |
                            BatteryStatusFlags.Discharging |
                            BatteryStatusFlags.AcOffline |
                            BatteryStatusFlags.Charging |
                            BatteryStatusFlags.LowBattery |
                            BatteryStatusFlags.CriticalBattery;
            (battery.StatusFlags & ~validMask).Should().Be(BatteryStatusFlags.None);
        }

        [Fact]
        public void BatteryLifeSeconds_ShouldBeNullOrNonNegative()
        {
            var battery = BatteryInfo.Current;
            battery.BatteryLifeSeconds.Should().Match(v => !v.HasValue || v.Value >= 0);
        }

        [Fact]
        public void BatteryFullLifeSeconds_ShouldBeNullOrNonNegative()
        {
            var battery = BatteryInfo.Current;
            battery.BatteryFullLifeSeconds.Should().Match(v => !v.HasValue || v.Value >= 0);
        }

        [Fact]
        public void VoltageMillivolts_ShouldBeNullOrNonNegative()
        {
            var battery = BatteryInfo.Current;
            battery.VoltageMillivolts.Should().Match(v => !v.HasValue || v.Value >= 0);
        }

        [Fact]
        public void Chemistry_ShouldBeNullOrNonEmpty()
        {
            var battery = BatteryInfo.Current;
            if (battery.Chemistry != null)
                battery.Chemistry.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void DesignCapacityMW_ShouldBeNullOrNonNegative()
        {
            var battery = BatteryInfo.Current;
            battery.DesignCapacityMW.Should().Match(v => !v.HasValue || v.Value >= 0);
        }

        [Fact]
        public void CurrentCapacityMW_ShouldBeNullOrNonNegative()
        {
            var battery = BatteryInfo.Current;
            battery.CurrentCapacityMW.Should().Match(v => !v.HasValue || v.Value >= 0);
        }
    }
}
