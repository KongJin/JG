Import-Module (Join-Path $PSScriptRoot '..\RuleHarness.psm1') -Force

function Get-RecurrencePlanSummaryLines {
    param([Parameter(Mandatory)][object]$Report)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Recurrence Plan Harness')
    [void]$lines.Add('')
    [void]$lines.Add("- Source review: $($Report.sourceReviewPath)")
    [void]$lines.Add("- Source work report: $($Report.sourceWorkReportPath)")
    [void]$lines.Add("- Prevention items: $(@($Report.preventionItems).Count)")
    [void]$lines.Add("- Recommended batches: $(@($Report.recommendedBatches).Count)")
    [void]$lines.Add("- Manual validation required: $($Report.manualValidationRequired)")
    [void]$lines.Add('')
    [void]$lines.Add('## Prevention Items')
    if (@($Report.preventionItems).Count -eq 0) {
        [void]$lines.Add('- none')
    }
    else {
        foreach ($item in @($Report.preventionItems)) {
            [void]$lines.Add("- [$($item.kind)] $($item.summary)")
        }
    }
    @($lines)
}

function Test-RecurrencePlanPreventableSkip {
    param([Parameter(Mandatory)][object]$SkippedBatch)

    $nonPreventableReasonCodes = @(
        'agent-runner-task-limit'
        'agent-runner-disabled'
        'agent-runner-unavailable'
        'agent-runner-timeout'
        'agent-runner-contract-violation'
        'insufficient_snapshot'
    )

    $reasonCode = [string]$SkippedBatch.reasonCode
    if ($reasonCode -like 'insufficient*snapshot*') {
        return $false
    }

    $reasonCode -notin $nonPreventableReasonCodes
}

function Invoke-RecurrencePlanHarness {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$ReviewPath,
        [Parameter(Mandatory)][string]$WorkReportPath,
        [Parameter(Mandatory)][string]$OutputDir
    )

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    $reviewPathResolved = (Resolve-Path -LiteralPath $ReviewPath).Path
    Get-Content -Path $reviewPathResolved -Raw | ConvertFrom-Json | Out-Null
    $workReport = Get-Content -Path (Resolve-Path -LiteralPath $WorkReportPath).Path -Raw | ConvertFrom-Json
    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    $history = Read-RuleHarnessHistoryState -RepoRoot $RepoRoot -Config $config
    $currentCommit = ((& git -C $RepoRoot rev-parse HEAD) | Select-Object -First 1).Trim()

    $memoryPath = Join-Path $RepoRoot ([string]$config.learning.memoryPath)
    $memory = if (Test-Path -LiteralPath $memoryPath) {
        Get-Content -Path $memoryPath -Raw | ConvertFrom-Json
    }
    else {
        [pscustomobject]@{ entries = @() }
    }

    $preventionItems = [System.Collections.Generic.List[object]]::new()
    foreach ($skipped in @($workReport.skippedBatches)) {
        if (-not (Test-RecurrencePlanPreventableSkip -SkippedBatch $skipped)) {
            continue
        }
        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'skipped-batch'
            summary = "Batch $($skipped.id) skipped: $($skipped.reasonCode)"
            relatedPaths = @($skipped.targets)
        })
    }
    if ([bool]$workReport.rollback.performed) {
        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'rollback'
            summary = "Work harness rolled back $(@($workReport.rollback.failedBatches).Count) batch(es)."
            relatedPaths = @()
        })
    }
    if ([int]$workReport.retryAttempts -gt 0) {
        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'retry'
            summary = "Work harness needed $($workReport.retryAttempts) retry attempt(s)."
            relatedPaths = @()
        })
    }
    foreach ($entry in @($workReport.memoryUpdates)) {
        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'memory-update'
            summary = "Memory updated for $($entry.scopePath) hitCount=$($entry.hitCount)."
            relatedPaths = @([string]$entry.scopePath)
        })
    }
    foreach ($entry in @($memory.entries | Where-Object { [int]$_.hitCount -ge 2 } | Select-Object -First 10)) {
        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'recurring-memory'
            summary = "Recurring memory entry: $($entry.symptoms)"
            relatedPaths = @([string]$entry.scopePath)
        })
    }
    foreach ($entry in @($history.entries.Values | Where-Object { $_.lastStatus -in @('failed', 'skipped') } | Select-Object -First 10)) {
        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'history-state'
            summary = "History recorded $($entry.lastStatus): $($entry.lastReason)"
            relatedPaths = @()
        })
    }

    $targetArtifacts = @(
        @($preventionItems | ForEach-Object { @($_.relatedPaths) }) |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Sort-Object -Unique
    )

    $report = [pscustomobject]@{
        runId = [guid]::NewGuid().ToString()
        baseCommitSha = [string]$currentCommit
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        sourceReviewPath = [string]$reviewPathResolved
        sourceWorkReportPath = (Resolve-Path -LiteralPath $WorkReportPath).Path
        preventionItems = @($preventionItems)
        targetArtifacts = @($targetArtifacts)
        recommendedBatches = @()
        manualValidationRequired = (@($preventionItems | Where-Object kind -in @('rollback', 'retry')).Count -gt 0)
    }

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $report | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-RecurrencePlanSummaryLines -Report $report | Set-Content -Path $summaryPath -Encoding UTF8
    $report
}

Export-ModuleMember -Function Invoke-RecurrencePlanHarness
