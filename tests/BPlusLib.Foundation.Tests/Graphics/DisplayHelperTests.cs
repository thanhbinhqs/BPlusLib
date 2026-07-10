// <copyright file="DisplayHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Graphics;

namespace BPlusLib.Foundation.Tests.Graphics
{
    [Trait("Category", "Graphics")]
    public sealed class DisplayHelperTests
    {
        // ── GetDpiForWindow ──────────────────────────────────────────────

        [Fact]
        public void GetDpiForWindow_WithZeroHandle_ReturnsDefault()
        {
            // On Linux, GetDpiForWindow returns 96.
            // On Windows with zero handle, it may also return 96 or GDI caps.
            int dpi = DisplayHelper.GetDpiForWindow(IntPtr.Zero);

            dpi.Should().BeGreaterThan(0);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dpi.Should().Be(96, "because the Linux fallback returns 96");
            }
        }

        // ── GetDpiForMonitor ─────────────────────────────────────────────

        [Fact]
        public void GetDpiForMonitor_WithZeroHandle_ReturnsDefault()
        {
            var (dpiX, dpiY) = DisplayHelper.GetDpiForMonitor(IntPtr.Zero);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dpiX.Should().Be(96);
                dpiY.Should().Be(96);
            }
            else
            {
                // On Windows with a null handle, it may still return (96, 96)
                // due to the guard clause at the top of the method.
                dpiX.Should().Be(96);
                dpiY.Should().Be(96);
            }
        }

        // ── SetScreenResolution ───────────────────────────────────────────

        [Fact]
        public void SetScreenResolution_ShouldReturnFalse()
        {
            // Not running as admin / not on Windows -> always returns false.
            bool result = DisplayHelper.SetScreenResolution(1920, 1080);

            result.Should().BeFalse();
        }

        [Fact]
        public void SetScreenResolution_WithInvalidArgs_ReturnsFalse()
        {
            DisplayHelper.SetScreenResolution(0, 1080).Should().BeFalse();
            DisplayHelper.SetScreenResolution(1920, 0).Should().BeFalse();
            DisplayHelper.SetScreenResolution(1920, 1080, bitsPerPixel: 0).Should().BeFalse();
            DisplayHelper.SetScreenResolution(-1, -1).Should().BeFalse();
        }

        // ── IsHighContrastMode ────────────────────────────────────────────

        [Fact]
        public void IsHighContrastMode_ShouldReturnBool()
        {
            // On Linux, returns false (SystemParametersInfoW not available).
            bool isHC = DisplayHelper.IsHighContrastMode();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                isHC.Should().BeFalse("because SystemParametersInfoW is Windows-only");
            }
        }

        // ── GetColorDepth ─────────────────────────────────────────────────

        [Fact]
        public void GetColorDepth_ShouldReturnPositiveOrZero()
        {
            // On Linux returns 0; on Windows returns typically 32.
            int depth = DisplayHelper.GetColorDepth();

            depth.Should().BeGreaterThanOrEqualTo(0);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                depth.Should().Be(0, "because GetDeviceCaps is Windows-only");
            }
        }

        // ── GetScreenScaleFactor ──────────────────────────────────────────

        [Fact]
        public void GetScreenScaleFactor_ShouldReturnPositive()
        {
            // On Linux, returns 1.0. On Windows, returns something >= 1.0.
            double factor = DisplayHelper.GetScreenScaleFactor();

            factor.Should().BeGreaterThan(0);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                factor.Should().Be(1.0, "because the Linux fallback returns 1.0");
            }
        }
    }
}
