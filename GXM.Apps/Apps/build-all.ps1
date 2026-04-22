# Build all GXM apps
$apps = @("notepad", "paint", "calculator", "imageviewer", "welcome")

Write-Host "Building GXM Native Applications..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

foreach ($app in $apps) {
    Write-Host "Building $app.gxm..." -ForegroundColor Yellow
    
    # Check if source file exists
    if (!(Test-Path "$app.txt")) {
        Write-Host "  ERROR: $app.txt not found!" -ForegroundColor Red
        continue
    }
    
    # Package the app
    dotnet run --project ..\GXMPackager\GXMPackager.csproj "$app.txt" "$app.gxm" --script "$app.txt"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  SUCCESS: $app.gxm created" -ForegroundColor Green
    } else {
        Write-Host "  ERROR: Failed to build $app.gxm" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run .\deploy.ps1 to copy apps to guideXOS Ramdisk" -ForegroundColor White
Write-Host "  2. Or manually copy *.gxm to ..\..\..\guideXOS\Ramdisk\Programs\" -ForegroundColor White
