using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class FileAssociationHelperTests
    {
        [Fact]
        public void Register_NullAssociation_ReturnsFalse()
        {
            FileAssociationHelper.Register(null!).Should().BeFalse();
        }

        [Fact]
        public void IsRegistered_NonExistentExtension_ReturnsFalse()
        {
            FileAssociationHelper.IsRegistered(".bpluslib_nonexistent_ext_12345").Should().BeFalse();
        }

        [Fact]
        public void GetAssociation_NonExistentExtension_ReturnsNull()
        {
            FileAssociationHelper.GetAssociation(".bpluslib_nonexistent_ext_12345").Should().BeNull();
        }

        [Fact]
        public void Unregister_EmptyExtension_ReturnsFalse()
        {
            FileAssociationHelper.Unregister("", "SomeProgId").Should().BeFalse();
        }

        [Fact]
        public void FileAssociation_DefaultValues_AreCorrect()
        {
            var assoc = new FileAssociation();
            assoc.Extension.Should().Be(".txt");
            assoc.ProgId.Should().BeEmpty();
            assoc.Description.Should().BeEmpty();
            assoc.ExecutablePath.Should().BeEmpty();
            assoc.IconPath.Should().BeEmpty();
            assoc.IconIndex.Should().Be(0);
        }
    }
}
