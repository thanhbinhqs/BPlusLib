// <copyright file="ClipboardHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Clipboard;

namespace BPlusLib.Foundation.Tests.Clipboard
{
    [Trait("Category", "Clipboard")]
    public sealed class ClipboardHelperTests
    {
        // ── ClipboardFormat enum values ────────────────────────

        [Fact]
        public void ClipboardFormat_Values_AreCorrect()
        {
            ((ushort)ClipboardFormat.CF_TEXT).Should().Be(1);
            ((ushort)ClipboardFormat.CF_BITMAP).Should().Be(2);
            ((ushort)ClipboardFormat.CF_UNICODETEXT).Should().Be(13);
            ((ushort)ClipboardFormat.CF_HDROP).Should().Be(15);
        }

        // ── TrySetText ─────────────────────────────────────────

        [Fact]
        public void TrySetText_Null_ReturnsFalse()
        {
            bool result = ClipboardHelper.TrySetText(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void TrySetText_Empty_ReturnsFalse()
        {
            bool result = ClipboardHelper.TrySetText(string.Empty);
            result.Should().BeFalse();
        }

        // ── TryGetText ─────────────────────────────────────────

        [Fact]
        public void TryGetText_OnLinux_ReturnsNull()
        {
            // On Linux the P/Invoke calls to user32.dll will fail,
            // so TryGetText should gracefully return null.
            string? result = ClipboardHelper.TryGetText();
            result.Should().BeNull();
        }

        // ── TrySetFiles ────────────────────────────────────────

        [Fact]
        public void TrySetFiles_Null_ReturnsFalse()
        {
            bool result = ClipboardHelper.TrySetFiles(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void TrySetFiles_Empty_ReturnsFalse()
        {
            bool result = ClipboardHelper.TrySetFiles(Array.Empty<string>());
            result.Should().BeFalse();
        }

        // ── TryGetFiles ────────────────────────────────────────

        [Fact]
        public void TryGetFiles_OnLinux_ReturnsNull()
        {
            string[]? result = ClipboardHelper.TryGetFiles();
            result.Should().BeNull();
        }

        // ── TrySetImage ────────────────────────────────────────

        [Fact]
        public void TrySetImage_OnLinux_ReturnsFalse()
        {
            // On Linux, TrySetImage will either hit DllNotFoundException
            // from user32.dll or the non-net472 code path returning false.
            bool result = ClipboardHelper.TrySetImage("somefile.png");
            result.Should().BeFalse();
        }

        [Fact]
        public void TrySetImage_NullPath_ReturnsFalse()
        {
            bool result = ClipboardHelper.TrySetImage(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void TrySetImage_EmptyPath_ReturnsFalse()
        {
            bool result = ClipboardHelper.TrySetImage(string.Empty);
            result.Should().BeFalse();
        }

        // ── Clear ──────────────────────────────────────────────

        [Fact]
        public void Clear_OnLinux_ReturnsFalse()
        {
            bool result = ClipboardHelper.Clear();
            result.Should().BeFalse();
        }

        // ── ContainsText ───────────────────────────────────────

        [Fact]
        public void ContainsText_OnLinux_ReturnsFalse()
        {
            bool result = ClipboardHelper.ContainsText();
            result.Should().BeFalse();
        }

        // ── ContainsFiles ──────────────────────────────────────

        [Fact]
        public void ContainsFiles_OnLinux_ReturnsFalse()
        {
            bool result = ClipboardHelper.ContainsFiles();
            result.Should().BeFalse();
        }

        // ── ContainsImage ──────────────────────────────────────

        [Fact]
        public void ContainsImage_OnLinux_ReturnsFalse()
        {
            bool result = ClipboardHelper.ContainsImage();
            result.Should().BeFalse();
        }

        // ── GetAvailableFormats ────────────────────────────────

        [Fact]
        public void GetAvailableFormats_OnLinux_ReturnsEmpty()
        {
            ClipboardFormat[] formats = ClipboardHelper.GetAvailableFormats();
            formats.Should().BeEmpty();
        }

        // ── GetFormatNames ─────────────────────────────────────

        [Fact]
        public void GetFormatNames_OnLinux_ReturnsEmpty()
        {
            var names = ClipboardHelper.GetFormatNames();
            names.Should().BeEmpty();
        }
    }
}
