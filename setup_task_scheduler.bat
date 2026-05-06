@echo off
setlocal

:: Registers a Task Scheduler job that runs the screensaver with SYSTEM privileges
:: so all admin tasks (DISM, defrag, disk cleanup) execute at full elevation.

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] Run as Administrator.
    pause & exit /b 1
)

echo.
echo  ◈  SOFTCURSE LAB — Task Scheduler Setup
echo  =============================================
echo  This creates a scheduled task that launches the screensaver
echo  with SYSTEM privileges when the workstation is locked/idle.
echo.

set "SCR=%SystemRoot%\System32\SoftcurseLab.scr"
set "TASKNAME=CyberScreensaverAdmin"

:: Delete old task if present
schtasks /delete /tn "%TASKNAME%" /f >nul 2>&1

:: Create XML task definition
set "TMPXML=%TEMP%\cyber_task.xml"
(
echo ^<?xml version="1.0" encoding="UTF-16"?^>
echo ^<Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task"^>
echo   ^<RegistrationInfo^>
echo     ^<Description^>CyberScreensaver maintenance tasks with admin privileges^</Description^>
echo   ^</RegistrationInfo^>
echo   ^<Triggers^>
echo     ^<SessionStateChangeTrigger^>
echo       ^<StateChange^>RemoteDisconnect^</StateChange^>
echo     ^</SessionStateChangeTrigger^>
echo     ^<SessionStateChangeTrigger^>
echo       ^<StateChange^>ConsoleDisconnect^</StateChange^>
echo     ^</SessionStateChangeTrigger^>
echo   ^</Triggers^>
echo   ^<Principals^>
echo     ^<Principal id="Author"^>
echo       ^<UserId^>S-1-5-18^</UserId^>
echo       ^<RunLevel^>HighestAvailable^</RunLevel^>
echo     ^</Principal^>
echo   ^</Principals^>
echo   ^<Settings^>
echo     ^<MultipleInstancesPolicy^>IgnoreNew^</MultipleInstancesPolicy^>
echo     ^<DisallowStartIfOnBatteries^>false^</DisallowStartIfOnBatteries^>
echo     ^<StopIfGoingOnBatteries^>false^</StopIfGoingOnBatteries^>
echo     ^<ExecutionTimeLimit^>PT4H^</ExecutionTimeLimit^>
echo     ^<Priority^>7^</Priority^>
echo   ^</Settings^>
echo   ^<Actions Context="Author"^>
echo     ^<Exec^>
echo       ^<Command^>%SCR%^</Command^>
echo       ^<Arguments^>/s^</Arguments^>
echo     ^</Exec^>
echo   ^</Actions^>
echo ^</Task^>
) > "%TMPXML%"

schtasks /create /tn "%TASKNAME%" /xml "%TMPXML%" /f
if %errorlevel% equ 0 (
    echo  [OK] Task "%TASKNAME%" created successfully.
    echo  The screensaver will now run all maintenance tasks as SYSTEM
    echo  when the workstation session is locked or disconnected.
) else (
    echo  [ERROR] Failed to create scheduled task.
)

del /q "%TMPXML%" >nul 2>&1
echo.
pause
