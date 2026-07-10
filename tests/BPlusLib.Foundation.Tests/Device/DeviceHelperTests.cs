// <copyright file="DeviceHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Device;

namespace BPlusLib.Foundation.Tests.Device
{
    [Trait("Category", "Device")]
    public sealed class DeviceHelperTests
    {
        // ── RegisterDeviceNotification ────────────────────────────────────

        [Fact]
        public void RegisterDeviceNotification_WithZeroHwnd_ReturnsFalse()
        {
            IntPtr handle = DeviceHelper.RegisterDeviceNotification(
                IntPtr.Zero, Guid.Empty);

            handle.Should().Be(IntPtr.Zero, "because a null window handle should fail");
        }

        // ── UnregisterDeviceNotification ──────────────────────────────────

        [Fact]
        public void UnregisterDeviceNotification_WithZeroHandle_ReturnsFalse()
        {
            bool result = DeviceHelper.UnregisterDeviceNotification(IntPtr.Zero);

            result.Should().BeFalse("because a null notification handle should fail");
        }

        // ── GetAllDevices ─────────────────────────────────────────────────

        [Fact]
        public void GetAllDevices_OnLinux_ReturnsEmpty()
        {
            // SetupAPI is not available on Linux -> returns empty list.
            var devices = DeviceHelper.GetAllDevices();

            devices.Should().NotBeNull();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                devices.Should().BeEmpty("because SetupAPI is Windows-only");
            }
        }

        // ── GetDeviceProperty ─────────────────────────────────────────────

        [Fact]
        public void GetDeviceProperty_NonExistentDevice_ReturnsNull()
        {
            string? value = DeviceHelper.GetDeviceProperty(
                "NONEXISTENT\\DEVICE\\001", "DeviceDesc");

            value.Should().BeNull();
        }

        [Fact]
        public void GetDeviceProperty_NullDeviceId_ReturnsNull()
        {
            DeviceHelper.GetDeviceProperty(null!, "DeviceDesc").Should().BeNull();
        }

        [Fact]
        public void GetDeviceProperty_NullPropertyName_ReturnsNull()
        {
            DeviceHelper.GetDeviceProperty("some\\device\\id", null!).Should().BeNull();
        }

        [Fact]
        public void GetDeviceProperty_EmptyDeviceId_ReturnsNull()
        {
            DeviceHelper.GetDeviceProperty(string.Empty, "DeviceDesc").Should().BeNull();
        }

        [Fact]
        public void GetDeviceProperty_InvalidPropertyName_ReturnsNull()
        {
            DeviceHelper.GetDeviceProperty("some\\device\\id", "InvalidPropName").Should().BeNull();
        }

        // ── GetVolumeDevices ──────────────────────────────────────────────

        [Fact]
        public void GetVolumeDevices_ShouldNotThrow()
        {
            // On Linux, GetLogicalDrives and GetDriveTypeW are available via
            // kernel32.dll which does not exist on Linux -> DllNotFoundException
            // is caught and the method returns null.
            var volumes = DeviceHelper.GetVolumeDevices();

            // Should not throw.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Linux, the P/Invoke fails -> returns null.
                // On Windows with available drives, returns an array.
            }
        }

        // ── GetUsbDevices ─────────────────────────────────────────────────

        [Fact]
        public void GetUsbDevices_OnLinux_ReturnsEmpty()
        {
            var usbDevices = DeviceHelper.GetUsbDevices();

            usbDevices.Should().NotBeNull();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                usbDevices.Should().BeEmpty("because SetupAPI is Windows-only");
            }
        }

        // ── DeviceInfo model ──────────────────────────────────────────────

        [Fact]
        public void DeviceInfo_Properties_ShouldSetCorrectly()
        {
            var info = new DeviceInfo
            {
                DeviceId = @"USB\VID_1234\SERIAL123",
                Description = "USB Test Device",
                FriendlyName = "Test Device",
                Manufacturer = "TestCorp",
                DriverVersion = "10.0.0.1",
                DriverDate = "2024-01-01",
                HardwareId = @"USB\VID_1234&PID_5678",
                BusReportedDeviceDesc = "USB Input Device",
                ClassGuid = "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}",
                Status = "OK",
            };

            info.DeviceId.Should().Be(@"USB\VID_1234\SERIAL123");
            info.Description.Should().Be("USB Test Device");
            info.FriendlyName.Should().Be("Test Device");
            info.Manufacturer.Should().Be("TestCorp");
            info.DriverVersion.Should().Be("10.0.0.1");
            info.DriverDate.Should().Be("2024-01-01");
            info.HardwareId.Should().Be(@"USB\VID_1234&PID_5678");
            info.BusReportedDeviceDesc.Should().Be("USB Input Device");
            info.ClassGuid.Should().Be("{745a17a0-74d3-11d0-b6fe-00a0c90f57da}");
            info.Status.Should().Be("OK");
        }

        [Fact]
        public void DeviceInfo_ToString_ShouldContainDescription()
        {
            var info = new DeviceInfo
            {
                DeviceId = "TEST\\DEVICE\\001",
                Description = "My Device",
            };

            info.ToString().Should().Contain("My Device");
            info.ToString().Should().Contain("TEST\\DEVICE\\001");
        }

        [Fact]
        public void DeviceInfo_DefaultValues_ShouldBeNull()
        {
            var info = new DeviceInfo();

            info.DeviceId.Should().BeNull();
            info.Description.Should().BeNull();
            info.FriendlyName.Should().BeNull();
            info.Manufacturer.Should().BeNull();
            info.DriverVersion.Should().BeNull();
            info.DriverDate.Should().BeNull();
            info.HardwareId.Should().BeNull();
            info.BusReportedDeviceDesc.Should().BeNull();
            info.ClassGuid.Should().BeNull();
            info.Status.Should().BeNull();
        }

        // ── DeviceVolumeInfo model ────────────────────────────────────────

        [Fact]
        public void DeviceVolumeInfo_Properties_ShouldSetCorrectly()
        {
            var vol = new DeviceVolumeInfo
            {
                DriveLetter = "C:",
                VolumeLabel = "System",
                FileSystem = "NTFS",
                SerialNumber = "A1B2C3D4",
                DevicePath = @"\\.\PhysicalDrive0",
                DeviceType = "Fixed",
                IsReady = "True",
            };

            vol.DriveLetter.Should().Be("C:");
            vol.VolumeLabel.Should().Be("System");
            vol.FileSystem.Should().Be("NTFS");
            vol.SerialNumber.Should().Be("A1B2C3D4");
            vol.DevicePath.Should().Be(@"\\.\PhysicalDrive0");
            vol.DeviceType.Should().Be("Fixed");
            vol.IsReady.Should().Be("True");
        }

        [Fact]
        public void DeviceVolumeInfo_ToString_ShouldContainDriveLetter()
        {
            var vol = new DeviceVolumeInfo
            {
                DriveLetter = "D:",
                VolumeLabel = "Data",
            };

            vol.ToString().Should().Contain("D:");
            vol.ToString().Should().Contain("Data");
        }

        [Fact]
        public void DeviceVolumeInfo_DefaultValues_ShouldBeNull()
        {
            var vol = new DeviceVolumeInfo();

            vol.DriveLetter.Should().BeNull();
            vol.VolumeLabel.Should().BeNull();
            vol.FileSystem.Should().BeNull();
            vol.SerialNumber.Should().BeNull();
            vol.DevicePath.Should().BeNull();
            vol.DeviceType.Should().BeNull();
            vol.IsReady.Should().BeNull();
        }
    }
}
