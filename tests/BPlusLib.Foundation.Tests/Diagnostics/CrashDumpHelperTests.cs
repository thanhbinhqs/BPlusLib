// <copyright file="CrashDumpHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Diagnostics;

namespace BPlusLib.Foundation.Tests.Diagnostics
{
    [Trait("Category", "Diagnostics")]
    public sealed class CrashDumpHelperTests
    {
        private const int InvalidPid = int.MaxValue;

        // ── TryCreateMiniDump ───────────────────────────────────────────────

        [Fact]
        public void TryCreateMiniDump_InvalidPid_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateMiniDump(InvalidPid, "/tmp/test.dmp");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateMiniDump_NegativePid_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateMiniDump(-1, "/tmp/test.dmp");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateMiniDump_ZeroPid_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateMiniDump(0, "/tmp/test.dmp");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateMiniDump_EmptyPath_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateMiniDump(1, string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateMiniDump_NullPath_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateMiniDump(1, null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateMiniDump_OnLinux_ReturnsFalse()
        {
            // On Linux, MiniDumpWriteDump is not available => returns false
            bool result = CrashDumpHelper.TryCreateMiniDump(1, "/tmp/test_mini.dmp");
            result.Should().BeFalse();
        }

        // ── TryCreateFullDump ───────────────────────────────────────────────

        [Fact]
        public void TryCreateFullDump_InvalidPid_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateFullDump(InvalidPid, "/tmp/test.dmp");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateFullDump_NegativePid_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateFullDump(-1, "/tmp/test.dmp");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateFullDump_ZeroPid_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateFullDump(0, "/tmp/test.dmp");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateFullDump_EmptyPath_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateFullDump(1, string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateFullDump_NullPath_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateFullDump(1, null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryCreateFullDump_OnLinux_ReturnsFalse()
        {
            bool result = CrashDumpHelper.TryCreateFullDump(1, "/tmp/test_full.dmp");
            result.Should().BeFalse();
        }

        // ── GetDefaultDumpFolder ────────────────────────────────────────────

        [Fact]
        public void GetDefaultDumpFolder_ShouldReturnString()
        {
            string? folder = CrashDumpHelper.GetDefaultDumpFolder();
            // On Linux returns null; on Windows returns a path
            // We just verify it doesn't throw
        }

        [Fact]
        public void GetDefaultDumpFolder_ShouldNotThrow()
        {
            Action act = () => CrashDumpHelper.GetDefaultDumpFolder();
            act.Should().NotThrow();
        }

        // ── MiniDumpType enum ───────────────────────────────────────────────

        [Fact]
        public void MiniDumpType_Values_ShouldBeCorrect()
        {
            ((uint)MiniDumpType.MiniDumpNormal).Should().Be(0);
            ((uint)MiniDumpType.MiniDumpWithDataSegs).Should().Be(1);
            ((uint)MiniDumpType.MiniDumpWithFullMemory).Should().Be(2);
            ((uint)MiniDumpType.MiniDumpWithHandleData).Should().Be(4);
            ((uint)MiniDumpType.MiniDumpFilterMemory).Should().Be(8);
            ((uint)MiniDumpType.MiniDumpScanMemory).Should().Be(16);
            ((uint)MiniDumpType.MiniDumpWithUnloadedModules).Should().Be(32);
            ((uint)MiniDumpType.MiniDumpWithIndirectlyReferencedMemory).Should().Be(64);
            ((uint)MiniDumpType.MiniDumpFilterModulePaths).Should().Be(128);
            ((uint)MiniDumpType.MiniDumpWithProcessThreadData).Should().Be(256);
            ((uint)MiniDumpType.MiniDumpWithPrivateReadWriteMemory).Should().Be(512);
            ((uint)MiniDumpType.MiniDumpWithoutOptionalData).Should().Be(1024);
            ((uint)MiniDumpType.MiniDumpWithFullMemoryInfo).Should().Be(2048);
            ((uint)MiniDumpType.MiniDumpWithThreadInfo).Should().Be(4096);
            ((uint)MiniDumpType.MiniDumpWithCodeSegs).Should().Be(8192);
            ((uint)MiniDumpType.MiniDumpWithoutAuxiliaryState).Should().Be(16384);
            ((uint)MiniDumpType.MiniDumpWithFullAuxiliaryState).Should().Be(32768);
            ((uint)MiniDumpType.MiniDumpWithPrivateWriteCopyMemory).Should().Be(65536);
            ((uint)MiniDumpType.MiniDumpIgnoreInaccessibleMemory).Should().Be(131072);
            ((uint)MiniDumpType.MiniDumpWithTokenInformation).Should().Be(262144);
            ((uint)MiniDumpType.MiniDumpWithModuleHeaders).Should().Be(524288);
            ((uint)MiniDumpType.MiniDumpFilterTriage).Should().Be(1048576);
            ((uint)MiniDumpType.MiniDumpWithAvxXStateContext).Should().Be(2097152);
            ((uint)MiniDumpType.MiniDumpWithIptTrace).Should().Be(4194304);
        }
    }
}
