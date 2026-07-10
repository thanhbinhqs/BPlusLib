// <copyright file="SerialPortOwnerTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.SerialPorts;

namespace BPlusLib.Foundation.Tests.SerialPorts
{
    [Trait("Category", "SerialPorts")]
    public sealed class SerialPortOwnerTests
    {
        [Fact]
        public void AllProperties_CanBeSetViaInit()
        {
            var now = DateTime.UtcNow;
            var owner = new SerialPortOwner
            {
                PortName = "COM3",
                DevicePath = @"\Device\Serial0",
                ProcessId = 1234,
                ProcessName = "notepad.exe",
                ImagePath = @"C:\Windows\notepad.exe",
                CommandLine = "notepad.exe test.txt",
                StartTime = now,
                CompanyName = "Microsoft",
                ProductName = "Notepad",
                FileVersion = "10.0.19041.1",
                ProductVersion = "10.0.19041.1",
            };

            owner.PortName.Should().Be("COM3");
            owner.DevicePath.Should().Be(@"\Device\Serial0");
            owner.ProcessId.Should().Be(1234);
            owner.ProcessName.Should().Be("notepad.exe");
            owner.ImagePath.Should().Be(@"C:\Windows\notepad.exe");
            owner.CommandLine.Should().Be("notepad.exe test.txt");
            owner.StartTime.Should().Be(now);
            owner.CompanyName.Should().Be("Microsoft");
            owner.ProductName.Should().Be("Notepad");
            owner.FileVersion.Should().Be("10.0.19041.1");
            owner.ProductVersion.Should().Be("10.0.19041.1");
        }

        [Fact]
        public void DefaultValues_AreEmptyNullOrZero()
        {
            var owner = new SerialPortOwner();

            owner.PortName.Should().Be(string.Empty);
            owner.DevicePath.Should().Be(string.Empty);
            owner.ProcessId.Should().Be(0);
            owner.ProcessName.Should().Be(string.Empty);
            owner.ImagePath.Should().Be(string.Empty);
        }

        [Fact]
        public void ToString_ContainsPortNameAndPid()
        {
            var owner = new SerialPortOwner
            {
                PortName = "COM5",
                ProcessId = 5678,
                ProcessName = "test.exe",
                ImagePath = @"C:\test.exe",
            };

            var str = owner.ToString();
            str.Should().Contain("COM5");
            str.Should().Contain("5678");
            str.Should().Contain("test.exe");
            str.Should().Contain("C:\\test.exe");
        }

        [Fact]
        public void NullableProperties_StartAsNull()
        {
            var owner = new SerialPortOwner();

            owner.CommandLine.Should().BeNull();
            owner.StartTime.Should().BeNull();
            owner.CompanyName.Should().BeNull();
            owner.ProductName.Should().BeNull();
            owner.FileVersion.Should().BeNull();
            owner.ProductVersion.Should().BeNull();
        }
    }
}
