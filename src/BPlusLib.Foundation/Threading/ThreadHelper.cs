// <copyright file="ThreadHelper.cs" company="thanhbinhqs">
// Copyright (c) thanhbinhqs. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace BPlusLib.Foundation.Threading
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides static thread-management and synchronization helpers.
    /// All methods are cross-platform pure .NET — no P/Invoke, works on Linux, macOS, and Windows.
    /// Includes STA/MTA thread creation, UI-thread detection, marshalling, delayed execution,
    /// and thread-safe locked execution wrappers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// STA apartment state is only meaningful on Windows (COM interop). On non-Windows platforms,
    /// <see cref="Thread.SetApartmentState"/> is a no-op or may throw <see cref="PlatformNotSupportedException"/>.
    /// The STA methods are provided for compatibility with Windows-only COM scenarios.
    /// </para>
    /// <para>
    /// UI-thread detection relies on <see cref="SynchronizationContext"/> and is best-effort;
    /// it works with Windows Forms, WPF, and MAUI synchronization contexts.
    /// </para>
    /// </remarks>
    public static class ThreadHelper
    {
        /// <summary>
        /// The managed thread ID of the main (application) thread, captured at static initialization time.
        /// </summary>
        private static readonly int MainThreadId = Environment.CurrentManagedThreadId;

        /// <summary>
        /// Gets the apartment state of the current thread.
        /// </summary>
        /// <returns>The <see cref="ApartmentState"/> of the current thread.</returns>
        public static ApartmentState GetApartmentState()
        {
            try
            {
                return Thread.CurrentThread.GetApartmentState();
            }
            catch (PlatformNotSupportedException)
            {
                // On non-Windows platforms, apartment state is not supported.
                return ApartmentState.Unknown;
            }
        }

        /// <summary>
        /// Determines whether the current thread is a UI thread by inspecting
        /// <see cref="SynchronizationContext.Current"/>.
        /// Returns <c>true</c> if the current synchronization context is a
        /// <c>WindowsFormsSynchronizationContext</c> or <c>DispatcherSynchronizationContext</c>
        /// (or any context that marshals calls to a UI thread).
        /// </summary>
        /// <returns>
        /// <c>true</c> if the current thread is likely a UI thread; otherwise <c>false</c>.
        /// Note that on a non-Windows platform this returns <c>false</c> because the
        /// synchronization contexts are not present.
        /// </returns>
        public static bool IsUIThread()
        {
            var ctx = SynchronizationContext.Current;
            if (ctx is null)
            {
                return false;
            }

            // Check by type name to avoid assembly references to WindowsForms or WPF.
            var typeName = ctx.GetType().FullName;
            return typeName is "System.Windows.Forms.WindowsFormsSynchronizationContext"
                or "System.Windows.Threading.DispatcherSynchronizationContext";
        }

        /// <summary>
        /// Runs the specified action on a new STA (Single-Threaded Apartment) thread
        /// and blocks until it completes.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public static void RunInSta(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var thread = new Thread(() => action())
            {
                Name = "STA Helper Thread",
            };

            SetApartmentStateSafe(thread, ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Runs the specified function on a new STA (Single-Threaded Apartment) thread
        /// and returns its result.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <returns>The result of the function.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
        public static T? RunInSta<T>(Func<T> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            T? result = default;
            var thread = new Thread(() => { result = func(); })
            {
                Name = "STA Helper Thread",
            };

            SetApartmentStateSafe(thread, ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result;
        }

        /// <summary>
        /// Runs the specified action on a new STA thread and returns a <see cref="Task"/>
        /// that completes when the action finishes.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public static Task RunInStaAsync(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return Task.Run(() => RunInSta(action));
        }

        /// <summary>
        /// Runs the specified function on a new STA thread and returns a <see cref="Task{T}"/>
        /// that completes with the result.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <returns>A <see cref="Task{T}"/> representing the asynchronous operation with the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
        public static Task<T?> RunInStaAsync<T>(Func<T> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            return Task.Run(() => RunInSta(func));
        }

        /// <summary>
        /// Runs the specified action on a new MTA (Multi-Threaded Apartment) thread
        /// and blocks until it completes.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public static void RunInMta(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var thread = new Thread(() => action())
            {
                Name = "MTA Helper Thread",
            };

            SetApartmentStateSafe(thread, ApartmentState.MTA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Determines whether the current thread is the main (application) thread.
        /// The main thread ID is captured when this class is first initialized.
        /// </summary>
        /// <returns><c>true</c> if the current thread is the main thread; otherwise <c>false</c>.</returns>
        public static bool IsMainThread()
        {
            return Environment.CurrentManagedThreadId == MainThreadId;
        }

        /// <summary>
        /// Gets the UI <see cref="SynchronizationContext"/>, if one is available.
        /// This checks for <c>WindowsFormsSynchronizationContext</c> or
        /// <c>DispatcherSynchronizationContext</c> on the current thread.
        /// </summary>
        /// <returns>
        /// The current <see cref="SynchronizationContext"/> if it is a UI-aware context;
        /// otherwise <c>null</c>.
        /// </returns>
        public static SynchronizationContext? GetUiSynchronizationContext()
        {
            var ctx = SynchronizationContext.Current;
            if (ctx is null)
            {
                return null;
            }

            var typeName = ctx.GetType().FullName;
            if (typeName is "System.Windows.Forms.WindowsFormsSynchronizationContext"
                or "System.Windows.Threading.DispatcherSynchronizationContext")
            {
                return ctx;
            }

            return null;
        }

        /// <summary>
        /// Marshals the specified action to the UI thread using the current
        /// <see cref="SynchronizationContext"/>. If the current thread is already the UI thread,
        /// the action is executed synchronously. Otherwise it is posted to the UI synchronization context.
        /// </summary>
        /// <param name="action">The action to execute on the UI thread.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no UI synchronization context is available to marshal the action.
        /// </exception>
        public static void SwitchToUiThread(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (IsUIThread())
            {
                action();
                return;
            }

            var uiContext = GetUiSynchronizationContext();
            if (uiContext is null)
            {
                throw new InvalidOperationException(
                    "No UI synchronization context is available. " +
                    "Ensure this method is called from a UI thread (WinForms, WPF, or MAUI) " +
                    "or that a SynchronizationContext has been installed.");
            }

            uiContext.Send(_ => action(), null);
        }

        /// <summary>
        /// Executes the specified action after a specified delay on the thread pool.
        /// </summary>
        /// <param name="delayMs">The delay in milliseconds before the action is executed.</param>
        /// <param name="action">The action to execute after the delay.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="delayMs"/> is negative.
        /// </exception>
        public static void DelayExecute(int delayMs, Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (delayMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayMs), delayMs, "Delay must be non-negative.");
            }

            Task.Delay(delayMs).ContinueWith(_ => action(), TaskScheduler.Default);
        }

        /// <summary>
        /// Executes the specified function inside a lock on the given object,
        /// ensuring thread-safe access to shared state.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">The function to execute under the lock.</param>
        /// <param name="lockObject">The object to lock on.</param>
        /// <returns>The result of the function.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="func"/> or <paramref name="lockObject"/> is null.
        /// </exception>
        public static T? LockedExecute<T>(Func<T> func, object lockObject)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (lockObject is null)
            {
                throw new ArgumentNullException(nameof(lockObject));
            }

            lock (lockObject)
            {
                return func();
            }
        }

        /// <summary>
        /// Executes the specified action inside a lock on the given object,
        /// ensuring thread-safe access to shared state.
        /// </summary>
        /// <param name="action">The action to execute under the lock.</param>
        /// <param name="lockObject">The object to lock on.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="action"/> or <paramref name="lockObject"/> is null.
        /// </exception>
        public static void LockedExecute(Action action, object lockObject)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (lockObject is null)
            {
                throw new ArgumentNullException(nameof(lockObject));
            }

            lock (lockObject)
            {
                action();
            }
        }

        /// <summary>
        /// Safely sets the apartment state of a thread, swallowing
        /// <see cref="PlatformNotSupportedException"/> on non-Windows platforms.
        /// </summary>
        /// <param name="thread">The thread to configure.</param>
        /// <param name="state">The desired apartment state.</param>
        private static void SetApartmentStateSafe(Thread thread, ApartmentState state)
        {
            try
            {
                thread.SetApartmentState(state);
            }
            catch (PlatformNotSupportedException)
            {
                // Apartment state is Windows-only. This is a no-op on Linux/macOS.
            }
        }
    }
}
