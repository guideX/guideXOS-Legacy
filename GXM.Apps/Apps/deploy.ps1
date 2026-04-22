# Deploy GXM apps to guideXOS Ramdisk and rebuild ISO

Write-Host "GXM App Deployment Script" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build all apps
Write-Host "Step 1: Building all GXM apps..." -ForegroundColor Yellow
.\build-all.ps1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Aborting deployment." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Copying to Ramdisk..." -ForegroundColor Yellow

# Define paths
$ramdiskPath = "..\..\..\guideXOS\Ramdisk\Programs"

# Create Ramdisk Programs directory if it doesn't exist
if (!(Test-Path $ramdiskPath)) {
    Write-Host "Creating Ramdisk Programs directory..." -ForegroundColor White
    New-Item -ItemType Directory -Path $ramdiskPath -Force | Out-Null
}

# Copy all .gxm files
$gxmFiles = Get-ChildItem -Filter "*.gxm"

if ($gxmFiles.Count -eq 0) {
    Write-Host "No .gxm files found! Run build-all.ps1 first." -ForegroundColor Red
    exit 1
}

foreach ($file in $gxmFiles) {
    Write-Host "  Copying $($file.Name)..." -ForegroundColor White
    Copy-Item $file.FullName -Destination $ramdiskPath -Force
}

Write-Host "  $($gxmFiles.Count) files copied to Ramdisk" -ForegroundColor Green
Write-Host ""

# Step 3: Rebuild guideXOS ISO
Write-Host "Step 3: Rebuilding guideXOS ISO..." -ForegroundColor Yellow
Write-Host "  (This may take a few minutes...)" -ForegroundColor Gray

$originalPath = Get-Location
Set-Location "..\..\..\guideXOS"

if (Test-Path "build.ps1") {
    .\build.ps1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ISO rebuild successful!" -ForegroundColor Green
    } else {
        Write-Host "  ISO rebuild failed!" -ForegroundColor Red
        Set-Location $originalPath
        exit 1
    }
} else {
    Write-Host "  WARNING: build.ps1 not found in guideXOS directory" -ForegroundColor Red
    Write-Host "  Please manually rebuild the ISO" -ForegroundColor Yellow
}

Set-Location $originalPath

Write-Host ""
Write-Host "=========================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Apps deployed:" -ForegroundColor Cyan
foreach ($file in $gxmFiles) {
    Write-Host "  ? $($file.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Boot guideXOS in your VM or emulator" -ForegroundColor White
Write-Host "  2. Access apps from Start Menu or console" -ForegroundColor White
Write-Host "  3. From console: cd Programs && notepad" -ForegroundColor White
