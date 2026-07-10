// <copyright file="MessageBoxEx.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Dialogs;

/// <summary>
/// Extended message box with centering, dark mode, timeout, and async support.
/// </summary>
/// <remarks>
/// All methods are thread-safe. The async overloads use a background STA thread
/// with a hidden message pump so they work correctly from any synchronization context.
/// Centering uses a WH_CBT hook (same approach as the original implementation).
/// </remarks>
public static class MessageBoxEx
{
    // DWM API for dark mode support
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const uint MB_SETFOREGROUND = 0x00020000;

    /// <summary>
    /// Displays a message box with the specified parameters.
    /// </summary>
    /// <param name="parameters">The dialog parameters.</param>
    /// <param name="ct">A cancellation token that can close the dialog on cancellation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation,
    /// with a <see cref="DialogResult"/> indicating which button was clicked.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is null.</exception>
    public static Task<DialogResult> ShowAsync(DialogParams parameters, CancellationToken ct = default)
    {
        Guard.ThrowIfNull(parameters);
        return ShowAsyncCore(parameters, ct);
    }

    /// <summary>
    /// Displays a message box with the specified text, caption, buttons, and icon.
    /// </summary>
    /// <param name="owner">The owner window (can be null).</param>
    /// <param name="text">The message text.</param>
    /// <param name="caption">The dialog title.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <param name="ct">A cancellation token that can close the dialog on cancellation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation,
    /// with a <see cref="DialogResult"/> indicating which button was clicked.</returns>
    public static Task<DialogResult> ShowAsync(
        IWin32Window? owner,
        string text,
        string caption,
        DialogButton buttons = DialogButton.OK,
        DialogIcon icon = DialogIcon.None,
        CancellationToken ct = default)
    {
        var parameters = new DialogParams
        {
            Text = text ?? string.Empty,
            Caption = caption ?? string.Empty,
            Owner = owner,
            Buttons = buttons,
            Icon = icon,
        };

        return ShowAsyncCore(parameters, ct);
    }

