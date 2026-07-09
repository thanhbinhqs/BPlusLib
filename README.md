# BPlusLib

**Multi-purpose Windows utility library: SerialPortInspector, MessageBoxEx, Utils, and more.**

---

## Overview

BPlusLib is a collection of production-ready C# utility libraries for Windows desktop development. All components use pure P/Invoke — **no WMI, no PowerShell, no external executables**.

## Components

### SerialPortInspector
*Namespace: `BPlusLib.SerialPorts`*

Identifies which process owns each Windows Serial Port (COM Port).

```csharp
using BPlusLib.SerialPorts;

var ports = SerialPortInspector.GetAllOpenedSerialPorts();
foreach (var port in ports)
    Console.WriteLine($"{port.PortName} → PID {port.ProcessId} ({port.ProcessName}) v{port.FileVersion}");
```

### MessageBoxEx
*Namespace: `BPlusLib`*

Displays a message box centered on the parent window using WH_CBT hook.

```csharp
var result = MessageBoxEx.Show(
    parentHandle: form.Handle,
    text: "Save changes?",
    caption: "Confirm",
    buttons: MessageBoxExButtons.YesNoCancel,
    icon: MessageBoxExIcon.Question);
```

### Utils
*Namespace: `BPlusLib`*

Utility methods for IP address resolution and command execution.

```csharp
// Get local IP
string? ip = Utils.GetLocalIPAddress();

// Run cmd command
var result = Utils.RunCommand("ipconfig /all", timeoutMs: 10000);
Console.WriteLine(result.StandardOutput);

// Run PowerShell script
var ps = Utils.RunPowerShell("Get-Process | Select Name,CPU | ConvertTo-Json");
```

## Building

```bash
dotnet restore
dotnet build
dotnet pack -c Release
```

## NuGet (GitHub Packages)

```
https://nuget.pkg.github.com/thanhbinhqs/index.json
Package: BPlusLib
Version: 1.0.0
```

## License

MIT