// <copyright file="CredentialHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Security;

namespace BPlusLib.Foundation.Tests.Security
{
    [Trait("Category", "Security")]
    public sealed class CredentialHelperTests
    {
        /// <summary>
        /// Writes a credential, reads it back, verifies the values, then deletes it.
        /// </summary>
        [SkippableFact]
        public void WriteReadDelete_Roundtrips()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string targetName = "BPlusLib.Test.Roundtrip." + Guid.NewGuid().ToString("N");
            const string userName = "testuser";
            const string password = "testP@ssw0rd!";
            const string comment = "Test credential for roundtrip";

            // Write
            bool written = CredentialHelper.Write(targetName, userName, password, comment: comment);
            written.Should().BeTrue("Write should succeed on Windows");

            try
            {
                // Read
                var entry = CredentialHelper.Read(targetName);
                entry.Should().NotBeNull("Read should find the credential we just wrote");
                entry!.TargetName.Should().Be(targetName);
                entry.UserName.Should().Be(userName);
                entry.Password.Should().Be(password);
                entry.Comment.Should().Be(comment);
                entry.Type.Should().Be(CredentialType.Generic);
                entry.Persist.Should().Be(CredentialPersistence.LocalMachine);
            }
            finally
            {
                // Delete
                bool deleted = CredentialHelper.Delete(targetName);
                deleted.Should().BeTrue("Delete should succeed");

                // Verify deletion
                var afterDelete = CredentialHelper.Read(targetName);
                afterDelete.Should().BeNull("Credential should no longer exist after delete");
            }
        }

        /// <summary>
        /// Reading a non-existent credential returns null.
        /// </summary>
        [SkippableFact]
        public void Read_NonExistent_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string targetName = "BPlusLib.Test.NonExistent." + Guid.NewGuid().ToString("N");
            var entry = CredentialHelper.Read(targetName);
            entry.Should().BeNull();
        }

        /// <summary>
        /// Deleting a non-existent credential returns false.
        /// </summary>
        [SkippableFact]
        public void Delete_NonExistent_ReturnsFalse()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            string targetName = "BPlusLib.Test.DeleteNonExistent." + Guid.NewGuid().ToString("N");
            bool deleted = CredentialHelper.Delete(targetName);
            deleted.Should().BeFalse();
        }

        /// <summary>
        /// Enumerate does not throw and returns a list (possibly empty).
        /// </summary>
        [SkippableFact]
        public void Enumerate_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var entries = CredentialHelper.Enumerate();
            entries.Should().NotBeNull();
        }

        /// <summary>
        /// Write with null/empty target name returns false.
        /// </summary>
        [SkippableFact]
        public void Write_NullOrEmptyTarget_ReturnsFalse()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool resultNull = CredentialHelper.Write(null!, "user", "pass");
            resultNull.Should().BeFalse();

            bool resultEmpty = CredentialHelper.Write(string.Empty, "user", "pass");
            resultEmpty.Should().BeFalse();
        }
    }
}
