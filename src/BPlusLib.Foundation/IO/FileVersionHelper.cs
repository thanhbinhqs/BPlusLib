// <copyright file="FileVersionHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.IO
{
    /// <summary>
    /// Represents version information read from a PE file's version resource.
    /// </summary>
    public sealed record FileVersionInfo
    {
        /// <summary>Gets the file version string.</summary>
        public string? FileVersion { get; init; }

        /// <summary>Gets the product version string.</summary>
        public string? ProductVersion { get; init; }

        /// <summary>Gets the company name.</summary>
        public string? CompanyName { get; init; }

        /// <summary>Gets the product name.</summary>
        public string? ProductName { get; init; }

        /// <summary>Gets the file description.</summary>
        public string? FileDescription { get; init; }

        /// <summary>Gets the legal copyright.</summary>
        public string? LegalCopyright { get; init; }

        /// <summary>Gets the legal trademarks.</summary>
        public string? LegalTrademarks { get; init; }

        /// <summary>Gets the internal name.</summary>
        public string? InternalName { get; init; }

        /// <summary>Gets the original filename.</summary>
        public string? OriginalFilename { get; init; }

        /// <summary>Gets the comments.</summary>
        public string? Comments { get; init; }

        /// <summary>Gets the private build string.</summary>
        public string? PrivateBuild { get; init; }

        /// <summary>Gets the special build string.</summary>
        public string? SpecialBuild { get; init; }

        /// <summary>Gets the language identifier string.</summary>
        public string? Language { get; init; }
    }

    /// <summary>
    /// Provides methods to read version information from PE files using the version.dll API.
    /// All methods are thread-safe and gracefully return null on non-Windows platforms.
    /// </summary>
    public static class FileVersionHelper
    {
        /// <summary>
        /// Reads all available version info from a PE file using VerQueryValue.
        /// </summary>
        /// <param name="filePath">The path to the PE file to query.</param>
        /// <returns>A <see cref="FileVersionInfo"/> with populated values, or <c>null</c> if the file has no version resource.</returns>
        public static FileVersionInfo? GetVersionInfo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            try
            {
                // Step 1: Get the size of the version info
                uint handle = 0;
                uint size = VersionApi.GetFileVersionInfoSizeExW(
                    VersionApi.FILE_VER_GET_NEUTRAL,
                    filePath,
                    out handle);

                if (size == 0)
                {
                    return null;
                }

                // Step 2: Allocate buffer and get the version info
                IntPtr buffer = Marshal.AllocHGlobal((int)size);
                try
                {
                    if (!VersionApi.GetFileVersionInfoExW(
                            VersionApi.FILE_VER_GET_NEUTRAL,
                            filePath,
                            handle,
                            size,
                            buffer))
                    {
                        return null;
                    }

                    // Step 3: Query the root block for VS_FIXEDFILEINFO (not used directly,
                    // but validates the version resource exists)
                    IntPtr rootPtr = IntPtr.Zero;
                    uint rootLen = 0;
                    if (!VersionApi.VerQueryValueW(buffer, "\\", out rootPtr, out rootLen))
                    {
                        return null;
                    }

                    // Step 4: Get the translation array
                    IntPtr transPtr = IntPtr.Zero;
                    uint transLen = 0;
                    if (!VersionApi.VerQueryValueW(buffer, "\\VarFileInfo\\Translation", out transPtr, out transLen))
                    {
                        // Some files have version info but no translation table;
                        // try with no language
                        return QueryWithNoLanguage(buffer);
                    }

                    if (transPtr == IntPtr.Zero || transLen < 4)
                    {
                        return QueryWithNoLanguage(buffer);
                    }

                    // Parse the translation array (each entry is 2 x uint16 = 4 bytes)
                    int entryCount = (int)transLen / 4;
                    var result = new FileVersionInfo();

                    // Use the first translation
                    ushort lang = (ushort)Marshal.ReadInt16(transPtr);
                    ushort charset = (ushort)Marshal.ReadInt16(transPtr + 2);

                    result = QueryStringFileInfo(buffer, lang, charset);

                    // Try to read the language description
                    result = result with { Language = GetLanguageName(lang) };

                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Quick read: FileVersion string.</summary>
        public static string? GetFileVersion(string filePath)
            => GetVersionInfo(filePath)?.FileVersion;

        /// <summary>Quick read: ProductVersion string.</summary>
        public static string? GetProductVersion(string filePath)
            => GetVersionInfo(filePath)?.ProductVersion;

        /// <summary>Quick read: CompanyName.</summary>
        public static string? GetCompanyName(string filePath)
            => GetVersionInfo(filePath)?.CompanyName;

        /// <summary>
        /// Queries version string info using the given language and charset identifiers.
        /// </summary>
        private static FileVersionInfo QueryStringFileInfo(IntPtr buffer, ushort lang, ushort charset)
        {
            return new FileVersionInfo
            {
                FileVersion = QueryString(buffer, lang, charset, "FileVersion"),
                ProductVersion = QueryString(buffer, lang, charset, "ProductVersion"),
                CompanyName = QueryString(buffer, lang, charset, "CompanyName"),
                ProductName = QueryString(buffer, lang, charset, "ProductName"),
                FileDescription = QueryString(buffer, lang, charset, "FileDescription"),
                LegalCopyright = QueryString(buffer, lang, charset, "LegalCopyright"),
                LegalTrademarks = QueryString(buffer, lang, charset, "LegalTrademarks"),
                InternalName = QueryString(buffer, lang, charset, "InternalName"),
                OriginalFilename = QueryString(buffer, lang, charset, "OriginalFilename"),
                Comments = QueryString(buffer, lang, charset, "Comments"),
                PrivateBuild = QueryString(buffer, lang, charset, "PrivateBuild"),
                SpecialBuild = QueryString(buffer, lang, charset, "SpecialBuild"),
            };
        }

        /// <summary>
        /// Queries a single string from the version resource using VerQueryValueW.
        /// </summary>
        private static string? QueryString(IntPtr buffer, ushort lang, ushort charset, string key)
        {
            string subBlock = $"\\StringFileInfo\\{lang:X4}{charset:X4}\\{key}";
            IntPtr valuePtr = IntPtr.Zero;
            uint valueLen = 0;

            if (VersionApi.VerQueryValueW(buffer, subBlock, out valuePtr, out valueLen))
            {
                if (valuePtr != IntPtr.Zero && valueLen > 0)
                {
                    return Marshal.PtrToStringUni(valuePtr, (int)valueLen - 1);
                }
            }

            return null;
        }

        /// <summary>
        /// Fallback: query version strings without a specific translation block.
        /// </summary>
        private static FileVersionInfo? QueryWithNoLanguage(IntPtr buffer)
        {
            // Try some common languages
            ushort[] commonLangs = { 0x0409, 0x0407, 0x040C, 0x0411, 0x0809, 0x0413 };

            foreach (ushort lang in commonLangs)
            {
                var result = QueryStringFileInfo(buffer, lang, 0x04E4); // 0x04E4 = Unicode
                if (!string.IsNullOrEmpty(result.FileVersion) || !string.IsNullOrEmpty(result.CompanyName))
                {
                    return result with { Language = GetLanguageName(lang) };
                }
            }

            // Try with no language code (empty sub-block)
            var emptyResult = new FileVersionInfo();
            return emptyResult;
        }

        /// <summary>
        /// Converts a language ID to a readable name.
        /// </summary>
        private static string GetLanguageName(ushort lang)
        {
            return lang switch
            {
                0x0409 => "en-US",
                0x0407 => "de-DE",
                0x040C => "fr-FR",
                0x0410 => "it-IT",
                0x0411 => "ja-JP",
                0x0809 => "en-GB",
                0x0413 => "nl-NL",
                0x0419 => "ru-RU",
                0x0804 => "zh-CN",
                0x0404 => "zh-TW",
                0x0412 => "ko-KR",
                0x0416 => "pt-BR",
                0x040A => "es-ES",
                _ => $"0x{lang:X4}",
            };
        }
    }
}
