// <copyright file="SafeFileOpsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.IO;

namespace BPlusLib.Foundation.Tests.IO
{
    [Trait("Category", "IO")]
    public sealed class SafeFileOpsTests : IDisposable
    {
        private readonly string _tempDir;

        public SafeFileOpsTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SafeFileOpsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); }
                catch { /* Best-effort cleanup */ }
            }
        }

        private string GetTempPath(string fileName) =>
            Path.Combine(_tempDir, fileName);

        /// <summary>
        /// Creates a file and returns its path. Uses direct File.WriteAllText so
        /// the file is guaranteed to exist for subsequent SafeFileOps operations
        /// that require an existing destination (e.g. TryWriteAllText uses File.Replace).
        /// </summary>
        private string CreateFile(string fileName, string content = "initial")
        {
            string path = GetTempPath(fileName);
            File.WriteAllText(path, content);
            return path;
        }

        // ── TryWriteAllText / TryReadAllText ────────────────────────────

        [Fact]
        public void TryWriteAllText_And_TryReadAllText_ShouldRoundtrip()
        {
            // File.Replace requires the destination to exist first.
            string path = CreateFile("roundtrip.txt", "old content");
            string content = "Hello, World! 测试 \n第二行";

            bool writeResult = SafeFileOps.TryWriteAllText(path, content);
            writeResult.Should().BeTrue();

            bool readResult = SafeFileOps.TryReadAllText(path, out string? readContent, out Exception? error);
            readResult.Should().BeTrue();
            error.Should().BeNull();
            readContent.Should().Be(content);
        }

        [Fact]
        public void TryWriteAllText_WithNullContent_ShouldWriteEmptyString()
        {
            string path = CreateFile("empty_write.txt", "old");

            bool writeResult = SafeFileOps.TryWriteAllText(path, null);
            writeResult.Should().BeTrue();

            bool readResult = SafeFileOps.TryReadAllText(path, out string? readContent, out _);
            readResult.Should().BeTrue();
            readContent.Should().Be(string.Empty);
        }

        [Fact]
        public void TryWriteAllText_WithCustomEncoding_ShouldUseEncoding()
        {
            string path = CreateFile("encoding.txt", "old");
            string content = "Hello";

            bool writeResult = SafeFileOps.TryWriteAllText(path, content, Encoding.UTF32);
            writeResult.Should().BeTrue();

            // Read back to verify
            bool readResult = SafeFileOps.TryReadAllText(path, out string? readContent, out _);
            readResult.Should().BeTrue();
            readContent.Should().Be(content);
        }

        [Fact]
        public void TryWriteAllText_ToInvalidPath_ShouldReturnFalse()
        {
            string invalidPath = Path.Combine(_tempDir, "invalid\0chars.txt");
            bool result = SafeFileOps.TryWriteAllText(invalidPath, "test");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryWriteAllText_WithEmptyPath_ShouldReturnFalse()
        {
            SafeFileOps.TryWriteAllText(string.Empty, "test").Should().BeFalse();
        }

        [Fact]
        public void TryReadAllText_NonExistentFile_ShouldReturnFalse()
        {
            string path = GetTempPath("does_not_exist.txt");
            bool result = SafeFileOps.TryReadAllText(path, out string? content, out Exception? error);
            result.Should().BeFalse();
            content.Should().BeNull();
            error.Should().NotBeNull();
        }

        [Fact]
        public void TryReadAllText_EmptyPath_ShouldReturnFalse()
        {
            bool result = SafeFileOps.TryReadAllText(string.Empty, out string? content, out Exception? error);
            result.Should().BeFalse();
            content.Should().BeNull();
            error.Should().NotBeNull();
        }

        // ── TryCopy ────────────────────────────────────────────────────

        [Fact]
        public void TryCopy_ShouldCopyFile()
        {
            string source = CreateFile("copy_source.txt", "copy test content");
            string dest = GetTempPath("copy_dest.txt");

            bool result = SafeFileOps.TryCopy(source, dest);
            result.Should().BeTrue();

            bool readResult = SafeFileOps.TryReadAllText(dest, out string? content, out _);
            readResult.Should().BeTrue();
            content.Should().Be("copy test content");
        }

        [Fact]
        public void TryCopy_NonExistentSource_ShouldReturnFalse()
        {
            string source = GetTempPath("nonexistent_source.txt");
            string dest = GetTempPath("copy_dest2.txt");
            bool result = SafeFileOps.TryCopy(source, dest);
            result.Should().BeFalse();
        }

        // ── TryMove ────────────────────────────────────────────────────

        [Fact]
        public void TryMove_ShouldMoveFile()
        {
            string source = CreateFile("move_source.txt", "move test content");
            string dest = GetTempPath("move_dest.txt");

            bool result = SafeFileOps.TryMove(source, dest);
            result.Should().BeTrue();

            File.Exists(source).Should().BeFalse();
            File.Exists(dest).Should().BeTrue();

            bool readResult = SafeFileOps.TryReadAllText(dest, out string? content, out _);
            readResult.Should().BeTrue();
            content.Should().Be("move test content");
        }

        [Fact]
        public void TryMove_NonExistentSource_ShouldReturnFalse()
        {
            string source = GetTempPath("nonexistent_move_source.txt");
            string dest = GetTempPath("move_dest2.txt");
            bool result = SafeFileOps.TryMove(source, dest);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryMove_OverwriteExisting_ShouldSucceed()
        {
            string source = CreateFile("overwrite_source.txt", "new content");
            string dest = CreateFile("overwrite_dest.txt", "old content");

            bool result = SafeFileOps.TryMove(source, dest, overwrite: true);
            result.Should().BeTrue();

            bool readResult = SafeFileOps.TryReadAllText(dest, out string? content, out _);
            readResult.Should().BeTrue();
            content.Should().Be("new content");
        }

        // ── TryDelete ──────────────────────────────────────────────────

        [Fact]
        public void TryDelete_ShouldDeleteFile()
        {
            string path = CreateFile("delete_me.txt", "delete me");

            bool result = SafeFileOps.TryDelete(path);
            result.Should().BeTrue();
            File.Exists(path).Should().BeFalse();
        }

        [Fact]
        public void TryDelete_NonExistentFile_ShouldReturnTrue()
        {
            string path = GetTempPath("never_existed.txt");
            bool result = SafeFileOps.TryDelete(path);
            result.Should().BeTrue(); // Idempotent — non-existent is success
        }

        [Fact]
        public void TryDelete_EmptyPath_ShouldReturnFalse()
        {
            SafeFileOps.TryDelete(string.Empty).Should().BeFalse();
        }

        [Fact]
        public void TryDelete_Directory_ShouldDeleteDirectory()
        {
            string dir = Path.Combine(_tempDir, "subdir_to_delete");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "file.txt"), "test");

            bool result = SafeFileOps.TryDelete(dir, recursive: true);
            result.Should().BeTrue();
            Directory.Exists(dir).Should().BeFalse();
        }

        // ── IsFileLocked ───────────────────────────────────────────────

        [Fact]
        public void IsFileLocked_OnNonExistentFile_ShouldReturnFalse()
        {
            string path = GetTempPath("no_file.txt");
            bool result = SafeFileOps.IsFileLocked(path);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsFileLocked_OnExistingFile_ShouldReturnFalse()
        {
            string path = CreateFile("unlocked.txt", "not locked");
            bool result = SafeFileOps.IsFileLocked(path);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsFileLocked_EmptyPath_ShouldReturnFalse()
        {
            SafeFileOps.IsFileLocked(string.Empty).Should().BeFalse();
        }

        // ── EnsureDirectoryExists ──────────────────────────────────────

        [Fact]
        public void EnsureDirectoryExists_ShouldCreateDirectory()
        {
            string dir = Path.Combine(_tempDir, "new_dir", "nested");
            bool result = SafeFileOps.EnsureDirectoryExists(dir);
            result.Should().BeTrue();
            Directory.Exists(dir).Should().BeTrue();
        }

        [Fact]
        public void EnsureDirectoryExists_ExistingDirectory_ShouldReturnTrue()
        {
            bool result = SafeFileOps.EnsureDirectoryExists(_tempDir);
            result.Should().BeTrue();
        }

        [Fact]
        public void EnsureDirectoryExists_EmptyPath_ShouldReturnFalse()
        {
            SafeFileOps.EnsureDirectoryExists(string.Empty).Should().BeFalse();
        }

        // ── GetTempFilePath ────────────────────────────────────────────

        [Fact]
        public void GetTempFilePath_ShouldReturnAbsolutePath()
        {
            string path = SafeFileOps.GetTempFilePath();
            path.Should().NotBeNullOrEmpty();
            Path.IsPathRooted(path).Should().BeTrue();
        }

        [Fact]
        public void GetTempFilePath_ShouldHaveCorrectExtension()
        {
            string path = SafeFileOps.GetTempFilePath(".txt");
            path.Should().NotBeNullOrEmpty();
            path.Should().EndWith(".txt");
        }

        [Fact]
        public void GetTempFilePath_DefaultExtension_ShouldBeDotTmp()
        {
            string path = SafeFileOps.GetTempFilePath();
            path.Should().NotBeNullOrEmpty();
            path.Should().EndWith(".tmp");
        }

        [Fact]
        public void GetTempFilePath_WithoutLeadingDot_ShouldAddDot()
        {
            string path = SafeFileOps.GetTempFilePath("log");
            path.Should().NotBeNullOrEmpty();
            path.Should().EndWith(".log");
        }

        // ── TryGetFileHash ─────────────────────────────────────────────

        [Fact]
        public void TryGetFileHash_SHA256_ShouldReturnValidHash()
        {
            string path = CreateFile("hash_test.txt", "Hello World");
            // Note: "Hello World" (11 bytes, UTF-8 no BOM, no newline)
            // SHA256: a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e

            bool result = SafeFileOps.TryGetFileHash(path, HashAlgorithmName.SHA256, out string? hash, out Exception? error);
            result.Should().BeTrue();
            error.Should().BeNull();
            hash.Should().NotBeNullOrEmpty();
            hash.Should().Be("a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e");
        }

        [Fact]
        public void TryGetFileHash_MD5_ShouldReturnValidHash()
        {
            string path = CreateFile("hash_md5.txt", "Hello World");
            bool result = SafeFileOps.TryGetFileHash(path, HashAlgorithmName.MD5, out string? hash, out _);
            result.Should().BeTrue();
            hash.Should().Be("b10a8db164e0754105b7a99be72e3fe5");
        }

        [Fact]
        public void TryGetFileHash_SHA1_ShouldReturnValidHash()
        {
            string path = CreateFile("hash_sha1.txt", "Hello World");
            bool result = SafeFileOps.TryGetFileHash(path, HashAlgorithmName.SHA1, out string? hash, out _);
            result.Should().BeTrue();
            hash.Should().Be("0a4d55a8d778e5022fab701977c5d840bbc486d0");
        }

        [Fact]
        public void TryGetFileHash_NonExistentFile_ShouldReturnFalse()
        {
            string path = GetTempPath("no_hash_file.txt");
            bool result = SafeFileOps.TryGetFileHash(path, HashAlgorithmName.SHA256, out string? hash, out Exception? error);
            result.Should().BeFalse();
            hash.Should().BeNull();
            error.Should().NotBeNull();
        }

        [Fact]
        public void TryGetFileHash_EmptyPath_ShouldReturnFalse()
        {
            bool result = SafeFileOps.TryGetFileHash(string.Empty, HashAlgorithmName.SHA256, out string? hash, out Exception? error);
            result.Should().BeFalse();
            hash.Should().BeNull();
            error.Should().NotBeNull();
        }
    }
}
