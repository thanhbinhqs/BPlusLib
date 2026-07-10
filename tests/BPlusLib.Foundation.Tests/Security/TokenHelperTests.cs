// <copyright file="TokenHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Security;

namespace BPlusLib.Foundation.Tests.Security
{
    [Trait("Category", "Security")]
    public sealed class TokenHelperTests
    {
        private static int CurrentProcessId => System.Diagnostics.Process.GetCurrentProcess().Id;

        // ── OpenProcessToken ────────────────────────────────────────────────

        [Fact]
        public void OpenProcessToken_CurrentProcess_ShouldSucceedOrReturnNull()
        {
            IntPtr? token = null;
            Action act = () => token = TokenHelper.OpenProcessToken(CurrentProcessId);
            act.Should().NotThrow();
            // On Linux returns null; on Windows returns a valid handle
        }

        [Fact]
        public void OpenProcessToken_InvalidPid_ShouldReturnNull()
        {
            IntPtr? token = TokenHelper.OpenProcessToken(int.MaxValue);
            token.Should().BeNull();
        }

        // ── GetTokenInformation ──────────────────────────────────────────────

        [Fact]
        public void GetTokenInformation_WithNullHandle_ReturnsNull()
        {
            bool result = TokenHelper.GetTokenInformation(IntPtr.Zero, TOKEN_INFORMATION_CLASS.TokenUser, out byte[]? data);
            result.Should().BeFalse();
            data.Should().BeNull();
        }

        [Fact]
        public void GetTokenInformation_WithInvalidHandle_ReturnsFalse()
        {
            bool result = TokenHelper.GetTokenInformation(new IntPtr(12345), TOKEN_INFORMATION_CLASS.TokenUser, out byte[]? data);
            result.Should().BeFalse();
        }

        // ── GetTokenUser ────────────────────────────────────────────────────

        [Fact]
        public void GetTokenUser_WithNullHandle_ReturnsNull()
        {
            string? user = TokenHelper.GetTokenUser(IntPtr.Zero);
            user.Should().BeNull();
        }

        [Fact]
        public void GetTokenUser_WithInvalidHandle_ReturnsNull()
        {
            string? user = TokenHelper.GetTokenUser(new IntPtr(9999));
            user.Should().BeNull();
        }

        // ── GetTokenGroups ──────────────────────────────────────────────────

        [Fact]
        public void GetTokenGroups_WithNullHandle_ReturnsNull()
        {
            string[]? groups = TokenHelper.GetTokenGroups(IntPtr.Zero);
            groups.Should().BeNull();
        }

        [Fact]
        public void GetTokenGroups_WithInvalidHandle_ReturnsNull()
        {
            string[]? groups = TokenHelper.GetTokenGroups(new IntPtr(9999));
            groups.Should().BeNull();
        }

        // ── GetTokenType ────────────────────────────────────────────────────

        [Fact]
        public void GetTokenType_WithNullHandle_ReturnsNull()
        {
            TokenType? tokenType = TokenHelper.GetTokenType(IntPtr.Zero);
            tokenType.Should().BeNull();
        }

        [Fact]
        public void GetTokenType_WithInvalidHandle_ReturnsNull()
        {
            TokenType? tokenType = TokenHelper.GetTokenType(new IntPtr(9999));
            tokenType.Should().BeNull();
        }

        // ── GetTokenSessionId ───────────────────────────────────────────────

        [Fact]
        public void GetTokenSessionId_WithNullHandle_ReturnsNull()
        {
            int? sessionId = TokenHelper.GetTokenSessionId(IntPtr.Zero);
            sessionId.Should().BeNull();
        }

        [Fact]
        public void GetTokenSessionId_WithInvalidHandle_ReturnsNull()
        {
            int? sessionId = TokenHelper.GetTokenSessionId(new IntPtr(9999));
            sessionId.Should().BeNull();
        }

        // ── GetTokenSource ──────────────────────────────────────────────────

        [Fact]
        public void GetTokenSource_WithNullHandle_ReturnsNull()
        {
            string? source = TokenHelper.GetTokenSource(IntPtr.Zero);
            source.Should().BeNull();
        }

        [Fact]
        public void GetTokenSource_WithInvalidHandle_ReturnsNull()
        {
            string? source = TokenHelper.GetTokenSource(new IntPtr(9999));
            source.Should().BeNull();
        }

        // ── GetTokenStatistics ──────────────────────────────────────────────

        [Fact]
        public void GetTokenStatistics_WithNullHandle_ReturnsNull()
        {
            string? stats = TokenHelper.GetTokenStatistics(IntPtr.Zero);
            stats.Should().BeNull();
        }

        [Fact]
        public void GetTokenStatistics_WithInvalidHandle_ReturnsNull()
        {
            string? stats = TokenHelper.GetTokenStatistics(new IntPtr(9999));
            stats.Should().BeNull();
        }

        // ── Enum values ─────────────────────────────────────────────────────

        [Fact]
        public void TokenAccessLevels_Values_ShouldBeCorrect()
        {
            ((int)TokenAccessLevels.AssignPrimary).Should().Be(0x0001);
            ((int)TokenAccessLevels.Duplicate).Should().Be(0x0002);
            ((int)TokenAccessLevels.Impersonate).Should().Be(0x0004);
            ((int)TokenAccessLevels.Query).Should().Be(0x0008);
            ((int)TokenAccessLevels.QuerySource).Should().Be(0x0010);
            ((int)TokenAccessLevels.AdjustPrivileges).Should().Be(0x0020);
            ((int)TokenAccessLevels.AdjustDefault).Should().Be(0x0080);
            ((int)TokenAccessLevels.AdjustSessionId).Should().Be(0x0100);
            ((int)TokenAccessLevels.Read).Should().Be(0x00020008);
            ((int)TokenAccessLevels.Write).Should().Be(0x000200E0);
            ((int)TokenAccessLevels.AllAccess).Should().Be(0x000F01FF);
        }

        [Fact]
        public void TokenType_Values_ShouldBeCorrect()
        {
            ((int)TokenType.TokenPrimary).Should().Be(1);
            ((int)TokenType.TokenImpersonation).Should().Be(2);
        }

        [Fact]
        public void TOKEN_INFORMATION_CLASS_Values_ShouldBeCorrect()
        {
            ((int)TOKEN_INFORMATION_CLASS.TokenUser).Should().Be(1);
            ((int)TOKEN_INFORMATION_CLASS.TokenGroups).Should().Be(2);
            ((int)TOKEN_INFORMATION_CLASS.TokenPrivileges).Should().Be(3);
            ((int)TOKEN_INFORMATION_CLASS.TokenType).Should().Be(8);
            ((int)TOKEN_INFORMATION_CLASS.TokenSessionId).Should().Be(12);
            ((int)TOKEN_INFORMATION_CLASS.TokenIntegrityLevel).Should().Be(25);
        }
    }
}
