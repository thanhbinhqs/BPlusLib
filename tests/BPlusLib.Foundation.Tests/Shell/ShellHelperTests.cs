// <copyright file="ShellHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Shell;

namespace BPlusLib.Foundation.Tests.Shell
{
    [Trait("Category", "Shell")]
    public sealed class ShellHelperTests
    {
        // ── ExecuteVerb ───────────────────────────────────────────────────

        [Fact]
        public void ExecuteVerb_NonExistentFile_ReturnsFalse()
        {
            // ShellExecuteExW is not available on Linux -> returns false.
            bool result = ShellHelper.ExecuteVerb("/nonexistent/file.txt");

            result.Should().BeFalse();
        }

        [Fact]
        public void ExecuteVerb_NullPath_ReturnsFalse()
        {
            ShellHelper.ExecuteVerb(null!).Should().BeFalse();
        }

        [Fact]
        public void ExecuteVerb_EmptyPath_ReturnsFalse()
        {
            ShellHelper.ExecuteVerb(string.Empty).Should().BeFalse();
        }

        [Fact]
        public void ExecuteVerb_WithRunAs_ReturnsFalse()
        {
            // On Linux, ShellExecuteExW is not available regardless of flags.
            bool result = ShellHelper.ExecuteVerb("test.txt", runAs: true);

            result.Should().BeFalse();
        }

        // ── GetAvailableVerbs ─────────────────────────────────────────────

        [Fact]
        public void GetAvailableVerbs_NonExistentExtension_ReturnsEmpty()
        {
            // Pass a path with a non-existent extension.
            // On Linux, Registry access fails -> returns empty list.
            var verbs = ShellHelper.GetAvailableVerbs("test.nonexistent");

            verbs.Should().NotBeNull();
            verbs.Should().BeEmpty();
        }

        [Fact]
        public void GetAvailableVerbs_NullPath_ReturnsEmpty()
        {
            var verbs = ShellHelper.GetAvailableVerbs(null!);

            verbs.Should().NotBeNull();
            verbs.Should().BeEmpty();
        }

        [Fact]
        public void GetAvailableVerbs_EmptyPath_ReturnsEmpty()
        {
            var verbs = ShellHelper.GetAvailableVerbs(string.Empty);

            verbs.Should().NotBeNull();
            verbs.Should().BeEmpty();
        }

        // ── GetDefaultProgram ─────────────────────────────────────────────

        [Fact]
        public void GetDefaultProgram_NonExistent_ReturnsNull()
        {
            // AssocQueryStringW not available on Linux -> returns null.
            string? prog = ShellHelper.GetDefaultProgram(".nonexistent_xyz");

            prog.Should().BeNull();
        }

        [Fact]
        public void GetDefaultProgram_NullExtension_ReturnsNull()
        {
            ShellHelper.GetDefaultProgram(null!).Should().BeNull();
        }

        [Fact]
        public void GetDefaultProgram_EmptyExtension_ReturnsNull()
        {
            ShellHelper.GetDefaultProgram(string.Empty).Should().BeNull();
        }

        // ── IsDefaultProgramForExtension ──────────────────────────────────

        [Fact]
        public void IsDefaultProgramForExtension_NullArgs_ReturnsFalse()
        {
            ShellHelper.IsDefaultProgramForExtension(null!, ".txt").Should().BeFalse();
            ShellHelper.IsDefaultProgramForExtension("app.exe", null!).Should().BeFalse();
        }

        [Fact]
        public void IsDefaultProgramForExtension_EmptyArgs_ReturnsFalse()
        {
            ShellHelper.IsDefaultProgramForExtension(string.Empty, ".txt").Should().BeFalse();
            ShellHelper.IsDefaultProgramForExtension("app.exe", string.Empty).Should().BeFalse();
        }

        // ── GetProgId ─────────────────────────────────────────────────────

        [Fact]
        public void GetProgId_NonExistent_ReturnsNull()
        {
            // Registry lookup on Linux -> null.
            string? progId = ShellHelper.GetProgId(".nonexistent_xyz");

            progId.Should().BeNull();
        }

        [Fact]
        public void GetProgId_NullExtension_ReturnsNull()
        {
            ShellHelper.GetProgId(null!).Should().BeNull();
        }

        [Fact]
        public void GetProgId_EmptyExtension_ReturnsNull()
        {
            ShellHelper.GetProgId(string.Empty).Should().BeNull();
        }

        // ── OpenWithDialog ────────────────────────────────────────────────

        [Fact]
        public void OpenWithDialog_NonExistent_ReturnsFalse()
        {
            // ShellExecuteExW not available on Linux -> returns false.
            bool result = ShellHelper.OpenWithDialog("/nonexistent/file.txt");

            result.Should().BeFalse();
        }

        [Fact]
        public void OpenWithDialog_NullPath_ReturnsFalse()
        {
            ShellHelper.OpenWithDialog(null!).Should().BeFalse();
        }

        // ── Recycle bin ───────────────────────────────────────────────────

        [Fact]
        public void GetRecycleBinSize_ShouldNotThrow()
        {
            // On Linux, SHQueryRecycleBinW is not available -> returns null.
            long? size = ShellHelper.GetRecycleBinSize();

            // Should not throw regardless of platform.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                size.Should().BeNull("because SHQueryRecycleBinW is Windows-only");
            }
        }

