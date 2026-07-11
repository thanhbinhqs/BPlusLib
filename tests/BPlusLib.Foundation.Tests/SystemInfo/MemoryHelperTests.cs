// <copyright file="MemoryHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Tests.SystemInfo
{
    [Trait("Category", "SystemInfo")]
    public sealed class MemoryHelperTests
    {
        private const string TestMappingName = "BPlusLib_MemoryHelper_Test_" + nameof(CreateAndOpen_SharedMemory);

        [SkippableFact]
        public void CreateAndOpen_SharedMemory()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            const int size = 1024;
            const int testValue = unchecked((int)0xABCDEF01);

            // Create the shared memory mapping
            using (var creator = MemoryHelper.CreateOrOpen(TestMappingName, size, readWrite: true))
            {
                creator.Should().NotBeNull();
                creator!.Size.Should().Be(size);
                creator.Pointer.Should().NotBe(IntPtr.Zero);

                // Write a test value while the creator view is alive
                Marshal.WriteInt32(creator.Pointer, testValue);

                // Open a second view to the same mapping (creator still alive)
                using (var opener = MemoryHelper.Open(TestMappingName, readWrite: true))
                {
                    opener.Should().NotBeNull();
                    opener!.Pointer.Should().NotBe(IntPtr.Zero);

                    int readValue = Marshal.ReadInt32(opener.Pointer);
                    readValue.Should().Be(testValue);
                }
            }
        }

        [SkippableFact]
        public void Create_ZeroSize_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var view = MemoryHelper.CreateOrOpen("BPlusLib_Test_ZeroSize", 0);
            view.Should().BeNull();
        }

        [SkippableFact]
        public void Open_NonExistent_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var view = MemoryHelper.Open("BPlusLib_Test_NonExistent_" + Guid.NewGuid());
            view.Should().BeNull();
        }

        [SkippableFact]
        public void GetProcessMemoryCounters_ReturnsNonZero()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var counters = MemoryHelper.GetProcessMemoryCounters();
            counters.Should().NotBeNull();
            counters!.WorkingSetSize.Should().BeGreaterThan(0);
            counters.PeakWorkingSetSize.Should().BeGreaterThan(0);
            counters.PageFaultCount.Should().BeGreaterThan(0);
        }

        [SkippableFact]
        public void Dispose_View_MultipleCalls()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var view = MemoryHelper.CreateOrOpen("BPlusLib_Test_DisposeMultiple", 64);
            view.Should().NotBeNull();

            // Dispose multiple times — should not throw
            view!.Dispose();
            view.Dispose();
            view.Dispose();

            view.IsDisposed.Should().BeTrue();
            Action act = () => { var p = view.Pointer; };
            act.Should().Throw<ObjectDisposedException>();
        }
    }
}
