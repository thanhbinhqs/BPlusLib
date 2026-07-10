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
}