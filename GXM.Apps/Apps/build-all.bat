@echo off
echo Building GXM Native Applications...
echo =====================================
echo.

call :build notepad
call :build paint
call :build calculator
call :build imageviewer
call :build welcome

echo.
echo =====================================
echo Build complete!
echo.
echo Next steps:
echo   1. Run deploy.bat to copy apps to guideXOS Ramdisk
echo   2. Or manually copy *.gxm to ..\..\..\guideXOS\Ramdisk\Programs\
pause
exit /b

:build
if not exist "%1.txt" (
    echo ERROR: %1.txt not found!
    exit /b 1
)
echo Building %1.gxm...
dotnet run --project ..\GXMPackager\GXMPackager.csproj "%1.txt" "%1.gxm" --script "%1.txt"
if %ERRORLEVEL% equ 0 (
    echo   SUCCESS: %1.gxm created
) else (
    echo   ERROR: Failed to build %1.gxm
)
echo.
exit /b
