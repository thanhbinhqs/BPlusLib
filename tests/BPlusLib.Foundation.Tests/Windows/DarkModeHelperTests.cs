using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class DarkModeHelperTests
    {
        [Fact]
        public void IsDarkModeAvailable_DoesNotThrow()
        {
            Action act = () => DarkModeHelper.IsDarkModeAvailable();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetDarkBackColor_ReturnsNonEmpty()
        {
            DarkModeHelper.GetDarkBackColor().Should().NotBe(default);
        }

        [Fact]
        public void GetDarkForeColor_ReturnsNonEmpty()
        {
            DarkModeHelper.GetDarkForeColor().Should().NotBe(default);
        }

#if FEATURE_WINDOW_MODULE
        [Fact]
        public void ApplyDarkMode_NullControl_ReturnsFalse()
        {
            DarkModeHelper.ApplyDarkMode(null!).Should().BeFalse();
        }

        [Fact]
        public void RemoveDarkMode_NullControl_ReturnsFalse()
        {
            DarkModeHelper.RemoveDarkMode(null!).Should().BeFalse();
        }
#endif
    }
}
