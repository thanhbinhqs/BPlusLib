// <copyright file="SafeProcessHandle.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BPlusLib.Foundation.Native.SafeHandles
{
    /// <summary>
    /// Safe handle wrapper for process handles opened via OpenProcess.
    /// Uses CloseHandle for release.
    /// </summary>
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeProcessHandle"/> class.
        /// </summary>
        public SafeProcessHandle()
            : base(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeProcessHandle"/> class
        /// with an existing process handle.
        /// </summary>
        public SafeProcessHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(handle);
        }

        /// <inheritdoc />
#if NET472
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
