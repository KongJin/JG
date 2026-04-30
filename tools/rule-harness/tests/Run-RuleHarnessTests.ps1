Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Import-Module (Join-Path $repoRoot 'tools/rule-harness/RuleHarness.psm1') -Force
$config = Get-RuleHarnessConfig -ConfigPath (Join-Path $repoRoot 'tools/rule-harness/config.json')
$scratchRoot = Join-Path $repoRoot 'Temp/RuleHarnessFixtureTests'

if (Test-Path -LiteralPath $scratchRoot) {
    Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
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

function Invoke-RuleHarnessTestGit {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& git -C $RepoPath @Arguments 2>$null)
        if ($LASTEXITCODE -ne 0) {
            $commandText = @($Arguments) -join ' '
            $message = "Fixture git command failed: git -C $RepoPath $commandText"
            if (@($output).Count -gt 0) {
                $message = "$message`n$($output -join [Environment]::NewLine)"
            }

            throw $message
        }
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    @($output)
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
    Set-Content -Path (Join-Path $RepoPath 'AGENTS.md') -Value 'Read `/docs/rules/architecture-rules.md`.' -Encoding UTF8
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
        Invoke-RuleHarnessTestGit -RepoPath $RepoPath -Arguments @('init') | Out-Null
        Invoke-RuleHarnessTestGit -RepoPath $RepoPath -Arguments @('config', 'user.name', 'rule-harness-tests') | Out-Null
        Invoke-RuleHarnessTestGit -RepoPath $RepoPath -Arguments @('config', 'user.email', 'rule-harness-tests@example.com') | Out-Null
        Invoke-RuleHarnessTestGit -RepoPath $RepoPath -Arguments @('add', '.') | Out-Null
        Invoke-RuleHarnessTestGit -RepoPath $RepoPath -Arguments @('commit', '-m', 'init') | Out-Null
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

function Write-CompileStatusFixture {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [string]$Status,
        [string]$Summary = '',
        [string]$ReasonCode = '',
        [bool]$RuntimeSmokeClean = $false
    )

    $statePath = Join-Path $RepoPath 'Temp/RuleHarnessState/compile-status.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $statePath) -Force | Out-Null
    [pscustomobject]@{
        status            = $Status
        summary           = $Summary
        source            = 'fixture'
        reasonCode        = $ReasonCode
        checkedAtUtc      = '2026-04-09T12:34:56Z'
        runtimeSmokeClean = $RuntimeSmokeClean
    } | ConvertTo-Json -Depth 10 | Set-Content -Path $statePath -Encoding UTF8
}

function Write-FeatureDependencyReportFixture {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [bool]$HasCycles,
        [object[]]$Cycles = @(),
        [int]$FeatureCount = 1,
        [int]$EdgeCount = 0
    )

    $statePath = Join-Path $RepoPath 'Temp/LayerDependencyValidator/feature-dependencies.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $statePath) -Force | Out-Null
    [pscustomobject]@{
        generatedAtUtc = '2026-04-10T12:34:56Z'
        featureCount   = $FeatureCount
        edgeCount      = $EdgeCount
        hasCycles      = $HasCycles
        edges          = @()
        cycles         = @($Cycles)
    } | ConvertTo-Json -Depth 10 | Set-Content -Path $statePath -Encoding UTF8
}

function Copy-FeatureDependencyValidatorFixture {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath
    )

    $editorRoot = Join-Path $RepoPath 'Assets/Editor'
    New-Item -ItemType Directory -Path $editorRoot -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Assets/Editor/LayerDependencyValidator.cs') -Destination (Join-Path $editorRoot 'LayerDependencyValidator.cs') -Force
}

function Write-CompileRefreshStub {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [string]$Status = 'passed'
    )

    $scriptPath = Join-Path $RepoPath 'tools/rule-harness/write-compile-status.ps1'
    Set-Content -Path $scriptPath -Value @"
param([string]`$RepoRoot = (Resolve-Path (Join-Path `$PSScriptRoot '..\\..')).Path)
`$outputPath = Join-Path `$RepoRoot 'Temp/RuleHarnessState/compile-status.json'
New-Item -ItemType Directory -Path (Split-Path -Parent `$outputPath) -Force | Out-Null
[pscustomobject]@{
    status = '$Status'
    summary = 'fixture compile status'
    source = 'fixture-refresh'
    reasonCode = ''
    checkedAtUtc = '2026-04-10T12:34:56Z'
    runtimeSmokeClean = `$false
} | ConvertTo-Json -Depth 10 | Set-Content -Path `$outputPath -Encoding UTF8
[pscustomobject]@{
    status = '$Status'
    outputPath = `$outputPath
}
"@ -Encoding UTF8
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

