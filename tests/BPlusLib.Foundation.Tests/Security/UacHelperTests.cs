// <copyright file="UacHelperTests.cs" company="BPlusLib.Foundation.Tests">
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
    public sealed class UacHelperTests
    {
        [SkippableFact]
        public void IsElevated_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = UacHelper.IsElevated();
        }

        [SkippableFact]
        public void GetIntegrityLevel_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var level = UacHelper.GetIntegrityLevel();
            level.Should().NotBe(IntegrityLevel.Unknown);
        }

        [SkippableFact]
        public void IsStandardUser_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = UacHelper.IsStandardUser();
        }

        [SkippableFact]
        public void IsUacEnabled_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            _ = UacHelper.IsUacEnabled();
        }
    }
}
