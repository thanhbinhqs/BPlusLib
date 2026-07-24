// <copyright file="AutoUpdateHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
#if !NET472
using System.Net.Http;
#endif
using System.Threading;
using System.Threading.Tasks;

#if !NET472
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace BPlusLib.Foundation.Windows
{
    /// <summary>Information about an available update.</summary>
    public sealed class UpdateInfo
    {
        /// <summary>Version string (e.g. "v2.7.0").</summary>
        public string Version { get; init; } = string.Empty;
        /// <summary>Release notes in markdown.</summary>
        public string ReleaseNotes { get; init; } = string.Empty;
        /// <summary>When the release was published.</summary>
        public DateTime PublishedAt { get; init; }
        /// <summary>Direct download URL for the asset.</summary>
        public string DownloadUrl { get; init; } = string.Empty;
        /// <summary>Asset file size in bytes.</summary>
        public long FileSize { get; init; }
        /// <summary>Whether this version is newer than the specified current version.</summary>
        public bool IsNewerThanCurrent { get; init; }
    }

    /// <summary>
    /// Provides auto-update functionality via GitHub Releases API.
    /// All methods are thread-safe and gracefully return null/false on error.
    /// </summary>
    public static class AutoUpdateHelper
    {
#if !NET472
        private static readonly HttpClient _client = new();
#endif

        /// <summary>
        /// Checks for the latest release on GitHub.
        /// </summary>
        /// <param name="owner">GitHub repository owner.</param>
        /// <param name="repo">GitHub repository name.</param>
        /// <param name="currentVersion">Current app version for comparison.</param>
        /// <returns>Update info if a newer version is available, or null on error.</returns>
        public static async Task<UpdateInfo?> CheckForUpdateAsync(
            string owner, string repo, Version? currentVersion = null)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                return null;

#if NET472
            // System.Text.Json is not available on net472 without additional packages.
            await Task.CompletedTask;
            return null;
#else
            try
            {
                string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "BPlusLib.AutoUpdate");
                request.Headers.Add("Accept", "application/vnd.github.v3+json");

                using var response = await _client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);
                if (release is null) return null;

                // Parse version
                string versionTag = release.TagName ?? "";
                string versionStr = versionTag.StartsWith("v") ? versionTag.Substring(1) : versionTag;
                Version? releaseVersion = Version.TryParse(versionStr, out var v) ? v : null;
                Version? current = currentVersion ?? GetCurrentVersion();

                bool isNewer = releaseVersion is not null && current is not null && releaseVersion > current;

                string downloadUrl = "";
                long fileSize = 0;
                if (release.Assets is not null && release.Assets.Length > 0)
                {
                    // Find the first downloadable asset (e.g. .msi, .exe, .zip)
                    var asset = release.Assets[0];
                    downloadUrl = asset.BrowserDownloadUrl ?? "";
                    fileSize = asset.Size;
                }

                return new UpdateInfo
                {
                    Version = versionTag,
                    ReleaseNotes = release.Body ?? "",
                    PublishedAt = release.PublishedAt,
                    DownloadUrl = downloadUrl,
                    FileSize = fileSize,
                    IsNewerThanCurrent = isNewer,
                };
            }
            catch
            {
                return null;
            }
#endif
        }

        /// <summary>
        /// Compares two version strings to determine if an update is available.
        /// </summary>
        public static bool IsUpdateAvailable(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion))
                return false;

            try
            {
                string cv = currentVersion.TrimStart('v');
                string lv = latestVersion.TrimStart('v');
                var current = Version.Parse(cv);
                var latest = Version.Parse(lv);
                return latest > current;
            }
            catch { return false; }
        }

        /// <summary>
        /// Downloads a file from a URL to a local path with progress reporting.
        /// </summary>
        public static async Task<bool> DownloadUpdateAsync(
            string downloadUrl, string targetPath,
            IProgress<double>? progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(targetPath))
                return false;

#if NET472
            // System.Net.Http.HttpClient is not directly used on net472.
            await Task.CompletedTask;
            return false;
#else
            try
            {
                using var response = await _client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode) return false;

                long? totalBytes = response.Content.Headers.ContentLength;
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                byte[] buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, bytesRead, ct);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                        progress?.Report((double)totalRead / totalBytes.Value * 100);
                }

                progress?.Report(100);
                return true;
            }
            catch { return false; }
#endif
        }

        /// <summary>
        /// Launches an installer file.
        /// </summary>
        public static bool LaunchInstaller(string installerPath)
        {
            if (string.IsNullOrEmpty(installerPath) || !File.Exists(installerPath))
                return false;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                });
                return true;
            }
            catch { return false; }
        }

        private static Version? GetCurrentVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly is null) return null;
                var version = assembly.GetName().Version;
                return version;
            }
            catch { return null; }
        }

#if !NET472
        // Internal JSON models for GitHub API
        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }

            [JsonPropertyName("published_at")]
            public DateTime PublishedAt { get; set; }

            [JsonPropertyName("assets")]
            public GitHubAsset[]? Assets { get; set; }
        }

        private class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }

            [JsonPropertyName("size")]
            public long Size { get; set; }
        }
#endif
    }
}
