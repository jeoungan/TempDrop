[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if (-not $iscc) {
    throw 'Inno Setup 6 was not found. Install it from https://jrsoftware.org/isinfo.php.'
}

& $iscc "/DMyAppVersion=$Version" (Join-Path $projectRoot 'installer\TempDrop.iss')
if ($LASTEXITCODE -ne 0) {
    throw 'Installer compilation failed.'
}

$artifactRoot = Join-Path $projectRoot 'artifacts'
$releaseFiles = @(
    (Join-Path $artifactRoot ("TempDrop-{0}-portable.zip" -f $Version)),
    (Join-Path $artifactRoot ("TempDrop-{0}-setup-x64.exe" -f $Version))
)
$lines = foreach ($file in $releaseFiles) {
    $item = Get-Item -LiteralPath $file
    $hash = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($item.Name)"
}
$lines | Set-Content -LiteralPath (Join-Path $artifactRoot 'SHA256SUMS.txt') -Encoding ASCII
Write-Host "Installer package created in $artifactRoot" -ForegroundColor Green
