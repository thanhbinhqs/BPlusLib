// <copyright file="ChecksumHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BPlusLib.Foundation.IO
{
    /// <summary>
    /// Provides file checksum computation using standard cryptographic
    /// hash algorithms (MD5, SHA-1, SHA-256, SHA-512) and a pure-managed
    /// CRC-32 implementation. All methods use buffered I/O (8 KB buffer),
    /// are thread-safe, and never throw — returning <c>"ERROR"</c> on failure.
    /// </summary>
    /// <remarks>
    /// CRC-32 is implemented manually using the standard IEEE polynomial
    /// (0xEDB88320) so that it works on all target frameworks including
    /// .NET Framework 4.7.2 without any external packages.
    /// </remarks>
    public static class ChecksumHelper
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const int BufferSize = 8192;
        private const string ErrorResult = "ERROR";

        // -----------------------------------------------------------------
        // CRC-32 lookup table
        // -----------------------------------------------------------------

        private static readonly uint[] Crc32Table = new uint[256];

        /// <summary>
        /// Initialises the static CRC-32 lookup table using polynomial 0xEDB88320.
        /// </summary>
        static ChecksumHelper()
        {
            const uint poly = 0xEDB88320;

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (poly ^ (crc >> 1)) : (crc >> 1);
                }

                Crc32Table[i] = crc;
            }
        }

        // -----------------------------------------------------------------
        // Specific hash methods
        // -----------------------------------------------------------------

        /// <summary>
        /// Computes the MD5 hash of the file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// A lowercase hex-encoded MD5 hash string, or <c>"ERROR"</c> on failure.
        /// </returns>
        public static string ComputeMD5(string path)
        {
            return ComputeHashCore(path, HashAlgorithmName.MD5);
        }

        /// <summary>
        /// Computes the SHA-1 hash of the file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// A lowercase hex-encoded SHA-1 hash string, or <c>"ERROR"</c> on failure.
        /// </returns>
        public static string ComputeSHA1(string path)
        {
            return ComputeHashCore(path, HashAlgorithmName.SHA1);
        }

        /// <summary>
        /// Computes the SHA-256 hash of the file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// A lowercase hex-encoded SHA-256 hash string, or <c>"ERROR"</c> on failure.
        /// </returns>
        public static string ComputeSHA256(string path)
        {
            return ComputeHashCore(path, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// Computes the SHA-512 hash of the file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// A lowercase hex-encoded SHA-512 hash string, or <c>"ERROR"</c> on failure.
        /// </returns>
        public static string ComputeSHA512(string path)
        {
            return ComputeHashCore(path, HashAlgorithmName.SHA512);
        }

        /// <summary>
        /// Computes a CRC-32 checksum of the file at <paramref name="path"/>
        /// using the standard IEEE polynomial.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// A lowercase 8-character hex CRC-32 value, or <c>"ERROR"</c> on failure.
        /// </returns>
        public static string ComputeCRC32(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
                uint crc = 0xFFFFFFFF;
                int read;
                byte[] buffer = new byte[BufferSize];

                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        crc = Crc32Table[(crc ^ buffer[i]) & 0xFF] ^ (crc >> 8);
                    }
                }

                return (~crc).ToString("x8");
            }
            catch
            {
                return ErrorResult;
            }
        }

        // -----------------------------------------------------------------
        // Generic hash (cryptographic)
        // -----------------------------------------------------------------

        /// <summary>
        /// Computes the cryptographic hash of the file at <paramref name="path"/>
        /// using the specified <paramref name="algorithm"/>.
        /// Supports MD5, SHA1, SHA256, SHA384, and SHA512.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="algorithm">The hash algorithm name.</param>
        /// <returns>
        /// A lowercase hex-encoded hash string, or <c>"ERROR"</c> on failure.
        /// </returns>
        public static string ComputeHash(string path, HashAlgorithmName algorithm)
        {
            // Route CRC-32 to the dedicated implementation.
            if (algorithm.Name is not null &&
                algorithm.Name.Equals("CRC32", StringComparison.OrdinalIgnoreCase))
            {
                return ComputeCRC32(path);
            }

            return ComputeHashCore(path, algorithm);
        }

        /// <summary>
        /// Verifies that the file at <paramref name="path"/> produces the
        /// expected <paramref name="expectedHash"/> string for the given
        /// <paramref name="algorithm"/>. Comparison is case-insensitive.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="expectedHash">The expected hash hex string.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>
        /// <see langword="true"/> if the computed hash matches <paramref name="expectedHash"/>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool VerifyHash(string path, string expectedHash, HashAlgorithmName algorithm)
        {
            if (string.IsNullOrEmpty(expectedHash))
                return false;

            string computed = ComputeHash(path, algorithm);

            if (string.Equals(computed, ErrorResult, StringComparison.Ordinal))
                return false;

            return string.Equals(computed, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        // -----------------------------------------------------------------
        // Private implementation
        // -----------------------------------------------------------------

        /// <summary>
        /// Core hash computation using <see cref="IncrementalHash"/> for all
        /// supported cryptographic algorithms. Falls back to the classic
        /// <see cref="HashAlgorithm.Create(string)"/> pattern on net472 if
        /// <see cref="IncrementalHash"/> is not available for a given algo.
        /// </summary>
        private static string ComputeHashCore(string path, HashAlgorithmName algorithm)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return ErrorResult;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

                // Prefer IncrementalHash (available on net6.0+, net472 with
                // System.Security.Cryptography.xxx package).
                return ComputeWithIncrementalHash(fs, algorithm);
            }
            catch
            {
                return ErrorResult;
            }
        }

        /// <summary>
        /// Computes a hash using <see cref="IncrementalHash"/>.
        /// Falls back to the classic crypto-stream approach on frameworks
        /// where IncrementalHash does not support the requested algorithm.
        /// </summary>
        private static string ComputeWithIncrementalHash(Stream stream, HashAlgorithmName algorithm)
        {
            try
            {
                using var hasher = IncrementalHash.CreateHash(algorithm);
                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
#if NETFRAMEWORK || NET6_0
                    hasher.AppendData(buffer, 0, bytesRead);
#else
                    hasher.AppendData(buffer.AsSpan(0, bytesRead));
#endif
                }

                byte[] hashBytes = hasher.GetHashAndReset();
                return BytesToHexLower(hashBytes);
            }
            catch
            {
                // Fallback: use classic HashAlgorithm.
                return ComputeWithClassicHash(stream, algorithm);
            }
        }

        /// <summary>
        /// Fallback hash computation using the classic
        /// <see cref="HashAlgorithm.Create(string)"/> and
        /// <see cref="CryptoStream"/> approach, compatible with net472.
        /// </summary>
        private static string ComputeWithClassicHash(Stream stream, HashAlgorithmName algorithm)
        {
            try
            {
                string? algoName = algorithm.Name;

                if (string.IsNullOrEmpty(algoName))
                    return ErrorResult;

#if NET8_0_OR_GREATER
                // Use IncrementalHash on modern .NET (not obsolete).
                var incrementalHash = IncrementalHash.CreateHash(
                    algorithm.Name switch
                    {
                        "MD5" => HashAlgorithmName.MD5,
                        "SHA1" => HashAlgorithmName.SHA1,
                        "SHA256" => HashAlgorithmName.SHA256,
                        "SHA512" => HashAlgorithmName.SHA512,
                        _ => HashAlgorithmName.SHA256,
                    });
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    incrementalHash.AppendData(buffer.AsSpan(0, bytesRead));
                byte[] hashBytes = incrementalHash.GetHashAndReset();
                return BytesToHexLower(hashBytes);
#else
                using var hashAlgo = HashAlgorithm.Create(algoName);

                if (hashAlgo is null)
                    return ErrorResult;

                // Use CryptoStream to compute the hash incrementally.
                stream.Position = 0;

                using var cryptoStream = new CryptoStream(
                    stream,
                    hashAlgo,
                    CryptoStreamMode.Read);

                byte[] buffer = new byte[BufferSize];
                while (cryptoStream.Read(buffer, 0, buffer.Length) > 0)
                {
                    // Read through — hash accumulates in hashAlgo.Hash.
                }

                cryptoStream.FlushFinalBlock();

                byte[]? hashBytes = hashAlgo.Hash;
                if (hashBytes is null)
                    return ErrorResult;

                return BytesToHexLower(hashBytes);
#endif
            }
            catch
            {
                return ErrorResult;
            }
        }

        /// <summary>
        /// Converts a byte array to a lowercase hex string.
        /// Uses platform-optimised path on .NET 6.0+ and a StringBuilder
        /// fallback on .NET Framework 4.7.2.
        /// </summary>
        private static string BytesToHexLower(byte[] bytes)
        {
#if NET472
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
#else
            return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
        }
    }
}
