// <copyright file="ProgressDialog.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BPlusLib.Foundation.Common;
using BPlusLib.Foundation.Native;
using BPlusLib.Foundation.Window;
using FormsDialogResult = System.Windows.Forms.DialogResult;

namespace BPlusLib.Foundation.Dialogs;

/// <summary>
/// A modal progress dialog with a progress bar, status text, and optional cancel button.
/// Thread-safe: <see cref="Report"/> can be called from any thread.
/// </summary>
/// <remarks>
/// DPI-aware, supports dark mode, keyboard shortcuts (Esc=Cancel).
/// </remarks>
public sealed class ProgressDialog : IDisposable
{
    private readonly ProgressDialogParams _parameters;
    private readonly IWin32Window? _owner;
    private ProgressDialogForm? _form;
    private TaskCompletionSource<bool>? _tcs;
    private Thread? _thread;
    private CancellationTokenSource? _internalCts;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressDialog"/> class.
    /// </summary>
    /// <param name="owner">The owner window (can be null).</param>
    /// <param name="parameters">The progress dialog parameters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is null.</exception>
    public ProgressDialog(IWin32Window? owner, ProgressDialogParams parameters)
    {
        Guard.ThrowIfNull(parameters);
        _owner = owner;
        _parameters = parameters;
        _internalCts = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets a cancellation token that is signalled when the user clicks Cancel
    /// or the dialog is closed.
    /// </summary>
    public CancellationToken CancellationToken => _internalCts?.Token ?? default;

    /// <summary>
    /// Shows the progress dialog asynchronously. The returned task completes
    /// when the dialog is closed (either by the user clicking Cancel, or by
    /// <see cref="IDisposable.Dispose"/> being called, or by cancellation).
    /// </summary>
    /// <param name="ct">A cancellation token to cancel showing the dialog.</param>
    /// <returns>A task that completes when the dialog is dismissed.</returns>
    public Task ShowAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_tcs != null)
        {
            return _tcs.Task;
        }

        _tcs = new TaskCompletionSource<bool>();

        using var registration = ct.Register(() =>
        {
            CloseDialog();
        });

