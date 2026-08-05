<#
.SYNOPSIS
    Import và trusted BinhVSerial certificate trên máy đích.
.DESCRIPTION
    - Import certificate vào TrustedPublisher
    - Import certificate vào Root CA
    - Bật test signing mode
.NOTES
    Chạy PowerShell As Administrator trên máy đích
    Copy BinhVSerialTest.cer cùng thư mục với script này
#>

param(
    [string]$CertFile = ".\BinhVSerialTest.cer",
    [switch]$EnableTestSigning
)

$ErrorActionPreference = "Stop"

Write-Host "=== BinhVSerial Certificate Import ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. Kiểm tra certificate file
# ============================================
if (-not (Test-Path $CertFile)) {
    Write-Host "Certificate file not found: $CertFile" -ForegroundColor Red
    Write-Host "Copy BinhVSerialTest.cer to this directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "[1/4] Importing certificate..." -ForegroundColor Yellow

# Import certificate
$cert = Import-Certificate -FilePath $CertFile -CertStoreLocation Cert:\LocalMachine\My

Write-Host "  Subject: $($cert.Subject)" -ForegroundColor White
Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor White

# ============================================
# 2. Trusted Publishers
# ============================================
Write-Host "[2/4] Adding to TrustedPublisher store..." -ForegroundColor Yellow

$existing = Get-ChildItem -Path Cert:\LocalMachine\TrustedPublisher -ErrorAction SilentlyContinue |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint }

if ($existing) {
    Write-Host "  Already trusted" -ForegroundColor DarkGray
} else {
    Copy-Item -Path $cert.PSPath -Destination Cert:\LocalMachine\TrustedPublisher -Force
    Write-Host "  Added to TrustedPublisher" -ForegroundColor Green
}

# ============================================
# 3. Root CA
# ============================================
Write-Host "[3/4] Adding to Root store..." -ForegroundColor Yellow

$existingRoot = Get-ChildItem -Path Cert:\LocalMachine\Root -ErrorAction SilentlyContinue |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint }

if ($existingRoot) {
    Write-Host "  Already in Root store" -ForegroundColor DarkGray
} else {
    Copy-Item -Path $cert.PSPath -Destination Cert:\LocalMachine\Root -Force
    Write-Host "  Added to Root store" -ForegroundColor Green
}

# ============================================
# 4. Test Signing (optional)
# ============================================
if ($EnableTestSigning) {
    Write-Host "[4/4] Enabling test signing..." -ForegroundColor Yellow

    $testSigning = bcdedit /enum {current} | Select-String "testsigning"
    if ($testSigning -match "Yes") {
        Write-Host "  Test signing already enabled" -ForegroundColor DarkGray
    } else {
        bcdedit /set testsigning on
        Write-Host "  Test signing enabled" -ForegroundColor Green
        Write-Host "  REBOOT REQUIRED" -ForegroundColor Yellow
    }
} else {
    Write-Host "[4/4] Skipping test signing (use -EnableTestSigning to enable)" -ForegroundColor DarkGray
}

# ============================================
# Verify
# ============================================
Write-Host ""
Write-Host "=== Verification ===" -ForegroundColor Cyan
Write-Host ""

# Check TrustedPublisher
$tpCount = (Get-ChildItem Cert:\LocalMachine\TrustedPublisher |
    Where-Object { $_.Subject -like "*BinhVSerial*" }).Count
Write-Host "TrustedPublisher: $tpCount BinhVSerial cert(s)" -ForegroundColor $(if ($tpCount -gt 0) { "Green" } else { "Red" })

# Check Root
$rootCount = (Get-ChildItem Cert:\LocalMachine\Root |
    Where-Object { $_.Subject -like "*BinhVSerial*" }).Count
Write-Host "Root store: $rootCount BinhVSerial cert(s)" -ForegroundColor $(if ($rootCount -gt 0) { "Green" } else { "Red" })

# Check test signing
$testSigningStatus = bcdedit /enum {current} 2>&1 | Select-String "testsigning"
$isTestSigning = $testSigningStatus -match "Yes"
Write-Host "Test signing: $(if ($isTestSigning) { 'Enabled' } else { 'Disabled' })" -ForegroundColor $(if ($isTestSigning) { "Green" } else { "Yellow" })

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Driver signing status:" -ForegroundColor White
Write-Host "  Certificate: TrustedPublisher + Root" -ForegroundColor Green
Write-Host "  Test signing: $(if ($isTestSigning) { 'ON' } else { 'OFF — run with -EnableTestSigning' })" -ForegroundColor $(if ($isTestSigning) { "Green" } else { "Yellow" })

if (-not $isTestSigning) {
    Write-Host ""
    Write-Host "To enable test signing:" -ForegroundColor Yellow
    Write-Host "  .\Import-BinhVSerialCert.ps1 -EnableTestSigning" -ForegroundColor White
    Write-Host "  Then reboot" -ForegroundColor White
}
