// <copyright file="NetworkAdapterInfoTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Tests.SystemInfo
{
    [Trait("Category", "SystemInfo")]
    public sealed class NetworkAdapterInfoTests
    {
        [Fact]
        public void GetAllAdapters_ShouldNotThrow()
        {
            System.Collections.Generic.IReadOnlyList<NetworkAdapterInfo> adapters = null!;
            Action act = () => adapters = NetworkInfo.GetAllAdapters();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetAllAdapters_ShouldReturnEmptyOrValidList()
        {
            var adapters = NetworkInfo.GetAllAdapters();
            adapters.Should().NotBeNull();
            // On Linux, P/Invoke to iphlpapi fails → returns empty list
            // On Windows, would return adapters
        }

        [Fact]
        public void GetAllAdapters_EmptyOnLinux()
        {
            var adapters = NetworkInfo.GetAllAdapters();
            // Linux: all P/Invoke fails → empty list
            adapters.Count.Should().Be(0);
        }

        [Fact]
        public void MacAddress_ShouldBeNullOrValidFormat()
        {
            var adapters = NetworkInfo.GetAllAdapters();
            foreach (var adapter in adapters)
            {
                if (adapter.MacAddress != null)
                {
                    // Format: XX:XX:XX:XX:XX:XX
                    adapter.MacAddress.Length.Should().Be(17);
                    adapter.MacAddress.Count(c => c == ':').Should().Be(5);
                }
            }
        }

        [Fact]
        public void GetAllAdapters_ReturnsReadOnlyList()
        {
            var adapters = NetworkInfo.GetAllAdapters();
            adapters.Should().BeAssignableTo<System.Collections.Generic.IReadOnlyList<NetworkAdapterInfo>>();
        }
    }
}
