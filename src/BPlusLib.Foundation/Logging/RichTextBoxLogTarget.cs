#if FEATURE_WINDOW_MODULE
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NLog;
using NLog.Targets;

namespace BPlusLib.Foundation.Logging
{
    /// <summary>
    /// Custom NLog target that writes log entries to a WinForms RichTextBox
    /// with color coding by log level. Supports cross-thread logging —
    /// can be created on any thread; the SynchronizationContext is captured
    /// lazily on first write.
    /// </summary>
    [Target("RichTextBox")]
    public sealed class RichTextBoxLogTarget : TargetWithLayout, IDisposable
    {
        private readonly RichTextBox _textBox;
        private SynchronizationContext? _syncContext;
        private int _maxLines = 5000;
        private bool _disposed;

        /// <summary>
        /// Maximum number of lines in the RichTextBox. Older lines are trimmed.
        /// </summary>
        public int MaxLines
        {
            get => _maxLines;
            set => _maxLines = value;
        }

        /// <summary>
        /// Creates a new RichTextBoxLogTarget attached to the specified RichTextBox.
        /// Can be called from any thread — the SynchronizationContext is captured
        /// lazily on first log write (or immediately if already on the UI thread).
        /// </summary>
        /// <param name="textBox">The RichTextBox to render log entries to.</param>
        public RichTextBoxLogTarget(RichTextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            // Capture SynchronizationContext if already on UI thread; otherwise
            // it will be captured lazily on first Write() via BeginInvoke callback.
            _syncContext = SynchronizationContext.Current;
            Name = "RichTextBox";
        }

        /// <summary>
        /// Writes a log entry to the RichTextBox with color coding.
        /// Thread-safe: marshals to UI thread automatically.
        /// </summary>
        protected override void Write(LogEventInfo logEvent)
        {
            if (_disposed) return;

            string message = Layout.Render(logEvent);
            var color = GetColorForLevel(logEvent.Level);

            // Lazy capture: if no SynchronizationContext yet (created off-UI thread),
            // try to get one now. If still null, use Control.BeginInvoke as fallback.
            if (_syncContext == null)
            {
                if (_textBox.InvokeRequired)
                {
                    // Fallback: use WinForms BeginInvoke directly (thread-safe)
                    _textBox.BeginInvoke(new Action(() => AppendText(message, color)));
                    return;
                }
                _syncContext = SynchronizationContext.Current;
            }

            if (_syncContext != null)
            {
                _syncContext.Post(_ => AppendText(message, color), null);
            }
            else
            {
                // Last resort: direct invoke (only safe if already on UI thread)
                _textBox.BeginInvoke(new Action(() => AppendText(message, color)));
            }
        }

        /// <summary>
        /// Appends text to the RichTextBox. Always called on UI thread.
        /// No lock — single-threaded UI access guaranteed by marshaling.
        /// </summary>
        private void AppendText(string text, Color color)
        {
            if (_disposed) return;

            try
            {
                if (_textBox.IsDisposed) return;

                _textBox.SelectionStart = _textBox.TextLength;
                _textBox.SelectionLength = 0;
                _textBox.SelectionColor = color;
                _textBox.AppendText(text + Environment.NewLine);
                _textBox.SelectionColor = _textBox.ForeColor;

                // Trim old lines if exceeding max
                if (_textBox.Lines.Length > _maxLines)
                {
                    int excess = _textBox.Lines.Length - _maxLines;
                    _textBox.Select(0, _textBox.GetFirstCharIndexFromLine(excess));
                    _textBox.SelectedText = string.Empty;
                }

                // Auto-scroll to bottom
                _textBox.SelectionStart = _textBox.TextLength;
                _textBox.ScrollToCaret();
            }
            catch (ObjectDisposedException)
            {
                // RichTextBox was disposed between check and use — ignore.
            }
        }

        private static Color GetColorForLevel(LogLevel level)
        {
            if (level == LogLevel.Trace) return Color.Gray;
            if (level == LogLevel.Debug) return Color.LightGray;
            if (level == LogLevel.Info) return Color.White;
            if (level == LogLevel.Warn) return Color.Yellow;
            if (level == LogLevel.Error) return Color.OrangeRed;
            if (level == LogLevel.Fatal) return Color.Red;
            return Color.White;
        }

        /// <summary>
        /// Disposes the target and detaches from the RichTextBox.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _syncContext = null;
        }
    }
}
#endif
