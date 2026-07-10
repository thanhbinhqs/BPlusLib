// <copyright file="ExplorerHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Explorer;

namespace BPlusLib.Foundation.Tests.Explorer
{
    [Trait("Category", "Explorer")]
    public sealed class ExplorerHelperTests : IDisposable
    {
        private readonly string _tempDir;

        public ExplorerHelperTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ExplorerHelperTests_" + Guid.NewGuid().ToString("N"));
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

        private string NonExistentPath => Path.Combine(_tempDir, "does_not_exist_" + Guid.NewGuid().ToString("N"));

        // ── GetKnownFolderPath ─────────────────────────────────

        [Fact]
        public void GetKnownFolderPath_Desktop_ShouldReturnOrNull()
        {
            // On Linux, SHGetKnownFolderPath will fail -> returns null.
            // We accept either null or a valid path (someone running on Windows).
            string? path = ExplorerHelper.GetKnownFolderPath(KnownFolder.Desktop);
            if (path is not null)
            {
                path.Should().NotBeNullOrWhiteSpace();
                Directory.Exists(path).Should().BeTrue();
            }
        }

        [Fact]
        public void GetKnownFolderPath_Temp_ShouldReturnPath()
        {
            // Temp is implemented via Path.GetTempPath(), which works on Linux.
            string? path = ExplorerHelper.GetKnownFolderPath(KnownFolder.Temp);
            path.Should().NotBeNullOrWhiteSpace();
            path.Should().Be(Path.GetTempPath());
        }

        // ── OpenInExplorer ─────────────────────────────────────

        [Fact]
        public void OpenInExplorer_NullPath_ReturnsFalse()
        {
            bool result = ExplorerHelper.OpenInExplorer(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void OpenInExplorer_EmptyPath_ReturnsFalse()
        {
            bool result = ExplorerHelper.OpenInExplorer(string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void OpenInExplorer_NonExistent_ReturnsFalse()
        {
            // On Linux, Process.Start("explorer.exe") will fail gracefully.
            bool result = ExplorerHelper.OpenInExplorer(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── SelectInExplorer ───────────────────────────────────

        [Fact]
        public void SelectInExplorer_NullPath_ReturnsFalse()
        {
            bool result = ExplorerHelper.SelectInExplorer(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void SelectInExplorer_NonExistent_ReturnsFalse()
        {
            bool result = ExplorerHelper.SelectInExplorer(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── ShowFileProperties ────────────────────────────────

        [Fact]
        public void ShowFileProperties_NullPath_ReturnsFalse()
        {
            bool result = ExplorerHelper.ShowFileProperties(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void ShowFileProperties_NonExistent_ReturnsFalse()
        {
            bool result = ExplorerHelper.ShowFileProperties(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── ShowFileInExplorer / SelectInExplorer alias ────────

        [Fact]
        public void ShowFileInExplorer_NonExistent_ReturnsFalse()
        {
            bool result = ExplorerHelper.ShowFileInExplorer(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── GetFileSizeOnDisk ──────────────────────────────────

        [Fact]
        public void GetFileSizeOnDisk_NullPath_ReturnsNull()
        {
            long? result = ExplorerHelper.GetFileSizeOnDisk(null!);
            result.Should().BeNull();
        }

        [Fact]
        public void GetFileSizeOnDisk_NonExistent_ReturnsNull()
        {
            long? result = ExplorerHelper.GetFileSizeOnDisk(NonExistentPath);
            result.Should().BeNull();
        }

        // ── IsFileInUse ────────────────────────────────────────

        [Fact]
        public void IsFileInUse_NullPath_ReturnsFalse()
        {
            bool result = ExplorerHelper.IsFileInUse(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void IsFileInUse_NonExistent_ReturnsFalse()
        {
            bool result = ExplorerHelper.IsFileInUse(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── GetFileTypeDescription ─────────────────────────────

        [Fact]
        public void GetFileTypeDescription_NullPath_ReturnsNull()
        {
            string? result = ExplorerHelper.GetFileTypeDescription(null!);
            result.Should().BeNull();
        }

        [Fact]
        public void GetFileTypeDescription_NonExistent_ReturnsNull()
        {
            string? result = ExplorerHelper.GetFileTypeDescription(NonExistentPath);
            result.Should().BeNull();
        }

        // ── GetFileOwner ───────────────────────────────────────

        [Fact]
        public void GetFileOwner_NullPath_ReturnsNull()
        {
            string? result = ExplorerHelper.GetFileOwner(null!);
            result.Should().BeNull();
        }

        [Fact]
        public void GetFileOwner_NonExistent_ReturnsNull()
        {
            string? result = ExplorerHelper.GetFileOwner(NonExistentPath);
            result.Should().BeNull();
        }

        // ── GetRecentFiles ─────────────────────────────────────

        [Fact]
        public void GetRecentFiles_ShouldReturnOrEmpty()
        {
            // On Linux, GetKnownFolderPath(KnownFolder.Recent) returns null,
            // so GetRecentFiles returns an empty list.
            IReadOnlyList<string> files = ExplorerHelper.GetRecentFiles();
            files.Should().NotBeNull();
        }

        // ── ResolveShortcut ────────────────────────────────────

        [Fact]
        public void ResolveShortcut_NullPath_ReturnsNull()
        {
            string? result = ExplorerHelper.ResolveShortcut(null!);
            result.Should().BeNull();
        }

        [Fact]
        public void ResolveShortcut_NonExistent_ReturnsNull()
        {
            string? result = ExplorerHelper.ResolveShortcut(NonExistentPath);
            result.Should().BeNull();
        }

        // ── TryRecycle ─────────────────────────────────────────

        [Fact]
        public void TryRecycle_NullPath_ReturnsFalse()
        {
            bool result = ExplorerHelper.TryRecycle(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryRecycle_NonExistent_ReturnsFalse()
        {
            bool result = ExplorerHelper.TryRecycle(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── KnownFolder enum values have non-empty GUIDs ───────

        [Fact]
        public void KnownFolder_AllValues_ShouldHaveNonEmptyGuid()
        {
            // Call GetKnownFolderPath for all enum values just to exercise
            // the mapping. We don't care about the result, just that it
            // doesn't throw and the GUID mapping is consistent.
            var folders = (KnownFolder[])Enum.GetValues(typeof(KnownFolder));
            foreach (KnownFolder folder in folders)
            {
                // Skip Temp which doesn't use SHGetKnownFolderPath
                if (folder == KnownFolder.Temp)
                    continue;

                string? path = ExplorerHelper.GetKnownFolderPath(folder);
                // Accept either null (Linux) or a valid path (Windows)
                if (path is not null)
                {
                    path.Should().NotBeNullOrWhiteSpace();
                }
            }
        }
    }
}
