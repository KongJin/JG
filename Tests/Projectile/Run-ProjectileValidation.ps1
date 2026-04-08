Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$setupPath = Join-Path $repoRoot 'Assets/Scripts/Features/Projectile/ProjectileSetup.cs'

if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Expected Projectile setup scaffold at '$setupPath'."
}

$content = Get-Content -Path $setupPath -Raw
if ($content -notmatch 'class\s+ProjectileSetup\b') {
    throw 'ProjectileSetup.cs does not declare ProjectileSetup.'
}

Write-Host 'Projectile targeted validation passed.'
