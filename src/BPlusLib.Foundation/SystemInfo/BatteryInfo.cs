// <copyright file="BatteryInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Defines flags returned by <see cref="BatteryInfo.StatusFlags"/>
    /// from GetSystemPowerStatus.
    /// </summary>
    [Flags]
    public enum BatteryStatusFlags : byte
    {
        /// <summary>No battery status flags (default).</summary>
        None = 0,

        /// <summary>The battery is currently discharging.</summary>
        Discharging = 1,

        /// <summary>The AC power is offline (system is on battery).</summary>
        AcOffline = 2,

        /// <summary>The battery is charging.</summary>
        Charging = 4,

        /// <summary>The battery is low.</summary>
        LowBattery = 8,

        /// <summary>The battery is critically low.</summary>
        CriticalBattery = 16,
    }

    /// <summary>
    /// Provides information about the system battery status and
    /// capabilities using P/Invoke (kernel32) and registry reads.
    /// No WMI dependency.
    /// </summary>
    public sealed class BatteryInfo
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private static readonly string BatteryClassGuid =
            "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}";

        private static readonly string BatteryRegistryPath =
            $@"SYSTEM\CurrentControlSet\Control\Class\{BatteryClassGuid}\0000";

        // =====================================================================
        // P/Invoke struct
        // =====================================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            internal byte ACLineStatus;
            internal byte BatteryFlag;
            internal byte BatteryLifePercent;
            internal byte Reserved1;
            internal int BatteryLifeTime;
            internal int BatteryFullLifeTime;
        }

        // =====================================================================
        // P/Invoke
        // =====================================================================

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        // =====================================================================
        // Lazy singleton
        // =====================================================================

        private static readonly Lazy<BatteryInfo> LazyCurrent =
            new Lazy<BatteryInfo>(() => new BatteryInfo());

        /// <summary>
        /// Gets the singleton <see cref="BatteryInfo"/> instance
        /// representing the current battery state.
        /// </summary>
        public static BatteryInfo Current => LazyCurrent.Value;

        // =====================================================================
        // Private constructor
        // =====================================================================

        private BatteryInfo()
        {
            LoadFromSystemPowerStatus();
            LoadFromRegistry();
        }

        // =====================================================================
        // Backing fields
        // =====================================================================

        private bool _isPresent;
        private int _estimatedChargePercent;
        private bool _isCharging;
        private BatteryStatusFlags _statusFlags;
        private int? _batteryLifeSeconds;
        private int? _batteryFullLifeSeconds;
        private double? _voltageMillivolts;
        private string? _chemistry;
        private int? _designCapacityMW;
        private int? _currentCapacityMW;

        // =====================================================================
        // Public properties
        // =====================================================================

        /// <summary>Gets whether a system battery is present.</summary>
        public bool IsPresent => _isPresent;

        /// <summary>Gets the estimated charge percentage (0–100). Returns 0 if no battery present.</summary>
        public int EstimatedChargePercent => _estimatedChargePercent;

        /// <summary>Gets whether the battery is currently charging.</summary>
        public bool IsCharging => _isCharging;

        /// <summary>Gets the raw battery status flags from GetSystemPowerStatus.</summary>
        public BatteryStatusFlags StatusFlags => _statusFlags;

        /// <summary>Gets the remaining battery life in seconds, or <c>null</c> if unknown.</summary>
        public int? BatteryLifeSeconds => _batteryLifeSeconds;

        /// <summary>Gets the full charge battery lifetime in seconds, or <c>null</c> if unknown.</summary>
        public int? BatteryFullLifeSeconds => _batteryFullLifeSeconds;

        /// <summary>Gets the battery voltage in millivolts, or <c>null</c> if unknown.</summary>
        public double? VoltageMillivolts => _voltageMillivolts;

        /// <summary>Gets the battery chemistry string (e.g., "LION", "NiMH", "PbAc"), or <c>null</c>.</summary>
        public string? Chemistry => _chemistry;

        /// <summary>Gets the design capacity in milliwatt-hours, or <c>null</c>.</summary>
        public int? DesignCapacityMW => _designCapacityMW;

        /// <summary>Gets the current capacity in milliwatt-hours, or <c>null</c>.</summary>
        public int? CurrentCapacityMW => _currentCapacityMW;

        // =====================================================================
        // Private methods
        // =====================================================================

        private void LoadFromSystemPowerStatus()
        {
            try
            {
                if (!GetSystemPowerStatus(out var status))
                    return;

                // ACLineStatus: 0 = offline, 1 = online, 255 = unknown
                _isCharging = status.ACLineStatus == 1;
                _isPresent = status.BatteryFlag != 255; // 255 = no battery

                // BatteryLifePercent: 0-100, 255 = unknown
                if (status.BatteryLifePercent <= 100)
                    _estimatedChargePercent = status.BatteryLifePercent;
                else
                    _estimatedChargePercent = _isPresent ? 0 : 0;

                // BatteryFlag: bitmask
                BatteryStatusFlags flags = 0;
                if ((status.BatteryFlag & 1) != 0) flags |= BatteryStatusFlags.Discharging;
                if (status.ACLineStatus == 0 && _isPresent) flags |= BatteryStatusFlags.AcOffline;
                if ((status.BatteryFlag & 4) != 0 || _isCharging) flags |= BatteryStatusFlags.Charging;
                if ((status.BatteryFlag & 8) != 0) flags |= BatteryStatusFlags.LowBattery;
                if ((status.BatteryFlag & 16) != 0) flags |= BatteryStatusFlags.CriticalBattery;
                _statusFlags = flags;

                // BatteryLifeTime: -1 = unknown
                if (status.BatteryLifeTime >= 0)
                    _batteryLifeSeconds = status.BatteryLifeTime;

                // BatteryFullLifeTime: -1 = unknown
                if (status.BatteryFullLifeTime >= 0)
                    _batteryFullLifeSeconds = status.BatteryFullLifeTime;
            }
            catch
            {
                // Non-Windows or unsupported
            }
        }

        private void LoadFromRegistry()
        {
            try
            {
                // Try the standard battery class device path first
                using var key = Registry.LocalMachine.OpenSubKey(BatteryRegistryPath);
                if (key == null)
                {
                    // Try to enumerate battery subkeys under the class GUID
                    using var classKey = Registry.LocalMachine.OpenSubKey(
                        $@"SYSTEM\CurrentControlSet\Control\Class\{BatteryClassGuid}");
                    if (classKey != null)
                    {
                        foreach (string subKeyName in classKey.GetSubKeyNames())
                        {
                            if (subKeyName.Length == 4 && int.TryParse(subKeyName, out _))
                            {
                                using var subKey = classKey.OpenSubKey(subKeyName);
                                if (subKey != null)
                                {
                                    ReadBatteryRegistryValues(subKey);
                                }
                            }
                        }
                    }

                    return;
                }

                ReadBatteryRegistryValues(key);
            }
            catch
            {
                // Registry read failed — non-fatal
            }
        }

        private void ReadBatteryRegistryValues(RegistryKey key)
        {
            try
            {
                // Chemistry
                object? chemistry = key.GetValue("Chemistry");
                if (chemistry is byte[] chemBytes && chemBytes.Length >= 4)
                {
                    _chemistry = Encoding.ASCII.GetString(chemBytes, 0, 4).TrimEnd('\0');
                }
                else if (chemistry is int chemInt)
                {
                    // Some systems store chemistry as a DWORD
                    _chemistry = chemInt switch
                    {
                        1 => "PbAc",
                        2 => "NiCd",
                        3 => "NiMH",
                        4 => "LION",
                        5 => "LiPo",
                        6 => "NiZn",
                        7 => "RAM",
                        8 => "AgO",
                        9 => "AgZn",
                        _ => null
                    };
                }

                // Design capacity (in milliwatt-hours or similar)
                object? designedCapacity = key.GetValue("DesignedCapacity");
                if (designedCapacity is int dc)
                    _designCapacityMW = dc;

                // Current/full charge capacity
                object? fullChargedCapacity = key.GetValue("FullChargedCapacity");
                if (fullChargedCapacity is int fcc)
                    _currentCapacityMW = fcc;

                // Voltage
                object? voltage = key.GetValue("Voltage");
                if (voltage is int v)
                {
                    // Voltage is typically in millivolts already, but may be in
                    // microvolts. We'll store as-is.
                    _voltageMillivolts = v;
                }

                // Battery tag (confirms presence)
                object? batteryTag = key.GetValue("BatteryTag");
                if (batteryTag is int tag && tag > 0)
                    _isPresent = true;
            }
            catch
            {
                // Ignore individual value failures
            }
        }
    }
}
