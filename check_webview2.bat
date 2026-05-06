@echo off
echo.
echo  Checking WebView2 Runtime...
echo.

set FOUND=0

reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel%==0 ( echo  [OK] WebView2 found in HKLM WOW6432Node & set FOUND=1 )

reg query "HKLM\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel%==0 ( echo  [OK] WebView2 found in HKLM & set FOUND=1 )

reg query "HKCU\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel%==0 ( echo  [OK] WebView2 found in HKCU & set FOUND=1 )

if %FOUND%==0 (
  echo  [MISSING] WebView2 Runtime NOT installed!
  echo.
  echo  Download and install from:
  echo  https://go.microsoft.com/fwlink/p/?LinkId=2124703
  echo.
  start https://go.microsoft.com/fwlink/p/?LinkId=2124703
) else (
  echo.
  echo  WebView2 is installed.
)

echo.
echo  Crash log location:
echo  %TEMP%\SoftcurseLab_crash.txt
echo.
if exist "%TEMP%\SoftcurseLab_crash.txt" (
  echo  --- LOG CONTENTS ---
  type "%TEMP%\SoftcurseLab_crash.txt"
) else (
  echo  [No crash log found - screensaver may not have started at all]
)
echo.
pause
