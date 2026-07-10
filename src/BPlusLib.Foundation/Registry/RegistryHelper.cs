// <copyright file="RegistryHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

#if NET472
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that when a method returns, the parameter may be null even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute
    {
    }

    /// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }
}
#endif

namespace BPlusLib.Foundation.Registry
{
    /// <summary>
    /// Represents the well-known registry hives used for path parsing.
    /// </summary>
    public enum RegistryHive
    {
        /// <summary>HKEY_CLASSES_ROOT</summary>
        ClassesRoot,

        /// <summary>HKEY_CURRENT_USER</summary>
        CurrentUser,

        /// <summary>HKEY_LOCAL_MACHINE</summary>
        LocalMachine,

        /// <summary>HKEY_USERS</summary>
        Users,

        /// <summary>HKEY_CURRENT_CONFIG</summary>
        CurrentConfig,

        /// <summary>HKEY_PERFORMANCE_DATA</summary>
        PerformanceData,
    }

    /// <summary>
    /// Represents a single registry value entry with its data and kind.
    /// </summary>
    public class RegistryValueEntry
    {
        /// <summary>
        /// Gets or sets the name of the registry value.
        /// An empty string represents the default (unnamed) value.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value data.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets the registry value kind.
        /// </summary>
        public RegistryValueKind Kind { get; set; } = RegistryValueKind.Unknown;
    }

    /// <summary>
    /// Represents a snapshot backup of a registry key and its subkeys.
    /// </summary>
    public class RegistryBackup
    {
        /// <summary>
        /// Gets or sets the full registry key path (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\...").
        /// </summary>
        public string KeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time when the backup was taken.
        /// </summary>
        public DateTime BackupTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the dictionary of captured values.
        /// Keys are value names for the root key, or "SubKeyName\ValueName" for values in subkeys.
        /// </summary>
        public Dictionary<string, RegistryValueEntry> Values { get; set; } =
            new Dictionary<string, RegistryValueEntry>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Thread-safe registry helper providing safe read/write operations, .reg export/import,
    /// and backup/restore functionality. All methods catch exceptions internally and return
    /// <see langword="false"/> or <see langword="null"/> rather than throwing.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Microsoft.Win32.Registry"/> and <see cref="RegistryKey"/> classes.
    /// The registry hive is parsed from the path prefix
    /// (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\...").
    /// On non-Windows platforms registry calls throw <see cref="PlatformNotSupportedException"/>,
    /// which is always caught and handled gracefully.
    /// </remarks>
    public static class RegistryHelper
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const string RegFileHeader = "Windows Registry Editor Version 5.00";
        private const string DefaultValueName = "";

        // -----------------------------------------------------------------
        // Public read operations
        // -----------------------------------------------------------------

