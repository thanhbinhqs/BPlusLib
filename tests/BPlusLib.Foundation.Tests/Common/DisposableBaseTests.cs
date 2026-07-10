// <copyright file="DisposableBaseTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Common;

namespace BPlusLib.Foundation.Tests.Common
{
    [Trait("Category", "Common")]
    public sealed class DisposableBaseTests
    {
        /// <summary>
        /// Concrete testable implementation of <see cref="DisposableBase"/>.
        /// </summary>
        private sealed class TestDisposable : DisposableBase
        {
            public bool ManagedDisposed { get; private set; }
            public bool UnmanagedDisposed { get; private set; }

            protected override void DisposeManaged()
            {
                ManagedDisposed = true;
            }

            protected override void DisposeUnmanaged()
            {
                UnmanagedDisposed = true;
            }

            public new void ThrowIfDisposed() => base.ThrowIfDisposed();
        }

        [Fact]
        public void Dispose_Once_RunsCleanly()
        {
            var obj = new TestDisposable();
            obj.Invoking(x => x.Dispose()).Should().NotThrow();
            obj.ManagedDisposed.Should().BeTrue();
        }

        [Fact]
        public void Dispose_Twice_IsIdempotent()
        {
            var obj = new TestDisposable();
            obj.Dispose();
            obj.Invoking(x => x.Dispose()).Should().NotThrow();
        }

        [Fact]
        public void IsDisposed_TrueAfterDispose()
        {
            var obj = new TestDisposable();
            obj.IsDisposed.Should().BeFalse();
            obj.Dispose();
            obj.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void ThrowIfDisposed_ThrowsAfterDispose()
        {
            var obj = new TestDisposable();
            obj.Dispose();
            obj.Invoking(x => x.ThrowIfDisposed()).Should().Throw<ObjectDisposedException>();
        }
    }
}