        _thread = new Thread(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                _form = new ProgressDialogForm(_parameters, _owner);

                // When the form closes (user clicks Cancel or we close it), signal the TCS
                _form.FormClosed += (_, _) =>
                {
                    _internalCts?.Cancel();
                    _tcs?.TrySetResult(true);
                };

                _form.ShowDialog();
            }
            catch (OperationCanceledException)
            {
                _tcs?.TrySetResult(false);
            }
            catch (Exception ex)
            {
                _tcs?.TrySetException(ex);
            }
        });

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Name = "ProgressDialog STA";
        _thread.Start();

        return _tcs.Task;
    }

    /// <summary>
    /// Reports progress to the dialog. Can be called from any thread.
    /// </summary>
    /// <param name="percent">The progress percentage (0-100).</param>
    /// <param name="statusText">Optional status text to display.</param>
    public void Report(int percent, string? statusText = null)
    {
        ThrowIfDisposed();

        var form = _form;
        if (form == null || form.IsDisposed)
            return;

        var report = new ProgressReport
        {
            Percent = percent,
            StatusText = statusText,
            IsIndeterminate = _parameters.IsIndeterminate,
        };

        // Marshal to UI thread if needed
        if (form.InvokeRequired)
        {
            try
            {
                form.BeginInvoke(() => form.UpdateProgress(report));
            }
            catch
            {
                // Form may have been disposed
            }
        }
        else
        {
            form.UpdateProgress(report);
        }
    }

    /// <summary>
    /// Closes the progress dialog if it is open.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CloseDialog();
        _internalCts?.Dispose();
        _internalCts = null;
    }

    private void CloseDialog()
    {
        var form = _form;
        if (form != null && !form.IsDisposed && form.IsHandleCreated)
        {
            try
            {
                form.BeginInvoke(() =>
                {
                    if (!form.IsDisposed)
                    {
                        form.Close();
                    }
                });
            }
            catch
            {
                // Best-effort close
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProgressDialog));
    }

    /// <summary>
    /// The actual progress form presented to the user.
    /// </summary>
    private sealed class ProgressDialogForm : Form
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;
        private readonly Button? _cancelButton;
        private readonly IWin32Window? _owner;

        public ProgressDialogForm(ProgressDialogParams parameters, IWin32Window? owner)
        {
            _owner = owner;
            SuspendLayout();

            Text = parameters.Title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;

            // --- Status label ---
            _statusLabel = new Label
            {
                Text = parameters.Text,
                AutoSize = true,
                Location = new Point(12, 12),
                MaximumSize = new Size(360, 40),
            };

            // --- Progress bar ---
            _progressBar = new ProgressBar
            {
                Location = new Point(12, 40),
                Width = 360,
                Height = 23,
                Style = parameters.IsIndeterminate
                    ? ProgressBarStyle.Marquee
                    : ProgressBarStyle.Blocks,
                Maximum = parameters.Maximum > 0 ? parameters.Maximum : 100,
                Value = 0,
            };

            // --- Cancel button ---
            Button? cancelBtn = null;
            if (parameters.ShowCancelButton)
            {
                cancelBtn = new Button
                {
                    Text = "Cancel",
                    DialogResult = FormsDialogResult.Cancel,
                    Location = new Point(297, 72),
                    Size = new Size(75, 23),
                };
                cancelBtn.Click += (_, _) => Close();
                _cancelButton = cancelBtn;
            }

            // --- Layout ---
            int height = parameters.ShowCancelButton ? 107 : 75;
            ClientSize = new Size(384, height);

            var controls = new Control[] { _statusLabel, _progressBar };
            if (_cancelButton != null)
                controls = new Control[] { _statusLabel, _progressBar, _cancelButton };

            Controls.AddRange(controls);
            ResumeLayout(false);
            PerformLayout();

            // Apply DPI scaling
            ApplyDpiScaling();

            // Apply dark mode
            if (parameters.DarkMode == DarkModeStyle.Dark)
            {
                ApplyDarkMode();
            }

            // Center on owner if set
            if (owner != null)
            {
                StartPosition = FormStartPosition.Manual;
                CenterOnOwner(owner.Handle);
            }
        }

        public void UpdateProgress(ProgressReport report)
        {
            if (IsDisposed)
                return;

            if (!string.IsNullOrEmpty(report.StatusText))
            {
                _statusLabel.Text = report.StatusText;
            }

            if (!report.IsIndeterminate)
            {
                _progressBar.Style = ProgressBarStyle.Blocks;
                _progressBar.Value = Math.Max(0, Math.Min(report.Percent, _progressBar.Maximum));
            }
            else
            {
                _progressBar.Style = ProgressBarStyle.Marquee;
            }
        }

        private void ApplyDpiScaling()
        {
            try
            {
                if (Handle != IntPtr.Zero)
                {
                    var dpi = MonitorHelper.GetDpiForWindow(Handle);
                    if (dpi.Scale > 1.01f)
                    {
                        Scale(new SizeF(dpi.X, dpi.Y));
                    }
                }
            }
            catch
            {
                // DPI scaling is best-effort
            }
        }

        private void ApplyDarkMode()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            _statusLabel.ForeColor = Color.White;

            if (_cancelButton != null)
            {
                _cancelButton.BackColor = Color.FromArgb(60, 60, 60);
                _cancelButton.ForeColor = Color.White;
            }
        }

        private void CenterOnOwner(IntPtr ownerHandle)
        {
            try
            {
                if (User32.GetWindowRect(ownerHandle, out RECT parentRect) &&
                    User32.GetWindowRect(Handle, out RECT dlgRect))
                {
                    int x = parentRect.Left + (parentRect.Width - dlgRect.Width) / 2;
                    int y = parentRect.Top + (parentRect.Height - dlgRect.Height) / 2;
                    Location = new Point(x, y);
                }
            }
            catch
            {
                // Centering is best-effort
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && _cancelButton != null)
            {
                Close();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }
    }
}
#endif
