param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessRoles'),
    [ValidateSet('FeatureScope', 'ProjectSurface', 'Deep')]
    [string]$TechDebtMode = 'FeatureScope',
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PipelineHeadSha {
    param([Parameter(Mandatory)][string]$RepoRoot)

    ((& git -C $RepoRoot rev-parse HEAD) | Select-Object -First 1).Trim()
}

function Invoke-PipelineTechDebtReview {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$OutputRoot,
        [Parameter(Mandatory)][string]$OutputDir,
        [Parameter(Mandatory)][string]$Mode
    )

    & (Join-Path $PSScriptRoot 'run-tech-debt-review.ps1') `
        -RepoRoot $RepoRoot `
        -ConfigPath $ConfigPath `
        -OutputRoot $OutputRoot `
        -OutputDir $OutputDir `
        -Mode $Mode | Out-Null
}

function Write-PipelineSkippedReviewWork {
    param(
        [Parameter(Mandatory)][string]$OutputDir,
        [Parameter(Mandatory)][string]$ReviewPath,
        [Parameter(Mandatory)][object]$Review,
        [Parameter(Mandatory)][string]$ReasonCode,
        [Parameter(Mandatory)][string]$Summary
    )

    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    $workReport = [pscustomobject]@{
        inputReviewPath = (Resolve-Path -LiteralPath $ReviewPath).Path
        baseCommitSha = [string]$Review.baseCommitSha
        appliedBatches = @()
        skippedBatches = @([pscustomobject]@{
            id = 'review-work-skipped'
            kind = 'pipeline'
            reason = $Summary
            reasonCode = $ReasonCode
            status = 'skipped'
        })
        rollback = [pscustomobject]@{ performed = $false; failedBatches = @() }
        retryAttempts = 0
        memoryUpdates = @()
        stageResults = @([pscustomobject]@{
            stage = 'review_work'
            status = 'skipped'
            attempted = $false
            summary = $Summary
        })
        failed = $false
    }
    $workReport | ConvertTo-Json -Depth 30 | Set-Content -Path (Join-Path $OutputDir 'report.json') -Encoding UTF8
    @('# Review Work Harness', '', "- skipped: $ReasonCode", "- reason: $Summary") | Set-Content -Path (Join-Path $OutputDir 'summary.md') -Encoding UTF8
    Set-Content -Path (Join-Path $OutputDir 'log.txt') -Value $Summary -Encoding UTF8
}

if (-not (Test-Path -LiteralPath $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

$runRoot = Join-Path $OutputRoot (Get-Date -Format 'yyyyMMdd-HHmmss')
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
Set-Content -Path (Join-Path $OutputRoot 'latest-pipeline.txt') -Value $runRoot -Encoding UTF8

$reviewDir = Join-Path $runRoot '01-tech-debt-review'
$workDir = Join-Path $runRoot '02-review-work'
$planDir = Join-Path $runRoot '03-recurrence-plan'
$recurrenceWorkDir = Join-Path $runRoot '04-recurrence-work'

Invoke-PipelineTechDebtReview `
    -RepoRoot $RepoRoot `
    -ConfigPath $ConfigPath `
    -OutputRoot $OutputRoot `
    -OutputDir $reviewDir `
    -Mode $TechDebtMode

$reviewPath = Join-Path $reviewDir 'report.json'
$review = Get-Content -Path $reviewPath -Raw | ConvertFrom-Json
$reviewBaseCommit = [string]$review.baseCommitSha
$currentHead = Get-PipelineHeadSha -RepoRoot $RepoRoot
if (-not [string]::IsNullOrWhiteSpace($reviewBaseCommit) -and $reviewBaseCommit -ne $currentHead) {
    Write-Host "Review artifact base commit changed during pipeline. Refreshing review once. Review=$reviewBaseCommit Head=$currentHead"
    Invoke-PipelineTechDebtReview `
        -RepoRoot $RepoRoot `
        -ConfigPath $ConfigPath `
        -OutputRoot $OutputRoot `
        -OutputDir $reviewDir `
        -Mode $TechDebtMode
    $review = Get-Content -Path $reviewPath -Raw | ConvertFrom-Json
    $reviewBaseCommit = [string]$review.baseCommitSha
    $currentHead = Get-PipelineHeadSha -RepoRoot $RepoRoot
}

$skipReasonCode = $null
$skipSummary = $null
if ([string]$review.severityBand -eq 'critical') {
    $skipReasonCode = 'critical-review'
    $skipSummary = 'Review work skipped because the tech-debt review severity was critical.'
}
elseif (@($review.blockers | Where-Object { $_.kind -eq 'compile-gate' -and $_.summary -match 'failed' }).Count -gt 0) {
    $skipReasonCode = 'compile-gate-failed'
    $skipSummary = 'Review work skipped because the compile gate failed.'
}
elseif (-not [string]::IsNullOrWhiteSpace($reviewBaseCommit) -and $reviewBaseCommit -ne $currentHead) {
    $skipReasonCode = 'stale-review-artifact'
    $skipSummary = "Review work skipped because the review artifact commit changed during the pipeline. Review=$reviewBaseCommit Head=$currentHead."
}
$skipReviewWork = -not [string]::IsNullOrWhiteSpace($skipReasonCode)

if ($skipReviewWork) {
    Write-PipelineSkippedReviewWork `
        -OutputDir $workDir `
        -ReviewPath $reviewPath `
        -Review $review `
        -ReasonCode $skipReasonCode `
        -Summary $skipSummary
}
else {
    try {
        & (Join-Path $PSScriptRoot 'run-review-work.ps1') `
            -RepoRoot $RepoRoot `
            -ConfigPath $ConfigPath `
            -ReviewPath $reviewPath `
            -OutputRoot $OutputRoot `
            -OutputDir $workDir `
            -DryRun:$DryRun | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'baseCommitSha must match current HEAD') {
            throw
        }

        $currentHead = Get-PipelineHeadSha -RepoRoot $RepoRoot
        Write-PipelineSkippedReviewWork `
            -OutputDir $workDir `
            -ReviewPath $reviewPath `
            -Review $review `
            -ReasonCode 'stale-review-artifact' `
            -Summary "Review work skipped because HEAD changed before mutation. Review=$([string]$review.baseCommitSha) Head=$currentHead."
    }
}

$workReportPath = Join-Path $workDir 'report.json'
& (Join-Path $PSScriptRoot 'run-recurrence-plan.ps1') `
    -RepoRoot $RepoRoot `
    -ConfigPath $ConfigPath `
    -ReviewPath $reviewPath `
    -WorkReportPath $workReportPath `
    -OutputRoot $OutputRoot `
    -OutputDir $planDir | Out-Null

$planPath = Join-Path $planDir 'report.json'
& (Join-Path $PSScriptRoot 'run-recurrence-work.ps1') `
    -RepoRoot $RepoRoot `
    -ConfigPath $ConfigPath `
    -PlanPath $planPath `
    -OutputRoot $OutputRoot `
    -OutputDir $recurrenceWorkDir `
    -DryRun:$DryRun | Out-Null

[pscustomobject]@{
    runRoot = $runRoot
    reviewPath = $reviewPath
    workReportPath = $workReportPath
    recurrencePlanPath = $planPath
    recurrenceWorkReportPath = (Join-Path $recurrenceWorkDir 'report.json')
}
