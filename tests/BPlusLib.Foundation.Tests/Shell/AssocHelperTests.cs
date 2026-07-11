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
            Skip.IfNot(OperatingSystem.IsWindows());

            string? desc = AssocHelper.GetFileTypeDescription(".txt");

            desc.Should().NotBeNullOrEmpty();
            desc.Should().Contain("Text");
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
        public void GetAssociatedExecutable_Txt_ReturnsNotepad()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string? exe = AssocHelper.GetAssociatedExecutable(".txt");

            exe.Should().NotBeNullOrEmpty();
            exe.Should().Contain("notepad");
        }

        // ── GetProgId ──────────────────────────────────────────────────

        [SkippableFact]
        public void GetProgId_Txt_ReturnsTxtfile()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string? progId = AssocHelper.GetProgId(".txt");

            progId.Should().NotBeNullOrEmpty();
            progId.Should().Be("txtfile");
        }

        // ── IsExtensionRegistered ──────────────────────────────────────

        [SkippableFact]
        public void IsExtensionRegistered_Txt_ReturnsTrue()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool registered = AssocHelper.IsExtensionRegistered(".txt");

            registered.Should().BeTrue();
        }

        [Fact]
        public void IsExtensionRegistered_Unknown_ReturnsFalse()
        {
            // On Windows this queries .nonexistent_xyz; on Linux it's always false
            bool registered = AssocHelper.IsExtensionRegistered(".nonexistent_xyz");

            registered.Should().BeFalse();
        }

        [Fact]
        public void IsExtensionRegistered_Null_ReturnsFalse()
        {
            AssocHelper.IsExtensionRegistered(null!).Should().BeFalse();
        }

        // ── GetContentType ─────────────────────────────────────────────

        [SkippableFact]
        public void GetContentType_Txt_ReturnsTextPlain()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string? contentType = AssocHelper.GetContentType(".txt");

            contentType.Should().NotBeNullOrEmpty();
            contentType.Should().Be("text/plain");
        }

        // ── GetOpenCommand ─────────────────────────────────────────────

        [SkippableFact]
        public void GetOpenCommand_Txt_ReturnsNotepadCommand()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string? cmd = AssocHelper.GetOpenCommand(".txt");

            cmd.Should().NotBeNullOrEmpty();
            cmd.Should().Contain("NOTEPAD");
        }
    }
}
