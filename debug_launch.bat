@echo off
echo  Launching SoftcurseLab in debug mode...
echo  Crash log: %TEMP%\SoftcurseLab_crash.txt
echo.
del /q "%TEMP%\SoftcurseLab_crash.txt" 2>nul

cd /d "%~dp0build"
if not exist SoftcurseLab.scr (
  echo  [ERROR] Build first with build.bat
  pause & exit /b 1
)

start "" SoftcurseLab.scr /s
timeout /t 12 /nobreak >nul

echo  --- Crash log after 5 seconds ---
if exist "%TEMP%\SoftcurseLab_crash.txt" (
  type "%TEMP%\SoftcurseLab_crash.txt"
) else (
  echo  No log file created.
  echo  Possible causes:
  echo    1. WebView2 not installed - run check_webview2.bat
  echo    2. .NET 8 Desktop Runtime not installed
  echo    3. Process crashed before log was written
)
echo.
pause
