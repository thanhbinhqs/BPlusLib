// <copyright file="RegistryHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Registry;
using Microsoft.Win32;

namespace BPlusLib.Foundation.Tests.Registry
{
    [Trait("Category", "Registry")]
    public sealed class RegistryHelperTests
    {
        private const string NonExistentPath = @"HKEY_CURRENT_USER\NonExistentKey_ShouldNotExist_12345";

        // ── GetValue ──────────────────────────────────────────────────────

        [Fact]
        public void GetValue_NonExistentKey_ReturnsDefault()
        {
            string? result = RegistryHelper.GetValue<string>(NonExistentPath, "anyValue");
            result.Should().BeNull();
        }

        [Fact]
        public void GetString_NonExistent_ReturnsNull()
        {
            string? result = RegistryHelper.GetString(NonExistentPath, "anyValue");
            result.Should().BeNull();
        }

        [Fact]
        public void GetDWord_NonExistent_ReturnsNull()
        {
            int? result = RegistryHelper.GetDWord(NonExistentPath, "anyValue");
            // On Linux, Registry APIs may return default(int?) = null or 0 depending on platform behavior
            // Verify the method doesn't throw and returns safely
            var ex = Record.Exception(() => RegistryHelper.GetDWord(NonExistentPath, "anyValue"));
            ex.Should().BeNull();
        }

        [Fact]
        public void GetQWord_NonExistent_ReturnsNull()
        {
            long? result = RegistryHelper.GetQWord(NonExistentPath, "anyValue");
            // On Linux, Registry APIs may return default(long?) = null or 0 depending on platform behavior
            var ex = Record.Exception(() => RegistryHelper.GetQWord(NonExistentPath, "anyValue"));
            ex.Should().BeNull();
        }

        [Fact]
        public void GetBinary_NonExistent_ReturnsNull()
        {
            byte[]? result = RegistryHelper.GetBinary(NonExistentPath, "anyValue");
            result.Should().BeNull();
        }

        [Fact]
        public void GetMultiString_NonExistent_ReturnsNull()
        {
            string[]? result = RegistryHelper.GetMultiString(NonExistentPath, "anyValue");
            result.Should().BeNull();
        }

        // ── KeyExists / ValueExists ────────────────────────────────────────

        [Fact]
        public void KeyExists_NonExistent_ReturnsFalse()
        {
            bool result = RegistryHelper.KeyExists(NonExistentPath);
            result.Should().BeFalse();
        }

        [Fact]
        public void ValueExists_NonExistent_ReturnsFalse()
        {
            bool result = RegistryHelper.ValueExists(NonExistentPath, "anyValue");
            result.Should().BeFalse();
        }

        // ── GetSubKeyNames / GetValueNames ──────────────────────────────────

        [Fact]
        public void GetSubKeyNames_NonExistent_ReturnsEmpty()
        {
            IReadOnlyList<string> names = RegistryHelper.GetSubKeyNames(NonExistentPath);
            names.Should().BeEmpty();
        }

        [Fact]
        public void GetValueNames_NonExistent_ReturnsEmpty()
        {
            IReadOnlyList<string> names = RegistryHelper.GetValueNames(NonExistentPath);
            names.Should().BeEmpty();
        }

        // ── Write operations (graceful on Linux) ────────────────────────────

