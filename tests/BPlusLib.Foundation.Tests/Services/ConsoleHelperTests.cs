// <copyright file="ConsoleHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Services;

namespace BPlusLib.Foundation.Tests.Services
{
    [Trait("Category", "Services")]
    public sealed class ConsoleHelperTests
    {
        [SkippableFact]
        public void GetWindowHandle_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var hwnd = ConsoleHelper.GetWindowHandle();
            // May be zero if no console attached (e.g., test runner)
            // Just verify no exception
        }

        [SkippableFact]
        public void HasConsole_ReturnsBool()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var has = ConsoleHelper.HasConsole;
            // Just verify it's a bool without exception
            has.GetType().Should().Be(typeof(bool));
        }

        [SkippableFact]
        public void SetAndGetTitle_Roundtrips()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            if (!ConsoleHelper.HasConsole) return;
            var original = ConsoleHelper.GetTitle();
            ConsoleHelper.SetTitle("BPlusLibTestTitle").Should().BeTrue();
            var retrieved = ConsoleHelper.GetTitle();
            retrieved.Should().Be("BPlusLibTestTitle");
            // Restore
            if (original is not null)
                ConsoleHelper.SetTitle(original);
        }

        [SkippableFact]
        public void GetTitle_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var title = ConsoleHelper.GetTitle();
            // May be null if no console — no exception expected
        }

        [SkippableFact]
        public void EnableQuickEdit_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            if (!ConsoleHelper.HasConsole) return;
            var result = ConsoleHelper.EnableQuickEdit(false);
            result.GetType().Should().Be(typeof(bool));
            result = ConsoleHelper.EnableQuickEdit(true);
            result.GetType().Should().Be(typeof(bool));
        }

        [SkippableFact]
        public void SetTextColor_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            if (!ConsoleHelper.HasConsole) return;
            ConsoleHelper.SetTextColor(ConsoleHelper.ConsoleColor.Green).Should().BeTrue();
        }
    }
}
