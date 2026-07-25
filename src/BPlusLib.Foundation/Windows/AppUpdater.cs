// <copyright file="AppUpdater.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
#if !NET472
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>Update manifest from server API.</summary>
    public sealed class AppUpdateInfo
    {
        #if !NET472

        [System.Text.Json.Serialization.JsonPropertyName("version")]

        #endif
        public string Version { get; set; } = string.Empty;
        #if !NET472

        [System.Text.Json.Serialization.JsonPropertyName("fileUrl")]

        #endif
        public string FileUrl { get; set; } = string.Empty;
        #if !NET472

        [System.Text.Json.Serialization.JsonPropertyName("releaseNotes")]

        #endif
        public string? ReleaseNotes { get; set; }
        #if !NET472

        [System.Text.Json.Serialization.JsonPropertyName("fileSize")]

        #endif
        public long FileSize { get; set; }
        #if !NET472

        [System.Text.Json.Serialization.JsonPropertyName("sha256")]

        #endif
        public string? Sha256 { get; set; }
    }

    /// <summary>Result of an update operation.</summary>
    public sealed class UpdateResult
    {
        public bool Success { get; init; }
        public AppUpdateInfo? UpdateInfo { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Self-contained self-update helper. Client calls 1 method — library handles everything.
    /// </summary>
    public static class AppUpdater
    {
#if !NET472
        private static readonly HttpClient _client = new();
#endif
        private const string UpdaterRoot = "BPlusLib_Updater";
        private const string LockFileName = ".updating";

        /// <summary>
        /// Performs complete self-update: check → download → backup → replace → restart.
        /// If <paramref name="currentVersion"/> is null, version is auto-detected from the running assembly.
        /// </summary>
        public static async Task<UpdateResult> UpdateAsync(
            string apiUrl, Version? currentVersion = null,
            IProgress<double>? progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(apiUrl))
                return new UpdateResult { ErrorMessage = "API URL is required." };

            // Auto-detect version if not provided
            currentVersion ??= GetCurrentVersion();

            var info = await CheckForUpdateAsync(apiUrl, currentVersion);
            if (info is null)
                return new UpdateResult { ErrorMessage = "No update available." };

            string tempDir = GetTempDir();
            string zipPath = Path.Combine(tempDir, "update.zip");
            bool downloaded = await DownloadAsync(info.FileUrl, zipPath, progress, ct);
            if (!downloaded)
                return new UpdateResult { ErrorMessage = "Download failed.", UpdateInfo = info };

            string extractDir = Path.Combine(tempDir, "files");
            bool extracted = Extract(zipPath, extractDir);
            if (!extracted)
                return new UpdateResult { ErrorMessage = "Extraction failed.", UpdateInfo = info };

            CreateLockFile();
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string appExe = GetCurrentExePath();
            string scriptPath = Path.Combine(tempDir, "update.ps1");
            GenerateScript(scriptPath, extractDir, appDir, appExe, tempDir);
            LaunchHidden(scriptPath);
            Environment.Exit(0);

            return new UpdateResult { Success = true, UpdateInfo = info };
        }

        /// <summary>
        /// Checks API for updates. If <paramref name="currentVersion"/> is null, version is auto-detected.
        /// </summary>
        public static async Task<AppUpdateInfo?> CheckForUpdateAsync(
            string apiUrl, Version? currentVersion = null)
        {
            if (string.IsNullOrEmpty(apiUrl)) return null;

            // Auto-detect version if not provided
            currentVersion ??= GetCurrentVersion();
#if NET472
            await Task.CompletedTask;
            return null;
#else
            try
            {
                using var response = await _client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode) return null;
                string json = await response.Content.ReadAsStringAsync();
                var info = JsonSerializer.Deserialize<AppUpdateInfo>(json);
                if (info is null || string.IsNullOrEmpty(info.Version)) return null;
                if (currentVersion is not null)
                {
                    string lv = info.Version.TrimStart('v', 'V');
                    if (Version.TryParse(lv, out var latest) && latest <= currentVersion)
                        return null;
                }
                return info;
            }
            catch { return null; }
#endif
        }

        /// <summary>Compares version strings.</summary>
        public static bool IsUpdateAvailable(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion)) return false;
            try { return Version.Parse(latestVersion.TrimStart('v', 'V')) > Version.Parse(currentVersion.TrimStart('v', 'V')); }
            catch { return false; }
        }

        /// <summary>Downloads a file with progress.</summary>
        public static async Task<bool> DownloadAsync(string url, string targetPath,
            IProgress<double>? progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(targetPath)) return false;
