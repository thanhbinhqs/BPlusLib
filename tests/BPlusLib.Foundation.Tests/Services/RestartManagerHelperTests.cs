// <copyright file="RestartManagerHelperTests.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
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
    /// Unit tests for the <see cref="RestartManagerSession"/> class.
    /// All tests are skipped on non-Windows platforms.
    /// </summary>
    [Trait("Category", "Services")]
    public sealed class RestartManagerHelperTests
    {
        /// <summary>
        /// Verifies that a new RestartManager session can be created and disposed.
        /// </summary>
        [SkippableFact]
        public void CreateSession_Dispose_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var session = new RestartManagerSession();
            var disposeException = Record.Exception(() => session.Dispose());
            disposeException.Should().BeNull();
        }

        /// <summary>
        /// Verifies that Dispose can be called multiple times safely.
        /// </summary>
        [SkippableFact]
        public void Dispose_MultipleCalls_Safe()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var session = new RestartManagerSession();
            session.Dispose();
            var secondDispose = Record.Exception(() => session.Dispose());
            secondDispose.Should().BeNull();
        }

        /// <summary>
        /// Verifies that operations on a disposed session throw <see cref="ObjectDisposedException"/>.
        /// </summary>
        [SkippableFact]
        public void GetProcesses_Disposed_ThrowsObjectDisposed()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var session = new RestartManagerSession();
            session.Dispose();

            Action act = () => session.GetProcesses();
            act.Should().Throw<ObjectDisposedException>();
        }

        /// <summary>
        /// Verifies that registering a non-existent file returns false or throws gracefully.
        /// </summary>
        [SkippableFact]
        public void RegisterFiles_NonExistent_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            using var session = new RestartManagerSession();
            string nonExistentFile = @"C:\DoesNotExist_" + Guid.NewGuid().ToString("N") + ".tmp";

            // Registering a non-existent file should not throw; it just registers the path
            var exception = Record.Exception(() => session.RegisterFiles(nonExistentFile));
            exception.Should().BeNull();
        }
    }
}
