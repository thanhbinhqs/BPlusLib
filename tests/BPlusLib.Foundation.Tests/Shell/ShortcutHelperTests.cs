// <copyright file="ShortcutHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Shell;

namespace BPlusLib.Foundation.Tests.Shell
{
    [Trait("Category", "Shell")]
    public sealed class ShortcutHelperTests
    {
        [SkippableFact]
        public void IsShortcut_LnkExtension_ReturnsTrue()
        {
            ShortcutHelper.IsShortcut("test.lnk").Should().BeTrue();
        }

        [SkippableFact]
        public void IsShortcut_NonLnk_ReturnsFalse()
        {
            ShortcutHelper.IsShortcut("test.txt").Should().BeFalse();
        }

        [SkippableFact]
        public void GetTargetPath_NonExistent_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var path = ShortcutHelper.GetTargetPath(@"C:\NONEXISTENT_XYZ.lnk");
            path.Should().BeNull();
        }

        [SkippableFact]
        public void CreateAndRead_Roundtrips()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            string tempLnk = Path.Combine(Path.GetTempPath(), $"BPlusLib_{Guid.NewGuid():N}.lnk");
            try
            {
                var info = new ShortcutInfo
                {
                    TargetPath = @"C:\Windows\System32\notepad.exe",
                    Arguments = "test.txt",
                    Description = "BPlusLib test shortcut",
                    WorkingDirectory = @"C:\Windows\System32",
                    ShowCommand = 1,
                };
                ShortcutHelper.Create(tempLnk, info).Should().BeTrue();
                var read = ShortcutHelper.Read(tempLnk);
                read.Should().NotBeNull();
                read!.TargetPath.ToLowerInvariant().Should().Contain("notepad.exe");
                read.Description.Should().Be("BPlusLib test shortcut");
            }
            finally
            {
                try { File.Delete(tempLnk); } catch { }
            }
        }
    }
}
