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
    /// with color coding by log level.
    /// </summary>
    [Target("RichTextBox")]
    public sealed class RichTextBoxLogTarget : TargetWithLayout
    {
        private readonly RichTextBox _textBox;
        private readonly SynchronizationContext _syncContext;
        private readonly object _lock = new object();
        private int _maxLines = 5000;

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
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="textBox">The RichTextBox to render log entries to.</param>
        public RichTextBoxLogTarget(RichTextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _syncContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException("RichTextBoxLogTarget must be created on the UI thread.");
            Name = "RichTextBox";
        }

        /// <summary>
        /// Writes a log entry to the RichTextBox with color coding.
        /// </summary>
        protected override void Write(LogEventInfo logEvent)
        {
            string message = Layout.Render(logEvent);
            var color = GetColorForLevel(logEvent.Level);

            if (_syncContext != null)
            {
                _syncContext.Post(_ => AppendText(message, color), null);
            }
            else
            {
                AppendText(message, color);
            }
        }

        private void AppendText(string text, Color color)
        {
            lock (_lock)
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
    }
}
#endif
