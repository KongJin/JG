Import-Module (Join-Path $PSScriptRoot '..\RuleHarness.psm1') -Force

function Get-ReviewWorkSummaryLines {
    param([Parameter(Mandatory)][object]$Report)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Review Work Harness')
    [void]$lines.Add('')
    [void]$lines.Add("- Input review: $($Report.inputReviewPath)")
    [void]$lines.Add("- Applied batches: $(@($Report.appliedBatches).Count)")
    [void]$lines.Add("- Skipped batches: $(@($Report.skippedBatches).Count)")
    [void]$lines.Add("- Retry attempts: $($Report.retryAttempts)")
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

function New-ReviewWorkAgentTask {
    param(
        [Parameter(Mandatory)][string]$TaskId,
        [Parameter(Mandatory)][string]$SourceArtifactPath,
        [Parameter(Mandatory)][string]$BaseCommitSha,
        [Parameter(Mandatory)][object[]]$Observations,
        [string[]]$CandidateFiles = @(),
        [string]$Goal = 'Resolve the observed technical debt safely.'
    )

    [pscustomobject]@{
        taskId = $TaskId
        sourceArtifactPath = $SourceArtifactPath
        baseCommitSha = $BaseCommitSha
        observations = @($Observations)
        candidateFiles = @($CandidateFiles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        goal = $Goal
        constraints = @(
            'Do not bypass dirty target guard.'
            'Do not bypass ownership guard.'
            'Keep changes scoped to the observed issue.'
            'Run validation through the rule harness mutation guard before commit.'
        )
        status = 'pending'
    }
}

function Get-ReviewWorkQueuePriority {
    param([Parameter(Mandatory)][string]$Title)

    $priorityByTitle = @{
        'Swallowed exception' = 1
        'Runtime Resources.Load dependency' = 2
        'Runtime object lookup API' = 3
        'Runtime legacy compatibility path' = 9
    }

    if ($priorityByTitle.ContainsKey($Title)) {
        return [int]$priorityByTitle[$Title]
    }

    5
}

function ConvertTo-ReviewWorkAgentQueue {
    param(
        [Parameter(Mandatory)][object]$InputObject,
        [Parameter(Mandatory)][string]$SourceArtifactPath
    )

    $queue = [System.Collections.Generic.List[object]]::new()
    $counter = 1
    $workItems = @(
        @($InputObject.reviewItems) |
            Where-Object {
                [string]$_.severity -in @('high', 'medium') -and
                [string]$_.title -ne 'Runtime placeholder or generated stub'
            }
    )

    $groups = @(
        $workItems |
            Group-Object title |
            Sort-Object @{ Expression = {
                Get-ReviewWorkQueuePriority -Title ([string]$_.Name)
            } }, Name
    )

    foreach ($group in @($groups)) {
        $observations = @($group.Group)
        $candidateFiles = @(
            $observations |
                ForEach-Object { @($_.evidence) } |
                ForEach-Object { [string]$_.path } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
        )

        [void]$queue.Add((New-ReviewWorkAgentTask `
            -TaskId ('review-work-{0:d3}' -f $counter) `
            -SourceArtifactPath $SourceArtifactPath `
            -BaseCommitSha ([string]$InputObject.baseCommitSha) `
            -Observations $observations `
            -CandidateFiles $candidateFiles `
            -Goal ("Investigate and safely resolve review observations: {0}" -f [string]$group.Name)))
        $counter++
    }

    @($queue)
}

function Get-ReviewWorkFindingKey {
    param([Parameter(Mandatory)][object]$Finding)

    $evidence = @($Finding.evidence | Select-Object -First 1)
    $path = if ($evidence.Count -gt 0) { [string]$evidence[0].path } else { '' }
    $line = if ($evidence.Count -gt 0) { [string]$evidence[0].line } else { '' }
    '{0}|{1}|{2}|{3}' -f [string]$Finding.findingType, [string]$Finding.title, $path, $line
}

function Test-ReviewWorkSafeRelativePath {
    param([Parameter(Mandatory)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('\', '/')
    if ($normalized.StartsWith('/') -or [System.IO.Path]::IsPathRooted($Path)) {
        return $false
    }

    foreach ($segment in @($normalized -split '/')) {
        if ($segment -eq '..') {
            return $false
        }
    }

    $true
}

function New-ReviewWorkBatch {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Kind,
        [Parameter(Mandatory)][string[]]$TargetFiles,
        [Parameter(Mandatory)][string]$Reason,
        [Parameter(Mandatory)][object[]]$Operations,
        [string[]]$ExpectedFindingsResolved = @(),
        [string[]]$FeatureNames = @(),
        [string[]]$OwnerDocs = @('AGENTS.md'),
        [string[]]$SourceFindingTypes = @()
    )

    [pscustomobject]@{
        id = $Id
        kind = $Kind
        targetFiles = @($TargetFiles)
        reason = $Reason
        validation = @('rule_harness_tests')
        expectedFindingsResolved = @($ExpectedFindingsResolved)
        status = 'planned'
        featureNames = @($FeatureNames)
        ownerDocs = @($OwnerDocs)
        sourceFindingTypes = @($SourceFindingTypes)
        fingerprint = $null
        riskScore = $null
        riskLabel = $null
        ownershipStatus = 'pending'
        operations = @($Operations)
    }
}

function Get-ReviewWorkArchitectureOwnerDoc {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $ownerDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot
    if ([string]::IsNullOrWhiteSpace([string]$ownerDoc)) {
        return 'AGENTS.md'
    }

    [string]$ownerDoc
}

function Get-ReviewWorkFeatureNameFromPath {
    param([string]$Path)

    $normalized = $Path.Replace('\', '/')
    if ($normalized -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
        return $Matches['feature']
    }

    ''
}

function Get-ReviewWorkPlaceholderSetupBatch {
    param(
        [Parameter(Mandatory)][object]$Finding,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Id
    )

    $evidence = @($Finding.evidence | Select-Object -First 1)
    if ($evidence.Count -eq 0) {
        return $null
    }

    $targetPath = ([string]$evidence[0].path).Replace('\', '/')
    if (-not (Test-ReviewWorkSafeRelativePath -Path $targetPath)) {
        return $null
    }
    if ($targetPath -notmatch '^Assets/Scripts/Features/[^/]+/[^/]+Setup\.cs$') {
        return $null
    }

    $fullPath = Join-Path $RepoRoot $targetPath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return $null
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -notmatch '(?is)generated composition root placeholder') {
        return $null
    }
    if ($content -notmatch '(?is)namespace\s+(?<namespace>[A-Za-z0-9_.]+)\s*\{.*?public\s+sealed\s+class\s+(?<class>[A-Za-z0-9_]+)\s*\{\s*\}\s*\}\s*$') {
        return $null
    }

    $namespace = $Matches['namespace']
    $className = $Matches['class']
    $featureName = Get-ReviewWorkFeatureNameFromPath -Path $targetPath
    $summaryName = if ([string]::IsNullOrWhiteSpace($featureName)) { $className } else { $featureName }
    $updatedContent = @"
namespace $namespace
{
    /// <summary>
    /// Composition root for the $summaryName feature.
    /// </summary>
    public sealed class $className
    {
    }
}
"@

    New-ReviewWorkBatch `
        -Id $Id `
        -Kind 'code_fix' `
        -TargetFiles @($targetPath) `
        -Reason "Replace generated placeholder text in '$targetPath' with a concrete feature composition-root marker." `
        -ExpectedFindingsResolved @((Get-ReviewWorkFindingKey -Finding $Finding)) `
        -Operations @([pscustomobject]@{
            type = 'write_file'
            targetPath = $targetPath
            content = $updatedContent
        }) `
        -FeatureNames @($featureName) `
        -OwnerDocs @((Get-ReviewWorkArchitectureOwnerDoc -RepoRoot $RepoRoot)) `
        -SourceFindingTypes @([string]$Finding.findingType)
}

function ConvertTo-ReviewWorkBatchesFromReviewItems {
    param(
        [Parameter(Mandatory)][object]$InputObject,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][int]$StartIndex
    )

    $batches = [System.Collections.Generic.List[object]]::new()
    $counter = $StartIndex

    foreach ($finding in @($InputObject.reviewItems)) {
        $title = [string]$finding.title
        $batch = $null
        if ($title -eq 'Runtime placeholder or generated stub') {
            $batch = Get-ReviewWorkPlaceholderSetupBatch -Finding $finding -RepoRoot $RepoRoot -Id ('batch-{0:d3}' -f $counter)
        }

        if ($null -ne $batch) {
            [void]$batches.Add($batch)
            $counter++
        }
    }

    @($batches)
}

function Invoke-ReviewWorkHarness {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$ReviewPath,
        [Parameter(Mandatory)][string]$OutputDir,
        [switch]$DryRun
    )

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    $input = Test-RuleHarnessRoleInput -RepoRoot $RepoRoot -InputPath $ReviewPath -ThrowOnError
    $explicitBatches = @(ConvertTo-RuleHarnessRoleBatches -InputObject $input.payload)
    $derivedBatches = @(ConvertTo-ReviewWorkBatchesFromReviewItems -InputObject $input.payload -RepoRoot $RepoRoot -StartIndex ($explicitBatches.Count + 1))
    $agentWorkQueue = @(ConvertTo-ReviewWorkAgentQueue -InputObject $input.payload -SourceArtifactPath $input.path)
    $agentRun = Invoke-RuleHarnessAgentBatchRunner `
        -RepoRoot $RepoRoot `
        -ConfigPath $ConfigPath `
        -RoleName 'review_work' `
        -AgentWorkQueue $agentWorkQueue `
        -OutputDir $OutputDir
    $agentBatches = @($agentRun.plannedBatches)
    $agentWorkReports = @($agentRun.agentWorkReports)
    $batches = @($explicitBatches + $derivedBatches + $agentBatches)
    $mutation = Invoke-RuleHarnessRoleMutation -RepoRoot $RepoRoot -ConfigPath $ConfigPath -PlannedBatches $batches -RoleInputPath $input.path -DryRun:$DryRun
    Add-RuleHarnessAgentWorkState -Report $mutation -AgentWorkQueue $agentWorkQueue -AgentWorkReports $agentWorkReports -WorkLabel 'review work'
    $mutation | Add-Member -NotePropertyName 'inputReviewPath' -NotePropertyValue $input.path -Force
    $mutation | Add-Member -NotePropertyName 'baseCommitSha' -NotePropertyValue ([string]$input.payload.baseCommitSha) -Force

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $mutation | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-ReviewWorkSummaryLines -Report $mutation | Set-Content -Path $summaryPath -Encoding UTF8
    $mutation
}

Export-ModuleMember -Function Invoke-ReviewWorkHarness
