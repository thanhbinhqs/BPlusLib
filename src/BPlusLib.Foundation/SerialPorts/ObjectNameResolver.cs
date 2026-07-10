// <copyright file="ObjectNameResolver.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.SerialPorts
{
    internal sealed class ObjectNameResolver : IDisposable
    {
        private byte[]? _objectBuffer;
        private bool _disposed;

        internal string? ResolveObjectName(IntPtr handle)
        {
            if (handle == IntPtr.Zero || handle == NativeMethods.InvalidHandleValue)
                return null;

            int bufferSize = _objectBuffer?.Length ?? 256;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (_objectBuffer == null || _objectBuffer.Length < bufferSize)
                    _objectBuffer = new byte[bufferSize];

                GCHandle pin = default;
                try
                {
                    pin = GCHandle.Alloc(_objectBuffer, GCHandleType.Pinned);
                    IntPtr bufferPtr = pin.AddrOfPinnedObject();

                    int status = NativeMethods.NtQueryObject(
                        handle, NativeMethods.ObjectNameInformation,
                        bufferPtr, _objectBuffer.Length, out int returnLength);

                    if (NativeMethods.NtSuccess(status))
                    {
                        var nameInfo = Marshal.PtrToStructure<ObjectNameInformation>(bufferPtr);
                        string? name = Utilities.UnicodeStringToString(nameInfo.Name);
                        if (!string.IsNullOrEmpty(name)) return name;
                        return null;
                    }

                    if (status == NativeMethods.StatusBufferOverflow ||
                        status == NativeMethods.StatusBufferTooSmall ||
                        status == NativeMethods.StatusInfoLengthMismatch)
                    {
                        if (returnLength > bufferSize && returnLength <= NativeMethods.MaxObjectNameChars * 2)
                        { bufferSize = returnLength; continue; }
                        int newSize = bufferSize * 2;
                        if (newSize > NativeMethods.MaxObjectNameChars * 4) return null;
                        bufferSize = newSize;
                        continue;
                    }
                    return null;
                }
                catch { return null; }
                finally { if (pin.IsAllocated) pin.Free(); }
            }
            return null;
        }

        public void Dispose()
        {
            if (!_disposed) { _objectBuffer = null; _disposed = true; }
        }
    }
}