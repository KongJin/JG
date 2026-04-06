param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$ArtifactDir = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarness'),
    [string]$ReportPath,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY,
    [string]$ReviewJsonPath,
    [string]$Model = $env:RULE_HARNESS_MODEL,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'RuleHarness.psm1') -Force

if (-not (Test-Path -LiteralPath $ArtifactDir)) {
    New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null
}

if (-not $ReportPath) {
    $ReportPath = Join-Path $ArtifactDir 'rule-harness-report.json'
}

$report = Invoke-RuleHarness `
    -RepoRoot $RepoRoot `
    -ConfigPath $ConfigPath `
    -ApiKey $env:OPENAI_API_KEY `
    -Model $Model `
    -DryRun:$DryRun `
    -ReviewJsonPath $ReviewJsonPath `
    -SummaryPath $SummaryPath

$report | ConvertTo-Json -Depth 50 | Set-Content -Path $ReportPath -Encoding UTF8
Write-Host "Rule harness report written to $ReportPath"
