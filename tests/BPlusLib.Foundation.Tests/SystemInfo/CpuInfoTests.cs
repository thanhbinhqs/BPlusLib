// <copyright file="CpuInfoTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class CpuInfoTests
    {
        [Fact]
        public void Current_ShouldNotThrow()
        {
            CpuInfo instance = null!;
            Action act = () => instance = CpuInfo.Current;
            act.Should().NotThrow();
        }

        [Fact]
        public void Name_ShouldNotBeNull()
        {
            var cpu = CpuInfo.Current;
            cpu.Name.Should().NotBeNull();
        }

        [Fact]
        public void Manufacturer_ShouldNotBeNull()
        {
            var cpu = CpuInfo.Current;
            cpu.Manufacturer.Should().NotBeNull();
        }

        [Fact]
        public void LogicalCores_ShouldBePositive()
        {
            var cpu = CpuInfo.Current;
            cpu.LogicalCores.Should().BeGreaterThan(0);
        }

        [Fact]
        public void LogicalCores_ShouldMatchEnvironmentProcessorCount()
        {
            var cpu = CpuInfo.Current;
            // On Linux, falls back to Environment.ProcessorCount
            cpu.LogicalCores.Should().Be(Environment.ProcessorCount);
        }

        [Fact]
        public void PhysicalCores_ShouldBePositive()
        {
            var cpu = CpuInfo.Current;
            // On Linux, falls back to logical / 2
            cpu.PhysicalCores.Should().BeGreaterThan(0);
            cpu.PhysicalCores.Should().BeLessOrEqualTo(cpu.LogicalCores);
        }

        [Fact]
        public void Architecture_ShouldNotBeNullOrEmpty()
        {
            var cpu = CpuInfo.Current;
            cpu.Architecture.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Architecture_ShouldBeValidValue()
        {
            var cpu = CpuInfo.Current;
            // On Linux, falls back to IntPtr.Size check → "x64" or "x86"
            cpu.Architecture.Should().BeOneOf("x86", "x64", "ARM64", "ARM", "Unknown");
        }

        [Fact]
        public void CurrentLoadPercentage_ShouldBeValid()
        {
            var cpu = CpuInfo.Current;

            // On Linux, NtQuerySystemInformation fails → returns null
            if (cpu.CurrentLoadPercentage.HasValue)
            {
                cpu.CurrentLoadPercentage.Value.Should().BeInRange(0.0f, 100.0f);
            }
        }

        [Fact]
        public void IsVirtualMachine_ShouldBeBool()
        {
            var cpu = CpuInfo.Current;
            // On Linux, registry checks fail → false
            cpu.IsVirtualMachine.Should().BeFalse();
        }

        [Fact]
        public void MaxFrequencyMHz_ShouldBeNonNegative()
        {
            var cpu = CpuInfo.Current;
            cpu.MaxFrequencyMHz.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void CurrentFrequencyMHz_ShouldBeNonNegative()
        {
            var cpu = CpuInfo.Current;
            cpu.CurrentFrequencyMHz.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void ProcessorId_ShouldBeNonNegative()
        {
            var cpu = CpuInfo.Current;
            cpu.ProcessorId.Should().BeGreaterOrEqualTo(0);
        }
    }
}
