// <copyright file="DeviceInfoParser.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Hardware
{
    /// <summary>
    /// Parses device information from SetupAPI handles into HardwareDeviceInfo objects.
    /// </summary>
    internal static class DeviceInfoParser
    {
        /// <summary>
        /// Extracts a string registry property from a device info element.
        /// </summary>
        internal static string GetStringProperty(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo, int property)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceRegistryProperty(
                    devInfoSet, ref devInfo, property, out _, IntPtr.Zero, 0, out requiredSize);

                if (requiredSize <= 0) return string.Empty;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    if (SetupApi.SetupDiGetDeviceRegistryProperty(
                        devInfoSet, ref devInfo, property, out _, buffer, requiredSize, out _))
                    {
                        return Marshal.PtrToStringUni(buffer) ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Extracts a multi-string registry property from a device info element.
        /// Returns the first string from the multi-string buffer.
        /// </summary>
        internal static string GetFirstMultiStringProperty(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo, int property)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceRegistryProperty(
                    devInfoSet, ref devInfo, property, out _, IntPtr.Zero, 0, out requiredSize);

                if (requiredSize <= 0) return string.Empty;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    if (SetupApi.SetupDiGetDeviceRegistryProperty(
                        devInfoSet, ref devInfo, property, out _, buffer, requiredSize, out _))
                    {
                        // Multi-string: first null-terminated string, then another null terminator
                        return Marshal.PtrToStringUni(buffer) ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Extracts an integer registry property from a device info element.
        /// </summary>
        internal static int GetIntProperty(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo, int property)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceRegistryProperty(
                    devInfoSet, ref devInfo, property, out _, IntPtr.Zero, 0, out requiredSize);

                if (requiredSize <= 0) return 0;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    if (SetupApi.SetupDiGetDeviceRegistryProperty(
                        devInfoSet, ref devInfo, property, out _, buffer, requiredSize, out _))
                    {
                        return Marshal.ReadInt32(buffer);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// Gets the device instance ID from a device info element.
        /// </summary>
        internal static string GetDeviceInstanceId(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo)
        {
            try
            {
                int requiredSize = 0;
                SetupApi.SetupDiGetDeviceInstanceId(
                    devInfoSet, ref devInfo, IntPtr.Zero, 0, out requiredSize);

                if (requiredSize <= 0) return string.Empty;

                IntPtr buffer = Marshal.AllocHGlobal(requiredSize * 2); // Unicode chars
                try
                {
                    if (SetupApi.SetupDiGetDeviceInstanceId(
                        devInfoSet, ref devInfo, buffer, requiredSize, out _))
                    {
                        return Marshal.PtrToStringUni(buffer) ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Parses a HardwareDeviceInfo from a device info element.
        /// </summary>
        internal static HardwareDeviceInfo Parse(IntPtr devInfoSet, ref SetupApi.SP_DEVINFO_DATA devInfo)
        {
            string deviceId = GetDeviceInstanceId(devInfoSet, ref devInfo);
            string friendlyName = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_FRIENDLYNAME);
            string manufacturer = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_MFG);
            string className = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_CLASS);
            string classGuid = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_CLASSGUID);
            string locationInfo = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_LOCATION_INFORMATION);
            string enumeratorName = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_ENUMERATOR_NAME);
            string description = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_DEVICEDESC);
            string hardwareId = GetFirstMultiStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_HARDWAREID);
            string busReportedDesc = GetStringProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_BUSREPORTEDDEVDESC);
            int configFlags = GetIntProperty(devInfoSet, ref devInfo, SetupApi.SPDRP_CONFIGFLAGS);

            return new HardwareDeviceInfo
            {
                DeviceId = deviceId,
                DeviceName = friendlyName,
                Manufacturer = manufacturer,
                ClassGuid = classGuid,
                ClassName = className,
                LocationInfo = locationInfo,
                EnumeratorName = enumeratorName,
                DeviceDescription = description,
                HardwareId = hardwareId,
                BusReportedDeviceDesc = busReportedDesc,
                IsConnected = true,
                IsRemovable = (configFlags & SetupApi.CONFIGFLAG_REMOVABLE) != 0,
            };
        }
    }
}