        [Fact]
        public void EmptyRecycleBin_OnLinux_ReturnsFalse()
        {
            bool result = ShellHelper.EmptyRecycleBin();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result.Should().BeFalse("because SHEmptyRecycleBinW is Windows-only");
            }
        }

        // ── IsInRecycleBin ────────────────────────────────────────────────

        [Fact]
        public void IsInRecycleBin_NormalPath_ReturnsFalse()
        {
            bool result = ShellHelper.IsInRecycleBin("/home/user/test.txt");

            result.Should().BeFalse();
        }

        [Fact]
        public void IsInRecycleBin_RecyclerPath_ReturnsTrue()
        {
            // Path containing "$Recycle.Bin" returns true.
            bool result = ShellHelper.IsInRecycleBin("/mnt/C/$Recycle.Bin/deleted-file.txt");

            result.Should().BeTrue();
        }

        [Fact]
        public void IsInRecycleBin_RecyclerPathLowerCase_ReturnsTrue()
        {
            // Case-insensitive check.
            bool result = ShellHelper.IsInRecycleBin("/C/$recycle.bin/file.txt");

            result.Should().BeTrue();
        }

        [Fact]
        public void IsInRecycleBin_NullPath_ReturnsFalse()
        {
            ShellHelper.IsInRecycleBin(null!).Should().BeFalse();
        }

        [Fact]
        public void IsInRecycleBin_EmptyPath_ReturnsFalse()
        {
            ShellHelper.IsInRecycleBin(string.Empty).Should().BeFalse();
        }

        [Fact]
        public void IsInRecycleBin_RecyclerName_ReturnsTrue()
        {
            // Use a Linux-style path. The method checks the parent directory name.
            bool result = ShellHelper.IsInRecycleBin("/mnt/RECYCLER/file.txt");

            result.Should().BeTrue();
        }

        [Fact]
        public void IsInRecycleBin_RecycledName_ReturnsTrue()
        {
            bool result = ShellHelper.IsInRecycleBin("/mnt/Recycled/file.txt");

            result.Should().BeTrue();
        }

        // ── GetExtensionDescription ───────────────────────────────────────

        [Fact]
        public void GetExtensionDescription_NonExistent_ReturnsNull()
        {
            string? desc = ShellHelper.GetExtensionDescription(".nonexistent_xyz");

            desc.Should().BeNull();
        }

        [Fact]
        public void GetExtensionDescription_NullExtension_ReturnsNull()
        {
            ShellHelper.GetExtensionDescription(null!).Should().BeNull();
        }

        [Fact]
        public void GetExtensionDescription_EmptyExtension_ReturnsNull()
        {
            ShellHelper.GetExtensionDescription(string.Empty).Should().BeNull();
        }

        // ── IsShortcut ────────────────────────────────────────────────────

        [Fact]
        public void IsShortcut_LnkFile_ReturnsTrue()
        {
            bool result = ShellHelper.IsShortcut("shortcut.lnk");

            result.Should().BeTrue();
        }

        [Fact]
        public void IsShortcut_LnkFileUpperCase_ReturnsTrue()
        {
            bool result = ShellHelper.IsShortcut("SHORTCUT.LNK");

            result.Should().BeTrue();
        }

        [Fact]
        public void IsShortcut_TxtFile_ReturnsFalse()
        {
            bool result = ShellHelper.IsShortcut("document.txt");

            result.Should().BeFalse();
        }

        [Fact]
        public void IsShortcut_NullPath_ReturnsFalse()
        {
            ShellHelper.IsShortcut(null!).Should().BeFalse();
        }

        [Fact]
        public void IsShortcut_EmptyPath_ReturnsFalse()
        {
            ShellHelper.IsShortcut(string.Empty).Should().BeFalse();
        }

        // ── GetFileTitle ──────────────────────────────────────────────────

        [Fact]
        public void GetFileTitle_NonExistent_ReturnsNull()
        {
            // On Linux, SHGetFileInfoW is not available -> returns null.
            string? title = ShellHelper.GetFileTitle("/nonexistent/file.txt");

            title.Should().BeNull();
        }

        [Fact]
        public void GetFileTitle_NullPath_ReturnsNull()
        {
            ShellHelper.GetFileTitle(null!).Should().BeNull();
        }

        [Fact]
        public void GetFileTitle_EmptyPath_ReturnsNull()
        {
            ShellHelper.GetFileTitle(string.Empty).Should().BeNull();
        }

        // ── GetFileExtension ──────────────────────────────────────────────

        [Fact]
        public void GetFileExtension_WithValidPath_ShouldReturnExtension()
        {
            string? ext = ShellHelper.GetFileExtension("document.txt");

            ext.Should().Be(".txt");
        }

        [Fact]
        public void GetFileExtension_NoExtension_ShouldReturnEmpty()
        {
            string? ext = ShellHelper.GetFileExtension("README");

            ext.Should().Be(string.Empty);
        }

        [Fact]
        public void GetFileExtension_NullPath_ReturnsNull()
        {
            ShellHelper.GetFileExtension(null!).Should().BeNull();
        }
    }
}
