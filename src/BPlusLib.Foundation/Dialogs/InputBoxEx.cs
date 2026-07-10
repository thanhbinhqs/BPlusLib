// <copyright file="InputBoxEx.cs" company="BPlusLib">
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
/// Displays a modal input dialog with label, text box, and OK/Cancel buttons.
/// </summary>
/// <remarks>
/// DPI-aware, supports dark mode, keyboard shortcuts (Enter=OK, Esc=Cancel),
/// and custom validation.
/// </remarks>
public static class InputBoxEx
{
    /// <summary>
    /// Shows an input box dialog asynchronously.
    /// </summary>
    /// <param name="parameters">The input box parameters.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An <see cref="InputBoxResult{T}"/> indicating whether the user confirmed
    /// and the entered value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is null.</exception>
    public static Task<InputBoxResult<string>> ShowAsync(InputBoxParams parameters, CancellationToken ct = default)
    {
        Guard.ThrowIfNull(parameters);
        return ShowAsyncCore(parameters, ct);
    }

    private static Task<InputBoxResult<string>> ShowAsyncCore(InputBoxParams parameters, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<InputBoxResult<string>>();

        var thread = new Thread(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                using var form = new InputBoxForm(parameters);

                // Set owner if specified
                if (parameters.Owner != null)
                {
                    // We need to set the owner on the same thread as the form
                    // Use a small helper to transfer the handle
                    form.Load += (_, _) =>
                    {
                        IntPtr ownerHandle = parameters.Owner.Handle;
                        if (ownerHandle != IntPtr.Zero)
                        {
                            User32.SetWindowPos(
                                form.Handle,
                                ownerHandle,
                                0, 0, 0, 0,
                                User32.SWP_NOSIZE | User32.SWP_NOACTIVATE | User32.SWP_NOMOVE);
                        }
                    };
                }

                // Register cancellation: close the form on cancellation
                using var cancellationReg = ct.Register(() =>
                {
                    if (form.IsHandleCreated && !form.IsDisposed)
                    {
                        form.BeginInvoke(() =>
                        {
                            if (!form.IsDisposed)
                            {
                                form.DialogResult = DialogResult.Cancel;
                                form.Close();
                            }
                        });
                    }
                });

                DialogResult result = form.ShowDialog();

                string? value = form.InputValue;
                tcs.TrySetResult(new InputBoxResult<string>(
                    result == DialogResult.OK,
                    value));
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetResult(new InputBoxResult<string>(false, null));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Name = "InputBoxEx STA";
        thread.Start();

        return tcs.Task;
    }

    /// <summary>
    /// The actual input form presented to the user.
    /// </summary>
    private sealed class InputBoxForm : Form
    {
        private readonly TextBox _textBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly Label _label;
        private readonly InputBoxParams _params;

        public string? InputValue => _textBox.Text;

        public InputBoxForm(InputBoxParams parameters)
        {
            _params = parameters;
            SuspendLayout();

            Text = parameters.Title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            SizeGripStyle = SizeGripStyle.Hide;
            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            // --- Label ---
            _label = new Label
            {
                Text = parameters.Label,
                AutoSize = true,
                Location = new Point(12, 12),
            };

            // --- TextBox ---
            _textBox = new TextBox
            {
                Location = new Point(12, 32),
                Width = 300,
                Text = parameters.DefaultValue ?? string.Empty,
                UseSystemPasswordChar = parameters.UsePasswordMask,
            };

            if (!string.IsNullOrEmpty(parameters.Placeholder) && string.IsNullOrEmpty(parameters.DefaultValue))
            {
                _textBox.Text = parameters.Placeholder;
                _textBox.ForeColor = SystemColors.GrayText;
                _textBox.Enter += (_, _) =>
                {
                    if (_textBox.Text == parameters.Placeholder)
                    {
                        _textBox.Text = string.Empty;
                        _textBox.ForeColor = SystemColors.WindowText;
                    }
                };
                _textBox.Leave += (_, _) =>
                {
                    if (string.IsNullOrEmpty(_textBox.Text))
                    {
                        _textBox.Text = parameters.Placeholder;
                        _textBox.ForeColor = SystemColors.GrayText;
                    }
                };
            }

            // --- OK button ---
            _okButton = new Button
            {
                Text = "OK",
                DialogResult = FormsDialogResult.OK,
                Location = new Point(156, 60),
                Size = new Size(75, 23),
            };
            _okButton.Click += (_, _) => ValidateInput();

            // --- Cancel button ---
            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = FormsDialogResult.Cancel,
                Location = new Point(237, 60),
                Size = new Size(75, 23),
            };

            // --- Layout ---
            ClientSize = new Size(324, 95);
            Controls.AddRange(new Control[] { _label, _textBox, _okButton, _cancelButton });
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
            if (parameters.Owner != null)
            {
                StartPosition = FormStartPosition.Manual;
                CenterOnOwner(parameters.Owner.Handle);
            }
        }

        private void ValidateInput()
        {
            if (_params.Validator != null)
            {
                string? error = _params.Validator(_textBox.Text);
                if (error != null)
                {
                    MessageBox.Show(this, error, _params.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = FormsDialogResult.None; // Prevent form from closing
                }
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
            _label.ForeColor = Color.White;
            _textBox.BackColor = Color.FromArgb(45, 45, 45);
            _textBox.ForeColor = Color.White;
            _okButton.BackColor = Color.FromArgb(60, 60, 60);
            _okButton.ForeColor = Color.White;
            _cancelButton.BackColor = Color.FromArgb(60, 60, 60);
            _cancelButton.ForeColor = Color.White;
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _textBox.Focus();
            _textBox.SelectAll();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                ValidateInput();
                if (DialogResult != FormsDialogResult.None)
                {
                    Close();
                }

                return true;
            }

            if (keyData == Keys.Escape)
            {
                DialogResult = FormsDialogResult.Cancel;
                Close();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }
    }
}
#endif
