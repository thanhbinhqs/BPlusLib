// <copyright file="DpiScaleTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Window;

namespace BPlusLib.Foundation.Tests.Window
{
    [Trait("Category", "Window")]
    public sealed class DpiScaleTests
    {
        [Fact]
        public void Scale_IsAverageOfXAndY()
        {
            var dpi = new DpiScale(1.0f, 2.0f);
            dpi.Scale.Should().Be(1.5f);
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            var a = new DpiScale(1.5f, 1.5f);
            var b = new DpiScale(1.5f, 1.5f);
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Inequality_DifferentValues_AreNotEqual()
        {
            var a = new DpiScale(1.0f, 2.0f);
            var b = new DpiScale(2.0f, 1.0f);
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void ZeroValues_AreValid()
        {
            var dpi = new DpiScale(0f, 0f);
            dpi.X.Should().Be(0f);
            dpi.Y.Should().Be(0f);
            dpi.Scale.Should().Be(0f);
        }
    }
}
