using System;
using System.Drawing;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class CustomWindowHelperTests
    {
        [Fact]
        public void HandleNCHitTest_ZeroHandle_ReturnsHTCLIENT()
        {
            CustomWindowHelper.HandleNCHitTest(IntPtr.Zero, 0, 0).Should().Be(1); // HTCLIENT = 1
        }

        [Fact]
        public void HandleNCCalcSize_DoesNotThrow()
        {
            var rect = new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 };
            Action act = () => CustomWindowHelper.HandleNCCalcSize(ref rect, true);
            act.Should().NotThrow();
        }

        [Fact]
        public void HandleNCCalcSize_RemoveBorder_AdjustsRect()
        {
            var rect = new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 };
            CustomWindowHelper.HandleNCCalcSize(ref rect, true);
            rect.Left.Should().Be(1);
            rect.Top.Should().Be(1);
            rect.Right.Should().Be(799);
            rect.Bottom.Should().Be(599);
        }

        [Fact]
        public void HandleNCCalcSize_NoRemoveBorder_DoesNotAdjust()
        {
            var rect = new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 };
            CustomWindowHelper.HandleNCCalcSize(ref rect, false);
            rect.Left.Should().Be(0);
            rect.Top.Should().Be(0);
            rect.Right.Should().Be(800);
            rect.Bottom.Should().Be(600);
        }

        [Fact]
        public void ApplyDwmFrame_ZeroHandle_ReturnsFalse()
        {
            CustomWindowHelper.ApplyDwmFrame(IntPtr.Zero).Should().BeFalse();
        }

        [Fact]
        public void DisableDwmNcRendering_ZeroHandle_ReturnsFalse()
        {
            CustomWindowHelper.DisableDwmNcRendering(IntPtr.Zero).Should().BeFalse();
        }

        [Fact]
        public void EnableCustomChrome_ZeroHandle_ReturnsFalse()
        {
            CustomWindowHelper.EnableCustomChrome(IntPtr.Zero).Should().BeFalse();
        }

        [Fact]
        public void ScreenToClient_ZeroHandle_ReturnsOriginalPoint()
        {
            var result = CustomWindowHelper.ScreenToClient(IntPtr.Zero, 100, 200);
            result.X.Should().Be(100);
            result.Y.Should().Be(200);
        }
    }
}
