// <copyright file="AppUpdaterTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class AppUpdaterTests
    {
        // =================================================================
        // Model tests
        // =================================================================

        [Fact]
        public void AppUpdateInfo_DefaultValues()
        {
            var info = new AppUpdateInfo();
            info.Version.Should().BeEmpty();
            info.FileUrl.Should().BeEmpty();
            info.ReleaseNotes.Should().BeNull();
            info.FileSize.Should().Be(0);
            info.Sha256.Should().BeNull();
        }

        [Fact]
        public void UpdateResult_DefaultValues()
        {
            var result = new UpdateResult();
            result.Success.Should().BeFalse();
            result.UpdateInfo.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void UpdateResult_WithValues()
        {
            var result = new UpdateResult
            {
                Success = true,
                UpdateInfo = new AppUpdateInfo { Version = "1.0.0" },
                ErrorMessage = null,
            };
            result.Success.Should().BeTrue();
            result.UpdateInfo!.Version.Should().Be("1.0.0");
        }

        // =================================================================
        // IsUpdateAvailable tests
        // =================================================================

        [Fact]
        public void IsUpdateAvailable_SameVersion_ReturnsFalse()
        {
            AppUpdater.IsUpdateAvailable("1.0.0", "1.0.0").Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_NewerVersion_ReturnsTrue()
        {
            AppUpdater.IsUpdateAvailable("1.0.0", "2.0.0").Should().BeTrue();
        }

        [Fact]
        public void IsUpdateAvailable_OlderVersion_ReturnsFalse()
        {
            AppUpdater.IsUpdateAvailable("2.0.0", "1.0.0").Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_VPrefix_Handled()
        {
            AppUpdater.IsUpdateAvailable("v1.0.0", "v2.0.0").Should().BeTrue();
        }

        [Fact]
        public void IsUpdateAvailable_EmptyStrings_ReturnsFalse()
        {
            AppUpdater.IsUpdateAvailable("", "").Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_NullStrings_ReturnsFalse()
        {
            AppUpdater.IsUpdateAvailable(null!, null!).Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_MinorVersion_Works()
        {
            AppUpdater.IsUpdateAvailable("1.0.0", "1.1.0").Should().BeTrue();
            AppUpdater.IsUpdateAvailable("1.1.0", "1.0.0").Should().BeFalse();
        }

        [Fact]
        public void IsUpdateAvailable_PatchVersion_Works()
        {
            AppUpdater.IsUpdateAvailable("1.0.0", "1.0.1").Should().BeTrue();
            AppUpdater.IsUpdateAvailable("1.0.1", "1.0.0").Should().BeFalse();
        }

        // =================================================================
        // CheckForUpdate tests
        // =================================================================

        [Fact]
        public async Task CheckForUpdate_NullUrl_ReturnsNull()
        {
            var result = await AppUpdater.CheckForUpdateAsync(null!);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CheckForUpdate_EmptyUrl_ReturnsNull()
        {
            var result = await AppUpdater.CheckForUpdateAsync("");
            result.Should().BeNull();
        }

        [Fact]
        public async Task CheckForUpdate_InvalidUrl_ReturnsNull()
        {
            var result = await AppUpdater.CheckForUpdateAsync("https://nonexistent.example.com/api");
            result.Should().BeNull();
        }

        // =================================================================
        // Download tests
        // =================================================================

        [Fact]
        public async Task Download_NullUrl_ReturnsFalse()
        {
            var result = await AppUpdater.DownloadAsync(null!, "/tmp/test.zip");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Download_EmptyTarget_ReturnsFalse()
        {
            var result = await AppUpdater.DownloadAsync("https://example.com/file.zip", "");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Download_InvalidUrl_ReturnsFalse()
        {
            var result = await AppUpdater.DownloadAsync(
                "https://nonexistent.example.com/file.zip",
                "/tmp/test_download.zip");
            result.Should().BeFalse();
        }

        // =================================================================
        // Extract tests
        // =================================================================

        [Fact]
        public void Extract_NonExistentFile_ReturnsFalse()
        {
            AppUpdater.Extract("/nonexistent/file.zip", "/tmp/output").Should().BeFalse();
        }

        [Fact]
        public void Extract_EmptyPath_ReturnsFalse()
        {
            AppUpdater.Extract("", "").Should().BeFalse();
        }

        [Fact]
        public void Extract_NullPath_ReturnsFalse()
        {
            AppUpdater.Extract(null!, null!).Should().BeFalse();
        }

        // =================================================================
        // Cleanup tests
        // =================================================================

        [Fact]
        public void Cleanup_DoesNotThrow()
        {
            Action act = () => AppUpdater.Cleanup();
            act.Should().NotThrow();
        }
    }
}
