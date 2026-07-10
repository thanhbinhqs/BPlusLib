// <copyright file="BiosInfoTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class BiosInfoTests
    {
        [Fact]
        public void Current_ShouldNotThrow()
        {
            BiosInfo instance = null!;
            Action act = () => instance = BiosInfo.Current;
            act.Should().NotThrow();
        }

        [Fact]
        public void IsUefi_ShouldBeBool()
        {
            var bios = BiosInfo.Current;
            // On Linux, registry checks fail → returns false
            bios.IsUefi.Should().BeFalse();
        }

        [Fact]
        public void Manufacturer_ShouldBeNullOnNonWindows()
        {
            var bios = BiosInfo.Current;
            bios.Manufacturer.Should().BeNull();
        }

        [Fact]
        public void Name_ShouldBeNullOnNonWindows()
        {
            var bios = BiosInfo.Current;
            bios.Name.Should().BeNull();
        }

        [Fact]
        public void Version_ShouldBeNullOnNonWindows()
        {
            var bios = BiosInfo.Current;
            bios.Version.Should().BeNull();
        }

        [Fact]
        public void SerialNumber_ShouldBeNullOnNonWindows()
        {
            var bios = BiosInfo.Current;
            bios.SerialNumber.Should().BeNull();
        }

        [Fact]
        public void ReleaseDate_ShouldBeNullOnNonWindows()
        {
            var bios = BiosInfo.Current;
            bios.ReleaseDate.Should().BeNull();
        }

        [Fact]
        public void SmbiosVersion_ShouldBeNullOnNonWindows()
        {
            var bios = BiosInfo.Current;
            bios.SmbiosVersion.Should().BeNull();
        }
    }
}
