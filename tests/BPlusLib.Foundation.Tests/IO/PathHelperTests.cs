// <copyright file="PathHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.IO;

namespace BPlusLib.Foundation.Tests.IO
{
    [Trait("Category", "IO")]
    public sealed class PathHelperTests : IDisposable
    {
        private readonly string _tempDir;

        public PathHelperTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PathHelperTests_" + Guid.NewGuid().ToString("N"));
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

        // ── SafeCombine ────────────────────────────────────────────────

        [Fact]
        public void SafeCombine_Basic_ShouldCombine()
        {
            string? result = PathHelper.SafeCombine("/base", "sub/file.txt");
            result.Should().NotBeNull();
            result.Should().Contain("base");
            result.Should().Contain("sub");
            result.Should().Contain("file.txt");
        }

        [Fact]
        public void SafeCombine_WithNull_ShouldReturnNull()
        {
            PathHelper.SafeCombine(null!, "file.txt").Should().BeNull();
            PathHelper.SafeCombine("/base", null!).Should().BeNull();
        }

        [Fact]
        public void SafeCombine_WithInvalidChars_ShouldReturnNull()
        {
            // \0 is universally invalid in paths
            PathHelper.SafeCombine("/base", "file\0.txt").Should().BeNull();
        }

        // ── HasInvalidPathChars ────────────────────────────────────────

        [Fact]
        public void HasInvalidPathChars_WithValidPath_ShouldReturnFalse()
        {
            PathHelper.HasInvalidPathChars("/home/user/file.txt").Should().BeFalse();
        }

        [Fact]
        public void HasInvalidPathChars_WithNullChar_ShouldReturnTrue()
        {
            // \0 is universally invalid in paths
            PathHelper.HasInvalidPathChars("file\0.txt").Should().BeTrue();
        }

        [Fact]
        public void HasInvalidPathChars_NullOrEmpty_ShouldReturnFalse()
        {
            PathHelper.HasInvalidPathChars(null!).Should().BeFalse();
            PathHelper.HasInvalidPathChars(string.Empty).Should().BeFalse();
        }

        // ── HasInvalidFileNameChars ────────────────────────────────────

        [Fact]
        public void HasInvalidFileNameChars_WithValidName_ShouldReturnFalse()
        {
            PathHelper.HasInvalidFileNameChars("myfile.txt").Should().BeFalse();
        }

        [Fact]
        public void HasInvalidFileNameChars_WithNullChar_ShouldReturnTrue()
        {
            // \0 is universally invalid in filenames
            PathHelper.HasInvalidFileNameChars("file\0.txt").Should().BeTrue();
        }

        [Fact]
        public void HasInvalidFileNameChars_WithDirectorySeparator_ShouldReturnTrue()
        {
            // / is invalid in filenames on all platforms
            PathHelper.HasInvalidFileNameChars("file/name.txt").Should().BeTrue();
        }

        [Fact]
        public void HasInvalidFileNameChars_NullOrEmpty_ShouldReturnFalse()
        {
            PathHelper.HasInvalidFileNameChars(null!).Should().BeFalse();
            PathHelper.HasInvalidFileNameChars(string.Empty).Should().BeFalse();
        }

        // ── SanitizeFileName ───────────────────────────────────────────

        [Fact]
        public void SanitizeFileName_WithInvalidChars_ShouldReplace()
        {
            string result = PathHelper.SanitizeFileName("file\0.txt", '_');
            result.Should().Be("file_.txt");
        }

        [Fact]
        public void SanitizeFileName_WithNoInvalidChars_ShouldReturnSame()
        {
            string result = PathHelper.SanitizeFileName("normal-file.txt");
            result.Should().Be("normal-file.txt");
        }

        [Fact]
        public void SanitizeFileName_NullOrEmpty_ShouldReturnEmpty()
        {
            PathHelper.SanitizeFileName(null!).Should().Be(string.Empty);
            PathHelper.SanitizeFileName(string.Empty).Should().Be(string.Empty);
        }

        [Fact]
        public void SanitizeFileName_WithDirectorySeparator_ShouldReplace()
        {
            string result = PathHelper.SanitizeFileName("a/b.txt", '-');
            result.Should().Be("a-b.txt");
        }

        // ── IsAbsolutePath ─────────────────────────────────────────────

        [Fact]
        public void IsAbsolutePath_WithAbsolutePath_ShouldBeTrue()
        {
            PathHelper.IsAbsolutePath("/home/user").Should().BeTrue();
        }

        [Fact]
        public void IsAbsolutePath_WithRelativePath_ShouldBeFalse()
        {
            PathHelper.IsAbsolutePath("relative/path").Should().BeFalse();
            PathHelper.IsAbsolutePath("file.txt").Should().BeFalse();
        }

        [Fact]
        public void IsAbsolutePath_Empty_ShouldBeFalse()
        {
            PathHelper.IsAbsolutePath(string.Empty).Should().BeFalse();
        }

