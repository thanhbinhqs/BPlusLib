// <copyright file="ServiceHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Services;
using FluentAssertions;
using Xunit;

namespace BPlusLib.Foundation.Tests.Services
{
    /// <summary>
    /// Unit tests for the <see cref="ServiceHelper"/> class.
    /// Most tests are skipped on non-Windows platforms via <see cref="SkippableFactAttribute"/>.
    /// </summary>
    [Trait("Category", "Services")]
    public sealed class ServiceHelperTests
    {
        // =================================================================
        // ServiceInfo model tests (no OS dependency)
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceState.Stopped"/> has the correct underlying value (0x01).
        /// </summary>
        [Fact]
        public void ServiceState_Stopped_Value()
        {
            ((int)ServiceState.Stopped).Should().Be(0x01);
        }

        /// <summary>
        /// Verifies that <see cref="ServiceState.Running"/> has the correct underlying value (0x04).
        /// </summary>
        [Fact]
        public void ServiceState_Running_Value()
        {
            ((int)ServiceState.Running).Should().Be(0x04);
        }

        /// <summary>
        /// Verifies that <see cref="ServiceState.Paused"/> has the correct underlying value (0x07).
        /// </summary>
        [Fact]
        public void ServiceState_Paused_Value()
        {
            ((int)ServiceState.Paused).Should().Be(0x07);
        }

        /// <summary>
        /// Verifies that <see cref="ServiceStartType.Automatic"/> has the correct underlying value (0x02).
        /// </summary>
        [Fact]
        public void ServiceStartType_Automatic_Value()
        {
            ((int)ServiceStartType.Automatic).Should().Be(0x02);
        }

        /// <summary>
        /// Verifies that <see cref="ServiceStartType.Disabled"/> has the correct underlying value (0x04).
        /// </summary>
        [Fact]
        public void ServiceStartType_Disabled_Value()
        {
            ((int)ServiceStartType.Disabled).Should().Be(0x04);
        }

        /// <summary>
        /// Verifies that a <see cref="ServiceInfo"/> instance has correct defaults.
        /// </summary>
        [Fact]
        public void ServiceInfo_DefaultInstance()
        {
            var info = new ServiceInfo();
            info.ServiceName.Should().BeNull();
            info.DisplayName.Should().BeNull();
            info.State.Should().Be(default(ServiceState));
            info.StartType.Should().Be(default(ServiceStartType));
            info.IsRunning.Should().BeFalse();
            info.IsPending.Should().BeFalse();
            info.ProcessId.Should().Be(0u);
            // When ServiceName and DisplayName are null, string interpolation renders them as ""
            // Default ServiceState value (0) has no matching member, so ToString shows "0"
            info.ToString().Should().Be(" (): 0");
        }

