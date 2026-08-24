param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw 'Visual Studio Installer (vswhere.exe) was not found.'
}

$visualStudio = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $visualStudio) {
    throw 'Visual Studio C++ toolchain was not found.'
}

$cmake = Join-Path $visualStudio 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
$source = Join-Path $projectRoot 'native\ArkOverlayNative'
$build = Join-Path $projectRoot 'native\ArkOverlayNative\build'
$destination = Join-Path $projectRoot 'bin\Reborn'

& $cmake -S $source -B $build -A x64
if ($LASTEXITCODE -ne 0) { throw 'Native CMake configure failed.' }
& $cmake --build $build --config $Configuration --parallel
if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }

$binary = Join-Path $build "out\$Configuration\ArkOverlayNative.dll"
if (-not (Test-Path -LiteralPath $binary)) {
    $binary = Join-Path $build 'out\ArkOverlayNative.dll'
}
if (-not (Test-Path -LiteralPath $binary)) {
    throw 'ArkOverlayNative.dll was not produced.'
}
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath $binary -Destination (Join-Path $destination 'ArkOverlayNative.dll') -Force
Write-Host "Built: $(Join-Path $destination 'ArkOverlayNative.dll')"
