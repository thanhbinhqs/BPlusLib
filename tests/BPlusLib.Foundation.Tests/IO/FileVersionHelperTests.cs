// <copyright file="FileVersionHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.IO;

namespace BPlusLib.Foundation.Tests.IO
{
    [Trait("Category", "IO")]
    public sealed class FileVersionHelperTests
    {
        // ── Windows-only: kernel32.dll ─────────────────────────────────

        [SkippableFact]
        public void GetVersionInfo_Kernel32_ReturnsData()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string kernel32 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "kernel32.dll");

            var info = FileVersionHelper.GetVersionInfo(kernel32);

            info.Should().NotBeNull();
            info!.FileVersion.Should().NotBeNullOrEmpty();
            info.ProductVersion.Should().NotBeNullOrEmpty();
        }

        [SkippableFact]
        public void GetFileVersion_Kernel32_ReturnsVersion()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string kernel32 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "kernel32.dll");

            string? version = FileVersionHelper.GetFileVersion(kernel32);

            version.Should().NotBeNullOrEmpty();
        }

        [SkippableFact]
        public void GetCompanyName_Kernel32_ReturnsMicrosoft()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string kernel32 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "kernel32.dll");

            string? company = FileVersionHelper.GetCompanyName(kernel32);

            // kernel32.dll is always from Microsoft
            company.Should().NotBeNullOrEmpty();
            company.Should().Contain("Microsoft");
        }

        // ── Cross-platform: edge cases ─────────────────────────────────

        [Fact]
        public void GetVersionInfo_TextFile_ReturnsNull()
        {
            // Create a temporary text file (not a PE file)
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "not a PE file");
                var info = FileVersionHelper.GetVersionInfo(tempFile);
                info.Should().BeNull();
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetVersionInfo_NullPath_ReturnsNull()
        {
            FileVersionHelper.GetVersionInfo(null!).Should().BeNull();
        }

        [Fact]
        public void GetVersionInfo_EmptyPath_ReturnsNull()
        {
            FileVersionHelper.GetVersionInfo(string.Empty).Should().BeNull();
        }

        [Fact]
        public void GetVersionInfo_NonExistentPath_ReturnsNull()
        {
            FileVersionHelper.GetVersionInfo(@"Z:\nonexistent\file.dll").Should().BeNull();
        }
    }
}
