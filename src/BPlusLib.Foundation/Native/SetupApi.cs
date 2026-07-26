// <copyright file="SetupApi.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native
{
    /// <summary>
    /// P/Invoke declarations for SetupAPI.dll — Windows device installation functions.
    /// </summary>
    internal static class SetupApi
    {
        // DIGCF flags
        internal const int DIGCF_DEFAULT = 0x00000001;
        internal const int DIGCF_PRESENT = 0x00000002;
        internal const int DIGCF_ALLCLASSES = 0x00000004;
        internal const int DIGCF_PROFILE = 0x00000008;
        internal const int DIGCF_DEVICEINTERFACE = 0x00000010;

        // SPDRP flags (SetupDiGetDeviceRegistryProperty)
        internal const int SPDRP_DEVICEDESC = 0x00000000;
        internal const int SPDRP_HARDWAREID = 0x00000001;
        internal const int SPDRP_FRIENDLYNAME = 0x0000000C;
        internal const int SPDRP_MFG = 0x0000000B;
        internal const int SPDRP_CLASS = 0x00000007;
        internal const int SPDRP_CLASSGUID = 0x00000008;
        internal const int SPDRP_DRIVER = 0x00000009;
        internal const int SPDRP_CONFIGFLAGS = 0x0000000A;
        internal const int SPDRP_LOCATION_INFORMATION = 0x0000000D;
        internal const int SPDRP_ENUMERATOR_NAME = 0x0000001A;
        internal const int SPDRP_INSTALL_DATE = 0x00000012;
        internal const int SPDRP_BUSREPORTEDDEVDESC = 0x00000029;
        internal const int SPDRP_HARDWAREIDSTRING = 0x00000001;
        internal const int SPDRP_DEVICE_POWER_DATA = 0x0000000E;

        // CONFIGFLAGS
        internal const int CONFIGFLAG_REMOVABLE = 0x00000010;

        // INVALID_HANDLE_VALUE
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>Retrieves a device information set for all devices of a class.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            int Flags);

        /// <summary>Retrieves a device information set for all devices.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            int Flags);

        /// <summary>Retrieves a device information element.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            int MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        /// <summary>Retrieves a device registry property.</summary>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            int Property,
            out int PropertyRegDataType,
            IntPtr PropertyBuffer,
            int PropertyBufferSize,
            out int RequiredSize);

        /// <summary>Retrieves the device instance ID for a device information element.</summary>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            IntPtr DeviceInstanceId,
            int DeviceInstanceIdSize,
            out int RequiredSize);

        /// <summary>Frees a device information set.</summary>
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        /// <summary>Device information set element.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        /// <summary>Device interface data.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        /// <summary>Device interface detail data.</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        /// <summary>SetupDiGetDeviceInterfaceDetail.</summary>
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            out int RequiredSize,
            ref SP_DEVINFO_DATA DeviceInfoData);
    }
}
