@echo off
setlocal
echo.
echo  ◈  SOFTCURSE LAB — Build Script
echo  ======================================
echo.

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] .NET SDK not found.
    echo  Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo  [OK] .NET SDK %DOTNET_VER% found.

:: Check WebView2 Runtime
reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel% neq 0 (
    reg query "HKCU\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
)
if %errorlevel% neq 0 (
    echo.
    echo  [WARNING] WebView2 Runtime may not be installed.
    echo  Download: https://go.microsoft.com/fwlink/p/?LinkId=2124703
    echo  (The screensaver will show an error without it)
    echo.
)

cd /d "%~dp0src"

echo  [RESTORE] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo  [ERROR] NuGet restore failed.
    pause & exit /b 1
)

echo  [BUILD] Compiling CyberScreensaver...
dotnet publish -c Release -r win-x64 --self-contained false -o "..\build" ^
    -p:DebugType=none ^
    -p:DebugSymbols=false

if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] Build failed. See errors above.
    pause & exit /b 1
)

cd /d "%~dp0build"
if exist SoftcurseLab.exe (
    copy /y SoftcurseLab.exe SoftcurseLab.scr >nul
    echo  [OK] Created SoftcurseLab.scr  in .\build\
    echo  [OK] CyberUI.html should also be in .\build\
) else (
    echo  [WARN] SoftcurseLab.exe not found in build output.
)

echo.
echo  ======================================
echo  Build complete!
echo.
echo  REQUIRED at runtime (install if missing):
echo    - .NET 8 Desktop Runtime
echo      https://dotnet.microsoft.com/download/dotnet/8.0
echo    - Microsoft WebView2 Runtime (Evergreen)
echo      https://go.microsoft.com/fwlink/p/?LinkId=2124703
echo.
echo  Next steps:
echo    1. Run install.bat (as Administrator) to install system-wide
echo    OR
echo    2. Double-click .\build\SoftcurseLab.scr to configure
echo.
pause
