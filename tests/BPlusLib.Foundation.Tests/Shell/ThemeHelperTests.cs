// <copyright file="ThemeHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using BPlusLib.Foundation.Shell;

namespace BPlusLib.Foundation.Tests.Shell
{
    [Trait("Category", "Shell")]
    public sealed class ThemeHelperTests
    {
        // ── IsLightTheme ───────────────────────────────────────────────

        [SkippableFact]
        public void IsLightTheme_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool result = ThemeHelper.IsLightTheme();
        }

        // ── IsAppsLightTheme ───────────────────────────────────────────

        [SkippableFact]
        public void IsAppsLightTheme_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool result = ThemeHelper.IsAppsLightTheme();
        }

        // ── IsSystemLightTheme ──────────────────────────────────────────

        [SkippableFact]
        public void IsSystemLightTheme_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool result = ThemeHelper.IsSystemLightTheme();
        }

        // ── GetAccentColor ──────────────────────────────────────────────

        [SkippableFact]
        public void GetAccentColor_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            uint color = ThemeHelper.GetAccentColor();
        }

        // ── IsCompositionEnabled ────────────────────────────────────────

        [SkippableFact]
        public void IsCompositionEnabled_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool result = ThemeHelper.IsCompositionEnabled();
        }

        // ── SetWindowDarkMode (cross-platform) ──────────────────────────

        [Fact]
        public void SetWindowDarkMode_NullWindow_ReturnsFalse()
        {
            bool result = ThemeHelper.SetWindowDarkMode(IntPtr.Zero, true);

            Assert.False(result);
        }
    }
}
