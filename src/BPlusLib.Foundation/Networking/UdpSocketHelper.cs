// <copyright file="UdpSocketHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BPlusLib.Foundation.Networking
{
    /// <summary>
    /// Provides static helper methods for creating UDP endpoints and performing
    /// one-shot send, receive, and broadcast operations. All methods handle errors
    /// gracefully by returning <see langword="null"/> or <see langword="false"/>
    /// on failure instead of throwing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="CreateEndpoint"/> method creates a new <see cref="UdpEndpoint"/>
    /// bound to an optional local port.
    /// </para>
    /// <para>
    /// The <see cref="SendDatagram"/>, <see cref="ReceiveDatagram"/>, and
    /// <see cref="Broadcast"/> methods create an ephemeral <see cref="UdpEndpoint"/>
    /// for a single operation and close it immediately after the operation completes
    /// or fails. These are convenience helpers for simple one-shot scenarios; for
    /// repeated or concurrent use, create a <see cref="UdpEndpoint"/> instance
    /// directly.
    /// </para>
    /// </remarks>
    public static class UdpSocketHelper
    {
        /// <summary>
        /// Creates a new <see cref="UdpEndpoint"/> bound to the specified local port.
        /// </summary>
        /// <param name="localPort">
        /// The local port to bind to, or <see langword="null"/> to let the operating
        /// system assign an ephemeral port. Defaults to <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="UdpEndpoint"/> instance, or <see langword="null"/> if
        /// the endpoint could not be created.
        /// </returns>
        public static UdpEndpoint? CreateEndpoint(int? localPort = null)
        {
            try
            {
                return new UdpEndpoint(localPort);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sends a UDP datagram to the specified remote host and port using an
        /// ephemeral local endpoint that is closed after the send completes.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        /// <param name="localPort">
        /// The local port to bind to, or <see langword="null"/> to let the OS
        /// assign an ephemeral port. Defaults to <see langword="null"/>.
        /// </param>
        /// <param name="timeoutMs">
        /// The send timeout in milliseconds. Defaults to 5000.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the datagram was sent successfully;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool SendDatagram(byte[] data, string host, int port, int? localPort = null, int timeoutMs = 5000)
        {
            if (data == null || string.IsNullOrEmpty(host))
                return false;

            if (port < 0 || port > 65535)
                return false;

            UdpEndpoint? endpoint = null;
            try
            {
                endpoint = new UdpEndpoint(localPort);
                endpoint.SendTimeout = timeoutMs;
                return endpoint.Send(data, host, port);
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    endpoint?.Dispose();
                }
                catch
                {
                    // Suppress exceptions during cleanup.
                }
            }
        }

        /// <summary>
        /// Receives a single UDP datagram on the specified local port using an
        /// ephemeral endpoint that is closed after the receive completes or times
        /// out.
        /// </summary>
        /// <param name="port">The local port to listen on.</param>
        /// <param name="timeoutMs">
        /// The receive timeout in milliseconds. Defaults to 5000. Pass
        /// <see cref="Timeout.Infinite"/> (-1) to wait indefinitely.
        /// </param>
        /// <returns>
        /// The received data as a byte array, or <see langword="null"/> if the
        /// operation timed out or an error occurred.
        /// </returns>
        public static byte[]? ReceiveDatagram(int port, int timeoutMs = 5000)
        {
            if (port < 0 || port > 65535)
                return null;

            UdpEndpoint? endpoint = null;
            try
            {
                endpoint = new UdpEndpoint(port);
                var result = endpoint.Receive(timeoutMs);
                return result?.Data;
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    endpoint?.Dispose();
                }
                catch
                {
                    // Suppress exceptions during cleanup.
                }
            }
        }

        /// <summary>
        /// Sends a UDP broadcast datagram to all interfaces on the specified port
        /// using an ephemeral local endpoint that is closed after the broadcast
        /// completes.
        /// </summary>
        /// <param name="data">The byte array to broadcast.</param>
        /// <param name="port">The remote port number to broadcast to.</param>
        /// <param name="localPort">
        /// The local port to bind to, or <see langword="null"/> to let the OS
        /// assign an ephemeral port. Defaults to <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the broadcast datagram was sent successfully;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool Broadcast(byte[] data, int port, int? localPort = null)
        {
            if (data == null)
                return false;

            if (port < 0 || port > 65535)
                return false;

            UdpEndpoint? endpoint = null;
            try
            {
                endpoint = new UdpEndpoint(localPort);
                endpoint.EnableBroadcast = true;
                return endpoint.Send(data, "255.255.255.255", port);
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    endpoint?.Dispose();
                }
                catch
                {
                    // Suppress exceptions during cleanup.
                }
            }
        }
    }
}
