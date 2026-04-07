param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$ArtifactDir = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarness'),
    [string]$ReportPath,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY,
    [string]$ReviewJsonPath,
    [string]$ApiKey,
    [string]$ApiBaseUrl = $env:RULE_HARNESS_API_BASE_URL,
    [string]$Model = $(if ($env:RULE_HARNESS_MODEL) { $env:RULE_HARNESS_MODEL } elseif ($env:GLM_API_KEY) { 'glm-5' } else { $null }),
    [switch]$DisableLlm,
    [switch]$RequireLlm,
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

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    if (-not [string]::IsNullOrWhiteSpace($env:RULE_HARNESS_API_KEY)) {
        $ApiKey = $env:RULE_HARNESS_API_KEY
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:OPENAI_API_KEY)) {
        $ApiKey = $env:OPENAI_API_KEY
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:GLM_API_KEY)) {
        $ApiKey = $env:GLM_API_KEY
    }
}

$effectiveApiBaseUrl = $ApiBaseUrl
if ([string]::IsNullOrWhiteSpace($effectiveApiBaseUrl)) {
    if ($Model -match '^glm-') {
        $effectiveApiBaseUrl = 'https://open.bigmodel.cn/api/paas/v4'
    }
    else {
        $effectiveApiBaseUrl = 'https://api.openai.com/v1'
    }
}

$llmEnabled = (-not $DisableLlm) -and (-not [string]::IsNullOrWhiteSpace($ApiKey))
if ($RequireLlm -and -not $llmEnabled) {
    throw 'LLM mode was required, but no API key is available. Set RULE_HARNESS_API_KEY, OPENAI_API_KEY, GLM_API_KEY, or pass -ApiKey.'
}

if ($llmEnabled) {
    Write-Host "Rule harness LLM review enabled. Model: $Model"
    Write-Host "Rule harness API base: $effectiveApiBaseUrl"
}
else {
    Write-Host 'Rule harness running in static-only mode.'
}

$report = Invoke-RuleHarness `
    -RepoRoot $RepoRoot `
    -ConfigPath $ConfigPath `
    -ApiKey $ApiKey `
    -ApiBaseUrl $effectiveApiBaseUrl `
    -Model $Model `
    -DisableLlm:$DisableLlm `
    -DryRun:$DryRun `
    -ReviewJsonPath $ReviewJsonPath `
    -SummaryPath $SummaryPath

$report | ConvertTo-Json -Depth 50 | Set-Content -Path $ReportPath -Encoding UTF8
Write-Host "Rule harness report written to $ReportPath"
