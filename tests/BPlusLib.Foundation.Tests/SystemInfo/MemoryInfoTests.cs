// <copyright file="MemoryInfoTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class MemoryInfoTests
    {
        [Fact]
        public void Current_ShouldNotThrow()
        {
            MemoryInfo instance = null!;
            Action act = () => instance = MemoryInfo.Current;
            act.Should().NotThrow();
        }

        [Fact]
        public void TotalPhysicalBytes_ShouldBePositiveOrZero()
        {
            var mem = MemoryInfo.Current;
            mem.TotalPhysicalBytes.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void AvailablePhysicalBytes_ShouldBeLessOrEqualToTotal()
        {
            var mem = MemoryInfo.Current;
            mem.AvailablePhysicalBytes.Should().BeLessOrEqualTo(mem.TotalPhysicalBytes);
        }

        [Fact]
        public void UsedPhysicalBytes_ShouldBeNonNegative()
        {
            var mem = MemoryInfo.Current;
            mem.UsedPhysicalBytes.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void PhysicalUsagePercent_ShouldBeBetween0And100()
        {
            var mem = MemoryInfo.Current;
            mem.PhysicalUsagePercent.Should().BeInRange(0.0, 100.0);
        }

        [Fact]
        public void TotalVirtualBytes_ShouldBePositiveOrZero()
        {
            var mem = MemoryInfo.Current;
            mem.TotalVirtualBytes.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void AvailableVirtualBytes_ShouldBeLessOrEqualToTotal()
        {
            var mem = MemoryInfo.Current;
            mem.AvailableVirtualBytes.Should().BeLessOrEqualTo(mem.TotalVirtualBytes);
        }

        [Fact]
        public void UsedVirtualBytes_ShouldBeNonNegative()
        {
            var mem = MemoryInfo.Current;
            mem.UsedVirtualBytes.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void TotalPageFileBytes_ShouldBePositiveOrZero()
        {
            var mem = MemoryInfo.Current;
            mem.TotalPageFileBytes.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void AvailablePageFileBytes_ShouldBeLessOrEqualToTotal()
        {
            var mem = MemoryInfo.Current;
            mem.AvailablePageFileBytes.Should().BeLessOrEqualTo(mem.TotalPageFileBytes);
        }

        [Fact]
        public void UsedPageFileBytes_ShouldBeNonNegative()
        {
            var mem = MemoryInfo.Current;
            mem.UsedPageFileBytes.Should().BeGreaterOrEqualTo(0);
        }
    }
}
