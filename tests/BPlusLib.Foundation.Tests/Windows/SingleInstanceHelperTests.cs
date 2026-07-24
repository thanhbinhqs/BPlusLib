using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class SingleInstanceHelperTests
    {
        [Fact]
        public void Acquire_ValidName_Succeeds()
        {
            string name = $"BPlusLib_TestSI_{Guid.NewGuid():N}";
            using var guard = SingleInstanceHelper.Acquire(name);
            guard.Should().NotBeNull();
            guard!.IsNewInstance.Should().BeTrue();
        }

        [Fact]
        public void Acquire_SecondInstance_ReturnsNull()
        {
            string name = $"BPlusLib_TestSI2_{Guid.NewGuid():N}";
            using var first = SingleInstanceHelper.Acquire(name);
            first.Should().NotBeNull();

            using var second = SingleInstanceHelper.Acquire(name);
            second.Should().BeNull();
        }

        [Fact]
        public void Acquire_EmptyName_ReturnsNull()
        {
            SingleInstanceHelper.Acquire("").Should().BeNull();
            SingleInstanceHelper.Acquire(null!).Should().BeNull();
        }

        [Fact]
        public void Dispose_ReleasesMutex()
        {
            string name = $"BPlusLib_TestSI3_{Guid.NewGuid():N}";
            var guard = SingleInstanceHelper.Acquire(name);
            guard.Should().NotBeNull();
            guard!.Dispose();

            // After dispose, another instance should succeed
            using var second = SingleInstanceHelper.Acquire(name);
            second.Should().NotBeNull();
        }

        [Fact]
        public void IsAlreadyRunning_ReturnsBool()
        {
            // No other instance running for this unique name
            string name = $"BPlusLib_TestSIR_{Guid.NewGuid():N}";
            SingleInstanceHelper.IsAlreadyRunning(name).Should().BeFalse();
        }

        [Fact]
        public void DoubleDispose_NoException()
        {
            string name = $"BPlusLib_TestSI4_{Guid.NewGuid():N}";
            var guard = SingleInstanceHelper.Acquire(name);
            guard.Should().NotBeNull();
            guard!.Dispose();
            guard.Dispose(); // Should not throw
        }
    }
}
