// <copyright file="DiskInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Defines the type of a logical drive.
    /// </summary>
    public enum DriveTypeEx
    {
        /// <summary>The drive type cannot be determined.</summary>
        Unknown = 0,

        /// <summary>The root path is invalid (no root directory).</summary>
        NoRootDirectory = 1,

        /// <summary>The drive is a removable media (e.g., floppy, USB flash).</summary>
        Removable = 2,

        /// <summary>The drive is a fixed disk (e.g., hard drive).</summary>
        Fixed = 3,

        /// <summary>The drive is a network drive.</summary>
        Network = 4,

        /// <summary>The drive is a CD-ROM/DVD/Blu-ray drive.</summary>
        CDRom = 5,

        /// <summary>The drive is a RAM disk.</summary>
        Ram = 6,
    }

    /// <summary>
    /// Provides detailed information about a single logical drive —
    /// name, volume label, file system, capacity, usage, and serial number.
    /// All data is obtained via P/Invoke; no WMI.
    /// </summary>
    public sealed class DriveInfoEx
    {
        internal DriveInfoEx()
        {
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>Gets the drive name (e.g., "C:").</summary>
        public string Name { get; internal set; } = string.Empty;

        /// <summary>Gets the volume label (e.g., "System").</summary>
        public string VolumeLabel { get; internal set; } = string.Empty;

        /// <summary>Gets the file system name (e.g., "NTFS", "FAT32").</summary>
        public string FileSystem { get; internal set; } = string.Empty;

        /// <summary>Gets the drive type.</summary>
        public DriveTypeEx DriveType { get; internal set; } = DriveTypeEx.Unknown;

        /// <summary>Gets the total capacity in bytes.</summary>
        public long TotalBytes { get; internal set; }

        /// <summary>Gets the available free space in bytes.</summary>
        public long AvailableBytes { get; internal set; }

        /// <summary>Gets the used space in bytes (Total - Available).</summary>
        public long UsedBytes => TotalBytes - AvailableBytes;

        /// <summary>
        /// Gets the usage percentage (0.0–100.0).
        /// Returns 0 if total capacity is zero.
        /// </summary>
        public double UsagePercent
        {
            get
            {
                if (TotalBytes <= 0) return 0.0;
                double used = TotalBytes - AvailableBytes;
                return (used / TotalBytes) * 100.0;
            }
        }

        /// <summary>Gets whether the drive is ready (available for reads).</summary>
        public bool IsReady { get; internal set; }

        /// <summary>Gets the volume serial number formatted as hex (e.g., "A8B3-C4D5"), or <c>null</c>.</summary>
        public string? SerialNumber { get; internal set; }

        /// <summary>
        /// Gets the total free space from an alternate P/Invoke call
        /// (GetDiskFreeSpaceEx), typically the same as <see cref="AvailableBytes"/>.
        /// </summary>
        public long? TotalFreeBytes { get; internal set; }
    }

    /// <summary>
    /// Static class that enumerates logical drives on the system
    /// using P/Invoke (kernel32) exclusively. No WMI.
    /// </summary>
    public static class DiskInfo
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const uint DriveUnknown = 0;
        private const uint DriveNoRootDir = 1;
        private const uint DriveRemovable = 2;
        private const uint DriveFixed = 3;
        private const uint DriveNetwork = 4;
        private const uint DriveCDRom = 5;
        private const uint DriveRam = 6;

        private const int MaxPath = 260;
        private const int MaxVolumeName = 256;

        private static readonly char[] DriveLetterFormat = new[] { 'A', ':', '\\' };

        // =====================================================================
        // P/Invoke
        // =====================================================================

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        private static extern uint GetLogicalDrives();

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern uint GetDriveTypeW([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVolumeInformationW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            StringBuilder? lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            StringBuilder? lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceExW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpDirectoryName,
            out ulong lpFreeBytesAvailableToCaller,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        // =====================================================================
        // Public methods
        // =====================================================================

        /// <summary>
        /// Enumerates all logical drives present on the system.
        /// Non-ready or inaccessible drives are silently skipped.
        /// </summary>
        /// <returns>A read-only list of <see cref="DriveInfoEx"/> instances.</returns>
        public static IReadOnlyList<DriveInfoEx> GetAllDrives()
        {
            var drives = new List<DriveInfoEx>();

            try
            {
                uint driveBits = GetLogicalDrives();
                if (driveBits == 0)
                    return drives;

                for (int i = 0; i < 26; i++)
                {
                    if ((driveBits & (1u << i)) == 0)
                        continue;

                    char letter = (char)('A' + i);
                    string rootPath = $"{letter}:\\";

                    try
                    {
                        var drive = PopulateDriveInfo(rootPath);
                        if (drive != null)
                            drives.Add(drive);
                    }
                    catch
                    {
                        // Skip drives that cause errors
                    }
                }
            }
            catch
            {
                // Non-Windows or API failure — return empty list
            }

            return drives;
        }

        /// <summary>
        /// Gets information about a specific drive by name (e.g., "C:" or "C:\").
        /// </summary>
        /// <param name="driveName">The drive name (e.g., "C:", "C:\").</param>
        /// <returns>A <see cref="DriveInfoEx"/> instance, or <c>null</c> if the drive is invalid or inaccessible.</returns>
        public static DriveInfoEx? GetDrive(string driveName)
        {
            if (string.IsNullOrEmpty(driveName))
                return null;

            string rootPath = driveName;
            // Normalize: if it's just "C:", add backslash
            if (rootPath.Length == 2 && rootPath[1] == ':')
                rootPath += '\\';
            else if (rootPath.Length < 3 || rootPath[1] != ':' || rootPath[2] != '\\')
                return null;

            try
            {
                return PopulateDriveInfo(rootPath);
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // Private helpers
        // =====================================================================

        private static DriveInfoEx? PopulateDriveInfo(string rootPath)
        {
            uint driveType;
            try
            {
                driveType = GetDriveTypeW(rootPath);
            }
            catch
            {
                return null;
            }

            var result = new DriveInfoEx
            {
                Name = rootPath.Substring(0, 2),
                DriveType = MapDriveType(driveType)
            };

            // Try to get volume info
            try
            {
                var volumeName = new StringBuilder(MaxVolumeName);
                var fileSystemName = new StringBuilder(MaxPath);

                if (GetVolumeInformationW(
                        rootPath,
                        volumeName,
                        MaxVolumeName,
                        out uint serialNumber,
                        out _,
                        out _,
                        fileSystemName,
                        MaxPath))
                {
                    result.VolumeLabel = volumeName.ToString();
                    result.FileSystem = fileSystemName.ToString();
                    result.SerialNumber = FormatSerialNumber(serialNumber);
                    result.IsReady = true;
                }
                else
                {
                    result.IsReady = driveType == DriveFixed;
                }
            }
            catch
            {
                result.IsReady = false;
            }

            // Try to get free space
            try
            {
                if (GetDiskFreeSpaceExW(
                        rootPath,
                        out ulong freeBytesAvailable,
                        out ulong totalBytes,
                        out ulong totalFreeBytes))
                {
                    result.TotalBytes = (long)totalBytes;
                    result.AvailableBytes = (long)freeBytesAvailable;
                    result.TotalFreeBytes = (long)totalFreeBytes;
                }
            }
            catch
            {
                // Space info unavailable
            }

            return result;
        }

        private static DriveTypeEx MapDriveType(uint win32DriveType)
        {
            return win32DriveType switch
            {
                DriveUnknown => DriveTypeEx.Unknown,
                DriveNoRootDir => DriveTypeEx.NoRootDirectory,
                DriveRemovable => DriveTypeEx.Removable,
                DriveFixed => DriveTypeEx.Fixed,
                DriveNetwork => DriveTypeEx.Network,
                DriveCDRom => DriveTypeEx.CDRom,
                DriveRam => DriveTypeEx.Ram,
                _ => DriveTypeEx.Unknown
            };
        }

        private static string FormatSerialNumber(uint serialNumber)
        {
            if (serialNumber == 0)
                return "0000-0000";

            // Format as XXXX-XXXX
            uint high = (serialNumber >> 16) & 0xFFFF;
            uint low = serialNumber & 0xFFFF;
            return $"{high:X4}-{low:X4}";
        }
    }
}
