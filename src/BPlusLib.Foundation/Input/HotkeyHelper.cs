// <copyright file="HotkeyHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Input
{
    /// <summary>
    /// Modifier keys for hotkey registration.
    /// </summary>
    [Flags]
    public enum HotkeyModifiers
    {
        /// <summary>No modifier.</summary>
        None = 0,
        /// <summary>Alt key.</summary>
        Alt = 1,
        /// <summary>Ctrl key.</summary>
        Control = 2,
        /// <summary>Shift key.</summary>
        Shift = 4,
        /// <summary>Windows logo key.</summary>
        Win = 8,
        /// <summary>No repeat when holding.</summary>
        NoRepeat = 0x4000,
    }

    /// <summary>
    /// Represents a registered global hotkey. Disposing unregisters it.
    /// </summary>
    public sealed class HotkeyRegistration : IDisposable
    {
        private readonly IntPtr _hWnd;
        private readonly int _id;
        private bool _disposed;

        internal HotkeyRegistration(IntPtr hWnd, int id)
        {
            _hWnd = hWnd;
            _id = id;
        }

        /// <summary>
        /// Registers a global hotkey. The window specified by <paramref name="hWnd"/>
        /// will receive WM_HOTKEY (0x0312) messages when the hotkey is pressed.
        /// </summary>
        /// <param name="hWnd">Window handle that receives WM_HOTKEY messages.</param>
        /// <param name="id">Unique hotkey identifier (use WM_HOTKEY.wParam to distinguish).</param>
        /// <param name="modifiers">Modifier keys (Alt, Ctrl, Shift, Win).</param>
        /// <param name="virtualKey">Virtual key code (e.g., 0x43 for 'C').</param>
        /// <returns>A <see cref="HotkeyRegistration"/> if successful, or null on failure.</returns>
        public static HotkeyRegistration? Register(
            IntPtr hWnd, int id,
            HotkeyModifiers modifiers, byte virtualKey)
        {
            if (hWnd == IntPtr.Zero) return null;
            if (id < 0) return null;

            try
            {
                uint modFlags = 0;
                if (modifiers.HasFlag(HotkeyModifiers.Alt)) modFlags |= User32.MOD_ALT;
                if (modifiers.HasFlag(HotkeyModifiers.Control)) modFlags |= User32.MOD_CONTROL;
                if (modifiers.HasFlag(HotkeyModifiers.Shift)) modFlags |= User32.MOD_SHIFT;
                if (modifiers.HasFlag(HotkeyModifiers.Win)) modFlags |= User32.MOD_WIN;
                if (modifiers.HasFlag(HotkeyModifiers.NoRepeat)) modFlags |= User32.MOD_NOREPEAT;

                bool ok = User32.RegisterHotKey(hWnd, id, modFlags, virtualKey);
                return ok ? new HotkeyRegistration(hWnd, id) : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Unregisters the hotkey.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool Unregister()
        {
            if (_disposed) return false;
            _disposed = true;
            try
            {
                return User32.UnregisterHotKey(_hWnd, _id);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the window handle associated with this registration.
        /// </summary>
        public IntPtr WindowHandle => _hWnd;

        /// <summary>
        /// Gets the hotkey identifier.
        /// </summary>
        public int Id => _id;

        public void Dispose() => Unregister();
    }
}
