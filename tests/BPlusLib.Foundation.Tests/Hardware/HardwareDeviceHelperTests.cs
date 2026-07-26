// <copyright file="HardwareDeviceHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.Hardware;

namespace BPlusLib.Foundation.Tests.Hardware
{
    [Trait("Category", "Hardware")]
    public sealed class HardwareDeviceHelperTests
    {
        // ── GetAllDevices ─────────────────────────────────────────────

        [SkippableFact]
        public void GetAllDevices_ReturnsNonEmpty()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetAllDevices();
            devices.Should().NotBeEmpty();
        }

        [SkippableFact]
        public void GetAllDevices_HasDeviceIds()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetAllDevices();
            devices.Should().AllSatisfy(d =>
            {
                d.DeviceId.Should().NotBeNullOrEmpty();
                d.DeviceName.Should().NotBeNull();
            });
        }

        // ── GetDevicesByClass ─────────────────────────────────────────

        [SkippableFact]
        public void GetUsbDevices_ReturnsList()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetUsbDevices();
            devices.Should().NotBeNull();
        }

        [SkippableFact]
        public void GetComPorts_ReturnsList()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetComPorts();
            devices.Should().NotBeNull();
        }

        [SkippableFact]
        public void GetDiskDrives_ReturnsNonEmpty()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetDiskDrives();
            devices.Should().NotBeEmpty();
        }

        [SkippableFact]
        public void GetNetworkAdapters_ReturnsNonEmpty()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetNetworkAdapters();
            devices.Should().NotBeEmpty();
        }

        [SkippableFact]
        public void GetUsbStorageDevices_ReturnsUsbSpeedInfo()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var devices = HardwareDeviceHelper.GetUsbStorageDevices();
            // Should not throw, even if empty
            devices.Should().NotBeNull();
        }

        // ── GetDeviceById ─────────────────────────────────────────────

        [SkippableFact]
        public void GetDeviceById_NullReturnsNull()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            HardwareDeviceHelper.GetDeviceById(null!).Should().BeNull();
            HardwareDeviceHelper.GetDeviceById("").Should().BeNull();
        }

        [SkippableFact]
        public void GetDeviceById_NonexistentReturnsNull()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            HardwareDeviceHelper.GetDeviceById("ROOT\\NONEXISTENT\\0000").Should().BeNull();
        }

        // ── IsDevicePresent ───────────────────────────────────────────

        [SkippableFact]
        public void IsDevicePresent_NullReturnsFalse()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            HardwareDeviceHelper.IsDevicePresent(null!).Should().BeFalse();
        }

        // ── UsbDeviceParser ───────────────────────────────────────────

        [Fact]
        public void UsbDeviceParser_TryParseVidPid_ValidId()
        {
            string deviceId = @"USB\VID_1234&PID_5678\SerialNumber123";
            bool result = UsbDeviceParser.TryParseVidPid(deviceId, out int vid, out int pid);
            result.Should().BeTrue();
            vid.Should().Be(0x1234);
            pid.Should().Be(0x5678);
        }

        [Fact]
        public void UsbDeviceParser_TryParseVidPid_InvalidId()
        {
            bool result = UsbDeviceParser.TryParseVidPid(@"PCI\VEN_1234", out _, out _);
            result.Should().BeFalse();
        }

        [Fact]
        public void UsbDeviceParser_ParseSerialNumber_ValidId()
        {
            string deviceId = @"USB\VID_1234&PID_5678\ABCD1234";
            string? serial = UsbDeviceParser.ParseSerialNumber(deviceId);
            serial.Should().Be("ABCD1234");
        }

        [Fact]
        public void UsbDeviceParser_IsUsbDevice_True()
        {
            UsbDeviceParser.IsUsbDevice(@"USB\VID_1234&PID_5678").Should().BeTrue();
        }

        [Fact]
        public void UsbDeviceParser_IsUsbDevice_False()
        {
            UsbDeviceParser.IsUsbDevice(@"PCI\VEN_1234").Should().BeFalse();
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_HighSpeed()
        {
            string desc = "USB 2.00, Speed = High speed (480Mbit/s), ...";
            UsbDeviceParser.ParseSpeed(desc).Should().Be(UsbSpeed.HighSpeed);
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_SuperSpeed()
        {
            string desc = "USB 3.00, Speed = SuperSpeed (5Gbit/s), ...";
            UsbDeviceParser.ParseSpeed(desc).Should().Be(UsbSpeed.SuperSpeed);
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_SuperSpeedPlus()
        {
            string desc = "USB 3.10, Speed = SuperSpeed+ (10Gbit/s), ...";
            UsbDeviceParser.ParseSpeed(desc).Should().Be(UsbSpeed.SuperSpeedPlus);
        }

        [Fact]
        public void UsbDeviceParser_ParseSpeed_EmptyReturnsUnknown()
        {
            UsbDeviceParser.ParseSpeed("").Should().Be(UsbSpeed.Unknown);
            UsbDeviceParser.ParseSpeed(null!).Should().Be(UsbSpeed.Unknown);
        }

        [Fact]
        public void UsbDeviceParser_GetUsbVersionString_Works()
        {
            UsbDeviceParser.GetUsbVersionString(UsbSpeed.HighSpeed).Should().Be("2.0");
            UsbDeviceParser.GetUsbVersionString(UsbSpeed.SuperSpeed).Should().Be("3.0/3.1 Gen 1");
            UsbDeviceParser.GetUsbVersionString(UsbSpeed.SuperSpeedPlus).Should().Be("3.1 Gen 2/3.2");
        }

        [Fact]
        public void UsbDeviceParser_GetDeviceClassName_Works()
        {
            UsbDeviceParser.GetDeviceClassName(0x08).Should().Be("Mass Storage");
            UsbDeviceParser.GetDeviceClassName(0x03).Should().Be("HID (Human Interface)");
            UsbDeviceParser.GetDeviceClassName(0x09).Should().Be("Hub");
        }

        [Fact]
        public void UsbDeviceParser_GetSpeedDescription_Works()
        {
            UsbDeviceParser.GetSpeedDescription(UsbSpeed.HighSpeed).Should().Contain("480 Mbps");
            UsbDeviceParser.GetSpeedDescription(UsbSpeed.SuperSpeed).Should().Contain("5 Gbps");
        }

        [Fact]
        public void UsbDeviceParser_ParseMaxPower_Works()
        {
            string desc = "..., MaxPower = 500mA, ...";
            UsbDeviceParser.ParseMaxPower(desc).Should().Be(500);
        }

        [Fact]
        public void UsbDeviceParser_ParseMaxPower_EmptyReturnsZero()
        {
            UsbDeviceParser.ParseMaxPower("").Should().Be(0);
            UsbDeviceParser.ParseMaxPower(null!).Should().Be(0);
        }

        // ── HardwareDeviceInfo ─────────────────────────────────────────

        [Fact]
        public void HardwareDeviceInfo_ToString_UsbDevice()
        {
            var device = new HardwareDeviceInfo
            {
                DeviceName = "Kingston DataTraveler",
                VendorId = 0x0951,
                ProductId = 0x1666,
                Speed = UsbSpeed.HighSpeed,
                UsbVersion = "2.0",
                ClassName = "Disk drives",
            };

            string str = device.ToString();
            str.Should().Contain("Kingston DataTraveler");
            str.Should().Contain("0951");
            str.Should().Contain("1666");
            str.Should().Contain("2.0");
        }

        [Fact]
        public void HardwareDeviceInfo_ToString_NonUsbDevice()
        {
            var device = new HardwareDeviceInfo
            {
                DeviceName = "Intel(R) Ethernet Connection",
                ClassName = "Network adapters",
            };

            string str = device.ToString();
            str.Should().Contain("Intel(R) Ethernet Connection");
            str.Should().Contain("Network adapters");
        }
    }
}
