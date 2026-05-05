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

    if ([string]$SkippedBatch.kind -eq 'agent_work') {
        return $false
    }

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

function Get-RecurrencePlanExistingFilePaths {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [string[]]$Paths = @()
    )

    @(
        foreach ($path in @($Paths)) {
            $relativePath = ([string]$path).Replace('\', '/')
            if ([string]::IsNullOrWhiteSpace($relativePath) -or [System.IO.Path]::IsPathRooted($relativePath) -or $relativePath -match '(^|/)\.\.(/|$)') {
                continue
            }

            $fullPath = Join-Path $RepoRoot $relativePath
            if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
                $relativePath
            }
        }
    ) | Sort-Object -Unique
}

function Test-RecurrencePlanPreventableMemoryEntry {
    param([Parameter(Mandatory)][object]$Entry)

    $scopeType = [string]$Entry.scopeType
    if ($scopeType -eq 'harness-ops') {
        return $false
    }

    $status = [string]$Entry.status
    if ($status -in @('blocked-operational', 'external-blocked', 'operational-blocked')) {
        return $false
    }

    $symptoms = [string]$Entry.symptoms
    if ($symptoms -match 'Status=429|1113|余额不足|无可用资源包|TimeoutSec=|작업 시간이 초과되었습니다') {
        return $false
    }

    $true
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
        $relatedPaths = @(Get-RecurrencePlanExistingFilePaths -RepoRoot $RepoRoot -Paths @($skipped.targets))
        if (@($relatedPaths).Count -eq 0) {
            continue
        }

        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'skipped-batch'
            summary = "Batch $($skipped.id) skipped: $($skipped.reasonCode)"
            relatedPaths = @($relatedPaths)
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
        if ([int]$entry.hitCount -lt 2) {
            continue
        }

        $relatedPaths = @(Get-RecurrencePlanExistingFilePaths -RepoRoot $RepoRoot -Paths @([string]$entry.scopePath))
        if (@($relatedPaths).Count -eq 0) {
            continue
        }

        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'memory-update'
            summary = "Memory updated for $($entry.scopePath) hitCount=$($entry.hitCount)."
            relatedPaths = @($relatedPaths)
        })
    }
    foreach ($entry in @($memory.entries | Where-Object { [int]$_.hitCount -ge 2 } | Select-Object -First 10)) {
        if (-not (Test-RecurrencePlanPreventableMemoryEntry -Entry $entry)) {
            continue
        }

        $relatedPaths = @(Get-RecurrencePlanExistingFilePaths -RepoRoot $RepoRoot -Paths @([string]$entry.scopePath))
        if (@($relatedPaths).Count -eq 0) {
            continue
        }

        [void]$preventionItems.Add([pscustomobject]@{
            kind = 'recurring-memory'
            summary = "Recurring memory entry: $($entry.symptoms)"
            relatedPaths = @($relatedPaths)
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
