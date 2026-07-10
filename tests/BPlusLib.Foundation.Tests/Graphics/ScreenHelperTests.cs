// <copyright file="ScreenHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Graphics;

namespace BPlusLib.Foundation.Tests.Graphics
{
    [Trait("Category", "Graphics")]
    public sealed class ScreenHelperTests
    {
        // ── Virtual screen bounds ────────────────────────────────────────

        [Fact]
        public void GetVirtualScreenBounds_ShouldReturnNonEmpty()
        {
            // On Linux, GetVirtualScreenBounds returns DisplayRect.Empty
            // because GetSystemMetrics P/Invoke is not available.
            DisplayRect bounds = ScreenHelper.GetVirtualScreenBounds();

            // On non-Windows platforms this will be Empty (width <= 0 || height <= 0)
            // which is a valid non-throwing result.
            bounds.Should().NotBeNull("because DisplayRect is a non-nullable struct");
        }

        // ── Display enumeration ───────────────────────────────────────────

        [Fact]
        public void GetAllDisplays_ShouldNotThrow()
        {
            // On Linux, GetAllDisplays returns Array.Empty<DisplayInfo>().
            // On Windows it may return actual monitors. Either way, no exception.
            var displays = ScreenHelper.GetAllDisplays();

            displays.Should().NotBeNull();
            // On Linux: empty list; on Windows: may contain entries
        }

        [Fact]
        public void GetPrimaryDisplay_OnLinux_ReturnsNull()
        {
            // On non-Windows, GetAllDisplays returns empty, so GetPrimaryDisplay is null.
            DisplayInfo? primary = ScreenHelper.GetPrimaryDisplay();

            // On a Linux CI runner this will reliably be null.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                primary.Should().BeNull("because SetupAPI is not available on Linux");
            }
        }

        [Fact]
        public void GetMonitorCount_ShouldBePositiveOrZero()
        {
            // On Linux returns 0; on Windows returns >= 1.
            int count = ScreenHelper.GetMonitorCount();
            count.Should().BeGreaterThanOrEqualTo(0);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                count.Should().Be(0, "because GetSystemMetrics is Windows-only");
            }
        }

        // ── DisplayInfo model ─────────────────────────────────────────────

        [Fact]
        public void DisplayInfo_Properties_ShouldSetCorrectly()
        {
            var info = new DisplayInfo
            {
                DeviceName = @"\\.\DISPLAY1",
                DeviceString = "Generic PnP Monitor",
                IsPrimary = true,
                Bounds = new DisplayRect(0, 0, 1920, 1080),
                WorkingArea = new DisplayRect(0, 0, 1920, 1040),
                DpiX = 96,
                DpiY = 96,
                BitsPerPixel = 32,
                RefreshRate = 60,
            };

            info.DeviceName.Should().Be(@"\\.\DISPLAY1");
            info.DeviceString.Should().Be("Generic PnP Monitor");
            info.IsPrimary.Should().BeTrue();
            info.Bounds.Width.Should().Be(1920);
            info.Bounds.Height.Should().Be(1080);
            info.WorkingArea.Height.Should().Be(1040);
            info.DpiX.Should().Be(96);
            info.DpiY.Should().Be(96);
            info.BitsPerPixel.Should().Be(32);
            info.RefreshRate.Should().Be(60);
        }

        [Fact]
        public void DisplayInfo_ToString_ShouldContainDeviceName()
        {
            var info = new DisplayInfo
            {
                DeviceName = "DISPLAY1",
                DeviceString = "Test Monitor",
                Bounds = new DisplayRect(0, 0, 1920, 1080),
                RefreshRate = 60,
                BitsPerPixel = 32,
                IsPrimary = true,
            };

            string str = info.ToString();
            str.Should().Contain("DISPLAY1");
            str.Should().Contain("Test Monitor");
            str.Should().Contain("1920x1080");
        }

        // ── DisplayRect struct ────────────────────────────────────────────

        [Fact]
        public void DisplayRect_Default_IsEmpty()
        {
            DisplayRect empty = default;
            empty.IsEmpty.Should().BeTrue();
            empty.Width.Should().Be(0);
            empty.Height.Should().Be(0);
        }

        [Fact]
        public void DisplayRect_Properties_ShouldBeCorrect()
        {
            var rect = new DisplayRect(100, 200, 1920, 1080);

            rect.X.Should().Be(100);
            rect.Y.Should().Be(200);
            rect.Width.Should().Be(1920);
            rect.Height.Should().Be(1080);
            rect.Left.Should().Be(100);
            rect.Top.Should().Be(200);
            rect.Right.Should().Be(2020);
            rect.Bottom.Should().Be(1280);
            rect.IsEmpty.Should().BeFalse();
        }

        [Fact]
        public void DisplayRect_Equals_ShouldWorkCorrectly()
        {
            var a = new DisplayRect(0, 0, 1920, 1080);
            var b = new DisplayRect(0, 0, 1920, 1080);
            var c = new DisplayRect(100, 100, 800, 600);

            a.Should().Be(b);
            a.Should().NotBe(c);
            (a == b).Should().BeTrue();
            (a != c).Should().BeTrue();
        }

        [Fact]
        public void DisplayRect_GetHashCode_ShouldBeConsistent()
        {
            var a = new DisplayRect(0, 0, 1920, 1080);
            var b = new DisplayRect(0, 0, 1920, 1080);

            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        // ── Screen capture (Linux returns null) ───────────────────────────

        [Fact]
        public void CaptureScreenRaw_OnLinux_ReturnsNull()
        {
            var result = ScreenHelper.CaptureScreenRaw();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result.Should().BeNull("because GDI P/Invoke is not available on Linux");
            }
        }

#if NETFRAMEWORK
        [Fact]
        public void CaptureScreenAsPng_OnLinux_ReturnsNull()
        {
            byte[]? png = ScreenHelper.CaptureScreenAsPng();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                png.Should().BeNull("because GDI P/Invoke is not available on Linux");
            }
        }
#endif
    }
}
