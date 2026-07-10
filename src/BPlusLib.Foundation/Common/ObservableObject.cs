// <copyright file="ObservableObject.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BPlusLib.Foundation.Common
{
    /// <summary>
    /// Provides a base class for objects whose property changes can be observed.
    /// Implements <see cref="INotifyPropertyChanged"/> and <see cref="INotifyPropertyChanging"/>.
    /// All events are raised on the current thread — no synchronization context marshaling is performed.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <summary>
        /// Occurs when a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs when a property value is about to change.
        /// </summary>
        public event PropertyChangingEventHandler? PropertyChanging;

        /// <summary>
        /// Sets the backing field to a new value, raising <see cref="PropertyChanging"/> before
        /// the assignment and <see cref="PropertyChanged"/> after, provided the new value differs
        /// from the current value.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="propertyName">
        /// The name of the property. Automatically populated by the compiler when called from a property setter.
        /// </param>
        /// <returns><c>true</c> if the value was changed; <c>false</c> if the values were equal.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            OnPropertyChanging(propertyName);
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that changed. Automatically populated by the compiler when called from a property setter.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanging"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that is changing. Automatically populated by the compiler when called from a property setter.
        /// </param>
        protected void OnPropertyChanging([CallerMemberName] string? propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
    }
}
