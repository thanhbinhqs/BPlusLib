#if FEATURE_WINDOW_MODULE

using System;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.Window;

namespace BPlusLib.Foundation.Tests.Window
{
    [Trait("Category", "Window")]
    public sealed class DragMoveHelperTests
    {
        [Fact]
        public void BeginDrag_ZeroHandle_ReturnsFalse()
        {
            DragMoveHelper.BeginDrag(IntPtr.Zero).Should().BeFalse();
        }

        [Fact]
        public void BeginDrag_ZeroHandle_DoesNotThrow()
        {
            Action act = () => DragMoveHelper.BeginDrag(IntPtr.Zero);
            act.Should().NotThrow();
        }
    }
}

#endif
