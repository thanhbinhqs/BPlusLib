// <copyright file="TestPlatform.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Tests
{
    internal static class TestPlatform
    {
        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
    }
}
