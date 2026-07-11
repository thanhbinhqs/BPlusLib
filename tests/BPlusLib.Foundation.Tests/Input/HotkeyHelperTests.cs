using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Input;

namespace BPlusLib.Foundation.Tests.Input
{
    [Trait("Category", "Input")]
    public sealed class HotkeyHelperTests
    {
        [SkippableFact]
        public void Register_NullWindow_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var reg = HotkeyRegistration.Register(IntPtr.Zero, 1,
                HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);
            reg.Should().BeNull();
        }

        [SkippableFact]
        public void Register_InvalidId_ReturnsNull()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var reg = HotkeyRegistration.Register(new IntPtr(1), -1,
                HotkeyModifiers.Control, 0x43);
            reg.Should().BeNull();
        }

        [SkippableFact]
        public void RegisterAndDispose_NoException()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            // Without a valid window, RegisterHotKey may fail — just verify no exception
            var reg = HotkeyRegistration.Register(
                new IntPtr(0x12345), 999,
                HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);
            // reg may be null (expected without real window) — dispose safely
            reg?.Dispose();
        }

        [SkippableFact]
        public void DoubleDispose_NoException()
        {
            Skip.IfNot(OperatingSystem.IsWindows());
            var reg = new HotkeyRegistration(IntPtr.Zero, 1);
            reg.Dispose();
            reg.Dispose(); // Should not throw
        }
    }
}
