// <copyright file="SafeLibraryHandle.cs" company="BPlusLib">
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
    /// Safe handle wrapper for HMODULE / library handles returned by LoadLibrary.
    /// Uses FreeLibrary for release.
    /// </summary>
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeLibraryHandle"/> class.
        /// </summary>
        public SafeLibraryHandle()
            : base(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeLibraryHandle"/> class
        /// with an existing handle value.
        /// </summary>
        public SafeLibraryHandle(IntPtr handle, bool ownsHandle)
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
            return FreeLibrary(handle);
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}
