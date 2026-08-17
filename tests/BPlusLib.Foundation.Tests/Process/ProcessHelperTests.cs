// <copyright file="ProcessHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Process;

namespace BPlusLib.Foundation.Tests.Process
{
    [Trait("Category", "Process")]
    public sealed class ProcessHelperTests
    {
        // ── CommandRunnerResult model tests ─────────────────────────────

        [Fact]
        public void CommandRunnerResult_DefaultValues_ShouldBeCorrect()
        {
            var result = new CommandRunnerResult();
            result.ExitCode.Should().Be(0);
            result.StandardOutput.Should().Be(string.Empty);
            result.StandardError.Should().Be(string.Empty);
            result.TimedOut.Should().BeFalse();
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public void CommandRunnerResult_Succeeded_DefaultsToTrue()
        {
            var result = new CommandRunnerResult();
            result.Succeeded.Should().BeTrue();
        }

        // ── RunCommand platform-specific behavior ───────────────────────

        [Fact]
        public void RunCommand_OnNonWindows_ShouldThrowPlatformNotSupportedException()
        {
            Action act = () => CommandRunner.RunCommand("echo", "hello");
            if (TestPlatform.IsWindows())
            {
                act.Should().NotThrow();
            }
            else
            {
                act.Should().Throw<PlatformNotSupportedException>();
            }
        }

        [Fact]
        public void RunCommand_WithNullFileName_ShouldThrowPlatformNotSupportedOnNonWindows()
        {
            Action act = () => CommandRunner.RunCommand(null!, "hello");
            if (TestPlatform.IsWindows())
            {
                act.Should().Throw<ArgumentNullException>();
            }
            else
            {
                act.Should().Throw<PlatformNotSupportedException>();
            }
        }

        [Fact]
        public void RunCommandAsync_OnNonWindows_ShouldThrowPlatformNotSupportedException()
        {
            Func<System.Threading.Tasks.Task> act = () => CommandRunner.RunCommandAsync("echo", "hello");
            if (TestPlatform.IsWindows())
            {
                act.Should().NotThrowAsync();
            }
            else
            {
                act.Should().ThrowAsync<PlatformNotSupportedException>();
            }
        }

        [Fact]
        public void RunCommand_WithInvalidExe_OnNonWindows_ShouldThrowPlatformNotSupportedException()
        {
            Action act = () => CommandRunner.RunCommand("nonexistent_command_xyz", "");
            if (TestPlatform.IsWindows())
            {
                act.Should().Throw<System.ComponentModel.Win32Exception>();
            }
            else
            {
                act.Should().Throw<PlatformNotSupportedException>();
            }
        }

        [Fact]
        public void RunCommand_WithTimeout_OnNonWindows_ShouldThrowPlatformNotSupportedException()
        {
            Action act = () => CommandRunner.RunCommand("sleep", "10", timeoutMs: 500);
            if (TestPlatform.IsWindows())
            {
                act.Should().NotThrow();
            }
            else
            {
                act.Should().Throw<PlatformNotSupportedException>();
            }
        }
    }
}
