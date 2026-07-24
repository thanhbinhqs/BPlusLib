// <copyright file="SingleInstanceHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>
    /// Provides a named-mutex-based single-instance guard for desktop applications.
    /// Use <see cref="Acquire"/> to check and hold the mutex, and dispose the
    /// returned <see cref="SingleInstanceGuard"/> when the application exits.
    /// </summary>
    /// <remarks>
    /// Thread-safe. All methods return null/false instead of throwing on error.
    /// </remarks>
    public static class SingleInstanceHelper
    {
        /// <summary>
        /// Checks if another instance is already running for the given app name.
        /// This does NOT acquire the mutex — it just probes.
        /// </summary>
        /// <param name="appName">Unique application name.</param>
        /// <param name="global">If true, the mutex is system-wide (all users). If false, current user only.</param>
        /// <returns>True if another instance is already running.</returns>
        public static bool IsAlreadyRunning(string appName, bool global = false)
        {
            if (string.IsNullOrEmpty(appName)) return false;

            string mutexName = global ? $"Global\\{appName}" : appName;
            bool createdNew;
            using var mutex = new Mutex(false, mutexName, out createdNew);
            return !createdNew;
        }

        /// <summary>
        /// Tries to acquire the single-instance mutex. Returns a guard that
        /// must be disposed when the app exits, or null if another instance
        /// is already running.
        /// </summary>
        /// <param name="appName">Unique application name.</param>
        /// <param name="global">If true, the mutex is system-wide.</param>
        /// <returns>A <see cref="SingleInstanceGuard"/> if this is the only instance, or null.</returns>
        public static SingleInstanceGuard? Acquire(string appName, bool global = false)
        {
            if (string.IsNullOrEmpty(appName)) return null;

            try
            {
                string mutexName = global ? $"Global\\{appName}" : appName;
                var mutex = new Mutex(false, mutexName, out bool createdNew);
                if (!createdNew)
                {
                    mutex.Dispose();
                    return null;
                }
                return new SingleInstanceGuard(mutex);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A disposable guard that holds the single-instance mutex.
    /// Dispose it when the application exits to release the mutex.
    /// </summary>
    public sealed class SingleInstanceGuard : IDisposable
    {
        private Mutex? _mutex;
        private bool _disposed;

        internal SingleInstanceGuard(Mutex mutex)
        {
            _mutex = mutex;
        }

        /// <summary>
        /// Returns true if this is the first/only instance.
        /// </summary>
        public bool IsNewInstance => _mutex is not null && !_disposed;

        /// <summary>
        /// Releases the mutex.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                try { _mutex?.ReleaseMutex(); } catch { }
                try { _mutex?.Dispose(); } catch { }
                _mutex = null;
            }
        }
    }
}
