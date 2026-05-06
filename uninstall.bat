@echo off
setlocal

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] Run as Administrator.
    pause & exit /b 1
)

echo.
echo  ◈  SOFTCURSE LAB — Uninstaller
echo.

set "DEST=%SystemRoot%\System32\SoftcurseLab.scr"

:: Remove registry
reg delete "HKCU\Control Panel\Desktop" /v SCRNSAVE.EXE /f >nul 2>&1
reg delete "HKCU\Control Panel\Desktop" /v ScreenSaveActive /f >nul 2>&1
echo  [OK] Registry entries removed.

:: Remove file
if exist "%DEST%" (
    del /f /q "%DEST%"
    echo  [OK] Removed %DEST%
) else (
    echo  [SKIP] File not found: %DEST%
)

:: Remove config folder
set "CFGDIR=%APPDATA%\CyberScreensaver"
if exist "%CFGDIR%" (
    rmdir /s /q "%CFGDIR%"
    echo  [OK] Removed config folder.
)

RUNDLL32.EXE user32.dll,UpdatePerUserSystemParameters ,1 ,True
echo.
echo  Uninstall complete.
pause
