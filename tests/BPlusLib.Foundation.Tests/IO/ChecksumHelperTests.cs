// <copyright file="ChecksumHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.IO;

namespace BPlusLib.Foundation.Tests.IO
{
    [Trait("Category", "IO")]
    public sealed class ChecksumHelperTests : IDisposable
    {
        private readonly string _tempFile;

        // Known hash values for "Hello World" (no BOM, no newline, UTF-8)
        private const string Content = "Hello World";
        private const string ExpectedMd5 = "b10a8db164e0754105b7a99be72e3fe5";
        private const string ExpectedSha1 = "0a4d55a8d778e5022fab701977c5d840bbc486d0";
        private const string ExpectedSha256 = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";
        private const string ExpectedSha512 = "2c74fd17edafd80e8447b0d46741ee243b7eb74dd2149a0ab1b9246fb30382f27e853d8585719e0e67cbda0daa8f51671064615d645ae27acb15bfb1447f459b";
        private const string ExpectedCrc32 = "4a17b156";

        public ChecksumHelperTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), "ChecksumHelperTests_" + Guid.NewGuid().ToString("N") + ".txt");
            // Write "Hello World" without BOM and without trailing newline
            File.WriteAllText(_tempFile, Content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
            {
                try { File.Delete(_tempFile); }
                catch { /* Best-effort cleanup */ }
            }
        }

        // ── Specific hash methods ──────────────────────────────────────

        [Fact]
        public void ComputeMD5_WithKnownContent_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeMD5(_tempFile);
            hash.Should().Be(ExpectedMd5);
        }

        [Fact]
        public void ComputeSHA1_WithKnownContent_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeSHA1(_tempFile);
            hash.Should().Be(ExpectedSha1);
        }

        [Fact]
        public void ComputeSHA256_WithKnownContent_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeSHA256(_tempFile);
            hash.Should().Be(ExpectedSha256);
        }

        [Fact]
        public void ComputeSHA512_WithKnownContent_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeSHA512(_tempFile);
            hash.Should().Be(ExpectedSha512);
        }

        [Fact]
        public void ComputeCRC32_WithKnownContent_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeCRC32(_tempFile);
            hash.Should().Be(ExpectedCrc32);
        }

        // ── Generic ComputeHash ────────────────────────────────────────

        [Fact]
        public void ComputeHash_MD5_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeHash(_tempFile, HashAlgorithmName.MD5);
            hash.Should().Be(ExpectedMd5);
        }

        [Fact]
        public void ComputeHash_SHA256_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeHash(_tempFile, HashAlgorithmName.SHA256);
            hash.Should().Be(ExpectedSha256);
        }

        [Fact]
        public void ComputeHash_CRC32_ShouldMatch()
        {
            string hash = ChecksumHelper.ComputeHash(_tempFile, new HashAlgorithmName("CRC32"));
            hash.Should().Be(ExpectedCrc32);
        }

        // ── Error handling ─────────────────────────────────────────────

        [Fact]
        public void ComputeHash_WithNonExistentFile_ShouldReturn_ERROR()
        {
            string path = Path.Combine(Path.GetTempPath(), "nonexistent_file_xyz.txt");
            string hash = ChecksumHelper.ComputeHash(path, HashAlgorithmName.MD5);
            hash.Should().Be("ERROR");
        }

        [Fact]
        public void ComputeMD5_WithNonExistentFile_ShouldReturn_ERROR()
        {
            string path = Path.Combine(Path.GetTempPath(), "nonexistent_file_xyz.txt");
            string hash = ChecksumHelper.ComputeMD5(path);
            hash.Should().Be("ERROR");
        }

        [Fact]
        public void ComputeSHA256_WithNonExistentFile_ShouldReturn_ERROR()
        {
            string path = Path.Combine(Path.GetTempPath(), "nonexistent_file_xyz.txt");
            string hash = ChecksumHelper.ComputeSHA256(path);
            hash.Should().Be("ERROR");
        }

        [Fact]
        public void ComputeCRC32_WithNonExistentFile_ShouldReturn_ERROR()
        {
            string path = Path.Combine(Path.GetTempPath(), "nonexistent_file_xyz.txt");
            string hash = ChecksumHelper.ComputeCRC32(path);
            hash.Should().Be("ERROR");
        }

        // ── VerifyHash ─────────────────────────────────────────────────

        [Fact]
        public void VerifyHash_WithCorrectHash_ShouldReturnTrue()
        {
            bool result = ChecksumHelper.VerifyHash(_tempFile, ExpectedSha256, HashAlgorithmName.SHA256);
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyHash_WithIncorrectHash_ShouldReturnFalse()
        {
            bool result = ChecksumHelper.VerifyHash(_tempFile, "0000000000000000000000000000000000000000000000000000000000000000", HashAlgorithmName.SHA256);
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_WithEmptyExpected_ShouldReturnFalse()
        {
            bool result = ChecksumHelper.VerifyHash(_tempFile, string.Empty, HashAlgorithmName.SHA256);
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_WithNonExistentFile_ShouldReturnFalse()
        {
            string path = Path.Combine(Path.GetTempPath(), "no_file_for_verify.txt");
            bool result = ChecksumHelper.VerifyHash(path, ExpectedSha256, HashAlgorithmName.SHA256);
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_CaseInsensitive_ShouldMatch()
        {
            string upperHash = ExpectedSha256.ToUpperInvariant();
            bool result = ChecksumHelper.VerifyHash(_tempFile, upperHash, HashAlgorithmName.SHA256);
            result.Should().BeTrue();
        }

        // ── CRC32 edge cases ───────────────────────────────────────────

        [Fact]
        public void ComputeCRC32_EmptyFile_ShouldBeExpected()
        {
            string emptyFile = Path.Combine(Path.GetTempPath(), "empty_crc_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(emptyFile, string.Empty);
                // CRC32 of empty file is 0x00000000 → "00000000"
                string hash = ChecksumHelper.ComputeCRC32(emptyFile);
                hash.Should().Be("00000000");
            }
            finally
            {
                if (File.Exists(emptyFile)) File.Delete(emptyFile);
            }
        }
    }
}
