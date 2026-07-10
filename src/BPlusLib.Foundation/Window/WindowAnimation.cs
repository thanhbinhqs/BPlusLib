// <copyright file="WindowAnimation.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Window
{
    /// <summary>
    /// Provides window animation utilities: flash, shake, fade-in, and fade-out.
    /// All methods are asynchronous and support cancellation.
    /// </summary>
    public static class WindowAnimation
    {
        /// <summary>
        /// Flashes the form's caption and taskbar button a specified number of times.
        /// </summary>
        /// <param name="form">The form to flash.</param>
        /// <param name="flashCount">The number of flash cycles. Default is 3.</param>
        /// <param name="ct">A cancellation token to stop the flash early.</param>
        /// <returns>A task that completes when the flash operation finishes.</returns>
        public static Task FlashAsync(Form form, int flashCount = 3, CancellationToken ct = default)
        {
            Guard.ThrowIfNull(form);
            Guard.ThrowIfOutOfRange(flashCount, 1, 100);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!form.IsHandleCreated)
            {
                tcs.TrySetException(new InvalidOperationException("Form handle is not created."));
                return tcs.Task;
            }

            var info = new FLASHWINFO();
            info.Init();
            info.hwnd = form.Handle;
            info.dwFlags = User32.FLASHW_ALL;
            info.uCount = (uint)flashCount;
            info.dwTimeout = 0;

            if (!User32.FlashWindowEx(ref info))
            {
                tcs.TrySetException(new InvalidOperationException("FlashWindowEx failed."));
                return tcs.Task;
            }

            // Register cancellation to stop flashing
            ct.Register(() =>
            {
                if (form.IsHandleCreated && !form.IsDisposed)
                {
                    var stopInfo = new FLASHWINFO();
                    stopInfo.Init();
                    stopInfo.hwnd = form.Handle;
                    stopInfo.dwFlags = User32.FLASHW_STOP;
                    User32.FlashWindowEx(ref stopInfo);
                }

                tcs.TrySetCanceled(ct);
            });

            // FlashWindowEx runs asynchronously; we complete after a short delay
            // that approximates the flash duration so the caller can await.
            var estimatedDelay = Math.Max(100, flashCount * 200);
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(estimatedDelay, ct).ConfigureAwait(false);
                    tcs.TrySetResult(true);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled(ct);
                }
            }, ct);

            return tcs.Task;
        }

        /// <summary>
        /// Shakes the form horizontally by the specified intensity.
        /// </summary>
        /// <param name="form">The form to shake.</param>
        /// <param name="intensity">The shake offset in pixels. Default is 5.</param>
        /// <param name="duration">The duration of the shake effect. Default is 300 ms.</param>
        /// <param name="ct">A cancellation token to stop the shake early.</param>
        /// <returns>A task that completes when the shake finishes.</returns>
        public static Task ShakeAsync(Form form, int intensity = 5, TimeSpan? duration = null, CancellationToken ct = default)
        {
            Guard.ThrowIfNull(form);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var totalDuration = duration ?? TimeSpan.FromMilliseconds(300);
            var originalLocation = form.Location;

            if (totalDuration.TotalMilliseconds <= 0)
            {
                tcs.TrySetResult(true);
                return tcs.Task;
            }

            var interval = TimeSpan.FromMilliseconds(15); // ~60 fps
            var totalSteps = Math.Max(1, (int)(totalDuration.TotalMilliseconds / interval.TotalMilliseconds));
            var currentStep = 0;

            var timer = new Timer(
                _ =>
                {
                    var step = Interlocked.Increment(ref currentStep);

                    if (step >= totalSteps || ct.IsCancellationRequested)
                    {
                        // Restore original position
                        if (form.IsHandleCreated && !form.IsDisposed)
                        {
                            form.BeginInvoke(new Action(() =>
                            {
                                if (!form.IsDisposed)
                                    form.Location = originalLocation;
                            }));
                        }

                        if (ct.IsCancellationRequested)
                            tcs.TrySetCanceled(ct);
                        else
                            tcs.TrySetResult(true);

                        return;
                    }

                    int offset = (step % 2 == 0) ? -intensity : intensity;
                    if (form.IsHandleCreated && !form.IsDisposed)
                    {
                        form.BeginInvoke(new Action(() =>
                        {
                            if (!form.IsDisposed)
                                form.Location = new Point(originalLocation.X + offset, originalLocation.Y);
                        }));
                    }
                },
                null,
                (int)interval.TotalMilliseconds,
                (int)interval.TotalMilliseconds);

            // Clean up timer on cancellation
            ct.Register(() =>
            {
                timer.Dispose();
                if (form.IsHandleCreated && !form.IsDisposed)
                {
                    form.BeginInvoke(new Action(() =>
                    {
                        if (!form.IsDisposed)
                            form.Location = originalLocation;
                    }));
                }

                tcs.TrySetCanceled(ct);
            });

            // Ensure completion even if everything else fails
            tcs.Task.ContinueWith(_ => timer.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        /// <summary>
        /// Fades the form in from opacity 0 to 1.
        /// </summary>
        /// <param name="form">The form to fade in.</param>
        /// <param name="duration">The duration of the fade effect. Default is 200 ms.</param>
        /// <param name="steps">The number of opacity steps. Default is 20.</param>
        /// <param name="ct">A cancellation token to stop the fade early.</param>
        /// <returns>A task that completes when the fade-in finishes.</returns>
        public static Task FadeInAsync(Form form, TimeSpan? duration = null, int steps = 20, CancellationToken ct = default)
        {
            Guard.ThrowIfNull(form);
            Guard.ThrowIfOutOfRange(steps, 1, 200);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var totalDuration = duration ?? TimeSpan.FromMilliseconds(200);

            if (totalDuration.TotalMilliseconds <= 0)
            {
                form.BeginInvoke(new Action(() => form.Opacity = 1.0));
                tcs.TrySetResult(true);
                return tcs.Task;
            }

            var intervalMs = Math.Max(10, (int)(totalDuration.TotalMilliseconds / steps));
            var currentStep = 0;

            var timer = new Timer(
                _ =>
                {
                    var step = Interlocked.Increment(ref currentStep);

                    if (step >= steps || ct.IsCancellationRequested)
                    {
                        if (form.IsHandleCreated && !form.IsDisposed)
                        {
                            form.BeginInvoke(new Action(() =>
                            {
                                if (!form.IsDisposed)
                                    form.Opacity = 1.0;
                            }));
                        }

                        if (ct.IsCancellationRequested)
                            tcs.TrySetCanceled(ct);
                        else
                            tcs.TrySetResult(true);

                        return;
                    }

                    double opacity = (double)step / steps;
                    if (form.IsHandleCreated && !form.IsDisposed)
                    {
                        form.BeginInvoke(new Action(() =>
                        {
                            if (!form.IsDisposed)
                                form.Opacity = opacity;
                        }));
                    }
                },
                null,
                intervalMs,
                intervalMs);

            ct.Register(() =>
            {
                timer.Dispose();
                if (form.IsHandleCreated && !form.IsDisposed)
                {
                    form.BeginInvoke(new Action(() =>
                    {
                        if (!form.IsDisposed)
                            form.Opacity = 1.0;
                    }));
                }

                tcs.TrySetCanceled(ct);
            });

            tcs.Task.ContinueWith(_ => timer.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        /// <summary>
        /// Fades the form out from opacity 1 to 0.
        /// </summary>
        /// <param name="form">The form to fade out.</param>
        /// <param name="duration">The duration of the fade effect. Default is 200 ms.</param>
        /// <param name="steps">The number of opacity steps. Default is 20.</param>
        /// <param name="ct">A cancellation token to stop the fade early.</param>
        /// <returns>A task that completes when the fade-out finishes.</returns>
        public static Task FadeOutAsync(Form form, TimeSpan? duration = null, int steps = 20, CancellationToken ct = default)
        {
            Guard.ThrowIfNull(form);
            Guard.ThrowIfOutOfRange(steps, 1, 200);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var totalDuration = duration ?? TimeSpan.FromMilliseconds(200);

            if (totalDuration.TotalMilliseconds <= 0)
            {
                form.BeginInvoke(new Action(() => form.Opacity = 0.0));
                tcs.TrySetResult(true);
                return tcs.Task;
            }

            var intervalMs = Math.Max(10, (int)(totalDuration.TotalMilliseconds / steps));
            var currentStep = 0;

            var timer = new Timer(
                _ =>
                {
                    var step = Interlocked.Increment(ref currentStep);

                    if (step >= steps || ct.IsCancellationRequested)
                    {
                        if (form.IsHandleCreated && !form.IsDisposed)
                        {
                            form.BeginInvoke(new Action(() =>
                            {
                                if (!form.IsDisposed)
                                    form.Opacity = 0.0;
                            }));
                        }

                        if (ct.IsCancellationRequested)
                            tcs.TrySetCanceled(ct);
                        else
                            tcs.TrySetResult(true);

                        return;
                    }

                    double opacity = 1.0 - ((double)step / steps);
                    if (form.IsHandleCreated && !form.IsDisposed)
                    {
                        form.BeginInvoke(new Action(() =>
                        {
                            if (!form.IsDisposed)
                                form.Opacity = opacity;
                        }));
                    }
                },
                null,
                intervalMs,
                intervalMs);

            ct.Register(() =>
            {
                timer.Dispose();
                if (form.IsHandleCreated && !form.IsDisposed)
                {
                    form.BeginInvoke(new Action(() =>
                    {
                        if (!form.IsDisposed)
                            form.Opacity = 0.0;
                    }));
                }

                tcs.TrySetCanceled(ct);
            });

            tcs.Task.ContinueWith(_ => timer.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }
    }
}

#endif
