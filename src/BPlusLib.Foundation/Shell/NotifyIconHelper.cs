// <copyright file="NotifyIconHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Shell
{
    /// <summary>
    /// Represents a system tray notification icon that can be shown, hidden,
    /// modified, and display balloon tips.
    /// Thread-safe: each instance uses its own lock.
    /// </summary>
    public sealed class NotifyIcon : IDisposable
    {
        private readonly IntPtr _hWnd;
        private readonly uint _callbackMessage;
        private readonly uint _id;
        private IntPtr _iconHandle;
        private string _tooltip = string.Empty;
        private bool _visible;
        private bool _disposed;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance via <see cref="NotifyIconHelper.Create"/>.
        /// </summary>
        internal NotifyIcon(IntPtr hWnd, uint callbackMessage, uint id, IntPtr iconHandle)
        {
            _hWnd = hWnd;
            _callbackMessage = callbackMessage;
            _id = id;
            _iconHandle = iconHandle;
        }

        /// <summary>Adds the icon to the notification area.</summary>
        public bool Show()
        {
            if (_disposed) return false;
            lock (_lock)
            {
                var data = BuildNotifyIconData(Shell32.NIM_ADD);
                _visible = Shell32.Shell_NotifyIconW(Shell32.NIM_ADD, ref data);
                return _visible;
            }
        }

        /// <summary>Removes the icon from the notification area.</summary>
        public bool Hide()
        {
            if (_disposed) return false;
            lock (_lock)
            {
                var data = BuildNotifyIconData(Shell32.NIM_DELETE);
                _visible = false;
                return Shell32.Shell_NotifyIconW(Shell32.NIM_DELETE, ref data);
            }
        }

        /// <summary>Updates the icon, tooltip, or other properties in-place.</summary>
        public bool Update()
        {
            if (_disposed || !_visible) return false;
            lock (_lock)
            {
                var data = BuildNotifyIconData(Shell32.NIM_MODIFY);
                return Shell32.Shell_NotifyIconW(Shell32.NIM_MODIFY, ref data);
            }
        }

        /// <summary>
        /// Shows a balloon tip notification.
        /// </summary>
        /// <param name="title">Balloon title text.</param>
        /// <param name="text">Balloon body text.</param>
        /// <param name="iconType">Icon type for the balloon (None, Info, Warning, Error).</param>
        /// <param name="timeoutMs">Display duration in ms (min 10000, max 30000 on older Windows).</param>
        /// <returns>True if the balloon was displayed.</returns>
        public bool ShowBalloonTip(string title, string text,
            BalloonIconType iconType = BalloonIconType.Info, uint timeoutMs = 10000)
        {
            if (_disposed || !_visible) return false;
            lock (_lock)
            {
                int size = Marshal.SizeOf<NOTIFYICONDATAW>();
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    var data = BuildNotifyIconData(Shell32.NIM_MODIFY);
                    data.uFlags |= Shell32.NIF_INFO;
                    data.szInfo = text ?? string.Empty;
                    data.szInfoTitle = title ?? string.Empty;
                    data.uTimeoutOrVersion = timeoutMs;
                    data.dwInfoFlags = (uint)iconType;
                    Marshal.StructureToPtr(data, ptr, false);
                    var refData = Marshal.PtrToStructure<NOTIFYICONDATAW>(ptr);
                    return Shell32.Shell_NotifyIconW(Shell32.NIM_MODIFY, ref refData);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        /// <summary>Updates the icon handle.</summary>
        public bool SetIcon(IntPtr iconHandle)
        {
            if (_disposed) return false;
            lock (_lock)
            {
                _iconHandle = iconHandle;
                return Update();
            }
        }

        /// <summary>Updates the tooltip text.</summary>
        public bool SetTooltip(string tooltip)
        {
            if (_disposed) return false;
            lock (_lock)
            {
                _tooltip = tooltip ?? string.Empty;
                return Update();
            }
        }

        /// <summary>Gets whether the icon is currently visible in the tray.</summary>
        public bool IsVisible => _visible && !_disposed;

        private NOTIFYICONDATAW BuildNotifyIconData(uint message)
        {
            var data = new NOTIFYICONDATAW();
            data.cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>();
            data.hWnd = _hWnd;
            data.uID = _id;
            data.uFlags = Shell32.NIF_MESSAGE | Shell32.NIF_ICON;
            data.uCallbackMessage = _callbackMessage;
            data.hIcon = _iconHandle;
            data.szTip = _tooltip ?? string.Empty;
            if (!string.IsNullOrEmpty(_tooltip))
                data.uFlags |= Shell32.NIF_TIP;
            return data;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Hide();
                _disposed = true;
            }
        }
    }

    /// <summary>Balloon tip icon type.</summary>
    public enum BalloonIconType
    {
        /// <summary>No icon.</summary>
        None = 0,
        /// <summary>Information icon.</summary>
        Info = 1,
        /// <summary>Warning icon.</summary>
        Warning = 2,
        /// <summary>Error icon.</summary>
        Error = 3,
    }

    /// <summary>
    /// Static helper methods for creating system tray notification icons.
    /// </summary>
    public static class NotifyIconHelper
    {
        /// <summary>
        /// Creates a <see cref="NotifyIcon"/> instance. Call <see cref="NotifyIcon.Show"/>
        /// to display it in the notification area.
        /// </summary>
        /// <param name="hWnd">Window handle that receives callback messages (WM_APP + callbackMessageId).</param>
        /// <param name="callbackMessageId">Message ID sent to hWnd when the icon is interacted with.</param>
        /// <param name="iconHandle">Handle to the icon (HICON) to display.</param>
        /// <param name="id">Unique identifier for this icon within the calling process (default: 0).</param>
        /// <param name="tooltipText">Optional tooltip text for the icon.</param>
        /// <returns>A new <see cref="NotifyIcon"/> instance. Call .Show() to display.</returns>
        public static NotifyIcon Create(
            IntPtr hWnd,
            uint callbackMessageId,
            IntPtr iconHandle,
            uint id = 0,
            string? tooltipText = null)
        {
            if (hWnd == IntPtr.Zero)
                throw new ArgumentException("Window handle cannot be zero", nameof(hWnd));
            if (iconHandle == IntPtr.Zero)
                throw new ArgumentException("Icon handle cannot be zero", nameof(iconHandle));

            var icon = new NotifyIcon(hWnd, callbackMessageId, id, iconHandle);
            if (tooltipText is not null)
                icon.SetTooltip(tooltipText);
            return icon;
        }
    }
}
