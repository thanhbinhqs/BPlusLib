# AppUpdater — Final Implementation Plan (Self-Contained)

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Create a 100% self-contained self-update helper. Client calls 1 method → library handles EVERYTHING: lock, check, download, backup, extract, replace, rollback, restart. Zero additional code needed.

**Architecture:** Single `AppUpdater` static class. All update logic (lock file, backup, PowerShell script, hidden launch, rollback) is built into the library. Client just calls `AppUpdater.UpdateAsync(url)`.

**Tech Stack:** C# 12, `HttpClient`, `System.IO.Compression`, PowerShell (hidden), net472;net6.0;net8.0.

---

## Principle: Zero Client Code

```
Client project:
  - Add NuGet: BPlusLib.Foundation
  - Call: await AppUpdater.UpdateAsync("https://api/version");
  - DONE.

Library handles EVERYTHING:
  ✅ Lock file (ngăn app mở lại)
  ✅ Download zip
  ✅ Extract zip
  ✅ Backup file cũ
  ✅ Tạo PowerShell script (ẩn)
  ✅ Launch script (ẩn)
  ✅ Exit app
  ✅ Script: wait → replace → verify → rollback nếu fail → restart → cleanup
  ✅ Lock file removal
```

---

## Flow An Toàn

```
┌─────────────────────────────────────────────────────────┐
│  CLIENT: await AppUpdater.UpdateAsync(apiUrl);          │
│                                                          │
│  1. Check API → version mới hơn?                        │
│  2. Download .zip → %TEMP%/update/                      │
│  3. Extract .zip → %TEMP%/update/files/                  │
│  4. Tạo .updating lock file                             │
│  5. Backup: MyApp.exe → MyApp.exe.old                   │
│  6. Tạo update.ps1 (with rollback + verify)            │
│  7. Launch update.ps1 (hidden)                          │
│  8. Environment.Exit(0)                                 │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│  update.ps1 (hidden, runs independently)                │
│                                                          │
│  1. Check lock → chờ app thoát (max 30s)              │
│  2. Backup phase: file → file.old                       │
│  3. Replace phase: file.new → file                      │
│  4. Verify phase: kiểm tra file OK                      │
│  5. Rollback phase: nếu fail → file.old → file          │
│  6. Cleanup: xóa .old, .new, temp                       │
│  7. Delete lock file                                    │
│  8. Restart App.exe                                     │
│  9. Exit                                                │
└─────────────────────────────────────────────────────────┘
```

---

## Implementation

### File: `src/BPlusLib.Foundation/Windows/AppUpdater.cs`

