using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Input;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Tests.Input
{
    [Trait("Category", "Input")]
    public sealed class InputHelperTests
    {
        [SkippableFact]
        public void SendKeyPress_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            // May succeed or fail depending on UIPI — just verify no exception
            _ = InputHelper.SendKeyPress(VirtualKeyCode.A);
        }

        [SkippableFact]
        public void LeftClick_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = InputHelper.LeftClick();
        }

        [SkippableFact]
        public void SendText_Empty_ReturnsFalse()
        {
            InputHelper.SendText(null!).Should().BeFalse();
            InputHelper.SendText(string.Empty).Should().BeFalse();
        }

        [SkippableFact]
        public void SendText_NonEmpty_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = InputHelper.SendText("Hello");
        }

        [SkippableFact]
        public void MoveMouse_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = InputHelper.MoveMouse(100, 100, relative: true);
        }

        [SkippableFact]
        public void ScrollWheel_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = InputHelper.ScrollWheel(120);
        }
    }
}
