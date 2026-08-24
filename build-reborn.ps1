param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Destination = 'bin\Reborn',
    [switch]$SkipNative
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectRoot 'src\ArkTracker.Reborn'
$manifest = Join-Path $projectRoot 'src\ArkTracker.Reborn\app.manifest'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$frameworkAssemblyRoot = Join-Path $env:WINDIR 'Microsoft.NET\assembly'
$wpfAssemblies = @(
    (Join-Path $frameworkAssemblyRoot 'GAC_MSIL\WindowsFormsIntegration\v4.0_4.0.0.0__31bf3856ad364e35\WindowsFormsIntegration.dll'),
    (Join-Path $frameworkAssemblyRoot 'GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll'),
    (Join-Path $frameworkAssemblyRoot 'GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll'),
    (Join-Path $frameworkAssemblyRoot 'GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll'),
    (Join-Path $frameworkAssemblyRoot 'GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll')
)
$outputDirectory = Join-Path $projectRoot $Destination
$output = Join-Path $outputDirectory 'ArkTracker.Reborn.exe'

if (-not $SkipNative) {
    & (Join-Path $projectRoot 'build-native.ps1') -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Native overlay compilation failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework x64 C# compiler was not found.'
}

foreach ($wpfAssembly in $wpfAssemblies) {
    if (-not (Test-Path -LiteralPath $wpfAssembly)) {
        throw "Required WPF assembly was not found: $wpfAssembly"
    }
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$compilerArguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    "/win32manifest:$manifest",
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    "/out:$output"
)

foreach ($wpfAssembly in $wpfAssemblies) {
    $compilerArguments += "/reference:$wpfAssembly"
}

if ($Configuration -eq 'Release') {
    $compilerArguments += '/optimize+'
} else {
    $compilerArguments += @('/optimize-', '/debug:full')
}

Get-ChildItem -Path $sourceDirectory -Filter *.cs | ForEach-Object { $compilerArguments += $_.FullName }
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

$profile = Join-Path $projectRoot 'config\profiles\ase-2025-07-19-9bc40141.ini'
if (Test-Path -LiteralPath $profile) {
    Copy-Item -LiteralPath $profile -Destination (Join-Path $outputDirectory 'tracker.ini') -Force
}

$legacySettings = Join-Path $projectRoot 'bin\Dashboard4\ui-settings.ini'
$newSettings = Join-Path $outputDirectory 'ui-settings.ini'
if ((Test-Path -LiteralPath $legacySettings) -and -not (Test-Path -LiteralPath $newSettings)) {
    Copy-Item -LiteralPath $legacySettings -Destination $newSettings
}

Write-Host "Built: $output"
