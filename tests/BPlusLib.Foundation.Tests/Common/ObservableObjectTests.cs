// <copyright file="ObservableObjectTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Common;

namespace BPlusLib.Foundation.Tests.Common
{
    [Trait("Category", "Common")]
    public sealed class ObservableObjectTests
    {
        /// <summary>
        /// Concrete testable implementation of <see cref="ObservableObject"/>.
        /// </summary>
        private sealed class TestObservable : ObservableObject
        {
            private string _name = string.Empty;
            private int _age;

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            public int Age
            {
                get => _age;
                set => SetProperty(ref _age, value);
            }

            public new void OnPropertyChanged(string? propertyName = null)
                => base.OnPropertyChanged(propertyName);
        }

        [Fact]
        public void SetProperty_DifferentValue_RaisesPropertyChangingThenPropertyChanged()
        {
            var obj = new TestObservable();
            var changingEvents = new List<PropertyChangingEventArgs>();
            var changedEvents = new List<PropertyChangedEventArgs>();

            ((INotifyPropertyChanging)obj).PropertyChanging += (_, e) => changingEvents.Add(e);
            ((INotifyPropertyChanged)obj).PropertyChanged += (_, e) => changedEvents.Add(e);

            obj.Name = "Alice";

            changingEvents.Should().HaveCount(1);
            changingEvents[0].PropertyName.Should().Be("Name");

            changedEvents.Should().HaveCount(1);
            changedEvents[0].PropertyName.Should().Be("Name");
        }

        [Fact]
        public void SetProperty_SameValue_DoesNotRaiseEvents()
        {
            var obj = new TestObservable();
            obj.Name = "Alice";

            var changingCount = 0;
            var changedCount = 0;
            ((INotifyPropertyChanging)obj).PropertyChanging += (_, _) => changingCount++;
            ((INotifyPropertyChanged)obj).PropertyChanged += (_, _) => changedCount++;

            obj.Name = "Alice"; // same value

            changingCount.Should().Be(0);
            changedCount.Should().Be(0);
        }

        [Fact]
        public void OnPropertyChanged_RaisesEvent()
        {
            var obj = new TestObservable();
            string? capturedName = null;
            ((INotifyPropertyChanged)obj).PropertyChanged += (_, e) => capturedName = e.PropertyName;

            obj.OnPropertyChanged("TestProp");

            capturedName.Should().Be("TestProp");
        }

        [Fact]
        public void MultipleProperties_TrackedCorrectly()
        {
            var obj = new TestObservable();
            var changedProps = new List<string?>();

            ((INotifyPropertyChanged)obj).PropertyChanged += (_, e) => changedProps.Add(e.PropertyName);

            obj.Name = "Bob";
            obj.Age = 30;
            obj.Name = "Charlie";

            changedProps.Should().BeEquivalentTo(new[] { "Name", "Age", "Name" }, options => options.WithStrictOrdering());
        }
    }
}