    /// <summary>
    /// Displays a message box synchronously (backward-compatible overload).
    /// </summary>
    /// <param name="parentHandle">Handle to the parent window (IntPtr.Zero for no parent).</param>
    /// <param name="text">The message text.</param>
    /// <param name="caption">The dialog title.</param>
    /// <param name="buttons">The buttons to display (using legacy enum mapping).</param>
    /// <param name="icon">The icon to display (using legacy enum mapping).</param>
    /// <returns>A <see cref="DialogResult"/> indicating which button was clicked.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static DialogResult Show(
        IntPtr parentHandle,
        string text,
        string caption,
        DialogButton buttons = DialogButton.OK,
        DialogIcon icon = DialogIcon.None)
    {
        var parameters = new DialogParams
        {
            Text = text ?? string.Empty,
            Caption = caption ?? string.Empty,
            Owner = parentHandle != IntPtr.Zero ? new WindowWrapper(parentHandle) : null,
            Buttons = buttons,
            Icon = icon,
        };

        return ShowAsyncCore(parameters, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Displays a message box synchronously (backward-compatible overload, no parent).
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="caption">The dialog title.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <returns>A <see cref="DialogResult"/> indicating which button was clicked.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static DialogResult Show(
        string text,
        string caption,
        DialogButton buttons = DialogButton.OK,
        DialogIcon icon = DialogIcon.None)
    {
        return Show(IntPtr.Zero, text, caption, buttons, icon);
    }

    /// <summary>
    /// Core async implementation. Runs the native MessageBox on an STA thread.
    /// </summary>
    private static Task<DialogResult> ShowAsyncCore(DialogParams parameters, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<DialogResult>();

        var thread = new Thread(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                IntPtr ownerHwnd = parameters.Owner?.Handle ?? IntPtr.Zero;
                IntPtr dialogHwnd = IntPtr.Zero;

                // Install WH_CBT hook for centering and dark mode
                IntPtr hook = IntPtr.Zero;

                // The delegate must be stored to prevent GC while the hook is active.
                // We keep it alive via the thread's closure.
                User32.HookProc? hookProc = null;

                if (ownerHwnd != IntPtr.Zero)
                {
                    hookProc = (nCode, wParam, lParam) =>
                    {
                        if (nCode == User32.HCBT_ACTIVATE)
                        {
                            dialogHwnd = wParam;

                            // Center dialog on parent
                            CenterOnParent(wParam, ownerHwnd);

                            // Apply dark mode if requested
                            if (parameters.DarkMode == DarkModeStyle.Dark)
                            {
                                TryApplyDarkMode(wParam);
                            }
                        }

                        return User32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                    };

                    hook = User32.SetWindowsHookEx(
                        User32.WH_CBT,
                        hookProc,
                        IntPtr.Zero,
                        (uint)Kernel32.GetCurrentThreadId());
                }

                // Set up timeout + cancellation
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                IDisposable? cancellationRegistration = null;

                if (parameters.TimeoutMs > 0 || ct.CanBeCanceled)
                {
                    if (parameters.TimeoutMs > 0)
                    {
                        timeoutCts.CancelAfter(parameters.TimeoutMs);
                    }

                    cancellationRegistration = timeoutCts.Token.Register(() =>
                    {
                        // Post WM_CLOSE to the dialog to dismiss it on timeout/cancel
                        if (dialogHwnd != IntPtr.Zero)
                        {
                            User32.PostMessage(dialogHwnd, User32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        }
                    });
                }

                try
                {
                    uint uType = MapButtons(parameters.Buttons) | MapIcon(parameters.Icon) | MB_SETFOREGROUND;

                    int result = User32.MessageBox(
                        ownerHwnd,
                        parameters.Text ?? string.Empty,
                        parameters.Caption ?? string.Empty,
                        uType);

                    DialogResult mapped = MapResult(result);

                    // If cancellation was requested, return the timeout/cancel result
                    if (ct.IsCancellationRequested || timeoutCts.IsCancellationRequested)
                    {
                        tcs.TrySetResult(parameters.TimeoutResult != DialogResult.None
                            ? parameters.TimeoutResult
                            : mapped);
                    }
                    else
                    {
                        tcs.TrySetResult(mapped);
                    }
                }
                finally
                {
                    cancellationRegistration?.Dispose();

                    if (hook != IntPtr.Zero)
                    {
                        User32.UnhookWindowsHookEx(hook);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetResult(parameters.TimeoutResult != DialogResult.None
                    ? parameters.TimeoutResult
                    : DialogResult.Cancel);
            }
            catch (Exception ex)
            {
                // Ultimate fallback
                try
                {
                    int result = User32.MessageBox(
                        parameters.Owner?.Handle ?? IntPtr.Zero,
                        parameters.Text ?? string.Empty,
                        parameters.Caption ?? string.Empty,
                        0);

                    DialogResult mapped = MapResult(result);
                    tcs.TrySetResult(mapped);
                }
                catch
                {
                    tcs.TrySetResult(DialogResult.Cancel);
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Name = "MessageBoxEx STA";
        thread.Start();

        return tcs.Task;
    }

    /// <summary>
    /// Centers a dialog window on its parent window.
    /// </summary>
    private static void CenterOnParent(IntPtr dialogHandle, IntPtr parentHandle)
    {
        try
        {
            if (!User32.GetWindowRect(dialogHandle, out RECT dlgRect))
                return;

            if (!User32.GetWindowRect(parentHandle, out RECT parentRect))
                return;

            int dlgW = dlgRect.Width;
            int dlgH = dlgRect.Height;

            int parentCX = parentRect.Left + (parentRect.Width / 2);
            int parentCY = parentRect.Top + (parentRect.Height / 2);

            int x = parentCX - (dlgW / 2);
            int y = parentCY - (dlgH / 2);

            // Clamp to virtual screen
            int screenW = User32.GetSystemMetrics(User32.SM_CXVIRTUALSCREEN);
            int screenH = User32.GetSystemMetrics(User32.SM_CYVIRTUALSCREEN);
            int screenX = User32.GetSystemMetrics(User32.SM_XVIRTUALSCREEN);
            int screenY = User32.GetSystemMetrics(User32.SM_YVIRTUALSCREEN);

            x = Math.Max(screenX, Math.Min(x, screenX + screenW - dlgW));
            y = Math.Max(screenY, Math.Min(y, screenY + screenH - dlgH));

            User32.SetWindowPos(
                dialogHandle,
                IntPtr.Zero,
                x, y, 0, 0,
                User32.SWP_NOSIZE | User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
        }
        catch
        {
            // Centering is best-effort
        }
    }

    /// <summary>
    /// Applies dark mode to a dialog window using DWM.
    /// </summary>
    private static void TryApplyDarkMode(IntPtr hwnd)
    {
        try
        {
            int darkValue = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkValue, sizeof(int));
        }
        catch
        {
            // Dark mode is best-effort (requires Windows 10 20H1+)
        }
    }

    /// <summary>
    /// Maps <see cref="DialogButton"/> to Win32 MessageBox type flags.
    /// </summary>
    private static uint MapButtons(DialogButton buttons) => buttons switch
    {
        DialogButton.OK => 0x00000000,
        DialogButton.OKCancel => 0x00000001,
        DialogButton.AbortRetryIgnore => 0x00000002,
        DialogButton.YesNoCancel => 0x00000003,
        DialogButton.YesNo => 0x00000004,
        DialogButton.RetryCancel => 0x00000005,
        DialogButton.CancelTryContinue => 0x00000006,
        _ => 0x00000000,
    };

    /// <summary>
    /// Maps <see cref="DialogIcon"/> to Win32 MessageBox icon flags.
    /// </summary>
    private static uint MapIcon(DialogIcon icon) => icon switch
    {
        DialogIcon.None => 0x00000000,
        DialogIcon.Information => 0x00000040, // MB_ICONASTERISK
        DialogIcon.Question => 0x00000020,    // MB_ICONQUESTION
        DialogIcon.Warning => 0x00000030,     // MB_ICONEXCLAMATION
        DialogIcon.Error => 0x00000010,       // MB_ICONHAND
        DialogIcon.Shield => 0x00000080,      // MB_ICONSHIELD (Windows Vista+)
        _ => 0x00000000,
    };

    /// <summary>
    /// Maps a Win32 MessageBox result integer to <see cref="DialogResult"/>.
    /// </summary>
    private static DialogResult MapResult(int win32Result) => win32Result switch
    {
        1 => DialogResult.OK,
        2 => DialogResult.Cancel,
        3 => DialogResult.Abort,
        4 => DialogResult.Retry,
        5 => DialogResult.Ignore,
        6 => DialogResult.Yes,
        7 => DialogResult.No,
        10 => DialogResult.TryAgain,
        11 => DialogResult.Continue,
        _ => DialogResult.Cancel,
    };

    /// <summary>
    /// Simple IWin32Window wrapper around an IntPtr handle.
    /// </summary>
    private sealed class WindowWrapper : IWin32Window
    {
        public WindowWrapper(IntPtr handle) => Handle = handle;
        public IntPtr Handle { get; }
    }
}
#endif
