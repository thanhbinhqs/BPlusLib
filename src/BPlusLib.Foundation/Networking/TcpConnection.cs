// <copyright file="TcpConnection.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Networking
{
    /// <summary>
    /// A thread-safe, full-duplex wrapper around <see cref="System.Net.Sockets.TcpClient"/>
    /// that provides synchronous and asynchronous send/receive operations.
    /// All public methods are safe to call from multiple threads concurrently;
    /// send and receive paths are independent (concurrent send+receive is allowed)
    /// but only one send or one receive executes at a time within its own path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The underlying <see cref="NetworkStream"/> is created lazily on the first
    /// send or receive operation. <see cref="ReadTimeout"/> and <see cref="WriteTimeout"/>
    /// are set on the stream before each operation.
    /// </para>
    /// <para>
    /// On .NET Framework 4.7.2, async receive uses <c>Task.Run</c> with a timed-out
    /// synchronous <see cref="NetworkStream.Read(byte[], int, int)"/> because <c>ReadAsync(CancellationToken)</c>
    /// is not available. On .NET 6.0+ the cancellation-token-aware overload is used.
    /// </para>
    /// </remarks>
    public sealed class TcpConnection : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly object _streamLock = new();
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly SemaphoreSlim _receiveLock = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpConnection"/> class,
        /// creating a new <see cref="TcpClient"/> that connects to the specified
        /// host and port.
        /// </summary>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="port">The remote port number.</param>
        internal TcpConnection(string host, int port)
        {
            _tcpClient = new TcpClient(host, port);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpConnection"/> class,
        /// wrapping an already-connected <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="tcpClient">A connected <see cref="TcpClient"/>, typically
        /// obtained from <c>TcpServer.Accept</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tcpClient"/> is <see langword="null"/>.</exception>
        internal TcpConnection(TcpClient tcpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        }

        /// <summary>
        /// Gets a value indicating whether the underlying <see cref="TcpClient"/>
        /// is connected to a remote host.
        /// </summary>
        public bool Connected
        {
            get
            {
                try
                {
                    return _tcpClient.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the number of bytes available on the underlying socket.
        /// </summary>
        public int Available
        {
            get
            {
                try
                {
                    return _tcpClient.Available;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the local endpoint, or <see langword="null"/> if the socket is
        /// not available.
        /// </summary>
        public EndPoint? LocalEndPoint
        {
            get
            {
                try
                {
                    return _tcpClient.Client?.LocalEndPoint;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the remote endpoint, or <see langword="null"/> if the socket is
        /// not available.
        /// </summary>
        public EndPoint? RemoteEndPoint
        {
            get
            {
                try
                {
                    return _tcpClient.Client?.RemoteEndPoint;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="NetworkStream"/>, creating it lazily
        /// if necessary. Thread-safe via double-checked locking.
        /// </summary>
        /// <returns>The <see cref="NetworkStream"/> for the current connection.</returns>
        private NetworkStream GetStream()
        {
            if (_stream == null)
            {
                lock (_streamLock)
                {
                    if (_stream == null)
                    {
                        _stream = _tcpClient.GetStream();
                    }
                }
            }

            return _stream;
        }

        /// <summary>
        /// Sends data synchronously over the socket.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="offset">The zero-based offset in <paramref name="data"/> at which
        /// to begin sending. Defaults to 0.</param>
        /// <param name="count">The number of bytes to send, or <see langword="null"/> to
        /// send all bytes from <paramref name="offset"/> to the end of the array.</param>
        /// <returns><see langword="true"/> if the data was sent successfully;
        /// <see langword="false"/> otherwise.</returns>
        public bool Send(byte[] data, int offset = 0, int? count = null)
        {
            _sendLock.Wait();
            try
            {
                if (data == null)
                {
                    return false;
                }

                int actualCount = count ?? (data.Length - offset);
                if (offset < 0 || offset >= data.Length || actualCount < 0 || offset + actualCount > data.Length)
                {
                    return false;
                }

                var stream = GetStream();
                stream.WriteTimeout = 5000;
                stream.Write(data, offset, actualCount);
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
        /// Receives data synchronously from the socket.
        /// </summary>
        /// <param name="bufferSize">The size of the receive buffer. Defaults to 4096.</param>
        /// <param name="timeoutMs">The read timeout in milliseconds. Defaults to 5000.</param>
        /// <returns>A byte array containing the received data, or <see langword="null"/>
        /// if the operation timed out or the connection was closed.</returns>
        public byte[]? Receive(int bufferSize = 4096, int timeoutMs = 5000)
        {
            _receiveLock.Wait();
            try
            {
                if (bufferSize <= 0)
                {
                    return null;
                }

                var stream = GetStream();
                stream.ReadTimeout = timeoutMs;

                byte[] buffer = new byte[bufferSize];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead <= 0)
                {
                    return null;
                }

                if (bytesRead == bufferSize)
                {
                    return buffer;
                }

                byte[] result = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, result, 0, bytesRead);
                return result;
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
        /// Receives a string synchronously from the socket.
        /// </summary>
        /// <param name="bufferSize">The size of the receive buffer. Defaults to 4096.</param>
        /// <param name="timeoutMs">The read timeout in milliseconds. Defaults to 5000.</param>
        /// <param name="encoding">The text encoding to use, or <see langword="null"/> to
        /// default to <see cref="Encoding.UTF8"/>.</param>
        /// <returns>The received string, or <see langword="null"/> if the operation
        /// timed out or the connection was closed.</returns>
        public string? ReceiveString(int bufferSize = 4096, int timeoutMs = 5000, Encoding? encoding = null)
        {
            byte[]? bytes = Receive(bufferSize, timeoutMs);
            if (bytes == null)
            {
                return null;
            }

            try
            {
                return (encoding ?? Encoding.UTF8).GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sends data asynchronously over the socket.
        /// </summary>
        /// <param name="data">The byte array to send.</param>
        /// <param name="offset">The zero-based offset in <paramref name="data"/> at which
        /// to begin sending. Defaults to 0.</param>
        /// <param name="count">The number of bytes to send, or <see langword="null"/> to
        /// send all bytes from <paramref name="offset"/> to the end of the array.</param>
        /// <returns>A task that represents the asynchronous send operation. The result
        /// is <see langword="true"/> if the data was sent successfully;
        /// <see langword="false"/> otherwise.</returns>
        public async Task<bool> SendAsync(byte[] data, int offset = 0, int? count = null)
        {
            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (data == null)
                {
                    return false;
                }

                int actualCount = count ?? (data.Length - offset);
                if (offset < 0 || offset >= data.Length || actualCount < 0 || offset + actualCount > data.Length)
                {
                    return false;
                }

                var stream = GetStream();
                stream.WriteTimeout = 5000;
                await stream.WriteAsync(data, offset, actualCount).ConfigureAwait(false);
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
        /// Receives data asynchronously from the socket.
        /// </summary>
        /// <param name="bufferSize">The size of the receive buffer. Defaults to 4096.</param>
        /// <param name="timeoutMs">The read timeout in milliseconds. Defaults to 5000.</param>
        /// <returns>A task that represents the asynchronous receive operation. The result
        /// is a byte array containing the received data, or <see langword="null"/>
        /// if the operation timed out or the connection was closed.</returns>
        public async Task<byte[]?> ReceiveAsync(int bufferSize = 4096, int timeoutMs = 5000)
        {
            await _receiveLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (bufferSize <= 0)
                {
                    return null;
                }

                var stream = GetStream();
                stream.ReadTimeout = timeoutMs;

                byte[] buffer = new byte[bufferSize];
                int bytesRead;

#if NET472
                // On .NET Framework 4.7.2, NetworkStream.ReadAsync does not accept
                // a CancellationToken, so we use Task.Run with the synchronous Read
                // (which respects ReadTimeout).
                var readTask = Task.Run(() => stream.Read(buffer, 0, buffer.Length));
                if (readTask.Wait(timeoutMs))
                {
                    bytesRead = readTask.Result;
                }
                else
                {
                    return null;
                }
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)
                                                   .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
#endif

                if (bytesRead <= 0)
                {
                    return null;
                }

                if (bytesRead == bufferSize)
                {
                    return buffer;
                }

                byte[] result = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, result, 0, bytesRead);
                return result;
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
        /// Receives a string asynchronously from the socket.
        /// </summary>
        /// <param name="bufferSize">The size of the receive buffer. Defaults to 4096.</param>
        /// <param name="timeoutMs">The read timeout in milliseconds. Defaults to 5000.</param>
        /// <param name="encoding">The text encoding to use, or <see langword="null"/> to
        /// default to <see cref="Encoding.UTF8"/>.</param>
        /// <returns>A task that represents the asynchronous receive operation. The result
        /// is the received string, or <see langword="null"/> if the operation timed out
        /// or the connection was closed.</returns>
        public async Task<string?> ReceiveStringAsync(int bufferSize = 4096, int timeoutMs = 5000, Encoding? encoding = null)
        {
            try
            {
                byte[]? bytes = await ReceiveAsync(bufferSize, timeoutMs).ConfigureAwait(false);
                if (bytes == null)
                {
                    return null;
                }

                return (encoding ?? Encoding.UTF8).GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Closes the underlying <see cref="NetworkStream"/> and the
        /// <see cref="TcpClient"/>, releasing all managed resources.
        /// </summary>
        public void Close()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                }
            }
            catch
            {
                // Suppress exceptions during cleanup.
            }

            try
            {
                _tcpClient.Close();
            }
            catch
            {
                // Suppress exceptions during cleanup.
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="TcpConnection"/>.
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
