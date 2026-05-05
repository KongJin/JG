param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessRoles'),
    [string]$OutputDir,
    [ValidateSet('FeatureScope', 'ProjectSurface', 'Deep')]
    [string]$Mode = 'FeatureScope'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'roles/TechDebtReviewHarness.psm1') -Force

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Join-Path $OutputRoot (Get-Date -Format 'yyyyMMdd-HHmmss')) '01-tech-debt-review'
}
if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$logPath = Join-Path $OutputDir 'log.txt'
Start-Transcript -Path $logPath -Force | Out-Null
try {
    $report = Invoke-TechDebtReviewHarness -RepoRoot $RepoRoot -ConfigPath $ConfigPath -OutputDir $OutputDir -Mode $Mode
    $latestPath = Join-Path $OutputRoot 'latest-tech-debt-review.txt'
    if (-not (Test-Path -LiteralPath $OutputRoot)) {
        New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
    }
    Set-Content -Path $latestPath -Value $OutputDir -Encoding UTF8
    $report
}
finally {
    Stop-Transcript | Out-Null
}
