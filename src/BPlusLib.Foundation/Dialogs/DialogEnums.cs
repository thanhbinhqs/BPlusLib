// <copyright file="DialogEnums.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace BPlusLib.Foundation.Dialogs;

/// <summary>
/// Specifies the buttons to display on a dialog.
/// </summary>
public enum DialogButton
{
    /// <summary>An OK button.</summary>
    OK = 0,

    /// <summary>OK and Cancel buttons.</summary>
    OKCancel = 1,

    /// <summary>Yes and No buttons.</summary>
    YesNo = 2,

    /// <summary>Yes, No, and Cancel buttons.</summary>
    YesNoCancel = 3,

    /// <summary>Abort, Retry, and Ignore buttons.</summary>
    AbortRetryIgnore = 4,

    /// <summary>Retry and Cancel buttons.</summary>
    RetryCancel = 5,

    /// <summary>Cancel, Try Again, and Continue buttons.</summary>
    CancelTryContinue = 6,
}

/// <summary>
/// Specifies the icon to display on a dialog.
/// </summary>
public enum DialogIcon
{
    /// <summary>No icon.</summary>
    None = 0,

    /// <summary>Information icon.</summary>
    Information = 1,

    /// <summary>Question icon.</summary>
    Question = 2,

    /// <summary>Warning icon.</summary>
    Warning = 3,

    /// <summary>Error icon.</summary>
    Error = 4,

    /// <summary>Shield (UAC) icon.</summary>
    Shield = 5,
}

/// <summary>
/// Indicates which button the user clicked on a dialog.
/// </summary>
public enum DialogResult
{
    /// <summary>No result (dialog not yet closed).</summary>
    None = 0,

    /// <summary>OK button clicked.</summary>
    OK = 1,

    /// <summary>Cancel button clicked.</summary>
    Cancel = 2,

    /// <summary>Abort button clicked.</summary>
    Abort = 3,

    /// <summary>Retry button clicked.</summary>
    Retry = 4,

    /// <summary>Ignore button clicked.</summary>
    Ignore = 5,

    /// <summary>Yes button clicked.</summary>
    Yes = 6,

    /// <summary>No button clicked.</summary>
    No = 7,

    /// <summary>Try Again button clicked.</summary>
    TryAgain = 10,

    /// <summary>Continue button clicked.</summary>
    Continue = 11,
}

/// <summary>
/// Specifies the dark mode style for a dialog.
/// </summary>
public enum DarkModeStyle
{
    /// <summary>Follow the system setting.</summary>
    System = 0,

    /// <summary>Always use light mode.</summary>
    Light = 1,

    /// <summary>Always use dark mode.</summary>
    Dark = 2,

    /// <summary>Inherit from the parent window or application setting.</summary>
    Inherit = 3,
}
