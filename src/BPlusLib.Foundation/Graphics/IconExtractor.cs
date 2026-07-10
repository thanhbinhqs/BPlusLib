// <copyright file="IconExtractor.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Graphics
{
    /// <summary>
    /// Provides methods for extracting icons from executable files, DLLs, and ICO files
    /// via Win32 P/Invoke (Shell32, User32). Also resolves associated icon paths for
    /// file extensions via the Windows registry.
    /// All methods are thread-safe and gracefully return null/empty on non-Windows.
    /// </summary>
    public static class IconExtractor
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary>Maximum icon count we'll attempt to extract.</summary>
        private const int MaxIcons = 256;

        // SHGFI flags
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_SHELLICONSIZE = 0x000000004;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        // File attribute constants
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        // =====================================================================
        // P/Invoke — Shell32
        // =====================================================================

        [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconExW(
            string lpszFile,
            int nIconIndex,
            IntPtr[]? phiconLarge,
            IntPtr[]? phiconSmall,
            uint nIcons);

        [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfoW(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFOW psfi,
            uint cbFileInfo,
            uint uFlags);

        // =====================================================================
        // P/Invoke — User32
        // =====================================================================

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        // =====================================================================
        // P/Invoke — GDI32 (for GetDIBits on extracted icons)
        // =====================================================================

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern int GetDIBits(
            IntPtr hdc,
            IntPtr hbm,
            uint start,
            uint cLines,
            IntPtr lpvBits,
            ref BITMAPINFOHEADER lpbmi,
            uint usage);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr ho);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDCW(string? pwszDriver, string? pwszDevice, string? pszPort, IntPtr pdm);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern int GetObjectW(IntPtr hgdiobj, int cbBuffer, IntPtr lpvObject);

        // =====================================================================
        // Win32 structs
        // =====================================================================

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFOW
        {
            internal IntPtr hIcon;
            internal int iIcon;
            internal uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            internal string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            [MarshalAs(UnmanagedType.Bool)]
            internal bool fIcon;
            internal int xHotspot;
            internal int yHotspot;
            internal IntPtr hbmMask;
            internal IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            internal int bmType;
            internal int bmWidth;
            internal int bmHeight;
            internal int bmWidthBytes;
            internal ushort bmPlanes;
            internal ushort bmBitsPixel;
            internal IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            internal uint biSize;
            internal int biWidth;
            internal int biHeight;
            internal ushort biPlanes;
            internal ushort biBitCount;
            internal uint biCompression;
            internal uint biSizeImage;
            internal int biXPelsPerMeter;
            internal int biYPelsPerMeter;
            internal uint biClrUsed;
            internal uint biImportant;
        }

        // =====================================================================
        // Icon extraction
        // =====================================================================

        /// <summary>
        /// Extracts an icon from the specified executable, DLL, or ICO file
        /// and returns its raw BGRA pixel data with dimensions.
        /// </summary>
        /// <param name="filePath">Path to the file from which to extract the icon.</param>
        /// <param name="size">Desired icon size in pixels (32 is default).</param>
        /// <returns>A tuple of (pixelData, width, height), or null on failure or non-Windows.</returns>
        public static (byte[] PixelData, int Width, int Height)? ExtractIconRaw(
            string filePath,
            int size = 32)
        {
            if (string.IsNullOrEmpty(filePath) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            if (size <= 0)
            {
                return null;
            }

            // Extract the icon via ExtractIconExW
            // We request the large icon array only
            var largeIcons = new IntPtr[1];

            uint extracted = ExtractIconExW(
                filePath,
                0, // first icon index
                largeIcons,
                null,
                1);

            if (extracted == 0 || largeIcons[0] == IntPtr.Zero)
            {
                return null;
            }

            IntPtr hIcon = largeIcons[0];

            try
            {
                return IconHandleToRawPixels(hIcon, size);
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        /// <summary>
        /// Extracts an icon from the specified file and returns it as a PNG byte array.
        /// Uses System.Drawing on .NET Framework for PNG encoding.
        /// On .NET Core+, returns raw BGRA data with a simple PNG header (fallback).
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="size">Desired icon size (default: 32).</param>
        /// <returns>PNG bytes, or null on failure or non-Windows.</returns>
        public static byte[]? ExtractIconAsPng(string filePath, int size = 32)
        {
            var raw = ExtractIconRaw(filePath, size);
            if (raw == null)
            {
                return null;
            }

            var (pixelData, width, height) = raw.Value;

#if NETFRAMEWORK
            try
            {
                using var bitmap = new System.Drawing.Bitmap(
                    width,
                    height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                var bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                try
                {
                    int srcStride = width * 4;
                    int dstStride = bitmapData.Stride;
                    if (srcStride == dstStride)
                    {
                        Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Marshal.Copy(
                                pixelData,
                                y * srcStride,
                                bitmapData.Scan0 + (y * dstStride),
                                srcStride);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                using var ms = new System.IO.MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
#else
            // On .NET Core without System.Drawing, return raw BGRA pixel data
            // with a minimal wrapper type. The caller can interpret it.
            // For convenience, we encode a minimal PNG manually.
            // If that fails, return the raw data as-is (caller can check dimensions).
            try
            {
                return EncodeRawToPng(pixelData, width, height);
            }
            catch
            {
                return null;
            }
#endif
        }

        /// <summary>
        /// Tries to extract an icon as PNG bytes. Returns true on success.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="pngData">When this method returns, contains the PNG bytes or null.</param>
        /// <param name="size">Desired icon size (default: 32).</param>
        /// <returns>true if the icon was successfully extracted; otherwise, false.</returns>
        public static bool TryExtractIcon(string filePath, out byte[]? pngData, int size = 32)
        {
            try
            {
                pngData = ExtractIconAsPng(filePath, size);
                return pngData != null;
            }
            catch
            {
                pngData = null;
                return false;
            }
        }

        // =====================================================================
        // Associated icon paths
        // =====================================================================

        /// <summary>
        /// Gets the associated icon paths for a given file extension (e.g., ".txt", ".pdf").
        /// Uses the Windows registry to look up the ProgID and default icon.
        /// </summary>
        /// <param name="extension">The file extension including the dot (e.g., ".txt").</param>
        /// <returns>A list of icon file paths, or an empty list on non-Windows or if not found.</returns>
        public static IReadOnlyList<string> GetAssociatedIcons(string extension)
        {
            if (string.IsNullOrEmpty(extension) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Array.Empty<string>();
            }

            // Normalize extension: ensure leading dot
            string ext = extension.StartsWith(".", StringComparison.Ordinal)
                ? extension
                : "." + extension;

            var results = new List<string>();

            try
            {
                // Look up via registry
                // HKEY_CLASSES_ROOT\.ext -> (Default) = ProgID
                string? progId = GetRegistryValue(
                    @"HKEY_CLASSES_ROOT\" + ext,
                    string.Empty);

                if (string.IsNullOrEmpty(progId))
                {
                    // Try HKEY_CLASSES_ROOT\.ext\ProgID
                    progId = GetRegistryValue(
                        @"HKEY_CLASSES_ROOT\" + ext,
                        "ProgID");
                }

                if (!string.IsNullOrEmpty(progId))
                {
                    // HKEY_CLASSES_ROOT\ProgID\DefaultIcon -> (Default) = path
                    string? iconPath = GetRegistryValue(
                        @"HKEY_CLASSES_ROOT\" + progId + @"\DefaultIcon",
                        string.Empty);

                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        // Parse icon path (format: "path,index")
                        results.Add(iconPath);
                    }

                    // Also look for shell\open\command if needed
                }

                // If registry lookup failed, try SHGetFileInfo
                if (results.Count == 0)
                {
                    var shfi = default(SHFILEINFOW);
                    IntPtr h = SHGetFileInfoW(
                        "dummy" + ext,
                        FILE_ATTRIBUTE_NORMAL,
                        ref shfi,
                        (uint)Marshal.SizeOf<SHFILEINFOW>(),
                        SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_SHELLICONSIZE);

                    if (h != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                    {
                        results.Add($"ShellIcon:{ext}");
                        DestroyIcon(shfi.hIcon);
                    }
                }
            }
            catch
            {
                // Registry or API failure — return whatever we have
            }

            return results.AsReadOnly();
        }

        // =====================================================================
        // Internal helpers
        // =====================================================================

        /// <summary>
        /// Converts an HICON handle to raw BGRA pixel data of the specified size.
        /// Uses GetIconInfo to retrieve the color bitmap, then GetDIBits to extract pixels.
        /// </summary>
        private static (byte[] PixelData, int Width, int Height)? IconHandleToRawPixels(
            IntPtr hIcon,
            int targetSize)
        {
            if (!GetIconInfo(hIcon, out ICONINFO iconInfo))
            {
                return null;
            }

            IntPtr hbmColor = iconInfo.hbmColor;
            IntPtr hbmMask = iconInfo.hbmMask;

            try
            {
                int width;
                int height;

                // Use GetObject to determine bitmap dimensions
                var bm = default(BITMAP);
                IntPtr bmPtr = Marshal.AllocHGlobal(Marshal.SizeOf<BITMAP>());
                try
                {
                    if (GetObjectW(hbmColor, Marshal.SizeOf<BITMAP>(), bmPtr) > 0)
                    {
                        bm = Marshal.PtrToStructure<BITMAP>(bmPtr);
                        width = bm.bmWidth;
                        height = Math.Abs(bm.bmHeight);
                    }
                    else if (hbmMask != IntPtr.Zero &&
                             GetObjectW(hbmMask, Marshal.SizeOf<BITMAP>(), bmPtr) > 0)
                    {
                        // Some icons only have a mask bitmap; get size from that
                        bm = Marshal.PtrToStructure<BITMAP>(bmPtr);
                        width = bm.bmWidth;
                        height = Math.Abs(bm.bmHeight) / 2; // mask is double-height
                    }
                    else
                    {
                        // Fall back to target size
                        width = targetSize;
                        height = targetSize;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(bmPtr);
                }

                // Use GetDIBits to extract pixel data from the color bitmap
                if (hbmColor != IntPtr.Zero)
                {
                    IntPtr hdc = CreateDCW("DISPLAY", null, null, IntPtr.Zero);
                    if (hdc != IntPtr.Zero)
                    {
                        try
                        {
                            var bmi = new BITMAPINFOHEADER
                            {
                                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                                biWidth = width,
                                biHeight = -height, // top-down
                                biPlanes = 1,
                                biBitCount = 32,
                                biCompression = 0, // BI_RGB
                            };

                            int stride = width * 4;
                            byte[] pixelData = new byte[stride * height];

                            GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                            try
                            {
                                int ret = GetDIBits(
                                    hdc,
                                    hbmColor,
                                    0,
                                    (uint)height,
                                    handle.AddrOfPinnedObject(),
                                    ref bmi,
                                    0); // DIB_RGB_COLORS

                                if (ret != 0)
                                {
                                    return (pixelData, width, height);
                                }
                            }
                            finally
                            {
                                handle.Free();
                            }
                        }
                        finally
                        {
                            DeleteDC(hdc);
                        }
                    }
                }

                return null;
            }
            finally
            {
                if (iconInfo.hbmColor != IntPtr.Zero)
                {
                    DeleteObject(iconInfo.hbmColor);
                }

                if (iconInfo.hbmMask != IntPtr.Zero)
                {
                    DeleteObject(iconInfo.hbmMask);
                }
            }
        }

        /// <summary>
        /// Reads a string value from the Windows registry.
        /// </summary>
        private static string? GetRegistryValue(string keyPath, string valueName)
        {
            try
            {
                using var key = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.ClassesRoot,
                    Microsoft.Win32.RegistryView.Default);

                // Parse the key path
                // Format: HKEY_CLASSES_ROOT\... (we already know it's ClassesRoot)
                string subKey = keyPath;
                const string prefix = @"HKEY_CLASSES_ROOT\";
                if (subKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    subKey = subKey.Substring(prefix.Length);
                }

                using var sub = key.OpenSubKey(subKey);
                if (sub != null)
                {
                    return sub.GetValue(valueName) as string;
                }
            }
            catch
            {
                // Ignore registry errors
            }

            return null;
        }

#if !NETFRAMEWORK
        /// <summary>
        /// Encodes raw BGRA pixel data to a PNG byte array.
        /// This is a minimal PNG encoder for 32-bit RGBA images.
        /// </summary>
        private static byte[]? EncodeRawToPng(byte[] pixelData, int width, int height)
        {
            if (pixelData == null || width <= 0 || height <= 0)
            {
                return null;
            }

            try
            {
                int stride = width * 4;
                if (pixelData.Length < stride * height)
                {
                    return null;
                }

                using var ms = new System.IO.MemoryStream();
                var writer = new System.IO.BinaryWriter(ms);

                // PNG signature
                writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

                // IHDR chunk
                byte[] ihdrData = new byte[13];
                WriteBigEndianInt32(ihdrData, 0, width);
                WriteBigEndianInt32(ihdrData, 4, height);
                ihdrData[8] = 8; // bit depth
                ihdrData[9] = 6; // color type RGBA
                ihdrData[10] = 0; // compression
                ihdrData[11] = 0; // filter
                ihdrData[12] = 0; // interlace

                WritePngChunk(writer, "IHDR", ihdrData);

                // IDAT chunk — pixel data with filter bytes
                int rawRowSize = 1 + stride; // filter byte + pixel data
                int rawDataSize = rawRowSize * height;
                byte[] rawData = new byte[rawDataSize];

                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * rawRowSize;
                    rawData[rowOffset] = 0; // filter: None
                    Buffer.BlockCopy(pixelData, y * stride, rawData, rowOffset + 1, stride);
                }

                // Compress with zlib
                byte[] compressedData = CompressZLib(rawData);

                WritePngChunk(writer, "IDAT", compressedData);

                // IEND chunk
                WritePngChunk(writer, "IEND", Array.Empty<byte>());

                writer.Flush();
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private static void WriteBigEndianInt32(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private static void WritePngChunk(System.IO.BinaryWriter writer, string type, byte[] data)
        {
            // Length
            WriteBigEndianInt32(writer, data.Length);

            // Type
            byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            writer.Write(typeBytes);

            // Data
            if (data.Length > 0)
            {
                writer.Write(data);
            }

            // CRC
            byte[] crcInput = new byte[4 + data.Length];
            Buffer.BlockCopy(typeBytes, 0, crcInput, 0, 4);
            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, crcInput, 4, data.Length);
            }

            uint crcValue = Crc32(crcInput);
            writer.Write(crcValue);
        }

        private static void WriteBigEndianInt32(System.IO.BinaryWriter writer, int value)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)((value >> 24) & 0xFF);
            bytes[1] = (byte)((value >> 16) & 0xFF);
            bytes[2] = (byte)((value >> 8) & 0xFF);
            bytes[3] = (byte)(value & 0xFF);
            writer.Write(bytes);
        }

        private static byte[] CompressZLib(byte[] data)
        {
            // Use DeflateStream with Zlib header
            using var compressedMs = new System.IO.MemoryStream();

            // Write Zlib header (RFC 1950)
            // CMF = 0x78 (deflate, window size 32K)
            // FLG = 0x01 (check bits: (CMF*256 + FLG) % 31 == 0)
            compressedMs.WriteByte(0x78);
            compressedMs.WriteByte(0x01);

            using (var deflateStream = new System.IO.Compression.DeflateStream(
                compressedMs,
                System.IO.Compression.CompressionLevel.Optimal,
                leaveOpen: true))
            {
                deflateStream.Write(data, 0, data.Length);
            }

            // Adler32 checksum
            uint adler = Adler32(data);
            byte[] adlerBytes = new byte[4];
            WriteBigEndianInt32(adlerBytes, 0, (int)adler);
            compressedMs.Write(adlerBytes, 0, 4);

            return compressedMs.ToArray();
        }

        private static uint Adler32(byte[] data)
        {
            const uint MOD = 65521;
            uint a = 1, b = 0;
            foreach (byte t in data)
            {
                a = (a + t) % MOD;
                b = (b + a) % MOD;
            }

            return (b << 16) | a;
        }

        /// <summary>
        /// Computes the CRC-32 checksum for PNG chunk validation.
        /// </summary>
        private static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc = (crc >> 1) ^ 0xEDB88320;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc ^ 0xFFFFFFFF;
        }
#endif
    }
}
