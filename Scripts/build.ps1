<#
.SYNOPSIS
    Builds guideXOS and produces a bootable ISO.

.DESCRIPTION
    Publishes the guideXOS project via dotnet publish with NativeAOT,
    assembles the ramdisk, links the entry-point loader, and generates
    the bootable ISO.

    Uses the same build command as Visual Studio's launch profiles
    (launchSettings.json), including the EmitLegacyAssetsFileItems
    flag required by the custom ILCompiler package.

.PARAMETER Configuration
    Build configuration (default: Debug).

.PARAMETER SkipISO
    If set, skips the ISO generation step (useful for quick compile checks).

.PARAMETER VM
    Optionally set the VM target to auto-launch after build.
    Valid values: qemu, qemuusb, qemutap, vmware, vmwareworkstation, virtualbox
    If not set, only builds without launching a VM.

.EXAMPLE
    .\Scripts\build.ps1
    .\Scripts\build.ps1 -Configuration Release
    .\Scripts\build.ps1 -SkipISO
    .\Scripts\build.ps1 -VM qemu
#>
param(
    [string]$Configuration = "Debug",
    [switch]$SkipISO,
    [string]$VM = ""
)

$ErrorActionPreference = "Stop"
$SolutionRoot = Split-Path -Parent $PSScriptRoot
$ProjectDir   = Join-Path $SolutionRoot "guideXOS"
$ProjectFile  = Join-Path $ProjectDir "guideXOS.csproj"

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " guideXOS Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration : $Configuration"
Write-Host "Solution Root : $SolutionRoot"
Write-Host "Skip ISO      : $SkipISO"
if ($VM) { Write-Host "VM            : $VM" }
Write-Host ""

# --- Build via dotnet publish (matching launchSettings.json) ---
# The custom ILCompiler package requires EmitLegacyAssetsFileItems=true
# to populate the PackageDefinitions ItemGroup.
$publishArgs = @(
    "publish"
    $ProjectFile
    "-r", "win-x64"
    "-c", $Configuration
    "-p:EmitLegacyAssetsFileItems=true"
)

if ($SkipISO) {
    $publishArgs += "-p:SkipISO=true"
}

if ($VM) {
    $publishArgs += "-p:vm=$VM"
}

Write-Host "[1/1] Publishing guideXOS..." -ForegroundColor Yellow
Push-Location $SolutionRoot
try {
    dotnet @publishArgs
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

# --- Locate ISO ---
$isoPath = $null
$searchDirs = @(
    (Join-Path $SolutionRoot "bin" $Configuration "net9.0" "win-x64" "native"),
    (Join-Path $SolutionRoot "bin" $Configuration "net9.0" "linux-x64" "native"),
    (Join-Path $ProjectDir "bin" $Configuration "net9.0" "win-x64" "native"),
    (Join-Path $ProjectDir "bin" $Configuration "net9.0" "linux-x64" "native")
)
foreach ($dir in $searchDirs) {
    $candidate = Join-Path $dir "guideXOS.iso"
    if (Test-Path $candidate) { $isoPath = $candidate; break }
}

if (-not $isoPath) {
    $found = Get-ChildItem -Path (Join-Path $SolutionRoot "bin") -Recurse -Filter "guideXOS.iso" -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($found) { $isoPath = $found.FullName }
}

# If the BIOS ISO was built successfully but a later target (like BuildUEFI_ISO)
# failed, treat it as a success with a warning rather than a hard failure.
if ($exitCode -ne 0 -and $isoPath) {
    Write-Host ""
    Write-Host "WARNING: dotnet publish exited with code $exitCode, but the ISO was created." -ForegroundColor Yellow
    Write-Host "The UEFI ISO target may have failed (this is a known issue with create_uefi_img.bat)." -ForegroundColor Yellow
    Write-Host "The BIOS ISO is still usable." -ForegroundColor Yellow
    $exitCode = 0
} elseif ($exitCode -ne 0) {
    Write-Error "Build failed with exit code $exitCode"
    exit $exitCode
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
if ($isoPath) {
    Write-Host " Build complete!" -ForegroundColor Green
    Write-Host " ISO: $isoPath" -ForegroundColor Green
} elseif ($SkipISO) {
    Write-Host " Build complete (ISO skipped)." -ForegroundColor Green
} else {
    Write-Host " Build complete." -ForegroundColor Green
    Write-Host " (Could not locate ISO - check output above)" -ForegroundColor Yellow
}
Write-Host "========================================" -ForegroundColor Green
