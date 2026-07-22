[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $projectRoot 'artifacts'
$buildRoot = Join-Path $artifactRoot 'build'
$portableRoot = Join-Path $artifactRoot 'TempDrop-portable'
$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) {
    throw '.NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
New-Item -ItemType Directory -Path $portableRoot -Force | Out-Null

$coreSources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src\TempDrop.Core') -Filter *.cs | Select-Object -ExpandProperty FullName)
$appSources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src\TempDrop') -Filter *.cs | Select-Object -ExpandProperty FullName)
$testSources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests\TempDrop.Tests') -Filter *.cs | Select-Object -ExpandProperty FullName)
$guiTestSources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests\TempDrop.GuiTests') -Filter *.cs | Select-Object -ExpandProperty FullName)
$appSupportSources = @($appSources | Where-Object { [IO.Path]::GetFileName($_) -ne 'Program.cs' })
$commonReferences = @(
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Web.Extensions.dll'
)
$optimize = if ($Configuration -eq 'Release') { '/optimize+' } else { '/optimize-' }

$appArguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    $optimize,
    '/debug:pdbonly',
    ('/out:' + (Join-Path $buildRoot 'TempDrop.exe')),
    ('/win32icon:' + (Join-Path $projectRoot 'assets\TempDrop.ico')),
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
) + $commonReferences + $coreSources + $appSources

& $csc @appArguments
if ($LASTEXITCODE -ne 0) { throw 'TempDrop application compilation failed.' }

$testArguments = @(
    '/nologo',
    '/target:exe',
    '/platform:anycpu',
    $optimize,
    ('/out:' + (Join-Path $buildRoot 'TempDrop.Tests.exe'))
) + $commonReferences + $coreSources + $testSources

& $csc @testArguments
if ($LASTEXITCODE -ne 0) { throw 'TempDrop test compilation failed.' }

$guiTestArguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    $optimize,
    ('/out:' + (Join-Path $buildRoot 'TempDrop.GuiTests.exe')),
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
) + $commonReferences + $coreSources + $appSupportSources + $guiTestSources

& $csc @guiTestArguments
if ($LASTEXITCODE -ne 0) { throw 'TempDrop GUI test compilation failed.' }

Copy-Item -LiteralPath (Join-Path $buildRoot 'TempDrop.exe') -Destination (Join-Path $portableRoot 'TempDrop.exe') -Force
foreach ($name in @('README.md', 'README.ko.md', 'LICENSE', 'PRIVACY.md', 'SECURITY.md')) {
    $source = Join-Path $projectRoot $name
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $portableRoot $name) -Force
    }
}

Write-Host "Build completed: $buildRoot" -ForegroundColor Green
