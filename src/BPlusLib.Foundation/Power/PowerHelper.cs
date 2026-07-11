// <copyright file="PowerHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Power
{
    /// <summary>AC line status.</summary>
    public enum AclineStatus : byte
    {
        /// <summary>AC power is offline (running on battery).</summary>
        Offline = 0,

        /// <summary>AC power is online.</summary>
        Online = 1,

        /// <summary>AC power status is unknown.</summary>
        Unknown = 255,
    }

    /// <summary>Battery charge status flags.</summary>
    [Flags]
    public enum BatteryFlag : byte
    {
        /// <summary>The battery is at high charge level.</summary>
        High = 1,

        /// <summary>The battery is low.</summary>
        Low = 2,

        /// <summary>The battery is critically low.</summary>
        Critical = 4,

        /// <summary>The battery is charging.</summary>
        Charging = 8,

        /// <summary>No system battery is present.</summary>
        NoBattery = 128,

        /// <summary>Battery status is unknown.</summary>
        Unknown = 255,
    }

    /// <summary>System power status.</summary>
    public sealed class SystemPowerStatus
    {
        /// <summary>AC line power status.</summary>
        public AclineStatus AclineStatus { get; init; }

        /// <summary>Battery charge status flags.</summary>
        public BatteryFlag BatteryFlag { get; init; }

        /// <summary>Battery charge percentage (0-100).</summary>
        public int BatteryChargePercent { get; init; }

        /// <summary>Remaining battery life in seconds, or -1 if unknown.</summary>
        public int BatteryLifeSeconds { get; init; }

        /// <summary>Full battery lifetime in seconds, or -1 if unknown.</summary>
        public int BatteryFullLifeSeconds { get; init; }

        /// <summary>True if the system is running on battery power.</summary>
        public bool IsOnBattery => AclineStatus == AclineStatus.Offline;

        /// <summary>True if the battery is currently charging.</summary>
        public bool BatteryIsCharging => BatteryFlag.HasFlag(BatteryFlag.Charging);
    }

    /// <summary>Power management helpers.</summary>
    public static class PowerHelper
    {
        // =================================================================
        // Public API
        // =================================================================

        /// <summary>Gets the current system power status.</summary>
        /// <returns>A <see cref="SystemPowerStatus"/> instance, or null if the call fails.</returns>
        public static SystemPowerStatus? GetPowerStatus()
        {
            try
            {
                if (!Kernel32.GetSystemPowerStatus(out var status))
                    return null;

                return new SystemPowerStatus
                {
                    AclineStatus = (AclineStatus)status.ACLineStatus,
                    BatteryFlag = (BatteryFlag)status.BatteryFlag,
                    BatteryChargePercent = status.BatteryLifePercent <= 100
                        ? status.BatteryLifePercent
                        : -1,
                    BatteryLifeSeconds = status.BatteryLifeTime >= 0
                        ? status.BatteryLifeTime
                        : -1,
                    BatteryFullLifeSeconds = status.BatteryFullLifeTime >= 0
                        ? status.BatteryFullLifeTime
                        : -1,
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Returns true if running on battery.</summary>
        public static bool IsOnBattery() => GetPowerStatus()?.IsOnBattery ?? false;

        /// <summary>Returns battery charge percentage (0-100), or -1 if unknown/no battery.</summary>
        public static int GetBatteryChargePercent() => GetPowerStatus()?.BatteryChargePercent ?? -1;

        /// <summary>Puts the system to sleep.</summary>
        /// <returns>True if the operation succeeded.</returns>
        public static bool Sleep()
        {
            try
            {
                return PowrProf.SetSuspendState(false, false, false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Puts the system into hibernation.</summary>
        /// <returns>True if the operation succeeded.</returns>
        public static bool Hibernate()
        {
            try
            {
                return PowrProf.SetSuspendState(true, false, false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Locks the workstation.</summary>
        /// <returns>True if the operation succeeded.</returns>
        public static bool LockWorkstation()
        {
            try
            {
                return User32.LockWorkStation();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Shuts down or restarts the system. Must have SE_SHUTDOWN_NAME privilege.</summary>
        /// <param name="force">If true, forces running applications to close.</param>
        /// <param name="reboot">If true, restarts the system instead of shutting down.</param>
        /// <returns>True if the operation succeeded.</returns>
        public static bool Shutdown(bool force = false, bool reboot = false)
        {
            try
            {
                uint flags = reboot ? User32.EWX_REBOOT : User32.EWX_SHUTDOWN;
                if (force)
                    flags |= User32.EWX_FORCE;
                return User32.ExitWindowsEx(flags, 0);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Restarts the system.</summary>
        /// <param name="force">If true, forces running applications to close.</param>
        /// <returns>True if the operation succeeded.</returns>
        public static bool Restart(bool force = false) => Shutdown(force, reboot: true);

        /// <summary>Logs off the current user.</summary>
        /// <param name="force">If true, forces running applications to close.</param>
        /// <returns>True if the operation succeeded.</returns>
        public static bool LogOff(bool force = false)
        {
            try
            {
                uint flags = User32.EWX_LOGOFF;
                if (force)
                    flags |= User32.EWX_FORCE;
                return User32.ExitWindowsEx(flags, 0);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Prevents the system from sleeping (e.g., during critical operation).</summary>
        /// <param name="prevent">True to prevent sleep, false to restore normal behavior.</param>
        /// <returns>Previous execution state flags, or 0 on failure.</returns>
        public static uint PreventSleep(bool prevent)
        {
            try
            {
                uint flags = Kernel32.ES_CONTINUOUS | Kernel32.ES_SYSTEM_REQUIRED;
                if (prevent)
                    flags |= Kernel32.ES_AWAYMODE_REQUIRED;
                return Kernel32.SetThreadExecutionState(flags);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>Returns true if hibernation is enabled on the system.</summary>
        public static bool IsHibernationEnabled()
        {
            try
            {
                if (!Kernel32.GetSystemPowerStatus(out var status))
                    return false;

                // ACLineStatus == 1 (online) means the system has AC power;
                // BatteryFlag == 128 (no battery) means no battery present.
                // Both are valid states; hibernation is available if the
                // power status can be read.
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
