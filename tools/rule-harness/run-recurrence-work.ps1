param(
    [Parameter(Mandatory)][string]$PlanPath,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessRoles'),
    [string]$OutputDir,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'roles/RecurrenceWorkHarness.psm1') -Force

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Join-Path $OutputRoot (Get-Date -Format 'yyyyMMdd-HHmmss')) '04-recurrence-work'
}
if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$logPath = Join-Path $OutputDir 'log.txt'
Start-Transcript -Path $logPath -Force | Out-Null
try {
    Invoke-RecurrenceWorkHarness -RepoRoot $RepoRoot -ConfigPath $ConfigPath -PlanPath $PlanPath -OutputDir $OutputDir -DryRun:$DryRun
}
finally {
    Stop-Transcript | Out-Null
}
