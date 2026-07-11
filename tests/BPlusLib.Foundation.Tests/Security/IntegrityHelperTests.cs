// <copyright file="IntegrityHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Security;

namespace BPlusLib.Foundation.Tests.Security
{
    [Trait("Category", "Security")]
    public sealed class IntegrityHelperTests
    {
        private static int CurrentProcessId => System.Diagnostics.Process.GetCurrentProcess().Id;

        // ── CurrentProcessIntegrityLevel ─────────────────────────────────────

        [Fact]
        public void CurrentProcessIntegrityLevel_ShouldNotThrow()
        {
            IntegrityLevel level = IntegrityLevel.Unknown;
            Action act = () => level = IntegrityHelper.CurrentProcessIntegrityLevel;
            act.Should().NotThrow();
        }

        [Fact]
        public void CurrentProcessIntegrityLevel_ShouldReturnValue()
        {
            IntegrityLevel level = IntegrityHelper.CurrentProcessIntegrityLevel;
            // On Linux returns Unknown; on Windows returns the actual level
            level.Should().BeOneOf(
                IntegrityLevel.Unknown,
                IntegrityLevel.Untrusted,
                IntegrityLevel.Low,
                IntegrityLevel.Medium,
                IntegrityLevel.High,
                IntegrityLevel.System,
                IntegrityLevel.MediumPlus,
                IntegrityLevel.ProtectedProcess);
        }

        // ── GetProcessIntegrityLevel ─────────────────────────────────────────

        [Fact]
        public void GetProcessIntegrityLevel_InvalidPid_ReturnsUnknown()
        {
            IntegrityLevel level = IntegrityHelper.GetProcessIntegrityLevel(int.MaxValue);
            level.Should().Be(IntegrityLevel.Unknown);
        }

        [Fact]
        public void GetProcessIntegrityLevel_NegativePid_ReturnsUnknown()
        {
            IntegrityLevel level = IntegrityHelper.GetProcessIntegrityLevel(-1);
            level.Should().Be(IntegrityLevel.Unknown);
        }

        [Fact]
        public void GetProcessIntegrityLevel_CurrentProcess_ShouldNotThrow()
        {
            IntegrityLevel level = IntegrityLevel.Unknown;
            Action act = () => level = IntegrityHelper.GetProcessIntegrityLevel(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── SetProcessIntegrityLevel ─────────────────────────────────────────

        [Fact]
        public void SetProcessIntegrityLevel_Unknown_ReturnsFalse()
        {
            bool result = IntegrityHelper.SetProcessIntegrityLevel(IntegrityLevel.Unknown);
            result.Should().BeFalse();
        }

        [Fact]
        public void SetProcessIntegrityLevel_ShouldNotThrow()
        {
            Action act = () => IntegrityHelper.SetProcessIntegrityLevel(IntegrityLevel.Low);
            act.Should().NotThrow();
        }

        // ── IntegrityLevel enum values ──────────────────────────────────────

        [Fact]
        public void IntegrityLevel_Values_ShouldBeCorrect()
        {
            ((int)IntegrityLevel.Untrusted).Should().Be(0);
            ((int)IntegrityLevel.Low).Should().Be(0x1000);
            ((int)IntegrityLevel.Medium).Should().Be(0x2000);
            ((int)IntegrityLevel.High).Should().Be(0x3000);
            ((int)IntegrityLevel.System).Should().Be(0x4000);
            ((int)IntegrityLevel.ProtectedProcess).Should().Be(0x5000);
            ((int)IntegrityLevel.Unknown).Should().Be(-1);
        }
    }
}
