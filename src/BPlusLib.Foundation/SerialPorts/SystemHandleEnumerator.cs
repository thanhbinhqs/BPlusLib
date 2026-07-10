// <copyright file="SystemHandleEnumerator.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.SerialPorts
{
    internal sealed class SystemHandleEnumerator : IDisposable
    {
        private byte[]? _buffer;
        private GCHandle _pin;
        private bool _disposed;

        internal IntPtr BufferAddress { get; private set; }
        internal int HandleCount { get; private set; }
        internal int FirstEntryOffset { get; private set; }

        internal bool EnumerateAllHandles()
        {
            ReleasePreviousBuffer();
            int currentSize = NativeMethods.InitialHandleBufferSize;

            while (currentSize <= NativeMethods.MaxHandleBufferSize)
            {
                _buffer = new byte[currentSize];
                _pin = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
                BufferAddress = _pin.AddrOfPinnedObject();

                int status = NativeMethods.NtQuerySystemInformation(
                    NativeMethods.SystemExtendedHandleInformation,
                    BufferAddress, currentSize, out int returnedLength);

                if (NativeMethods.NtSuccess(status))
                {
                    var header = Marshal.PtrToStructure<SystemExtendedHandleInformation>(BufferAddress);
                    HandleCount = header.NumberOfHandles;
                    FirstEntryOffset = Utilities.GetHandleInfoHeaderSize();
                    return HandleCount > 0;
                }

                _pin.Free();
                _buffer = null;
                BufferAddress = IntPtr.Zero;
                HandleCount = 0;

                if (status == NativeMethods.StatusInfoLengthMismatch ||
                    status == NativeMethods.StatusBufferTooSmall ||
                    status == NativeMethods.StatusBufferOverflow)
                {
                    int nextSize = currentSize * 2;
                    if (nextSize > NativeMethods.MaxHandleBufferSize)
                        nextSize = NativeMethods.MaxHandleBufferSize;
                    if (nextSize <= currentSize) break;
                    currentSize = nextSize;
                    continue;
                }
                break;
            }
            return false;
        }

        internal SystemExtendedInformationHandleEntry GetEntry(int index)
        {
            int offset = FirstEntryOffset + (index * Utilities.HandleEntrySize);
            IntPtr address = IntPtr.Add(BufferAddress, offset);
            return Marshal.PtrToStructure<SystemExtendedInformationHandleEntry>(address);
        }

        private void ReleasePreviousBuffer()
        {
            if (_buffer != null)
            {
                if (_pin.IsAllocated) _pin.Free();
                _buffer = null;
                BufferAddress = IntPtr.Zero;
                HandleCount = 0;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ReleasePreviousBuffer();
                _disposed = true;
            }
        }
    }
}