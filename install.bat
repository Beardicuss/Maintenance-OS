@echo off
setlocal

:: ── Must run as Administrator ──────────────────────────────────────────────
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] This script must be run as Administrator.
    echo  Right-click install.bat → Run as administrator
    pause & exit /b 1
)

echo.
echo  ◈  SOFTCURSE LAB — Installer
echo  ====================================
echo.

set "SRC=%~dp0build\SoftcurseLab.scr"
set "DEST=%SystemRoot%\System32\SoftcurseLab.scr"

:: ── Check build exists ────────────────────────────────────────────────────
if not exist "%SRC%" (
    echo  [ERROR] Build not found at: %SRC%
    echo  Run build.bat first.
    pause & exit /b 1
)

:: ── Copy to System32 ──────────────────────────────────────────────────────
echo  [COPY] %SRC%
echo    --^> %DEST%
copy /y "%SRC%" "%DEST%" >nul
if %errorlevel% neq 0 (
    echo  [ERROR] Failed to copy. Make sure you are running as Administrator.
    pause & exit /b 1
)
echo  [OK] Screensaver installed to System32.

:: ── Register in registry ──────────────────────────────────────────────────
echo  [REG] Setting as active screensaver...
reg add "HKCU\Control Panel\Desktop" /v SCRNSAVE.EXE /t REG_SZ /d "%DEST%" /f >nul
reg add "HKCU\Control Panel\Desktop" /v ScreenSaveActive /t REG_SZ /d "1" /f >nul
:: Wait 5 minutes before activating screensaver (300 seconds)
reg add "HKCU\Control Panel\Desktop" /v ScreenSaveTimeOut /t REG_SZ /d "300" /f >nul
:: Require password on resume (security)
reg add "HKCU\Control Panel\Desktop" /v ScreenSaverIsSecure /t REG_SZ /d "1" /f >nul

echo  [OK] Registry configured.

:: ── Notify the desktop service to pick up changes ─────────────────────────
RUNDLL32.EXE user32.dll,UpdatePerUserSystemParameters ,1 ,True

echo.
echo  ====================================
echo  Installation complete!
echo.
echo  Screensaver will activate after 5 minutes of idle.
echo  To configure: right-click Desktop → Personalize → Screen Saver
echo  Or double-click %DEST%
echo.
echo  IMPORTANT: For admin tasks (defrag, DISM, cleanup) to work,
echo  go to Task Scheduler and create a task that runs
echo  SoftcurseLab.scr /s  as SYSTEM or with highest privileges.
echo  See README.md for detailed instructions.
echo.
pause
