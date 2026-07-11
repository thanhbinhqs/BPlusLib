// <copyright file="ConsoleHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Services
{
    /// <summary>
    /// Provides Windows console management via pure P/Invoke —
    /// allocate/attach/free console, show/hide window, set title, set text color,
    /// manage console mode (QuickEdit, Ctrl+C), and check console existence.
    /// All methods are thread-safe and gracefully return false/null on non-Windows.
    /// </summary>
    public static class ConsoleHelper
    {
        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <returns>True if the console was allocated successfully.</returns>
        public static bool Allocate()
        {
            try { return Kernel32.AllocConsole(); }
            catch { return false; }
        }

        /// <summary>
        /// Detaches the calling process from its console.
        /// </summary>
        /// <returns>True if the console was freed successfully.</returns>
        public static bool Free()
        {
            try { return Kernel32.FreeConsole(); }
            catch { return false; }
        }

        /// <summary>
        /// Attaches the calling process to the console of the specified process.
        /// Pass -1 or ATTACH_PARENT_PROCESS to attach to the parent's console.
        /// </summary>
        /// <param name="processId">Process ID, or -1 for parent process console.</param>
        /// <returns>True if attached successfully.</returns>
        public static bool Attach(int processId = -1)
        {
            try { return Kernel32.AttachConsole(processId); }
            catch { return false; }
        }

        /// <summary>
        /// Gets the window handle of the console, or IntPtr.Zero if no console.
        /// </summary>
        public static IntPtr GetWindowHandle()
        {
            try { return Kernel32.GetConsoleWindow(); }
            catch { return IntPtr.Zero; }
        }

        /// <summary>
        /// Shows or hides the console window.
        /// </summary>
        /// <param name="visible">True to show, false to hide.</param>
        /// <returns>True if the window state was changed.</returns>
        public static bool SetWindowVisible(bool visible)
        {
            IntPtr hWnd = GetWindowHandle();
            if (hWnd == IntPtr.Zero) return false;
            try { return User32.ShowWindowAsync(hWnd, visible ? 5 : 0); }
            catch { return false; }
        }

        /// <summary>
        /// Sets the console window title.
        /// </summary>
        /// <param name="title">The new title text.</param>
        /// <returns>True if the title was set.</returns>
        public static bool SetTitle(string title)
        {
            if (title is null) return false;
            try { return Kernel32.SetConsoleTitleW(title); }
            catch { return false; }
        }

        /// <summary>
        /// Gets the current console window title.
        /// </summary>
        /// <returns>The console title, or null on failure.</returns>
        public static string? GetTitle()
        {
            try
            {
                var sb = new StringBuilder(1024);
                int len = Kernel32.GetConsoleTitleW(sb, sb.Capacity);
                return len > 0 ? sb.ToString() : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Returns true if the current process has an attached console.
        /// </summary>
        public static bool HasConsole => GetWindowHandle() != IntPtr.Zero;

        /// <summary>
        /// Enables or disables QuickEdit mode on the console (which allows
        /// mouse selection and can block the process).
        /// </summary>
        /// <param name="enable">True to enable QuickEdit, false to disable.</param>
        /// <returns>True if the mode was changed.</returns>
        public static bool EnableQuickEdit(bool enable)
        {
            try
            {
                IntPtr handle = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);
                if (handle == (IntPtr)(-1) || handle == IntPtr.Zero) return false;
                if (!Kernel32.GetConsoleMode(handle, out uint mode)) return false;
                const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
                const uint ENABLE_EXTENDED_FLAGS = 0x0080;
                if (enable)
                    mode |= ENABLE_QUICK_EDIT_MODE;
                else
                    mode &= ~ENABLE_QUICK_EDIT_MODE;
                mode |= ENABLE_EXTENDED_FLAGS;
                return Kernel32.SetConsoleMode(handle, mode);
            }
            catch { return false; }
        }

        /// <summary>
        /// Enables or disables Ctrl+C handling in the console.
        /// When disabled, Ctrl+C is passed to the process as a signal.
        /// </summary>
        public static bool EnableCtrlC(bool enable)
        {
            try
            {
                IntPtr handle = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);
                if (handle == (IntPtr)(-1) || handle == IntPtr.Zero) return false;
                if (!Kernel32.GetConsoleMode(handle, out uint mode)) return false;
                const uint ENABLE_PROCESSED_INPUT = 0x0001;
                if (enable)
                    mode |= ENABLE_PROCESSED_INPUT;
                else
                    mode &= ~ENABLE_PROCESSED_INPUT;
                return Kernel32.SetConsoleMode(handle, mode);
            }
            catch { return false; }
        }

        /// <summary>
        /// Console foreground/background color mapping to Win32 console attributes.
        /// </summary>
        [Flags]
        public enum ConsoleColor
        {
            Black = 0,
            DarkBlue = 1,
            DarkGreen = 2,
            DarkCyan = 3,
            DarkRed = 4,
            DarkMagenta = 5,
            DarkYellow = 6,
            Gray = 7,
            DarkGray = 8,
            Blue = 9,
            Green = 10,
            Cyan = 11,
            Red = 12,
            Magenta = 13,
            Yellow = 14,
            White = 15,
        }

        /// <summary>
        /// Sets the console text color (foreground and optionally background).
        /// </summary>
        /// <param name="foreground">Foreground color.</param>
        /// <param name="background">Optional background color (default: Black).</param>
        /// <returns>True if the color was set.</returns>
        public static bool SetTextColor(ConsoleColor foreground, ConsoleColor? background = null)
        {
            try
            {
                IntPtr handle = Kernel32.GetStdHandle(Kernel32.STD_OUTPUT_HANDLE);
                if (handle == (IntPtr)(-1) || handle == IntPtr.Zero) return false;
                ushort attrs = (ushort)foreground;
                if (background.HasValue)
                    attrs |= (ushort)((ushort)background.Value << 4);
                return Kernel32.SetConsoleTextAttribute(handle, attrs);
            }
            catch { return false; }
        }
    }
}
