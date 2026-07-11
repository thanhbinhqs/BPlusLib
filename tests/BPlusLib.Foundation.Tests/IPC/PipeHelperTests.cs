// <copyright file="PipeHelperTests.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BPlusLib.Foundation.IPC;
using FluentAssertions;
using Xunit;

namespace BPlusLib.Foundation.Tests.IPC
{
    /// <summary>
    /// Unit tests for the PipeServer, PipeClient, and PipeHelper classes.
    /// All tests are skipped on non-Windows platforms.
    /// </summary>
    [Trait("Category", "IPC")]
    public sealed class PipeHelperTests
    {
        private static string UniquePipeName => "BPlusLibTest_" + Guid.NewGuid().ToString("N");

        /// <summary>
        /// Verifies a basic roundtrip: server creates pipe, client connects,
        /// sends a message, and server receives it.
        /// </summary>
        [SkippableFact]
        public void ClientServer_Roundtrip()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string pipeName = UniquePipeName;
            using var server = new PipeServer(pipeName);
            using var client = new PipeClient(pipeName);

            // Server waits for connection in background
            var serverReady = new ManualResetEventSlim(false);
            byte[]? received = null;

            var serverThread = new Thread(() =>
            {
                if (server.WaitForConnection(5000))
                {
                    serverReady.Set();
                    received = server.Read(4096);
                }
            });
            serverThread.Start();

            // Client connects and sends
            client.Connect(5000).Should().BeTrue();
            serverReady.Wait(5000);

            byte[] message = Encoding.UTF8.GetBytes("Hello from client!");
            client.Write(message).Should().BeTrue();

            serverThread.Join(5000);

            received.Should().NotBeNull();
            Encoding.UTF8.GetString(received!).Should().Be("Hello from client!");
        }

        /// <summary>
        /// Verifies that multiple messages can be sent and received over the same pipe connection.
        /// </summary>
        [SkippableFact]
        public void ClientServer_MultipleMessages()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string pipeName = UniquePipeName;
            using var server = new PipeServer(pipeName);
            using var client = new PipeClient(pipeName);

            var receivedMessages = new System.Collections.Concurrent.ConcurrentBag<string>();

            var serverThread = new Thread(() =>
            {
                if (server.WaitForConnection(5000))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var data = server.Read(4096);
                        if (data != null)
                        {
                            receivedMessages.Add(Encoding.UTF8.GetString(data));
                        }
                    }
                }
            });
            serverThread.Start();

            client.Connect(5000).Should().BeTrue();

            client.Write(Encoding.UTF8.GetBytes("Msg1")).Should().BeTrue();
            client.Write(Encoding.UTF8.GetBytes("Msg2")).Should().BeTrue();
            client.Write(Encoding.UTF8.GetBytes("Msg3")).Should().BeTrue();

            serverThread.Join(5000);

            receivedMessages.Should().HaveCount(3);
            receivedMessages.Should().Contain("Msg1");
            receivedMessages.Should().Contain("Msg2");
            receivedMessages.Should().Contain("Msg3");
        }

        /// <summary>
        /// Verifies that PipeExists returns false for a non-existent pipe.
        /// </summary>
        [SkippableFact]
        public void PipeExists_NonExistent_ReturnsFalse()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string nonExistentPipe = "PipeThatDoesNotExist_" + Guid.NewGuid().ToString("N");
            bool exists = PipeHelper.PipeExists(nonExistentPipe);
            exists.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that Dispose can be called safely on both server and client,
        /// including after the first dispose (no exception).
        /// </summary>
        [SkippableFact]
        public void Dispose_Safe()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var server = new PipeServer(UniquePipeName);
            var client = new PipeClient(UniquePipeName);

            // First dispose should succeed
            var serverDisposeException = Record.Exception(() => server.Dispose());
            serverDisposeException.Should().BeNull();

            var clientDisposeException = Record.Exception(() => client.Dispose());
            clientDisposeException.Should().BeNull();

            // Second dispose should also be safe
            serverDisposeException = Record.Exception(() => server.Dispose());
            serverDisposeException.Should().BeNull();

            clientDisposeException = Record.Exception(() => client.Dispose());
            clientDisposeException.Should().BeNull();
        }

        /// <summary>
        /// Verifies that Transact times out when connecting to a non-existent pipe.
        /// </summary>
        [SkippableFact]
        public void Transact_Timeout_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string nonExistentPipe = "PipeDoesNotExist_" + Guid.NewGuid().ToString("N");
            byte[] request = Encoding.UTF8.GetBytes("test");

            byte[]? result = PipeHelper.Transact(nonExistentPipe, request, timeoutMs: 100);
            result.Should().BeNull();
        }
    }
}
