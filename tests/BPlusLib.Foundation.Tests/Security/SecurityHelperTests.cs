// <copyright file="SecurityHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Security;

namespace BPlusLib.Foundation.Tests.Security
{
    [Trait("Category", "Security")]
    public sealed class SecurityHelperTests
    {
        private static int CurrentProcessId => System.Diagnostics.Process.GetCurrentProcess().Id;
        private const int InvalidPid = int.MaxValue;

        // ── IsCurrentProcessElevated ────────────────────────────────────────

        [Fact]
        public void IsCurrentProcessElevated_ShouldNotThrow()
        {
            bool elevated = false;
            Action act = () => elevated = SecurityHelper.IsCurrentProcessElevated();
            act.Should().NotThrow();
        }

        [Fact]
        public void IsCurrentProcessElevated_ShouldReturnBool()
        {
            bool elevated = SecurityHelper.IsCurrentProcessElevated();
            // On Linux returns false
            elevated.Should().BeFalse();
        }

        // ── IsProcessElevated ───────────────────────────────────────────────

        [Fact]
        public void IsProcessElevated_InvalidPid_ReturnsFalse()
        {
            bool result = SecurityHelper.IsProcessElevated(InvalidPid);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsProcessElevated_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.IsProcessElevated(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── IsProcess64Bit ──────────────────────────────────────────────────

        [Fact]
        public void IsProcess64Bit_InvalidPid_ReturnsFalse()
        {
            bool result = SecurityHelper.IsProcess64Bit(InvalidPid);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsProcess64Bit_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.IsProcess64Bit(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── GetProcessOwner ─────────────────────────────────────────────────

        [Fact]
        public void GetProcessOwner_InvalidPid_ReturnsNull()
        {
            string? owner = SecurityHelper.GetProcessOwner(InvalidPid);
            owner.Should().BeNull();
        }

        [Fact]
        public void GetProcessOwner_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.GetProcessOwner(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── IsInteractiveUser ───────────────────────────────────────────────

        [Fact]
        public void IsInteractiveUser_InvalidPid_ReturnsFalse()
        {
            bool result = SecurityHelper.IsInteractiveUser(InvalidPid);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsInteractiveUser_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.IsInteractiveUser(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── GetProcessIntegrityLevelString ──────────────────────────────────

        [Fact]
        public void GetProcessIntegrityLevelString_InvalidPid_ReturnsUnknown()
        {
            string? level = SecurityHelper.GetProcessIntegrityLevelString(InvalidPid);
            level.Should().Be("Unknown");
        }

        [Fact]
        public void GetProcessIntegrityLevelString_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.GetProcessIntegrityLevelString(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── CanAccessProcess ────────────────────────────────────────────────

        [Fact]
        public void CanAccessProcess_InvalidPid_ReturnsFalse()
        {
            bool result = SecurityHelper.CanAccessProcess(InvalidPid, TokenAccessLevels.Query);
            result.Should().BeFalse();
        }

        [Fact]
        public void CanAccessProcess_ZeroPid_ReturnsFalse()
        {
            // PID 0 (Idle) typically can't be accessed
            bool result = SecurityHelper.CanAccessProcess(0, TokenAccessLevels.Query);
            result.Should().BeFalse();
        }

        // ── IsProcessInAdminGroup ───────────────────────────────────────────

        [Fact]
        public void IsProcessInAdminGroup_InvalidPid_ReturnsFalse()
        {
            bool result = SecurityHelper.IsProcessInAdminGroup(InvalidPid);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsProcessInAdminGroup_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.IsProcessInAdminGroup(CurrentProcessId);
            act.Should().NotThrow();
        }

        // ── GetProcessSidHistory ────────────────────────────────────────────

        [Fact]
        public void GetProcessSidHistory_InvalidPid_ReturnsEmpty()
        {
            IReadOnlyList<string> history = SecurityHelper.GetProcessSidHistory(InvalidPid);
            history.Should().BeEmpty();
        }

        [Fact]
        public void GetProcessSidHistory_CurrentProcess_ShouldNotThrow()
        {
            Action act = () => SecurityHelper.GetProcessSidHistory(CurrentProcessId);
            act.Should().NotThrow();
        }
    }
}
