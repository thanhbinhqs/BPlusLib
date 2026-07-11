// <copyright file="UdpEndpoint.cs" company="BPlusLib">
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
    /// A thread-safe, stateful wrapper around <see cref="System.Net.Sockets.UdpClient"/>
    /// that provides synchronous and asynchronous send, receive, broadcast, and multicast
    /// operations. All public methods are safe to call from multiple threads concurrently;
    /// send and receive paths are independent and each protected by their own
    /// <see cref="SemaphoreSlim"/> so that only one send or one receive executes at a time
    /// within its own path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The underlying <see cref="UdpClient"/> is created in the constructor and closed
    /// in <see cref="Close"/> or <see cref="Dispose"/>.
    /// </para>
    /// <para>
    /// All public methods wrap operation code in <c>try/catch</c> and return a failure
    /// indicator (<see langword="null"/> or <see langword="false"/>) on any exception.
    /// </para>
    /// <para>
    /// Multicast operations (<see cref="JoinMulticastGroup"/> and
    /// <see cref="DropMulticastGroup"/>) work on all target frameworks.
    /// </para>
    /// </remarks>
    public sealed class UdpEndpoint : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly SemaphoreSlim _receiveLock = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpEndpoint"/> class,
        /// creating a <see cref="UdpClient"/> that binds to an optional local port.
        /// </summary>
        /// <param name="localPort">
        /// The local port to bind to, or <see langword="null"/> to let the operating
        /// system assign an ephemeral port.
        /// </param>
        public UdpEndpoint(int? localPort = null)
        {
            _udpClient = localPort.HasValue
                ? new UdpClient(localPort.Value)
                : new UdpClient(0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpEndpoint"/> class,
        /// creating a <see cref="UdpClient"/> that binds to the specified local
        /// port and IP address.
        /// </summary>
        /// <param name="localPort">The local port to bind to.</param>
        /// <param name="localAddress">
        /// The local IP address to bind to, or <see langword="null"/> to bind to
        /// all available interfaces (<see cref="IPAddress.Any"/>). Defaults to
        /// <see cref="IPAddress.Any"/>.
        /// </param>
        public UdpEndpoint(int localPort, IPAddress? localAddress = null)
        {
            var addr = localAddress ?? IPAddress.Any;
            var localEp = new IPEndPoint(addr, localPort);
            _udpClient = new UdpClient(localEp);
        }

        /// <summary>
        /// Gets the actual port on which the endpoint is bound. When an ephemeral
        /// port was requested (constructor <c>localPort</c> was <see langword="null"/>),
        /// this returns the OS-assigned port.
        /// </summary>
        /// <value>
        /// The bound port number, or 0 if the socket has been disposed or an error
        /// occurred reading the endpoint.
        /// </value>
        public int Port
        {
            get
            {
                try
                {
                    var ep = (IPEndPoint?)_udpClient.Client?.LocalEndPoint;
                    return ep?.Port ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the
        /// underlying <see cref="UdpClient"/> can send broadcast packets.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the <see cref="UdpClient"/> can send broadcasts;
        /// otherwise <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool EnableBroadcast
        {
            get
            {
                try
                {
                    return _udpClient.EnableBroadcast;
                }
                catch
                {
                    return false;
                }
            }

            set
            {
                try
                {
                    _udpClient.EnableBroadcast = value;
                }
                catch
                {
                    // Suppress exceptions during property set.
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies the amount of time (in milliseconds)
        /// after which a synchronous send operation times out.
        /// </summary>
        /// <value>
        /// The send timeout in milliseconds. The default is 0, which indicates an
        /// infinite time-out period.
        /// </value>
        public int SendTimeout
        {
            get
            {
                try
                {
                    return _udpClient.Client?.SendTimeout ?? 0;
                }
                catch
                {
                    return 0;
                }
            }

            set
            {
                try
                {
                    if (_udpClient.Client != null)
                    {
                        _udpClient.Client.SendTimeout = value;
                    }
                }
                catch
                {
                    // Suppress exceptions during property set.
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies the amount of time (in milliseconds)
        /// after which a synchronous receive operation times out.
        /// </summary>
        /// <value>
        /// The receive timeout in milliseconds. The default is 0, which indicates an
        /// infinite time-out period.
        /// </value>
        public int ReceiveTimeout
        {
            get
            {
                try
                {
                    return _udpClient.Client?.ReceiveTimeout ?? 0;
                }
                catch
                {
                    return 0;
                }
            }

            set
            {
                try
                {
                    if (_udpClient.Client != null)
                    {
                        _udpClient.Client.ReceiveTimeout = value;
                    }
                }
                catch
                {
                    // Suppress exceptions during property set.
                }
            }
        }

        /// <summary>
        /// Sends a UDP datagram synchronously to the specified remote host and port.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        /// <returns><see langword="true"/> if the datagram was sent successfully;
        /// <see langword="false"/> otherwise.</returns>
        public bool Send(byte[] data, string host, int port)
        {
            _sendLock.Wait();
            try
            {
                if (data == null || host == null)
                {
                    return false;
                }

                _udpClient.Send(data, data.Length, host, port);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Sends a UDP datagram synchronously to the specified remote endpoint.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="remoteEndpoint">The remote <see cref="EndPoint"/> to send to.</param>
        /// <returns><see langword="true"/> if the datagram was sent successfully;
        /// <see langword="false"/> otherwise.</returns>
        public bool Send(byte[] data, EndPoint remoteEndpoint)
        {
            _sendLock.Wait();
            try
            {
                if (data == null || remoteEndpoint == null)
                {
                    return false;
                }

                if (remoteEndpoint is not IPEndPoint ipEndpoint)
                {
                    return false;
                }

                _udpClient.Send(data, data.Length, ipEndpoint);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Sends a UDP datagram synchronously to the specified remote host and port
        /// using a segment of the data buffer.
        /// </summary>
        /// <param name="data">The byte array containing the data to send.</param>
        /// <param name="offset">The zero-based offset in <paramref name="data"/> at which
        /// to begin sending.</param>
        /// <param name="count">The number of bytes to send.</param>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        /// <returns><see langword="true"/> if the datagram was sent successfully;
        /// <see langword="false"/> otherwise.</returns>
        public bool Send(byte[] data, int offset, int count, string host, int port)
        {
            _sendLock.Wait();
            try
            {
                if (data == null || host == null)
                {
                    return false;
                }

                if (offset < 0 || offset >= data.Length || count < 0)
                {
                    return false;
                }

                // Slice the array for the send operation.
                byte[] segment;
                if (offset == 0 && count == data.Length)
                {
                    segment = data;
                }
                else
                {
                    segment = new byte[count];
                    Buffer.BlockCopy(data, offset, segment, 0, count);
                }

                _udpClient.Send(segment, segment.Length, host, port);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Receives a UDP datagram synchronously from any remote host, with a
        /// configurable timeout.
        /// </summary>
        /// <param name="timeoutMs">
        /// The receive timeout in milliseconds. Defaults to 5000. Pass
        /// <see cref="Timeout.Infinite"/> (-1) to wait indefinitely.
        /// </param>
        /// <returns>
        /// A tuple containing the received data and the remote endpoint of the sender,
        /// or <see langword="null"/> if the operation timed out or failed.
        /// Returns <see langword="null"/> on timeout.
        /// </returns>
        public (byte[]? Data, IPEndPoint? RemoteEndPoint)? Receive(int timeoutMs = 5000)
        {
            _receiveLock.Wait();
            try
            {
                if (timeoutMs >= 0)
                {
                    _udpClient.Client.ReceiveTimeout = timeoutMs;
                }

                IPEndPoint? remoteEp = null;
                byte[] data = _udpClient.Receive(ref remoteEp);
                return (data, remoteEp);
            }
            catch
            {
                return null;
            }
            finally
            {
                _receiveLock.Release();
            }
        }

        /// <summary>
        /// Sends a UDP datagram asynchronously to the specified remote host and port.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        /// <returns>
        /// A task that represents the asynchronous send operation. The result is
        /// <see langword="true"/> if the datagram was sent successfully;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public async Task<bool> SendAsync(byte[] data, string host, int port)
        {
            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (data == null || host == null)
                {
                    return false;
                }

                int bytesSent = await _udpClient.SendAsync(data, data.Length, host, port)
                                                 .ConfigureAwait(false);
                return bytesSent > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Sends a UDP datagram asynchronously to the specified remote endpoint.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="remoteEndpoint">The remote <see cref="EndPoint"/> to send to.</param>
        /// <returns>
        /// A task that represents the asynchronous send operation. The result is
        /// <see langword="true"/> if the datagram was sent successfully;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public async Task<bool> SendAsync(byte[] data, EndPoint remoteEndpoint)
        {
            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (data == null || remoteEndpoint == null)
                {
                    return false;
                }

                if (remoteEndpoint is not IPEndPoint ipEndpoint)
                {
                    return false;
                }

                int bytesSent = await _udpClient.SendAsync(data, data.Length, ipEndpoint)
                                                 .ConfigureAwait(false);
                return bytesSent > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Receives a UDP datagram asynchronously from any remote host, with a
        /// configurable timeout.
        /// </summary>
        /// <param name="timeoutMs">
        /// The receive timeout in milliseconds. Defaults to 5000. Pass
        /// <see cref="Timeout.Infinite"/> (-1) to wait indefinitely.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous receive operation. The task result
        /// is a tuple containing the received data and the remote endpoint of the sender,
        /// or <see langword="null"/> if the operation timed out or failed.
        /// </returns>
        public async Task<(byte[]? Data, IPEndPoint? RemoteEndPoint)?> ReceiveAsync(int timeoutMs = 5000)
        {
            await _receiveLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Task<UdpReceiveResult> receiveTask = _udpClient.ReceiveAsync();

                if (timeoutMs == Timeout.Infinite)
                {
                    UdpReceiveResult result = await receiveTask.ConfigureAwait(false);
                    return (result.Buffer, result.RemoteEndPoint);
                }

                var delayTask = Task.Delay(timeoutMs);
                Task completed = await Task.WhenAny(receiveTask, delayTask)
                                           .ConfigureAwait(false);

                if (completed == delayTask)
                {
                    return null;
                }

                UdpReceiveResult finalResult = await receiveTask.ConfigureAwait(false);
                return (finalResult.Buffer, finalResult.RemoteEndPoint);
            }
            catch
            {
                return null;
            }
            finally
            {
                _receiveLock.Release();
            }
        }

        /// <summary>
        /// Joins a multicast group.
        /// </summary>
        /// <param name="multicastAddress">
        /// The <see cref="IPAddress"/> of the multicast group to join.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the multicast group was joined successfully;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public bool JoinMulticastGroup(IPAddress multicastAddress)
        {
            try
            {
                if (multicastAddress == null)
                {
                    return false;
                }

                _udpClient.JoinMulticastGroup(multicastAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Drops membership of a multicast group.
        /// </summary>
        /// <param name="multicastAddress">
        /// The <see cref="IPAddress"/> of the multicast group to leave.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the multicast group was dropped successfully;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public bool DropMulticastGroup(IPAddress multicastAddress)
        {
            try
            {
                if (multicastAddress == null)
                {
                    return false;
                }

                _udpClient.DropMulticastGroup(multicastAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Closes the underlying <see cref="UdpClient"/>, releasing all associated
        /// resources. This method is safe to call multiple times.
        /// </summary>
        public void Close()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _udpClient.Close();
            }
            catch
            {
                // Suppress exceptions during cleanup.
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="UdpEndpoint"/> and closes
        /// the underlying <see cref="UdpClient"/>.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Close();
            _sendLock.Dispose();
            _receiveLock.Dispose();
        }
    }
}
