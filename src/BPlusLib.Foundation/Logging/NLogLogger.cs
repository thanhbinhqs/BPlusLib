using System;
using NLog;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// NLog-based file logger with configurable log levels and file rolling.
    /// </summary>
    public sealed class NLogLogger : IDisposable
    {
        private readonly Logger _logger;
        private bool _disposed;

        public NLogLogger(string logFilePath = "./logs/app.log", NLog.LogLevel minLevel = null)
        {
            minLevel ??= NLog.LogLevel.Info;
            var config = new NLog.Config.LoggingConfiguration();
            var fileTarget = new NLog.Targets.FileTarget("file")
            {
                FileName = logFilePath,
                Layout = "${longdate:format=dd/MM/yyyy HH:mm:ss.fff}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                MaxArchiveFiles = 30,
                AutoFlush = true
            };
            config.AddTarget(fileTarget);
            config.AddRule(minLevel, NLog.LogLevel.Error, "file");
            LogManager.Configuration = config;
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Trace(string message) => _logger.Trace(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Info(message);
        public void Warn(string message) => _logger.Warn(message);
        public void Error(string message, Exception ex = null)
        {
            if (ex != null) _logger.Error(ex, message);
            else _logger.Error(message);
        }
        public void Fatal(string message, Exception ex = null)
        {
            if (ex != null) _logger.Fatal(ex, message);
            else _logger.Fatal(message);
        }

        public Logger UnderlyingLogger => _logger;
        public void Flush() => LogManager.Flush();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            LogManager.Flush();
            LogManager.Shutdown();
        }
    }
}
