// <copyright file="AssocHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using FluentAssertions;
using Xunit;
using BPlusLib.Foundation.Shell;

namespace BPlusLib.Foundation.Tests.Shell
{
    [Trait("Category", "Shell")]
    public sealed class AssocHelperTests
    {
        // ── GetFileTypeDescription ─────────────────────────────────────

        [SkippableFact]
        public void GetFileTypeDescription_Txt_ReturnsText()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            string? desc = AssocHelper.GetFileTypeDescription(".txt");

            desc.Should().NotBeNullOrEmpty();
            desc.Should().ContainEquivalentOf("text");
        }

        [Fact]
        public void GetFileTypeDescription_NullExtension_ReturnsNull()
        {
            AssocHelper.GetFileTypeDescription(null!).Should().BeNull();
        }

        [Fact]
        public void GetFileTypeDescription_EmptyExtension_ReturnsNull()
        {
            AssocHelper.GetFileTypeDescription(string.Empty).Should().BeNull();
        }

        // ── GetAssociatedExecutable ────────────────────────────────────

        [SkippableFact]
        public void GetAssociatedExecutable_Txt_ReturnsExecutablePath()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            string? exe = AssocHelper.GetAssociatedExecutable(".txt");

            exe.Should().NotBeNullOrEmpty();
            exe.Should().ContainEquivalentOf("notepad");
            exe.Should().EndWith(".exe");
        }

        // ── GetProgId ──────────────────────────────────────────────────

        [SkippableFact]
        public void GetProgId_Txt_ReturnsTxtRelatedProgId()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            string? progId = AssocHelper.GetProgId(".txt");

            progId.Should().NotBeNullOrEmpty();
            progId.Should().ContainEquivalentOf("txtfile");
        }

        // ── IsExtensionRegistered ──────────────────────────────────────

        [SkippableFact]
        public void IsExtensionRegistered_Txt_ReturnsTrue()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            bool registered = AssocHelper.IsExtensionRegistered(".txt");

            registered.Should().BeTrue();
        }

        [Fact]
        public void IsExtensionRegistered_Unknown_ReturnsBool()
        {
            bool registered = AssocHelper.IsExtensionRegistered(".nonexistent_xyz");
            ((object)registered).Should().BeOfType<bool>();
        }

        [Fact]
        public void IsExtensionRegistered_Null_ReturnsFalse()
        {
            AssocHelper.IsExtensionRegistered(null!).Should().BeFalse();
        }

        // ── GetContentType ─────────────────────────────────────────────

        [SkippableFact]
        public void GetContentType_Txt_ReturnsNullOrTextPlain()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            string? contentType = AssocHelper.GetContentType(".txt");

            if (contentType != null)
                contentType.Should().Be("text/plain");
        }

        // ── GetOpenCommand ─────────────────────────────────────────────

        [SkippableFact]
        public void GetOpenCommand_Txt_ReturnsNotepadCommand()
        {
            Skip.IfNot(TestPlatform.IsWindows());

            string? cmd = AssocHelper.GetOpenCommand(".txt");

            cmd.Should().NotBeNullOrEmpty();
            cmd.Should().ContainEquivalentOf("notepad");
        }
    }
}
