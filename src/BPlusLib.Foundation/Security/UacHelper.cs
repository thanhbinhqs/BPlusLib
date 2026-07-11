// <copyright file="UacHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// Provides methods to detect UAC state, process elevation, integrity level,
    /// and to launch processes with elevated privileges (runas).
    /// All methods are thread-safe and gracefully return false/null on error.
    /// </summary>
    public static class UacHelper
    {
        private const string RegistryKeyUac = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
        private const string RegistryValueEnableLua = "EnableLUA";
        private const string RegistryValueConsentPromptBehaviorAdmin = "ConsentPromptBehaviorAdmin";

        /// <summary>
        /// Returns true if the current process is running elevated (as administrator).
        /// Uses TOKEN_ELEVATION — no WMI.
        /// </summary>
        public static bool IsElevated()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                IntPtr tokenHandle = IntPtr.Zero;
                if (!AdvApi32.OpenProcessToken(
                        AdvApi32.GetCurrentProcess(),
                        AdvApi32.TOKEN_QUERY,
                        out tokenHandle))
                    return false;

                try
                {
                    int size = Marshal.SizeOf<TOKEN_ELEVATION>();
                    IntPtr buf = Marshal.AllocHGlobal(size);
                    try
                    {
                        if (AdvApi32.GetTokenInformation(
                                tokenHandle,
                                AdvApi32.TokenElevation,
                                buf,
                                (uint)size,
                                out _))
                        {
                            var elevation = Marshal.PtrToStructure<TOKEN_ELEVATION>(buf);
                            return elevation.TokenIsElevated != 0;
                        }
                        return false;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buf);
                    }
                }
                finally
                {
                    AdvApi32.CloseHandle(tokenHandle);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the integrity level of the current process.
        /// </summary>
        public static IntegrityLevel GetIntegrityLevel()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IntegrityLevel.Unknown;

            try
            {
                IntPtr tokenHandle = IntPtr.Zero;
                if (!AdvApi32.OpenProcessToken(
                        AdvApi32.GetCurrentProcess(),
                        AdvApi32.TOKEN_QUERY,
                        out tokenHandle))
                    return IntegrityLevel.Unknown;

                try
                {
                    // First query required size
                    uint needed = 0;
                    bool result = AdvApi32.GetTokenInformation(
                        tokenHandle,
                        AdvApi32.TokenIntegrityLevel,
                        IntPtr.Zero,
                        0,
                        out needed);

                    int lastErr = Marshal.GetLastWin32Error();
                    if (result || (lastErr != 122 && lastErr != 24)) // ERROR_INSUFFICIENT_BUFFER or ERROR_BAD_LENGTH
                    {
                        // Allocate buffer
                        IntPtr buf = Marshal.AllocHGlobal((int)needed);
                        try
                        {
                            if (AdvApi32.GetTokenInformation(
                                    tokenHandle,
                                    AdvApi32.TokenIntegrityLevel,
                                    buf,
                                    needed,
                                    out _))
                            {
                                // The integrity level SID is at the start of the TOKEN_MANDATORY_LABEL structure
                                // TOKEN_MANDATORY_LABEL has one member: SID_AND_ATTRIBUTES
                                // SID_AND_ATTRIBUTES has SID pointer + attributes
                                // First 4/8 bytes is IntPtr (SID), next 4 bytes is attributes
                                IntPtr sidPtr = Marshal.ReadIntPtr(buf);

                                if (sidPtr != IntPtr.Zero)
                                {
                                    IntPtr sidStr;
                                    if (AdvApi32.ConvertSidToStringSidW(sidPtr, out sidStr))
                                    {
                                        try
                                        {
                                            string sid = Marshal.PtrToStringUni(sidStr) ?? "";
                                            // Parse S-1-16-XXXXX
                                            int lastDash = sid.LastIndexOf('-');
                                            if (lastDash > 0 && int.TryParse(sid.Substring(lastDash + 1), out int level))
                                                return (IntegrityLevel)level;
                                        }
                                        finally
                                        {
                                            AdvApi32.LocalFree(sidStr);
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buf);
                        }
                    }
                    return IntegrityLevel.Unknown;
                }
                finally
                {
                    AdvApi32.CloseHandle(tokenHandle);
                }
            }
            catch
            {
                return IntegrityLevel.Unknown;
            }
        }

        /// <summary>
        /// Returns true if the current process is running as a standard user
        /// (not elevated, not SYSTEM).
        /// </summary>
        public static bool IsStandardUser()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true;

            IntegrityLevel level = GetIntegrityLevel();
            return level == IntegrityLevel.Medium || level == IntegrityLevel.MediumPlus;
        }

        /// <summary>
        /// Returns true if UAC is enabled on the system (EnableLUA registry value).
        /// </summary>
        public static bool IsUacEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryKeyUac);
                if (key is null) return false;
                object? value = key.GetValue(RegistryValueEnableLua);
                return value is int intVal && intVal == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the UAC consent prompt behavior for administrators.
        /// 0 = Elevate without prompting, 1 = Prompt for credentials, 2 = Prompt for consent, etc.
        /// </summary>
        public static int GetConsentPromptBehavior()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryKeyUac);
                if (key is null) return -1;
                object? value = key.GetValue(RegistryValueConsentPromptBehaviorAdmin);
                return value is int intVal ? intVal : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Restarts the current executable with elevated privileges (runas verb).
        /// Returns true if the elevated process was launched.
        /// The current process does NOT exit automatically.
        /// </summary>
        /// <param name="arguments">Optional command-line arguments for the elevated process.</param>
        /// <returns>True if the elevated process was launched.</returns>
        public static bool RunElevated(string? arguments = null)
        {
            string? executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
                return false;

            return RunAsAdmin(executablePath, arguments);
        }

        /// <summary>
        /// Launches a specific executable with elevated privileges (runas verb).
        /// </summary>
        /// <param name="executablePath">Path to the executable.</param>
        /// <param name="arguments">Optional command-line arguments.</param>
        /// <returns>True if the process was launched.</returns>
        public static bool RunAsAdmin(string executablePath, string? arguments = null)
        {
            if (string.IsNullOrEmpty(executablePath))
                return false;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                var info = new SHELLEXECUTEINFOW();
                info.cbSize = Marshal.SizeOf<SHELLEXECUTEINFOW>();
                info.lpVerb = "runas";
                info.lpFile = executablePath;
                info.lpParameters = arguments;
                info.nShow = 1; // SW_NORMAL
                info.fMask = 0x00000400; // SEE_MASK_FLAG_NO_UI

                return Shell32.ShellExecuteExW(ref info);
            }
            catch
            {
                return false;
            }
        }
    }
}
