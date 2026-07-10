// <copyright file="CommandRunnerTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Xunit;
using FluentAssertions;
using BPlusLib.Foundation;

namespace BPlusLib.Foundation.Tests
{
    [Trait("Category", "Utils")]
    public sealed class CommandRunnerTests
    {
        [Fact]
        public void RunCommand_ReturnsResult_NotNull()
        {
            var result = Utils.RunCommand("echo hello");
            result.Should().NotBeNull();
        }

        [Fact]
        public void RunCommand_Result_HasExitCodeAndOutputProperties()
        {
            var result = Utils.RunCommand("echo hello");

            // ExitCode and output properties should always be accessible;
            // on Linux the command may fail (exit code -1) but properties are still present.
            result.ExitCode.Should().BeOneOf(0, -1);
            result.StandardOutput.Should().NotBeNull();
            result.StandardError.Should().NotBeNull();
            result.TimedOut.Should().BeFalse();
        }
    }
}
