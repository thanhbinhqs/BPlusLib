// <copyright file="EnvironmentHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Tests.SystemInfo
{
    [Trait("Category", "SystemInfo")]
    public sealed class EnvironmentHelperTests
    {
        // ── GetMachineName ─────────────────────────────────────────────

        [Fact]
        public void GetMachineName_ReturnsNonEmpty()
        {
            string name = EnvironmentHelper.GetMachineName();
            name.Should().NotBeNullOrEmpty();
        }

        // ── ExpandString ───────────────────────────────────────────────

        [Fact]
        public void ExpandString_Works()
        {
            // %HOME% should exist on Linux/macOS; %TEMP% on Windows
            string? expanded = EnvironmentHelper.ExpandString("%HOME%");
            if (expanded == null || expanded == "%HOME%")
            {
                expanded = EnvironmentHelper.ExpandString("%TEMP%");
            }

            if (expanded != null)
            {
                expanded.Should().NotBe("%HOME%");
                expanded.Should().NotBe("%TEMP%");
            }

            // null input returns null
            EnvironmentHelper.ExpandString(null).Should().BeNull();
        }

        // ── IsDomainJoined ─────────────────────────────────────────────

        [Fact]
        public void IsDomainJoined_DoesNotThrow()
        {
            // Should not throw on any platform; returns false on non-Windows
            bool result = EnvironmentHelper.IsDomainJoined();
            result.GetType().Should().Be(typeof(bool));
        }

        // ── AddToUserPath ──────────────────────────────────────────────

        [Fact]
        public void AddToUserPath_DoesNotThrow()
        {
            // Should not throw; may not persist on non-Windows but shouldn't crash
            bool result = EnvironmentHelper.AddToUserPath(Environment.CurrentDirectory);
            // Just verify no exception was thrown; result depends on platform
            result.GetType().Should().Be(typeof(bool));
        }

        // ── RemoveFromUserPath ─────────────────────────────────────────

        [Fact]
        public void RemoveFromUserPath_DoesNotThrow()
        {
            bool result = EnvironmentHelper.RemoveFromUserPath(Environment.CurrentDirectory);
            result.GetType().Should().Be(typeof(bool));
        }

        // ── GetUserPathDirectories ─────────────────────────────────────

        [Fact]
        public void GetUserPathDirectories_DoesNotThrow()
        {
            var dirs = EnvironmentHelper.GetUserPathDirectories();
            dirs.Should().NotBeNull();
            dirs.Should().BeAssignableTo<List<string>>();
        }

        // ── GetVariable / SetVariable / DeleteVariable ────────────────

        [Fact]
        public void GetVariable_NullInput_ReturnsNull()
        {
            EnvironmentHelper.GetVariable(null!).Should().BeNull();
        }

        [Fact]
        public void SetVariable_GetVariable_Roundtrips()
        {
            string key = "BPLUS_TEST_VAR_" + Guid.NewGuid().ToString("N");
            try
            {
                EnvironmentHelper.SetVariable(key, "test_value").Should().BeTrue();
                EnvironmentHelper.GetVariable(key).Should().Be("test_value");
            }
            finally
            {
                EnvironmentHelper.DeleteVariable(key);
            }
        }
    }
}