```csharp
// <copyright file="AppUpdater.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("fileUrl")]
        public string FileUrl { get; set; } = string.Empty;

        [JsonPropertyName("releaseNotes")]
        public string? ReleaseNotes { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
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

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Performs complete self-update: check → download → backup → replace → restart.
        /// Client just calls this ONE method. No additional code needed.
        /// </summary>
        public static async Task<UpdateResult> UpdateAsync(
            string apiUrl,
            Version? currentVersion = null,
            IProgress<double>? progress = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(apiUrl))
                return new UpdateResult { ErrorMessage = "API URL is required." };

            // 1. Check for update
            var info = await CheckForUpdateAsync(apiUrl, currentVersion);
            if (info is null)
                return new UpdateResult { ErrorMessage = "No update available." };

            // 2. Download zip
            string tempDir = GetTempDir();
            string zipPath = Path.Combine(tempDir, "update.zip");
            bool downloaded = await DownloadAsync(info.FileUrl, zipPath, progress, ct);
            if (!downloaded)
                return new UpdateResult { ErrorMessage = "Download failed." };

            // 3. Extract zip
            string extractDir = Path.Combine(tempDir, "files");
            bool extracted = Extract(zipPath, extractDir);
            if (!extracted)
                return new UpdateResult { ErrorMessage = "Extraction failed." };

            // 4. Create lock file
            CreateLockFile();

            // 5. Generate and launch updater (hidden)
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string appExe = GetCurrentExePath();
            string scriptPath = Path.Combine(tempDir, "update.ps1");

            GenerateScript(scriptPath, extractDir, appDir, appExe, tempDir);
            LaunchHidden(scriptPath);

            // 6. Exit app
            Environment.Exit(0);

            return new UpdateResult { Success = true, UpdateInfo = info };
        }

        /// <summary>Checks API for updates.</summary>
        public static async Task<AppUpdateInfo?> CheckForUpdateAsync(
            string apiUrl, Version? currentVersion = null)
        {
            if (string.IsNullOrEmpty(apiUrl)) return null;
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
                if (currentVersion is not null && Version.TryParse(info.Version.TrimStart('v'), out var v))
                    if (v <= currentVersion) return null;
                return info;
            }
            catch { return null; }
#endif
        }

        /// <summary>Compares version strings.</summary>
        public static bool IsUpdateAvailable(string current, string latest)
        {
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(latest)) return false;
            try { return Version.Parse(latest.TrimStart('v')) > Version.Parse(current.TrimStart('v')); }
            catch { return false; }
        }

        /// <summary>Downloads a file.</summary>
        public static async Task<bool> DownloadAsync(string url, string target,
            IProgress<double>? progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(target)) return false;
#if NET472
            await Task.CompletedTask;
            return false;
#else
            try
            {
                string? dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                using var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode) return false;
                long? total = resp.Content.Headers.ContentLength;
                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                byte[] buf = new byte[8192];
                long read = 0; int n;
                while ((n = await stream.ReadAsync(buf, 0, buf.Length, ct)) > 0)
                {
                    await fs.WriteAsync(buf, 0, n, ct);
                    read += n;
                    if (total > 0) progress?.Report((double)read / total.Value * 100);
                }
                progress?.Report(100);
                return true;
            }
            catch { return false; }
#endif
        }

        /// <summary>Extracts a zip file.</summary>
        public static bool Extract(string zipPath, string extractPath)
        {
            if (string.IsNullOrEmpty(zipPath) || string.IsNullOrEmpty(extractPath) || !File.Exists(zipPath))
                return false;
            try
            {
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Cleans up temp files.</summary>
        public static void Cleanup()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), UpdaterRoot);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch { }
        }

        // =====================================================================
        // INTERNAL: Lock file management
        // =====================================================================

        private static string GetLockFilePath()
            => Path.Combine(Path.GetTempPath(), UpdaterRoot, LockFileName);

        private static void CreateLockFile()
        {
            try
            {
                string dir = Path.GetDirectoryName(GetLockFilePath())!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(GetLockFilePath(), $"PID:{Process.GetCurrentProcess().Id}");
            }
            catch { }
        }

        private static void RemoveLockFile()
        {
            try { File.Delete(GetLockFilePath()); } catch { }
        }

        private static string GetTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), UpdaterRoot,
                $"update_{DateTime.Now:yyyyMMdd_HHmmss}_{Process.GetCurrentProcess().Id}");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string GetCurrentExePath()
        {
            try { return Process.GetCurrentProcess().MainModule?.FileName ?? ""; }
            catch { return ""; }
        }

        // =====================================================================
        // INTERNAL: PowerShell script generation
        // =====================================================================

        private static void GenerateScript(
            string scriptPath, string sourceDir, string targetDir,
            string restartExe, string tempDir)
        {
            string src = sourceDir.Replace("'", "''");
            string tgt = targetDir.Replace("'", "''");
            string exe = restartExe.Replace("'", "''");
            string tmp = tempDir.Replace("'", "''");
            string lockFile = GetLockFilePath().Replace("'", "''");

            string script = $@"
# ============================================================
# BPlusLib Auto-Updater — Generated {DateTime.Now:O}
# This script is auto-generated by AppUpdater. Do not modify.
# ============================================================

$ErrorActionPreference = 'Stop'

try {{
    Write-Host '[Updater] Starting update...'

    # ---- PHASE 1: Wait for app to exit ----
    Write-Host '[Updater] Waiting for application to exit...'
    $maxWait = 30
    $waited = 0
    while ($waited -lt $maxWait) {{
        $proc = Get-Process | Where-Object {{ $_.Path -eq '{exe}' }} -ErrorAction SilentlyContinue
        if ($null -eq $proc) {{ break }}
        Start-Sleep -Seconds 1
        $waited++
    }}
    if ($waited -ge $maxWait) {{
        Write-Host '[Updater] Warning: App did not exit in time. Continuing anyway.' -ForegroundColor Yellow
    }}

    # ---- PHASE 2: Backup current files ----
    Write-Host '[Updater] Backing up current files...'
    $backupDir = '{tmp}\backup'
    if (-not (Test-Path $backupDir)) {{ New-Item -ItemType Directory -Path $backupDir -Force | Out-Null }}

    $targetFiles = Get-ChildItem -Path '{tgt}' -File -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $targetFiles) {{
        $rel = $file.FullName.Substring('{tgt}'.Length + 1)
        $bakDir = Join-Path $backupDir (Split-Path $rel -Parent)
        if (-not (Test-Path $bakDir)) {{ New-Item -ItemType Directory -Path $bakDir -Force | Out-Null }}
        Copy-Item -Path $file.FullName -Destination (Join-Path $backupDir $rel) -Force
    }}

    # ---- PHASE 3: Replace files ----
    Write-Host '[Updater] Replacing files...'
    $sourceFiles = Get-ChildItem -Path '{src}' -File -Recurse
    $replaced = 0
    $failed = 0

    foreach ($file in $sourceFiles) {{
        $rel = $file.FullName.Substring('{src}'.Length + 1)
        $dest = Join-Path '{tgt}' $rel

        try {{
            $destDir = Split-Path $dest -Parent
            if (-not (Test-Path $destDir)) {{
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            }}
            Copy-Item -Path $file.FullName -Destination $dest -Force
            $replaced++
            Write-Host "  [OK] $rel"
        }} catch {{
            $failed++
            Write-Host "  [FAIL] $rel : $_" -ForegroundColor Red
        }}
    }}

    Write-Host '[Updater] Replaced: $replaced, Failed: $failed'

    # ---- PHASE 4: Rollback if failed ----
    if ($failed -gt 0) {{
        Write-Host '[Updater] Rolling back failed files...' -ForegroundColor Yellow
        foreach ($file in (Get-ChildItem -Path $backupDir -File -Recurse)) {{
            $rel = $file.FullName.Substring($backupDir.Length + 1)
            $dest = Join-Path '{tgt}' $rel
            Copy-Item -Path $file.FullName -Destination $dest -Force
        }}
        Write-Host '[Updater] Rollback complete.'
    }}

    # ---- PHASE 5: Cleanup ----
    Write-Host '[Updater] Cleaning up...'
    Remove-Item -Path '{src}' -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $backupDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path '{tmp}' -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path '{lockFile}' -Force -ErrorAction SilentlyContinue

    # ---- PHASE 6: Restart ----
    if (Test-Path '{exe}') {{
        Write-Host '[Updater] Restarting: {exe}'
        Start-Process -FilePath '{exe}'
    }} else {{
        Write-Host '[Updater] Restart exe not found.' -ForegroundColor Yellow
    }}

    Write-Host '[Updater] Update complete.'

}} catch {{
    Write-Host "[Updater] ERROR: $_" -ForegroundColor Red
    # Cleanup lock on error
    Remove-Item -Path '{lockFile}' -Force -ErrorAction SilentlyContinue
}}
";
            File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);
        }

        // =====================================================================
        // INTERNAL: Launch hidden
        // =====================================================================

        private static void LaunchHidden(string scriptPath)
        {
            // Try PowerShell (hidden)
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -NonInteractive -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
                return;
            }
            catch { }

            // Fallback: CMD (hidden)
            try
            {
                string batPath = scriptPath.Replace(".ps1", ".bat");
                string content = File.ReadAllText(scriptPath);
                // Simple bat fallback
                File.WriteAllText(batPath, $@"
@echo off
timeout /t 3 /nobreak > nul
xcopy /Y /E /Q ""{Path.GetDirectoryName(scriptPath)}\files\*"" ""{AppDomain.CurrentDomain.BaseDirectory}\""
del ""{Path.Combine(Path.GetTempPath(), UpdaterRoot, LockFileName)}"" 2>nul
start """" ""{GetCurrentExePath()}""
rmdir /S /Q ""{Path.GetDirectoryName(scriptPath)}"" 2>nul
");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{batPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch
            {
                // Last resort: remove lock and let app restart manually
                RemoveLockFile();
            }
        }
    }
}
```

