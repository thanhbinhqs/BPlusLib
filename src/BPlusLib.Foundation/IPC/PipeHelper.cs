// <copyright file="PipeHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.IPC
{
    /// <summary>
    /// Static helper methods for Windows named pipe operations.
    /// </summary>
    public static class PipeHelper
    {
        /// <summary>
        /// Performs a one-shot named pipe transaction: sends a request and receives a response.
        /// Uses <see cref="Kernel32.CallNamedPipeW"/> internally.
        /// </summary>
        /// <param name="pipeName">The named pipe name (e.g., "MyPipe").</param>
        /// <param name="request">The data to send.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 10000).</param>
        /// <returns>The response data, or null if the transaction failed or timed out.</returns>
        public static byte[]? Transact(string pipeName, byte[] request, int timeoutMs = 10000)
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new ArgumentException("Pipe name cannot be null or empty.", nameof(pipeName));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string pipePath = @"\\.\pipe\" + pipeName;
            byte[] outputBuffer = new byte[4096];

            if (Kernel32.CallNamedPipeW(
                    pipePath,
                    request,
                    (uint)request.Length,
                    outputBuffer,
                    (uint)outputBuffer.Length,
                    out uint bytesRead,
                    (uint)timeoutMs))
            {
                if (bytesRead == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] result = new byte[bytesRead];
                Array.Copy(outputBuffer, result, bytesRead);
                return result;
            }

            return null;
        }

        /// <summary>
        /// Checks whether a named pipe of the given name currently exists on the local machine.
        /// Uses <see cref="Kernel32.WaitNamedPipeW"/> with a zero timeout.
        /// </summary>
        /// <param name="pipeName">The named pipe name (e.g., "MyPipe").</param>
        /// <returns>True if the pipe exists (has at least one instance available).</returns>
        public static bool PipeExists(string pipeName)
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                return false;
            }

            string pipePath = @"\\.\pipe\" + pipeName;

            // WaitNamedPipeW with zero timeout returns immediately.
            // If pipe exists, it returns true (even if pipe is busy).
            return Kernel32.WaitNamedPipeW(pipePath, 0);
        }
    }
}
