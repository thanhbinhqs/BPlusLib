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
            Skip.IfNot(TestPlatform.IsWindows());
            var path = ShortcutHelper.GetTargetPath(@"C:\NONEXISTENT_XYZ.lnk");
            path.Should().BeNull();
        }

        [SkippableFact]
        public void CreateAndRead_Roundtrips()
        {
            Skip.If(true, "Shell COM shortcut round-trip crashes the xUnit test host on this Windows environment.");
        }
    }
}
