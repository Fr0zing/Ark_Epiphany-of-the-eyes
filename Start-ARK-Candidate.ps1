$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workingDirectory = Join-Path $projectRoot 'bin\StableCandidate'
$executable = Join-Path $workingDirectory 'ArkTracker.Reborn.exe'

if (-not (Test-Path -LiteralPath $executable)) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        'Тестовая сборка (bin\StableCandidate) не найдена. Соберите проект.',
        'ARK Vision',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}

# Do not kill another tracker: two overlays would compete for the same
# transparent topmost layer and make it impossible to tell which build is
# being tested. This mirrors Start-ARK-Tracker-Stable.ps1.
$existing = Get-Process -Name 'ArkTracker.Reborn' -ErrorAction SilentlyContinue
if ($existing) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        'Уже запущен другой ARK Tracker. Закройте его вручную и повторите запуск тестовой сборки.',
        'ARK Vision',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
    exit 0
}

Start-Process -FilePath $executable `
    -WorkingDirectory $workingDirectory `
    -ArgumentList '--radar', '--overlay', '--wait', '--all-teams'
