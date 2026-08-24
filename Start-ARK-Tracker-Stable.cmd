@echo off
setlocal
set "tracker=%~dp0bin\Reborn-Stable\ArkTracker.Reborn.exe"
if not exist "%tracker%" (
  echo Stable tracker was not found: %tracker%
  pause
  exit /b 1
)

rem The executable needs these switches to open the dashboard rather than run
rem a one-off diagnostic and immediately exit.
start "ARK Vision Stable" /D "%~dp0bin\Reborn-Stable" "%tracker%" --radar --overlay --wait --all-teams
endlocal
exit /b
