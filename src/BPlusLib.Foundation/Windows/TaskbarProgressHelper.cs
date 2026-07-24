// <copyright file="TaskbarProgressHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>Progress state for the taskbar button.</summary>
    public enum TaskbarProgressState
    {
        /// <summary>No progress indicator.</summary>
        None = 0,
        /// <summary>Indeterminate (marquee/animation).</summary>
        Indeterminate = 1,
        /// <summary>Normal progress bar.</summary>
        Normal = 2,
        /// <summary>Error state (red).</summary>
        Error = 3,
        /// <summary>Paused state (yellow).</summary>
        Paused = 4,
    }

    /// <summary>
    /// Provides methods to show progress in the taskbar button (Windows 7+).
    /// Uses ITaskbarList3 COM interface. All methods are thread-safe and
    /// gracefully return false on non-Windows or older Windows.
    /// </summary>
    public static class TaskbarProgressHelper
    {
        private static Shell32.ITaskbarList3? _taskbarList;

        private static Shell32.ITaskbarList3 GetTaskbar()
        {
            if (_taskbarList is null)
            {
                Type t = Type.GetTypeFromCLSID(Shell32.CLSID_TaskbarList)!;
                _taskbarList = (Shell32.ITaskbarList3)Activator.CreateInstance(t)!;
                _taskbarList.HrInit();
            }
            return _taskbarList;
        }

        /// <summary>
        /// Sets the progress value for the taskbar button.
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <param name="completed">Completed amount (0 to total).</param>
        /// <param name="total">Total amount.</param>
        /// <returns>True on success.</returns>
        public static bool SetProgress(IntPtr hwnd, ulong completed, ulong total)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                var tb = GetTaskbar();
                tb.SetProgressState(hwnd, Shell32.TBPF_NORMAL);
                tb.SetProgressValue(hwnd, completed, total);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Sets the progress state for the taskbar button.
        /// </summary>
        public static bool SetState(IntPtr hwnd, TaskbarProgressState state)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                var tb = GetTaskbar();
                tb.SetProgressState(hwnd, MapState(state));
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Clears the progress indicator from the taskbar button.
        /// </summary>
        public static bool ClearProgress(IntPtr hwnd)
        {
            return SetState(hwnd, TaskbarProgressState.None);
        }

#if FEATURE_WINDOW_MODULE
        /// <summary>WinForms convenience: set progress by percentage.</summary>
        public static bool SetProgress(System.Windows.Forms.Form form, int percent)
        {
            if (form is null) return false;
            percent = Math.Max(0, Math.Min(100, percent));
            return SetProgress(form.Handle, (ulong)percent, 100);
        }

        /// <summary>WinForms convenience: set state.</summary>
        public static bool SetState(System.Windows.Forms.Form form, TaskbarProgressState state)
        {
            if (form is null) return false;
            return SetState(form.Handle, state);
        }

        /// <summary>WinForms convenience: clear progress.</summary>
        public static bool ClearProgress(System.Windows.Forms.Form form)
        {
            if (form is null) return false;
            return ClearProgress(form.Handle);
        }
#endif

        private static uint MapState(TaskbarProgressState state) => state switch
        {
            TaskbarProgressState.None => Shell32.TBPF_NOPROGRESS,
            TaskbarProgressState.Indeterminate => Shell32.TBPF_INDETERMINATE,
            TaskbarProgressState.Normal => Shell32.TBPF_NORMAL,
            TaskbarProgressState.Error => Shell32.TBPF_ERROR,
            TaskbarProgressState.Paused => Shell32.TBPF_PAUSED,
            _ => Shell32.TBPF_NOPROGRESS,
        };
    }
}
