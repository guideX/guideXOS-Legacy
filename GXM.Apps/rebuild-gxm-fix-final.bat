@echo off
echo ================================================================================
echo GXM App Freeze Fix - Rebuild and Deploy Script
echo ================================================================================
echo.
echo This script will:
echo 1. Clean the build
echo 2. Rebuild guideXOS with all fixes
echo 3. Copy the new binary to Ramdisk
echo 4. Rebuild the ISO
echo.
echo IMPORTANT: Make sure Visual Studio is CLOSED before running this!
echo.
pause

echo.
echo Step 1: Cleaning build...
cd /d "%~dp0guideXOS"
dotnet clean
if errorlevel 1 (
    echo ERROR: Clean failed!
    pause
    exit /b 1
)

echo.
echo Step 2: Building guideXOS...
dotnet build -c Release
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Step 3: Copying binary to Ramdisk...
copy /Y "bin\Release\net9.0\win-x64\native\guideXOS.exe" "..\Ramdisk\boot\kernel.bin"
if errorlevel 1 (
    echo ERROR: Failed to copy kernel!
    pause
    exit /b 1
)

echo.
echo Step 4: Rebuilding ISO...
cd /d "%~dp0"
call makeiso.bat
if errorlevel 1 (
    echo ERROR: ISO creation failed!
    pause
    exit /b 1
)

echo.
echo ================================================================================
echo SUCCESS! guideXOS has been rebuilt with GXM app freeze fixes
echo ================================================================================
echo.
echo Changes applied:
echo   - Fixed memory leaks in ExtractUtf8, ToBool, ApplyGuiLine
echo   - Reduced safety limit from 1000 to 100 iterations
echo   - Added safety counter to CR/LF skip loop (max 10)
echo   - Fixed CLEAR action in GXMScriptWindow
echo.
echo Your ISO is ready to test!
echo.
pause
