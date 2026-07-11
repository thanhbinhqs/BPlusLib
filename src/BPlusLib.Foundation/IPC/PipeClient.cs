// <copyright file="PipeClient.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.IPC
{
    /// <summary>
    /// Named pipe client for connecting to a local Windows named pipe server.
    /// Uses CallNamedPipeW for one-shot transactions and CreateFileW for session-oriented communication.
    /// </summary>
    public sealed class PipeClient : IDisposable
    {
        private readonly string _pipeName;
        private readonly string _pipePath;
        private readonly object _lock = new();
        private IntPtr _handle = IntPtr.Zero;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeClient"/> class.
        /// </summary>
        /// <param name="pipeName">The named pipe name (e.g., "MyPipe").</param>
        public PipeClient(string pipeName)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _pipePath = @"\\.\pipe\" + _pipeName;
        }

        /// <summary>
        /// Gets the full pipe path.
        /// </summary>
        public string PipePath => _pipePath;

        /// <summary>
        /// Gets whether the client is currently connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    return _handle != IntPtr.Zero && _handle != Kernel32.INVALID_HANDLE_VALUE;
                }
            }
        }

        /// <summary>
        /// Connects to the named pipe server. Waits for the pipe to become available if busy.
        /// </summary>
        /// <param name="timeoutMs">Time to wait in milliseconds (default: 10000).</param>
        /// <returns>True if connected successfully.</returns>
        public bool Connect(int timeoutMs = 10000)
        {
            lock (_lock)
            {
                CheckDisposed();

                // Wait for the pipe to become available
                if (!Kernel32.WaitNamedPipeW(_pipePath, (uint)timeoutMs))
                {
                    return false;
                }

                // Use CreateFileW to open the pipe
                _handle = Kernel32.CreateFileW(
                    _pipePath,
                    Kernel32.GENERIC_READ | Kernel32.GENERIC_WRITE,
                    0, // no sharing
                    IntPtr.Zero, // default security
                    Kernel32.OPEN_EXISTING,
                    0, // no special attributes
                    IntPtr.Zero); // no template

                if (_handle == Kernel32.INVALID_HANDLE_VALUE)
                {
                    _handle = IntPtr.Zero;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Reads data from the connected pipe.
        /// </summary>
        /// <param name="maxBytes">Maximum number of bytes to read (default: 4096).</param>
        /// <returns>The read data, or null if no data or error.</returns>
        public byte[]? Read(int maxBytes = 4096)
        {
            lock (_lock)
            {
                CheckDisposed();

                if (_handle == IntPtr.Zero)
                {
                    return null;
                }

                byte[] buffer = new byte[maxBytes];

                if (!Kernel32.ReadFile(_handle, buffer, (uint)maxBytes, out uint bytesRead, IntPtr.Zero))
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
        /// Disconnects and disposes the pipe client.
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
