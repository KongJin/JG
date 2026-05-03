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
    [void]$lines.Add("- Action items: $(@($Report.actionItems).Count)")
    [void]$lines.Add('')
    [void]$lines.Add('## Stage Status')
    foreach ($stage in @($Report.stageResults)) {
        [void]$lines.Add("- $($stage.stage) [$($stage.status)] $($stage.summary)")
    }
    if (@($Report.actionItems).Count -gt 0) {
        [void]$lines.Add('')
        [void]$lines.Add('## Action Items')
        foreach ($item in @($Report.actionItems | Select-Object -First 10)) {
            [void]$lines.Add("- [$($item.severity)] $($item.summary)")
        }
    }
    @($lines)
}

function Merge-ReviewWorkActionItems {
    param([object[]]$Items)

    $merged = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($item in @($Items)) {
        if ($null -eq $item) {
            continue
        }

        $key = '{0}|{1}|{2}|{3}' -f [string]$item.kind, [string]$item.severity, [string]$item.summary, [string]$item.details
        if ($seen.Add($key)) {
            [void]$merged.Add($item)
        }
    }

    @($merged)
}

function New-ReviewWorkAgentTask {
    param(
        [Parameter(Mandatory)][string]$TaskId,
        [Parameter(Mandatory)][string]$SourceArtifactPath,
        [Parameter(Mandatory)][string]$BaseCommitSha,
        [Parameter(Mandatory)][object[]]$Observations,
        [string[]]$CandidateFiles = @(),
        [string]$Goal = 'Resolve the observed technical debt safely.',
        [string]$TaskKind = 'batch_synth',
        [object[]]$CleanupCandidates = @(),
        [string[]]$AutoApplyKinds = @(),
        [string[]]$ReportOnlyKinds = @(),
        [string[]]$ScoutRoots = @()
    )

    [pscustomobject]@{
        taskId = $TaskId
        taskKind = $TaskKind
        sourceArtifactPath = $SourceArtifactPath
        baseCommitSha = $BaseCommitSha
        observations = @($Observations)
        candidateFiles = @($CandidateFiles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        cleanupCandidates = @($CleanupCandidates)
        cleanupPolicy = [pscustomobject]@{
            autoApplyKinds = @($AutoApplyKinds)
            reportOnlyKinds = @($ReportOnlyKinds)
            scoutRoots = @($ScoutRoots)
        }
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
        [Parameter(Mandatory)][string]$SourceArtifactPath,
        [object]$Config = $null
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

    $cleanupSettings = if ($null -ne $Config) { Get-ReviewWorkCleanupSettings -Config $Config } else { $null }
    $agentRunnerEnabled = $false
    if ($null -ne $Config -and $Config.PSObject.Properties.Name -contains 'agentRunner' -and $Config.agentRunner.PSObject.Properties.Name -contains 'enabled') {
        $agentRunnerEnabled = [bool]$Config.agentRunner.enabled
    }
    $llmScoutEnabled = $false
    if ($null -ne $cleanupSettings -and $cleanupSettings.PSObject.Properties.Name -contains 'llmScoutEnabled') {
        $llmScoutEnabled = [bool]$cleanupSettings.llmScoutEnabled
    }

    if ($workItems.Count -eq 0 -and $agentRunnerEnabled -and $llmScoutEnabled) {
        $candidateHints = @(
            @($InputObject.cleanupCandidates) |
                ForEach-Object {
                    @([string]$_.path) + @($_.targetFiles | ForEach-Object { [string]$_ }) + @([string]$_.callerPath)
                } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
                Select-Object -Unique -First ([Math]::Max(0, [int]$cleanupSettings.llmScoutMaxCandidateHints))
        )
        $scoutObservation = [pscustomobject]@{
            findingType = 'cleanup_scout'
            severity = 'medium'
            title = 'Broad LLM cleanup scout'
            evidence = @()
        }
        [void]$queue.Add((New-ReviewWorkAgentTask `
            -TaskId 'cleanup-scout-001' `
            -SourceArtifactPath $SourceArtifactPath `
            -BaseCommitSha ([string]$InputObject.baseCommitSha) `
            -Observations @($scoutObservation) `
            -CandidateFiles $candidateHints `
            -Goal 'Use read-only repo inspection to broadly find delete_unused, simplify_inline, and report-only move_owner cleanup candidates.' `
            -TaskKind 'cleanup_scout' `
            -CleanupCandidates @($InputObject.cleanupCandidates) `
            -AutoApplyKinds @($cleanupSettings.autoApplyKinds) `
            -ReportOnlyKinds @($cleanupSettings.reportOnlyKinds) `
            -ScoutRoots @($cleanupSettings.llmScoutRoots)))
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
        [string[]]$SourceFindingTypes = @(),
        [string]$CleanupKind = ''
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
        cleanupKind = $CleanupKind
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

function Get-ReviewWorkCleanupSettings {
    param([Parameter(Mandatory)][object]$Config)

    if ($Config.PSObject.Properties.Name -notcontains 'cleanup') {
        return [pscustomobject]@{
            enabled = $false
            maxAutoApplyBatchesPerRun = 0
            autoApplyKinds = @()
            reportOnlyKinds = @()
            llmScoutEnabled = $false
            llmScoutMaxCandidateHints = 0
            llmScoutRoots = @()
        }
    }

    $llmScout = if ($Config.cleanup.PSObject.Properties.Name -contains 'llmScout') { $Config.cleanup.llmScout } else { $null }
    [pscustomobject]@{
        enabled = [bool]$Config.cleanup.enabled
        maxAutoApplyBatchesPerRun = if ($Config.cleanup.PSObject.Properties.Name -contains 'maxAutoApplyBatchesPerRun') { [int]$Config.cleanup.maxAutoApplyBatchesPerRun } else { 1 }
        autoApplyKinds = @($Config.cleanup.autoApplyKinds)
        reportOnlyKinds = @($Config.cleanup.reportOnlyKinds)
        llmScoutEnabled = if ($null -ne $llmScout -and $llmScout.PSObject.Properties.Name -contains 'enabled') { [bool]$llmScout.enabled } else { $false }
        llmScoutMaxCandidateHints = if ($null -ne $llmScout -and $llmScout.PSObject.Properties.Name -contains 'maxCandidateHints') { [int]$llmScout.maxCandidateHints } else { 24 }
        llmScoutRoots = if ($null -ne $llmScout -and $llmScout.PSObject.Properties.Name -contains 'roots') { @($llmScout.roots) } else { @('Assets/Scripts', 'Assets/Resources', 'docs', 'tools') }
    }
}

function Get-ReviewWorkCleanupCandidateKey {
    param([Parameter(Mandatory)][object]$Candidate)

    '{0}|{1}' -f [string]$Candidate.kind, [string]$Candidate.path
}

function Get-ReviewWorkDeleteUnusedBatch {
    param(
        [Parameter(Mandatory)][object]$Candidate,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Id
    )

    $targetFiles = @($Candidate.targetFiles | ForEach-Object { ([string]$_).Replace('\', '/') } | Where-Object { Test-ReviewWorkSafeRelativePath -Path ([string]$_) } | Sort-Object -Unique)
    if ($targetFiles.Count -eq 0) {
        return $null
    }

    $operations = @(
        $targetFiles | ForEach-Object {
            [pscustomobject]@{
                type = 'delete_file'
                targetPath = [string]$_
                referenceTokens = @($Candidate.referenceTokens)
            }
        }
    )

    New-ReviewWorkBatch `
        -Id $Id `
        -Kind 'code_fix' `
        -TargetFiles $targetFiles `
        -Reason "Delete unused cleanup candidate '$([string]$Candidate.path)' after reference and GUID scan." `
        -ExpectedFindingsResolved @((Get-ReviewWorkCleanupCandidateKey -Candidate $Candidate)) `
        -Operations @($operations) `
        -OwnerDocs @((Get-ReviewWorkArchitectureOwnerDoc -RepoRoot $RepoRoot)) `
        -SourceFindingTypes @('tech_debt') `
        -CleanupKind 'delete_unused'
}

function Get-ReviewWorkSimplifyInlineBatch {
    param(
        [Parameter(Mandatory)][object]$Candidate,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Id
    )

    $callerPath = ([string]$Candidate.callerPath).Replace('\', '/')
    if (-not (Test-ReviewWorkSafeRelativePath -Path $callerPath)) {
        return $null
    }

    $callerFullPath = Join-Path $RepoRoot $callerPath
    if (-not (Test-Path -LiteralPath $callerFullPath)) {
        return $null
    }

    $callToken = [string]$Candidate.callToken
    $inlineExpression = [string]$Candidate.inlineExpression
    if ([string]::IsNullOrWhiteSpace($callToken) -or [string]::IsNullOrWhiteSpace($inlineExpression)) {
        return $null
    }

    $callerContent = Get-Content -LiteralPath $callerFullPath -Raw
    $callCount = ([regex]::Matches($callerContent, [regex]::Escape($callToken))).Count
    if ($callCount -ne 1) {
        return $null
    }

    $updatedCallerContent = $callerContent.Replace($callToken, "($inlineExpression)")
    $targetFiles = @($Candidate.targetFiles | ForEach-Object { ([string]$_).Replace('\', '/') } | Where-Object { Test-ReviewWorkSafeRelativePath -Path ([string]$_) } | Sort-Object -Unique)
    $deleteTargets = @($targetFiles | Where-Object { $_ -ne $callerPath })
    $operations = [System.Collections.Generic.List[object]]::new()
    [void]$operations.Add([pscustomobject]@{
        type = 'write_file'
        targetPath = $callerPath
        content = $updatedCallerContent
    })
    foreach ($target in @($deleteTargets)) {
        [void]$operations.Add([pscustomobject]@{
            type = 'delete_file'
            targetPath = [string]$target
            referenceTokens = @($Candidate.referenceTokens)
        })
    }

    New-ReviewWorkBatch `
        -Id $Id `
        -Kind 'code_fix' `
        -TargetFiles $targetFiles `
        -Reason "Inline one-use cleanup candidate '$([string]$Candidate.path)' into '$callerPath' and delete the thin helper." `
        -ExpectedFindingsResolved @((Get-ReviewWorkCleanupCandidateKey -Candidate $Candidate)) `
        -Operations @($operations) `
        -OwnerDocs @((Get-ReviewWorkArchitectureOwnerDoc -RepoRoot $RepoRoot)) `
        -SourceFindingTypes @('tech_debt') `
        -CleanupKind 'simplify_inline'
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

function ConvertTo-ReviewWorkCleanupBatches {
    param(
        [Parameter(Mandatory)][object]$InputObject,
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][object]$Config,
        [Parameter(Mandatory)][int]$StartIndex
    )

    $settings = Get-ReviewWorkCleanupSettings -Config $Config
    if (-not $settings.enabled) {
        return @()
    }

    $batches = [System.Collections.Generic.List[object]]::new()
    $counter = $StartIndex
    $autoKinds = @($settings.autoApplyKinds | ForEach-Object { [string]$_ })
    $maxCount = [Math]::Max(0, [int]$settings.maxAutoApplyBatchesPerRun)

    foreach ($candidate in @($InputObject.cleanupCandidates | Where-Object { [bool]$_.autoApply -and [string]$_.kind -in $autoKinds })) {
        if ($batches.Count -ge $maxCount) {
            break
        }

        $batch = $null
        $id = 'cleanup-{0:d3}' -f $counter
        switch ([string]$candidate.kind) {
            'delete_unused' {
                $batch = Get-ReviewWorkDeleteUnusedBatch -Candidate $candidate -RepoRoot $RepoRoot -Id $id
            }
            'simplify_inline' {
                $batch = Get-ReviewWorkSimplifyInlineBatch -Candidate $candidate -RepoRoot $RepoRoot -Id $id
            }
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
    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    $explicitBatches = @(ConvertTo-RuleHarnessRoleBatches -InputObject $input.payload)
    $derivedBatches = @(ConvertTo-ReviewWorkBatchesFromReviewItems -InputObject $input.payload -RepoRoot $RepoRoot -StartIndex ($explicitBatches.Count + 1))
    $cleanupBatches = @(ConvertTo-ReviewWorkCleanupBatches -InputObject $input.payload -RepoRoot $RepoRoot -Config $config -StartIndex ($explicitBatches.Count + $derivedBatches.Count + 1))
    $agentWorkQueue = @(ConvertTo-ReviewWorkAgentQueue -InputObject $input.payload -SourceArtifactPath $input.path -Config $config)
    $agentRun = Invoke-RuleHarnessAgentBatchRunner `
        -RepoRoot $RepoRoot `
        -ConfigPath $ConfigPath `
        -RoleName 'review_work' `
        -AgentWorkQueue $agentWorkQueue `
        -OutputDir $OutputDir
    $agentBatches = @($agentRun.plannedBatches)
    $agentWorkReports = @($agentRun.agentWorkReports)
    $agentActionItems = if ($agentRun.PSObject.Properties.Name -contains 'agentActionItems') { @($agentRun.agentActionItems) } else { @() }
    $batches = @($explicitBatches + $derivedBatches + $cleanupBatches + $agentBatches)
    $mutation = Invoke-RuleHarnessRoleMutation -RepoRoot $RepoRoot -ConfigPath $ConfigPath -PlannedBatches $batches -RoleInputPath $input.path -DryRun:$DryRun
    Add-RuleHarnessAgentWorkState -Report $mutation -AgentWorkQueue $agentWorkQueue -AgentWorkReports $agentWorkReports -WorkLabel 'review work'
    $additionalActionItems = @(@($input.payload.actionItems | Where-Object { [string]$_.kind -eq 'manual-cleanup-review' }) + @($agentActionItems))
    if ($additionalActionItems.Count -gt 0) {
        $existingActionItems = @($mutation.actionItems)
        $mutation | Add-Member -NotePropertyName 'actionItems' -NotePropertyValue @(Merge-ReviewWorkActionItems -Items @($existingActionItems + $additionalActionItems)) -Force
    }
    $mutation | Add-Member -NotePropertyName 'inputReviewPath' -NotePropertyValue $input.path -Force
    $mutation | Add-Member -NotePropertyName 'baseCommitSha' -NotePropertyValue ([string]$input.payload.baseCommitSha) -Force

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $mutation | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-ReviewWorkSummaryLines -Report $mutation | Set-Content -Path $summaryPath -Encoding UTF8
    $mutation
}

Export-ModuleMember -Function Invoke-ReviewWorkHarness
