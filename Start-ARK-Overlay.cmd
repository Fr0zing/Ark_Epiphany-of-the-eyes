@echo off
powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~dp0bin\Reborn\ArkTracker.Reborn.exe' -WorkingDirectory '%~dp0bin\Reborn' -ArgumentList '--overlay','--wait' -Verb RunAs"
exit /b
