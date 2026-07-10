// <copyright file="NativeStructures.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct SystemExtendedInformationHandleEntry
    {
        public IntPtr ObjectPointer;
        public IntPtr HandleValue;
        public int UniqueProcessId;
        public int ObjectTypeIndex;
        public int GrantedAccess;
        public int HandleAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemExtendedHandleInformation
    {
        public IntPtr Reserved;
        public int NumberOfHandles;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectNameInformation
    {
        public UnicodeString Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessBasicInformation
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
        public long ToInt64() => ((long)HighDateTime << 32) | LowDateTime;
    }

    /// <summary>
    /// POINT structure — defines the x- and y-coordinates of a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// MONITORINFO structure — contains information about a display monitor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        public void Init() => cbSize = Marshal.SizeOf<MONITORINFO>();
    }

    /// <summary>
    /// MONITORINFOEX structure — contains information about a display monitor
    /// with the device name.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
        public void Init() => cbSize = Marshal.SizeOf<MONITORINFOEX>();
    }

    /// <summary>
    /// MINMAXINFO structure — contains information about a window's maximized size
    /// and position and its minimum and maximum tracking size.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    /// <summary>
    /// MSG structure — contains message information from a thread's message queue.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    /// <summary>
    /// FLASHWINFO structure — contains the flash status for a window and the number of times to flash it.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct FLASHWINFO
    {
        public int cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;

        public void Init() => cbSize = Marshal.SizeOf<FLASHWINFO>();
    }

    /// <summary>
    /// SHFILEINFOW structure — receives information about a file object.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHFILEINFOW
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    /// <summary>
    /// SHELLEXECUTEINFOW structure — contains information used by ShellExecuteExW.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHELLEXECUTEINFOW
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpVerb;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpParameters;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;

        public void Init() => cbSize = Marshal.SizeOf<SHELLEXECUTEINFOW>();
    }
}
