@echo off
setlocal
set "tracker=%~dp0bin\StableCandidate\ArkTracker.Reborn.exe"
if not exist "%tracker%" (
  echo Candidate tracker was not found: %tracker%
  pause
  exit /b 1
)

rem The executable needs these switches to open the dashboard rather than run
rem a one-off diagnostic and immediately exit.
start "ARK Vision Candidate" /D "%~dp0bin\StableCandidate" "%tracker%" --radar --overlay --wait --all-teams
endlocal
exit /b
