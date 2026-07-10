// <copyright file="OperatingSystemInfo.cs" company="BPlusLib">
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
    /// Provides detailed information about the host operating system —
    /// name, version, edition, architecture, boot time, and more.
    /// All data is obtained via P/Invoke (kernel32, ntdll) and registry reads;
    /// no WMI dependency.
    /// </summary>
    public sealed class OperatingSystemInfo
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const int ProductWorkstation = 1;
        private const int ProductDomainController = 2;
        private const int ProductServer = 3;

        private static readonly string RegistryKeyWindowsNt =
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        // =====================================================================
        // P/Invoke structs
        // =====================================================================

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OSVERSIONINFOEXW
        {
            internal int dwOSVersionInfoSize;
            internal int dwMajorVersion;
            internal int dwMinorVersion;
            internal int dwBuildNumber;
            internal int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
            internal ushort wServicePackMajor;
            internal ushort wServicePackMinor;
            internal ushort wSuiteMask;
            internal byte wProductType;
            internal byte wReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            internal ushort wProcessorArchitecture;
            internal ushort wReserved;
            internal uint dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal ushort wProcessorLevel;
            internal ushort wProcessorRevision;
        }

        // =====================================================================
        // P/Invoke declarations
        // =====================================================================

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEXW lpVersionInformation);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        private static extern long GetTickCount64();

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

        // =====================================================================
        // Lazy singleton
        // =====================================================================

        private static readonly Lazy<OperatingSystemInfo> LazyCurrent =
            new Lazy<OperatingSystemInfo>(() => new OperatingSystemInfo());

        /// <summary>
        /// Gets the singleton <see cref="OperatingSystemInfo"/> instance
        /// representing the host operating system.
        /// </summary>
        public static OperatingSystemInfo Current => LazyCurrent.Value;

        // =====================================================================
        // Private constructor — populate all properties
        // =====================================================================

        private OperatingSystemInfo()
        {
            LoadVersionInfo();

            string arch;
            try
            {
                var nativeInfo = default(SYSTEM_INFO);
                GetNativeSystemInfo(ref nativeInfo);
                arch = nativeInfo.wProcessorArchitecture switch
                {
                    0 => "x86",
                    9 => "x64",
                    12 => "ARM64",
                    5 => "ARM",
                    _ => "Unknown"
                };
                _is64Bit = nativeInfo.wProcessorArchitecture == 9 ||
                           nativeInfo.wProcessorArchitecture == 12;
            }
            catch
            {
                arch = IntPtr.Size == 8 ? "x64" : "x86";
                _is64Bit = IntPtr.Size == 8;
            }

            _architecture = arch;

            try
            {
                _is64Bit = _is64Bit || CheckIsWow64();
            }
            catch
            {
                // Fallback already set
            }

            LoadRegistryInfo();
            ComputeBootTime();
        }

        // =====================================================================
        // Backing fields
        // =====================================================================

        private string _name = string.Empty;
        private string _version = string.Empty;
        private string _edition = string.Empty;
        private int _buildNumber;
        private string _servicePack = string.Empty;
        private string _architecture = string.Empty;
        private DateTime? _installDate;
        private DateTime? _lastBootUpTime;
        private bool _isServer;
        private bool _is64Bit;
        private int _suiteMask;
        private byte _productType;
        private string _csdVersion = string.Empty;

        // =====================================================================
        // Public properties
        // =====================================================================

        /// <summary>Gets the OS display name (e.g., "Windows 10 Pro").</summary>
        public string Name => _name;

        /// <summary>Gets the OS version string (e.g., "10.0.19041").</summary>
        public string Version => _version;

        /// <summary>Gets the OS edition (e.g., "Professional", "Enterprise").</summary>
        public string Edition => _edition;

        /// <summary>Gets the OS build number.</summary>
        public int BuildNumber => _buildNumber;

        /// <summary>Gets the service pack level (empty string if none).</summary>
        public string ServicePack => _servicePack;

        /// <summary>Gets the processor architecture string ("x86", "x64", "ARM64").</summary>
        public string Architecture => _architecture;

        /// <summary>Gets the date the OS was installed, if available.</summary>
        public DateTime? InstallDate => _installDate;

        /// <summary>Gets the last boot time, if computable.</summary>
        public DateTime? LastBootUpTime => _lastBootUpTime;

        /// <summary>Gets whether the OS is a server SKU.</summary>
        public bool IsServer => _isServer;

        /// <summary>Gets whether the OS is 64-bit.</summary>
        public bool Is64Bit => _is64Bit;

        /// <summary>Gets the product suite mask from the OS version info.</summary>
        public int SuiteMask => _suiteMask;

        /// <summary>Gets the product type byte (VER_NT_WORKSTATION, VER_NT_SERVER, etc.).</summary>
        public byte ProductType => _productType;

        /// <summary>Gets the CSD version string from the OS version info.</summary>
        public string CSDVersion => _csdVersion;

        // =====================================================================
        // Private methods
        // =====================================================================

        private void LoadVersionInfo()
        {
            try
            {
                var osvi = default(OSVERSIONINFOEXW);
                osvi.dwOSVersionInfoSize = Marshal.SizeOf<OSVERSIONINFOEXW>();
                int result = RtlGetVersion(ref osvi);
                if (result == 0)
                {
                    _version = $"{osvi.dwMajorVersion}.{osvi.dwMinorVersion}.{osvi.dwBuildNumber}";
                    _buildNumber = osvi.dwBuildNumber;
                    _servicePack = osvi.wServicePackMajor > 0
                        ? $"Service Pack {osvi.wServicePackMajor}.{osvi.wServicePackMinor}"
                        : string.Empty;
                    _suiteMask = osvi.wSuiteMask;
                    _productType = osvi.wProductType;
                    _csdVersion = osvi.szCSDVersion ?? string.Empty;
                    _isServer = osvi.wProductType != ProductWorkstation;
                }
            }
            catch
            {
                // Fallback: Environment.OSVersion
                try
                {
                    var os = Environment.OSVersion;
                    _version = $"{os.Version.Major}.{os.Version.Minor}.{os.Version.Build}";
                    _buildNumber = os.Version.Build;
                }
                catch
                {
                    _version = "0.0.0";
                }
            }
        }

        private bool CheckIsWow64()
        {
            try
            {
                var hProcess = GetCurrentProcessHandle();
                if (hProcess != IntPtr.Zero &&
                    IsWow64Process(hProcess, out bool isWow64))
                {
                    return isWow64;
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        private static IntPtr GetCurrentProcessHandle()
        {
            try
            {
                return System.Runtime.InteropServices.Marshal.GetHINSTANCE(typeof(OperatingSystemInfo).Module);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private void LoadRegistryInfo()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryKeyWindowsNt);
                if (key == null) return;

                string? productName = key.GetValue("ProductName") as string;
                string? editionId = key.GetValue("EditionID") as string;
                string? displayVersion = key.GetValue("DisplayVersion") as string;
                string? releaseId = key.GetValue("ReleaseId") as string;
                object? installDateObj = key.GetValue("InstallDate");

                _edition = editionId ?? string.Empty;

                // Construct friendly name
                string ver = displayVersion ?? releaseId ?? string.Empty;
                if (!string.IsNullOrEmpty(productName))
                {
                    _name = ver.Length > 0 ? $"{productName} (Version {ver})" : productName!;
                }
                else
                {
                    _name = $"Windows (Version {_version})";
                }

                // InstallDate is stored as a DWORD (Unix timestamp)
                if (installDateObj is int installTimestamp && installTimestamp > 0)
                {
                    _installDate = DateTimeOffset.FromUnixTimeSeconds(installTimestamp).DateTime;
                }

                // Also read CSDVersion from registry for older OS
                string? regCSD = key.GetValue("CSDVersion") as string;
                if (!string.IsNullOrEmpty(regCSD) && string.IsNullOrEmpty(_csdVersion))
                {
                    _csdVersion = regCSD!;
                }

                // Check if server by reading InstallationType
                string? installationType = key.GetValue("InstallationType") as string;
                if (!string.IsNullOrEmpty(installationType))
                {
                    _isServer = installationType!.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch
            {
                // Registry read failed — non-fatal
            }
        }

        private void ComputeBootTime()
        {
            try
            {
                long tickCount = GetTickCount64();
                // tickCount is milliseconds since boot
                _lastBootUpTime = DateTime.UtcNow.AddMilliseconds(-tickCount);
            }
            catch
            {
                _lastBootUpTime = null;
            }
        }
    }
}
