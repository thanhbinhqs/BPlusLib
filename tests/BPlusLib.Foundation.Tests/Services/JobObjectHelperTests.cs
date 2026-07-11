// <copyright file="JobObjectHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Services;
using FluentAssertions;
using Xunit;

namespace BPlusLib.Foundation.Tests.Services
{
    /// <summary>
    /// Unit tests for the <see cref="JobObjectHelper"/> class.
    /// All tests are skipped on non-Windows platforms via <see cref="SkippableFactAttribute"/>.
    /// </summary>
    [Trait("Category", "Services")]
    public sealed class JobObjectHelperTests
    {
        /// <summary>
        /// Verifies that a new job object can be created without a name and disposed without error.
        /// </summary>
        [SkippableFact]
        public void CreateJob_NoName_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            using var helper = new JobObjectHelper();
            helper.Handle.Should().NotBe(IntPtr.Zero);
            helper.Name.Should().BeNull();
        }

        /// <summary>
        /// Verifies that a new job object can be created with a name and disposed without error.
        /// </summary>
        [SkippableFact]
        public void CreateJob_WithName_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            using var helper = new JobObjectHelper("TestJob_" + Guid.NewGuid().ToString("N"));
            helper.Handle.Should().NotBe(IntPtr.Zero);
            helper.Name.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that the current process can be assigned to a job object,
        /// or gracefully fails if already in a job.
        /// </summary>
        [SkippableFact]
        public void AssignCurrentProcess_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            using var helper = new JobObjectHelper();
            // Either succeeds (process was not already in a job) or
            // gracefully fails with false (already in a job — not an error).
            // The important thing is no exception is thrown.
            var exception = Record.Exception(() => helper.AssignProcessById(Environment.ProcessId));
            exception.Should().BeNull();
        }

        /// <summary>
        /// Verifies that <see cref="JobObjectHelper.SetKillOnClose"/> succeeds.
        /// </summary>
        [SkippableFact]
        public void SetKillOnClose_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            using var helper = new JobObjectHelper();
            bool result = helper.SetKillOnClose(true);
            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that <see cref="JobObjectHelper.Terminate"/> on an empty job
        /// succeeds (no-op for empty job).
        /// </summary>
        [SkippableFact]
        public void Terminate_EmptyJob_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            using var helper = new JobObjectHelper();
            bool result = helper.Terminate(0);
            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that <see cref="JobObjectHelper.IsCurrentProcessInJob"/> does not throw.
        /// </summary>
        [SkippableFact]
        public void IsCurrentProcessInJob_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            Action act = () => JobObjectHelper.IsCurrentProcessInJob();
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that calling <see cref="JobObjectHelper.Dispose"/> multiple times
        /// does not throw.
        /// </summary>
        [SkippableFact]
        public void Dispose_MultipleCalls_NoException()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var helper = new JobObjectHelper();
            helper.Dispose();
            // Second dispose should be a no-op.
            Action act = () => helper.Dispose();
            act.Should().NotThrow();
        }
    }
}
