// <copyright file="DebounceTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Common;

namespace BPlusLib.Foundation.Tests.Common
{
    [Trait("Category", "Common")]
    public sealed class DebounceTests
    {
        [Fact]
        public void Invoke_TriggersActionAfterDelay()
        {
            var debounce = new Debounce(TimeSpan.FromMilliseconds(50));
            var executed = false;

            debounce.Invoke(() => executed = true);

            executed.Should().BeFalse();
            Thread.Sleep(120);
            executed.Should().BeTrue();
            debounce.Dispose();
        }

        [Fact]
        public void MultipleInvokes_ResetTimer_OnlyLastFires()
        {
            var debounce = new Debounce(TimeSpan.FromMilliseconds(50));
            var lastValue = 0;

            debounce.Invoke(() => lastValue = 1);
            debounce.Invoke(() => lastValue = 2);
            debounce.Invoke(() => lastValue = 3);

            Thread.Sleep(120);
            lastValue.Should().Be(3);
            debounce.Dispose();
        }

        [Fact]
        public void Cancel_PreventsExecution()
        {
            var debounce = new Debounce(TimeSpan.FromMilliseconds(50));
            var executed = false;

            debounce.Invoke(() => executed = true);
            debounce.Cancel();

            Thread.Sleep(120);
            executed.Should().BeFalse();
            debounce.Dispose();
        }

        [Fact]
        public void Dispose_StopsTimer()
        {
            var debounce = new Debounce(TimeSpan.FromMilliseconds(50));
            var executed = false;

            debounce.Invoke(() => executed = true);
            debounce.Dispose();

            Thread.Sleep(120);
            executed.Should().BeFalse();
        }
    }
}
