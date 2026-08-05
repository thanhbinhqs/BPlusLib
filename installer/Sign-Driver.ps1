<#
.SYNOPSIS
    Tạo self-signed certificate và ký số cho BinhVSerial driver.
.DESCRIPTION
    - Tạo certificate self-signed cho code signing
    - Ký .sys, .cat, .inf files
    - Trusted certificate trên máy local
.NOTES
    Chạy PowerShell As Administrator
#>

param(
    [string]$CertSubject = "CN=BinhVSerial Development",
    [string]$CertStore = "Cert:\LocalMachine\TrustedPublisher",
    [string]$RootStore = "Cert:\LocalMachine\Root",
    [string]$DriverPath = ".\src\drivers",
    [string]$OutputCert = ".\installer\BinhVSerialTest.cer"
)

$ErrorActionPreference = "Stop"

Write-Host "=== BinhVSerial Driver Signing ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. Tạo Self-Signed Certificate
# ============================================
Write-Host "[1/5] Creating self-signed certificate..." -ForegroundColor Yellow

$existingCert = Get-ChildItem -Path Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -eq $CertSubject -and $_.HasPrivateKey }

if ($existingCert) {
    Write-Host "  Certificate already exists: $($existingCert.Thumbprint)" -ForegroundColor Green
    $cert = $existingCert
} else {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $CertSubject `
        -CertStoreLocation Cert:\LocalMachine\My `
        -NotAfter (Get-Date).AddYears(5) `
        -KeyUsage DigitalSignature `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256

    Write-Host "  Created: $($cert.Thumbprint)" -ForegroundColor Green
}

# Export certificate (không có private key) để distribute
$cerPath = Join-Path (Split-Path $OutputCert) "BinhVSerialTest.cer"
New-Item -ItemType Directory -Force -Path (Split-Path $cerPath) | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null
Write-Host "  Exported: $cerPath" -ForegroundColor Green

# ============================================
# 2. Trusted Certificate trên máy local
# ============================================
Write-Host "[2/5] Trusting certificate..." -ForegroundColor Yellow

# Trusted Publishers
$inTrustedPub = Get-ChildItem -Path $CertStore -ErrorAction SilentlyContinue |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint }

if (-not $inTrustedPub) {
    Copy-Item -Path $cert.PSPath -Destination $CertStore -Force
    Write-Host "  Added to TrustedPublisher store" -ForegroundColor Green
} else {
    Write-Host "  Already in TrustedPublisher store" -ForegroundColor DarkGray
}

# Root CA (cho self-signed)
$inRoot = Get-ChildItem -Path $RootStore -ErrorAction SilentlyContinue |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint }

if (-not $inRoot) {
    Copy-Item -Path $cert.PSPath -Destination $RootStore -Force
    Write-Host "  Added to Root store" -ForegroundColor Green
} else {
    Write-Host "  Already in Root store" -ForegroundColor DarkGray
}

# ============================================
# 3. Tìm driver files cần ký
# ============================================
Write-Host "[3/5] Finding driver files..." -ForegroundColor Yellow

$driverFiles = @()

# Tìm .sys files
$sysFiles = Get-ChildItem -Path $DriverPath -Recurse -Filter "*.sys" -ErrorAction SilentlyContinue
if ($sysFiles) { $driverFiles += $sysFiles }

# Tìm .inf files
$infFiles = Get-ChildItem -Path $DriverPath -Recurse -Filter "*.inf" -ErrorAction SilentlyContinue
if ($infFiles) { $driverFiles += $infFiles }

# Tìm .cat files (nếu đã có)
$catFiles = Get-ChildItem -Path $DriverPath -Recurse -Filter "*.cat" -ErrorAction SilentlyContinue
if ($catFiles) { $driverFiles += $catFiles }

Write-Host "  Found $($driverFiles.Count) files to sign" -ForegroundColor Green

if ($driverFiles.Count -eq 0) {
    Write-Host "  No driver files found. Build driver first." -ForegroundColor Red
    exit 1
}

# ============================================
# 4. Ký số files
# ============================================
Write-Host "[4/5] Signing files..." -ForegroundColor Yellow

# Kiểm tra signtool
$signtoolPath = $null
$possiblePaths = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x86\signtool.exe",
    "${env:ProgramFiles}\Windows Kits\10\bin\x64\signtool.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $signtoolPath = $path
        break
    }
}

if (-not $signtoolPath) {
    # Thử tìm trong PATH
    $signtoolPath = Get-Command signtool.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
}

if (-not $signtoolPath) {
    Write-Host "  signtool.exe not found. Install Windows SDK." -ForegroundColor Red
    Write-Host "  Download: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/" -ForegroundColor Yellow
    exit 1
}

Write-Host "  Using: $signtoolPath" -ForegroundColor DarkGray

# Ký từng file
foreach ($file in $driverFiles) {
    Write-Host "  Signing: $($file.Name)" -ForegroundColor White

    $result = & $signtoolPath sign `
        /v `
        /s My `
        /sha1 $cert.Thumbprint `
        /fd sha256 `
        /tr http://timestamp.digicert.com `
        /td sha256 `
        $file.FullName 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "    OK" -ForegroundColor Green
    } else {
        Write-Host "    FAILED: $result" -ForegroundColor Red
    }
}

# ============================================
# 5. Tạo CAT file (nếu cần)
# ============================================
Write-Host "[5/5] Creating catalog file..." -ForegroundColor Yellow

# Kiểm tra Inf2Cat
$inf2catPath = $null
foreach ($path in $possiblePaths) {
    $inf2catCandidate = Split-Path $path | Join-Path -ChildPath "Inf2Cat.exe"
    if (Test-Path $inf2catCandidate) {
        $inf2catPath = $inf2catCandidate
        break
    }
}

if ($inf2catPath) {
    foreach ($inf in $infFiles) {
        $catName = [System.IO.Path]::ChangeExtension($inf.Name, ".cat")
        $catPath = Join-Path $inf.DirectoryName $catName

        Write-Host "  Creating catalog: $catName" -ForegroundColor White

        $result = & $inf2catPath `
            /driver:$($inf.DirectoryName) `
            /os:10_x86,10_x64,10_A64,7_x86,7_x64,8_x86,8_x64 `
            2>&1

        if ($LASTEXITCODE -eq 0 -and (Test-Path $catPath)) {
            # Sign the .cat file
            & $signtoolPath sign `
                /v `
                /s My `
                /sha1 $cert.Thumbprint `
                /fd sha256 `
                /tr http://timestamp.digicert.com `
                /td sha256 `
                $catPath

            Write-Host "    Created and signed: $catName" -ForegroundColor Green
        } else {
            Write-Host "    Inf2Cat failed (OK for dev): $result" -ForegroundColor DarkGray
        }
    }
} else {
    Write-Host "  Inf2Cat.exe not found — skipping catalog creation" -ForegroundColor DarkGray
}

# ============================================
# Summary
# ============================================
Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Certificate Thumbprint: $($cert.Thumbprint)" -ForegroundColor White
Write-Host "Certificate File: $cerPath" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Copy $cerPath to target machines" -ForegroundColor White
Write-Host "  2. Import certificate to TrustedPublisher + Root stores" -ForegroundColor White
Write-Host "  3. Enable test signing: bcdedit /set testsigning on" -ForegroundColor White
Write-Host "  4. Reboot" -ForegroundColor White
Write-Host ""
Write-Host "To sign on another machine:" -ForegroundColor Yellow
Write-Host "  Import-BinhVSerialCert.ps1" -ForegroundColor White
