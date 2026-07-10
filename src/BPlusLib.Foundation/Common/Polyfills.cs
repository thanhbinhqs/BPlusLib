// <copyright file="Polyfills.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if NET472

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for <see cref="CallerArgumentExpressionAttribute"/> introduced in C# 10.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for <see cref="DoesNotReturnAttribute"/> introduced in .NET 5.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute
    {
    }

    /// <summary>
    /// Polyfill for <see cref="NotNullAttribute"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true)]
    internal sealed class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Polyfill for <see cref="NotNullIfNotNullAttribute"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true)]
    internal sealed class NotNullIfNotNullAttribute : Attribute
    {
        public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;
        public string ParameterName { get; }
    }
}

#endif