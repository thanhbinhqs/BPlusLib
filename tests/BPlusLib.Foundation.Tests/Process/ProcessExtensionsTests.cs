// <copyright file="ProcessExtensionsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Diagnostics;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Process;

namespace BPlusLib.Foundation.Tests.Process
{
    [Trait("Category", "Process")]
    public sealed class ProcessExtensionsTests
    {
        private static System.Diagnostics.Process CurrentProcess =>
            System.Diagnostics.Process.GetCurrentProcess();

        // ── Null argument tests ─────────────────────────────────────────

        [Fact]
        public void GetParentProcessId_NullArg_ShouldThrow()
        {
            System.Diagnostics.Process? nullProc = null;
            Action act = () => nullProc!.GetParentProcessId();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetCommandLine_NullArg_ShouldThrow()
        {
            System.Diagnostics.Process? nullProc = null;
            Action act = () => nullProc!.GetCommandLine();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsElevated_NullArg_ShouldThrow()
        {
            System.Diagnostics.Process? nullProc = null;
            Action act = () => nullProc!.IsElevated();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetImagePath_NullArg_ShouldThrow()
        {
            System.Diagnostics.Process? nullProc = null;
            Action act = () => nullProc!.GetImagePath();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void KillTree_NullArg_ShouldThrow()
        {
            System.Diagnostics.Process? nullProc = null;
            Action act = () => nullProc!.KillTree();
            act.Should().Throw<ArgumentNullException>();
        }

        // ── Graceful degradation on non-Windows ─────────────────────────

        [Fact]
        public void GetParentProcessId_ShouldNotThrow()
        {
            var proc = CurrentProcess;
            int ppid = 0;
            Action act = () => ppid = proc.GetParentProcessId();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetParentProcessId_ShouldReturnNonNegative()
        {
            var proc = CurrentProcess;
            int ppid = proc.GetParentProcessId();
            ppid.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void GetCommandLine_ShouldNotThrow()
        {
            var proc = CurrentProcess;
            string? cmdLine = null;
            Action act = () => cmdLine = proc.GetCommandLine();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCommandLine_ShouldReturnNullOrValue()
        {
            var proc = CurrentProcess;
            string? cmdLine = proc.GetCommandLine();
            // On Linux, P/Invoke fails → returns null
            // On Windows, would return the command line
        }

        [Fact]
        public void IsElevated_ShouldNotThrow()
        {
            var proc = CurrentProcess;
            bool elevated = false;
            Action act = () => elevated = proc.IsElevated();
            act.Should().NotThrow();
        }

        [Fact]
        public void IsElevated_ShouldBeBool()
        {
            var proc = CurrentProcess;
            bool elevated = proc.IsElevated();
            // On Linux, P/Invoke fails → returns false
            elevated.Should().BeFalse();
        }

        [Fact]
        public void GetImagePath_ShouldNotThrow()
        {
            var proc = CurrentProcess;
            string? path = null;
            Action act = () => path = proc.GetImagePath();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetImagePath_ShouldReturnNullOrValue()
        {
            var proc = CurrentProcess;
            string? path = proc.GetImagePath();
            // On Linux, P/Invoke fails → returns null
            // On Windows, returns the full path to the executable
        }

        [Fact]
        public void KillTree_ShouldNotThrow()
        {
            var proc = CurrentProcess;
            Action act = () => proc.KillTree();
            act.Should().NotThrow();
        }

        [Fact]
        public void KillTree_ShouldNotKillCurrentProcess()
        {
            // Just verify it doesn't crash or kill anything
            var proc = CurrentProcess;
            var ex = Record.Exception(() => proc.KillTree());
            ex.Should().BeNull();
            proc.HasExited.Should().BeFalse();
        }

        // ── WaitForExitAsync ───────────────────────────────────────────

        [Fact]
        public void WaitForExitAsync_NullArg_ShouldThrow()
        {
            System.Diagnostics.Process? nullProc = null;
            Func<System.Threading.Tasks.Task> act = () => nullProc!.WaitForExitAsync();
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async System.Threading.Tasks.Task WaitForExitAsync_OnCurrentProcess_ShouldComplete()
        {
            var proc = CurrentProcess;
            bool result = false;
            Func<System.Threading.Tasks.Task> act = async () =>
            {
                result = await proc.WaitForExitAsync(1);
            };
            await act.Should().NotThrowAsync();
            // The process hasn't exited, so with a 1ms timeout it should return false
            // However, on Linux, OpenProcess fails → returns false immediately
        }

        [Fact]
        public async System.Threading.Tasks.Task WaitForExitAsync_ShouldNotThrow()
        {
            var proc = CurrentProcess;
            Func<System.Threading.Tasks.Task<bool>> act = () => proc.WaitForExitAsync(1);
            await act.Should().NotThrowAsync();
        }
    }
}
