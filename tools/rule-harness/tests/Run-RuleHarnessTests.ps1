Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Import-Module (Join-Path $repoRoot 'tools/rule-harness/RuleHarness.psm1') -Force
$config = Get-RuleHarnessConfig -ConfigPath (Join-Path $repoRoot 'tools/rule-harness/config.json')
$scratchRoot = Join-Path $repoRoot 'Temp/RuleHarnessFixtureTests'

if (Test-Path -LiteralPath $scratchRoot) {
    Remove-Item -LiteralPath $scratchRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $scratchRoot -Force | Out-Null

function Assert-RuleHarness {
    param(
        [Parameter(Mandatory)]
        [bool]$Condition,
        [Parameter(Mandatory)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function New-RuleHarnessTestConfig {
    param(
        [Parameter(Mandatory)]
        [object]$SourceConfig,
        [int]$MaxScopesPerRun = 4
    )

    $clone = ($SourceConfig | ConvertTo-Json -Depth 50) | ConvertFrom-Json
    $clone.scan.maxScopesPerRun = $MaxScopesPerRun
    $clone.validation.harnessTestScript = 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1'
    $clone.history.statePath = 'Temp/RuleHarnessState/history.json'
    $clone.state.featureScanStatePath = 'Temp/RuleHarnessState/feature-scan-state.json'
    $clone.state.docProposalBacklogPath = 'Temp/RuleHarnessState/doc-proposals.json'
    $clone
}

function Add-FeatureFixture {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [string]$FeatureName,
        [string]$ApplicationContent,
        [switch]$SkipSetup
    )

    $featureRoot = Join-Path $RepoPath "Assets/Scripts/Features/$FeatureName"
    $appRoot = Join-Path $featureRoot 'Application'
    New-Item -ItemType Directory -Path $appRoot -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath "Tests/$FeatureName") -Force | Out-Null

    if (-not $SkipSetup) {
        Set-Content -Path (Join-Path $featureRoot "${FeatureName}Setup.cs") -Value @"
namespace Features.$FeatureName
{
    public sealed class ${FeatureName}Setup
    {
    }
}
"@ -Encoding UTF8
    }

    if (-not [string]::IsNullOrWhiteSpace($ApplicationContent)) {
        Set-Content -Path (Join-Path $appRoot "${FeatureName}Service.cs") -Value $ApplicationContent -Encoding UTF8
    }
}

function Initialize-RuleHarnessScopeRepo {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [object[]]$Features,
        [string]$ArchitectureDocContent = '# Architecture Rules'
    )

    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'docs/rules') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'Temp') -Force | Out-Null
    Set-Content -Path (Join-Path $RepoPath 'CLAUDE.md') -Value 'Read `/docs/rules/architecture-rules.md`.' -Encoding UTF8
    Set-Content -Path (Join-Path $RepoPath 'docs/rules/architecture-rules.md') -Value $ArchitectureDocContent -Encoding UTF8

    foreach ($feature in @($Features)) {
        $skipSetup = $false
        if ($feature.PSObject.Properties.Name -contains 'SkipSetup') {
            $skipSetup = [bool]$feature.SkipSetup
        }
        Add-FeatureFixture `
            -RepoPath $RepoPath `
            -FeatureName ([string]$feature.Name) `
            -ApplicationContent ([string]$feature.ApplicationContent) `
            -SkipSetup:$skipSetup
    }

    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'tools') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot 'tools/rule-harness') -Destination (Join-Path $RepoPath 'tools/rule-harness') -Recurse -Force
    Set-Content -Path (Join-Path $RepoPath 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1') -Value 'Write-Host ''fixture harness tests passed''' -Encoding UTF8

    Push-Location $RepoPath
    try {
        git init | Out-Null
        git config user.name 'rule-harness-tests'
        git config user.email 'rule-harness-tests@example.com'
        git add .
        git commit -m 'init' | Out-Null
    }
    finally {
        Pop-Location
    }
}

function Write-FeatureScanStateFixture {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [object[]]$Entries
    )

    $statePath = Join-Path $RepoPath 'Temp/RuleHarnessState/feature-scan-state.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $statePath) -Force | Out-Null
    [pscustomobject]@{
        schemaVersion = 1
        entries       = $Entries
    } | ConvertTo-Json -Depth 20 | Set-Content -Path $statePath -Encoding UTF8
}

$orderingRepo = Join-Path $scratchRoot 'feature-ordering'
Initialize-RuleHarnessScopeRepo -RepoPath $orderingRepo -Features @(
    [pscustomobject]@{ Name = 'Aged'; ApplicationContent = 'namespace Features.Aged.Application { public sealed class AgedService { } }' },
    [pscustomobject]@{ Name = 'Fresh'; ApplicationContent = 'namespace Features.Fresh.Application { public sealed class FreshService { } }' },
    [pscustomobject]@{ Name = 'NewA'; ApplicationContent = 'namespace Features.NewA.Application { public sealed class NewAService { } }' },
    [pscustomobject]@{ Name = 'NewB'; ApplicationContent = 'namespace Features.NewB.Application { public sealed class NewBService { } }' }
)
Write-FeatureScanStateFixture -RepoPath $orderingRepo -Entries @(
    [pscustomobject]@{ scopeId = 'Aged'; lastCheckedAtUtc = '2026-04-01T00:00:00Z'; lastResult = 'clean'; lastFindingSeverity = $null; lastRunId = 'old'; lastCommitSha = 'a'; lastStoppedReason = $null },
    [pscustomobject]@{ scopeId = 'Fresh'; lastCheckedAtUtc = '2026-04-08T00:00:00Z'; lastResult = 'clean'; lastFindingSeverity = $null; lastRunId = 'new'; lastCommitSha = 'b'; lastStoppedReason = $null }
)
$orderingConfig = New-RuleHarnessTestConfig -SourceConfig $config -MaxScopesPerRun 4
$orderingReport = Invoke-RuleHarness `
    -RepoRoot $orderingRepo `
    -ConfigPath (Join-Path $orderingRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition ((@($orderingReport.scannedScopes) -join ',') -eq 'NewA,NewB,Aged,Fresh') `
    -Message 'Expected feature scan order to prefer unchecked scopes, then older checked scopes, then name order.'

$quotaRepo = Join-Path $scratchRoot 'feature-quota'
Initialize-RuleHarnessScopeRepo -RepoPath $quotaRepo -Features @(
    [pscustomobject]@{ Name = 'One'; ApplicationContent = 'namespace Features.One.Application { public sealed class OneService { } }' },
    [pscustomobject]@{ Name = 'Two'; ApplicationContent = 'namespace Features.Two.Application { public sealed class TwoService { } }' },
    [pscustomobject]@{ Name = 'Three'; ApplicationContent = 'namespace Features.Three.Application { public sealed class ThreeService { } }' },
    [pscustomobject]@{ Name = 'Four'; ApplicationContent = 'namespace Features.Four.Application { public sealed class FourService { } }' },
    [pscustomobject]@{ Name = 'Five'; ApplicationContent = 'namespace Features.Five.Application { public sealed class FiveService { } }' }
)
$quotaConfig = New-RuleHarnessTestConfig -SourceConfig $config -MaxScopesPerRun 4
$quotaReport = Invoke-RuleHarness `
    -RepoRoot $quotaRepo `
    -ConfigPath (Join-Path $quotaRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition (@($quotaReport.scannedScopes).Count -eq 4 -and @($quotaReport.completedScopes).Count -eq 4 -and $null -eq $quotaReport.stoppedScope) `
    -Message 'Expected clean runs to stop after the configured max scope count.'

$lowRepo = Join-Path $scratchRoot 'low-finding'
Initialize-RuleHarnessScopeRepo -RepoPath $lowRepo -Features @(
    [pscustomobject]@{ Name = 'Solo'; ApplicationContent = 'namespace Features.Solo.Application { public sealed class SoloService { } }'; SkipSetup = $true }
)
$lowReviewPath = Join-Path $lowRepo 'Temp/low-review.json'
[pscustomobject]@{
    findings = @(
        [pscustomobject]@{
            findingType = 'doc_drift'
            severity = 'low'
            ownerDoc = 'docs/rules/architecture-rules.md'
            title = 'Low severity note'
            message = 'Low severity note'
            confidence = 'low'
            source = 'agent_review'
            remediationKind = 'report_only'
            rationale = ''
            evidence = @([pscustomobject]@{ path = 'Assets/Scripts/Features/Solo/Application/SoloService.cs'; line = 1; snippet = 'SoloService' })
            proposedDocEdit = $null
        }
    )
} | ConvertTo-Json -Depth 20 | Set-Content -Path $lowReviewPath -Encoding UTF8
$lowReport = Invoke-RuleHarness `
    -RepoRoot $lowRepo `
    -ConfigPath (Join-Path $lowRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun `
    -ReviewJsonPath $lowReviewPath
$lowBacklog = Read-RuleHarnessDocProposalBacklog -RepoRoot $lowRepo -Config $config
Assert-RuleHarness `
    -Condition (@($lowReport.findings | Where-Object severity -eq 'low').Count -eq 1 -and $null -eq $lowReport.stoppedScope -and @($lowReport.docProposals).Count -eq 0 -and @($lowBacklog.entries).Count -eq 0) `
    -Message 'Expected low severity findings to be reported without stopping the run or entering the doc proposal backlog.'

$stopRepo = Join-Path $scratchRoot 'feature-stop-and-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $stopRepo -Features @(
    [pscustomobject]@{ Name = 'CleanA'; ApplicationContent = 'namespace Features.CleanA.Application { public sealed class CleanAService { } }' },
    [pscustomobject]@{ Name = 'Broken'; ApplicationContent = @"
using UnityEngine;

namespace Features.Broken.Application
{
    public sealed class BrokenService
    {
    }
}
"@ },
    [pscustomobject]@{ Name = 'CleanB'; ApplicationContent = 'namespace Features.CleanB.Application { public sealed class CleanBService { } }' }
)
Write-FeatureScanStateFixture -RepoPath $stopRepo -Entries @(
    [pscustomobject]@{ scopeId = 'Broken'; lastCheckedAtUtc = '2026-04-05T00:00:00Z'; lastResult = 'clean'; lastFindingSeverity = $null; lastRunId = 'r1'; lastCommitSha = 'a'; lastStoppedReason = $null },
    [pscustomobject]@{ scopeId = 'CleanB'; lastCheckedAtUtc = '2026-04-06T00:00:00Z'; lastResult = 'clean'; lastFindingSeverity = $null; lastRunId = 'r2'; lastCommitSha = 'b'; lastStoppedReason = $null }
)
$stopReport = Invoke-RuleHarness `
    -RepoRoot $stopRepo `
    -ConfigPath (Join-Path $stopRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$stopFeatureState = Read-RuleHarnessFeatureScanState -RepoRoot $stopRepo -Config $config
$stopBacklog = Read-RuleHarnessDocProposalBacklog -RepoRoot $stopRepo -Config $config
$stopProposalPath = Join-Path $stopRepo 'Temp/RuleHarness/rule-harness-doc-proposals.md'
Assert-RuleHarness `
    -Condition ((@($stopReport.scannedScopes) -join ',') -eq 'CleanA,Broken') `
    -Message 'Expected run to stop on the first scope with high/medium findings after earlier clean scopes.'
Assert-RuleHarness `
    -Condition ($stopReport.stoppedScope.scopeId -eq 'Broken' -and @($stopReport.docProposals).Count -ge 1 -and @($stopReport.docEdits).Count -eq 0) `
    -Message 'Expected the stopped scope to emit doc proposals while blocking markdown edits.'
Assert-RuleHarness `
    -Condition ($stopReport.commit.created -and -not (Get-Content -Path (Join-Path $stopRepo 'Assets/Scripts/Features/Broken/Application/BrokenService.cs') -Raw).Contains('using UnityEngine;')) `
    -Message 'Expected an existing code file in the stopped feature to be fixed and committed.'
Assert-RuleHarness `
    -Condition ($stopFeatureState.entries.ContainsKey('CleanA') -and $stopFeatureState.entries.ContainsKey('Broken') -and [string]$stopFeatureState.entries['CleanB'].lastRunId -eq 'r2') `
    -Message 'Expected only attempted scopes to receive updated lastCheckedAt timestamps.'
Assert-RuleHarness `
    -Condition (@($stopBacklog.entries).Count -ge 1 -and (Test-Path -LiteralPath $stopProposalPath)) `
    -Message 'Expected high/medium findings to populate both the proposal backlog and the per-run proposal markdown file.'
Assert-RuleHarness `
    -Condition ($stopReport.stoppedScope.finalStatus -eq 'clean' -and [int]$stopReport.stoppedScope.remainingFindingCount -eq 0 -and [bool]$stopReport.stoppedScope.resolvedInRun) `
    -Message 'Expected same-run auto-fix success to record a clean stopped scope with zero remaining findings.'
Assert-RuleHarness `
    -Condition ([string]$stopFeatureState.entries['Broken'].lastResult -eq 'clean' -and $null -eq $stopFeatureState.entries['Broken'].lastFindingSeverity -and $null -eq $stopFeatureState.entries['Broken'].lastStoppedReason) `
    -Message 'Expected same-run auto-fix success to persist a clean feature state entry.'
Assert-RuleHarness `
    -Condition (@($stopBacklog.entries | Where-Object status -eq 'resolved').Count -ge 1) `
    -Message 'Expected same-run auto-fix success to resolve the matching doc proposal backlog entry.'
Assert-RuleHarness `
    -Condition (@($stopReport.stageResults | Where-Object stage -eq 'state_cleanup' | Select-Object -First 1).Count -eq 1) `
    -Message 'Expected reports to include the state_cleanup stage.'

$staleRepo = Join-Path $scratchRoot 'stale-cleanup'
Initialize-RuleHarnessScopeRepo -RepoPath $staleRepo -Features @(
    [pscustomobject]@{ Name = 'Stale'; ApplicationContent = @"
using UnityEngine;

namespace Features.Stale.Application
{
    public sealed class StaleService
    {
    }
}
"@ }
)
$staleReport1 = Invoke-RuleHarness `
    -RepoRoot $staleRepo `
    -ConfigPath (Join-Path $staleRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Set-Content -Path (Join-Path $staleRepo 'Assets/Scripts/Features/Stale/Application/StaleService.cs') -Value @"
namespace Features.Stale.Application
{
    public sealed class StaleService
    {
    }
}
"@ -Encoding UTF8
$staleReport2 = Invoke-RuleHarness `
    -RepoRoot $staleRepo `
    -ConfigPath (Join-Path $staleRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
$staleFeatureState = Read-RuleHarnessFeatureScanState -RepoRoot $staleRepo -Config $config
$staleBacklog = Read-RuleHarnessDocProposalBacklog -RepoRoot $staleRepo -Config $config
$staleCleanupStage = @($staleReport2.stageResults | Where-Object stage -eq 'state_cleanup' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition ($null -ne $staleReport1.stoppedScope -and [string]$staleFeatureState.entries['Stale'].lastResult -eq 'clean' -and $null -eq $staleFeatureState.entries['Stale'].lastFindingSeverity -and $null -eq $staleFeatureState.entries['Stale'].lastStoppedReason) `
    -Message 'Expected a clean rescan after a manual fix to repair stale feature state back to clean.'
Assert-RuleHarness `
    -Condition (@($staleBacklog.entries).Count -eq 1 -and [string]$staleBacklog.entries[0].status -eq 'resolved') `
    -Message 'Expected a clean rescan after a manual fix to resolve the matching doc proposal ledger entry.'
Assert-RuleHarness `
    -Condition ($null -ne $staleCleanupStage -and [int]$staleCleanupStage.details.resolvedProposalCount -ge 1 -and [int]$staleCleanupStage.details.staleStateRepairedCount -ge 1) `
    -Message 'Expected state_cleanup to report both resolved proposals and repaired stale state after a clean rescan.'

$repeatRepo = Join-Path $scratchRoot 'proposal-repeat'
Initialize-RuleHarnessScopeRepo -RepoPath $repeatRepo -Features @(
    [pscustomobject]@{ Name = 'Loop'; ApplicationContent = @"
using UnityEngine;

namespace Features.Loop.Application
{
    public sealed class LoopService
    {
        public void Run()
        {
            Debug.Log(""loop"");
        }
    }
}
"@ }
)
$repeatReviewPath1 = Join-Path $repeatRepo 'Temp/review-1.json'
[pscustomobject]@{
    findings = @(
        [pscustomobject]@{
            findingType = 'code_violation'
            severity = 'high'
            ownerDoc = 'docs/rules/architecture-rules.md'
            title = 'Unity API used in Application'
            message = 'Application layer file ''Assets/Scripts/Features/Loop/Application/LoopService.cs'' appears to reference Unity API types.'
            confidence = 'high'
            source = 'agent_review'
            remediationKind = 'code_fix'
            rationale = ''
            evidence = @([pscustomobject]@{ path = 'Assets/Scripts/Features/Loop/Application/LoopService.cs'; line = 1; snippet = 'using UnityEngine;' })
            proposedDocEdit = $null
        }
    )
} | ConvertTo-Json -Depth 20 | Set-Content -Path $repeatReviewPath1 -Encoding UTF8
$repeatReviewPath2 = Join-Path $repeatRepo 'Temp/review-2.json'
[pscustomobject]@{
    findings = @(
        [pscustomobject]@{
            findingType = 'code_violation'
            severity = 'high'
            ownerDoc = 'docs/rules/architecture-rules.md'
            title = 'Unity API used in Application layer'
            message = 'Application layer file ''Assets/Scripts/Features/Loop/Application/LoopService.cs'' appears to reference Unity API types.'
            confidence = 'high'
            source = 'agent_review'
            remediationKind = 'code_fix'
            rationale = ''
            evidence = @([pscustomobject]@{ path = 'Assets/Scripts/Features/Loop/Application/LoopService.cs'; line = 1; snippet = 'using UnityEngine;' })
            proposedDocEdit = $null
        }
    )
} | ConvertTo-Json -Depth 20 | Set-Content -Path $repeatReviewPath2 -Encoding UTF8
$repeatReport1 = Invoke-RuleHarness `
    -RepoRoot $repeatRepo `
    -ConfigPath (Join-Path $repeatRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun `
    -ReviewJsonPath $repeatReviewPath1
$repeatReport2 = Invoke-RuleHarness `
    -RepoRoot $repeatRepo `
    -ConfigPath (Join-Path $repeatRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun `
    -ReviewJsonPath $repeatReviewPath2
$repeatBacklog = Read-RuleHarnessDocProposalBacklog -RepoRoot $repeatRepo -Config $config
Assert-RuleHarness `
    -Condition (@($repeatReport1.docProposals).Count -ge 1 -and @($repeatReport2.docProposals).Count -ge 1 -and @($repeatBacklog.entries).Count -eq 1 -and [int]$repeatBacklog.entries[0].hitCount -eq 2 -and [string]$repeatBacklog.entries[0].findingFamily -eq 'code_violation/application_unity_api') `
    -Message 'Expected retitled high/medium findings to dedupe by canonical signature into one backlog entry.'

$reopenRepo = Join-Path $scratchRoot 'proposal-reopen'
Initialize-RuleHarnessScopeRepo -RepoPath $reopenRepo -Features @(
    [pscustomobject]@{ Name = 'Loop'; ApplicationContent = @"
using UnityEngine;

namespace Features.Loop.Application
{
    public sealed class LoopService
    {
    }
}
"@ }
)
$reopenReviewPath = Join-Path $reopenRepo 'Temp/review.json'
[pscustomobject]@{
    findings = @(
        [pscustomobject]@{
            findingType = 'code_violation'
            severity = 'high'
            ownerDoc = 'docs/rules/architecture-rules.md'
            title = 'Unity API used in Application layer'
            message = 'Application layer file ''Assets/Scripts/Features/Loop/Application/LoopService.cs'' appears to reference Unity API types.'
            confidence = 'high'
            source = 'agent_review'
            remediationKind = 'code_fix'
            rationale = ''
            evidence = @([pscustomobject]@{ path = 'Assets/Scripts/Features/Loop/Application/LoopService.cs'; line = 1; snippet = 'using UnityEngine;' })
            proposedDocEdit = $null
        }
    )
} | ConvertTo-Json -Depth 20 | Set-Content -Path $reopenReviewPath -Encoding UTF8
$reopenReport1 = Invoke-RuleHarness `
    -RepoRoot $reopenRepo `
    -ConfigPath (Join-Path $reopenRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun `
    -ReviewJsonPath $reopenReviewPath
Set-Content -Path (Join-Path $reopenRepo 'Assets/Scripts/Features/Loop/Application/LoopService.cs') -Value @"
namespace Features.Loop.Application
{
    public sealed class LoopService
    {
    }
}
"@ -Encoding UTF8
$reopenReport2 = Invoke-RuleHarness `
    -RepoRoot $reopenRepo `
    -ConfigPath (Join-Path $reopenRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Set-Content -Path (Join-Path $reopenRepo 'Assets/Scripts/Features/Loop/Application/LoopService.cs') -Value @"
using UnityEngine;

namespace Features.Loop.Application
{
    public sealed class LoopService
    {
    }
}
"@ -Encoding UTF8
$reopenReport3 = Invoke-RuleHarness `
    -RepoRoot $reopenRepo `
    -ConfigPath (Join-Path $reopenRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun `
    -ReviewJsonPath $reopenReviewPath
$reopenBacklog = Read-RuleHarnessDocProposalBacklog -RepoRoot $reopenRepo -Config $config
$reopenCleanupStage = @($reopenReport3.stageResults | Where-Object stage -eq 'state_cleanup' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition ($null -ne $reopenReport1.stoppedScope -and $null -eq $reopenReport2.stoppedScope -and $null -ne $reopenReport3.stoppedScope) `
    -Message 'Expected reopen test to create, resolve, and then re-detect the same proposal signature across three runs.'
Assert-RuleHarness `
    -Condition (@($reopenBacklog.entries).Count -eq 1 -and [string]$reopenBacklog.entries[0].status -eq 'active' -and [int]$reopenBacklog.entries[0].hitCount -eq 2) `
    -Message 'Expected a resolved proposal entry to reactivate in place instead of creating a duplicate entry.'
Assert-RuleHarness `
    -Condition ($null -ne $reopenCleanupStage -and [int]$reopenCleanupStage.details.reactivatedProposalCount -ge 1) `
    -Message 'Expected state_cleanup to record when a resolved proposal signature is reactivated.'

$relatedRepo = Join-Path $scratchRoot 'related-feature-edit'
Initialize-RuleHarnessScopeRepo -RepoPath $relatedRepo -Features @(
    [pscustomobject]@{ Name = 'Caller'; ApplicationContent = 'namespace Features.Caller.Application { public sealed class CallerService { } }' },
    [pscustomobject]@{ Name = 'Helper'; ApplicationContent = @"
using UnityEngine;

namespace Features.Helper.Application
{
    public sealed class HelperService
    {
    }
}
"@ }
)
$relatedFinding = ConvertTo-RuleHarnessReviewedFindings -Findings @(
    [pscustomobject]@{
        findingType = 'code_violation'
        severity = 'high'
        ownerDoc = 'docs/rules/architecture-rules.md'
        title = 'Unity API used in Application'
        message = 'Caller flow requires updating a related feature implementation.'
        confidence = 'high'
        source = 'agent_review'
        remediationKind = 'code_fix'
        rationale = ''
        evidence = @(
            [pscustomobject]@{ path = 'Assets/Scripts/Features/Caller/Application/CallerService.cs'; line = 1; snippet = 'CallerService' },
            [pscustomobject]@{ path = 'Assets/Scripts/Features/Helper/Application/HelperService.cs'; line = 1; snippet = 'using UnityEngine;' }
        )
        proposedDocEdit = $null
    }
)
$relatedBatches = Get-RuleHarnessPlannedBatches -ReviewedFindings $relatedFinding -DocEdits @() -RepoRoot $relatedRepo
Assert-RuleHarness `
    -Condition (@($relatedBatches | Where-Object { $_.kind -eq 'code_fix' -and $_.targetFiles -contains 'Assets/Scripts/Features/Helper/Application/HelperService.cs' }).Count -eq 1) `
    -Message 'Expected reviewed findings to permit existing-file code fixes in related features.'

$retitledBatches = Get-RuleHarnessPlannedBatches -ReviewedFindings @(
    [pscustomobject]@{
        findingType = 'code_violation'
        severity = 'high'
        ownerDoc = 'docs/rules/architecture-rules.md'
        title = 'Unity API used in Application layer'
        message = 'Application layer file ''Assets/Scripts/Features/Helper/Application/HelperService.cs'' imports UnityEngine, which violates the Application layer purity rule.'
        confidence = 'high'
        source = 'agent_review'
        remediationKind = 'code_fix'
        rationale = ''
        evidence = @(
            [pscustomobject]@{ path = 'Assets/Scripts/Features/Helper/Application/HelperService.cs'; line = 1; snippet = 'using UnityEngine;' }
        )
        proposedDocEdit = $null
    }
) -DocEdits @() -RepoRoot $relatedRepo
Assert-RuleHarness `
    -Condition (@($retitledBatches | Where-Object { $_.kind -eq 'code_fix' -and $_.targetFiles -contains 'Assets/Scripts/Features/Helper/Application/HelperService.cs' }).Count -eq 1) `
    -Message 'Expected LLM-retitled Unity Application findings to still produce code-fix batches when the file contains removable using directives.'

$commentRepo = Join-Path $scratchRoot 'comment-only-application'
Initialize-RuleHarnessScopeRepo -RepoPath $commentRepo -Features @(
    [pscustomobject]@{ Name = 'CommentOnly'; ApplicationContent = @"
namespace Features.CommentOnly.Application
{
    /// <summary>
    /// Bootstrap emits Debug.Log when this event is observed.
    /// </summary>
    public readonly struct CommentOnlyEvent
    {
    }
}
"@ }
)
$commentConfig = New-RuleHarnessTestConfig -SourceConfig $config
$commentFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $commentRepo `
    -Config $commentConfig `
    -ScopeId 'CommentOnly'
Assert-RuleHarness `
    -Condition (@($commentFindings | Where-Object { $_.title -eq 'Unity API used in Application' }).Count -eq 0) `
    -Message 'Expected comment-only Debug.Log references in Application files to avoid Unity API findings.'

$qualifiedRepo = Join-Path $scratchRoot 'qualified-application-reference'
Initialize-RuleHarnessScopeRepo -RepoPath $qualifiedRepo -Features @(
    [pscustomobject]@{ Name = 'Qualified'; ApplicationContent = @"
namespace Features.Qualified.Application
{
    public sealed class QualifiedService
    {
        public float ReadTime()
        {
            return UnityEngine.Time.time;
        }
    }
}
"@ }
)
$qualifiedConfig = New-RuleHarnessTestConfig -SourceConfig $config
$qualifiedFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $qualifiedRepo `
    -Config $qualifiedConfig `
    -ScopeId 'Qualified'
Assert-RuleHarness `
    -Condition (@($qualifiedFindings | Where-Object { $_.title -eq 'Unity API used in Application' }).Count -eq 1) `
    -Message 'Expected fully-qualified UnityEngine references in Application files to be reported as Unity API findings.'

$scheduledRepo = Join-Path $scratchRoot 'scheduled-status'
Initialize-RuleHarnessScopeRepo -RepoPath $scheduledRepo -Features @(
    [pscustomobject]@{ Name = 'CleanA'; ApplicationContent = 'namespace Features.CleanA.Application { public sealed class CleanAService { } }' },
    [pscustomobject]@{ Name = 'Broken'; ApplicationContent = 'namespace Features.Broken.Application { public sealed class BrokenService { public void Run() { Debug.Log(""broken""); } } }' }
)
Push-Location $scheduledRepo
try {
    & (Join-Path $scheduledRepo 'tools/rule-harness/run-rule-harness-scheduled.ps1') -DisableLlm -MutationMode 'code_and_rules'
}
finally {
    Pop-Location
}
$latestStatusPath = Join-Path $scheduledRepo 'Temp/RuleHarnessScheduled/latest-status.json'
$latestStatus = Get-Content -Path $latestStatusPath -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (Test-Path -LiteralPath $latestStatusPath) `
    -Message 'Expected scheduled wrapper to write latest-status.json.'
Assert-RuleHarness `
    -Condition (-not [string]::IsNullOrWhiteSpace([string]$latestStatus.currentScope) -and $latestStatus.PSObject.Properties.Name -contains 'completedScopes' -and $latestStatus.PSObject.Properties.Name -contains 'nextScope' -and $latestStatus.PSObject.Properties.Name -contains 'topDocProposals') `
    -Message 'Expected scheduled latest status to expose currentScope, completedScopes, nextScope, and topDocProposals.'
Assert-RuleHarness `
    -Condition ((Test-Path -LiteralPath ([string]$latestStatus.docProposalPath)) -and (@($latestStatus.topDocProposals).Count -ge 1)) `
    -Message 'Expected scheduled runs to emit the proposal markdown file and surface top doc proposals.'

Write-Host 'Rule harness fixture tests passed.'
