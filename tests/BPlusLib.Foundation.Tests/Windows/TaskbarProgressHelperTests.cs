using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class TaskbarProgressHelperTests
    {
        [Fact]
        public void SetProgress_ZeroHandle_ReturnsFalse()
        {
            TaskbarProgressHelper.SetProgress(IntPtr.Zero, 50, 100).Should().BeFalse();
        }

        [Fact]
        public void SetState_ZeroHandle_ReturnsFalse()
        {
            TaskbarProgressHelper.SetState(IntPtr.Zero, TaskbarProgressState.Normal).Should().BeFalse();
        }

        [Fact]
        public void ClearProgress_ZeroHandle_ReturnsFalse()
        {
            TaskbarProgressHelper.ClearProgress(IntPtr.Zero).Should().BeFalse();
        }

        [Fact]
        public void SetProgress_DoesNotThrow()
        {
            // Just verify no exception on invalid handle
            Action act = () => TaskbarProgressHelper.SetProgress(IntPtr.Zero, 0, 0);
            act.Should().NotThrow();
        }

        [Fact]
        public void TaskbarProgressState_Values_AreCorrect()
        {
            TaskbarProgressState.None.Should().Be((TaskbarProgressState)0);
            TaskbarProgressState.Normal.Should().Be((TaskbarProgressState)2);
        }
    }
}
