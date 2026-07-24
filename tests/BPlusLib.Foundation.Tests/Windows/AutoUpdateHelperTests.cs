using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class AutoUpdateHelperTests
    {
        [Fact]
        public async Task CheckForUpdate_NullOwner_ReturnsNull()
        {
            var result = await AutoUpdateHelper.CheckForUpdateAsync(null!, "repo");
            result.Should().BeNull();
        }

        [Fact]
        public async Task CheckForUpdate_EmptyRepo_ReturnsNull()
        {
            var result = await AutoUpdateHelper.CheckForUpdateAsync("owner", "");
            result.Should().BeNull();
        }

        [Fact]
        public async Task CheckForUpdate_NonExistentRepo_ReturnsNull()
        {
            var result = await AutoUpdateHelper.CheckForUpdateAsync("nonexistent-owner-xyz-123", "nonexistent-repo-xyz");
            result.Should().BeNull();
        }

        [Fact]
        public void IsUpdateAvailable_SameVersion_ReturnsFalse()
        {
            AutoUpdateHelper.IsUpdateAvailable("1.0.0", "1.0.0").Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_NewerVersion_ReturnsTrue()
        {
            AutoUpdateHelper.IsUpdateAvailable("1.0.0", "2.0.0").Should().BeTrue();
        }

        [Fact]
        public void IsUpdateAvailable_OlderVersion_ReturnsFalse()
        {
            AutoUpdateHelper.IsUpdateAvailable("2.0.0", "1.0.0").Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_EmptyStrings_ReturnsFalse()
        {
            AutoUpdateHelper.IsUpdateAvailable("", "").Should().BeFalse();
        }

        [Fact]
        public void LaunchInstaller_NonExistent_ReturnsFalse()
        {
            AutoUpdateHelper.LaunchInstaller("/nonexistent/file.exe").Should().BeFalse();
        }
    }
}
