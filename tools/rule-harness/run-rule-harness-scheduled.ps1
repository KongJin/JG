param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessScheduled'),
    [string]$ApiKey,
    [string]$ApiBaseUrl,
    [string]$Model,
    [string]$MutationMode = 'code_and_rules',
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
$docProposalPath = Join-Path $runDir 'rule-harness-doc-proposals.md'
$logPath = Join-Path $runDir 'rule-harness.log'
$latestPointer = Join-Path $OutputRoot 'latest-run.txt'
$latestStatusPath = Join-Path $OutputRoot 'latest-status.json'
$runTimestamp = Get-Date -Format o
$scheduledError = $null

Start-Transcript -Path $logPath -Force | Out-Null
try {
    Write-Host "Rule harness scheduled run started at $runTimestamp"
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
        -MutationMode $MutationMode `
        -EnableMutation `
        -RequireLlm:$requireLlmForRun `
        -DisableLlm:$DisableLlm `
        -LogPathHint $logPath

    Write-Host "Rule harness scheduled run finished at $(Get-Date -Format o)"
}
catch {
    $scheduledError = $_.Exception
    Write-Host "Rule harness scheduled run failed at $(Get-Date -Format o): $($scheduledError.Message)"
}
finally {
    $report = $null
    if (Test-Path -LiteralPath $reportPath) {
        $report = Get-Content -Path $reportPath -Raw | ConvertFrom-Json
    }

    $completedScopes = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $report) {
        foreach ($scope in @($report.completedScopes)) {
            [void]$completedScopes.Add([string]$scope)
        }
    }

    $topActionItems = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $report -and @($report.actionItems).Count -gt 0) {
        foreach ($item in @($report.actionItems | Select-Object -First 5)) {
            [void]$topActionItems.Add($item)
        }
    }

    $topPromotionCandidates = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $report -and @($report.promotionCandidates).Count -gt 0) {
        foreach ($candidate in @($report.promotionCandidates | Select-Object -First 3)) {
            [void]$topPromotionCandidates.Add($candidate)
        }
    }

    $topDocProposals = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $report -and @($report.docProposals).Count -gt 0) {
        foreach ($proposal in @($report.docProposals | Select-Object -First 5)) {
            [void]$topDocProposals.Add($proposal)
        }
    }

    $latestStatus = [pscustomobject]@{
        runDir                = $runDir
        reportPath            = $reportPath
        summaryPath           = $summaryPath
        docProposalPath       = $docProposalPath
        logPath               = $logPath
        failed                = if ($null -ne $report) { [bool]$report.failed } else { $true }
        llmEnabled            = if ($null -ne $report) { [bool]$report.execution.llmEnabled } else { (-not $DisableLlm) }
        timestamp             = $runTimestamp
        currentScope          = if ($null -ne $report -and $null -ne $report.stoppedScope) { [string]$report.stoppedScope.scopeId } else { $null }
        completedScopes       = $completedScopes
        nextScope             = if ($null -ne $report -and @($report.nextScopeCandidates).Count -gt 0) { [string]$report.nextScopeCandidates[0] } else { $null }
        topActionItems        = $topActionItems
        topPromotionCandidates = $topPromotionCandidates
        topDocProposals       = $topDocProposals
        retryCount            = if ($null -ne $report) { [int]$report.retryAttempts } else { 0 }
        learnedAnything       = if ($null -ne $report) { (@($report.memoryUpdates).Count -gt 0) -or (@($report.promotionCandidates).Count -gt 0) } else { $false }
        errorMessage          = if ($null -ne $scheduledError) { $scheduledError.Message } else { $null }
    }

    Set-Content -Path $latestPointer -Value $runDir -Encoding UTF8
    $latestStatus | ConvertTo-Json -Depth 20 | Set-Content -Path $latestStatusPath -Encoding UTF8
    Stop-Transcript | Out-Null
}

if ($null -ne $scheduledError) {
    throw $scheduledError
}
