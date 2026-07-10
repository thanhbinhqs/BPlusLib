// <copyright file="StringExtensionsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Extensions;

namespace BPlusLib.Foundation.Tests.Extensions
{
    [Trait("Category", "Extensions")]
    public sealed class StringExtensionsTests
    {
        // ── Truncate ────────────────────────────────────────────────────────

        [Fact]
        public void Truncate_ShorterThanMax_ReturnsOriginal()
        {
            "Hello".Truncate(10).Should().Be("Hello");
        }

        [Fact]
        public void Truncate_EqualToMax_ReturnsOriginal()
        {
            "Hello".Truncate(5).Should().Be("Hello");
        }

        [Fact]
        public void Truncate_LongerThanMax_Truncates()
        {
            "Hello World".Truncate(5).Should().Be("Hello");
        }

        [Fact]
        public void Truncate_WithEllipsis_AppendsEllipsis()
        {
            "Hello World".Truncate(5, ellipsis: true).Should().Be("Hello...");
        }

        [Fact]
        public void Truncate_Null_ReturnsEmpty()
        {
            string? s = null;
            s.Truncate(5).Should().Be(string.Empty);
        }

        [Fact]
        public void Truncate_Empty_ReturnsEmpty()
        {
            string.Empty.Truncate(5).Should().Be(string.Empty);
        }

        [Fact]
        public void Truncate_ZeroMaxLength_ReturnsEmpty()
        {
            "Hello".Truncate(0).Should().Be(string.Empty);
        }

        [Fact]
        public void Truncate_ZeroMaxLengthWithEllipsis_ReturnsEllipsis()
        {
            "Hello".Truncate(0, ellipsis: true).Should().Be("...");
        }

