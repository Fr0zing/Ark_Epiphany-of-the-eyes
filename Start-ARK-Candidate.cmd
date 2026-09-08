@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-ARK-Candidate.ps1" %*
if errorlevel 1 (
    echo Build or launch failed. Read the error above.
    pause
    exit /b 1
)
exit /b 0