        // ── GetRelativePath ────────────────────────────────────────────

        [Fact]
        public void GetRelativePath_ShouldComputeCorrectly()
        {
            string? relative = PathHelper.GetRelativePath("/home/user/docs/file.txt", "/home/user/");
            relative.Should().NotBeNull();
            relative.Should().Be("docs/file.txt");
        }

        [Fact]
        public void GetRelativePath_SamePath_ShouldReturnDotOrEmpty()
        {
            string basePath = _tempDir;
            string? relative = PathHelper.GetRelativePath(basePath, basePath);
            relative.Should().NotBeNull();
        }

        [Fact]
        public void GetRelativePath_NullOrEmpty_ShouldReturnNull()
        {
            PathHelper.GetRelativePath(null!, "/base").Should().BeNull();
            PathHelper.GetRelativePath("/full", null!).Should().BeNull();
            PathHelper.GetRelativePath(string.Empty, "/base").Should().BeNull();
        }

        // ── NormalizePath ──────────────────────────────────────────────

        [Fact]
        public void NormalizePath_ShouldNormalize()
        {
            string normalized = PathHelper.NormalizePath(_tempDir);
            normalized.Should().NotBeNullOrEmpty();
            Path.IsPathRooted(normalized).Should().BeTrue();
        }

        [Fact]
        public void NormalizePath_Empty_ShouldReturnEmpty()
        {
            PathHelper.NormalizePath(string.Empty).Should().Be(string.Empty);
        }

        [Fact]
        public void NormalizePath_WithRelative_ShouldReturnFull()
        {
            string normalized = PathHelper.NormalizePath(".");
            normalized.Should().NotBeNullOrEmpty();
            Path.IsPathRooted(normalized).Should().BeTrue();
        }

        // ── PathExists ─────────────────────────────────────────────────

        [Fact]
        public void PathExists_ExistingFile_ShouldBeTrue()
        {
            string path = Path.Combine(_tempDir, "exists.txt");
            File.WriteAllText(path, "test");
            PathHelper.PathExists(path).Should().BeTrue();
        }

        [Fact]
        public void PathExists_NonExistent_ShouldBeFalse()
        {
            string path = Path.Combine(_tempDir, "no_such_file.txt");
            PathHelper.PathExists(path).Should().BeFalse();
        }

        [Fact]
        public void PathExists_ExistingDirectory_ShouldBeTrue()
        {
            PathHelper.PathExists(_tempDir).Should().BeTrue();
        }

        [Fact]
        public void PathExists_Empty_ShouldBeFalse()
        {
            PathHelper.PathExists(string.Empty).Should().BeFalse();
        }

        // ── GetAvailableFileName ───────────────────────────────────────

        [Fact]
        public void GetAvailableFileName_WithNonExistent_ShouldReturnBase()
        {
            string path = Path.Combine(_tempDir, "unique.txt");
            string result = PathHelper.GetAvailableFileName(path);
            result.Should().Be(path);
        }

        [Fact]
        public void GetAvailableFileName_ShouldGenerateSequential()
        {
            string path = Path.Combine(_tempDir, "seq.txt");
            File.WriteAllText(path, "first");

            string result = PathHelper.GetAvailableFileName(path);
            result.Should().NotBe(path);
            result.Should().Contain("seq");
            result.Should().Contain("(1)");
        }

        [Fact]
        public void GetAvailableFileName_WithPrefix_ShouldIncludePrefix()
        {
            string path = Path.Combine(_tempDir, "file.txt");
            File.WriteAllText(path, "original");

            string result = PathHelper.GetAvailableFileName(path, "Copy - ");
            result.Should().Contain("Copy - ");
        }

        [Fact]
        public void GetAvailableFileName_Empty_ShouldReturnEmpty()
        {
            PathHelper.GetAvailableFileName(string.Empty).Should().Be(string.Empty);
        }

        // ── GetPathSize ────────────────────────────────────────────────

        [Fact]
        public void GetPathSize_ExistingFile_ShouldReturnPositiveOrZero()
        {
            string path = Path.Combine(_tempDir, "size_test.txt");
            File.WriteAllText(path, "some content here");

            long size = PathHelper.GetPathSize(path);
            size.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetPathSize_NonExistent_ShouldReturnMinusOne()
        {
            string path = Path.Combine(_tempDir, "no_file_here.txt");
            long size = PathHelper.GetPathSize(path);
            size.Should().Be(-1);
        }

        [Fact]
        public void GetPathSize_EmptyPath_ShouldReturnMinusOne()
        {
            long size = PathHelper.GetPathSize(string.Empty);
            size.Should().Be(-1);
        }

        [Fact]
        public void GetPathSize_EmptyFile_ShouldReturnZero()
        {
            string path = Path.Combine(_tempDir, "empty_file.txt");
            File.WriteAllText(path, string.Empty);

            long size = PathHelper.GetPathSize(path);
            size.Should().Be(0);
        }
    }
}
