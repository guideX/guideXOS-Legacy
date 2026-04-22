# Master Build Script for All GXM Applications
# Builds all GXM apps in the solution and packages them

param(
    [string]$Configuration = "Release"
)

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   GXM Apps Build System" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Build GXMPackager first
Write-Host "Building GXMPackager tool..." -ForegroundColor Yellow
Push-Location GXMPackager
dotnet build -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build GXMPackager!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "  GXMPackager built successfully!" -ForegroundColor Green
Write-Host ""

# Build GUI Script examples
Write-Host "Creating GUI Script GXM files..." -ForegroundColor Yellow

$packager = "GXMPackager\bin\$Configuration\net9.0\GXMPackager.exe"

# Create notepad-simple.gxm
$scriptPath = "Notepad.GXM\notepad-simple.txt"
$outputPath = "..\guideXOS\Ramdisk\Programs\notepad-demo.gxm"

if (Test-Path $scriptPath) {
    Write-Host "  Packaging notepad-demo.gxm..." -ForegroundColor Gray
    & $packager $scriptPath $outputPath --script $scriptPath
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    Created: $outputPath" -ForegroundColor Green
    }
}

# Create sample GUI script
$sampleScript = "GXMPackager\sample-gui.txt"
$sampleOutput = "..\guideXOS\Ramdisk\Programs\sample-gui.gxm"

if (Test-Path $sampleScript) {
    Write-Host "  Packaging sample-gui.gxm..." -ForegroundColor Gray
    & $packager $sampleScript $sampleOutput --script $sampleScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    Created: $sampleOutput" -ForegroundColor Green
    }
}

Write-Host ""

# Build native GXM apps (commented out until NativeAOT is properly configured)
Write-Host "Native GXM Apps (NativeAOT):" -ForegroundColor Yellow
Write-Host "  Note: NativeAOT compilation requires additional setup" -ForegroundColor Gray
Write-Host "  See BUILD_INSTRUCTIONS.md for details" -ForegroundColor Gray
Write-Host ""

# Uncomment when ready to build native apps:
# Write-Host "Building Notepad.GXM..." -ForegroundColor Yellow
# Push-Location Notepad.GXM
# .\build.ps1 -Configuration $Configuration
# Pop-Location

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "   Build Summary" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "GUI Script GXM files created in:" -ForegroundColor Cyan
Write-Host "  ..\guideXOS\Ramdisk\Programs\" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Rebuild guideXOS to include new GXM files in ISO" -ForegroundColor White
Write-Host "  2. Boot guideXOS" -ForegroundColor White
Write-Host "  3. Use 'launch' command to run apps" -ForegroundColor White
Write-Host ""
