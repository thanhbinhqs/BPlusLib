#if FEATURE_WINDOW_MODULE
using System;
using System.Threading;
using System.Windows.Forms;
using FluentAssertions;
using BPlusLib.Foundation.Logging;
using NLog;
using Xunit;

namespace BPlusLib.Foundation.Tests.Logging
{
    [Trait("Category", "Logging")]
    public class RichTextBoxLogTargetTests
    {
        [Fact]
        public void Constructor_NullTextBox_Throws()
        {
            Action act = () => new RichTextBoxLogTarget(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_CanCreateFromAnyThread()
        {
            Exception? threadException = null;
            var ready = new ManualResetEventSlim(false);
            var done = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                try
                {
                    // Create RichTextBox on STA thread (required for WinForms)
                    var rtb = new RichTextBox();
                    var target = new RichTextBoxLogTarget(rtb);
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
            using var target = new RichTextBoxLogTarget(rtb);
            target.MaxLines.Should().Be(5000);
            rtb.Dispose();
        }

        [Fact]
        public void MaxLines_CanSetCustomValue()
        {
            var rtb = new RichTextBox();
            using var target = new RichTextBoxLogTarget(rtb);
            target.MaxLines = 1000;
            target.MaxLines.Should().Be(1000);
            rtb.Dispose();
        }

        [Fact]
        public void Dispose_PreventsFurtherWrites()
        {
            var rtb = new RichTextBox();
            var target = new RichTextBoxLogTarget(rtb);
            target.Dispose();

            // Write after dispose should not throw
            var logEvent = LogEventInfo.Create(LogLevel.Info, "test", "message");
            Action act = () => target.Write(logEvent);
            act.Should().NotThrow();

            rtb.Dispose();
        }

        [Fact]
        public void Dispose_CanCallMultipleTimes()
        {
            var rtb = new RichTextBox();
            using var target = new RichTextBoxLogTarget(rtb);
            target.Dispose();
            Action act = () => target.Dispose();
            act.Should().NotThrow();
            rtb.Dispose();
        }

        [Fact]
        public void Write_CrossThread_MarshalsToUiThread()
        {
            var rtb = new RichTextBox();
            using var target = new RichTextBoxLogTarget(rtb);
            var done = new ManualResetEventSlim(false);

            // Log from background thread
            var thread = new Thread(() =>
            {
                var logEvent = LogEventInfo.Create(LogLevel.Info, "test", "Cross-thread message");
                target.Write(logEvent);
                done.Set();
            });
            thread.Start();
            done.Wait(TimeSpan.FromSeconds(5));

            // Verify text was appended (may need small delay for marshaling)
            Thread.Sleep(200);
            rtb.Text.Should().Contain("Cross-thread message");
            rtb.Dispose();
        }

        [Fact]
        public void Write_MultipleThreads_LogsAll()
        {
            var rtb = new RichTextBox();
            using var target = new RichTextBoxLogTarget(rtb) { MaxLines = 100 };
            var allDone = new CountdownEvent(5);

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var thread = new Thread(() =>
                {
                    var logEvent = LogEventInfo.Create(
                        LogLevel.Info, "test", $"Thread-{idx} message");
                    target.Write(logEvent);
                    allDone.Signal();
                });
                thread.Start();
            }

            allDone.Wait(TimeSpan.FromSeconds(10));
            Thread.Sleep(500); // Allow marshaling

            rtb.Lines.Length.Should().BeGreaterOrEqualTo(5);
            rtb.Text.Should().Contain("Thread-0");
            rtb.Text.Should().Contain("Thread-4");
            rtb.Dispose();
        }
    }
}
#endif
