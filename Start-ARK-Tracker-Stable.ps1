$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workingDirectory = Join-Path $projectRoot 'bin\Reborn-Stable'
$executable = Join-Path $workingDirectory 'ArkTracker.Reborn.exe'

if (-not (Test-Path -LiteralPath $executable)) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        'Стабильная сборка не найдена. Сначала соберите проект.',
        'ARK Vision',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}

# Do not kill another tracker: it may be the user's known-good old version.
# Two overlays would compete for the same transparent topmost layer and make
# it impossible to tell which build is being tested.
$existing = Get-Process -Name 'ArkTracker.Reborn' -ErrorAction SilentlyContinue
if ($existing) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        'Уже запущен другой ARK Tracker. Закройте его вручную и повторите запуск стабильной версии.',
        'ARK Vision',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
    exit 0
}

Start-Process -FilePath $executable `
    -WorkingDirectory $workingDirectory `
    -ArgumentList '--radar', '--overlay', '--wait', '--all-teams'
