using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class AutoStartHelperTests
    {
        [Fact]
        public void Enable_ValidName_DoesNotThrow()
        {
            var act = () => AutoStartHelper.Enable("BPlusLib_TestApp", "/usr/bin/test");
            act.Should().NotThrow();
        }

        [Fact]
        public void Disable_ValidName_DoesNotThrow()
        {
            var act = () => AutoStartHelper.Disable("BPlusLib_TestApp_NonExistent");
            act.Should().NotThrow();
        }

        [Fact]
        public void IsEnabled_NonExistent_ReturnsFalse()
        {
            AutoStartHelper.IsEnabled("BPlusLib_NonExistent_App_12345").Should().BeFalse();
        }

        [Fact]
        public void GetCommand_NonExistent_ReturnsNull()
        {
            AutoStartHelper.GetCommand("BPlusLib_NonExistent_App_12345").Should().BeNull();
        }

        [Fact]
        public void RemoveFromStartup_EmptyName_ReturnsFalse()
        {
            AutoStartHelper.RemoveFromStartup("").Should().BeFalse();
        }
    }
}
