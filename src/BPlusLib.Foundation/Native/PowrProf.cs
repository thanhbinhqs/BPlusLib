// <copyright file="PowrProf.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for powrprof.dll — power management and
    /// sleep/hibernate operations.
    /// </summary>
    internal static class PowrProf
    {
        /// <summary>
        /// Suspends the system (sleep or hibernate).
        /// </summary>
        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetSuspendState(
            [MarshalAs(UnmanagedType.Bool)] bool hibernate,
            [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
            [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);
    }
}
