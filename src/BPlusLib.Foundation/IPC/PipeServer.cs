// <copyright file="PipeServer.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.IPC
{
    /// <summary>
    /// Thread-safe named pipe server based on Windows named pipe APIs.
    /// Supports byte-mode pipes with wait-type connections.
    /// </summary>
    public sealed class PipeServer : IDisposable
    {
        private readonly string _pipeName;
        private readonly uint _maxInstances;
        private readonly uint _bufferSize;
        private readonly object _lock = new();
        private IntPtr _handle = IntPtr.Zero;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeServer"/> class.
        /// </summary>
        /// <param name="pipeName">The named pipe name (e.g., "MyPipe").</param>
        /// <param name="maxInstances">Maximum number of concurrent instances (default: 255 = unlimited).</param>
        /// <param name="bufferSize">Output and input buffer size in bytes (default: 4096).</param>
        public PipeServer(string pipeName, uint maxInstances = Kernel32.PIPE_UNLIMITED_INSTANCES, uint bufferSize = 4096)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _maxInstances = maxInstances;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Gets the full pipe path (e.g., \\.\pipe\MyPipe).
        /// </summary>
        public string PipePath => @"\\.\pipe\" + _pipeName;

        /// <summary>
        /// Waits for a client to connect to the pipe.
        /// Creates a new pipe instance if needed.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (default: infinite = -1).</param>
        /// <returns>True if a client connected; false on timeout or failure.</returns>
        public bool WaitForConnection(int timeoutMs = -1)
        {
            lock (_lock)
            {
                CheckDisposed();

                // Create the pipe if not already created
                if (_handle == IntPtr.Zero)
                {
                    // NMPWAIT_WAIT_FOREVER = 0xFFFFFFFF for infinite timeout
                    uint defaultTimeout = timeoutMs < 0 ? 0xFFFFFFFF : (uint)Math.Max(0, timeoutMs);
                    _handle = Kernel32.CreateNamedPipeW(
                        PipePath,
                        Kernel32.PIPE_ACCESS_DUPLEX,
                        Kernel32.PIPE_TYPE_BYTE | Kernel32.PIPE_READMODE_BYTE | Kernel32.PIPE_WAIT,
                        _maxInstances,
                        _bufferSize,
                        _bufferSize,
                        defaultTimeout,
                        IntPtr.Zero);

                    if (_handle == Kernel32.INVALID_HANDLE_VALUE)
                    {
                        _handle = IntPtr.Zero;
                        return false;
                    }
                }

                // Connect the pipe (overlapped = IntPtr.Zero means blocking)
                if (!Kernel32.ConnectNamedPipe(_handle, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == Kernel32.ERROR_PIPE_CONNECTED)
                    {
                        // Client already connected; that's fine.
                        return true;
                    }

                    // On timeout or other error
                    if (error == 121 /* ERROR_SEM_TIMEOUT */ || error == Kernel32.ERROR_PIPE_LISTENING)
                    {
                        return false;
                    }

                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Reads data from the connected pipe.
        /// </summary>
        /// <param name="maxBytes">Maximum number of bytes to read (default: buffer size).</param>
        /// <returns>The read data, or null if no data or error.</returns>
        public byte[]? Read(int maxBytes = 0)
        {
            lock (_lock)
            {
                CheckDisposed();

                if (_handle == IntPtr.Zero)
                {
                    return null;
                }

                int bufferSize = maxBytes > 0 ? maxBytes : (int)_bufferSize;
                byte[] buffer = new byte[bufferSize];

                if (!Kernel32.ReadFile(_handle, buffer, (uint)bufferSize, out uint bytesRead, IntPtr.Zero))
                {
                    return null;
                }

                if (bytesRead == 0)
                {
                    return null;
                }

                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
        }

        /// <summary>
        /// Writes data to the connected pipe.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully.</returns>
        public bool Write(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            lock (_lock)
            {
                CheckDisposed();

                if (_handle == IntPtr.Zero)
                {
                    return false;
                }

                if (!Kernel32.WriteFile(_handle, data, (uint)data.Length, out uint bytesWritten, IntPtr.Zero))
                {
                    return false;
                }

                return bytesWritten == data.Length;
            }
        }

        /// <summary>
        /// Disconnects the current client, allowing the server instance to be reused.
        /// </summary>
        /// <returns>True if the client was disconnected successfully.</returns>
        public bool Disconnect()
        {
            lock (_lock)
            {
                CheckDisposed();

                if (_handle == IntPtr.Zero)
                {
                    return false;
                }

                return Kernel32.DisconnectNamedPipe(_handle);
            }
        }

        /// <summary>
        /// Impersonates the connected client's security context.
        /// </summary>
        /// <returns>True if impersonation succeeded.</returns>
        public bool ImpersonateClient()
        {
            lock (_lock)
            {
                CheckDisposed();

                if (_handle == IntPtr.Zero)
                {
                    return false;
                }

                return Kernel32.ImpersonateNamedPipeClient(_handle);
            }
        }

        /// <summary>
        /// Reverts from impersonation back to the original security context.
        /// </summary>
        /// <returns>True if reversion succeeded.</returns>
        public static bool RevertToSelf()
        {
            return Kernel32.RevertToSelf();
        }

        /// <summary>
        /// Disposes the pipe server, closing the underlying handle.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                if (_handle != IntPtr.Zero)
                {
                    Kernel32.DisconnectNamedPipe(_handle);
                    Kernel32.CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
