using System;
using System.IO;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Windows;

namespace BPlusLib.Foundation.Tests.Windows
{
    [Trait("Category", "Windows")]
    public sealed class GlobalExceptionHandlerTests
    {
        [Fact]
        public void Instance_IsSingleton()
        {
            var a = GlobalExceptionHandler.Instance;
            var b = GlobalExceptionHandler.Instance;
            a.Should().BeSameAs(b);
        }

        [Fact]
        public void Enable_Disable_Cycle()
        {
            var handler = GlobalExceptionHandler.Instance;
            // Ensure clean state
            handler.Disable();

            handler.Enable().Should().BeTrue();
            handler.IsHandling.Should().BeTrue();

            handler.Disable().Should().BeTrue();
            handler.IsHandling.Should().BeFalse();
        }

        [Fact]
        public void Enable_WhenAlreadyEnabled_ReturnsTrue()
        {
            var handler = GlobalExceptionHandler.Instance;
            handler.Disable();

            handler.Enable().Should().BeTrue();
            handler.Enable().Should().BeTrue(); // idempotent
            handler.IsHandling.Should().BeTrue();

            handler.Disable();
        }

        [Fact]
        public void Disable_WhenAlreadyDisabled_ReturnsTrue()
        {
            var handler = GlobalExceptionHandler.Instance;
            handler.Disable();
            handler.Disable().Should().BeTrue(); // idempotent
        }

        [Fact]
        public void CreateCrashReport_ReturnsValidReport()
        {
            var ex = new InvalidOperationException("test error");
            var report = GlobalExceptionHandler.CreateCrashReport(ex);
            report.Should().NotBeNull();
            report.ExceptionType.Should().Contain("InvalidOperationException");
            report.Message.Should().Be("test error");
            report.SystemInfo.Should().ContainKey("OS");
        }

        [Fact]
        public void SaveCrashReport_ValidPath_Succeeds()
        {
            var report = GlobalExceptionHandler.CreateCrashReport(new Exception("test"));
            string path = Path.Combine(Path.GetTempPath(), $"crash_test_{Guid.NewGuid():N}.txt");
            try
            {
                GlobalExceptionHandler.SaveCrashReport(report, path).Should().BeTrue();
                File.Exists(path).Should().BeTrue();
                File.ReadAllText(path).Should().Contain("CRASH REPORT");
            }
            finally { try { File.Delete(path); } catch { } }
        }

        [Fact]
        public void SaveCrashReport_NullReport_ReturnsFalse()
        {
            GlobalExceptionHandler.SaveCrashReport(null!, "/tmp/test.txt").Should().BeFalse();
        }

        [Fact]
        public void SaveCrashReport_EmptyPath_ReturnsFalse()
        {
            var report = GlobalExceptionHandler.CreateCrashReport(new Exception("test"));
            GlobalExceptionHandler.SaveCrashReport(report, "").Should().BeFalse();
            GlobalExceptionHandler.SaveCrashReport(report, null!).Should().BeFalse();
        }

        [Fact]
        public void CreateCrashReport_WithInnerException_CapturesIt()
        {
            var inner = new ArgumentException("inner error");
            var outer = new InvalidOperationException("outer error", inner);
            var report = GlobalExceptionHandler.CreateCrashReport(outer);
            report.InnerException.Should().Contain("inner error");
        }

        [Fact]
        public void CreateCrashReport_HasSystemInfo()
        {
            var report = GlobalExceptionHandler.CreateCrashReport(new Exception("test"));
            report.SystemInfo.Should().ContainKey("OS");
            report.SystemInfo.Should().ContainKey("Architecture");
            report.SystemInfo.Should().ContainKey("FrameworkDescription");
            report.SystemInfo.Should().ContainKey("ProcessorCount");
            report.SystemInfo.Should().ContainKey("Is64BitProcess");
        }
    }
}
