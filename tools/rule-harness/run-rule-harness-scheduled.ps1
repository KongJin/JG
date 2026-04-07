param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessScheduled'),
    [string]$ApiKey,
    [string]$ApiBaseUrl,
    [string]$Model,
    [switch]$RequireLlm = $true,
    [switch]$DisableLlm
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runDir = Join-Path $OutputRoot $timestamp
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$reportPath = Join-Path $runDir 'rule-harness-report.json'
$summaryPath = Join-Path $runDir 'rule-harness-summary.md'
$logPath = Join-Path $runDir 'rule-harness.log'
$latestPointer = Join-Path $OutputRoot 'latest-run.txt'

Start-Transcript -Path $logPath -Force | Out-Null
try {
    Write-Host "Rule harness scheduled run started at $(Get-Date -Format o)"
    Write-Host "RepoRoot: $RepoRoot"
    Write-Host "Output: $runDir"

    $requireLlmForRun = $RequireLlm
    if ($DisableLlm) {
        $requireLlmForRun = $false
    }

    & (Join-Path $PSScriptRoot 'run-rule-harness.ps1') `
        -RepoRoot $RepoRoot `
        -ArtifactDir $runDir `
        -ReportPath $reportPath `
        -SummaryPath $summaryPath `
        -ApiKey $ApiKey `
        -ApiBaseUrl $ApiBaseUrl `
        -Model $Model `
        -RequireLlm:$requireLlmForRun `
        -DisableLlm:$DisableLlm

    Set-Content -Path $latestPointer -Value $runDir -Encoding UTF8
    Write-Host "Rule harness scheduled run finished at $(Get-Date -Format o)"
}
finally {
    Stop-Transcript | Out-Null
}
