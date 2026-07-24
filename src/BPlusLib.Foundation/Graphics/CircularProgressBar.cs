// <copyright file="CircularProgressBar.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BPlusLib.Foundation.Graphics
{
    /// <summary>
    /// A circular progress bar control that displays a ring-shaped progress
    /// indicator with customizable text in the center. Supports smooth animation,
    /// DPI awareness, and full color customization.
    /// </summary>
    /// <remarks>
    /// <para>All rendering uses GDI+ with anti-aliasing. The control is optimized
    /// for performance — double-buffered, with minimal allocations in the paint path.</para>
    /// <para>Use <see cref="Value"/> to set the current progress (0 to 100 by default).
    /// Use <see cref="DisplayText"/> for custom center text, or set
    /// <see cref="ShowPercentage"/> to auto-display the percentage.</para>
    /// </remarks>
    [DefaultProperty(nameof(Value))]
    [DefaultEvent(nameof(ValueChanged))]
    [ToolboxBitmap(typeof(ProgressBar))]
    public class CircularProgressBar : Control
    {
        // =====================================================================
        // Fields
        // =====================================================================

        private int _value;
        private int _minimum;
        private int _maximum = 100;
        private float _displayedValue;
        private readonly Timer _animationTimer;
        private string? _displayText;
        private Color _progressColor = Color.DodgerBlue;
        private Color _progressColor2 = Color.Empty;
        private Color _trackColor = Color.LightGray;
        private Color _textColor = Color.Black;
        private Color _centerTextColor = Color.Black;
        private int _lineWidth = 12;
        private int _lineCapSize = 6;
        private bool _animationEnabled = true;
        private int _animationSpeed = 20;
        private float _animationStep = 2.0f;
        private bool _showPercentage = true;
        private bool _showText = true;
        private Font? _textFont;
        private StringFormat _textFormat;
        private bool _disposed;

        // =====================================================================
        // Events
        // =====================================================================

        /// <summary>Fires when the Value property changes.</summary>
        [Category("Action")]
        [Description("Occurs when the Value property changes.")]
        public event EventHandler? ValueChanged;

        // =====================================================================
        // Constructor
        // =====================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularProgressBar"/> control.
        /// </summary>
        public CircularProgressBar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(150, 150);
            MinimumSize = new Size(50, 50);

            _textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            _animationTimer = new Timer
            {
                Interval = _animationSpeed,
                Enabled = false,
            };
            _animationTimer.Tick += AnimationTick;

            _displayedValue = 0;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Gets or sets the current value. Clamped between <see cref="Minimum"/> and <see cref="Maximum"/>.
        /// </summary>
        [Category("Data")]
        [Description("The current value of the progress bar.")]
        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value == clamped) return;
                _value = clamped;

                if (_animationEnabled)
                    StartAnimation();
                else
                    _displayedValue = _value;

                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        [Category("Data")]
        [Description("The minimum value.")]
        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                if (value > _maximum) value = _maximum;
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        [Category("Data")]
        [Description("The maximum value.")]
        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                if (value < _minimum) value = _minimum;
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the text displayed in the center of the ring.
        /// If null and <see cref="ShowPercentage"/> is true, the percentage is shown.
        /// </summary>
        [Category("Appearance")]
        [Description("Custom text displayed in the center. If null, percentage is shown.")]
        [DefaultValue(null)]
        public string? DisplayText
        {
            get => _displayText;
            set { _displayText = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets whether to auto-display the percentage value in the center.
        /// </summary>
        [Category("Appearance")]
        [Description("If true, auto-displays the percentage in the center.")]
        [DefaultValue(true)]
        public bool ShowPercentage
        {
            get => _showPercentage;
            set { _showPercentage = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets whether to show text at all in the center.
        /// </summary>
        [Category("Appearance")]
        [Description("If true, shows text in the center.")]
        [DefaultValue(true)]
        public bool ShowText
        {
            get => _showText;
            set { _showText = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the progress ring color.
        /// </summary>
        [Category("Appearance")]
        [Description("The color of the progress ring.")]
        public Color ProgressColor
        {
            get => _progressColor;
            set { _progressColor = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the gradient end color for the progress ring.
        /// Set to <see cref="Color.Empty"/> for no gradient (solid color).
        /// </summary>
        [Category("Appearance")]
        [Description("Gradient end color for the progress ring. Empty = no gradient.")]
        public Color ProgressColor2
        {
            get => _progressColor2;
            set { _progressColor2 = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the track (background) ring color.
        /// </summary>
        [Category("Appearance")]
        [Description("The color of the background track ring.")]
        public Color TrackColor
        {
            get => _trackColor;
            set { _trackColor = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the text color for percentage display.
        /// </summary>
        [Category("Appearance")]
        [Description("The color of the percentage text.")]
        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the text color for custom center text.
        /// </summary>
        [Category("Appearance")]
        [Description("The color of the custom center text.")]
        public Color CenterTextColor
        {
            get => _centerTextColor;
            set { _centerTextColor = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the width of the progress ring line.
        /// </summary>
        [Category("Appearance")]
        [Description("The width of the progress ring line.")]
        [DefaultValue(12)]
        public int LineWidth
        {
            get => _lineWidth;
            set { _lineWidth = Math.Max(2, value); Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the size of the line cap (rounded ends).
        /// </summary>
        [Category("Appearance")]
        [Description("The size of the rounded line cap.")]
        [DefaultValue(6)]
        public int LineCapSize
        {
            get => _lineCapSize;
            set { _lineCapSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the font used for center text.
        /// </summary>
        [Category("Appearance")]
        [Description("The font for center text. If null, uses control Font scaled to 18% of size.")]
        public Font? TextFont
        {
            get => _textFont;
            set { _textFont = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets whether animation is enabled.
        /// </summary>
        [Category("Behavior")]
        [Description("If true, progress changes are animated smoothly.")]
        [DefaultValue(true)]
        public bool AnimationEnabled
        {
            get => _animationEnabled;
            set
            {
                _animationEnabled = value;
                if (!value)
                {
                    _animationTimer.Stop();
                    _displayedValue = _value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the animation speed in milliseconds per tick.
        /// </summary>
        [Category("Behavior")]
        [Description("Animation speed in milliseconds per tick.")]
        [DefaultValue(20)]
        public int AnimationSpeed
        {
            get => _animationSpeed;
            set { _animationSpeed = Math.Max(5, value); _animationTimer.Interval = _animationSpeed; }
        }

        /// <summary>
        /// Gets or sets how much the displayed value changes per animation tick.
        /// </summary>
        [Category("Behavior")]
        [Description("Value increment per animation tick.")]
        [DefaultValue(2.0f)]
        public float AnimationStep
        {
            get => _animationStep;
            set { _animationStep = Math.Max(0.1f, value); }
        }

        /// <summary>
        /// Gets the computed percentage (0-100) based on current value and range.
        /// </summary>
        [Browsable(false)]
        public float Percentage
        {
            get
            {
                int range = _maximum - _minimum;
                return range == 0 ? 0f : (float)(_value - _minimum) / range * 100f;
            }
        }

        /// <summary>
        /// Gets the displayed percentage during animation.
        /// </summary>
        [Browsable(false)]
        public float DisplayedPercentage
        {
            get
            {
                int range = _maximum - _minimum;
                return range == 0 ? 0f : (_displayedValue - _minimum) / range * 100f;
            }
        }

        // =====================================================================
        // Public Methods
        // =====================================================================

        /// <summary>
        /// Sets the minimum and maximum values.
        /// </summary>
        public void SetRange(int minimum, int maximum)
        {
            if (minimum > maximum)
            {
                int temp = minimum;
                minimum = maximum;
                maximum = temp;
            }
            _minimum = minimum;
            _maximum = maximum;
            if (_value < _minimum) Value = _minimum;
            if (_value > _maximum) Value = _maximum;
            Invalidate();
        }

        // =====================================================================
        // Painting
        // =====================================================================

        /// <summary>
        /// Raises the Paint event and renders the circular progress bar.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_disposed) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;
            int size = Math.Min(w, h);
            float padding = _lineWidth / 2f + 2;
            float diameter = size - padding * 2;
            float cx = w / 2f;
            float cy = h / 2f;

            // Calculate angles (start at top, sweep clockwise)
            float startAngle = -90f;
            float sweepAngle = 360f * (_displayedValue - _minimum) / Math.Max(1, _maximum - _minimum);

            // 1. Draw track ring
            if (_trackColor != Color.Transparent)
            {
                using var trackPen = new Pen(_trackColor, _lineWidth);
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawEllipse(trackPen, padding, padding, diameter, diameter);
            }

            // 2. Draw progress ring
            if (sweepAngle > 0.1f)
            {
                RectangleF rect = new RectangleF(padding, padding, diameter, diameter);

                if (_progressColor2 != Color.Empty)
                {
                    using var brush = new LinearGradientBrush(
                        rect, _progressColor, _progressColor2, 0f);
                    using var pen = new Pen(brush, _lineWidth);
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, padding, padding, diameter, diameter, startAngle, sweepAngle);
                }
                else
                {
                    using var pen = new Pen(_progressColor, _lineWidth);
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, padding, padding, diameter, diameter, startAngle, sweepAngle);
                }
            }

            // 3. Draw center text
            if (_showText)
            {
                string text = GetCenterText();
                if (!string.IsNullOrEmpty(text))
                {
                    Font font = _textFont ?? new Font(Font.FontFamily, size * 0.18f);
                    Color textColor = !string.IsNullOrEmpty(_displayText) ? _centerTextColor : _textColor;

                    using var brush = new SolidBrush(textColor);
                    RectangleF textRect = new RectangleF(0, 0, w, h);
                    g.DrawString(text, font, brush, textRect, _textFormat);

                    if (_textFont is null)
                        font.Dispose();
                }
            }
        }

        private string GetCenterText()
        {
            if (!string.IsNullOrEmpty(_displayText))
                return _displayText;

            if (_showPercentage)
                return $"{Percentage:F0}%";

            return "";
        }

        // =====================================================================
        // Animation
        // =====================================================================

        private void StartAnimation()
        {
            if (!_animationEnabled || _disposed) return;

            if (Math.Abs(_displayedValue - _value) < 0.5f)
            {
                _displayedValue = _value;
                Invalidate();
                return;
            }

            _animationTimer.Stop();
            _animationTimer.Start();
        }

        private void AnimationTick(object? sender, EventArgs e)
        {
            if (_disposed) return;

            float diff = _value - _displayedValue;
            if (Math.Abs(diff) < 0.5f)
            {
                _displayedValue = _value;
                _animationTimer.Stop();
                Invalidate();
                return;
            }

            if (diff > 0)
                _displayedValue = Math.Min(_displayedValue + _animationStep, _value);
            else
                _displayedValue = Math.Max(_displayedValue - _animationStep, _value);

            Invalidate();
        }

        // =====================================================================
        // Resize
        // =====================================================================

        /// <summary>
        /// Raises the Resize event and forces a repaint.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        // =====================================================================
        // Dispose
        // =====================================================================

        /// <summary>
        /// Releases all resources used by the control.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                _animationTimer.Stop();
                _animationTimer.Dispose();
                _textFormat.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
#endif
