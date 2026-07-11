using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Shell;

namespace BPlusLib.Foundation.Tests.Shell
{
    [Trait("Category", "Shell")]
    public sealed class NotifyIconHelperTests
    {
        [SkippableFact]
        public void Create_NullWindow_Throws()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            Action act = () => NotifyIconHelper.Create(IntPtr.Zero, 0x8000, new IntPtr(1));
            act.Should().Throw<ArgumentException>();
        }

        [SkippableFact]
        public void Create_NullIcon_Throws()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            Action act = () => NotifyIconHelper.Create(new IntPtr(0x12345), 0x8000, IntPtr.Zero);
            act.Should().Throw<ArgumentException>();
        }

        [SkippableFact]
        public void Create_CreatesInstance()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var icon = NotifyIconHelper.Create(new IntPtr(0x12345), 0x8000, new IntPtr(0x123), 42, "test");
            icon.Should().NotBeNull();
            icon.Dispose();
        }

        [SkippableFact]
        public void Dispose_Idempotent()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var icon = NotifyIconHelper.Create(new IntPtr(0x12345), 0x8000, new IntPtr(0x123));
            icon.Dispose();
            icon.Dispose(); // Should not throw
        }
    }
}
