using System;
using NLog;
using NLog.Config;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Factory for creating NLogLogger instances with optional RichTextBox display.
    /// Supports both WinForms and WPF cross-thread logging.
    /// </summary>
    public static class RichTextBoxLoggerFactory
    {
        /// <summary>
        /// Creates an NLogLogger that writes to file only (no RichTextBox).
        /// </summary>
        public static NLogLogger CreateFileOnly(
            string logFilePath = "./logs/app.log",
            NLog.LogLevel? minLevel = null)
        {
            return new NLogLogger(logFilePath, minLevel ?? NLog.LogLevel.Debug);
        }

#if FEATURE_WINDOW_MODULE
        /// <summary>
        /// Creates an NLogLogger that writes to both a file and a WinForms RichTextBox.
        /// Can be called from any thread — the target handles cross-thread marshaling.
        /// </summary>
        public static NLogLogger CreateWithRichTextBox(
            System.Windows.Forms.RichTextBox textBox,
            string logFilePath = "./logs/app.log",
            NLog.LogLevel? minLevel = null)
        {
            var level = minLevel ?? NLog.LogLevel.Debug;
            var config = new LoggingConfiguration();

            var fileTarget = new NLog.Targets.FileTarget("file")
            {
                FileName = logFilePath,
                Layout = "${longdate:format=dd/MM/yyyy HH:mm:ss.fff}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                MaxArchiveFiles = 30,
                AutoFlush = true
            };

            var rtbTarget = new RichTextBoxLogTarget(textBox)
            {
                Layout = "${longdate:format=dd/MM/yyyy HH:mm:ss.fff}|${level:uppercase=true}|${message}"
            };

            config.AddTarget(fileTarget);
            config.AddTarget(rtbTarget);
            config.AddRule(level, NLog.LogLevel.Fatal, "file");
            config.AddRule(level, NLog.LogLevel.Fatal, "RichTextBox");

            LogManager.Configuration = config;
            return new NLogLogger(logFilePath, level);
        }

        /// <summary>
        /// Creates an NLogLogger that writes to a WinForms RichTextBox only (no file).
        /// Can be called from any thread — the target handles cross-thread marshaling.
        /// </summary>
        public static NLogLogger CreateRichTextBoxOnly(
            System.Windows.Forms.RichTextBox textBox,
            NLog.LogLevel? minLevel = null)
        {
            var level = minLevel ?? NLog.LogLevel.Debug;
            var config = new LoggingConfiguration();

            var rtbTarget = new RichTextBoxLogTarget(textBox)
            {
                Layout = "${longdate:format=dd/MM/yyyy HH:mm:ss.fff}|${level:uppercase=true}|${message}"
            };

            config.AddTarget(rtbTarget);
            config.AddRule(level, NLog.LogLevel.Fatal, "RichTextBox");

            LogManager.Configuration = config;
            return new NLogLogger("richtextbox", level);
        }
#endif
    }
}
