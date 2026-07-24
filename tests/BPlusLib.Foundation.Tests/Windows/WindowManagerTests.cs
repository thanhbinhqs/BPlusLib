using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class WindowManagerTests
    {
        [Fact]
        public void Restore_NullKey_ReturnsNull()
        {
            var result = WindowManager.Restore(IntPtr.Zero, null);
            result.Should().BeNull();
        }

        [Fact]
        public void Delete_EmptyKey_ReturnsFalse()
        {
            WindowManager.Delete("").Should().BeFalse();
        }

        [Fact]
        public void Save_IntPtrZero_ReturnsFalse()
        {
            var settings = new WindowSettings { X = 100, Y = 100, Width = 800, Height = 600 };
            WindowManager.Save(IntPtr.Zero, settings).Should().BeFalse();
        }

        [Fact]
        public void Restore_IntPtrZero_ReturnsNull()
        {
            var result = WindowManager.Restore(IntPtr.Zero);
            result.Should().BeNull();
        }

        [Fact]
        public void WindowSettings_DefaultValues_AreCorrect()
        {
            var s = new WindowSettings();
            s.X.Should().Be(0);
            s.Width.Should().Be(0);
            s.IsMaximized.Should().BeFalse();
        }

        [Fact]
        public void WindowManager_DoesNotThrow()
        {
            Action act = () =>
            {
                WindowManager.Delete("test_nonexistent_key");
                var r = WindowManager.Restore(IntPtr.Zero, "test_nonexistent_key");
            };
            act.Should().NotThrow();
        }
    }
}