        /// <summary>
        /// Reads a registry value and returns it as <typeparamref name="T"/>.
        /// Supported types: <see cref="string"/>, <see cref="int"/>,
        /// <see cref="long"/>, <see cref="byte"/>[], <see cref="string"/>[].
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="keyPath">Full registry key path (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\...").</param>
        /// <param name="valueName">The name of the value to read.</param>
        /// <returns>The value converted to <typeparamref name="T"/>, or <see langword="default"/> on failure.</returns>
        [return: MaybeNull]
        public static T? GetValue<T>(string keyPath, string valueName)
        {
            if (string.IsNullOrEmpty(keyPath))
                return default;

            try
            {
                if (!TryOpenKey(keyPath, writable: false, out var hive, out var subKey, out var key))
                    return default;

                using (key)
                {
                    object? data = key.GetValue(valueName);
                    if (data is null)
                        return default;

                    return (T)ConvertValue<T>(data);
                }
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Reads a registry string value.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <returns>The string value, or <see langword="null"/> on failure.</returns>
        public static string? GetString(string keyPath, string valueName)
        {
            return GetValue<string>(keyPath, valueName);
        }

        /// <summary>
        /// Reads a registry DWORD (32-bit integer) value.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <returns>The integer value, or <see langword="null"/> on failure.</returns>
        public static int? GetDWord(string keyPath, string valueName)
        {
            return GetValue<int>(keyPath, valueName);
        }

        /// <summary>
        /// Reads a registry QWORD (64-bit integer) value.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <returns>The long value, or <see langword="null"/> on failure.</returns>
        public static long? GetQWord(string keyPath, string valueName)
        {
            return GetValue<long>(keyPath, valueName);
        }

        /// <summary>
        /// Reads a registry binary value.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <returns>The byte array, or <see langword="null"/> on failure.</returns>
        public static byte[]? GetBinary(string keyPath, string valueName)
        {
            return GetValue<byte[]>(keyPath, valueName);
        }

        /// <summary>
        /// Reads a registry MULTI_SZ (multi-string) value.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <returns>The string array, or <see langword="null"/> on failure.</returns>
        public static string[]? GetMultiString(string keyPath, string valueName)
        {
            return GetValue<string[]>(keyPath, valueName);
        }

        /// <summary>
        /// Checks whether the specified value exists under the given key.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value to check.</param>
        /// <returns><see langword="true"/> if the value exists; otherwise <see langword="false"/>.</returns>
        public static bool ValueExists(string keyPath, string valueName)
        {
            if (string.IsNullOrEmpty(keyPath))
                return false;

            try
            {
                if (!TryOpenKey(keyPath, writable: false, out var hive, out var subKey, out var key))
                    return false;

                using (key)
                {
                    return key.GetValue(valueName) is not null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether the specified registry key exists.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <returns><see langword="true"/> if the key exists; otherwise <see langword="false"/>.</returns>
        public static bool KeyExists(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
                return false;

            try
            {
                if (!TryParseHive(keyPath, out var hive, out var subKey))
                    return false;

                using var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return false;

                using var key = hiveKey.OpenSubKey(subKey);
                return key is not null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the names of all subkeys under the specified key.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <returns>A read-only list of subkey names, or an empty list on failure.</returns>
        public static IReadOnlyList<string> GetSubKeyNames(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
                return Array.Empty<string>();

            try
            {
                if (!TryOpenKey(keyPath, writable: false, out var hive, out var subKey, out var key))
                    return Array.Empty<string>();

                using (key)
                {
                    return key.GetSubKeyNames();
                }
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Gets the names of all values under the specified key.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <returns>A read-only list of value names, or an empty list on failure.</returns>
        public static IReadOnlyList<string> GetValueNames(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
                return Array.Empty<string>();

            try
            {
                if (!TryOpenKey(keyPath, writable: false, out var hive, out var subKey, out var key))
                    return Array.Empty<string>();

                using (key)
                {
                    return key.GetValueNames();
                }
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        // -----------------------------------------------------------------
        // Public write operations
        // -----------------------------------------------------------------

        /// <summary>
        /// Attempts to set a registry value. Creates the key if it does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <param name="value">The value data.</param>
        /// <param name="kind">
        /// The registry value kind. If <see langword="null"/>, the kind is inferred
        /// from the type of <paramref name="value"/>.
        /// </param>
        /// <returns><see langword="true"/> if the value was set successfully; otherwise <see langword="false"/>.</returns>
        public static bool TrySetValue<T>(string keyPath, string valueName, T value, RegistryValueKind? kind = null)
        {
            if (string.IsNullOrEmpty(keyPath))
                return false;

            try
            {
                if (!TryParseHive(keyPath, out var hive, out var subKey))
                    return false;

                using var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return false;

                using var key = hiveKey.CreateSubKey(subKey);
                if (key is null)
                    return false;

                RegistryValueKind resolvedKind = kind ?? InferValueKind(value);
                key.SetValue(valueName, (object?)value ?? string.Empty, resolvedKind);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to delete a registry value.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="valueName">The name of the value to delete.</param>
        /// <returns><see langword="true"/> if the value was deleted (or did not exist); otherwise <see langword="false"/>.</returns>
        public static bool TryDeleteValue(string keyPath, string valueName)
        {
            if (string.IsNullOrEmpty(keyPath))
                return false;

            try
            {
                if (!TryOpenKey(keyPath, writable: true, out var hive, out var subKey, out var key))
                    return false;

                using (key)
                {
                    if (key.GetValue(valueName) is null)
                        return true; // Already absent.

                    key.DeleteValue(valueName);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to delete a registry key.
        /// </summary>
        /// <param name="keyPath">Full registry key path.</param>
        /// <param name="recursive">
        /// If <see langword="true"/> and the key has subkeys, deletes them recursively.
        /// </param>
        /// <returns><see langword="true"/> if the key was deleted (or did not exist); otherwise <see langword="false"/>.</returns>
        public static bool TryDeleteKey(string keyPath, bool recursive = false)
        {
            if (string.IsNullOrEmpty(keyPath))
                return false;

            try
            {
                if (!TryParseHive(keyPath, out var hive, out var subKey))
                    return false;

                if (string.IsNullOrEmpty(subKey))
                    return false; // Cannot delete a root hive key.

                using var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return false;

                // Check if the key exists first.
                using (var check = hiveKey.OpenSubKey(subKey))
                {
                    if (check is null)
                        return true; // Already absent.
                }

                if (recursive)
                {
                    hiveKey.DeleteSubKeyTree(subKey);
                }
                else
                {
                    hiveKey.DeleteSubKey(subKey);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // .reg export / import
        // -----------------------------------------------------------------

        /// <summary>
        /// Exports a registry key and all its subkeys to a .reg file (UTF-16 LE with BOM).
        /// </summary>
        /// <param name="keyPath">Full registry key path to export.</param>
        /// <param name="filePath">The destination .reg file path.</param>
        /// <returns><see langword="true"/> if the export succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryExportToReg(string keyPath, string filePath)
        {
            if (string.IsNullOrEmpty(keyPath) || string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                if (!TryParseHive(keyPath, out var hive, out var subKey))
                    return false;

                using var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return false;

                using var key = hiveKey.OpenSubKey(subKey);
                if (key is null)
                    return false;

                var lines = new List<string> { RegFileHeader, string.Empty };
                ExportKeyRecursive(key, keyPath, lines);

                // Write UTF-16 LE with BOM.
                byte[] bom = new byte[] { 0xFF, 0xFE };
                byte[] content = Encoding.Unicode.GetBytes(string.Join("\r\n", lines) + "\r\n");

                File.WriteAllBytes(filePath, bom);
                using (var fs = new FileStream(filePath, FileMode.Append))
                {
                    fs.Write(content, 0, content.Length);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Imports entries from a .reg file and applies them to the registry.
        /// </summary>
        /// <param name="filePath">The .reg file path to import.</param>
        /// <returns><see langword="true"/> if the import completed (partially or fully); otherwise <see langword="false"/>.</returns>
        public static bool TryImportFromReg(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                string text;
                using (var reader = new StreamReader(filePath, Encoding.Unicode))
                {
                    text = reader.ReadToEnd();
                }

                var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                return ParseRegLines(lines);
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Backup / Restore
        // -----------------------------------------------------------------

        /// <summary>
        /// Creates a snapshot backup of the specified registry key and all its subkeys.
        /// </summary>
        /// <param name="keyPath">Full registry key path to back up.</param>
        /// <returns>A <see cref="RegistryBackup"/> on success, or <see langword="null"/> on failure.</returns>
        public static RegistryBackup? TryBackupKey(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
                return null;

            try
            {
                if (!TryParseHive(keyPath, out var hive, out var subKey))
                    return null;

                using var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return null;

                using var key = hiveKey.OpenSubKey(subKey);
                if (key is null)
                    return null;

                var backup = new RegistryBackup
                {
                    KeyPath = keyPath,
                    BackupTime = DateTime.UtcNow,
                };

                BackupKeyRecursive(key, string.Empty, backup.Values);
                return backup;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Restores a registry key from a <see cref="RegistryBackup"/>.
        /// </summary>
        /// <param name="backup">The backup to restore from.</param>
        /// <returns><see langword="true"/> if the restore succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryRestoreKey(RegistryBackup backup)
        {
            if (backup is null || string.IsNullOrEmpty(backup.KeyPath))
                return false;

            try
            {
                if (!TryParseHive(backup.KeyPath, out var hive, out var subKey))
                    return false;

                using var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return false;

                // Group entries by their subkey path.
                var grouped = new Dictionary<string, Dictionary<string, RegistryValueEntry>>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in backup.Values)
                {
                    string entryKey = kvp.Key;
                    string effectiveSubKey;
                    string valueName;

                    int sepIndex = entryKey.LastIndexOf('\\');
                    if (sepIndex >= 0)
                    {
                        effectiveSubKey = entryKey.Substring(0, sepIndex);
                        valueName = entryKey.Substring(sepIndex + 1);
                    }
                    else
                    {
                        effectiveSubKey = string.Empty;
                        valueName = entryKey;
                    }

                    if (!grouped.TryGetValue(effectiveSubKey, out var valueMap))
                    {
                        valueMap = new Dictionary<string, RegistryValueEntry>(StringComparer.OrdinalIgnoreCase);
                        grouped[effectiveSubKey] = valueMap;
                    }

                    valueMap[valueName] = kvp.Value;
                }

                foreach (var group in grouped)
                {
                    string targetSubKey = string.IsNullOrEmpty(group.Key)
                        ? subKey
                        : subKey + "\\" + group.Key;

                    using var targetKey = hiveKey.CreateSubKey(targetSubKey);
                    if (targetKey is null)
                        continue;

                    foreach (var entry in group.Value)
                    {
                        targetKey.SetValue(
                            entry.Value.Name,
                            entry.Value.Data ?? string.Empty,
                            entry.Value.Kind);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Private helpers - hive parsing
        // -----------------------------------------------------------------

        private static bool TryParseHive(string keyPath, out RegistryHive hive, out string subKey)
        {
            hive = RegistryHive.LocalMachine;
            subKey = string.Empty;

            if (string.IsNullOrEmpty(keyPath))
                return false;

            // Find the first backslash.
            int sepIndex = keyPath.IndexOf('\\');
            string hivePart = sepIndex >= 0 ? keyPath.Substring(0, sepIndex) : keyPath;
            subKey = sepIndex >= 0 ? keyPath.Substring(sepIndex + 1) : string.Empty;

            switch (hivePart.ToUpperInvariant())
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    hive = RegistryHive.ClassesRoot;
                    break;
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    hive = RegistryHive.CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    hive = RegistryHive.LocalMachine;
                    break;
                case "HKEY_USERS":
                case "HKU":
                    hive = RegistryHive.Users;
                    break;
                case "HKEY_CURRENT_CONFIG":
                case "HKCC":
                    hive = RegistryHive.CurrentConfig;
                    break;
                case "HKEY_PERFORMANCE_DATA":
                    hive = RegistryHive.PerformanceData;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static RegistryKey? GetHiveKey(RegistryHive hive)
        {
            try
            {
                return hive switch
                {
                    RegistryHive.ClassesRoot => Microsoft.Win32.Registry.ClassesRoot,
                    RegistryHive.CurrentUser => Microsoft.Win32.Registry.CurrentUser,
                    RegistryHive.LocalMachine => Microsoft.Win32.Registry.LocalMachine,
                    RegistryHive.Users => Microsoft.Win32.Registry.Users,
                    RegistryHive.CurrentConfig => Microsoft.Win32.Registry.CurrentConfig,
                    RegistryHive.PerformanceData => Microsoft.Win32.Registry.PerformanceData,
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------------------
        // Private helpers - key opening
        // -----------------------------------------------------------------

        private static bool TryOpenKey(
            string keyPath,
            bool writable,
            out RegistryHive hive,
            out string subKey,
            [NotNullWhen(true)] out RegistryKey? key)
        {
            hive = RegistryHive.LocalMachine;
            subKey = string.Empty;
            key = null;

            try
            {
                if (!TryParseHive(keyPath, out hive, out subKey))
                    return false;

                var hiveKey = GetHiveKey(hive);
                if (hiveKey is null)
                    return false;

                key = hiveKey.OpenSubKey(subKey, writable);
                return key is not null;
            }
            catch
            {
                key = null;
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Private helpers - type conversion
        // -----------------------------------------------------------------

        private static object ConvertValue<T>(object data)
        {
            Type targetType = typeof(T);

            // If the data is already the correct type, return directly.
            if (data is T t)
                return t;

            // Handle numeric conversions: int <-> long.
            if (targetType == typeof(int) && data is long longVal)
                return (int)longVal;

            if (targetType == typeof(long) && data is int intVal)
                return (long)intVal;

            // Handle string[] from object[] if needed.
            if (targetType == typeof(string[]) && data is string[] sa)
                return sa;

            if (targetType == typeof(string[]) && data is object[] oa)
                return oa.Cast<string>().ToArray();

            // Fallback: direct cast (may throw, caught by caller).
            return data;
        }

        private static RegistryValueKind InferValueKind<T>(T value)
        {
            if (value is null)
                return RegistryValueKind.String;

            Type t = typeof(T);

            if (t == typeof(string))
                return RegistryValueKind.String;

            if (t == typeof(int) || t == typeof(uint))
                return RegistryValueKind.DWord;

            if (t == typeof(long) || t == typeof(ulong))
                return RegistryValueKind.QWord;

            if (t == typeof(byte[]))
                return RegistryValueKind.Binary;

            if (t == typeof(string[]))
                return RegistryValueKind.MultiString;

            // Fallback: try the runtime type.
            Type runtimeType = value.GetType();
            if (runtimeType == typeof(string))
                return RegistryValueKind.String;
            if (runtimeType == typeof(int) || runtimeType == typeof(uint))
                return RegistryValueKind.DWord;
            if (runtimeType == typeof(long) || runtimeType == typeof(ulong))
                return RegistryValueKind.QWord;
            if (runtimeType == typeof(byte[]))
                return RegistryValueKind.Binary;
            if (runtimeType == typeof(string[]))
                return RegistryValueKind.MultiString;

            return RegistryValueKind.String;
        }

        // -----------------------------------------------------------------
        // Private helpers - .reg export
        // -----------------------------------------------------------------

        private static void ExportKeyRecursive(RegistryKey key, string currentKeyPath, List<string> lines)
        {
            // Write the key header.
            lines.Add($"[{currentKeyPath}]");

            // Write all values for this key.
            string[] valueNames = key.GetValueNames();
            foreach (string valueName in valueNames)
            {
                var kind = key.GetValueKind(valueName);
                object? data = key.GetValue(valueName);

                string line = FormatRegValue(valueName, data, kind);
                lines.Add(line);
            }

            lines.Add(string.Empty);

            // Recurse into subkeys.
            string[] subKeyNames = key.GetSubKeyNames();
            foreach (string subKeyName in subKeyNames)
            {
                using var subKey = key.OpenSubKey(subKeyName);
                if (subKey is not null)
                {
                    string subKeyPath = currentKeyPath + "\\" + subKeyName;
                    ExportKeyRecursive(subKey, subKeyPath, lines);
                }
            }
        }

        private static string FormatRegValue(string valueName, object? data, RegistryValueKind kind)
        {
            string namePart = string.IsNullOrEmpty(valueName)
                ? "@"
                : $"\"{EscapeRegString(valueName)}\"";

            if (data is null)
                return $"{namePart}=-";

            switch (kind)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return $"{namePart}=\"{EscapeRegString(data.ToString() ?? string.Empty)}\"";

                case RegistryValueKind.DWord:
                {
                    uint dword = data is uint ud ? ud : Convert.ToUInt32(data, CultureInfo.InvariantCulture);
                    return $"{namePart}=dword:{dword:X8}";
                }

                case RegistryValueKind.QWord:
                {
                    ulong qword = data is ulong uq ? uq : Convert.ToUInt64(data, CultureInfo.InvariantCulture);
                    byte[] bytes = BitConverter.GetBytes(qword);
                    return $"{namePart}=hex(b):{FormatHexBytes(bytes)}";
                }

                case RegistryValueKind.Binary:
                {
                    byte[] bytes = data as byte[] ?? Array.Empty<byte>();
                    return $"{namePart}=hex:{FormatHexBytes(bytes)}";
                }

                case RegistryValueKind.MultiString:
                {
                    string[]? strings = data as string[];
                    if (strings is null && data is string s)
                        strings = new[] { s };

                    if (strings is null)
                        return $"{namePart}=hex(7):";

                    // MULTI_SZ: each string null-terminated, final double null.
                    byte[] raw = EncodeMultiStringBytes(strings);
                    return $"{namePart}=hex(7):{FormatHexBytes(raw)}";
                }

                default:
                    // Unknown / REG_NONE: write as hex.
                    if (data is byte[] fallbackBytes)
                        return $"{namePart}=hex:{FormatHexBytes(fallbackBytes)}";

                    return $"{namePart}=\"{EscapeRegString(data.ToString() ?? string.Empty)}\"";
            }
        }

        private static string EscapeRegString(string s)
        {
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        private static string FormatHexBytes(byte[] bytes)
        {
            return string.Join(",", bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        }

        private static byte[] EncodeMultiStringBytes(string[] strings)
        {
            using var ms = new MemoryStream();
            foreach (string s in strings)
            {
                byte[] strBytes = Encoding.Unicode.GetBytes(s + "\0");
                ms.Write(strBytes, 0, strBytes.Length);
            }

            // Final double null terminator.
            ms.WriteByte(0);
            ms.WriteByte(0);
            return ms.ToArray();
        }

        // -----------------------------------------------------------------
        // Private helpers - .reg import
        // -----------------------------------------------------------------

        private static bool ParseRegLines(string[] lines)
        {
            string? currentKeyPath = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                // Skip blank lines and comments.
                if (line.Length == 0 || line[0] == ';')
                    continue;

                // Skip the header line.
                if (line.StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Section header: [HKEY...]
                if (line.Length > 0 && line[0] == '[' && line[line.Length - 1] == ']')
                {
                    currentKeyPath = line.Substring(1, line.Length - 2);
                    continue;
                }

                if (currentKeyPath is null)
                    continue;

                // Parse value line.
                try
                {
                    ParseAndApplyRegValue(currentKeyPath, line);
                }
                catch
                {
                    // Continue parsing other lines even if one fails.
                }
            }

            return true;
        }

        private static void ParseAndApplyRegValue(string keyPath, string line)
        {
            // Determine if this is a default value (@) or a named value.
            string valueName;
            string remainder;

            if (line.Length > 0 && line[0] == '@')
            {
                valueName = string.Empty;
                remainder = line.Substring(1).TrimStart();
            }
            else if (line.Length > 0 && line[0] == '"')
            {
                // Parse quoted name.
                int closingQuote = FindClosingQuote(line, 0);
                if (closingQuote < 0)
                    return;

                valueName = UnescapeRegString(line.Substring(1, closingQuote - 1));
                remainder = line.Substring(closingQuote + 1).TrimStart();
            }
            else
            {
                return; // Not a valid value line.
            }

            // remainder should start with '='.
            if (remainder.Length == 0 || remainder[0] != '=')
                return;

            string valuePart = remainder.Substring(1).TrimStart();

            if (!TryParseHive(keyPath, out var hive, out var subKey))
                return;

            using var hiveKey = GetHiveKey(hive);
            if (hiveKey is null)
                return;

            using var key = hiveKey.CreateSubKey(subKey);
            if (key is null)
                return;

            // Determine the value type from the format.
            if (valuePart.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
            {
                string hex = valuePart.Substring(6).Trim();
                uint val = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                key.SetValue(valueName, (int)val, RegistryValueKind.DWord);
            }
            else if (valuePart.StartsWith("hex(", StringComparison.OrdinalIgnoreCase))
            {
                // hex(2): = REG_EXPAND_SZ, hex(7): = REG_MULTI_SZ, hex(b): = REG_QWORD
                int parenEnd = valuePart.IndexOf(')');
                if (parenEnd < 0)
                    return;

                string typeCode = valuePart.Substring(4, parenEnd - 4);
                string hexData = valuePart.Substring(parenEnd + 2); // skip ":"
                byte[] bytes = ParseHexBytes(hexData);

                switch (typeCode)
                {
                    case "2": // REG_EXPAND_SZ
                        string expandSz = Encoding.Unicode.GetString(bytes).TrimEnd('\0');
                        key.SetValue(valueName, expandSz, RegistryValueKind.ExpandString);
                        break;

                    case "7": // REG_MULTI_SZ
                        string multiRaw = Encoding.Unicode.GetString(bytes);
                        string[] multiParts = multiRaw.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                        key.SetValue(valueName, multiParts, RegistryValueKind.MultiString);
                        break;

                    case "b": // REG_QWORD
                    case "B":
                        if (bytes.Length >= 8)
                        {
                            long qword = BitConverter.ToInt64(bytes, 0);
                            key.SetValue(valueName, qword, RegistryValueKind.QWord);
                        }

                        break;

                    default:
                        // Unknown type code - store as binary.
                        key.SetValue(valueName, bytes, RegistryValueKind.Binary);
                        break;
                }
            }
            else if (valuePart.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
            {
                // REG_BINARY
                string hexData = valuePart.Substring(4);
                byte[] bytes = ParseHexBytes(hexData);
                key.SetValue(valueName, bytes, RegistryValueKind.Binary);
            }
            else if (valuePart.StartsWith("hex", StringComparison.OrdinalIgnoreCase) &&
                     valuePart.Length > 3 &&
                     !char.IsLetterOrDigit(valuePart[3]))
            {
                // "hex" followed by non-alphanumeric (e.g. hex: without parentheses)
                string hexData = valuePart.Substring(3).TrimStart(':');
                byte[] bytes = ParseHexBytes(hexData);
                key.SetValue(valueName, bytes, RegistryValueKind.Binary);
            }
            else if (valuePart.Length > 0 && valuePart[0] == '-')
            {
                // Value deletion marker.
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
            else if (valuePart.Length > 0 && valuePart[0] == '"')
            {
                // String value.
                int closingQuote = FindClosingQuote(valuePart, 0);
                if (closingQuote < 0)
                    return;

                string strValue = UnescapeRegString(valuePart.Substring(1, closingQuote - 1));
                key.SetValue(valueName, strValue, RegistryValueKind.String);
            }
        }

        private static int FindClosingQuote(string s, int startIndex)
        {
            for (int i = startIndex + 1; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    i++; // Skip escaped character.
                    continue;
                }

                if (s[i] == '"')
                    return i;
            }

            return -1;
        }

        private static string UnescapeRegString(string s)
        {
            return s
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r");
        }

        private static byte[] ParseHexBytes(string hexData)
        {
            // Hex data format: 01,02,03,04,... or 01 02 03 ...
            using var ms = new MemoryStream();
            var parts = hexData.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length == 0)
                    continue;

                // Handle line continuation backslash.
                if (trimmed == "\\")
                    continue;

                if (trimmed.Length >= 2 && byte.TryParse(
                        trimmed.Length > 2 ? trimmed.Substring(0, 2) : trimmed,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out byte b))
                {
                    ms.WriteByte(b);
                }
            }

            return ms.ToArray();
        }

        // -----------------------------------------------------------------
        // Private helpers - backup internals
        // -----------------------------------------------------------------

        private static void BackupKeyRecursive(
            RegistryKey key,
            string relativePath,
            Dictionary<string, RegistryValueEntry> values)
        {
            string[] valueNames = key.GetValueNames();

            foreach (string valueName in valueNames)
            {
                try
                {
                    var kind = key.GetValueKind(valueName);
                    object? data = key.GetValue(valueName);

                    string entryKey = string.IsNullOrEmpty(relativePath)
                        ? valueName
                        : relativePath + "\\" + valueName;

                    values[entryKey] = new RegistryValueEntry
                    {
                        Name = valueName,
                        Data = data,
                        Kind = kind,
                    };
                }
                catch
                {
                    // Skip values that cannot be read.
                }
            }

            // Recurse into subkeys.
            string[] subKeyNames = key.GetSubKeyNames();
            foreach (string subKeyName in subKeyNames)
            {
                try
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey is not null)
                    {
                        string childPath = string.IsNullOrEmpty(relativePath)
                            ? subKeyName
                            : relativePath + "\\" + subKeyName;

                        BackupKeyRecursive(subKey, childPath, values);
                    }
                }
                catch
                {
                    // Skip subkeys that cannot be read.
                }
            }
        }
    }
}
