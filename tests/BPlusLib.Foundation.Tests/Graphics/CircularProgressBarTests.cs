// <copyright file="CircularProgressBarTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if FEATURE_WINDOW_MODULE
using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Graphics;

namespace BPlusLib.Foundation.Tests.Graphics
{
    [Trait("Category", "Graphics")]
    public sealed class CircularProgressBarTests
    {
        [Fact]
        public void Constructor_CreatesValidInstance()
        {
            var control = new CircularProgressBar();
            control.Should().NotBeNull();
            control.Value.Should().Be(0);
            control.Maximum.Should().Be(100);
            control.Minimum.Should().Be(0);
        }

        [Fact]
        public void Value_Default_IsZero()
        {
            var control = new CircularProgressBar();
            control.Value.Should().Be(0);
        }

        [Fact]
        public void Maximum_Default_Is100()
        {
            var control = new CircularProgressBar();
            control.Maximum.Should().Be(100);
        }

        [Fact]
        public void Minimum_Default_IsZero()
        {
            var control = new CircularProgressBar();
            control.Minimum.Should().Be(0);
        }

        [Fact]
        public void Value_SetTo50_StoresCorrectly()
        {
            var control = new CircularProgressBar();
            control.Value = 50;
            control.Value.Should().Be(50);
        }

        [Fact]
        public void Value_SetAboveMax_ClampsToMax()
        {
            var control = new CircularProgressBar { Maximum = 100 };
            control.Value = 150;
            control.Value.Should().Be(100);
        }

        [Fact]
        public void Value_SetBelowMin_ClampsToMin()
        {
            var control = new CircularProgressBar { Minimum = 10, Maximum = 100 };
            control.Value = 5;
            control.Value.Should().Be(10);
        }

        [Fact]
        public void Text_Default_IsEmpty()
        {
            var control = new CircularProgressBar();
            control.DisplayText.Should().BeNullOrEmpty();
        }

        [Fact]
        public void Text_SetCustom_ShowsCustomText()
        {
            var control = new CircularProgressBar();
            control.DisplayText = "Hello";
            control.DisplayText.Should().Be("Hello");
        }

        [Fact]
        public void ShowPercentage_Default_IsTrue()
        {
            var control = new CircularProgressBar();
            control.ShowPercentage.Should().BeTrue();
        }

        [Fact]
        public void ProgressColor_Default_IsDodgerBlue()
        {
            var control = new CircularProgressBar();
            control.ProgressColor.Should().Be(Color.DodgerBlue);
        }

        [Fact]
        public void TrackColor_Default_IsLightGray()
        {
            var control = new CircularProgressBar();
            control.TrackColor.Should().Be(Color.LightGray);
        }

        [Fact]
        public void LineWidth_Default_IsPositive()
        {
            var control = new CircularProgressBar();
            control.LineWidth.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AnimationEnabled_Default_IsTrue()
        {
            var control = new CircularProgressBar();
            control.AnimationEnabled.Should().BeTrue();
        }

        [Fact]
        public void AnimationSpeed_Default_IsPositive()
        {
            var control = new CircularProgressBar();
            control.AnimationSpeed.Should().BeGreaterThan(0);
        }

        [Fact]
        public void SetRange_SetsMinAndMax()
        {
            var control = new CircularProgressBar();
            control.SetRange(0, 200);
            control.Minimum.Should().Be(0);
            control.Maximum.Should().Be(200);
        }

        [Fact]
        public void SetRange_InvalidRange_SwapsValues()
        {
            var control = new CircularProgressBar();
            control.SetRange(100, 0);
            control.Minimum.Should().Be(0);
            control.Maximum.Should().Be(100);
        }

        [Fact]
        public void Percentage_ComputedCorrectly()
        {
            var control = new CircularProgressBar { Minimum = 0, Maximum = 100 };
            control.Value = 50;
            control.Percentage.Should().BeApproximately(50.0f, 0.01f);
        }

        [Fact]
        public void Percentage_ZeroRange_ReturnsZero()
        {
            var control = new CircularProgressBar { Minimum = 50, Maximum = 50 };
            control.Percentage.Should().Be(0);
        }

        [Fact]
        public void Font_CustomFont_IsApplied()
        {
            var control = new CircularProgressBar();
            var customFont = new Font("Arial", 14);
            control.TextFont = customFont;
            control.TextFont.Should().Be(customFont);
        }

        [Fact]
        public void Dispose_Safe()
        {
            var control = new CircularProgressBar();
            Action act = () => control.Dispose();
            act.Should().NotThrow();
        }
    }
}
#endif
