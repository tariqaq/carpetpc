$ErrorActionPreference = 'Stop'

$root = Join-Path $env:LOCALAPPDATA 'CarpetPC\WakeTraining'
$modelRoot = Join-Path $env:LOCALAPPDATA 'CarpetPC\Models\WakeWordModel'

@(
    (Join-Path $root 'positive'),
    (Join-Path $root 'negative'),
    (Join-Path $root 'checkpoints'),
    (Join-Path $root 'reports'),
    $modelRoot
) | ForEach-Object {
    New-Item -ItemType Directory -Force -Path $_ | Out-Null
}

Write-Host "Wake training folders ready:"
Write-Host "  $root"
Write-Host "  $modelRoot"
