// <copyright file="OperatingSystemInfoTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Tests.SystemInfo
{
    [Trait("Category", "SystemInfo")]
    public sealed class OperatingSystemInfoTests
    {
        [Fact]
        public void Current_ShouldNotThrow()
        {
            OperatingSystemInfo instance = null!;
            Action act = () => instance = OperatingSystemInfo.Current;
            act.Should().NotThrow();
        }

        [Fact]
        public void Current_Properties_ShouldReturnDefaultsOnNonWindows()
        {
            var os = OperatingSystemInfo.Current;

            // On Linux, P/Invoke and registry calls fail gracefully, falling back
            // to Environment.OSVersion which returns the real kernel version.
            os.Name.Should().Be(string.Empty);
            os.Edition.Should().Be(string.Empty);
            os.BuildNumber.Should().BeGreaterOrEqualTo(0);
            os.ServicePack.Should().Be(string.Empty);
        }

        [Fact]
        public void Current_Properties_ShouldNotBeNull()
        {
            var os = OperatingSystemInfo.Current;

            os.Name.Should().NotBeNull();
            os.Version.Should().NotBeNull();
            os.Edition.Should().NotBeNull();
            os.ServicePack.Should().NotBeNull();
            os.Architecture.Should().NotBeNull();
        }

        [Fact]
        public void Current_BuildNumber_ShouldBeNonNegative()
        {
            var os = OperatingSystemInfo.Current;
            os.BuildNumber.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void Current_IsServer_ShouldBeBool()
        {
            var os = OperatingSystemInfo.Current;
            os.IsServer.Should().BeFalse();
        }

        [Fact]
        public void Current_Is64Bit_ShouldBeBool()
        {
            var os = OperatingSystemInfo.Current;
            // On Linux, falls back to IntPtr.Size check
            os.Is64Bit.Should().Be(IntPtr.Size == 8);
        }

        [Fact]
        public void Current_Architecture_ShouldNotBeNullOrEmpty()
        {
            var os = OperatingSystemInfo.Current;
            os.Architecture.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Current_SuiteMask_ShouldBeNonNegative()
        {
            var os = OperatingSystemInfo.Current;
            os.SuiteMask.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void Current_ProductType_ShouldBeByte()
        {
            var os = OperatingSystemInfo.Current;
            os.ProductType.Should().BeInRange(0, 255);
        }

        [Fact]
        public void Current_CSDVersion_ShouldNotBeNull()
        {
            var os = OperatingSystemInfo.Current;
            os.CSDVersion.Should().NotBeNull();
        }
    }
}
