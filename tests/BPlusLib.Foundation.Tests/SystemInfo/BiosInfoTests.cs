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
            ((object)bios.IsUefi).Should().BeOfType<bool>();
        }

        [Fact]
        public void Manufacturer_ShouldBeNullOrNonEmpty()
        {
            var bios = BiosInfo.Current;
            if (bios.Manufacturer != null)
                bios.Manufacturer.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Name_ShouldBeNullOrNonEmpty()
        {
            var bios = BiosInfo.Current;
            if (bios.Name != null)
                bios.Name.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Version_ShouldBeNullOrNonEmpty()
        {
            var bios = BiosInfo.Current;
            if (bios.Version != null)
                bios.Version.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void SerialNumber_ShouldBeNullOrNonEmpty()
        {
            var bios = BiosInfo.Current;
            if (bios.SerialNumber != null)
                bios.SerialNumber.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ReleaseDate_ShouldBeReasonableWhenPresent()
        {
            var bios = BiosInfo.Current;
            if (bios.ReleaseDate.HasValue)
            {
                bios.ReleaseDate.Value.Year.Should().BeGreaterOrEqualTo(1980);
                bios.ReleaseDate.Value.Should().BeOnOrBefore(DateTime.Today.AddDays(1));
            }
        }

        [Fact]
        public void SmbiosVersion_ShouldBeNullOrNonEmpty()
        {
            var bios = BiosInfo.Current;
            if (bios.SmbiosVersion != null)
                bios.SmbiosVersion.Should().NotBeNullOrWhiteSpace();
        }
    }
}
