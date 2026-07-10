// <copyright file="IconExtractorTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Graphics;

namespace BPlusLib.Foundation.Tests.Graphics
{
    [Trait("Category", "Graphics")]
    public sealed class IconExtractorTests
    {
        // ── ExtractIconRaw ───────────────────────────────────────────────

        [Fact]
        public void ExtractIconRaw_NonExistentFile_ReturnsNull()
        {
            // On Linux, ExtractIconExW P/Invoke is not available,
            // so this always returns null.
            // On Windows, the file doesn't exist, so it also returns null.
            var result = IconExtractor.ExtractIconRaw("/nonexistent/file.exe");

            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconRaw_NullPath_ReturnsNull()
        {
            var result = IconExtractor.ExtractIconRaw(null!);

            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconRaw_EmptyPath_ReturnsNull()
        {
            var result = IconExtractor.ExtractIconRaw(string.Empty);

            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconRaw_ZeroSize_ReturnsNull()
        {
            var result = IconExtractor.ExtractIconRaw("test.exe", size: 0);

            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconRaw_NegativeSize_ReturnsNull()
        {
            var result = IconExtractor.ExtractIconRaw("test.exe", size: -1);

            result.Should().BeNull();
        }

        // ── ExtractIconAsPng ──────────────────────────────────────────────

        [Fact]
        public void ExtractIconAsPng_NonExistentFile_ReturnsNull()
        {
            byte[]? png = IconExtractor.ExtractIconAsPng("/nonexistent/file.exe");

            png.Should().BeNull();
        }

        [Fact]
        public void ExtractIconAsPng_NullPath_ReturnsNull()
        {
            byte[]? png = IconExtractor.ExtractIconAsPng(null!);

            png.Should().BeNull();
        }

        // ── TryExtractIcon ────────────────────────────────────────────────

        [Fact]
        public void TryExtractIcon_NonExistentFile_ReturnsFalse()
        {
            bool success = IconExtractor.TryExtractIcon(
                "/nonexistent/file.exe",
                out byte[]? pngData);

            success.Should().BeFalse();
            pngData.Should().BeNull();
        }

        [Fact]
        public void TryExtractIcon_NullPath_ReturnsFalse()
        {
            bool success = IconExtractor.TryExtractIcon(
                null!,
                out byte[]? pngData);

            success.Should().BeFalse();
            pngData.Should().BeNull();
        }

        // ── GetAssociatedIcons ────────────────────────────────────────────

        [Fact]
        public void GetAssociatedIcons_TxtExtension_ShouldReturnOrEmpty()
        {
            // On Linux, returns empty because P/Invoke (SHGetFileInfoW) and
            // registry access are not available.
            var icons = IconExtractor.GetAssociatedIcons(".txt");

            icons.Should().NotBeNull();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                icons.Should().BeEmpty("because registry/Shell32 is Windows-only");
            }
        }

        [Fact]
        public void GetAssociatedIcons_InvalidExtension_ReturnsEmpty()
        {
            var icons = IconExtractor.GetAssociatedIcons(".nonexistent_extension_xyz");

            icons.Should().NotBeNull();
            icons.Should().BeEmpty();
        }

        [Fact]
        public void GetAssociatedIcons_NullExtension_ReturnsEmpty()
        {
            var icons = IconExtractor.GetAssociatedIcons(null!);

            icons.Should().NotBeNull();
            icons.Should().BeEmpty();
        }

        [Fact]
        public void GetAssociatedIcons_EmptyExtension_ReturnsEmpty()
        {
            var icons = IconExtractor.GetAssociatedIcons(string.Empty);

            icons.Should().NotBeNull();
            icons.Should().BeEmpty();
        }

        [Fact]
        public void GetAssociatedIcons_WithoutLeadingDot_ShouldNormalize()
        {
            // The method normalizes by adding a leading dot.
            var icons = IconExtractor.GetAssociatedIcons("txt");

            icons.Should().NotBeNull();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                icons.Should().BeEmpty();
            }
        }
    }
}
