// <copyright file="WinTrustHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Security;

namespace BPlusLib.Foundation.Tests.Security
{
    [Trait("Category", "Security")]
    public sealed class WinTrustHelperTests
    {
        [SkippableFact]
        public void Verify_NullPath_ReturnsUnsigned()
        {
            var info = WinTrustHelper.Verify(null!);
            info.Should().NotBeNull();
            info.IsSigned.Should().BeFalse();
        }

        [SkippableFact]
        public void Verify_NonExistent_ReturnsUnsigned()
        {
            var info = WinTrustHelper.Verify(@"C:\NONEXISTENT_FILE_XYZ123.dll");
            info.Should().NotBeNull();
            info.IsSigned.Should().BeFalse();
        }

        [SkippableFact]
        public void IsSigned_Kernel32_ReturnsTrue()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var result = WinTrustHelper.IsSigned(@"C:\Windows\System32\kernel32.dll");
            result.Should().BeTrue();
        }

        [SkippableFact]
        public void GetPublisher_Kernel32_ReturnsMicrosoft()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            var publisher = WinTrustHelper.GetPublisher(@"C:\Windows\System32\kernel32.dll");
            publisher.Should().NotBeNullOrEmpty();
            publisher.Should().Contain("Microsoft");
        }

        [SkippableFact]
        public void Verify_UnsignedFile_ReturnsUntrusted()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            string tempFile = System.IO.Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(tempFile, "not a PE file");
                var info = WinTrustHelper.Verify(tempFile);
                info.IsSigned.Should().BeFalse();
                info.TrustLevel.Should().Be(TrustLevel.Untrusted);
            }
            finally
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }
}
