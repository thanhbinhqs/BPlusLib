// <copyright file="RestartManagerHelperTests.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
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
        private static RestartManagerSession CreateSessionOrSkip()
        {
            try
            {
                return new RestartManagerSession();
            }
            catch (Win32Exception ex)
            {
                Skip.If(true, $"Restart Manager unavailable on this machine: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Verifies that a new RestartManager session can be created and disposed.
        /// </summary>
        [SkippableFact]
        public void CreateSession_Dispose_Succeeds()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            var session = CreateSessionOrSkip();
            var disposeException = Record.Exception(() => session.Dispose());
            disposeException.Should().BeNull();
        }

        /// <summary>
        /// Verifies that Dispose can be called multiple times safely.
        /// </summary>
        [SkippableFact]
        public void Dispose_MultipleCalls_Safe()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            var session = CreateSessionOrSkip();
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
            Skip.IfNot(TestPlatform.IsWindows());

            var session = CreateSessionOrSkip();
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
            Skip.IfNot(TestPlatform.IsWindows());

            using var session = CreateSessionOrSkip();
            string nonExistentFile = @"C:\DoesNotExist_" + Guid.NewGuid().ToString("N") + ".tmp";

            // Registering a non-existent file should not throw; it just registers the path
            var exception = Record.Exception(() => session.RegisterFiles(nonExistentFile));
            exception.Should().BeNull();
        }
    }
}
