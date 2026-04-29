Import-Module (Join-Path $PSScriptRoot '..\RuleHarness.psm1') -Force

function Get-RecurrenceWorkSummaryLines {
    param([Parameter(Mandatory)][object]$Report)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Recurrence Work Harness')
    [void]$lines.Add('')
    [void]$lines.Add("- Input plan: $($Report.inputPreventionPlanPath)")
    [void]$lines.Add("- Applied batches: $(@($Report.appliedBatches).Count)")
    [void]$lines.Add("- Skipped batches: $(@($Report.skippedBatches).Count)")
    [void]$lines.Add("- Failed: $($Report.failed)")
    [void]$lines.Add("- Agent work queue: $(@($Report.agentWorkQueue).Count)")
    [void]$lines.Add("- Agent work reports: $(@($Report.agentWorkReports).Count)")
    [void]$lines.Add('')
    [void]$lines.Add('## Stage Status')
    foreach ($stage in @($Report.stageResults)) {
        [void]$lines.Add("- $($stage.stage) [$($stage.status)] $($stage.summary)")
    }
    @($lines)
}

function New-RecurrenceWorkAgentTask {
    param(
        [Parameter(Mandatory)][string]$TaskId,
        [Parameter(Mandatory)][string]$SourceArtifactPath,
        [Parameter(Mandatory)][string]$BaseCommitSha,
        [Parameter(Mandatory)][object[]]$PreventionItems,
        [string[]]$CandidateFiles = @()
    )

    [pscustomobject]@{
        taskId = $TaskId
        sourceArtifactPath = $SourceArtifactPath
        baseCommitSha = $BaseCommitSha
        preventionItems = @($PreventionItems)
        candidateFiles = @($CandidateFiles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        goal = 'Implement recurrence prevention work safely from the prevention plan.'
        constraints = @(
            'Do not bypass dirty target guard.'
            'Do not bypass ownership guard.'
            'Rules/tooling/docs changes must satisfy recurrence closeout requirements.'
            'Run validation through the rule harness mutation guard before commit.'
        )
        status = 'pending'
    }
}

function ConvertTo-RecurrenceWorkAgentQueue {
    param(
        [Parameter(Mandatory)][object]$InputObject,
        [Parameter(Mandatory)][string]$SourceArtifactPath
    )

    $queue = [System.Collections.Generic.List[object]]::new()
    $counter = 1
    foreach ($group in @(@($InputObject.preventionItems) | Group-Object kind)) {
        $items = @($group.Group)
        $candidateFiles = @(
            $items |
                ForEach-Object {
                    if ($_.PSObject.Properties.Name -contains 'targetPath') { [string]$_.targetPath }
                    if ($_.PSObject.Properties.Name -contains 'targetPaths') { @($_.targetPaths) }
                    if ($_.PSObject.Properties.Name -contains 'relatedPaths') { @($_.relatedPaths) }
                } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
        )

        [void]$queue.Add((New-RecurrenceWorkAgentTask `
            -TaskId ('recurrence-work-{0:d3}' -f $counter) `
            -SourceArtifactPath $SourceArtifactPath `
            -BaseCommitSha ([string]$InputObject.baseCommitSha) `
            -PreventionItems $items `
            -CandidateFiles $candidateFiles))
        $counter++
    }

    @($queue)
}

function Invoke-RecurrenceWorkHarness {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$PlanPath,
        [Parameter(Mandatory)][string]$OutputDir,
        [switch]$DryRun
    )

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    $input = Test-RuleHarnessRoleInput -RepoRoot $RepoRoot -InputPath $PlanPath -ThrowOnError
    $agentWorkQueue = @(ConvertTo-RecurrenceWorkAgentQueue -InputObject $input.payload -SourceArtifactPath $input.path)
    $agentRun = Invoke-RuleHarnessAgentBatchRunner `
        -RepoRoot $RepoRoot `
        -ConfigPath $ConfigPath `
        -RoleName 'recurrence_work' `
        -AgentWorkQueue $agentWorkQueue `
        -OutputDir $OutputDir
    $agentBatches = @($agentRun.plannedBatches)
    $agentWorkReports = @($agentRun.agentWorkReports)
    $explicitBatches = @(ConvertTo-RuleHarnessRoleBatches -InputObject $input.payload)
    $batches = @($explicitBatches + $agentBatches)
    $mutation = Invoke-RuleHarnessRoleMutation -RepoRoot $RepoRoot -ConfigPath $ConfigPath -PlannedBatches $batches -RoleInputPath $input.path -DryRun:$DryRun
    Add-RuleHarnessAgentWorkState -Report $mutation -AgentWorkQueue $agentWorkQueue -AgentWorkReports $agentWorkReports -WorkLabel 'recurrence work'
    $mutation | Add-Member -NotePropertyName 'inputPreventionPlanPath' -NotePropertyValue $input.path -Force
    $mutation | Add-Member -NotePropertyName 'baseCommitSha' -NotePropertyValue ([string]$input.payload.baseCommitSha) -Force

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $mutation | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-RecurrenceWorkSummaryLines -Report $mutation | Set-Content -Path $summaryPath -Encoding UTF8
    $mutation
}

Export-ModuleMember -Function Invoke-RecurrenceWorkHarness
