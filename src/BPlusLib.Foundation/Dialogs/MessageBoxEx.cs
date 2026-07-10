// <copyright file="MessageBoxEx.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation
{
    /// <summary>
    /// Extended MessageBox utilities for Windows desktop applications.
    /// Provides centered-on-parent message box display and other common
    /// dialog enhancements.
    /// </summary>
    /// <remarks>
    /// All methods are thread-safe and never throw — failing gracefully
    /// to the standard MessageBox placement if centering fails.
    /// </remarks>
    public static class MessageBoxEx
    {
        /// <summary>
        /// Displays a message box centered on the specified parent window.
        /// </summary>
        /// <param name="parentHandle">
        /// Handle to the parent window. If <see cref="IntPtr.Zero"/>, the
        /// message box is centered on the screen.
        /// </param>
        /// <param name="text">The message text to display.</param>
        /// <param name="caption">The dialog box title.</param>
        /// <param name="buttons">The button configuration.</param>
        /// <param name="icon">The icon to display.</param>
        /// <returns>
        /// A <see cref="MessageBoxExResult"/> indicating which button was clicked.
        /// </returns>
        /// <remarks>
        /// Uses a WH_CBT hook to intercept the dialog creation and reposition
        /// it to the center of the parent window before it becomes visible.
        /// This avoids the visual flicker of repositioning after showing.
        ///
        /// The hook is installed immediately before the MessageBox call and
        /// removed immediately after, so it has minimal global impact.
        /// </remarks>
        public static MessageBoxExResult Show(
            IntPtr parentHandle,
            string text,
            string caption,
            MessageBoxExButtons buttons = MessageBoxExButtons.OK,
            MessageBoxExIcon icon = MessageBoxExIcon.None)
        {
            try
            {
                uint uType = MapButtons(buttons) | MapIcon(icon) | 0x00020000; // MB_SETFOREGROUND

                IntPtr hook = IntPtr.Zero;
                NativeMethods.HookProc? hookProc = null;

                if (parentHandle != IntPtr.Zero)
                {
                    // Install a CBT hook to center the dialog when it's created
                    hookProc = (nCode, wParam, lParam) =>
                    {
                        if (nCode == NativeMethods.HCBT_ACTIVATE)
                        {
                            CenterOnParent(wParam, parentHandle);
                        }

                        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                    };

                    hook = NativeMethods.SetWindowsHookEx(
                        NativeMethods.WH_CBT,
                        hookProc,
                        IntPtr.Zero,
                        (uint)NativeMethods.GetCurrentThreadId());
                }

                try
                {
                    int result = NativeMethods.MessageBox(parentHandle, text ?? string.Empty, caption ?? string.Empty, uType);
                    return MapResult(result);
                }
                finally
                {
                    if (hook != IntPtr.Zero)
                    {
                        NativeMethods.UnhookWindowsHookEx(hook);
                    }
                }
            }
            catch
            {
                // Ultimate fallback: plain MessageBox (may not be centered)
                try
                {
                    int result = NativeMethods.MessageBox(parentHandle, text ?? string.Empty, caption ?? string.Empty, 0);
                    return MapResult(result);
                }
                catch
                {
                    return MessageBoxExResult.Cancel;
                }
            }
        }

        /// <summary>
        /// Displays a message box centered on the desktop (no parent).
        /// </summary>
        public static MessageBoxExResult Show(
            string text,
            string caption,
            MessageBoxExButtons buttons = MessageBoxExButtons.OK,
            MessageBoxExIcon icon = MessageBoxExIcon.None)
        {
            return Show(IntPtr.Zero, text, caption, buttons, icon);
        }

        /// <summary>
        /// Centers a dialog window on its parent window.
        /// </summary>
        private static void CenterOnParent(IntPtr dialogHandle, IntPtr parentHandle)
        {
            try
            {
                NativeMethods.GetWindowRect(dialogHandle, out RECT dlgRect);
                NativeMethods.GetWindowRect(parentHandle, out RECT parentRect);

                int dlgW = dlgRect.Width;
                int dlgH = dlgRect.Height;

                int parentCX = parentRect.Left + (parentRect.Width / 2);
                int parentCY = parentRect.Top + (parentRect.Height / 2);

                int x = parentCX - (dlgW / 2);
                int y = parentCY - (dlgH / 2);

                // Clamp to screen boundaries
                int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

                x = Math.Max(0, Math.Min(x, screenW - dlgW));
                y = Math.Max(0, Math.Min(y, screenH - dlgH));

                NativeMethods.SetWindowPos(
                    dialogHandle,
                    IntPtr.Zero,
                    x, y, 0, 0,
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
            }
            catch
            {
                // Centering is best-effort
            }
        }

        private static uint MapButtons(MessageBoxExButtons buttons) => buttons switch
        {
            MessageBoxExButtons.OK => 0x00000000,
            MessageBoxExButtons.OKCancel => 0x00000001,
            MessageBoxExButtons.AbortRetryIgnore => 0x00000002,
            MessageBoxExButtons.YesNoCancel => 0x00000003,
            MessageBoxExButtons.YesNo => 0x00000004,
            MessageBoxExButtons.RetryCancel => 0x00000005,
            MessageBoxExButtons.CancelTryContinue => 0x00000006,
            _ => 0x00000000,
        };

        private static uint MapIcon(MessageBoxExIcon icon) => icon switch
        {
            MessageBoxExIcon.None => 0x00000000,
            MessageBoxExIcon.Hand => 0x00000010,
            MessageBoxExIcon.Question => 0x00000020,
            MessageBoxExIcon.Exclamation => 0x00000030,
            MessageBoxExIcon.Asterisk => 0x00000040,
            _ => 0x00000000,
        };

        private static MessageBoxExResult MapResult(int win32Result) => win32Result switch
        {
            1 => MessageBoxExResult.OK,
            2 => MessageBoxExResult.Cancel,
            3 => MessageBoxExResult.Abort,
            4 => MessageBoxExResult.Retry,
            5 => MessageBoxExResult.Ignore,
            6 => MessageBoxExResult.Yes,
            7 => MessageBoxExResult.No,
            10 => MessageBoxExResult.TryAgain,
            11 => MessageBoxExResult.Continue,
            _ => MessageBoxExResult.Cancel,
        };
    }

    /// <summary>
    /// Button configuration for <see cref="MessageBoxEx"/>.
    /// </summary>
    public enum MessageBoxExButtons
    {
        OK = 0,
        OKCancel = 1,
        AbortRetryIgnore = 2,
        YesNoCancel = 3,
        YesNo = 4,
        RetryCancel = 5,
        CancelTryContinue = 6,
    }

    /// <summary>
    /// Icon selection for <see cref="MessageBoxEx"/>.
    /// </summary>
    public enum MessageBoxExIcon
    {
        None = 0,
        Hand = 1,       // Stop / Error
        Question = 2,    // Question
        Exclamation = 3, // Warning
        Asterisk = 4,    // Information
    }

    /// <summary>
    /// Result values returned by <see cref="MessageBoxEx"/>.
    /// </summary>
    public enum MessageBoxExResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7,
        TryAgain = 10,
        Continue = 11,
    }
}