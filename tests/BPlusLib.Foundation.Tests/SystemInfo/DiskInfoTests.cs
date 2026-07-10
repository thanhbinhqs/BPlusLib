// <copyright file="DiskInfoTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Tests.SystemInfo
{
    [Trait("Category", "SystemInfo")]
    public sealed class DiskInfoTests
    {
        [Fact]
        public void GetAllDrives_ShouldNotThrow()
        {
            System.Collections.Generic.IReadOnlyList<DriveInfoEx> drives = null!;
            Action act = () => drives = DiskInfo.GetAllDrives();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetAllDrives_ShouldReturnList()
        {
            var drives = DiskInfo.GetAllDrives();
            drives.Should().NotBeNull();
        }

        [Fact]
        public void GetDrive_WithValidLetter_ShouldNotThrow()
        {
            // Try "/" (root on Linux) which is always valid
            DriveInfoEx? drive = null;
            Action act = () => drive = DiskInfo.GetDrive("/");
            act.Should().NotThrow();
        }

        [Fact]
        public void GetDrive_WithValidWindowsLetter_ShouldNotThrow()
        {
            // "C:" — on Linux this will return null (doesn't exist), but shouldn't throw
            Action act = () => DiskInfo.GetDrive("C:");
            act.Should().NotThrow();
        }

        [Fact]
        public void GetDrive_WithNull_ShouldReturnNull()
        {
            var drive = DiskInfo.GetDrive(null!);
            drive.Should().BeNull();
        }

        [Fact]
        public void GetDrive_WithEmpty_ShouldReturnNull()
        {
            var drive = DiskInfo.GetDrive(string.Empty);
            drive.Should().BeNull();
        }

        [Fact]
        public void GetDrive_WithInvalidName_ShouldReturnNull()
        {
            var drive = DiskInfo.GetDrive("invalid");
            drive.Should().BeNull();
        }

        [Fact]
        public void GetDrive_WithInvalidName2_ShouldReturnNull()
        {
            // Drive names with wrong format
            var drive = DiskInfo.GetDrive("AB");
            drive.Should().BeNull();
        }

        [Fact]
        public void AtLeastOneDriveShouldHaveNonNegativeTotalBytes()
        {
            var drives = DiskInfo.GetAllDrives();
            drives.Should().NotBeNull();

            if (drives.Count > 0)
            {
                drives.Any(d => d.TotalBytes >= 0).Should().BeTrue();
            }
        }

        [Fact]
        public void DriveInfoEx_Properties_ShouldNotThrow()
        {
            var drives = DiskInfo.GetAllDrives();
            foreach (var drive in drives)
            {
                drive.Name.Should().NotBeNull();
                drive.VolumeLabel.Should().NotBeNull();
                drive.FileSystem.Should().NotBeNull();
                drive.TotalBytes.Should().BeGreaterOrEqualTo(0);
                drive.AvailableBytes.Should().BeGreaterOrEqualTo(0);
                drive.UsedBytes.Should().BeGreaterOrEqualTo(0);
                drive.UsagePercent.Should().BeInRange(0.0, 100.0);
            }
        }
    }
}
