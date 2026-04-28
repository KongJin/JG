param(
    [Parameter(Mandatory)][string]$ReviewPath,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessRoles'),
    [string]$OutputDir,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'roles/ReviewWorkHarness.psm1') -Force

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Join-Path $OutputRoot (Get-Date -Format 'yyyyMMdd-HHmmss')) '02-review-work'
}
if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$logPath = Join-Path $OutputDir 'log.txt'
Start-Transcript -Path $logPath -Force | Out-Null
try {
    Invoke-ReviewWorkHarness -RepoRoot $RepoRoot -ConfigPath $ConfigPath -ReviewPath $ReviewPath -OutputDir $OutputDir -DryRun:$DryRun
}
finally {
    Stop-Transcript | Out-Null
}
