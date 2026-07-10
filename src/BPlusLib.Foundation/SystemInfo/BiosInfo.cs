// <copyright file="BiosInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides information about the system BIOS/firmware —
    /// manufacturer, version, serial number, release date,
    /// SMBIOS version, and UEFI detection.
    /// All data is obtained via registry reads; no WMI.
    /// </summary>
    public sealed class BiosInfo
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private static readonly string BiosRegistryPath =
            @"HARDWARE\DESCRIPTION\System\BIOS";

        private static readonly string SecureBootRegistryPath =
            @"SYSTEM\CurrentControlSet\Control\SecureBoot\State";

        private static readonly string PEFirmwareTypeRegistryPath =
            @"SYSTEM\CurrentControlSet\Control\PEFirmwareType";

        // =====================================================================
        // Lazy singleton
        // =====================================================================

        private static readonly Lazy<BiosInfo> LazyCurrent =
            new Lazy<BiosInfo>(() => new BiosInfo());

        /// <summary>
        /// Gets the singleton <see cref="BiosInfo"/> instance
        /// representing the system firmware.
        /// </summary>
        public static BiosInfo Current => LazyCurrent.Value;

        // =====================================================================
        // Private constructor
        // =====================================================================

        private BiosInfo()
        {
            LoadRegistryInfo();
            DetectUefi();
        }

        // =====================================================================
        // Backing fields
        // =====================================================================

        private string? _manufacturer;
        private string? _name;
        private string? _version;
        private string? _serialNumber;
        private DateTime? _releaseDate;
        private string? _smbiosVersion;
        private bool _isUefi;

        // =====================================================================
        // Public properties
        // =====================================================================

        /// <summary>Gets the BIOS manufacturer name (e.g., "American Megatrends Inc.").</summary>
        public string? Manufacturer => _manufacturer;

        /// <summary>Gets the BIOS product name (e.g., "Default System BIOS").</summary>
        public string? Name => _name;

        /// <summary>Gets the BIOS version string.</summary>
        public string? Version => _version;

        /// <summary>Gets the system serial number (often "To be filled by O.E.M.").</summary>
        public string? SerialNumber => _serialNumber;

        /// <summary>Gets the BIOS release date, if parseable.</summary>
        public DateTime? ReleaseDate => _releaseDate;

        /// <summary>Gets the SMBIOS version string (if available via BIOSVersion registry value).</summary>
        public string? SmbiosVersion => _smbiosVersion;

        /// <summary>
        /// Gets whether the system is booted in UEFI mode.
        /// Detection methods (checked in order):
        /// 1. Registry value <c>UEFISecureBootEnabled</c> under SecureBoot\State.
        /// 2. Registry value under <c>PEFirmwareType</c> = 2 (UEFI).
        /// </summary>
        public bool IsUefi => _isUefi;

        // =====================================================================
        // Private methods
        // =====================================================================

        private void LoadRegistryInfo()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(BiosRegistryPath);
                if (key == null) return;

                _manufacturer = key.GetValue("BaseBoardManufacturer") as string;
                _name = key.GetValue("BaseBoardProduct") as string;
                _version = key.GetValue("SystemVersion") as string;
                _serialNumber = key.GetValue("SystemSerialNumber") as string;

                string? releaseDateStr = key.GetValue("BIOSReleaseDate") as string;
                if (!string.IsNullOrEmpty(releaseDateStr))
                    _releaseDate = ParseBiosDate(releaseDateStr!);

                // BIOSVendor and BIOSVersion provide additional detail
                string? biosVendor = key.GetValue("BIOSVendor") as string;
                string? biosVersion = key.GetValue("BIOSVersion") as string;

                if (string.IsNullOrEmpty(_manufacturer) && !string.IsNullOrEmpty(biosVendor))
                    _manufacturer = biosVendor;

                if (string.IsNullOrEmpty(_name) && !string.IsNullOrEmpty(biosVersion))
                    _name = biosVersion;

                // SMBIOS version is sometimes embedded in the BIOSVersion value
                if (biosVersion != null)
                    _smbiosVersion = ExtractSmbiosVersion(biosVersion);
            }
            catch
            {
                // Registry read failed — non-fatal
            }
        }

        private void DetectUefi()
        {
            try
            {
                // Method 1: Check SecureBoot\State
                using var secureBootKey = Registry.LocalMachine.OpenSubKey(SecureBootRegistryPath);
                if (secureBootKey != null)
                {
                    object? uefiEnabled = secureBootKey.GetValue("UEFISecureBootEnabled");
                    if (uefiEnabled is int val && val == 1)
                    {
                        _isUefi = true;
                        return;
                    }

                    // Also check if the key "UEFISecureBoot" exists with value 1
                    object? uefiSecureBoot = secureBootKey.GetValue("UEFISecureBoot");
                    if (uefiSecureBoot is int val2 && val2 == 1)
                    {
                        _isUefi = true;
                        return;
                    }
                }

                // Method 2: Check PEFirmwareType
                using var peKey = Registry.LocalMachine.OpenSubKey(PEFirmwareTypeRegistryPath);
                if (peKey != null)
                {
                    object? firmwareType = peKey.GetValue(null); // (Default) value
                    if (firmwareType is int typeVal && typeVal == 2)
                    {
                        _isUefi = true;
                        return;
                    }

                    // Also try named value
                    object? peType = peKey.GetValue("FirmwareType");
                    if (peType is int peVal && peVal == 2)
                    {
                        _isUefi = true;
                        return;
                    }
                }

                _isUefi = false;
            }
            catch
            {
                _isUefi = false;
            }
        }

        private static DateTime? ParseBiosDate(string dateStr)
        {
            // BIOS dates are typically in format "MM/DD/YYYY" or "MM-DD-YYYY"
            // or sometimes "YYYY-MM-DD"
            if (string.IsNullOrEmpty(dateStr))
                return null;

            // Try common formats
            string[] formats = { "MM/dd/yyyy", "MM-dd-yyyy", "yyyy-MM-dd", "yyyy/MM/dd" };
            foreach (string fmt in formats)
            {
                if (DateTime.TryParseExact(dateStr, fmt, null,
                    System.Globalization.DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }

            // Try general parse as fallback
            if (DateTime.TryParse(dateStr, out DateTime fallback))
                return fallback;

            return null;
        }

        private static string? ExtractSmbiosVersion(string biosVersion)
        {
            // BIOSVersion often contains SMBIOS version in parentheses
            // e.g., "1.2.3 (SMBIOS: 3.2)"
            if (string.IsNullOrEmpty(biosVersion))
                return null;

            int start = biosVersion.IndexOf("SMBIOS", StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return null;

            // Find the version number after "SMBIOS"
            int colon = biosVersion.IndexOf(':', start);
            int versionStart = colon >= 0 ? colon + 1 : start + 6;
            int end = biosVersion.IndexOf(')', versionStart);
            if (end < 0)
                end = biosVersion.Length;

            string version = biosVersion.Substring(versionStart, end - versionStart).Trim();
            return version.Length > 0 ? version : null;
        }
    }
}
