[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$testExecutables = @(
    (Join-Path $projectRoot 'artifacts\build\TempDrop.Tests.exe'),
    (Join-Path $projectRoot 'artifacts\build\TempDrop.GuiTests.exe')
)
if (-not (Test-Path -LiteralPath $testExecutables[0]) -or -not (Test-Path -LiteralPath $testExecutables[1])) {
    & (Join-Path $PSScriptRoot 'build.ps1') -Configuration Release
}

foreach ($testExecutable in $testExecutables) {
    & $testExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "$([IO.Path]::GetFileName($testExecutable)) failed with exit code $LASTEXITCODE."
    }
}