#if NET472
            await Task.CompletedTask;
            return false;
#else
            try
            {
                string? dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode) return false;
                long? total = resp.Content.Headers.ContentLength;
                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                byte[] buf = new byte[8192]; long read = 0; int n;
                while ((n = await stream.ReadAsync(buf, 0, buf.Length, ct)) > 0)
                { await fs.WriteAsync(buf, 0, n, ct); read += n; if (total > 0) progress?.Report((double)read / total.Value * 100); }
                progress?.Report(100);
                return true;
            }
            catch { return false; }
#endif
        }

        /// <summary>Extracts a zip file.</summary>
        public static bool Extract(string zipPath, string extractPath)
        {
            if (string.IsNullOrEmpty(zipPath) || string.IsNullOrEmpty(extractPath) || !File.Exists(zipPath)) return false;
            try { if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true); Directory.CreateDirectory(extractPath); ZipFile.ExtractToDirectory(zipPath, extractPath); return true; }
            catch { return false; }
        }

        /// <summary>Cleans up temp files.</summary>
        public static void Cleanup()
        {
            try { string root = Path.Combine(Path.GetTempPath(), UpdaterRoot); if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch { }
        }

        private static string GetLockFilePath() => Path.Combine(Path.GetTempPath(), UpdaterRoot, LockFileName);

        private static void CreateLockFile()
        {
            try { string dir = Path.GetDirectoryName(GetLockFilePath())!; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); File.WriteAllText(GetLockFilePath(), $"PID:{System.Diagnostics.Process.GetCurrentProcess().Id}"); }
            catch { }
        }

        private static string GetTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), UpdaterRoot, $"update_{DateTime.Now:yyyyMMdd_HHmmss}_{System.Diagnostics.Process.GetCurrentProcess().Id}");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Auto-detects current app version. Priority:
        /// 1. Entry assembly version (the actual running app)
        /// 2. Executing assembly version (this library)
        /// 3. File version from the EXE on disk
        /// </summary>
        public static Version? GetCurrentVersion()
        {
            try
            {
                // Try entry assembly first — the actual running application
                var entry = Assembly.GetEntryAssembly();
                if (entry != null)
                {
                    var v = entry.GetName().Version;
                    if (v != null && (v.Major > 0 || v.Minor > 0 || v.Build > 0)) return v;
                }
            }
            catch { }

            try
            {
                // Fallback: executing assembly (this library itself)
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null && (v.Major > 0 || v.Minor > 0 || v.Build > 0)) return v;
            }
            catch { }

            try
            {
                // Last resort: read FileVersion from the EXE on disk
                string exePath = GetCurrentExePath();
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var vi = FileVersionInfo.GetVersionInfo(exePath);
                    if (!string.IsNullOrEmpty(vi.FileVersion))
                        return Version.Parse(vi.FileVersion);
                }
            }
            catch { }

            return null;
        }

        private static string GetCurrentExePath()
        {
            try { return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? ""; }
            catch { return ""; }
        }

        private static void GenerateScript(string scriptPath, string sourceDir, string targetDir, string restartExe, string tempDir)
        {
            string src = sourceDir.Replace("'", "''");
            string tgt = targetDir.Replace("'", "''");
            string exe = restartExe.Replace("'", "''");
            string tmp = tempDir.Replace("'", "''");
            string lockFile = GetLockFilePath().Replace("'", "''");

            // Use regular string to avoid C# interpolation issues with PowerShell $
            string script = "# BPlusLib Auto-Updater\n" +
                "$ErrorActionPreference = 'Stop'\n" +
                "try {\n" +
                "  Write-Host '[Updater] Waiting for app to exit...'\n" +
                "  $maxWait = 30\n" +
                "  $waited = 0\n" +
                "  while ($waited -lt $maxWait) {\n" +
                "    $proc = Get-Process | Where-Object { $_.Path -eq '" + exe + "' } -ErrorAction SilentlyContinue\n" +
                "    if ($null -eq $proc) { break }\n" +
                "    Start-Sleep -Seconds 1\n" +
                "    $waited++\n" +
                "  }\n" +
                "  Write-Host '[Updater] Backing up files...'\n" +
                "  $backupDir = '" + tmp + "\\backup'\n" +
                "  if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir -Force | Out-Null }\n" +
                "  Get-ChildItem -Path '" + tgt + "' -File -Recurse -ErrorAction SilentlyContinue | ForEach-Object {\n" +
                "    $rel = $_.FullName.Substring('" + tgt + "'.Length + 1)\n" +
                "    $bakDir = Join-Path $backupDir (Split-Path $rel -Parent)\n" +
                "    if (-not (Test-Path $bakDir)) { New-Item -ItemType Directory -Path $bakDir -Force | Out-Null }\n" +
                "    Copy-Item -Path $_.FullName -Destination (Join-Path $backupDir $rel) -Force\n" +
                "  }\n" +
                "  Write-Host '[Updater] Replacing files...'\n" +
                "  $replaced = 0\n" +
                "  $failed = 0\n" +
                "  Get-ChildItem -Path '" + src + "' -File -Recurse | ForEach-Object {\n" +
                "    $rel = $_.FullName.Substring('" + src + "'.Length + 1)\n" +
                "    $dest = Join-Path '" + tgt + "' $rel\n" +
                "    try {\n" +
                "      $destDir = Split-Path $dest -Parent\n" +
                "      if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }\n" +
                "      Copy-Item -Path $_.FullName -Destination $dest -Force\n" +
                "      $replaced++\n" +
                "    } catch { $failed++\n" +
                "    }\n" +
                "  }\n" +
                "  if ($failed -gt 0) {\n" +
                "    Write-Host '[Updater] Rolling back...'\n" +
                "    Get-ChildItem -Path $backupDir -File -Recurse | ForEach-Object {\n" +
                "      $rel = $_.FullName.Substring($backupDir.Length + 1)\n" +
                "      Copy-Item -Path $_.FullName -Destination (Join-Path '" + tgt + "' $rel) -Force\n" +
                "    }\n" +
                "  }\n" +
                "  Write-Host '[Updater] Cleanup...'\n" +
                "  Remove-Item -Path '" + src + "' -Recurse -Force -ErrorAction SilentlyContinue\n" +
                "  Remove-Item -Path $backupDir -Recurse -Force -ErrorAction SilentlyContinue\n" +
                "  Remove-Item -Path '" + tmp + "' -Recurse -Force -ErrorAction SilentlyContinue\n" +
                "  Remove-Item -Path '" + lockFile + "' -Force -ErrorAction SilentlyContinue\n" +
                "  if (Test-Path '" + exe + "') { Start-Process -FilePath '" + exe + "' }\n" +
                "  Write-Host '[Updater] Done.'\n" +
                "} catch { Remove-Item -Path '" + lockFile + "' -Force -ErrorAction SilentlyContinue }\n";

            File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);
        }

        private static void LaunchHidden(string scriptPath)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -NonInteractive -File \"{scriptPath}\"",
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                });
                return;
            }
            catch { }

            try
            {
                string batPath = scriptPath.Replace(".ps1", ".bat");
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string appExe = GetCurrentExePath();
                string srcDir = Path.Combine(Path.GetDirectoryName(scriptPath)!, "files");
                string lockFile = GetLockFilePath();
                File.WriteAllText(batPath, $"@echo off\ntimeout /t 3 /nobreak > nul\nxcopy /Y /E /Q \"{srcDir}\\*\" \"{appDir}\\\"\ndel \"{lockFile}\" 2>nul\nstart \"\" \"{appExe}\"\nrmdir /S /Q \"{Path.GetDirectoryName(scriptPath)}\" 2>nul\n");
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe", Arguments = $"/C \"{batPath}\"",
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch { try { File.Delete(GetLockFilePath()); } catch { } }
        }
    }
}
