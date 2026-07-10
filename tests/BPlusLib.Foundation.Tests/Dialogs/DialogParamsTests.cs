// <copyright file="DialogParamsTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE

using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Dialogs;

namespace BPlusLib.Foundation.Tests.Dialogs
{
    [Trait("Category", "Dialogs")]
    public sealed class DialogParamsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var p = new DialogParams();

            p.Text.Should().Be(string.Empty);
            p.Caption.Should().Be(string.Empty);
            p.Details.Should().BeNull();
            p.CheckboxText.Should().BeNull();
            p.CheckboxState.Should().BeFalse();
            p.Buttons.Should().Be(DialogButton.OK);
            p.Icon.Should().Be(DialogIcon.None);
            p.DarkMode.Should().Be(DarkModeStyle.Inherit);
            p.TimeoutMs.Should().Be(0);
            p.TimeoutResult.Should().Be(DialogResult.None);
            p.TopMost.Should().BeFalse();
            p.CustomButtons.Should().BeNull();
            p.Owner.Should().BeNull();
        }

        [Fact]
        public void TextAndCaption_CanBeSet()
        {
            var p = new DialogParams
            {
                Text = "Hello",
                Caption = "Title",
            };

            p.Text.Should().Be("Hello");
            p.Caption.Should().Be("Title");
        }

        [Fact]
        public void CustomButtons_ListCanBeValidated()
        {
            var p = new DialogParams
            {
                CustomButtons = new List<DialogCustomButton>
                {
                    new DialogCustomButton
                    {
                        Text = "Retry",
                        Result = DialogResult.Retry,
                        IsDefault = true,
                    },
                },
            };

            p.CustomButtons.Should().HaveCount(1);
            p.CustomButtons[0].Text.Should().Be("Retry");
            p.CustomButtons[0].Result.Should().Be(DialogResult.Retry);
            p.CustomButtons[0].IsDefault.Should().BeTrue();
            p.CustomButtons[0].IsCancel.Should().BeFalse();
        }

        [Fact]
        public void TimeoutAndTimeoutResult_Coupling()
        {
            var p = new DialogParams
            {
                TimeoutMs = 5000,
                TimeoutResult = DialogResult.Cancel,
            };

            p.TimeoutMs.Should().Be(5000);
            p.TimeoutResult.Should().Be(DialogResult.Cancel);
        }

        [Fact]
        public void CheckboxTextWithCheckboxState()
        {
            var p = new DialogParams
            {
                CheckboxText = "Don't show again",
                CheckboxState = true,
            };

            p.CheckboxText.Should().Be("Don't show again");
            p.CheckboxState.Should().BeTrue();
        }

        [Fact]
        public void DarkModeStyle_Defaults()
        {
            var p = new DialogParams();
            p.DarkMode.Should().Be(DarkModeStyle.Inherit);
        }
    }
}

#endif