$shadowRepo = Join-Path $scratchRoot 'short-type-shadowing'
Initialize-RuleHarnessScopeRepo -RepoPath $shadowRepo -Features @(
    [pscustomobject]@{ Name = 'Unit'; ApplicationContent = @"
namespace Features.Unit.Application
{
    public sealed class UnitService
    {
        private Unit[] _units;
    }
}
"@ }
)
New-Item -ItemType Directory -Path (Join-Path $shadowRepo 'Assets/Scripts/Features/Unit/Domain') -Force | Out-Null
Set-Content -Path (Join-Path $shadowRepo 'Assets/Scripts/Features/Unit/Domain/Unit.cs') -Value @"
namespace Features.Unit.Domain
{
    public sealed class Unit
    {
    }
}
"@ -Encoding UTF8
$shadowFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $shadowRepo `
    -Config $config `
    -ScopeId 'Unit'
Assert-RuleHarness `
    -Condition (@($shadowFindings | Where-Object { $_.title -eq 'Feature short-type shadowing' }).Count -eq 1) `
    -Message 'Expected same-name feature type usage without alias to be reported as short-type shadowing.'

$phantomRepo = Join-Path $scratchRoot 'phantom-contract'
Initialize-RuleHarnessScopeRepo -RepoPath $phantomRepo -Features @(
    [pscustomobject]@{ Name = 'Phantom'; ApplicationContent = @"
using Shared.EventBus;

namespace Features.Phantom.Application
{
    public sealed class PhantomService
    {
        private IEventBus _bus;
    }
}
"@ }
)
$phantomFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $phantomRepo `
    -Config $config `
    -ScopeId 'Phantom'
Assert-RuleHarness `
    -Condition (@($phantomFindings | Where-Object { $_.title -eq 'Phantom shared contract name' }).Count -eq 1) `
    -Message 'Expected IEventBus usage to be reported as a phantom shared contract.'

$phantomFixRepo = Join-Path $scratchRoot 'phantom-contract-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $phantomFixRepo -Features @(
    [pscustomobject]@{ Name = 'Phantom'; ApplicationContent = @"
using Shared.EventBus;

namespace Features.Phantom.Application
{
    public sealed class PhantomService
    {
        private readonly IEventBus _publisher;

        public PhantomService(IEventBus publisher)
        {
            _publisher = publisher;
        }

        public void Run()
        {
            _publisher.Publish(42);
        }
    }
}
"@ }
)
$phantomFixReport = Invoke-RuleHarness `
    -RepoRoot $phantomFixRepo `
    -ConfigPath (Join-Path $phantomFixRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$phantomFixContent = Get-Content -Path (Join-Path $phantomFixRepo 'Assets/Scripts/Features/Phantom/Application/PhantomService.cs') -Raw
Assert-RuleHarness `
    -Condition ($phantomFixReport.commit.created -and $phantomFixReport.stoppedScope.scopeId -eq 'Phantom' -and $phantomFixReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected publish-only phantom event bus usage to be auto-fixed and leave the feature clean.'
Assert-RuleHarness `
    -Condition ($phantomFixContent.Contains('IEventPublisher _publisher;') -and $phantomFixContent.Contains('PhantomService(IEventPublisher publisher)')) `
    -Message 'Expected phantom event bus auto-fix to replace IEventBus with the concrete publish-side contract.'

$importRepo = Join-Path $scratchRoot 'missing-imports'
Initialize-RuleHarnessScopeRepo -RepoPath $importRepo -Features @(
    [pscustomobject]@{ Name = 'Imports'; ApplicationContent = @"
namespace Features.Imports.Application
{
    public sealed class ImportsService
    {
        private Func<float> _clock;
        private GarageRoster _roster;
        private StatusNetworkAdapter _status;
        private SceneLoaderAdapter _loader;
    }
}
"@ }
)
$importFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $importRepo `
    -Config $config `
    -ScopeId 'Imports'
Assert-RuleHarness `
    -Condition (@($importFindings | Where-Object { $_.title -eq 'Missing import after symbol move' }).Count -ge 4) `
    -Message 'Expected known moved symbols without imports to be reported as missing import drift.'

$shadowFixRepo = Join-Path $scratchRoot 'short-type-shadowing-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $shadowFixRepo -Features @(
    [pscustomobject]@{ Name = 'Unit'; ApplicationContent = @"
namespace Features.Unit.Application
{
    public sealed class UnitService
    {
        private Unit[] _units;

        public Unit Copy(Unit source)
        {
            return source;
        }
    }
}
"@ }
)
New-Item -ItemType Directory -Path (Join-Path $shadowFixRepo 'Assets/Scripts/Features/Unit/Domain') -Force | Out-Null
Set-Content -Path (Join-Path $shadowFixRepo 'Assets/Scripts/Features/Unit/Domain/Unit.cs') -Value @"
namespace Features.Unit.Domain
{
    public sealed class Unit
    {
    }
}
"@ -Encoding UTF8
$shadowFixReport = Invoke-RuleHarness `
    -RepoRoot $shadowFixRepo `
    -ConfigPath (Join-Path $shadowFixRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$shadowFixContent = Get-Content -Path (Join-Path $shadowFixRepo 'Assets/Scripts/Features/Unit/Application/UnitService.cs') -Raw
Assert-RuleHarness `
    -Condition ($shadowFixReport.commit.created -and $shadowFixReport.stoppedScope.scopeId -eq 'Unit' -and $shadowFixReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected short-type shadowing findings to be auto-fixed and leave the feature clean.'
Assert-RuleHarness `
    -Condition ($shadowFixContent.Contains('private global::Features.Unit.Domain.Unit[] _units;') -and $shadowFixContent.Contains('public global::Features.Unit.Domain.Unit Copy(global::Features.Unit.Domain.Unit source)')) `
    -Message 'Expected short-type shadowing auto-fix to rewrite bare feature-type references to fully-qualified names.'

$importFixRepo = Join-Path $scratchRoot 'missing-import-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $importFixRepo -Features @(
    [pscustomobject]@{ Name = 'Imports'; ApplicationContent = @"
namespace Features.Imports.Application
{
    public sealed class ImportsService
    {
        private Func<float> _clock;
    }
}
"@ }
)
$importFixReport = Invoke-RuleHarness `
    -RepoRoot $importFixRepo `
    -ConfigPath (Join-Path $importFixRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$importFixContent = Get-Content -Path (Join-Path $importFixRepo 'Assets/Scripts/Features/Imports/Application/ImportsService.cs') -Raw
Assert-RuleHarness `
    -Condition ($importFixReport.commit.created -and $importFixReport.stoppedScope.scopeId -eq 'Imports' -and $importFixReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected missing-import findings to be auto-fixed and leave the feature clean.'
Assert-RuleHarness `
    -Condition ($importFixContent.Contains('using System;') -and $importFixContent.Contains('private Func<float> _clock;')) `
    -Message 'Expected missing-import auto-fix to add the required using directive without rewriting the symbol usage.'

$eventDriftRepo = Join-Path $scratchRoot 'event-contract-drift'
Initialize-RuleHarnessScopeRepo -RepoPath $eventDriftRepo -Features @(
    [pscustomobject]@{ Name = 'Player'; ApplicationContent = @"
namespace Features.Player.Application
{
    public sealed class DriftService
    {
        public void Handle(GameEndEvent e)
        {
            if (e.IsLocalPlayerDead)
            {
            }
        }
    }
}
"@ }
)
$eventDriftFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $eventDriftRepo `
    -Config $config `
    -ScopeId 'Player'
Assert-RuleHarness `
    -Condition (@($eventDriftFindings | Where-Object { $_.title -eq 'Event contract drift' }).Count -eq 1) `
    -Message 'Expected stale GameEndEvent member access to be reported as event contract drift.'

$eventRenameRepo = Join-Path $scratchRoot 'event-contract-rename-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $eventRenameRepo -Features @(
    [pscustomobject]@{ Name = 'Wave'; ApplicationContent = @"
using Features.Combat.Application.Events;

namespace Features.Wave.Application
{
    public sealed class DriftService
    {
        public float Read(DamageAppliedEvent e)
        {
            return e.RemainingHp;
        }
    }
}
"@ }
)
$eventRenameReport = Invoke-RuleHarness `
    -RepoRoot $eventRenameRepo `
    -ConfigPath (Join-Path $eventRenameRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$eventRenameContent = Get-Content -Path (Join-Path $eventRenameRepo 'Assets/Scripts/Features/Wave/Application/WaveService.cs') -Raw
Assert-RuleHarness `
    -Condition ($eventRenameReport.commit.created -and $eventRenameReport.stoppedScope.scopeId -eq 'Wave' -and $eventRenameReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected rename-only event contract drift to be auto-fixed and leave the feature clean.'
Assert-RuleHarness `
    -Condition ($eventRenameContent.Contains('e.RemainingHealth') -and -not $eventRenameContent.Contains('RemainingHp')) `
    -Message 'Expected event contract drift auto-fix to rewrite stale member names when the replacement is mechanical.'

$layerRepo = Join-Path $scratchRoot 'layer-violations'
Initialize-RuleHarnessScopeRepo -RepoPath $layerRepo -Features @(
    [pscustomobject]@{ Name = 'Wave'; ApplicationContent = 'namespace Features.Wave.Application { public sealed class Placeholder { } }' }
)
New-Item -ItemType Directory -Path (Join-Path $layerRepo 'Assets/Scripts/Features/Wave/Infrastructure') -Force | Out-Null
Set-Content -Path (Join-Path $layerRepo 'Assets/Scripts/Features/Wave/Infrastructure/WaveInfra.cs') -Value @"
namespace Features.Wave.Infrastructure
{
    public sealed class WaveInfra
    {
    }
}
"@ -Encoding UTF8
Set-Content -Path (Join-Path $layerRepo 'Assets/Scripts/Features/Wave/Application/WaveBadApp.cs') -Value @"
using Features.Wave.Infrastructure;

namespace Features.Wave.Application
{
    public sealed class WaveBadApp
    {
    }
}
"@ -Encoding UTF8
$layerFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $layerRepo `
    -Config $config `
    -ScopeId 'Wave'
Assert-RuleHarness `
    -Condition (@($layerFindings | Where-Object { $_.title -eq 'Layer dependency violation' }).Count -ge 1) `
    -Message 'Expected Application->Infrastructure imports to be reported as layer dependency violations.'

$bridgeRepo = Join-Path $scratchRoot 'energy-bridge-drift'
Initialize-RuleHarnessScopeRepo -RepoPath $bridgeRepo -Features @(
    [pscustomobject]@{ Name = 'Player'; ApplicationContent = @"
namespace Features.Player.Application
{
    public sealed class BridgeService
    {
        public object Build(IUnitEnergyPort port)
        {
            return new EnergyAdapter();
        }
    }
}
"@ }
)
$bridgeFindings = Get-RuleHarnessStaticFindings `
    -RepoRoot $bridgeRepo `
    -Config $config `
    -ScopeId 'Player'
Assert-RuleHarness `
    -Condition (@($bridgeFindings | Where-Object { $_.title -eq 'Concrete/interface drift' }).Count -eq 1) `
    -Message 'Expected direct EnergyAdapter construction next to IUnitEnergyPort usage to be reported as concrete/interface drift.'

$timeRepo = Join-Path $scratchRoot 'qualified-time-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $timeRepo -Features @(
    [pscustomobject]@{ Name = 'Timer'; ApplicationContent = @"
namespace Features.Timer.Application
{
    public sealed class TimerService
    {
        private readonly float _startedAt;

        public TimerService()
        {
            _startedAt = UnityEngine.Time.time;
        }
    }
}
"@ }
)
Set-Content -Path (Join-Path $timeRepo 'Assets/Scripts/Features/Timer/TimerSetup.cs') -Value @"
using Features.Timer.Application;

namespace Features.Timer
{
    public sealed class TimerSetup
    {
        public TimerService Build()
        {
            return new TimerService();
        }
    }
}
"@ -Encoding UTF8
Push-Location $timeRepo
try {
    Invoke-RuleHarnessTestGit -RepoPath $timeRepo -Arguments @('add', '.') | Out-Null
    Invoke-RuleHarnessTestGit -RepoPath $timeRepo -Arguments @('commit', '-m', 'custom timer setup') | Out-Null
}
finally {
    Pop-Location
}
$timeReport = Invoke-RuleHarness `
    -RepoRoot $timeRepo `
    -ConfigPath (Join-Path $timeRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$timeAppContent = Get-Content -Path (Join-Path $timeRepo 'Assets/Scripts/Features/Timer/Application/TimerService.cs') -Raw
$timeSetupContent = Get-Content -Path (Join-Path $timeRepo 'Assets/Scripts/Features/Timer/TimerSetup.cs') -Raw
$timeState = Read-RuleHarnessFeatureScanState -RepoRoot $timeRepo -Config $config
Assert-RuleHarness `
    -Condition ($timeReport.commit.created -and $timeReport.stoppedScope.scopeId -eq 'Timer' -and $timeReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected the qualified Unity time recipe to apply, commit, and leave the stopped scope clean.'
Assert-RuleHarness `
    -Condition ($timeAppContent.Contains('global::System.Func<float> _timeProvider;') -and $timeAppContent.Contains('_timeProvider = timeProvider') -and $timeAppContent.Contains('_timeProvider();') -and -not $timeAppContent.Contains('UnityEngine.Time.time')) `
    -Message 'Expected the Application file to be rewritten to use an injected time provider instead of UnityEngine.Time.time.'
Assert-RuleHarness `
    -Condition ($timeSetupContent.Contains('new TimerService(() => UnityEngine.Time.time)')) `
    -Message 'Expected the feature setup call site to inject a Unity time provider lambda.'
Assert-RuleHarness `
    -Condition ([string]$timeState.entries['Timer'].lastResult -eq 'clean' -and $null -eq $timeState.entries['Timer'].lastFindingSeverity) `
    -Message 'Expected the time-provider recipe to persist a clean state entry after the fix.'

$unscaledRepo = Join-Path $scratchRoot 'qualified-unscaled-time-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $unscaledRepo -Features @(
    [pscustomobject]@{ Name = 'Clock'; ApplicationContent = @"
namespace Features.Clock.Application
{
    public sealed class ClockService
    {
        private readonly float _startedAt;

        public ClockService()
        {
            _startedAt = UnityEngine.Time.unscaledTime;
        }
    }
}
"@ }
)
Set-Content -Path (Join-Path $unscaledRepo 'Assets/Scripts/Features/Clock/ClockSetup.cs') -Value @"
using Features.Clock.Application;

namespace Features.Clock
{
    public sealed class ClockSetup
    {
        public ClockService Build()
        {
            return new ClockService();
        }
    }
}
"@ -Encoding UTF8
Push-Location $unscaledRepo
try {
    Invoke-RuleHarnessTestGit -RepoPath $unscaledRepo -Arguments @('add', '.') | Out-Null
    Invoke-RuleHarnessTestGit -RepoPath $unscaledRepo -Arguments @('commit', '-m', 'custom clock setup') | Out-Null
}
finally {
    Pop-Location
}
$unscaledReport = Invoke-RuleHarness `
    -RepoRoot $unscaledRepo `
    -ConfigPath (Join-Path $unscaledRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$unscaledAppContent = Get-Content -Path (Join-Path $unscaledRepo 'Assets/Scripts/Features/Clock/Application/ClockService.cs') -Raw
$unscaledSetupContent = Get-Content -Path (Join-Path $unscaledRepo 'Assets/Scripts/Features/Clock/ClockSetup.cs') -Raw
Assert-RuleHarness `
    -Condition ($unscaledReport.commit.created -and $unscaledReport.stoppedScope.scopeId -eq 'Clock' -and $unscaledReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected the time-provider recipe to generalize to other float-returning UnityEngine.Time properties.'
Assert-RuleHarness `
    -Condition ($unscaledAppContent.Contains('_timeProvider();') -and -not $unscaledAppContent.Contains('UnityEngine.Time.unscaledTime')) `
    -Message 'Expected the Application file to replace UnityEngine.Time.unscaledTime with the injected time provider.'
Assert-RuleHarness `
    -Condition ($unscaledSetupContent.Contains('new ClockService(() => UnityEngine.Time.unscaledTime)')) `
    -Message 'Expected the setup call site to inject the specific UnityEngine.Time property that was originally referenced.'

$unplannedRepo = Join-Path $scratchRoot 'unplanned-code-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $unplannedRepo -Features @(
    [pscustomobject]@{ Name = 'Logger'; ApplicationContent = @"
namespace Features.Logger.Application
{
    public sealed class LoggerService
    {
        public void Run()
        {
            Debug.Log(""logger"");
        }
    }
}
"@ }
)
$unplannedReport = Invoke-RuleHarness `
    -RepoRoot $unplannedRepo `
    -ConfigPath (Join-Path $unplannedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
$unplannedPatchPlan = @($unplannedReport.stageResults | Where-Object stage -eq 'patch_plan' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition ($unplannedReport.stoppedScope.scopeId -eq 'Logger' -and @($unplannedReport.plannedBatches).Count -eq 0) `
    -Message 'Expected unsupported Unity code-fix patterns to stop the scope without producing planned batches.'
Assert-RuleHarness `
    -Condition (@($unplannedReport.actionItems | Where-Object kind -eq 'expand-auto-fix-coverage').Count -eq 1) `
    -Message 'Expected unplanned code-fix findings to surface an explainability action item.'
Assert-RuleHarness `
    -Condition ($null -ne $unplannedPatchPlan -and [int]$unplannedPatchPlan.details.unplannedFindingCount -eq 1) `
    -Message 'Expected patch_plan stage details to record unplanned findings when no recipe matched.'

$mathfRepo = Join-Path $scratchRoot 'mathf-scalar-fix'
Initialize-RuleHarnessScopeRepo -RepoPath $mathfRepo -Features @(
    [pscustomobject]@{ Name = 'Garage'; ApplicationContent = @"
namespace Features.Garage.Application
{
    public sealed class GarageService
    {
        public int ComputeMissing(int savedUnitCount)
        {
            return UnityEngine.Mathf.Max(0, 3 - savedUnitCount);
        }
    }
}
"@ }
)
$mathfReport = Invoke-RuleHarness `
    -RepoRoot $mathfRepo `
    -ConfigPath (Join-Path $mathfRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$mathfContent = Get-Content -Path (Join-Path $mathfRepo 'Assets/Scripts/Features/Garage/Application/GarageService.cs') -Raw
Assert-RuleHarness `
    -Condition ($mathfReport.commit.created -and $mathfReport.stoppedScope.scopeId -eq 'Garage' -and $mathfReport.stoppedScope.finalStatus -eq 'clean') `
    -Message 'Expected fully-qualified UnityEngine.Mathf scalar calls in Application to be auto-fixed and committed.'
Assert-RuleHarness `
    -Condition ($mathfContent.Contains('System.Math.Max(0, 3 - savedUnitCount)') -and -not $mathfContent.Contains('UnityEngine.Mathf.Max')) `
    -Message 'Expected the Mathf scalar recipe to rewrite UnityEngine.Mathf.Max to System.Math.Max.'
Assert-RuleHarness `
    -Condition (@($mathfReport.actionItems | Where-Object kind -eq 'expand-auto-fix-coverage').Count -eq 0) `
    -Message 'Expected supported Mathf scalar fixes to avoid expand-auto-fix-coverage action items.'

$compileGateRepo = Join-Path $scratchRoot 'compile-gate-default'
Initialize-RuleHarnessScopeRepo -RepoPath $compileGateRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
$compileGateReport = Invoke-RuleHarness `
    -RepoRoot $compileGateRepo `
    -ConfigPath (Join-Path $compileGateRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
$compileGateStage = @($compileGateReport.stageResults | Where-Object stage -eq 'compile_gate' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition ($compileGateReport.execution.cleanLevel -eq 'static-clean only' -and -not [bool]$compileGateReport.execution.compileVerified -and $null -ne $compileGateStage -and [string]$compileGateStage.status -eq 'skipped') `
    -Message 'Expected runs without compile evidence to be classified as static-clean only.'
Assert-RuleHarness `
    -Condition (@($compileGateReport.actionItems | Where-Object kind -eq 'verify-unity-compile').Count -eq 1) `
    -Message 'Expected runs without compile evidence to request explicit Unity compile verification.'

$compilePassedRepo = Join-Path $scratchRoot 'compile-gate-passed'
Initialize-RuleHarnessScopeRepo -RepoPath $compilePassedRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Write-CompileStatusFixture -RepoPath $compilePassedRepo -Status 'passed' -Summary 'Unity compile succeeded.'
$compilePassedReport = Invoke-RuleHarness `
    -RepoRoot $compilePassedRepo `
    -ConfigPath (Join-Path $compilePassedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
$compilePassedStage = @($compilePassedReport.stageResults | Where-Object stage -eq 'compile_gate' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition ($compilePassedReport.execution.cleanLevel -eq 'compile-clean' -and [bool]$compilePassedReport.execution.compileVerified -and $null -ne $compilePassedStage -and [string]$compilePassedStage.status -eq 'passed') `
    -Message 'Expected compile status handoff to upgrade runs from static-clean only to compile-clean.'
Assert-RuleHarness `
    -Condition (@($compilePassedReport.actionItems | Where-Object kind -eq 'verify-unity-compile').Count -eq 0) `
    -Message 'Expected compile-clean runs to omit the manual compile verification reminder.'
Assert-RuleHarness `
    -Condition ([string]$compilePassedReport.execution.compileGateStatus -eq 'passed') `
    -Message 'Expected compile-clean runs to expose a passed compile gate status.'

$compileUnavailableRepo = Join-Path $scratchRoot 'compile-gate-unavailable'
Initialize-RuleHarnessScopeRepo -RepoPath $compileUnavailableRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Write-CompileStatusFixture -RepoPath $compileUnavailableRepo -Status 'unavailable' -ReasonCode 'unity-mcp-unreachable' -Summary 'Unity MCP health check failed.'
$compileUnavailableReport = Invoke-RuleHarness `
    -RepoRoot $compileUnavailableRepo `
    -ConfigPath (Join-Path $compileUnavailableRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
$compileUnavailableStage = @($compileUnavailableReport.stageResults | Where-Object stage -eq 'compile_gate' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition (-not [bool]$compileUnavailableReport.execution.compileVerified -and [string]$compileUnavailableReport.execution.compileGateStatus -eq 'unavailable' -and [string]$compileUnavailableStage.status -eq 'skipped' -and -not [bool]$compileUnavailableReport.failed) `
    -Message 'Expected Unity MCP availability failures to be classified separately from real compile failures.'
Assert-RuleHarness `
    -Condition (@($compileUnavailableReport.actionItems | Where-Object kind -eq 'restore-unity-mcp').Count -eq 1) `
    -Message 'Expected unavailable compile verification to request Unity MCP restoration.'

$compileBlockedRepo = Join-Path $scratchRoot 'compile-gate-blocked'
Initialize-RuleHarnessScopeRepo -RepoPath $compileBlockedRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Write-CompileStatusFixture -RepoPath $compileBlockedRepo -Status 'blocked' -ReasonCode 'play-mode-active' -Summary 'Unity is currently in play mode.'
$compileBlockedReport = Invoke-RuleHarness `
    -RepoRoot $compileBlockedRepo `
    -ConfigPath (Join-Path $compileBlockedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition ([string]$compileBlockedReport.execution.compileGateStatus -eq 'blocked' -and @($compileBlockedReport.actionItems | Where-Object kind -eq 'retry-compile-verification').Count -eq 1 -and -not [bool]$compileBlockedReport.failed) `
    -Message 'Expected play-mode or compiling states to be classified as blocked compile verification, not compile failure.'

$compileFailedRepo = Join-Path $scratchRoot 'compile-gate-failed'
Initialize-RuleHarnessScopeRepo -RepoPath $compileFailedRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Write-CompileStatusFixture -RepoPath $compileFailedRepo -Status 'failed' -ReasonCode 'compile-errors' -Summary 'Unity compile reported 2 error(s).'
$compileFailedReport = Invoke-RuleHarness `
    -RepoRoot $compileFailedRepo `
    -ConfigPath (Join-Path $compileFailedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition ([string]$compileFailedReport.execution.compileGateStatus -eq 'failed' -and [bool]$compileFailedReport.failed -and @($compileFailedReport.actionItems | Where-Object kind -eq 'fix-compile-errors').Count -eq 1) `
    -Message 'Expected actual Unity compile errors to remain a hard failure.'

$rulesOnlyScopeRepo = Join-Path $scratchRoot 'rules-only-scope'
Initialize-RuleHarnessScopeRepo -RepoPath $rulesOnlyScopeRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
$rulesOnlyMutationState = [pscustomobject]@{
    enabled = $true
    mode    = 'code_and_rules'
}
$rulesOnlyScopeInfo = [pscustomobject]@{
    scopePath  = 'AGENTS.md'
    scopeGuard = 'rules-only'
}
$rulesOnlyFeatureBatch = [pscustomobject]@{
    id                       = 'batch-rules-feature'
    kind                     = 'code_fix'
    targetFiles              = @('Assets/Scripts/Features/Clean/Application/CleanService.cs')
    reason                   = 'Fixture feature mutation under rules-only scope.'
    validation               = @('rule_harness_tests')
    expectedFindingsResolved = @()
    status                   = 'planned'
    featureNames             = @('Clean')
    ownerDocs                = @('AGENTS.md')
    sourceFindingTypes       = @('doc_drift')
    fingerprint              = $null
    riskScore                = $null
    riskLabel                = $null
    ownershipStatus          = 'pending'
    operations               = @([pscustomobject]@{
        type        = 'replace_text'
        targetPath  = 'Assets/Scripts/Features/Clean/Application/CleanService.cs'
        searchText  = 'CleanService'
        replaceText = 'CleanService'
    })
}
$rulesOnlyFeatureMutation = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($rulesOnlyFeatureBatch) `
    -InitialStaticFindings @() `
    -RepoRoot $rulesOnlyScopeRepo `
    -Config $config `
    -MutationState $rulesOnlyMutationState `
    -ScopeInfo $rulesOnlyScopeInfo `
    -DryRun
$rulesOnlyFeatureStage = @($rulesOnlyFeatureMutation.stageResults | Where-Object stage -eq 'mutation' | Select-Object -First 1)[0]
Assert-RuleHarness `
    -Condition ($rulesOnlyFeatureMutation.failed -and @($rulesOnlyFeatureMutation.skippedBatches | Where-Object reasonCode -eq 'rules-scope-mutation-violation').Count -eq 1) `
    -Message 'Expected rules-only scope to stop feature code batches with rules-scope-mutation-violation.'
Assert-RuleHarness `
    -Condition ($null -ne $rulesOnlyFeatureStage -and [string]$rulesOnlyFeatureStage.details.failureReason -eq 'rules-scope-mutation-violation') `
    -Message 'Expected mutation stage details to expose the rules-scope-mutation-violation failure reason.'
Assert-RuleHarness `
    -Condition (@($rulesOnlyFeatureMutation.actionItems | Where-Object kind -eq 'rules-scope-mutation-violation').Count -eq 1) `
    -Message 'Expected rules-only scope violations to surface a dedicated action item.'

$rulesOnlyDocBatch = [pscustomobject]@{
    id                       = 'batch-rules-doc'
    kind                     = 'rule_fix'
    targetFiles              = @('AGENTS.md')
    reason                   = 'Fixture rules-only doc mutation.'
    validation               = @('rule_harness_tests')
    expectedFindingsResolved = @()
    status                   = 'planned'
    featureNames             = @()
    ownerDocs                = @('AGENTS.md')
    sourceFindingTypes       = @('doc_drift')
    fingerprint              = $null
    riskScore                = $null
    riskLabel                = $null
    ownershipStatus          = 'pending'
    operations               = @([pscustomobject]@{
        type = 'doc_edit'
        edits = @([pscustomobject]@{
            targetPath  = 'AGENTS.md'
            searchText  = 'Read `/docs/rules/architecture-rules.md`.'
            replaceText = 'Read `/docs/rules/architecture-rules.md`.'
        })
    })
}
$rulesOnlyDocMutation = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($rulesOnlyDocBatch) `
    -InitialStaticFindings @() `
    -RepoRoot $rulesOnlyScopeRepo `
    -Config $config `
    -MutationState $rulesOnlyMutationState `
    -ScopeInfo $rulesOnlyScopeInfo `
    -DryRun
Assert-RuleHarness `
    -Condition ((-not $rulesOnlyDocMutation.failed) -and @($rulesOnlyDocMutation.skippedBatches | Where-Object reasonCode -eq 'rules-scope-mutation-violation').Count -eq 0) `
    -Message 'Expected rules-only scope to allow AGENTS.md rule-fix batches.'

$rulesOnlyCsprojBatch = [pscustomobject]@{
    id                       = 'batch-rules-csproj'
    kind                     = 'code_fix'
    targetFiles              = @('Assembly-CSharp.csproj')
    reason                   = 'Fixture generated project mutation under rules-only scope.'
    validation               = @('rule_harness_tests')
    expectedFindingsResolved = @()
    status                   = 'planned'
    featureNames             = @()
    ownerDocs                = @('AGENTS.md')
    sourceFindingTypes       = @('doc_drift')
    fingerprint              = $null
    riskScore                = $null
    riskLabel                = $null
    ownershipStatus          = 'pending'
    operations               = @([pscustomobject]@{
        type        = 'replace_text'
        targetPath  = 'Assembly-CSharp.csproj'
        searchText  = '<Project>'
        replaceText = '<Project>'
    })
}
$rulesOnlyCsprojMutation = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($rulesOnlyCsprojBatch) `
    -InitialStaticFindings @() `
    -RepoRoot $rulesOnlyScopeRepo `
    -Config $config `
    -MutationState $rulesOnlyMutationState `
    -ScopeInfo $rulesOnlyScopeInfo `
    -DryRun
Assert-RuleHarness `
    -Condition (@($rulesOnlyCsprojMutation.skippedBatches | Where-Object reasonCode -eq 'rules-scope-mutation-violation').Count -eq 1) `
    -Message 'Expected rules-only scope to block generated .csproj mutation batches.'

$featureDependencyPassedRepo = Join-Path $scratchRoot 'feature-dependency-gate-passed'
Initialize-RuleHarnessScopeRepo -RepoPath $featureDependencyPassedRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
New-Item -ItemType Directory -Path (Join-Path $featureDependencyPassedRepo 'Assets/Editor') -Force | Out-Null
Set-Content -Path (Join-Path $featureDependencyPassedRepo 'Assets/Editor/LayerDependencyValidator.cs') -Value 'public sealed class LayerDependencyValidator { }' -Encoding UTF8
Write-FeatureDependencyReportFixture -RepoPath $featureDependencyPassedRepo -HasCycles $false -FeatureCount 4 -EdgeCount 7
$featureDependencyPassedReport = Invoke-RuleHarness `
    -RepoRoot $featureDependencyPassedRepo `
    -ConfigPath (Join-Path $featureDependencyPassedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition ([string]$featureDependencyPassedReport.execution.featureDependencyGateStatus -eq 'passed' -and [int]$featureDependencyPassedReport.execution.featureDependencyCycleCount -eq 0 -and -not [bool]$featureDependencyPassedReport.failed) `
    -Message 'Expected acyclic feature dependency reports to pass the feature dependency gate.'

$featureDependencyFailedRepo = Join-Path $scratchRoot 'feature-dependency-gate-failed'
Initialize-RuleHarnessScopeRepo -RepoPath $featureDependencyFailedRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
New-Item -ItemType Directory -Path (Join-Path $featureDependencyFailedRepo 'Assets/Editor') -Force | Out-Null
Set-Content -Path (Join-Path $featureDependencyFailedRepo 'Assets/Editor/LayerDependencyValidator.cs') -Value 'public sealed class LayerDependencyValidator { }' -Encoding UTF8
Write-FeatureDependencyReportFixture -RepoPath $featureDependencyFailedRepo -HasCycles $true -FeatureCount 3 -EdgeCount 3 -Cycles @(
    [pscustomobject]@{
        features = @('Combat', 'Enemy')
        evidence = @(
            [pscustomobject]@{ path = 'Assets/Scripts/Features/Combat/Application/CombatLoop.cs'; line = 12 },
            [pscustomobject]@{ path = 'Assets/Scripts/Features/Enemy/Application/EnemyLoop.cs'; line = 18 }
        )
    }
)
$featureDependencyFailedReport = Invoke-RuleHarness `
    -RepoRoot $featureDependencyFailedRepo `
    -ConfigPath (Join-Path $featureDependencyFailedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition ([string]$featureDependencyFailedReport.execution.featureDependencyGateStatus -eq 'failed' -and [int]$featureDependencyFailedReport.execution.featureDependencyCycleCount -eq 1 -and [bool]$featureDependencyFailedReport.failed) `
    -Message 'Expected feature dependency cycles to fail the feature dependency gate.'
Assert-RuleHarness `
    -Condition (@($featureDependencyFailedReport.findings | Where-Object { $_.title -eq 'Feature dependency cycle' }).Count -eq 1 -and @($featureDependencyFailedReport.actionItems | Where-Object kind -eq 'break-feature-dependency-cycle').Count -eq 1) `
    -Message 'Expected feature dependency cycles to surface a finding and action item.'
Assert-RuleHarness `
    -Condition ($featureDependencyPassedReport.PSObject.Properties.Name -contains 'featureDependencyRepairPolicySnapshot' -and [string]$featureDependencyPassedReport.featureDependencyRepairPolicySnapshot.sourceOrder[0] -eq 'AGENTS.md' -and @($featureDependencyPassedReport.featureDependencyRepairPolicySnapshot.sourceOrder) -contains 'docs/rules/architecture-rules.md') `
    -Message 'Expected cycle repair reports to expose the AGENTS.md -> owner doc policy source order.'

$featureDependencyUnsupportedRepo = Join-Path $scratchRoot 'feature-dependency-repair-unsupported'
Initialize-RuleHarnessScopeRepo -RepoPath $featureDependencyUnsupportedRepo -Features @(
    [pscustomobject]@{ Name = 'Combat'; ApplicationContent = @"
using Features.Enemy.Application;

namespace Features.Combat.Application
{
    public sealed class CombatService
    {
        private readonly EnemyService _enemy;

        public CombatService(EnemyService enemy)
        {
            _enemy = enemy;
        }

        public int Run()
        {
            return _enemy.GetThreat();
        }
    }
}
"@ },
    [pscustomobject]@{ Name = 'Enemy'; ApplicationContent = @"
using Features.Combat.Application;

namespace Features.Enemy.Application
{
    public sealed class EnemyService
    {
        public int GetThreat()
        {
            return 7;
        }

        public CombatService Echo(CombatService combat)
        {
            return combat;
        }
    }
}
"@ }
) -ArchitectureDocContent '# Architecture Rules'
Set-Content -Path (Join-Path $featureDependencyUnsupportedRepo 'Assets/Scripts/Features/Combat/CombatSetup.cs') -Value @"
using Features.Combat.Application;
using Features.Enemy.Application;

namespace Features.Combat
{
    public sealed class CombatSetup
    {
        public CombatService Create()
        {
            return new CombatService(new EnemyService());
        }
    }
}
"@ -Encoding UTF8
Copy-FeatureDependencyValidatorFixture -RepoPath $featureDependencyUnsupportedRepo
Write-CompileRefreshStub -RepoPath $featureDependencyUnsupportedRepo -Status 'passed'
$featureDependencyUnsupportedReport = Invoke-RuleHarness `
    -RepoRoot $featureDependencyUnsupportedRepo `
    -ConfigPath (Join-Path $featureDependencyUnsupportedRepo 'tools/rule-harness/config.json') `
    -DisableLlm
Assert-RuleHarness `
    -Condition ([string]$featureDependencyUnsupportedReport.execution.featureDependencyGateStatus -eq 'failed' -and [string]$featureDependencyUnsupportedReport.execution.featureDependencyRepairStatus -eq 'failed' -and [int]$featureDependencyUnsupportedReport.execution.featureDependencyUnsupportedCycleCount -eq 1 -and [bool]$featureDependencyUnsupportedReport.failed) `
    -Message 'Expected doc-blocked cycles to fail automatic feature dependency repair and remain failed.'
Assert-RuleHarness `
    -Condition (@($featureDependencyUnsupportedReport.findings | Where-Object { $_.title -eq 'Feature dependency cycle repair unsupported' }).Count -eq 1 -and -not (Test-Path -LiteralPath (Join-Path $featureDependencyUnsupportedRepo 'Assets/Scripts/Features/Combat/Application/Ports/IEnemyServicePort.cs'))) `
    -Message 'Expected unsupported cycle repair runs to collect a repair-unsupported finding without mutating code.'

$featureDependencyRepairRepo = Join-Path $scratchRoot 'feature-dependency-repair-port'
Initialize-RuleHarnessScopeRepo -RepoPath $featureDependencyRepairRepo -Features @(
    [pscustomobject]@{ Name = 'Combat'; ApplicationContent = @"
using Features.Enemy.Application;

namespace Features.Combat.Application
{
    public sealed class CombatService
    {
        private readonly EnemyService _enemy;

        public CombatService(EnemyService enemy)
        {
            _enemy = enemy;
        }

        public int Run()
        {
            return _enemy.GetThreat();
        }
    }
}
"@ },
    [pscustomobject]@{ Name = 'Enemy'; ApplicationContent = @"
using Features.Combat.Application;

namespace Features.Enemy.Application
{
    public sealed class EnemyService
    {
        public int GetThreat()
        {
            return 7;
        }

        public CombatService Echo(CombatService combat)
        {
            return combat;
        }
    }
}
"@ }
) -ArchitectureDocContent @"
# Architecture Rules

consumer-owned Application/Ports seams are preferred for cross-feature dependencies.
Application/Ports references are excluded from the DAG gate.
"@
Set-Content -Path (Join-Path $featureDependencyRepairRepo 'Assets/Scripts/Features/Combat/CombatSetup.cs') -Value @"
using Features.Combat.Application;
using Features.Enemy.Application;

namespace Features.Combat
{
    public sealed class CombatSetup
    {
        public CombatService Create()
        {
            return new CombatService(new EnemyService());
        }
    }
}
"@ -Encoding UTF8
Copy-FeatureDependencyValidatorFixture -RepoPath $featureDependencyRepairRepo
Write-CompileRefreshStub -RepoPath $featureDependencyRepairRepo -Status 'passed'
$featureDependencyRepairReport = Invoke-RuleHarness `
    -RepoRoot $featureDependencyRepairRepo `
    -ConfigPath (Join-Path $featureDependencyRepairRepo 'tools/rule-harness/config.json') `
    -DisableLlm
$combatServiceContent = Get-Content -Path (Join-Path $featureDependencyRepairRepo 'Assets/Scripts/Features/Combat/Application/CombatService.cs') -Raw
$combatSetupContent = Get-Content -Path (Join-Path $featureDependencyRepairRepo 'Assets/Scripts/Features/Combat/CombatSetup.cs') -Raw
Assert-RuleHarness `
    -Condition ([string]$featureDependencyRepairReport.execution.featureDependencyGateStatus -eq 'passed' -and [string]$featureDependencyRepairReport.execution.featureDependencyRepairStatus -eq 'passed' -and [string]$featureDependencyRepairReport.execution.compileGateStatus -eq 'passed' -and -not [bool]$featureDependencyRepairReport.failed) `
    -Message 'Expected supported cycles to be repaired into a compile-clean acyclic graph.'
Assert-RuleHarness `
    -Condition ((Test-Path -LiteralPath (Join-Path $featureDependencyRepairRepo 'Assets/Scripts/Features/Combat/Application/Ports/IEnemyServicePort.cs')) -and (Test-Path -LiteralPath (Join-Path $featureDependencyRepairRepo 'Assets/Scripts/Features/Enemy/Infrastructure/EnemyServicePortAdapter.cs')) -and $combatServiceContent.Contains('IEnemyServicePort') -and $combatSetupContent.Contains('EnemyServicePortAdapter') -and @($featureDependencyRepairReport.featureDependencyRepairCodeCommits).Count -ge 1) `
    -Message 'Expected port inversion repair to create the consumer port, provider adapter, and code commit.'

$mcpPolicyViolationRepo = Join-Path $scratchRoot 'mcp-policy-violation'
Initialize-RuleHarnessScopeRepo -RepoPath $mcpPolicyViolationRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Set-Content -Path (Join-Path $mcpPolicyViolationRepo 'tools/reintroduced-ui-smoke.ps1') -Value @"
`$root = "http://127.0.0.1:51234"
Invoke-RestMethod -Method Post -Uri "`$root/ui/button/invoke" -ContentType "application/json" -Body '{"path":"/UIRoot/Canvas/lobby/RoomDetailPanel/ReadyButton"}'
"@ -Encoding UTF8
$mcpPolicyViolationReport = Invoke-RuleHarness `
    -RepoRoot $mcpPolicyViolationRepo `
    -ConfigPath (Join-Path $mcpPolicyViolationRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition (@($mcpPolicyViolationReport.findings | Where-Object { $_.title -eq 'Hardcoded MCP UI smoke reintroduced' -and $_.evidence[0].path -eq 'tools/reintroduced-ui-smoke.ps1' }).Count -eq 1 -and [bool]$mcpPolicyViolationReport.failed) `
    -Message 'Expected hardcoded MCP UI smoke under tools/ to surface a dedicated high-severity finding and fail the run.'
Assert-RuleHarness `
    -Condition (@($mcpPolicyViolationReport.actionItems | Where-Object kind -eq 'remove-hardcoded-mcp-ui-smoke').Count -eq 1) `
    -Message 'Expected hardcoded MCP UI smoke findings to emit a policy action item.'

$mcpPolicyAllowedRepo = Join-Path $scratchRoot 'mcp-policy-allowed'
Initialize-RuleHarnessScopeRepo -RepoPath $mcpPolicyAllowedRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Copy-Item -LiteralPath (Join-Path $repoRoot 'tools/mcp-test-compile.ps1') -Destination (Join-Path $mcpPolicyAllowedRepo 'tools/mcp-test-compile.ps1') -Force
$mcpPolicyAllowedReport = Invoke-RuleHarness `
    -RepoRoot $mcpPolicyAllowedRepo `
    -ConfigPath (Join-Path $mcpPolicyAllowedRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun
Assert-RuleHarness `
    -Condition (@($mcpPolicyAllowedReport.findings | Where-Object title -eq 'Hardcoded MCP UI smoke reintroduced').Count -eq 0 -and -not [bool]$mcpPolicyAllowedReport.failed) `
    -Message 'Expected generic MCP compile/status scripts to avoid false positives from the hardcoded smoke policy.'

$mcpPolicySummaryRepo = Join-Path $scratchRoot 'mcp-policy-summary'
Initialize-RuleHarnessScopeRepo -RepoPath $mcpPolicySummaryRepo -Features @(
    [pscustomobject]@{ Name = 'Clean'; ApplicationContent = 'namespace Features.Clean.Application { public sealed class CleanService { } }' }
)
Set-Content -Path (Join-Path $mcpPolicySummaryRepo 'tools/reintroduced-ui-smoke.ps1') -Value @"
function Invoke-Smoke {
    param([string]`$Root = "http://127.0.0.1:51234")

    Invoke-RestMethod -Method Post -Uri "`$Root/input/click" -ContentType "application/json" -Body '{"x":0.5,"y":0.5,"normalized":true}'
}
"@ -Encoding UTF8
$summaryArtifactDir = Join-Path $mcpPolicySummaryRepo 'Temp/RuleHarnessScript'
$summaryReportPath = Join-Path $summaryArtifactDir 'rule-harness-report.json'
$summaryPath = Join-Path $summaryArtifactDir 'rule-harness-summary.md'
Push-Location $mcpPolicySummaryRepo
try {
    & (Join-Path $mcpPolicySummaryRepo 'tools/rule-harness/run-rule-harness.ps1') `
        -RepoRoot $mcpPolicySummaryRepo `
        -ConfigPath (Join-Path $mcpPolicySummaryRepo 'tools/rule-harness/config.json') `
        -ArtifactDir $summaryArtifactDir `
        -ReportPath $summaryReportPath `
        -SummaryPath $summaryPath `
        -DisableLlm `
        -DryRun `
        -SkipCompileStatusRefresh `
        -SkipFeatureDependencyRefresh
}
finally {
    Pop-Location
}
$summaryContent = Get-Content -Path $summaryPath -Raw
Assert-RuleHarness `
    -Condition ((Test-Path -LiteralPath $summaryPath) -and $summaryContent.Contains('### Stage Status') -and $summaryContent.Contains('### Next Actions') -and $summaryContent.Contains('Remove hardcoded MCP UI smoke from automation')) `
    -Message 'Expected summary output to surface the new MCP UI smoke policy in Stage Status and Next Actions.'

$scheduledRepo = Join-Path $scratchRoot 'scheduled-status'
Initialize-RuleHarnessScopeRepo -RepoPath $scheduledRepo -Features @(
    [pscustomobject]@{ Name = 'CleanA'; ApplicationContent = 'namespace Features.CleanA.Application { public sealed class CleanAService { } }' },
    [pscustomobject]@{ Name = 'Broken'; ApplicationContent = 'namespace Features.Broken.Application { public sealed class BrokenService { public void Run() { Debug.Log(""broken""); } } }' }
)
Push-Location $scheduledRepo
try {
    & (Join-Path $scheduledRepo 'tools/rule-harness/run-rule-harness-scheduled.ps1') -MutationMode 'code_and_rules'
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
    -Condition (-not [bool]$latestStatus.llmEnabled) `
    -Message 'Expected scheduled wrapper to default to deterministic static-only mode without requiring LLM quota.'
Assert-RuleHarness `
    -Condition ((Test-Path -LiteralPath ([string]$latestStatus.docProposalPath)) -and (@($latestStatus.topDocProposals).Count -ge 1)) `
    -Message 'Expected scheduled runs to emit the proposal markdown file and surface top doc proposals.'

$roleRepo = Join-Path $scratchRoot 'role-harness'
Initialize-RuleHarnessScopeRepo -RepoPath $roleRepo -Features @(
    [pscustomobject]@{ Name = 'RoleA'; ApplicationContent = 'namespace Features.RoleA.Application { public sealed class RoleAService { } }' },
    [pscustomobject]@{ Name = 'RoleB'; ApplicationContent = 'namespace Features.RoleB.Application { public sealed class RoleBService { public object Load() { return UnityEngine.Resources.Load<object>(""RoleBConfig""); } } }' },
    [pscustomobject]@{ Name = 'RoleC'; ApplicationContent = 'namespace Features.RoleC.Application { public sealed class RoleCService { } }' },
    [pscustomobject]@{ Name = 'RoleD'; ApplicationContent = 'namespace Features.RoleD.Application { public sealed class RoleDService { } }' },
    [pscustomobject]@{ Name = 'RoleE'; ApplicationContent = 'namespace Features.RoleE.Application { public sealed class RoleEService { } }' }
)
Set-Content -Path (Join-Path $roleRepo 'Assets/Scripts/Features/RoleE/RoleESetup.cs') -Value @"
namespace Features.RoleE
{
    /// <summary>
    /// Rule-harness generated composition root placeholder.
    /// Replace this scaffold with the real feature setup when wiring the feature.
    /// </summary>
    public sealed class RoleESetup
    {
    }
}
"@ -Encoding UTF8
Set-Content -Path (Join-Path $roleRepo 'Tests/RoleE/RoleESetupValidation.txt') -Value 'fixture validation asset for RoleE review-work derivation' -Encoding UTF8
Invoke-RuleHarnessTestGit -RepoPath $roleRepo -Arguments @('add', 'Assets/Scripts/Features/RoleE/RoleESetup.cs', 'Tests/RoleE/RoleESetupValidation.txt') | Out-Null
Invoke-RuleHarnessTestGit -RepoPath $roleRepo -Arguments @('commit', '-m', 'add placeholder setup fixture') | Out-Null
$roleConfigPath = Join-Path $roleRepo 'tools/rule-harness/config.json'
$roleConfig = Get-Content -Path $roleConfigPath -Raw | ConvertFrom-Json
$roleConfig.scan.maxScopesPerRun = 1
$roleConfig.agentRunner.enabled = $false
$roleConfig | ConvertTo-Json -Depth 50 | Set-Content -Path $roleConfigPath -Encoding UTF8
$roleReviewDir = Join-Path $roleRepo 'Temp/RoleReview'
& (Join-Path $roleRepo 'tools/rule-harness/run-tech-debt-review.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -OutputRoot (Join-Path $roleRepo 'Temp/RuleHarnessRoles') `
    -OutputDir $roleReviewDir | Out-Null
$roleReviewPath = Join-Path $roleReviewDir 'report.json'
$roleReview = Get-Content -Path $roleReviewPath -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($roleReview.scannedScopes).Count -eq 5 -and [int]$roleReview.severityScore -ge 0 -and [int]$roleReview.severityScore -le 100 -and $roleReview.PSObject.Properties.Name -contains 'scoreBreakdown' -and $roleReview.PSObject.Properties.Name -contains 'refactorTargets') `
    -Message 'Expected tech debt role review to scan all scopes and emit severity/refactor artifacts.'
Assert-RuleHarness `
    -Condition ([int]$roleReview.severityScore -gt 0 -and [int]$roleReview.scoreBreakdown.heuristicFindingCount -ge 1 -and @($roleReview.reviewItems | Where-Object title -eq 'Runtime Resources.Load dependency').Count -ge 1 -and @($roleReview.refactorTargets | Where-Object path -eq 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs').Count -eq 1) `
    -Message 'Expected tech debt role review to score heuristic debt signals and surface them as review/refactor artifacts.'
Assert-RuleHarness `
    -Condition (@($roleReview.reviewItems | Where-Object { $_.PSObject.Properties.Name -contains 'remediationKind' -or $_.PSObject.Properties.Name -contains 'source' }).Count -eq 0) `
    -Message 'Expected tech debt role review items to stay observational and omit remediation/source directives.'
Assert-RuleHarness `
    -Condition (-not (Test-Path -LiteralPath (Join-Path $roleRepo 'Temp/RuleHarnessState/feature-scan-state.json')) -and -not (Test-Path -LiteralPath (Join-Path $roleRepo 'Temp/RuleHarnessState/doc-proposals.json'))) `
    -Message 'Expected tech debt role review to leave feature scan state and doc proposal backlog untouched.'

& (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $roleReviewPath `
    -OutputDir (Join-Path $roleRepo 'Temp/DerivedReviewWork') `
    -DryRun | Out-Null
$derivedReviewWork = Get-Content -Path (Join-Path $roleRepo 'Temp/DerivedReviewWork/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition ((@($derivedReviewWork.decisionTrace | Where-Object { [string]$_ -like '*Batch batch-001 capability discovery*' }).Count -ge 1) -and @($derivedReviewWork.skippedBatches | Where-Object { [string]$_.id -eq 'batch-001' -and [string]$_.reasonCode -eq 'manual-validation-required' }).Count -eq 0) `
    -Message 'Expected review work role to derive a mutation batch from observational review items instead of relying on review remediation directives.'
Assert-RuleHarness `
    -Condition (@($derivedReviewWork.agentWorkQueue | Where-Object { @($_.observations | Where-Object title -eq 'Runtime Resources.Load dependency').Count -ge 1 }).Count -ge 1) `
    -Message 'Expected review work role to turn non-recipe review observations into coding-agent work queue entries.'

$agentOnlyReview = Join-Path $roleRepo 'Temp/agent-only-review.json'
[pscustomobject]@{
    baseCommitSha = [string]$roleReview.baseCommitSha
    reviewItems = @([pscustomobject]@{
        findingType = 'code-smell'
        severity = 'high'
        title = 'Runtime Resources.Load dependency'
        evidence = @([pscustomobject]@{
            path = 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs'
            line = 1
            snippet = 'Resources.Load<object>("RoleBConfig")'
        })
    })
    recommendedBatches = @()
} | ConvertTo-Json -Depth 20 | Set-Content -Path $agentOnlyReview -Encoding UTF8
& (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $agentOnlyReview `
    -OutputDir (Join-Path $roleRepo 'Temp/AgentOnlyReviewWork') `
    -DryRun | Out-Null
$agentOnlyReviewWork = Get-Content -Path (Join-Path $roleRepo 'Temp/AgentOnlyReviewWork/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition ([bool]$agentOnlyReviewWork.failed -and @($agentOnlyReviewWork.agentWorkQueue).Count -eq 1 -and @($agentOnlyReviewWork.agentWorkReports | Where-Object { $_.status -eq 'blocked' -and $_.blockedReason -eq 'agent-runner-disabled' }).Count -eq 1 -and @($agentOnlyReviewWork.skippedBatches | Where-Object { $_.kind -eq 'agent_work' -and $_.reasonCode -eq 'agent-runner-disabled' }).Count -eq 1) `
    -Message 'Expected review work role to report agent-work blocked state instead of silently passing when review observations require a coding agent.'

$fakeValidAgent = Join-Path $roleRepo 'Temp/fake-valid-agent.cmd'
Set-Content -Path $fakeValidAgent -Value @'
@echo off
set "OUTPUT="
:args
if "%~1"=="" goto write
if "%~1"=="-o" goto found_output
shift
goto args
:found_output
shift
set "OUTPUT=%~1"
shift
goto args
:write
if "%OUTPUT%"=="" exit /b 2
> "%OUTPUT%" echo {"summary":"fake valid batch","batches":[{"id":"fake","kind":"code_fix","targetFiles":["Assets/Scripts/Features/RoleB/Application/RoleBService.cs"],"reason":"fixture valid agent batch","validation":["rule_harness_tests"],"expectedFindingsResolved":[],"status":"proposed","ownerDocs":["AGENTS.md"],"operations":[{"type":"write_file","targetPath":"Assets/Scripts/Features/RoleB/Application/RoleBService.cs","content":"namespace Features.RoleB.Application { public sealed class RoleBService { public object Load() { return null; } } }"}]}],"blocked":[]}
exit /b 0
'@ -Encoding ASCII
$roleConfig.agentRunner.enabled = $true
$roleConfig.agentRunner.commandPath = $fakeValidAgent
$roleConfig.agentRunner.maxTasksPerRun = 1
$roleConfig.agentRunner.timeoutSec = 30
$roleConfig | ConvertTo-Json -Depth 50 | Set-Content -Path $roleConfigPath -Encoding UTF8
& (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $agentOnlyReview `
    -OutputDir (Join-Path $roleRepo 'Temp/FakeValidAgentReviewWork') `
    -DryRun | Out-Null
$fakeValidAgentWork = Get-Content -Path (Join-Path $roleRepo 'Temp/FakeValidAgentReviewWork/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($fakeValidAgentWork.agentWorkReports | Where-Object { $_.status -eq 'proposed' -and @($_.changedFiles | Where-Object { $_ -eq 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs' }).Count -eq 1 }).Count -eq 1) `
    -Message 'Expected enabled fake agent runner to convert valid JSON batches into proposed agent work.'

$successBudgetReview = Join-Path $roleRepo 'Temp/success-budget-review.json'
[pscustomobject]@{
    baseCommitSha = [string]$roleReview.baseCommitSha
    reviewItems = @(
        [pscustomobject]@{
            findingType = 'code-smell'
            severity = 'high'
            title = 'Runtime Resources.Load dependency'
            evidence = @([pscustomobject]@{
                path = 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs'
                line = 1
                snippet = 'Resources.Load<object>("RoleBConfig")'
            })
        },
        [pscustomobject]@{
            findingType = 'tech_debt'
            severity = 'medium'
            title = 'Runtime object lookup API'
            evidence = @([pscustomobject]@{
                path = 'Assets/Scripts/Features/RoleC/Application/RoleCService.cs'
                line = 1
                snippet = 'FindObjectsByType<object>()'
            })
        },
        [pscustomobject]@{
            findingType = 'tech_debt'
            severity = 'medium'
            title = 'Runtime legacy compatibility path'
            evidence = @([pscustomobject]@{
                path = 'Assets/Scripts/Features/RoleD/Application/RoleDService.cs'
                line = 1
                snippet = '// Legacy shim kept for fixture'
            })
        }
    )
    recommendedBatches = @()
} | ConvertTo-Json -Depth 20 | Set-Content -Path $successBudgetReview -Encoding UTF8

$fakeSuccessBudgetAgent = Join-Path $roleRepo 'Temp/fake-success-budget-agent.cmd'
Set-Content -Path $fakeSuccessBudgetAgent -Value @'
@echo off
set "OUTPUT="
:args
if "%~1"=="" goto write
if "%~1"=="-o" goto found_output
shift
goto args
:found_output
shift
set "OUTPUT=%~1"
shift
goto args
:write
if "%OUTPUT%"=="" exit /b 2
echo %OUTPUT% | findstr /C:"review-work-001" >nul
if not errorlevel 1 goto blocked_first
echo %OUTPUT% | findstr /C:"review-work-002" >nul
if not errorlevel 1 goto proposed_second
echo %OUTPUT% | findstr /C:"review-work-003" >nul
if not errorlevel 1 goto proposed_third
exit /b 3
:blocked_first
> "%OUTPUT%" echo {"summary":"fixture insufficient snapshot","batches":[],"blocked":[{"reasonCode":"insufficient_snapshot","summary":"fixture insufficient snapshot","targetFiles":["Assets/Scripts/Features/RoleB/Application/RoleBService.cs"]}]}
exit /b 0
:proposed_second
> "%OUTPUT%" echo {"summary":"fixture second proposed","batches":[{"id":"fake","kind":"code_fix","targetFiles":["Assets/Scripts/Features/RoleC/Application/RoleCService.cs"],"reason":"fixture success budget second task","validation":["rule_harness_tests"],"expectedFindingsResolved":[],"status":"proposed","ownerDocs":[],"operations":[{"type":"write_file","targetPath":"Assets/Scripts/Features/RoleC/Application/RoleCService.cs","content":"namespace Features.RoleC.Application { public sealed class RoleCService { public object Lookup() { return null; } } }"}]}],"blocked":[]}
exit /b 0
:proposed_third
> "%OUTPUT%" echo {"summary":"fixture third should be skipped","batches":[{"id":"fake","kind":"code_fix","targetFiles":["Assets/Scripts/Features/RoleD/Application/RoleDService.cs"],"reason":"fixture success budget third task","validation":["rule_harness_tests"],"expectedFindingsResolved":[],"status":"proposed","ownerDocs":["AGENTS.md"],"operations":[{"type":"write_file","targetPath":"Assets/Scripts/Features/RoleD/Application/RoleDService.cs","content":"namespace Features.RoleD.Application { public sealed class RoleDService { public object Legacy() { return null; } } }"}]}],"blocked":[]}
exit /b 0
'@ -Encoding ASCII
$roleConfig.agentRunner.commandPath = $fakeSuccessBudgetAgent
$roleConfig.agentRunner.maxTasksPerRun = 1
$roleConfig | ConvertTo-Json -Depth 50 | Set-Content -Path $roleConfigPath -Encoding UTF8
$successBudgetWorkDir = Join-Path $roleRepo 'Temp/FakeSuccessBudgetReviewWork'
& (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $successBudgetReview `
    -OutputDir $successBudgetWorkDir `
    -DryRun | Out-Null
$successBudgetWork = Get-Content -Path (Join-Path $successBudgetWorkDir 'report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($successBudgetWork.agentWorkReports | Where-Object { $_.taskId -eq 'review-work-001' -and $_.status -eq 'blocked' -and $_.blockedReason -eq 'insufficient_snapshot' }).Count -eq 1) `
    -Message 'Expected success-budget runner to record the first task-level blocked result.'
Assert-RuleHarness `
    -Condition (@($successBudgetWork.agentWorkReports | Where-Object { $_.taskId -eq 'review-work-002' -and $_.status -eq 'proposed' -and @($_.changedFiles | Where-Object { $_ -eq 'Assets/Scripts/Features/RoleC/Application/RoleCService.cs' }).Count -eq 1 }).Count -eq 1) `
    -Message 'Expected success-budget runner to continue after a task-level blocked result and propose the second task.'
Assert-RuleHarness `
    -Condition (@($successBudgetWork.agentWorkReports | Where-Object { $_.taskId -eq 'review-work-003' -and $_.status -eq 'skipped' -and $_.blockedReason -eq 'agent-runner-task-limit' -and $_.summary -match 'success budget was reached' }).Count -eq 1) `
    -Message 'Expected success-budget runner to leave only the third task queued after the proposed-task budget is reached.'

$fakeTimeoutAgent = Join-Path $roleRepo 'Temp/fake-timeout-agent.cmd'
Set-Content -Path $fakeTimeoutAgent -Value @'
@echo off
ping -n 6 127.0.0.1 >nul
'@ -Encoding ASCII
$roleConfig.agentRunner.commandPath = $fakeTimeoutAgent
$roleConfig.agentRunner.timeoutSec = 1
$roleConfig | ConvertTo-Json -Depth 50 | Set-Content -Path $roleConfigPath -Encoding UTF8
$timeoutAgentWorkDir = Join-Path $roleRepo 'Temp/FakeTimeoutAgentReviewWork'
& (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $successBudgetReview `
    -OutputDir $timeoutAgentWorkDir `
    -DryRun | Out-Null
$timeoutAgentWork = Get-Content -Path (Join-Path $timeoutAgentWorkDir 'report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($timeoutAgentWork.agentWorkReports).Count -eq 1 -and @($timeoutAgentWork.agentWorkReports | Where-Object { $_.taskId -eq 'review-work-001' -and $_.status -eq 'blocked' -and $_.blockedReason -eq 'agent-runner-timeout' }).Count -eq 1) `
    -Message 'Expected runner-level agent timeout to stop before trying later tasks.'
$roleConfig.agentRunner.timeoutSec = 30

$fakeReadOnlyAgent = Join-Path $roleRepo 'Temp/fake-read-only-agent.cmd'
Set-Content -Path $fakeReadOnlyAgent -Value @'
@echo off
set "OUTPUT="
:args
if "%~1"=="" goto write
if "%~1"=="-o" goto found_output
shift
goto args
:found_output
shift
set "OUTPUT=%~1"
shift
goto args
:write
if "%OUTPUT%"=="" exit /b 2
> "%OUTPUT%" echo {"summary":"fixture read-only block","batches":[],"blocked":[{"reasonCode":"read_only_sandbox","summary":"fixture read-only block","targetFiles":["Assets/Scripts/Features/RoleB/Application/RoleBService.cs"]}]}
exit /b 0
'@ -Encoding ASCII
$roleConfig.agentRunner.commandPath = $fakeReadOnlyAgent
$roleConfig | ConvertTo-Json -Depth 50 | Set-Content -Path $roleConfigPath -Encoding UTF8
$contractViolationWorkDir = Join-Path $roleRepo 'Temp/FakeReadOnlyAgentReviewWork'
& (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $agentOnlyReview `
    -OutputDir $contractViolationWorkDir `
    -DryRun | Out-Null
$contractViolationWork = Get-Content -Path (Join-Path $contractViolationWorkDir 'report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($contractViolationWork.agentWorkReports | Where-Object { $_.status -eq 'blocked' -and $_.blockedReason -eq 'agent-runner-contract-violation' }).Count -eq 1) `
    -Message 'Expected read_only_sandbox from an agent with complete snapshots to be normalized to agent-runner-contract-violation.'
$contractPlanDir = Join-Path $roleRepo 'Temp/FakeReadOnlyRecurrencePlan'
& (Join-Path $roleRepo 'tools/rule-harness/run-recurrence-plan.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $agentOnlyReview `
    -WorkReportPath (Join-Path $contractViolationWorkDir 'report.json') `
    -OutputDir $contractPlanDir | Out-Null
$contractPlan = Get-Content -Path (Join-Path $contractPlanDir 'report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($contractPlan.preventionItems | Where-Object { @($_.relatedPaths | Where-Object { $_ -eq 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs' }).Count -gt 0 }).Count -eq 0) `
    -Message 'Expected agent-runner-contract-violation to avoid re-queuing candidate product files as recurrence prevention targets.'

$insufficientSnapshotReportPath = Join-Path $roleRepo 'Temp/insufficient-snapshot-work-report.json'
[pscustomobject]@{
    inputReviewPath = $agentOnlyReview
    baseCommitSha = [string]$roleReview.baseCommitSha
    skippedBatches = @([pscustomobject]@{
        id = 'review-work-001'
        kind = 'agent_work'
        reasonCode = 'insufficient_candidate_snapshot'
        targets = @('Assets/Scripts/Features/RoleB/Application/RoleBService.cs')
    })
    rollback = [pscustomobject]@{ performed = $false; failedBatches = @() }
    retryAttempts = 0
    memoryUpdates = @()
} | ConvertTo-Json -Depth 20 | Set-Content -Path $insufficientSnapshotReportPath -Encoding UTF8
$insufficientSnapshotPlanDir = Join-Path $roleRepo 'Temp/InsufficientSnapshotRecurrencePlan'
& (Join-Path $roleRepo 'tools/rule-harness/run-recurrence-plan.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $agentOnlyReview `
    -WorkReportPath $insufficientSnapshotReportPath `
    -OutputDir $insufficientSnapshotPlanDir | Out-Null
$insufficientSnapshotPlan = Get-Content -Path (Join-Path $insufficientSnapshotPlanDir 'report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($insufficientSnapshotPlan.preventionItems | Where-Object { @($_.relatedPaths | Where-Object { $_ -eq 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs' }).Count -gt 0 }).Count -eq 0) `
    -Message 'Expected insufficient_snapshot to avoid re-queuing candidate product files as recurrence prevention targets.'

$insufficientContextReportPath = Join-Path $roleRepo 'Temp/insufficient-context-work-report.json'
[pscustomobject]@{
    inputReviewPath = $agentOnlyReview
    baseCommitSha = [string]$roleReview.baseCommitSha
    skippedBatches = @([pscustomobject]@{
        id = 'review-work-001'
        kind = 'agent_work'
        reasonCode = 'insufficient_context'
        targets = @('Assets/Scripts/Features/RoleB/Application/RoleBService.cs')
    })
    rollback = [pscustomobject]@{ performed = $false; failedBatches = @() }
    retryAttempts = 0
    memoryUpdates = @()
} | ConvertTo-Json -Depth 20 | Set-Content -Path $insufficientContextReportPath -Encoding UTF8
$insufficientContextPlanDir = Join-Path $roleRepo 'Temp/InsufficientContextRecurrencePlan'
& (Join-Path $roleRepo 'tools/rule-harness/run-recurrence-plan.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $agentOnlyReview `
    -WorkReportPath $insufficientContextReportPath `
    -OutputDir $insufficientContextPlanDir | Out-Null
$insufficientContextPlan = Get-Content -Path (Join-Path $insufficientContextPlanDir 'report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($insufficientContextPlan.preventionItems | Where-Object { @($_.relatedPaths | Where-Object { $_ -eq 'Assets/Scripts/Features/RoleB/Application/RoleBService.cs' }).Count -gt 0 }).Count -eq 0) `
    -Message 'Expected agent_work insufficient_context to avoid re-queuing candidate product files as recurrence prevention targets.'

$roleConfig.agentRunner.enabled = $false
$roleConfig.agentRunner.commandPath = ''
$roleConfig | ConvertTo-Json -Depth 50 | Set-Content -Path $roleConfigPath -Encoding UTF8

$badPathReview = Join-Path $roleRepo 'Temp/bad-path-review.json'
[pscustomobject]@{
    baseCommitSha = [string]$roleReview.baseCommitSha
    recommendedBatches = @([pscustomobject]@{
        id = 'bad-path'
        kind = 'code_fix'
        targetFiles = @('../outside.cs')
        reason = 'fixture unsafe path'
        validation = @()
        expectedFindingsResolved = @()
        status = 'proposed'
        operations = @()
    })
} | ConvertTo-Json -Depth 20 | Set-Content -Path $badPathReview -Encoding UTF8
$badPathRejected = $false
try {
    & (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
        -RepoRoot $roleRepo `
        -ConfigPath $roleConfigPath `
        -ReviewPath $badPathReview `
        -OutputDir (Join-Path $roleRepo 'Temp/BadPathWork') | Out-Null
}
catch {
    $badPathRejected = $true
}
Assert-RuleHarness `
    -Condition $badPathRejected `
    -Message 'Expected review work role to reject path traversal targets.'

$staleReview = Join-Path $roleRepo 'Temp/stale-review.json'
[pscustomobject]@{
    baseCommitSha = '0000000000000000000000000000000000000000'
    recommendedBatches = @()
} | ConvertTo-Json -Depth 20 | Set-Content -Path $staleReview -Encoding UTF8
$staleRejected = $false
try {
    & (Join-Path $roleRepo 'tools/rule-harness/run-review-work.ps1') `
        -RepoRoot $roleRepo `
        -ConfigPath $roleConfigPath `
        -ReviewPath $staleReview `
        -OutputDir (Join-Path $roleRepo 'Temp/StaleWork') | Out-Null
}
catch {
    $staleRejected = $true
}
Assert-RuleHarness `
    -Condition $staleRejected `
    -Message 'Expected review work role to reject stale commit artifacts.'

$roleWorkReportPath = Join-Path $roleRepo 'Temp/work-report.json'
[pscustomobject]@{
    inputReviewPath = $roleReviewPath
    baseCommitSha = [string]$roleReview.baseCommitSha
    skippedBatches = @([pscustomobject]@{ id = 'skip-1'; reasonCode = 'dirty-target-files'; targets = @('AGENTS.md') })
    rollback = [pscustomobject]@{ performed = $true; failedBatches = @('batch-001') }
    retryAttempts = 1
    memoryUpdates = @([pscustomobject]@{ scopePath = 'tools/rule-harness/README.md'; hitCount = 2 })
} | ConvertTo-Json -Depth 20 | Set-Content -Path $roleWorkReportPath -Encoding UTF8
& (Join-Path $roleRepo 'tools/rule-harness/run-recurrence-plan.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -ReviewPath $roleReviewPath `
    -WorkReportPath $roleWorkReportPath `
    -OutputDir (Join-Path $roleRepo 'Temp/RecurrencePlan') | Out-Null
$rolePlan = Get-Content -Path (Join-Path $roleRepo 'Temp/RecurrencePlan/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (@($rolePlan.preventionItems | Where-Object kind -in @('skipped-batch', 'rollback', 'retry', 'memory-update')).Count -ge 4 -and [bool]$rolePlan.manualValidationRequired) `
    -Message 'Expected recurrence plan role to convert work report failure signals into prevention items.'

$rulesCloseoutPlanPath = Join-Path $roleRepo 'Temp/rules-closeout-plan.json'
[pscustomobject]@{
    baseCommitSha = [string]$roleReview.baseCommitSha
    recommendedBatches = @([pscustomobject]@{
        id = 'rules-closeout-missing'
        kind = 'rule_fix'
        targetFiles = @('tools/rule-harness/roles/RecurrenceWorkHarness.psm1')
        reason = 'fixture rules closeout gate'
        validation = @('rule_harness_tests')
        expectedFindingsResolved = @()
        status = 'proposed'
        operations = @()
        ownerDocs = @('tools/rule-harness/README.md')
    })
} | ConvertTo-Json -Depth 20 | Set-Content -Path $rulesCloseoutPlanPath -Encoding UTF8
& (Join-Path $roleRepo 'tools/rule-harness/run-recurrence-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -PlanPath $rulesCloseoutPlanPath `
    -OutputDir (Join-Path $roleRepo 'Temp/RulesCloseoutWork') | Out-Null
$rulesCloseoutWork = Get-Content -Path (Join-Path $roleRepo 'Temp/RulesCloseoutWork/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition ([bool]$rulesCloseoutWork.failed -and @($rulesCloseoutWork.stageResults | Where-Object { $_.stage -eq 'rules_closeout' -and $_.status -eq 'failed' }).Count -eq 1) `
    -Message 'Expected recurrence work role to block rules-only mutations when closeout artifact does not cover the target.'

$agentOnlyPlanPath = Join-Path $roleRepo 'Temp/agent-only-plan.json'
[pscustomobject]@{
    baseCommitSha = [string]$roleReview.baseCommitSha
    sourceReviewPath = $roleReviewPath
    sourceWorkReportPath = $roleWorkReportPath
    preventionItems = @([pscustomobject]@{
        kind = 'skipped-batch'
        severity = 'medium'
        summary = 'Dirty target prevented review work.'
        targetPaths = @('AGENTS.md')
    })
    targetArtifacts = @()
    recommendedBatches = @()
    manualValidationRequired = $true
} | ConvertTo-Json -Depth 20 | Set-Content -Path $agentOnlyPlanPath -Encoding UTF8
& (Join-Path $roleRepo 'tools/rule-harness/run-recurrence-work.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -PlanPath $agentOnlyPlanPath `
    -OutputDir (Join-Path $roleRepo 'Temp/AgentOnlyRecurrenceWork') `
    -DryRun | Out-Null
$agentOnlyRecurrenceWork = Get-Content -Path (Join-Path $roleRepo 'Temp/AgentOnlyRecurrenceWork/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition ([bool]$agentOnlyRecurrenceWork.failed -and @($agentOnlyRecurrenceWork.agentWorkQueue).Count -eq 1 -and @($agentOnlyRecurrenceWork.agentWorkReports | Where-Object { $_.status -eq 'blocked' -and $_.blockedReason -eq 'agent-runner-disabled' }).Count -eq 1 -and @($agentOnlyRecurrenceWork.skippedBatches | Where-Object { $_.kind -eq 'agent_work' -and $_.reasonCode -eq 'agent-runner-disabled' }).Count -eq 1) `
    -Message 'Expected recurrence work role to report agent-work blocked state instead of silently skipping plans without deterministic batches.'

& (Join-Path $roleRepo 'tools/rule-harness/run-harness-pipeline.ps1') `
    -RepoRoot $roleRepo `
    -ConfigPath $roleConfigPath `
    -OutputRoot (Join-Path $roleRepo 'Temp/RuleHarnessRoles') `
    -DryRun | Out-Null
$rolePipelineRoot = Get-Content -Path (Join-Path $roleRepo 'Temp/RuleHarnessRoles/latest-pipeline.txt') -Raw
$rolePipelineRoot = $rolePipelineRoot.Trim()
Assert-RuleHarness `
    -Condition ((Test-Path -LiteralPath (Join-Path $rolePipelineRoot '01-tech-debt-review/report.json')) -and (Test-Path -LiteralPath (Join-Path $rolePipelineRoot '04-recurrence-work/report.json'))) `
    -Message 'Expected role harness pipeline to write stage artifacts from 01 through 04.'
$pipelineRecurrenceWork = Get-Content -Path (Join-Path $rolePipelineRoot '04-recurrence-work/report.json') -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition ($pipelineRecurrenceWork.PSObject.Properties.Name -contains 'agentWorkQueue') `
    -Message 'Expected role harness pipeline to invoke recurrence work role even when the plan has no deterministic recommended batches.'

Write-Host 'Rule harness fixture tests passed.'
