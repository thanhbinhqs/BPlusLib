#if FEATURE_WINDOW_MODULE
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using FluentAssertions;
using BPlusLib.Foundation.Logging;
using NLog;
using Xunit;
using NLogLogLevel = NLog.LogLevel;

namespace BPlusLib.Foundation.Tests.Logging
{
    [Trait("Category", "Logging")]
    public class RichTextBoxLogTargetTests
    {
        private static bool WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Application.DoEvents();
                if (condition())
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            Application.DoEvents();
            return condition();
        }

        private sealed class TestableRichTextBoxLogTarget : RichTextBoxLogTarget
        {
            public TestableRichTextBoxLogTarget(RichTextBox textBox)
                : base(textBox)
            {
            }

            public void WritePublic(LogEventInfo logEvent) => base.Write(logEvent);
        }

        [Fact]
        public void Constructor_NullTextBox_Throws()
        {
            Action act = () => new TestableRichTextBoxLogTarget(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_CanCreateFromAnyThread()
        {
            Exception? threadException = null;
            var done = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                try
                {
                    var rtb = new RichTextBox();
                    var target = new TestableRichTextBoxLogTarget(rtb);
                    target.Should().NotBeNull();
                    rtb.Dispose();
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
                finally
                {
                    done.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            done.Wait(TimeSpan.FromSeconds(5));

            threadException.Should().BeNull(
                because: "RichTextBoxLogTarget should be creatable from any thread");
        }

        [Fact]
        public void MaxLines_DefaultIs5000()
        {
            var rtb = new RichTextBox();
            using var target = new TestableRichTextBoxLogTarget(rtb);
            target.MaxLines.Should().Be(5000);
            rtb.Dispose();
        }

        [Fact]
        public void MaxLines_CanSetCustomValue()
        {
            var rtb = new RichTextBox();
            using var target = new TestableRichTextBoxLogTarget(rtb);
            target.MaxLines = 1000;
            target.MaxLines.Should().Be(1000);
            rtb.Dispose();
        }

        [Fact]
        public void Dispose_PreventsFurtherWrites()
        {
            var rtb = new RichTextBox();
            var target = new TestableRichTextBoxLogTarget(rtb);
            target.Dispose();

            var logEvent = LogEventInfo.Create(NLogLogLevel.Info, "test", "message");
            Action act = () => target.WritePublic(logEvent);
            act.Should().NotThrow();

            rtb.Dispose();
        }

        [Fact]
        public void Dispose_CanCallMultipleTimes()
        {
            var rtb = new RichTextBox();
            using var target = new TestableRichTextBoxLogTarget(rtb);
            target.Dispose();
            Action act = () => target.Dispose();
            act.Should().NotThrow();
            rtb.Dispose();
        }

        [SkippableFact]
        public void Write_CrossThread_MarshalsToUiThread()
        {
            Skip.IfNot(Application.MessageLoop, "Cross-thread WinForms marshaling assertions require an active UI message loop.");
            var rtb = new RichTextBox();
            _ = rtb.Handle;
            using var target = new TestableRichTextBoxLogTarget(rtb);
            var done = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                var logEvent = LogEventInfo.Create(NLogLogLevel.Info, "test", "Cross-thread message");
                target.WritePublic(logEvent);
                done.Set();
            });
            thread.Start();
            done.Wait(TimeSpan.FromSeconds(5));

            WaitUntil(() => rtb.Text.IndexOf("Cross-thread message", StringComparison.Ordinal) >= 0, 5000)
                .Should().BeTrue("because the UI thread should process the marshaled log write");
            rtb.Text.Should().Contain("Cross-thread message");
            rtb.Dispose();
        }

        [SkippableFact]
        public void Write_MultipleThreads_LogsAll()
        {
            Skip.IfNot(Application.MessageLoop, "Cross-thread WinForms marshaling assertions require an active UI message loop.");
            var rtb = new RichTextBox();
            _ = rtb.Handle;
            using var target = new TestableRichTextBoxLogTarget(rtb) { MaxLines = 100 };
            var allDone = new CountdownEvent(5);

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var thread = new Thread(() =>
                {
                    var logEvent = LogEventInfo.Create(
                        NLogLogLevel.Info, "test", $"Thread-{idx} message");
                    target.WritePublic(logEvent);
                    allDone.Signal();
                });
                thread.Start();
            }

            allDone.Wait(TimeSpan.FromSeconds(10));
            WaitUntil(() => rtb.Lines.Length >= 5, 5000)
                .Should().BeTrue("because the UI thread should process all queued log writes");

            rtb.Lines.Length.Should().BeGreaterOrEqualTo(5);
            rtb.Text.Should().Contain("Thread-0");
            rtb.Text.Should().Contain("Thread-4");
            rtb.Dispose();
        }
    }
}
#endif
