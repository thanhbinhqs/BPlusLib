// <copyright file="PowerHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.Power;

namespace BPlusLib.Foundation.Tests.Power
{
    [Trait("Category", "Power")]
    public sealed class PowerHelperTests
    {
        // ── GetPowerStatus ─────────────────────────────────────────────

        [SkippableFact]
        public void GetPowerStatus_ReturnsStatus()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var status = PowerHelper.GetPowerStatus();

            status.Should().NotBeNull();
            status.AclineStatus.Should().BeOneOf(
                AclineStatus.Offline, AclineStatus.Online, AclineStatus.Unknown);
        }

        // ── IsOnBattery ────────────────────────────────────────────────

        [SkippableFact]
        public void IsOnBattery_ReturnsBool()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            // Just verifying the call completes without throwing
            bool result = PowerHelper.IsOnBattery();
        }

        // ── GetBatteryChargePercent ─────────────────────────────────────

        [SkippableFact]
        public void GetBatteryChargePercent_ReturnsValue()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            int percent = PowerHelper.GetBatteryChargePercent();

            // -1 means unknown/no battery, otherwise 0-100
            percent.Should().BeInRange(-1, 100);
        }

        // ── LockWorkstation ─────────────────────────────────────────────

        [SkippableFact]
        public void LockWorkstation_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            // We can't actually verify the workstation was locked,
            // but we can verify the call doesn't throw.
            // It may fail (return false) if there's no console session.
            bool result = PowerHelper.LockWorkstation();
            // Not asserting the value — just checking no exception
        }

        // ── PreventSleep ────────────────────────────────────────────────

        [SkippableFact]
        public void PreventSleep_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            uint prev = PowerHelper.PreventSleep(true);
            // Restore previous state
            PowerHelper.PreventSleep(false);
            // prev may be 0 on failure, that's fine
        }
    }
}
