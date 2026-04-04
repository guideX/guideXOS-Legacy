<#
.SYNOPSIS
    Builds guideXOS and launches it in QEMU.

.DESCRIPTION
    Runs the build script first (using Visual Studio MSBuild), then starts
    QEMU with the generated ISO. Supports multiple VM profiles matching
    the MSBuild targets in the .csproj.

.PARAMETER Profile
    QEMU profile to use (default: standard).
      standard  - Full AHCI disk, AC97 audio, RTL8139 networking
      minimal   - Lightweight, no AHCI disk or audio
      usb       - XHCI USB controller with USB keyboard/mouse
      tap       - TAP networking (requires TAP adapter configured)

.PARAMETER Configuration
    Build configuration passed to build.ps1 (default: Release).

.PARAMETER SkipBuild
    If set, skips the build step and runs QEMU with the last built ISO.

.PARAMETER Memory
    VM memory in MB (default: 2056).

.PARAMETER Cpus
    Number of SMP cores (default: 2).

.EXAMPLE
    .\Scripts\run-qemu.ps1
    .\Scripts\run-qemu.ps1 -SkipBuild
    .\Scripts\run-qemu.ps1 -Profile usb -Memory 4096
    .\Scripts\run-qemu.ps1 -Profile tap
#>
param(
    [ValidateSet("standard", "minimal", "usb", "tap")]
    [string]$Profile = "standard",
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [int]$Memory = 2056,
    [int]$Cpus = 2
)

$ErrorActionPreference = "Stop"
$SolutionRoot = Split-Path -Parent $PSScriptRoot
$ProjectDir   = Join-Path $SolutionRoot "guideXOS"

# --- QEMU path ---
$QemuExe = "C:\Program Files\qemu\qemu-system-x86_64.exe"
if (-not (Test-Path $QemuExe)) {
    Write-Error "QEMU not found at: $QemuExe`nInstall from https://www.qemu.org/download/#windows"
    exit 1
}

# --- Build first (unless skipped) ---
if (-not $SkipBuild) {
    $buildScript = Join-Path $PSScriptRoot "build.ps1"
    Write-Host "Running build..." -ForegroundColor Cyan
    & $buildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed — aborting QEMU launch."
        exit $LASTEXITCODE
    }
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
    # Fallback: find most recent ISO anywhere under bin
    $found = Get-ChildItem -Path (Join-Path $SolutionRoot "bin") -Recurse -Filter "guideXOS.iso" -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($found) { $isoPath = $found.FullName }
}

if (-not $isoPath) {
    Write-Error "ISO not found. Run build first or check output paths."
    exit 1
}

# --- Locate ramdisk for AHCI disk ---
$RamdiskPath = Join-Path $SolutionRoot "Tools" "grub2" "boot" "ramdisk.tar"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " guideXOS QEMU Launcher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Profile : $Profile"
Write-Host "ISO     : $isoPath"
Write-Host "Memory  : ${Memory}MB"
Write-Host "CPUs    : $Cpus"
Write-Host ""

# --- Build QEMU arguments per profile ---
$baseArgs = @(
    "-m", $Memory
    "-smp", $Cpus
    "-k", "en-us"
    "-boot", "d"
    "-cdrom", $isoPath
    "-serial", "stdio"
    "-rtc", "base=localtime"
)

switch ($Profile) {
    "standard" {
        $profileArgs = @(
            "-d", "guest_errors"
            "-device", "AC97"
            "-netdev", "user,id=net0"
            "-device", "rtl8139,netdev=net0"
        )
        # Add AHCI disk if ramdisk exists
        if (Test-Path $RamdiskPath) {
            $profileArgs += @(
                "-drive", "id=disk,file=$RamdiskPath,if=none"
                "-device", "ahci,id=ahci"
                "-device", "ide-hd,drive=disk,bus=ahci.0"
            )
        }
    }
    "minimal" {
        $profileArgs = @(
            "-netdev", "user,id=net0"
            "-device", "rtl8139,netdev=net0"
        )
    }
    "usb" {
        $profileArgs = @(
            "-d", "guest_errors"
            "-device", "AC97"
            "-machine", "q35"
            "-device", "qemu-xhci,id=xhci"
            "-device", "usb-kbd,bus=xhci.0"
            "-device", "usb-mouse,bus=xhci.0"
            "-netdev", "user,id=net0"
            "-device", "rtl8139,netdev=net0"
        )
    }
    "tap" {
        $profileArgs = @(
            "-d", "guest_errors"
            "-device", "AC97"
            "-net", "nic,model=rtl8139"
            "-net", "tap,ifname=tap"
        )
    }
}

$allArgs = $baseArgs + $profileArgs

Write-Host "Launching QEMU..." -ForegroundColor Yellow
Write-Host "$QemuExe $($allArgs -join ' ')" -ForegroundColor DarkGray
Write-Host ""

& $QemuExe @allArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "QEMU exited with code $LASTEXITCODE" -ForegroundColor Yellow
}