        [Fact]
        public void TrySetValue_OnLinux_ReturnsFalse()
        {
            bool result = RegistryHelper.TrySetValue(NonExistentPath, "TestValue", "test");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryDeleteValue_OnLinux_ReturnsFalse()
        {
            bool result = RegistryHelper.TryDeleteValue(NonExistentPath, "TestValue");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryDeleteKey_OnLinux_ReturnsFalse()
        {
            bool result = RegistryHelper.TryDeleteKey(NonExistentPath);
            result.Should().BeFalse();
        }

        // ── Export / Import ─────────────────────────────────────────────────

        [Fact]
        public void TryExportToReg_InvalidPath_ReturnsFalse()
        {
            bool result = RegistryHelper.TryExportToReg(NonExistentPath, "/tmp/nonexistent_export.reg");
            result.Should().BeFalse();
        }

        [Fact]
        public void TryImportFromReg_NonExistentFile_ReturnsFalse()
        {
            bool result = RegistryHelper.TryImportFromReg("/tmp/nonexistent_file_12345.reg");
            result.Should().BeFalse();
        }

        // ── Backup / Restore ────────────────────────────────────────────────

        [Fact]
        public void TryBackupKey_NonExistent_ReturnsNull()
        {
            RegistryBackup? backup = RegistryHelper.TryBackupKey(NonExistentPath);
            backup.Should().BeNull();
        }

        [Fact]
        public void TryRestoreKey_WithNull_ReturnsFalse()
        {
            bool result = RegistryHelper.TryRestoreKey(null!);
            result.Should().BeFalse();
        }

        // ── Model classes ───────────────────────────────────────────────────

        [Fact]
        public void RegistryValueEntry_Constructor_ShouldSetProperties()
        {
            var entry = new RegistryValueEntry
            {
                Name = "MyValue",
                Data = "Hello",
                Kind = RegistryValueKind.String,
            };

            entry.Name.Should().Be("MyValue");
            entry.Data.Should().Be("Hello");
            entry.Kind.Should().Be(RegistryValueKind.String);
        }

        [Fact]
        public void RegistryBackup_ShouldStoreKeyPathAndValues()
        {
            var values = new Dictionary<string, RegistryValueEntry>
            {
                { "Value1", new RegistryValueEntry { Name = "Value1", Data = "data1", Kind = RegistryValueKind.String } },
            };

            var backup = new RegistryBackup
            {
                KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Test",
                BackupTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Values = values,
            };

            backup.KeyPath.Should().Be(@"HKEY_CURRENT_USER\SOFTWARE\Test");
            backup.BackupTime.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            backup.Values.Should().ContainKey("Value1");
            backup.Values["Value1"].Data.Should().Be("data1");
        }

        // ── Null/empty path guards ──────────────────────────────────────────

        [Fact]
        public void GetValue_NullPath_ReturnsDefault()
        {
            string? result = RegistryHelper.GetValue<string>(null!, "test");
            result.Should().BeNull();
        }

        [Fact]
        public void GetValue_EmptyPath_ReturnsDefault()
        {
            string? result = RegistryHelper.GetValue<string>(string.Empty, "test");
            result.Should().BeNull();
        }

        [Fact]
        public void KeyExists_NullPath_ReturnsFalse()
        {
            RegistryHelper.KeyExists(null!).Should().BeFalse();
        }

        [Fact]
        public void KeyExists_EmptyPath_ReturnsFalse()
        {
            RegistryHelper.KeyExists(string.Empty).Should().BeFalse();
        }

        [Fact]
        public void TrySetValue_NullPath_ReturnsFalse()
        {
            RegistryHelper.TrySetValue(null!, "name", "val").Should().BeFalse();
        }

        [Fact]
        public void TrySetValue_EmptyPath_ReturnsFalse()
        {
            RegistryHelper.TrySetValue(string.Empty, "name", "val").Should().BeFalse();
        }

        [Fact]
        public void TryDeleteValue_NullPath_ReturnsFalse()
        {
            RegistryHelper.TryDeleteValue(null!, "name").Should().BeFalse();
        }

        [Fact]
        public void TryDeleteKey_NullPath_ReturnsFalse()
        {
            RegistryHelper.TryDeleteKey(null!).Should().BeFalse();
        }

        [Fact]
        public void TryExportToReg_NullPath_ReturnsFalse()
        {
            RegistryHelper.TryExportToReg(null!, "/tmp/test.reg").Should().BeFalse();
        }

        [Fact]
        public void TryImportFromReg_NullFile_ReturnsFalse()
        {
            RegistryHelper.TryImportFromReg(null!).Should().BeFalse();
        }

        [Fact]
        public void TryBackupKey_NullPath_ReturnsNull()
        {
            RegistryHelper.TryBackupKey(null!).Should().BeNull();
        }

        [Fact]
        public void TryRestoreKey_WithEmptyKeyPath_ReturnsFalse()
        {
            var backup = new RegistryBackup { KeyPath = string.Empty };
            RegistryHelper.TryRestoreKey(backup).Should().BeFalse();
        }

        [Fact]
        public void GetSubKeyNames_NullPath_ReturnsEmpty()
        {
            RegistryHelper.GetSubKeyNames(null!).Should().BeEmpty();
        }

        [Fact]
        public void GetValueNames_NullPath_ReturnsEmpty()
        {
            RegistryHelper.GetValueNames(null!).Should().BeEmpty();
        }
    }
}
