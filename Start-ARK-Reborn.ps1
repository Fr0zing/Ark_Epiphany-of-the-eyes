$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workingDirectory = Join-Path $projectRoot 'bin\Reborn'
$executable = Join-Path $workingDirectory 'ArkTracker.Reborn.exe'

# Restart only our tracker. ShooterGame.exe is deliberately never stopped or
# modified by the launcher.
Get-Process -Name 'ArkTracker.Reborn' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 350

Start-Process -FilePath $executable `
    -WorkingDirectory $workingDirectory `
    -ArgumentList '--radar', '--overlay', '--wait'
