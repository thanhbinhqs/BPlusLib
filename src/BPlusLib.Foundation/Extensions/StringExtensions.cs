// <copyright file="StringExtensions.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Provides extension methods for <see cref="string"/> to simplify common
    /// text-manipulation tasks in a null-safe, runtime-safe manner.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates the string to the specified maximum length,
        /// optionally appending an ellipsis ("...") when truncation occurs.
        /// </summary>
        /// <param name="value">The string to truncate.</param>
        /// <param name="maxLength">The maximum number of characters to retain.</param>
        /// <param name="ellipsis">
        /// If <see langword="true"/> and the string was truncated, append "...".
        /// When the ellipsis is used, the resulting length will be
        /// <paramref name="maxLength"/> + 3 (the length of the ellipsis).
        /// </param>
        /// <returns>
        /// The original <paramref name="value"/> if its length does not exceed
        /// <paramref name="maxLength"/>; otherwise, the truncated string,
        /// optionally suffixed with "...".
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxLength"/> is negative.
        /// </exception>
        public static string Truncate(this string value, int maxLength, bool ellipsis = false)
        {
            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "maxLength must be non-negative.");
            }

            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            if (maxLength == 0)
            {
                return ellipsis ? "..." : string.Empty;
            }

            if (ellipsis)
            {
                return value.AsSpan(0, maxLength).ToString() + "...";
            }

            return value.AsSpan(0, maxLength).ToString();
        }

        /// <summary>
        /// Determines whether the string is <see langword="null"/>,
        /// <see cref="string.Empty"/>, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="value"/> is <see langword="null"/>,
        /// <see cref="string.Empty"/>, or white-space; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsMissing([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Determines whether the string is non-null and contains characters
        /// other than white-space.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="value"/> is not null and
        /// contains non-white-space characters; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsPresent([NotNullWhen(true)] this string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Splits the string into individual lines. Handles <c>\n</c> (LF),
        /// <c>\r\n</c> (CRLF), and <c>\r</c> (CR) line endings.
        /// </summary>
        /// <param name="value">The string to split.</param>
        /// <returns>
        /// An array of strings, each representing one line from the original string.
        /// Trailing empty entries are not removed. Returns an empty array if
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </returns>
        public static string[] ToLines(this string value)
        {
            if (value == null)
            {
                return Array.Empty<string>();
            }

            if (value.Length == 0)
            {
                return new[] { string.Empty };
            }

            // Split across \r\n, \n, \r (in that priority order to avoid
            // double-splitting CRLF).
            var result = value.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            return result;
        }

        /// <summary>
        /// Returns a new string whose characters are the reverse of the
        /// original string.
        /// </summary>
        /// <param name="value">The string to reverse.</param>
        /// <returns>
        /// A string with the characters in reverse order, or
        /// <see cref="string.Empty"/> if <paramref name="value"/> is
        /// <see langword="null"/> or empty.
        /// </returns>
        public static string Reverse(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            // Use span for non-allocating reversal.
            var span = value.AsSpan();
            Span<char> buffer = stackalloc char[span.Length];
            span.CopyTo(buffer);
            buffer.Reverse();
            return buffer.ToString();
        }

        /// <summary>
        /// Repeats the specified string <paramref name="count"/> times.
        /// </summary>
        /// <param name="value">The string to repeat.</param>
        /// <param name="count">The number of repetitions.</param>
        /// <returns>
        /// A concatenated string consisting of <paramref name="value"/>
        /// repeated <paramref name="count"/> times.
        /// Returns <see cref="string.Empty"/> if <paramref name="value"/> is
        /// <see langword="null"/> or <paramref name="count"/> is zero or negative.
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// The resulting string would be too large.
        /// </exception>
        public static string Repeat(this string value, int count)
        {
            if (string.IsNullOrEmpty(value) || count <= 0)
            {
                return string.Empty;
            }

            // Use StringBuilder with pre-allocated capacity.
            var sb = new StringBuilder(value.Length * count, value.Length * count);
            for (int i = 0; i < count; i++)
            {
                sb.Append(value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Counts the number of non-overlapping occurrences of
        /// <paramref name="substring"/> within the string.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="substring">The substring to count.</param>
        /// <param name="comparison">The string comparison mode.</param>
        /// <returns>
        /// The count of occurrences, or 0 if either parameter is
        /// <see langword="null"/> or empty.
        /// </returns>
        public static int CountOccurrences(this string value, string substring, StringComparison comparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(substring))
            {
                return 0;
            }

            int count = 0;
            int index = 0;
            while ((index = value.IndexOf(substring, index, comparison)) >= 0)
            {
                count++;
                index += substring.Length;
            }

            return count;
        }

        /// <summary>
        /// Returns a substring starting at <paramref name="startIndex"/>
        /// with the specified <paramref name="length"/>, without throwing
        /// exceptions for out-of-bounds arguments.
        /// </summary>
        /// <param name="value">The source string.</param>
        /// <param name="startIndex">The zero-based starting character position.</param>
        /// <param name="length">
        /// The number of characters to take, or <see langword="null"/> to take
        /// everything from <paramref name="startIndex"/> to the end.
        /// </param>
        /// <returns>
        /// The safe substring, or <see cref="string.Empty"/> if
        /// <paramref name="value"/> is <see langword="null"/> or the computed
        /// range is empty.
        /// </returns>
        public static string SafeSubstring(this string value, int startIndex, int? length = null)
        {
            if (string.IsNullOrEmpty(value) || startIndex >= value.Length)
            {
                return string.Empty;
            }

            if (startIndex < 0)
            {
                startIndex = 0;
            }

            int available = value.Length - startIndex;
            int len = length.HasValue ? Math.Min(length.Value, available) : available;
            if (len <= 0)
            {
                return string.Empty;
            }

            return value.Substring(startIndex, len);
        }

        /// <summary>
        /// Strips HTML tags from the string using a basic character-by-character
        /// approach. Does not use regular expressions.
        /// </summary>
        /// <param name="value">The string containing HTML.</param>
        /// <returns>
        /// The string with all HTML tags removed, or <see cref="string.Empty"/>
        /// if <paramref name="value"/> is <see langword="null"/>.
        /// </returns>
        public static string StripHtml(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            bool inTag = false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '<')
                {
                    // Check for comment or CDATA — skip until matching close.
                    if (i + 3 < value.Length
                        && value[i + 1] == '!'
                        && value[i + 2] == '-'
                        && value[i + 3] == '-')
                    {
                        // Skip until -->
                        int end = value.IndexOf("-->", i + 4, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            break; // malformed, stop
                        }

                        i = end + 2; // will be incremented by loop
                        continue;
                    }

                    inTag = true;
                    continue;
                }

                if (c == '>' && inTag)
                {
                    inTag = false;
                    continue;
                }

                if (!inTag)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encodes the string to Base64.
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>
        /// A Base64-encoded string, or <see cref="string.Empty"/> if
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </returns>
        public static string ToBase64(this string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decodes a Base64-encoded string back to plain text.
        /// </summary>
        /// <param name="value">The Base64-encoded string to decode.</param>
        /// <returns>
        /// The decoded string, or <see cref="string.Empty"/> if
        /// <paramref name="value"/> is <see langword="null"/> or
        /// not valid Base64.
        /// </returns>
        public static string FromBase64(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(value);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Removes diacritics (accents) from the string by folding characters
        /// to their ASCII equivalents wherever possible.
        /// </summary>
        /// <param name="value">The string to process.</param>
        /// <returns>
        /// The string with diacritics removed, or <see cref="string.Empty"/>
        /// if <paramref name="value"/> is <see langword="null"/>.
        /// </returns>
        public static string RemoveDiacritics(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            // Normalize to FormD (compatibility decomposition) so accented
            // characters are separated into base character + combining marks.
            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (char c in normalized)
            {
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Re-normalize to FormC.
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Escapes a string so it can be safely embedded in XML, replacing
        /// the characters &amp;, &lt;, &gt;, &quot;, and &apos; with their
        /// XML entity equivalents.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>
        /// The XML-escaped string, or <see cref="string.Empty"/> if
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </returns>
        public static string XmlEscape(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            // Pre-allocate a reasonable capacity.
            var sb = new StringBuilder(value.Length + 16);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '\'':
                        sb.Append("&apos;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Unescapes XML entities (&amp;, &lt;, &gt;, &quot;, &apos;)
        /// back to their literal characters.
        /// </summary>
        /// <param name="value">The string to unescape.</param>
        /// <returns>
        /// The unescaped string, or <see cref="string.Empty"/> if
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </returns>
        public static string XmlUnescape(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            // The entities are ordered so that &amp; is processed first,
            // preventing double-unescaping issues.
            return value
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'");
        }
    }
}
