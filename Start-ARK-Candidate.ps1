param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workingDirectory = Join-Path $projectRoot 'bin\StableCandidate'
$executable = Join-Path $workingDirectory 'ArkTracker.Reborn.exe'
$staging = $null
try {
    if (Get-Process -Name 'ArkTracker.Reborn' -ErrorAction SilentlyContinue) { throw 'Close ARK Tracker before building the new version.' }
    Write-Host 'ARK Vision: building latest sources...' -ForegroundColor Cyan
    Write-Host 'Requires Visual Studio / Build Tools: Desktop development with C++, Windows SDK, CMake tools; .NET Framework 4.8.'
    # Fresh cache: a downloaded repository may contain CMake paths from another PC.
    $stagingRelative = 'bin\BuildAndRun-' + [Guid]::NewGuid().ToString('N')
    $staging = Join-Path $projectRoot $stagingRelative
    & (Join-Path $projectRoot 'build-reborn.ps1') -Configuration Release -Destination $stagingRelative -NativeBuildDirectory ($stagingRelative + '\native')
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. The tracker was not started.' }
    $freshExe = Join-Path $staging 'ArkTracker.Reborn.exe'
    $test = Start-Process -FilePath $freshExe -WorkingDirectory $staging -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
    if ($test.ExitCode -ne 0) { throw "Self-test failed: $($test.ExitCode). The tracker was not started." }
    if (Get-Process -Name 'ArkTracker.Reborn' -ErrorAction SilentlyContinue) { throw 'Close the other tracker before installing this build.' }
    New-Item -ItemType Directory -Force -Path $workingDirectory | Out-Null
    $previous = Join-Path $staging 'previous'
    New-Item -ItemType Directory -Path $previous | Out-Null
    foreach ($name in @('ArkTracker.Reborn.exe', 'ArkOverlayNative.dll', 'tracker.ini')) {
        $old = Join-Path $workingDirectory $name
        if (Test-Path -LiteralPath $old) { Copy-Item -LiteralPath $old -Destination (Join-Path $previous $name) }
    }
    try {
        foreach ($name in @('ArkTracker.Reborn.exe', 'ArkOverlayNative.dll', 'tracker.ini')) {
            Copy-Item -LiteralPath (Join-Path $staging $name) -Destination (Join-Path $workingDirectory $name) -Force
        }
    } catch {
        foreach ($file in Get-ChildItem -LiteralPath $previous -File) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $workingDirectory $file.Name) -Force }
        throw
    }
    Write-Host 'Latest build installed. Personal UI settings preserved.' -ForegroundColor Green
    if (-not $BuildOnly) {
        Start-Process -FilePath $executable -WorkingDirectory $workingDirectory -Verb RunAs -WindowStyle Normal -ArgumentList '--radar', '--overlay', '--wait', '--all-teams'
    }
} catch {
    Write-Host ('ARK Vision: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
} finally {
    if ($staging -and (Test-Path -LiteralPath $staging)) {
        $stagingFull = [IO.Path]::GetFullPath($staging)
        $allowedPrefix = [IO.Path]::GetFullPath((Join-Path $projectRoot 'bin\BuildAndRun-'))
        if ($stagingFull.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $stagingFull -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
