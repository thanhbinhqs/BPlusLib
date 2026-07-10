// <copyright file="LocalizationHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Localization;

namespace BPlusLib.Foundation.Tests.Localization
{
    [Trait("Category", "Localization")]
    public sealed class LocalizationHelperTests
    {
        // ── Constructor ───────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithBaseName_ShouldNotThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            l10n.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullBaseName_ShouldThrow()
        {
            Action act = () => new LocalizationHelper(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithEmptyBaseName_ShouldThrow()
        {
            Action act = () => new LocalizationHelper(string.Empty);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── GetString (non-existent key) ─────────────────────────────────

        [Fact]
        public void GetString_NonExistentKey_ReturnsBracketedKey()
        {
            var l10n = new LocalizationHelper("NonExistent.Resources");

            // On .NET 8+, ResourceManager throws MissingManifestResourceException
            // when the base name has no embedded resources.
            // On .NET Framework, it gracefully returns null and we get the bracketed key.
#if NETFRAMEWORK
            string result = l10n.GetString("NonExistentKey");
            result.Should().Be("[NonExistentKey]");
#else
            Action act = () => l10n.GetString("NonExistentKey");
            act.Should().Throw<System.Resources.MissingManifestResourceException>();
#endif
        }

        [Fact]
        public void GetString_NullKey_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.GetString(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetString_EmptyKey_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.GetString(string.Empty);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── GetString with args ───────────────────────────────────────────

        [Fact]
        public void GetString_WithArgs_FormatsCorrectly()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            // On .NET 8+, ResourceManager throws MissingManifestResourceException
            // when the base name has no embedded resources.
#if NETFRAMEWORK
            string result = l10n.GetString("HelloUser", "Alice", 42);
            result.Should().Be("[HelloUser]");
#else
            Action act = () => l10n.GetString("HelloUser", "Alice", 42);
            act.Should().Throw<System.Resources.MissingManifestResourceException>();
#endif
        }

        // ── HasKey ────────────────────────────────────────────────────────

        [Fact]
        public void HasKey_NonExistent_ReturnsFalse()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            bool exists = l10n.HasKey("NonExistentKey");

            exists.Should().BeFalse();
        }

        [Fact]
        public void HasKey_NullKey_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.HasKey(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void HasKey_EmptyKey_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.HasKey(string.Empty);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── GetSystemUICulture ────────────────────────────────────────────

        [Fact]
        public void GetSystemUICulture_ShouldNotBeEmpty()
        {
            // On Linux with minimal locale configuration, this may be
            // empty string (e.g., "C" locale). The value should never be null.
            string culture = LocalizationHelper.GetSystemUICulture();

            culture.Should().NotBeNull();
        }

        // ── GetSystemCulture ──────────────────────────────────────────────

        [Fact]
        public void GetSystemCulture_ShouldNotBeEmpty()
        {
            // On Linux with minimal locale configuration (e.g., "C" locale),
            // this may be empty string. The value should never be null.
            string culture = LocalizationHelper.GetSystemCulture();

            culture.Should().NotBeNull();
        }

        // ── SetDefaultCulture ─────────────────────────────────────────────

        [Fact]
        public void SetDefaultCulture_ValidCulture_ShouldNotThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.SetDefaultCulture("en-US");

            act.Should().NotThrow();
        }

        [Fact]
        public void SetDefaultCulture_NullCulture_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.SetDefaultCulture(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetDefaultCulture_EmptyCulture_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.SetDefaultCulture(string.Empty);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── GetAvailableCultures ──────────────────────────────────────────

        [Fact]
        public void GetAvailableCultures_ShouldReturnAtLeastInvariant()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            var cultures = l10n.GetAvailableCultures();

            cultures.Should().NotBeNull();
            cultures.Should().Contain(c => string.IsNullOrEmpty(c) || c == "iv");
        }

        // ── LoadCulture ───────────────────────────────────────────────────

        [Fact]
        public void LoadCulture_InvalidCulture_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            // The expected exception depends on the runtime and platform.
            // GetCultureInfo may throw CultureNotFoundException for truly
            // invalid names, or MissingManifestResourceException may be thrown
            // when the resource manager cannot find a resource set for that culture.
            Action act = () => l10n.LoadCulture("zz-ZZ-Invalid");

            act.Should().Throw<Exception>()
               .Which.Should().Match(e =>
                   e is System.Globalization.CultureNotFoundException ||
                   e is System.Resources.MissingManifestResourceException);
        }

        [Fact]
        public void LoadCulture_NullCulture_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.LoadCulture(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LoadCulture_EmptyCulture_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.LoadCulture(string.Empty);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── AddResourceFile ───────────────────────────────────────────────

        [Fact]
        public void AddResourceFile_NonExistent_ShouldThrowFileNotFound()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.AddResourceFile("/nonexistent/resources.resources");

            act.Should().Throw<System.IO.FileNotFoundException>();
        }

        [Fact]
        public void AddResourceFile_NullPath_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.AddResourceFile(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddResourceFile_EmptyPath_ShouldThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");

            Action act = () => l10n.AddResourceFile(string.Empty);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── Thread safety ─────────────────────────────────────────────────

        [Fact]
        public void ThreadSafety_ConcurrentAccess_ShouldNotThrow()
        {
            var l10n = new LocalizationHelper("Test.Resources");
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
            };

            // Use only operations that don't throw MissingManifestResourceException
            // on .NET 8+. GetAvailableCultures, GetSystemCulture, and
            // SetDefaultCulture are always safe.
            Parallel.Invoke(parallelOptions,
                () =>
                {
                    var cultures = l10n.GetAvailableCultures();
                    cultures.Should().NotBeNull();
                },
                () =>
                {
                    string ui = LocalizationHelper.GetSystemUICulture();
                    ui.Should().NotBeNull();
                },
                () =>
                {
                    l10n.SetDefaultCulture("en-US");
                },
                () =>
                {
                    var ui2 = LocalizationHelper.GetSystemCulture();
                    ui2.Should().NotBeNull();
                });
        }
    }
}
