// <copyright file="IsExternalInit.cs" company="BPlusLib.Foundation">
// Copyright (c) BPlusLib.Foundation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#if NET472

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for the <c>init</c> accessor keyword introduced in C# 9.
    /// Required for .NET Framework 4.7.2 and .NET 6 which lack the type
    /// in their BCL.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

#endif