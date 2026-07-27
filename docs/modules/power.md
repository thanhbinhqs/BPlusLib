# Power

Power management helpers for querying system power status, battery information, and controlling sleep/hibernate/shutdown operations. Windows-only via P/Invoke.

## Enums

### AclineStatus
AC line power status.

| Value | Description |
|-------|-------------|
| Offline (0) | AC power is offline (running on battery) |
| Online (1) | AC power is online |
| Unknown (255) | AC power status is unknown |

### BatteryFlag
Battery charge status flags (Flags enum).

| Value | Description |
|-------|-------------|
| High (1) | Battery is at high charge level |
| Low (2) | Battery is low |
| Critical (4) | Battery is critically low |
| Charging (8) | Battery is charging |
| NoBattery (128) | No system battery is present |
| Unknown (255) | Battery status is unknown |

## Classes

### SystemPowerStatus
Represents the current system power status.

| Property | Returns | Description |
|----------|---------|-------------|
| AclineStatus | AclineStatus | AC line power status |
| BatteryFlag | BatteryFlag | Battery charge status flags |
| BatteryChargePercent | int | Battery charge percentage (0-100) |
| BatteryLifeSeconds | int | Remaining battery life in seconds, or -1 if unknown |
| BatteryFullLifeSeconds | int | Full battery lifetime in seconds, or -1 if unknown |
| IsOnBattery | bool | True if the system is running on battery power |
| BatteryIsCharging | bool | True if the battery is currently charging |

### PowerHelper
Power management helpers for querying status and controlling system power states.

| Method | Returns | Description |
|--------|---------|-------------|
| GetPowerStatus() | static SystemPowerStatus? | Gets the current system power status |
| IsOnBattery() | static bool | Returns true if running on battery |
| GetBatteryChargePercent() | static int | Returns battery charge percentage (0-100), or -1 if unknown |
| Sleep() | static bool | Puts the system to sleep |
| Hibernate() | static bool | Puts the system into hibernation |
| LockWorkstation() | static bool | Locks the workstation |
| Shutdown(bool force, bool reboot) | static bool | Shuts down or restarts the system (requires SE_SHUTDOWN_NAME privilege) |
| Restart(bool force) | static bool | Restarts the system |
| LogOff(bool force) | static bool | Logs off the current user |
| PreventSleep(bool prevent) | static uint | Prevents the system from sleeping; returns previous execution state flags |
| IsHibernationEnabled() | static bool | Returns true if hibernation is available on the system |

## Usage

```csharp
using BPlusLib.Foundation.Power;

// Query power status
var status = PowerHelper.GetPowerStatus();
if (status != null)
{
    Console.WriteLine($"AC: {status.AclineStatus}");
    Console.WriteLine($"Battery: {status.BatteryChargePercent}%");
    Console.WriteLine($"On Battery: {status.IsOnBattery}");
    Console.WriteLine($"Charging: {status.BatteryIsCharging}");
}

// Quick checks
bool onBattery = PowerHelper.IsOnBattery();
int charge = PowerHelper.GetBatteryChargePercent();

// Power actions
PowerHelper.Sleep();
PowerHelper.Hibernate();
PowerHelper.LockWorkstation();
PowerHelper.Shutdown(force: false, reboot: false);
PowerHelper.Restart(force: true);
PowerHelper.LogOff(force: false);

// Prevent sleep during critical operation
uint prevState = PowerHelper.PreventSleep(prevent: true);
// ... perform critical work ...
PowerHelper.PreventSleep(prevent: false); // Restore normal behavior
```

## Dependencies
- `kernel32.dll` (P/Invoke for `GetSystemPowerStatus`, `SetThreadExecutionState`)
- `powrprof.dll` (P/Invoke for `SetSuspendState`)
- `user32.dll` (P/Invoke for `ExitWindowsEx`, `LockWorkStation`)
- `BPlusLib.Foundation.Native` (for `Kernel32`, `PowrProf`, `User32` wrappers)
- Windows-only
