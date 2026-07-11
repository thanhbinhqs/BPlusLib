// <copyright file="TcpSocketHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Networking
{
    /// <summary>
    /// Provides static helper methods for creating TCP client connections and
    /// TCP server listeners. All methods handle errors gracefully by returning
    /// <see langword="null"/> on failure instead of throwing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Connect"/> and <see cref="ConnectAsync"/> methods create a
    /// <see cref="TcpClient"/>, connect it to the specified host and port with a
    /// configurable timeout, and return a <see cref="TcpConnection"/> wrapper.
    /// </para>
    /// <para>
    /// The <see cref="StartServer"/> and <see cref="StartServerAsync"/> methods
    /// create a <see cref="TcpServer"/> that begins listening immediately.
    /// </para>
    /// </remarks>
    public static class TcpSocketHelper
    {
        /// <summary>
        /// Creates a new TCP connection to the specified host and port with a
        /// configurable timeout.
        /// </summary>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        /// <param name="timeoutMs">
        /// The maximum time in milliseconds to wait for the connection to be
        /// established. Defaults to 5000.
        /// </param>
        /// <returns>
        /// A <see cref="TcpConnection"/> wrapping the connected client, or
        /// <see langword="null"/> if the connection could not be established
        /// within the specified timeout or an error occurred.
        /// </returns>
        public static TcpConnection? Connect(string host, int port, int timeoutMs = 5000)
        {
            if (string.IsNullOrEmpty(host))
                return null;

            if (port < 0 || port > 65535)
                return null;

            var client = new TcpClient();

            try
            {
                var task = client.ConnectAsync(host, port);
                if (task.Wait(timeoutMs))
                {
                    return new TcpConnection(client);
                }

                // Timeout — close the client and return null.
                client.Close();
                return null;
            }
            catch
            {
                try
                {
                    client.Close();
                }
                catch
                {
                    // Suppress exceptions during cleanup.
                }

                return null;
            }
        }

        /// <summary>
        /// Creates a new TCP connection to the specified host and port
        /// asynchronously, with a configurable timeout.
        /// </summary>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        /// <param name="timeoutMs">
        /// The maximum time in milliseconds to wait for the connection to be
        /// established. Defaults to 5000.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous connect operation. The task
        /// result is a <see cref="TcpConnection"/> wrapping the connected client,
        /// or <see langword="null"/> if the connection could not be established
        /// within the specified timeout or an error occurred.
        /// </returns>
        public static async Task<TcpConnection?> ConnectAsync(string host, int port, int timeoutMs = 5000)
        {
            if (string.IsNullOrEmpty(host))
                return null;

            if (port < 0 || port > 65535)
                return null;

            var client = new TcpClient();

            try
            {
#if NET472
                // On .NET Framework 4.7.2, TcpClient.ConnectAsync does not accept
                // a CancellationToken, so we race against a delay task.
                var connectTask = client.ConnectAsync(host, port);
                var delayTask = Task.Delay(timeoutMs);
                var completed = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                if (completed == delayTask)
                {
                    // Timeout elapsed — close the client and return null.
                    client.Close();
                    return null;
                }

                // Propagate any exception from the connect task.
                await connectTask.ConfigureAwait(false);
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
#endif

                return new TcpConnection(client);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    client.Close();
                }
                catch
                {
                    // Suppress exceptions during cleanup.
                }

                return null;
            }
            catch
            {
                try
                {
                    client.Close();
                }
                catch
                {
                    // Suppress exceptions during cleanup.
                }

                return null;
            }
        }

        /// <summary>
        /// Creates and starts a TCP server that listens on the specified port
        /// and optional local address.
        /// </summary>
        /// <param name="port">
        /// The port to listen on. Pass 0 to let the OS assign a port.
        /// </param>
        /// <param name="address">
        /// The local IP address to bind to, or <see langword="null"/> to bind
        /// to all available interfaces (<see cref="IPAddress.Any"/>).
        /// Defaults to <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A <see cref="TcpServer"/> that is already listening for incoming
        /// connections, or <see langword="null"/> if the server could not be
        /// started.
        /// </returns>
        public static TcpServer? StartServer(int port, IPAddress? address = null)
        {
            try
            {
                return new TcpServer(port, address);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates and starts a TCP server asynchronously that listens on the
        /// specified port and optional local address.
        /// </summary>
        /// <param name="port">
        /// The port to listen on. Pass 0 to let the OS assign a port.
        /// </param>
        /// <param name="address">
        /// The local IP address to bind to, or <see langword="null"/> to bind
        /// to all available interfaces (<see cref="IPAddress.Any"/>).
        /// Defaults to <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous start operation. The task
        /// result is a <see cref="TcpServer"/> that is already listening for
        /// incoming connections, or <see langword="null"/> if the server could
        /// not be started.
        /// </returns>
        /// <remarks>
        /// <see cref="TcpListener.Start()"/> is a synchronous operation; this
        /// method offloads it to the thread pool via <see cref="Task.Run(Action)"/>.
        /// </remarks>
        public static Task<TcpServer?> StartServerAsync(int port, IPAddress? address = null)
        {
            try
            {
                return Task.Run(() => StartServer(port, address));
            }
            catch
            {
                return Task.FromResult<TcpServer?>(null);
            }
        }
    }
}