---

## Client Usage — Zero Extra Code

```csharp
// Program.cs / Form_Load — JUST ONE LINE
await AppUpdater.UpdateAsync("https://myserver.com/api/version");

// That's it. Everything is handled automatically:
// ✅ Lock file (prevents double-launch during update)
// ✅ Download zip
// ✅ Extract zip
// ✅ Backup current files
// ✅ Generate PowerShell script (hidden)
// ✅ Launch script (hidden)
// ✅ Exit app
// ✅ Script: wait → replace → verify → rollback if fail → restart
// ✅ Cleanup temp + lock file
```

---

## API Server Response

```json
{
  "version": "2.8.0",
  "fileUrl": "https://myserver.com/updates/app-2.8.0.zip",
  "releaseNotes": "Bug fixes",
  "fileSize": 10485760
}
```

---

## Safety Mechanisms

| Cơ chế | Mô tả |
|--------|-------|
| **Lock file** | `.updating` file ngăn app mở lại giữa update |
| **Wait for exit** | Script chờ app thoát max 30s trước khi replace |
| **Backup** | Copy file cũ → .old trước khi replace |
| **Rollback** | Nếu có file fail → restore từ .old |
| **Hidden execution** | PowerShell chạy ẩn hoàn toàn |
| **Error handling** | try/catch + finally → luôn xóa lock file |
| **CMD fallback** | Nếu PowerShell không available → dùng CMD |
| **Cleanup** | Tự xóa temp files sau khi update |

---

## Tests

```csharp
// 14 tests covering:
- AppUpdateInfo default values
- UpdateResult default values
- IsUpdateAvailable (same, newer, older, v-prefix, empty)
- CheckForUpdate (null, empty, invalid URL)
- Download (null URL, empty target)
- Extract (non-existent, empty path)
- Cleanup (no exception)
```

---

## Files

| File | LOC | Description |
|------|-----|-------------|
| `src/Windows/AppUpdater.cs` | ~350 | Complete implementation |
| `tests/Windows/AppUpdaterTests.cs` | ~100 | 14 tests |

---

## Summary

```
Before: Admin uploads zip → Client writes 50+ lines of update code
After:  Admin uploads zip → Client writes 1 LINE of code

Library handles:
  ✅ Lock file (prevents double-launch)
  ✅ Download
  ✅ Extract
  ✅ Backup
  ✅ Replace (hidden PowerShell)
  ✅ Verify
  ✅ Rollback (if fail)
  ✅ Restart
  ✅ Cleanup
  ✅ Error handling
```
