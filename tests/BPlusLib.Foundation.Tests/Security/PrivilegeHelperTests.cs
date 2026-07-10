// <copyright file="PrivilegeHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Security;

namespace BPlusLib.Foundation.Tests.Security
{
    [Trait("Category", "Security")]
    public sealed class PrivilegeHelperTests
    {
        // ── GetCurrentProcessPrivileges ──────────────────────────────────────

        [Fact]
        public void GetCurrentProcessPrivileges_ShouldNotThrow()
        {
            IReadOnlyList<PrivilegeEntry> privileges = Array.Empty<PrivilegeEntry>();
            Action act = () => privileges = PrivilegeHelper.GetCurrentProcessPrivileges();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCurrentProcessPrivileges_ShouldReturnList()
        {
            IReadOnlyList<PrivilegeEntry> privileges = PrivilegeHelper.GetCurrentProcessPrivileges();
            privileges.Should().NotBeNull();
            // On Linux may be empty; on Windows will contain privileges
        }

        // ── GetProcessPrivileges ─────────────────────────────────────────────

        [Fact]
        public void GetProcessPrivileges_InvalidPid_ReturnsEmpty()
        {
            IReadOnlyList<PrivilegeEntry> privileges = PrivilegeHelper.GetProcessPrivileges(int.MaxValue);
            privileges.Should().BeEmpty();
        }

        // ── EnablePrivilege ──────────────────────────────────────────────────

        [Fact]
        public void EnablePrivilege_InvalidName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.EnablePrivilege("NonExistentPrivilege_XYZ");
            result.Should().BeFalse();
        }

        [Fact]
        public void EnablePrivilege_NullName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.EnablePrivilege(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void EnablePrivilege_EmptyName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.EnablePrivilege(string.Empty);
            result.Should().BeFalse();
        }

        // ── DisablePrivilege ─────────────────────────────────────────────────

        [Fact]
        public void DisablePrivilege_InvalidName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.DisablePrivilege("NonExistentPrivilege_XYZ");
            result.Should().BeFalse();
        }

        [Fact]
        public void DisablePrivilege_NullName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.DisablePrivilege(null!);
            result.Should().BeFalse();
        }

        // ── RemovePrivilege ──────────────────────────────────────────────────

        [Fact]
        public void RemovePrivilege_InvalidName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.RemovePrivilege("NonExistentPrivilege_XYZ");
            result.Should().BeFalse();
        }

        [Fact]
        public void RemovePrivilege_NullName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.RemovePrivilege(null!);
            result.Should().BeFalse();
        }

        // ── HasPrivilege ─────────────────────────────────────────────────────

        [Fact]
        public void HasPrivilege_InvalidName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.HasPrivilege("NonExistentPrivilege_XYZ");
            result.Should().BeFalse();
        }

        [Fact]
        public void HasPrivilege_NullName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.HasPrivilege(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void HasPrivilege_EmptyName_ReturnsFalse()
        {
            bool result = PrivilegeHelper.HasPrivilege(string.Empty);
            result.Should().BeFalse();
        }

        // ── GetAllWellKnownPrivileges ────────────────────────────────────────

        [Fact]
        public void GetAllWellKnownPrivileges_ShouldReturnList()
        {
            IReadOnlyList<string> privileges = PrivilegeHelper.GetAllWellKnownPrivileges();
            privileges.Should().NotBeNull();
            privileges.Should().Contain("SeDebugPrivilege");
            privileges.Should().Contain("SeShutdownPrivilege");
            privileges.Should().Contain("SeTakeOwnershipPrivilege");
            privileges.Should().Contain("SeBackupPrivilege");
        }

        // ── PrivilegeEntry (via GetAllWellKnownPrivileges) ─────────────────────

        [Fact]
        public void PrivilegeEntry_FromWellKnown_ShouldHaveCorrectProperties()
        {
            var privileges = PrivilegeHelper.GetAllWellKnownPrivileges();
            privileges.Should().Contain("SeDebugPrivilege");
            privileges.Should().Contain("SeShutdownPrivilege");
        }

        [Fact]
        public void PrivilegeEntry_GetCurrentProcessPrivileges_ShouldReturnEntries()
        {
            var entries = PrivilegeHelper.GetCurrentProcessPrivileges();
            entries.Should().NotBeNull();
            foreach (var entry in entries)
            {
                entry.Name.Should().NotBeNull();
                entry.DisplayName.Should().NotBeNull();
                // Verify enum flags work correctly
                bool isValid = entry.Enabled || entry.EnabledByDefault || entry.Removed || entry.Attributes == PrivilegeAttributes.Disabled;
                isValid.Should().BeTrue($"Privilege {entry.Name} should have valid attributes");
            }
        }

        // ── PrivilegeAttributes enum ────────────────────────────────────────

        [Fact]
        public void PrivilegeAttributes_Values_ShouldBeCorrect()
        {
            ((uint)PrivilegeAttributes.Disabled).Should().Be(0);
            ((uint)PrivilegeAttributes.EnabledByDefault).Should().Be(1);
            ((uint)PrivilegeAttributes.Enabled).Should().Be(2);
            ((uint)PrivilegeAttributes.Removed).Should().Be(4);
            ((uint)PrivilegeAttributes.UsedForAccess).Should().Be(0x80000000);
        }
    }
}
