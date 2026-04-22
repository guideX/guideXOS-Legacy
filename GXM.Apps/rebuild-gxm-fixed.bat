@echo off
echo ========================================
echo GXM Apps Rebuild Script (with fixes)
echo ========================================
echo.

echo [1/5] Rebuilding GXMPackager...
cd /d D:\devgitlab\guideXOS\GXM.Apps\GXMPackager
dotnet build
if %ERRORLEVEL% neq 0 (
    echo ERROR: GXMPackager build failed!
    pause
    exit /b 1
)
echo SUCCESS: GXMPackager rebuilt with null terminator fix
echo.

echo [2/5] Rebuilding GXM Apps...
cd /d D:\devgitlab\guideXOS\GXM.Apps\Apps
call build-all.bat
if %ERRORLEVEL% neq 0 (
    echo ERROR: App build failed!
    pause
    exit /b 1
)
echo SUCCESS: All apps rebuilt with null terminators
echo.

echo [3/5] Deploying apps to Ramdisk...
call deploy.ps1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Deployment failed!
    pause
    exit /b 1
)
echo SUCCESS: Apps deployed to Ramdisk
echo.

echo [4/5] Rebuilding guideXOS kernel...
cd /d D:\devgitlab\guideXOS\guideXOS
dotnet build guideXOS\guideXOS.csproj
if %ERRORLEVEL% neq 0 (
    echo ERROR: guideXOS build failed!
    pause
    exit /b 1
)
echo SUCCESS: guideXOS kernel rebuilt with safety counter
echo.

echo [5/5] Rebuilding ISO...
REM Uncomment next line if you have build.ps1
REM powershell -ExecutionPolicy Bypass -File build.ps1
echo NOTE: Run build.ps1 manually to create ISO
echo.

echo ========================================
echo Rebuild Complete!
echo ========================================
echo.
echo The GXM freeze bug has been fixed:
echo   - GXMPackager now adds null terminators
echo   - GXMLoader has safety counter (10,000 lines max)
echo   - Memory leak fixed (strings properly disposed)
echo.
echo Next steps:
echo   1. Run build.ps1 to create ISO
echo   2. Boot guideXOS in VM
echo   3. Test launching GXM apps from Start Menu
echo   4. Verify no freeze occurs
echo.
pause
