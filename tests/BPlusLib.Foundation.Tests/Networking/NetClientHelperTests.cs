// <copyright file="NetClientHelperTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Networking;

namespace BPlusLib.Foundation.Tests.Networking
{
    [Trait("Category", "Networking")]
    public sealed class NetClientHelperTests
    {
        private const string InvalidUrl = "https://invalid.nonexistent.example.com/test";
        private const string FtpInvalidUrl = "ftp://invalid.nonexistent.example.com/";

        // ── HTTP Get (sync) ────────────────────────────────────

        [Fact]
        public void HttpGet_InvalidUrl_ReturnsNull()
        {
            string? result = NetClientHelper.HttpGet(InvalidUrl, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void HttpGet_NullUrl_ReturnsNull()
        {
            string? result = NetClientHelper.HttpGet(null!, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void HttpGet_EmptyUrl_ReturnsNull()
        {
            string? result = NetClientHelper.HttpGet(string.Empty, timeoutMs: 1000);
            result.Should().BeNull();
        }

        // ── HTTP Post (sync) ───────────────────────────────────

        [Fact]
        public void HttpPost_InvalidUrl_ReturnsNull()
        {
            string? result = NetClientHelper.HttpPost(InvalidUrl, "body", timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void HttpPost_NullUrl_ReturnsNull()
        {
            string? result = NetClientHelper.HttpPost(null!, "body", timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void HttpPost_EmptyUrl_ReturnsNull()
        {
            string? result = NetClientHelper.HttpPost(string.Empty, "body", timeoutMs: 1000);
            result.Should().BeNull();
        }

        // ── HTTP Download (sync) ───────────────────────────────

        [Fact]
        public void HttpDownload_InvalidUrl_ReturnsNull()
        {
            byte[]? result = NetClientHelper.HttpDownload(InvalidUrl, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void HttpDownload_NullUrl_ReturnsNull()
        {
            byte[]? result = NetClientHelper.HttpDownload(null!, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void HttpDownload_EmptyUrl_ReturnsNull()
        {
            byte[]? result = NetClientHelper.HttpDownload(string.Empty, timeoutMs: 1000);
            result.Should().BeNull();
        }

        // ── TryDownloadFile (sync) ─────────────────────────────

        [Fact]
        public void TryDownloadFile_InvalidUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.TryDownloadFile(InvalidUrl, "/tmp/out.dat", timeoutMs: 1000);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryDownloadFile_NullUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.TryDownloadFile(null!, "/tmp/out.dat", timeoutMs: 1000);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryDownloadFile_EmptyUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.TryDownloadFile(string.Empty, "/tmp/out.dat", timeoutMs: 1000);
            result.Should().BeFalse();
        }

        [Fact]
        public void TryDownloadFile_NullOutputPath_ReturnsFalse()
        {
            bool result = NetClientHelper.TryDownloadFile(InvalidUrl, null!, timeoutMs: 1000);
            result.Should().BeFalse();
        }

        // ── HTTP Get Async ─────────────────────────────────────

        [Fact]
        public async Task HttpGetAsync_InvalidUrl_ReturnsNull()
        {
            string? result = await NetClientHelper.HttpGetAsync(InvalidUrl, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public async Task HttpGetAsync_NullUrl_ReturnsNull()
        {
            string? result = await NetClientHelper.HttpGetAsync(null!, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public async Task HttpGetAsync_EmptyUrl_ReturnsNull()
        {
            string? result = await NetClientHelper.HttpGetAsync(string.Empty, timeoutMs: 1000);
            result.Should().BeNull();
        }

        // ── HTTP Post Async ────────────────────────────────────

        [Fact]
        public async Task HttpPostAsync_InvalidUrl_ReturnsNull()
        {
            string? result = await NetClientHelper.HttpPostAsync(InvalidUrl, "body", timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public async Task HttpPostAsync_NullUrl_ReturnsNull()
        {
            string? result = await NetClientHelper.HttpPostAsync(null!, "body", timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public async Task HttpPostAsync_EmptyUrl_ReturnsNull()
        {
            string? result = await NetClientHelper.HttpPostAsync(string.Empty, "body", timeoutMs: 1000);
            result.Should().BeNull();
        }

        // ── HTTP Download Async ────────────────────────────────

        [Fact]
        public async Task HttpDownloadAsync_InvalidUrl_ReturnsNull()
        {
            byte[]? result = await NetClientHelper.HttpDownloadAsync(InvalidUrl, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public async Task HttpDownloadAsync_NullUrl_ReturnsNull()
        {
            byte[]? result = await NetClientHelper.HttpDownloadAsync(null!, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public async Task HttpDownloadAsync_EmptyUrl_ReturnsNull()
        {
            byte[]? result = await NetClientHelper.HttpDownloadAsync(string.Empty, timeoutMs: 1000);
            result.Should().BeNull();
        }

        // ── TryDownloadFile Async ──────────────────────────────

        [Fact]
        public async Task TryDownloadFileAsync_InvalidUrl_ReturnsFalse()
        {
            bool result = await NetClientHelper.TryDownloadFileAsync(InvalidUrl, "/tmp/async_out.dat", timeoutMs: 1000);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryDownloadFileAsync_NullUrl_ReturnsFalse()
        {
            bool result = await NetClientHelper.TryDownloadFileAsync(null!, "/tmp/async_out.dat", timeoutMs: 1000);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryDownloadFileAsync_EmptyUrl_ReturnsFalse()
        {
            bool result = await NetClientHelper.TryDownloadFileAsync(string.Empty, "/tmp/async_out.dat", timeoutMs: 1000);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryDownloadFileAsync_NullOutputPath_ReturnsFalse()
        {
            bool result = await NetClientHelper.TryDownloadFileAsync(InvalidUrl, null!, timeoutMs: 1000);
            result.Should().BeFalse();
        }

        // ── FTP Operations ─────────────────────────────────────

        [Fact]
        public void FtpListDirectory_InvalidUrl_ReturnsNull()
        {
            string[]? result = NetClientHelper.FtpListDirectory(FtpInvalidUrl, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void FtpListDirectory_NullUrl_ReturnsNull()
        {
            string[]? result = NetClientHelper.FtpListDirectory(null!, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void FtpListDirectory_EmptyUrl_ReturnsNull()
        {
            string[]? result = NetClientHelper.FtpListDirectory(string.Empty, timeoutMs: 1000);
            result.Should().BeNull();
        }

        [Fact]
        public void FtpDownloadFile_InvalidUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpDownloadFile(FtpInvalidUrl + "file.dat", "/tmp/ftp_out.dat");
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpDownloadFile_NullUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpDownloadFile(null!, "/tmp/ftp_out.dat");
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpDownloadFile_NullOutputPath_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpDownloadFile(FtpInvalidUrl + "file.dat", null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpUploadFile_InvalidUrl_ReturnsFalse()
        {
            // Upload to invalid server with a non-existent local file — should fail early.
            bool result = NetClientHelper.FtpUploadFile(FtpInvalidUrl + "upload.dat", "/tmp/nonexistent_upload_file.dat");
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpUploadFile_NullUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpUploadFile(null!, "/tmp/somefile.dat");
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpUploadFile_NullLocalPath_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpUploadFile(FtpInvalidUrl + "upload.dat", null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpCreateDirectory_InvalidUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpCreateDirectory(FtpInvalidUrl + "newdir");
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpCreateDirectory_NullUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpCreateDirectory(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpCreateDirectory_EmptyUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpCreateDirectory(string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpDeleteFile_InvalidUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpDeleteFile(FtpInvalidUrl + "oldfile.dat");
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpDeleteFile_NullUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpDeleteFile(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void FtpDeleteFile_EmptyUrl_ReturnsFalse()
        {
            bool result = NetClientHelper.FtpDeleteFile(string.Empty);
            result.Should().BeFalse();
        }

        // ── Network Info Helpers ───────────────────────────────

        [Fact]
        public void IsNetworkAvailable_ShouldReturnBool()
        {
            // This should not throw, regardless of actual network state.
            bool result = NetClientHelper.IsNetworkAvailable();
            // Accept either true or false — we just verify it doesn't throw.
            result.GetType().Should().Be(typeof(bool));
        }

        [Fact]
        public void IsInternetAvailable_WithShortTimeout_ShouldNotThrow()
        {
            // Short timeout; may or may not succeed, but should not throw.
            bool result = NetClientHelper.IsInternetAvailable(timeoutMs: 500);
            result.GetType().Should().Be(typeof(bool));
        }

        [Fact]
        public void GetPublicIpAddress_WithShortTimeout_ShouldNotThrow()
        {
            // Short timeout; may return null or an IP, but should not throw.
            string? ip = NetClientHelper.GetPublicIpAddress(timeoutMs: 1000);
            // Accept null (no network / timeout) or a valid IP string.
            // Just verify it doesn't throw an exception.
        }

        [Fact]
        public void GetPublicIpAddress_NegativeTimeout_ShouldNotThrow()
        {
            // Edge case: negative timeout should not cause an unhandled exception.
            string? ip = NetClientHelper.GetPublicIpAddress(timeoutMs: -1);
            // Accept any graceful outcome — it may succeed or fail.
        }
    }
}
