// <copyright file="DialogParams.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BPlusLib.Foundation.Dialogs;

/// <summary>
/// Defines a custom button for a message box.
/// </summary>
public sealed class DialogCustomButton
{
    /// <summary>
    /// Gets or sets the text displayed on the button.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="DialogResult"/> returned when this button is clicked.
    /// </summary>
    public DialogResult Result { get; set; } = DialogResult.None;

    /// <summary>
    /// Gets or sets a value indicating whether this button is the default (Enter key) button.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this button is the cancel (Esc key) button.
    /// </summary>
    public bool IsCancel { get; set; }
}

/// <summary>
/// Parameters for displaying a message box.
/// </summary>
public sealed class DialogParams
{
    /// <summary>
    /// Gets or sets the message text to display.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dialog box caption (title).
    /// </summary>
    public string Caption { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional detailed text (displayed in an expandable area).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the text for a checkbox displayed below the message.
    /// When non-null and non-empty, a checkbox is shown.
    /// </summary>
    public string? CheckboxText { get; set; }

    /// <summary>
    /// Gets or sets the initial checked state of the checkbox.
    /// </summary>
    public bool CheckboxState { get; set; }

    /// <summary>
    /// Gets or sets the owner window for the dialog.
    /// </summary>
    public IWin32Window? Owner { get; set; }

    /// <summary>
    /// Gets or sets the buttons to display.
    /// </summary>
    public DialogButton Buttons { get; set; } = DialogButton.OK;

    /// <summary>
    /// Gets or sets the icon to display.
    /// </summary>
    public DialogIcon Icon { get; set; } = DialogIcon.None;

    /// <summary>
    /// Gets or sets the dark mode style.
    /// </summary>
    public DarkModeStyle DarkMode { get; set; } = DarkModeStyle.Inherit;

    /// <summary>
    /// Gets or sets the timeout in milliseconds after which the dialog auto-closes.
    /// Zero or negative means no timeout.
    /// </summary>
    public int TimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DialogResult"/> returned when the dialog times out.
    /// </summary>
    public DialogResult TimeoutResult { get; set; } = DialogResult.None;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog should be topmost.
    /// </summary>
    public bool TopMost { get; set; }

    /// <summary>
    /// Gets or sets a list of custom buttons to add to the dialog.
    /// Only supported for the custom form-based dialog; ignored for native MessageBox.
    /// </summary>
    public List<DialogCustomButton>? CustomButtons { get; set; }
}

/// <summary>
/// Parameters for displaying an input box dialog.
/// </summary>
public sealed class InputBoxParams
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the label text displayed above the input field.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value pre-filled in the input field.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text shown when the input field is empty.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the input is masked (password mode).
    /// </summary>
    public bool UsePasswordMask { get; set; }

    /// <summary>
    /// Gets or sets an optional validator function that returns null on success
    /// or an error message on failure.
    /// </summary>
    public Func<string, string?>? Validator { get; set; }

    /// <summary>
    /// Gets or sets the owner window for the dialog.
    /// </summary>
    public IWin32Window? Owner { get; set; }

    /// <summary>
    /// Gets or sets the dark mode style.
    /// </summary>
    public DarkModeStyle DarkMode { get; set; } = DarkModeStyle.Inherit;
}

/// <summary>
/// Represents the result of an input box dialog.
/// </summary>
/// <typeparam name="T">The type of the input value.</typeparam>
public readonly struct InputBoxResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the user confirmed (clicked OK).
    /// </summary>
    public bool Confirmed { get; }

    /// <summary>
    /// Gets the value entered by the user.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputBoxResult{T}"/> struct.
    /// </summary>
    /// <param name="confirmed">Whether the user confirmed.</param>
    /// <param name="value">The input value.</param>
    public InputBoxResult(bool confirmed, T? value)
    {
        Confirmed = confirmed;
        Value = value;
    }
}

/// <summary>
/// Parameters for displaying a progress dialog.
/// </summary>
public sealed class ProgressDialogParams
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status text displayed above the progress bar.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the progress bar is in indeterminate mode.
    /// </summary>
    public bool IsIndeterminate { get; set; }

    /// <summary>
    /// Gets or sets the maximum value of the progress bar (for determinate mode).
    /// </summary>
    public int Maximum { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether a cancel button is shown.
    /// </summary>
    public bool ShowCancelButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog should be topmost.
    /// </summary>
    public bool TopMost { get; set; }

    /// <summary>
    /// Gets or sets the dark mode style.
    /// </summary>
    public DarkModeStyle DarkMode { get; set; } = DarkModeStyle.Inherit;
}

/// <summary>
/// Provides progress updates for a <see cref="ProgressDialog"/>.
/// </summary>
public sealed class ProgressReport
{
    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    public int Percent { get; set; }

    /// <summary>
    /// Gets or sets the current status text.
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the progress bar is in indeterminate mode.
    /// </summary>
    public bool IsIndeterminate { get; set; }
}
#endif