        [Fact]
        public void Truncate_NegativeMaxLength_ShouldThrow()
        {
            Action act = () => "Hello".Truncate(-1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // ── IsMissing ───────────────────────────────────────────────────────

        [Fact]
        public void IsMissing_Null_ReturnsTrue()
        {
            string? s = null;
            s.IsMissing().Should().BeTrue();
        }

        [Fact]
        public void IsMissing_Empty_ReturnsTrue()
        {
            string.Empty.IsMissing().Should().BeTrue();
        }

        [Fact]
        public void IsMissing_Whitespace_ReturnsTrue()
        {
            "   ".IsMissing().Should().BeTrue();
        }

        [Fact]
        public void IsMissing_ValidString_ReturnsFalse()
        {
            "hello".IsMissing().Should().BeFalse();
        }

        // ── IsPresent ───────────────────────────────────────────────────────

        [Fact]
        public void IsPresent_Null_ReturnsFalse()
        {
            string? s = null;
            s.IsPresent().Should().BeFalse();
        }

        [Fact]
        public void IsPresent_Empty_ReturnsFalse()
        {
            string.Empty.IsPresent().Should().BeFalse();
        }

        [Fact]
        public void IsPresent_Whitespace_ReturnsFalse()
        {
            "   ".IsPresent().Should().BeFalse();
        }

        [Fact]
        public void IsPresent_ValidString_ReturnsTrue()
        {
            "hello".IsPresent().Should().BeTrue();
        }

        // ── ToLines ─────────────────────────────────────────────────────────

        [Fact]
        public void ToLines_MixedLineEndings_Splits()
        {
            string input = "line1\r\nline2\nline3\rline4";
            string[] lines = input.ToLines();
            lines.Should().HaveCount(4);
            lines[0].Should().Be("line1");
            lines[1].Should().Be("line2");
            lines[2].Should().Be("line3");
            lines[3].Should().Be("line4");
        }

        [Fact]
        public void ToLines_Null_ReturnsEmpty()
        {
            string? s = null;
            s.ToLines().Should().BeEmpty();
        }

        [Fact]
        public void ToLines_Empty_ReturnsSingleEmptyString()
        {
            string.Empty.ToLines().Should().BeEquivalentTo(new[] { string.Empty });
        }

        [Fact]
        public void ToLines_NoLineBreaks_ReturnsSingleLine()
        {
            "single line".ToLines().Should().BeEquivalentTo(new[] { "single line" });
        }

        // ── Reverse ─────────────────────────────────────────────────────────

        [Fact]
        public void Reverse_Reverses()
        {
            "Hello".Reverse().Should().Be("olleH");
        }

        [Fact]
        public void Reverse_Null_ReturnsEmpty()
        {
            string? s = null;
            s.Reverse().Should().Be(string.Empty);
        }

        [Fact]
        public void Reverse_Empty_ReturnsEmpty()
        {
            string.Empty.Reverse().Should().Be(string.Empty);
        }

        [Fact]
        public void Reverse_SingleChar_ReturnsSame()
        {
            "A".Reverse().Should().Be("A");
        }

        // ── Repeat ──────────────────────────────────────────────────────────

        [Fact]
        public void Repeat_ZeroTimes_ReturnsEmpty()
        {
            "Hi".Repeat(0).Should().Be(string.Empty);
        }

        [Fact]
        public void Repeat_NegativeTimes_ReturnsEmpty()
        {
            "Hi".Repeat(-1).Should().Be(string.Empty);
        }

        [Fact]
        public void Repeat_MultipleTimes_Repeats()
        {
            "AB".Repeat(3).Should().Be("ABABAB");
        }

        [Fact]
        public void Repeat_Null_ReturnsEmpty()
        {
            string? s = null;
            s.Repeat(3).Should().Be(string.Empty);
        }

        [Fact]
        public void Repeat_Empty_ReturnsEmpty()
        {
            string.Empty.Repeat(5).Should().Be(string.Empty);
        }

        // ── CountOccurrences ────────────────────────────────────────────────

        [Fact]
        public void CountOccurrences_FindsAll()
        {
            "ababab".CountOccurrences("ab").Should().Be(3);
        }

        [Fact]
        public void CountOccurrences_NoMatch_ReturnsZero()
        {
            "hello".CountOccurrences("xyz").Should().Be(0);
        }

        [Fact]
        public void CountOccurrences_NullValue_ReturnsZero()
        {
            string? s = null;
            s.CountOccurrences("a").Should().Be(0);
        }

        [Fact]
        public void CountOccurrences_NullSubstring_ReturnsZero()
        {
            "hello".CountOccurrences(null!).Should().Be(0);
        }

        [Fact]
        public void CountOccurrences_EmptySubstring_ReturnsZero()
        {
            "hello".CountOccurrences(string.Empty).Should().Be(0);
        }

        [Fact]
        public void CountOccurrences_CaseSensitive_RespectsCase()
        {
            "AaAaA".CountOccurrences("a", StringComparison.Ordinal).Should().Be(2);
        }

        [Fact]
        public void CountOccurrences_CaseInsensitive_FindsAll()
        {
            "AaAaA".CountOccurrences("a", StringComparison.OrdinalIgnoreCase).Should().Be(5);
        }

        // ── SafeSubstring ───────────────────────────────────────────────────

        [Fact]
        public void SafeSubstring_NegativeStart_ReturnsFromStart()
        {
            "Hello".SafeSubstring(-2, 3).Should().Be("Hel");
        }

        [Fact]
        public void SafeSubstring_PastEnd_ReturnsEmpty()
        {
            "Hi".SafeSubstring(10).Should().Be(string.Empty);
        }

        [Fact]
        public void SafeSubstring_Normal_ReturnsSubstring()
        {
            "Hello World".SafeSubstring(6, 5).Should().Be("World");
        }

        [Fact]
        public void SafeSubstring_Null_ReturnsEmpty()
        {
            string? s = null;
            s.SafeSubstring(0).Should().Be(string.Empty);
        }

        [Fact]
        public void SafeSubstring_Empty_ReturnsEmpty()
        {
            string.Empty.SafeSubstring(0).Should().Be(string.Empty);
        }

        [Fact]
        public void SafeSubstring_WithoutLength_ReturnsToEnd()
        {
            "Hello".SafeSubstring(2).Should().Be("llo");
        }

        [Fact]
        public void SafeSubstring_NegativeStartWithoutLength_ReturnsFromStart()
        {
            "Hello".SafeSubstring(-1).Should().Be("Hello");
        }

        [Fact]
        public void SafeSubstring_LengthBeyondEnd_Clamps()
        {
            "Hi".SafeSubstring(0, 100).Should().Be("Hi");
        }

        // ── StripHtml ───────────────────────────────────────────────────────

        [Fact]
        public void StripHtml_RemovesTags()
        {
            "<p>Hello <b>World</b></p>".StripHtml().Should().Be("Hello World");
        }

        [Fact]
        public void StripHtml_Null_ReturnsEmpty()
        {
            string? s = null;
            s.StripHtml().Should().Be(string.Empty);
        }

        [Fact]
        public void StripHtml_Empty_ReturnsEmpty()
        {
            string.Empty.StripHtml().Should().Be(string.Empty);
        }

        [Fact]
        public void StripHtml_NoTags_ReturnsOriginal()
        {
            "Hello World".StripHtml().Should().Be("Hello World");
        }

        [Fact]
        public void StripHtml_WithComments_RemovesComments()
        {
            "Hello<!-- comment -->World".StripHtml().Should().Be("HelloWorld");
        }

        // ── ToBase64 / FromBase64 ───────────────────────────────────────────

        [Fact]
        public void ToBase64_RoundTrips()
        {
            string original = "Hello World!";
            string encoded = original.ToBase64();
            encoded.Should().NotBeNullOrEmpty();
            string decoded = encoded.FromBase64();
            decoded.Should().Be(original);
        }

        [Fact]
        public void ToBase64_Null_ReturnsEmpty()
        {
            string? s = null;
            s.ToBase64().Should().Be(string.Empty);
        }

        [Fact]
        public void FromBase64_Invalid_ReturnsEmpty()
        {
            "not-valid-base64!!".FromBase64().Should().Be(string.Empty);
        }

        [Fact]
        public void FromBase64_Null_ReturnsEmpty()
        {
            string? s = null;
            s.FromBase64().Should().Be(string.Empty);
        }

        [Fact]
        public void FromBase64_Empty_ReturnsEmpty()
        {
            string.Empty.FromBase64().Should().Be(string.Empty);
        }

        // ── RemoveDiacritics ────────────────────────────────────────────────

        [Fact]
        public void RemoveDiacritics_RemovesAccents()
        {
            "Café résumé naïve".RemoveDiacritics().Should().Be("Cafe resume naive");
        }

        [Fact]
        public void RemoveDiacritics_Null_ReturnsEmpty()
        {
            string? s = null;
            s.RemoveDiacritics().Should().Be(string.Empty);
        }

        [Fact]
        public void RemoveDiacritics_Empty_ReturnsEmpty()
        {
            string.Empty.RemoveDiacritics().Should().Be(string.Empty);
        }

        [Fact]
        public void RemoveDiacritics_NoAccents_ReturnsOriginal()
        {
            "Hello".RemoveDiacritics().Should().Be("Hello");
        }

        // ── XmlEscape / XmlUnescape ─────────────────────────────────────────

        [Fact]
        public void XmlEscape_EncodesSpecialChars()
        {
            "<hello & world>\"it's\"".XmlEscape().Should().Be("&lt;hello &amp; world&gt;&quot;it&apos;s&quot;");
        }

        [Fact]
        public void XmlEscape_Null_ReturnsEmpty()
        {
            string? s = null;
            s.XmlEscape().Should().Be(string.Empty);
        }

        [Fact]
        public void XmlEscape_Empty_ReturnsEmpty()
        {
            string.Empty.XmlEscape().Should().Be(string.Empty);
        }

        [Fact]
        public void XmlEscape_NoSpecialChars_ReturnsOriginal()
        {
            "Hello World".XmlEscape().Should().Be("Hello World");
        }

        [Fact]
        public void XmlUnescape_DecodesEntities()
        {
            "&lt;hello &amp; world&gt;".XmlUnescape().Should().Be("<hello & world>");
        }

        [Fact]
        public void XmlUnescape_QuotesAndApos_Decodes()
        {
            "&quot;it&apos;s&quot;".XmlUnescape().Should().Be("\"it's\"");
        }

        [Fact]
        public void XmlUnescape_Null_ReturnsEmpty()
        {
            string? s = null;
            s.XmlUnescape().Should().Be(string.Empty);
        }

        [Fact]
        public void XmlUnescape_Empty_ReturnsEmpty()
        {
            string.Empty.XmlUnescape().Should().Be(string.Empty);
        }

        [Fact]
        public void XmlUnescape_RoundTrip_Works()
        {
            string original = "<hello & world>\"it's\"";
            string escaped = original.XmlEscape();
            string unescaped = escaped.XmlUnescape();
            unescaped.Should().Be(original);
        }
    }
}