        /// <summary>
        /// Verifies that <see cref="ServiceInfo.IsRunning"/> returns true only for <see cref="ServiceState.Running"/>.
        /// </summary>
        [Fact]
        public void ServiceInfo_IsRunning_OnlyTrueForRunning()
        {
            var info = new ServiceInfo { State = ServiceState.Stopped };
            info.IsRunning.Should().BeFalse();

            info.State = ServiceState.StartPending;
            info.IsRunning.Should().BeFalse();

            info.State = ServiceState.Running;
            info.IsRunning.Should().BeTrue();

            info.State = ServiceState.Paused;
            info.IsRunning.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceInfo.IsPending"/> returns true for all pending states.
        /// </summary>
        [Fact]
        public void ServiceInfo_IsPending_TrueForPendingStates()
        {
            var info = new ServiceInfo { State = ServiceState.Running };
            info.IsPending.Should().BeFalse();

            info.State = ServiceState.StartPending;
            info.IsPending.Should().BeTrue();

            info.State = ServiceState.StopPending;
            info.IsPending.Should().BeTrue();

            info.State = ServiceState.PausePending;
            info.IsPending.Should().BeTrue();

            info.State = ServiceState.ContinuePending;
            info.IsPending.Should().BeTrue();

            info.State = ServiceState.Stopped;
            info.IsPending.Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — GetService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.GetService"/> returns null for null input.
        /// </summary>
        [Fact]
        public void GetService_NullName_ReturnsNull()
        {
            ServiceHelper.GetService(null!).Should().BeNull();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.GetService"/> returns null for empty input.
        /// </summary>
        [Fact]
        public void GetService_EmptyName_ReturnsNull()
        {
            ServiceHelper.GetService(string.Empty).Should().BeNull();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.GetService"/> returns null for a non-existent service
        /// (works on any platform because the API gracefully fails).
        /// </summary>
        [Fact]
        public void GetService_NonExistent_ReturnsNull()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.GetService(uniqueName).Should().BeNull();
        }

        // =================================================================
        // ServiceHelper — StartService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StartService"/> returns false for null input.
        /// </summary>
        [Fact]
        public void StartService_NullName_ReturnsFalse()
        {
            ServiceHelper.StartService(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StartService"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void StartService_EmptyName_ReturnsFalse()
        {
            ServiceHelper.StartService(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StartService"/> returns false for a non-existent service
        /// (works on any platform because the API gracefully fails).
        /// </summary>
        [Fact]
        public void StartService_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.StartService(uniqueName).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StartService"/> with a negative timeout returns false
        /// for a non-existent service.
        /// </summary>
        [Fact]
        public void StartService_NegativeTimeout_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.StartService(uniqueName, -1).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — StopService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StopService"/> returns false for null input.
        /// </summary>
        [Fact]
        public void StopService_NullName_ReturnsFalse()
        {
            ServiceHelper.StopService(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StopService"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void StopService_EmptyName_ReturnsFalse()
        {
            ServiceHelper.StopService(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.StopService"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void StopService_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.StopService(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — RestartService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.RestartService"/> returns false for null input.
        /// </summary>
        [Fact]
        public void RestartService_NullName_ReturnsFalse()
        {
            ServiceHelper.RestartService(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.RestartService"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void RestartService_EmptyName_ReturnsFalse()
        {
            ServiceHelper.RestartService(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.RestartService"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void RestartService_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.RestartService(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — PauseService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.PauseService"/> returns false for null input.
        /// </summary>
        [Fact]
        public void PauseService_NullName_ReturnsFalse()
        {
            ServiceHelper.PauseService(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.PauseService"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void PauseService_EmptyName_ReturnsFalse()
        {
            ServiceHelper.PauseService(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.PauseService"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void PauseService_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.PauseService(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — ContinueService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ContinueService"/> returns false for null input.
        /// </summary>
        [Fact]
        public void ContinueService_NullName_ReturnsFalse()
        {
            ServiceHelper.ContinueService(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ContinueService"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void ContinueService_EmptyName_ReturnsFalse()
        {
            ServiceHelper.ContinueService(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ContinueService"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void ContinueService_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.ContinueService(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — CreateService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.CreateService"/> returns false when service name is null.
        /// </summary>
        [Fact]
        public void CreateService_NullName_ReturnsFalse()
        {
            ServiceHelper.CreateService(null!, "Display", "C:\\test.exe").Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.CreateService"/> returns false when binary path is null.
        /// </summary>
        [Fact]
        public void CreateService_NullBinaryPath_ReturnsFalse()
        {
            ServiceHelper.CreateService("TestSvc", "Display", null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.CreateService"/> returns false when binary path is empty.
        /// </summary>
        [Fact]
        public void CreateService_EmptyBinaryPath_ReturnsFalse()
        {
            ServiceHelper.CreateService("TestSvc", "Display", string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.CreateService"/> returns false on non-Windows
        /// (or insufficient privileges). This is a cross-platform safe test.
        /// </summary>
        [Fact]
        public void CreateService_NonExistentBinary_ReturnsFalse()
        {
            string uniqueName = "BPlusLibTest_" + Guid.NewGuid().ToString("N");
            bool result = ServiceHelper.CreateService(
                uniqueName,
                "BPlusLib Test Service",
                "C:\\NonExistentPath\\test_service.exe");
            result.Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — DeleteService tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.DeleteService"/> returns false for null input.
        /// </summary>
        [Fact]
        public void DeleteService_NullName_ReturnsFalse()
        {
            ServiceHelper.DeleteService(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.DeleteService"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void DeleteService_EmptyName_ReturnsFalse()
        {
            ServiceHelper.DeleteService(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.DeleteService"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void DeleteService_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.DeleteService(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — EnumerateServices tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.EnumerateServices"/> returns an empty list
        /// when called with <see cref="ServiceState.Running"/> on non-Windows (or insufficient privilege).
        /// </summary>
        [Fact]
        public void EnumerateServices_Running_ReturnsList()
        {
            List<ServiceInfo> services = ServiceHelper.EnumerateServices(ServiceState.Running);
            services.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.EnumerateServices"/> with <see cref="ServiceState.Unknown"/>
        /// enumerates all services (returns a list, possibly empty).
        /// </summary>
        [Fact]
        public void EnumerateServices_AllStates_ReturnsList()
        {
            List<ServiceInfo> services = ServiceHelper.EnumerateServices(ServiceState.Unknown);
            services.Should().NotBeNull();
        }

        // =================================================================
        // ServiceHelper — ServiceExists tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ServiceExists"/> returns false for null input.
        /// </summary>
        [Fact]
        public void ServiceExists_NullName_ReturnsFalse()
        {
            ServiceHelper.ServiceExists(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ServiceExists"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void ServiceExists_EmptyName_ReturnsFalse()
        {
            ServiceHelper.ServiceExists(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ServiceExists"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void ServiceExists_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.ServiceExists(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // ServiceHelper — IsServiceRunning tests
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.IsServiceRunning"/> returns false for null input.
        /// </summary>
        [Fact]
        public void IsServiceRunning_NullName_ReturnsFalse()
        {
            ServiceHelper.IsServiceRunning(null!).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.IsServiceRunning"/> returns false for empty input.
        /// </summary>
        [Fact]
        public void IsServiceRunning_EmptyName_ReturnsFalse()
        {
            ServiceHelper.IsServiceRunning(string.Empty).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.IsServiceRunning"/> returns false for a non-existent service.
        /// </summary>
        [Fact]
        public void IsServiceRunning_NonExistent_ReturnsFalse()
        {
            string uniqueName = "NonExistentService_" + Guid.NewGuid().ToString("N");
            ServiceHelper.IsServiceRunning(uniqueName).Should().BeFalse();
        }

        // =================================================================
        // Windows-specific integration tests (skipped on non-Windows)
        // =================================================================

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.GetService"/> can retrieve an existing system service
        /// on Windows.
        /// </summary>
        [SkippableFact]
        public void GetService_Windows_SystemService()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            ServiceInfo? info = ServiceHelper.GetService("winmgmt");
            info.Should().NotBeNull();
            info!.ServiceName.Should().Be("winmgmt");
            info.DisplayName.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.ServiceExists"/> returns true for an existing
        /// system service on Windows.
        /// </summary>
        [SkippableFact]
        public void ServiceExists_Windows_SystemService()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            bool exists = ServiceHelper.ServiceExists("winmgmt");
            exists.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.IsServiceRunning"/> can query a system service
        /// on Windows.
        /// </summary>
        [SkippableFact]
        public void IsServiceRunning_Windows_SystemService()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            // This may be running or not depending on the system state — but should not throw
            bool running = ServiceHelper.IsServiceRunning("winmgmt");
            // No assertion on the value itself — just verifying no exception was thrown
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.EnumerateServices"/> returns at least one service
        /// when enumerating all states on Windows.
        /// </summary>
        [SkippableFact]
        public void EnumerateServices_Windows_AllStates_ReturnsResults()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            List<ServiceInfo> services = ServiceHelper.EnumerateServices(ServiceState.Unknown);
            services.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Verifies that <see cref="ServiceHelper.EnumerateServices"/> can filter by running state
        /// on Windows.
        /// </summary>
        [SkippableFact]
        public void EnumerateServices_Windows_Running_ReturnsResults()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            List<ServiceInfo> services = ServiceHelper.EnumerateServices(ServiceState.Running);
            services.Should().NotBeNullOrEmpty();
            services.Should().OnlyContain(s => s.State == ServiceState.Running);
        }
    }
}
