// <copyright file="ResizeHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Window
{
    /// <summary>
    /// Enables edge-based resizing on forms that have no standard window border.
    /// Typically used together with <see cref="DragMoveHelper"/> for custom chrome forms.
    /// </summary>
    public static class ResizeHelper
    {
        private static readonly Dictionary<Form, ResizeNativeWindow> ActiveHelpers = new();

        /// <summary>
        /// Enables resizing by dragging the edges of the specified form.
        /// </summary>
        /// <param name="form">The form to make resizable.</param>
        /// <param name="borderWidth">The width (in pixels) of the interactive resize border. Default is 4.</param>
        public static void EnableResize(Form form, int borderWidth = 4)
        {
            Guard.ThrowIfNull(form);

            if (ActiveHelpers.ContainsKey(form))
                return;

            var helper = new ResizeNativeWindow(form, borderWidth);
            ActiveHelpers[form] = helper;
        }

        /// <summary>
        /// Disables edge-based resizing on the specified form.
        /// </summary>
        /// <param name="form">The form to disable resize for.</param>
        public static void DisableResize(Form form)
        {
            Guard.ThrowIfNull(form);

            if (ActiveHelpers.TryGetValue(form, out var helper))
            {
                helper.Dispose();
                ActiveHelpers.Remove(form);
            }
        }

        /// <summary>
        /// NativeWindow subclass that intercepts WM_NCHITTEST to return the appropriate
        /// resize cursor constants based on the cursor position relative to the form edges.
        /// </summary>
        private sealed class ResizeNativeWindow : NativeWindow, IDisposable
        {
            private readonly Form _form;
            private readonly int _borderWidth;
            private bool _disposed;

            public ResizeNativeWindow(Form form, int borderWidth)
            {
                _form = form ?? throw new ArgumentNullException(nameof(form));
                _borderWidth = Math.Max(1, borderWidth);

                _form.HandleCreated += OnHandleCreated;
                _form.HandleDestroyed += OnHandleDestroyed;

                if (_form.IsHandleCreated)
                    AssignHandle(_form.Handle);
            }

            private void OnHandleCreated(object? sender, EventArgs e)
            {
                if (!_disposed)
                    AssignHandle(_form.Handle);
            }

            private void OnHandleDestroyed(object? sender, EventArgs e)
            {
                ReleaseHandle();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == User32.WM_NCHITTEST)
                {
                    HandleNcHitTest(ref m);
                    return;
                }

                base.WndProc(ref m);
            }

            private void HandleNcHitTest(ref Message m)
            {
                // Extract cursor position in screen coordinates from lParam
                int x = (short)((long)m.LParam & 0xFFFF);
                int y = (short)(((long)m.LParam >> 16) & 0xFFFF);
                var cursorScreen = new Point(x, y);

                // Convert to client coordinates
                var cursorClient = _form.PointToClient(cursorScreen);
                int clientW = _form.ClientSize.Width;
                int clientH = _form.ClientSize.Height;

                bool onLeft = cursorClient.X <= _borderWidth;
                bool onRight = cursorClient.X >= clientW - _borderWidth;
                bool onTop = cursorClient.Y <= _borderWidth;
                bool onBottom = cursorClient.Y >= clientH - _borderWidth;

                // Return the appropriate hit-test constant:
                // Corners take priority over edges
                if (onLeft && onTop)
                    m.Result = (IntPtr)User32.HTTOPLEFT;
                else if (onRight && onTop)
                    m.Result = (IntPtr)User32.HTTOPRIGHT;
                else if (onLeft && onBottom)
                    m.Result = (IntPtr)User32.HTBOTTOMLEFT;
                else if (onRight && onBottom)
                    m.Result = (IntPtr)User32.HTBOTTOMRIGHT;
                else if (onLeft)
                    m.Result = (IntPtr)User32.HTLEFT;
                else if (onRight)
                    m.Result = (IntPtr)User32.HTRIGHT;
                else if (onTop)
                    m.Result = (IntPtr)User32.HTTOP;
                else if (onBottom)
                    m.Result = (IntPtr)User32.HTBOTTOM;
                else
                    base.WndProc(ref m);
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _form.HandleCreated -= OnHandleCreated;
                    _form.HandleDestroyed -= OnHandleDestroyed;
                    ReleaseHandle();
                }
            }
        }
    }
}

#endif
