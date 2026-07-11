// <copyright file="TcpServer.cs" company="BPlusLib">
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
    /// A thread-safe TCP listener wrapper that accepts incoming client connections
    /// with optional timeout support. Only one accept operation is active at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="System.Net.Sockets.TcpListener"/> under the hood. The
    /// <see cref="Accept(int)"/> method supports synchronous waiting with a timeout
    /// by racing the accept task against <see cref="Task.Delay(int)"/>.
    /// </para>
    /// <para>
    /// Every public method is safe to call from multiple threads concurrently.
    /// An internal <see cref="SemaphoreSlim"/> ensures that only one accept operation
    /// executes at a time.
    /// </para>
    /// </remarks>
    public sealed class TcpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly SemaphoreSlim _acceptLock = new(1, 1);
        private bool _disposed;
        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer"/> class and starts
        /// listening on the specified port and optional local address.
        /// </summary>
        /// <param name="port">The port to listen on. Pass 0 to let the OS assign a port.</param>
        /// <param name="address">
        /// The local IP address to bind to, or <see langword="null"/> to bind to all
        /// available interfaces (<see cref="IPAddress.Any"/>). Defaults to
        /// <see cref="IPAddress.Any"/>.
        /// </param>
        public TcpServer(int port, IPAddress? address = null)
        {
            var addr = address ?? IPAddress.Any;
            _listener = new TcpListener(addr, port);
            StartListener();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer"/> class from a
        /// specific local endpoint and starts listening.
        /// </summary>
        /// <param name="localEndpoint">
        /// The local <see cref="IPEndPoint"/> to bind to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localEndpoint"/> is <see langword="null"/>.
        /// </exception>
        public TcpServer(IPEndPoint localEndpoint)
        {
            if (localEndpoint is null)
                throw new ArgumentNullException(nameof(localEndpoint));

            _listener = new TcpListener(localEndpoint);
            StartListener();
        }

        /// <summary>
        /// Gets the actual port on which the server is listening. When port 0 was
        /// specified in the constructor, this returns the OS-assigned port.
        /// </summary>
        /// <value>
        /// The bound port number, or 0 if the listener has been stopped or an error
        /// occurred reading the endpoint.
        /// </value>
        public int Port
        {
            get
            {
                try
                {
                    var ep = (IPEndPoint?)_listener.LocalEndpoint;
                    return ep?.Port ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the server is currently accepting
        /// connections.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the server was started and has not been stopped
        /// or disposed; otherwise <see langword="false"/>.
        /// </value>
        public bool IsRunning
        {
            get
            {
                lock (_acceptLock)
                {
                    return _isRunning;
                }
            }
        }

        /// <summary>
        /// Accepts a pending TCP connection synchronously, with an optional timeout.
        /// </summary>
        /// <param name="timeoutMs">
        /// The maximum time in milliseconds to wait for a connection, or
        /// <see cref="Timeout.Infinite"/> (-1) to wait indefinitely. Defaults to
        /// <see cref="Timeout.Infinite"/>.
        /// </param>
        /// <returns>
        /// A <see cref="TcpConnection"/> representing the accepted client connection,
        /// or <see langword="null"/> if no connection arrived within the specified
        /// timeout or an error occurred.
        /// </returns>
        public TcpConnection? Accept(int timeoutMs = Timeout.Infinite)
        {
            _acceptLock.Wait();
            try
            {
                if (!_isRunning || _disposed)
                    return null;

                var acceptTask = _listener.AcceptTcpClientAsync();

                if (timeoutMs == Timeout.Infinite)
                {
                    // Wait indefinitely.
                    acceptTask.GetAwaiter().GetResult();
                    return new TcpConnection(acceptTask.Result);
                }

                // Race the accept task against a delay.
                var delayTask = Task.Delay(timeoutMs);
                var completed = Task.WhenAny(acceptTask, delayTask).GetAwaiter().GetResult();

                if (completed == delayTask)
                {
                    // Timeout elapsed — no connection arrived.
                    return null;
                }

                return new TcpConnection(acceptTask.GetAwaiter().GetResult());
            }
            catch
            {
                return null;
            }
            finally
            {
                _acceptLock.Release();
            }
        }

        /// <summary>
        /// Accepts a pending TCP connection asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous accept operation. The task result
        /// is a <see cref="TcpConnection"/> representing the accepted client connection,
        /// or <see langword="null"/> if the operation failed.
        /// </returns>
        public async Task<TcpConnection?> AcceptAsync()
        {
            await _acceptLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_isRunning || _disposed)
                    return null;

                var tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                return new TcpConnection(tcpClient);
            }
            catch
            {
                return null;
            }
            finally
            {
                _acceptLock.Release();
            }
        }

        /// <summary>
        /// Stops the server, preventing it from accepting new connections. Existing
        /// connections are not affected.
        /// </summary>
        public void Stop()
        {
            lock (_acceptLock)
            {
                if (!_isRunning)
                    return;

                try
                {
                    _listener.Stop();
                }
                catch
                {
                    // Suppress exceptions during stop.
                }

                _isRunning = false;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="TcpServer"/> and stops
        /// listening for new connections.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
            _acceptLock.Dispose();
        }

        /// <summary>
        /// Starts the underlying <see cref="TcpListener"/> and sets
        /// <see cref="IsRunning"/> to <see langword="true"/>.
        /// </summary>
        private void StartListener()
        {
            try
            {
                _listener.Start();
                _isRunning = true;
            }
            catch
            {
                _isRunning = false;
                throw;
            }
        }
    }
}
