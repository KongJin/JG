param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'config.json'),
    [string]$OutputRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Temp/RuleHarnessRoles'),
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

& (Join-Path $PSScriptRoot 'run-tech-debt-review.ps1') `
    -RepoRoot $RepoRoot `
    -ConfigPath $ConfigPath `
    -OutputRoot $OutputRoot `
    -OutputDir $reviewDir | Out-Null

$reviewPath = Join-Path $reviewDir 'report.json'
$review = Get-Content -Path $reviewPath -Raw | ConvertFrom-Json
$skipReviewWork = ([string]$review.severityBand -eq 'critical') -or (@($review.blockers | Where-Object { $_.kind -eq 'compile-gate' -and $_.summary -match 'failed' }).Count -gt 0)

if ($skipReviewWork) {
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
    $workReport = [pscustomobject]@{
        inputReviewPath = (Resolve-Path -LiteralPath $reviewPath).Path
        baseCommitSha = [string]$review.baseCommitSha
        appliedBatches = @()
        skippedBatches = @([pscustomobject]@{
            id = 'review-work-skipped'
            kind = 'pipeline'
            reason = 'critical-or-compile-failed'
            reasonCode = 'critical-or-compile-failed'
            status = 'skipped'
        })
        rollback = [pscustomobject]@{ performed = $false; failedBatches = @() }
        retryAttempts = 0
        memoryUpdates = @()
        stageResults = @([pscustomobject]@{
            stage = 'review_work'
            status = 'skipped'
            attempted = $false
            summary = 'Review work skipped because the review was critical or compile gate failed.'
        })
        failed = $false
    }
    $workReport | ConvertTo-Json -Depth 30 | Set-Content -Path (Join-Path $workDir 'report.json') -Encoding UTF8
    @('# Review Work Harness', '', '- skipped: critical-or-compile-failed') | Set-Content -Path (Join-Path $workDir 'summary.md') -Encoding UTF8
    Set-Content -Path (Join-Path $workDir 'log.txt') -Value 'Review work skipped by pipeline guard.' -Encoding UTF8
}
else {
    & (Join-Path $PSScriptRoot 'run-review-work.ps1') `
        -RepoRoot $RepoRoot `
        -ConfigPath $ConfigPath `
        -ReviewPath $reviewPath `
        -OutputRoot $OutputRoot `
        -OutputDir $workDir `
        -DryRun:$DryRun | Out-Null
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
