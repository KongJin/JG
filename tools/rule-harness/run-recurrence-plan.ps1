param(
    [Parameter(Mandatory)][string]$ReviewPath,
    [Parameter(Mandatory)][string]$WorkReportPath,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessRoles'),
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'roles/RecurrencePlanHarness.psm1') -Force

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Join-Path $OutputRoot (Get-Date -Format 'yyyyMMdd-HHmmss')) '03-recurrence-plan'
}
if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$logPath = Join-Path $OutputDir 'log.txt'
Start-Transcript -Path $logPath -Force | Out-Null
try {
    $report = Invoke-RecurrencePlanHarness -RepoRoot $RepoRoot -ConfigPath $ConfigPath -ReviewPath $ReviewPath -WorkReportPath $WorkReportPath -OutputDir $OutputDir
    $latestPath = Join-Path $OutputRoot 'latest-recurrence-plan.txt'
    if (-not (Test-Path -LiteralPath $OutputRoot)) {
        New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
    }
    Set-Content -Path $latestPath -Value $OutputDir -Encoding UTF8
    $report
}
finally {
    Stop-Transcript | Out-Null
}
