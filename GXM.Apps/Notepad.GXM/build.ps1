# Build Script for Notepad GXM Applications
# This script packages all notepad GUI scripts as GXM files

param(
    [string]$OutputDir = "..\..\guideXOS\Ramdisk\Programs"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building Notepad GXM Applications" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the GXM Packager tool
Write-Host "[1/4] Building GXM Packager..." -ForegroundColor Yellow
Push-Location ..\GXMPackager
$packagerBuild = dotnet build -c Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Failed to build GXM Packager!" -ForegroundColor Red
    Write-Host $packagerBuild
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "  Done!" -ForegroundColor Green
Write-Host ""

$packagerPath = "..\GXMPackager\bin\Release\net9.0\GXMPackager.exe"

# Step 2: Package Simple version
Write-Host "[2/4] Packaging Simple Notepad..." -ForegroundColor Yellow
$scriptPath = "notepad-simple.txt"
$outputPath = "notepad.gxm"

if (!(Test-Path $scriptPath)) {
    Write-Host "  Error: Script file not found: $scriptPath" -ForegroundColor Red
    exit 1
}

& $packagerPath $scriptPath $outputPath --script $scriptPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "  Packaging failed!" -ForegroundColor Red
    exit 1
}

$gxmSize = (Get-Item $outputPath).Length
Write-Host "  Simple version: $gxmSize bytes" -ForegroundColor Green
Write-Host ""

# Step 3: Package Full version
Write-Host "[3/4] Packaging Full Notepad..." -ForegroundColor Yellow
$scriptPath = "notepad-full.txt"
$outputPath = "notepad-full.gxm"

& $packagerPath $scriptPath $outputPath --script $scriptPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "  Packaging failed!" -ForegroundColor Red
    exit 1
}

$gxmSize = (Get-Item $outputPath).Length
Write-Host "  Full version: $gxmSize bytes" -ForegroundColor Green
Write-Host ""

# Step 4: Copy to Ramdisk
if (Test-Path $OutputDir) {
    Write-Host "[4/4] Copying to Ramdisk..." -ForegroundColor Yellow
    Copy-Item "notepad.gxm" "$OutputDir\notepad.gxm" -Force
    Copy-Item "notepad-full.gxm" "$OutputDir\notepad-full.gxm" -Force
    
    # Copy demo if it exists
    if (Test-Path "notepad-demo.gxm") {
        Copy-Item "notepad-demo.gxm" "$OutputDir\notepad-demo.gxm" -Force
    }
    
    Write-Host "  Copied all versions to: $OutputDir\" -ForegroundColor Green
} else {
    Write-Host "[4/4] Ramdisk directory not found, skipping copy" -ForegroundColor Yellow
    Write-Host "  GXM files are in current directory" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Built versions:" -ForegroundColor Cyan
Write-Host "  notepad.gxm      - Simple version" -ForegroundColor White
Write-Host "  notepad-full.gxm - Full version with controls" -ForegroundColor White
if (Test-Path "notepad-demo.gxm") {
    Write-Host "  notepad-demo.gxm - Demo with all features" -ForegroundColor White
}
Write-Host ""
Write-Host "To use in guideXOS:" -ForegroundColor Cyan
Write-Host "  1. Rebuild guideXOS ISO: cd ..\..\guideXOS; dotnet build" -ForegroundColor White
Write-Host "  2. Boot guideXOS" -ForegroundColor White
Write-Host "  3. Run: cd Programs" -ForegroundColor White
Write-Host "  4. Run: notepad (or notepad-full, notepad-demo)" -ForegroundColor White
Write-Host ""
