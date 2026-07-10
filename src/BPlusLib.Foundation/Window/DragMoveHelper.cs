// <copyright file="DragMoveHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Window
{
    /// <summary>
    /// Enables form dragging by emulating title-bar drag behavior.
    /// Typically used on custom title bars or borderless forms.
    /// </summary>
    public static class DragMoveHelper
    {
        private static readonly Dictionary<Form, DragMoveNativeWindow> ActiveHelpers = new();

        /// <summary>
        /// Attaches drag-move behavior to the entire form surface.
        /// </summary>
        /// <param name="form">The form to make draggable.</param>
        public static void Attach(Form form)
        {
            Guard.ThrowIfNull(form);

            if (ActiveHelpers.ContainsKey(form))
                return;

            var helper = new DragMoveNativeWindow(form, null);
            ActiveHelpers[form] = helper;
        }

        /// <summary>
        /// Attaches drag-move behavior restricted to the specified control area.
        /// </summary>
        /// <param name="form">The form containing the drag area.</param>
        /// <param name="dragArea">The control that acts as the drag surface.</param>
        public static void Attach(Form form, Control dragArea)
        {
            Guard.ThrowIfNull(form);
            Guard.ThrowIfNull(dragArea);

            if (ActiveHelpers.ContainsKey(form))
                return;

            var helper = new DragMoveNativeWindow(form, dragArea);
            ActiveHelpers[form] = helper;
        }

        /// <summary>
        /// Detaches drag-move behavior from the specified form.
        /// </summary>
        /// <param name="form">The form to remove drag behavior from.</param>
        public static void Detach(Form form)
        {
            Guard.ThrowIfNull(form);

            if (ActiveHelpers.TryGetValue(form, out var helper))
            {
                helper.Dispose();
                ActiveHelpers.Remove(form);
            }
        }

        /// <summary>
        /// NativeWindow subclass that intercepts WM_NCHITTEST to emulate title-bar dragging
        /// and handles WM_GETMINMAXINFO for snap-size management.
        /// </summary>
        private sealed class DragMoveNativeWindow : NativeWindow, IDisposable
        {
            private readonly Form _form;
            private readonly Control? _dragArea;
            private bool _disposed;

            public DragMoveNativeWindow(Form form, Control? dragArea)
            {
                _form = form ?? throw new ArgumentNullException(nameof(form));
                _dragArea = dragArea;

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
                switch (m.Msg)
                {
                    case User32.WM_NCHITTEST:
                        HandleNcHitTest(ref m);
                        return;

                    case User32.WM_GETMINMAXINFO:
                        HandleGetMinMaxInfo(ref m);
                        return;

                    default:
                        base.WndProc(ref m);
                        break;
                }
            }

            private void HandleNcHitTest(ref Message m)
            {
                // Extract cursor position in screen coordinates from lParam
                int x = (short)((long)m.LParam & 0xFFFF);
                int y = (short)(((long)m.LParam >> 16) & 0xFFFF);
                var cursorScreen = new Point(x, y);

                if (_dragArea != null)
                {
                    // Convert screen position to drag-area client coordinates
                    var cursorClient = _dragArea.PointToClient(cursorScreen);
                    if (_dragArea.ClientRectangle.Contains(cursorClient))
                    {
                        m.Result = (IntPtr)User32.HTCAPTION;
                        return;
                    }
                }
                else
                {
                    // Entire form is draggable — skip client-area handling
                    // so the caption hit-test applies everywhere
                    m.Result = (IntPtr)User32.HTCAPTION;
                    return;
                }

                base.WndProc(ref m);
            }

            private void HandleGetMinMaxInfo(ref Message m)
            {
                // Let the form process normally first
                base.WndProc(ref m);

                // Then adjust max tracking size to prevent the window from
                // extending beyond the working area of its current monitor
                // (snap-size management — keeps the taskbar uncovered)
                if (m.Msg == User32.WM_GETMINMAXINFO && m.LParam != IntPtr.Zero)
                {
                    var mmi = Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
                    var workingArea = MonitorHelper.GetWorkingAreaFrom(_form.Handle);

                    mmi.ptMaxSize.X = workingArea.Width;
                    mmi.ptMaxSize.Y = workingArea.Height;
                    mmi.ptMaxPosition.X = workingArea.X;
                    mmi.ptMaxPosition.Y = workingArea.Y;

                    Marshal.StructureToPtr(mmi, m.LParam, false);
                    m.Result = IntPtr.Zero;
                }
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
