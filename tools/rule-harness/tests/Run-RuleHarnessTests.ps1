Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Import-Module (Join-Path $repoRoot 'tools/rule-harness/RuleHarness.psm1') -Force
$config = Get-RuleHarnessConfig -ConfigPath (Join-Path $repoRoot 'tools/rule-harness/config.json')
$fixturesRoot = Join-Path $PSScriptRoot 'fixtures'
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

$missingSsotFindings = Get-RuleHarnessStaticFindings -RepoRoot (Join-Path $fixturesRoot 'missing-ssot') -Config $config
Assert-RuleHarness `
    -Condition (@($missingSsotFindings | Where-Object { $_.findingType -eq 'broken_reference' -and $_.severity -eq 'high' }).Count -ge 1) `
    -Message 'Expected high broken_reference finding for missing SSOT doc.'

$unityInAppFindings = Get-RuleHarnessStaticFindings -RepoRoot (Join-Path $fixturesRoot 'unity-in-application') -Config $config
Assert-RuleHarness `
    -Condition (@($unityInAppFindings | Where-Object { $_.findingType -eq 'code_violation' -and $_.title -eq 'Unity API used in Application' }).Count -ge 1) `
    -Message 'Expected Application layer Unity API violation.'

$customPropertyFindings = Get-RuleHarnessStaticFindings -RepoRoot (Join-Path $fixturesRoot 'undocumented-custom-property') -Config $config
Assert-RuleHarness `
    -Condition (@($customPropertyFindings | Where-Object { $_.findingType -eq 'missing_rule' -and $_.message -match 'newKey' }).Count -ge 1) `
    -Message 'Expected undocumented CustomProperties key finding.'
Assert-RuleHarness `
    -Condition (@($customPropertyFindings | Where-Object { $_.remediationKind -eq 'rule_fix' }).Count -ge 1) `
    -Message 'Expected undocumented CustomProperties key to default to rule_fix.'
Assert-RuleHarness `
    -Condition (@($customPropertyFindings | Where-Object { $_.ownerDoc -eq 'Assets/Scripts/Features/Foo/README.md' }).Count -ge 1) `
    -Message 'Expected undocumented CustomProperties key ownerDoc to point at the owning feature README.'

$applyAllowedRoot = Join-Path $scratchRoot 'apply-allowed'
Copy-Item -LiteralPath (Join-Path $fixturesRoot 'apply-allowed') -Destination $applyAllowedRoot -Recurse -Force
$applyAllowedResult = Invoke-RuleHarnessDocEdits `
    -RepoRoot $applyAllowedRoot `
    -Config $config `
    -Edits @(
        [pscustomobject]@{
            targetPath = 'Assets/Scripts/Features/Foo/README.md'
            searchText = '../../../../agent/old.md'
            replaceText = '../../../../agent/architecture.md'
            reason = 'Fix moved rule doc path.'
        }
    )
Assert-RuleHarness `
    -Condition ($applyAllowedResult.edits[0].status -eq 'applied') `
    -Message 'Expected allowlisted README doc edit to apply.'
Assert-RuleHarness `
    -Condition ((Get-Content -Path (Join-Path $applyAllowedRoot 'Assets/Scripts/Features/Foo/README.md') -Raw) -match 'architecture\.md') `
    -Message 'Expected README content to be updated.'

$applyRejectedRoot = Join-Path $scratchRoot 'apply-rejected'
Copy-Item -LiteralPath (Join-Path $fixturesRoot 'apply-rejected') -Destination $applyRejectedRoot -Recurse -Force
$applyRejectedResult = Invoke-RuleHarnessDocEdits `
    -RepoRoot $applyRejectedRoot `
    -Config $config `
    -Edits @(
        [pscustomobject]@{
            targetPath = 'docs/design/blocked.md'
            searchText = 'old'
            replaceText = 'new'
            reason = 'This should be rejected.'
        }
    )
Assert-RuleHarness `
    -Condition ($applyRejectedResult.edits[0].status -eq 'rejected') `
    -Message 'Expected docs/design edit to be rejected by allowlist.'

$reviewedFindings = ConvertTo-RuleHarnessReviewedFindings -Findings @(
    [pscustomobject]@{
        findingType = 'broken_reference'
        severity = 'medium'
        ownerDoc = 'Assets/Scripts/Features/Foo/README.md'
        title = 'Broken markdown reference'
        message = 'README still points to a moved file.'
        confidence = 'high'
        source = 'agent_review'
        evidence = @()
    },
    [pscustomobject]@{
        findingType = 'missing_rule'
        severity = 'high'
        ownerDoc = 'Assets/Scripts/Features/Bar/README.md'
        title = 'Missing feature bootstrap root'
        message = "Feature 'Bar' has no root-level *Setup.cs or *Bootstrap.cs file."
        confidence = 'high'
        source = 'agent_review'
        evidence = @()
    }
)
$plannedBatches = Get-RuleHarnessPlannedBatches `
    -ReviewedFindings $reviewedFindings `
    -DocEdits @(
        [pscustomobject]@{
            targetPath = 'Assets/Scripts/Features/Foo/README.md'
            searchText = '../../../../agent/old.md'
            replaceText = '../../../../agent/architecture.md'
            reason = 'Fix moved rule doc path.'
        }
    ) `
    -RepoRoot $applyAllowedRoot
Assert-RuleHarness `
    -Condition (@($plannedBatches | Where-Object kind -eq 'rule_fix').Count -eq 1) `
    -Message 'Expected one rule_fix batch for allowlisted doc edits.'
Assert-RuleHarness `
    -Condition (@($plannedBatches | Where-Object { $_.kind -eq 'code_fix' -and $_.targetFiles -contains 'Assets/Scripts/Features/Bar/BarSetup.cs' }).Count -eq 1) `
    -Message 'Expected one code_fix batch for missing bootstrap root.'

$dirtyRepoRoot = Join-Path $scratchRoot 'dirty-targets'
Copy-Item -LiteralPath (Join-Path $fixturesRoot 'apply-allowed') -Destination $dirtyRepoRoot -Recurse -Force
Push-Location $dirtyRepoRoot
try {
    git init | Out-Null
    git config user.name 'rule-harness-tests'
    git config user.email 'rule-harness-tests@example.com'
    git add .
    git commit -m 'init' | Out-Null
    Add-Content -Path 'Assets/Scripts/Features/Foo/README.md' -Value "`nDirty change"
}
finally {
    Pop-Location
}
$dirtyTargets = Get-RuleHarnessDirtyTargetPaths `
    -RepoRoot $dirtyRepoRoot `
    -TargetFiles @('Assets/Scripts/Features/Foo/README.md')
Assert-RuleHarness `
    -Condition ('Assets/Scripts/Features/Foo/README.md' -in $dirtyTargets) `
    -Message 'Expected dirty target detection to return modified README.'

$mutationState = Get-RuleHarnessMutationState -Config $config -MutationMode 'code_and_rules' -EnableMutation
Assert-RuleHarness `
    -Condition ($mutationState.enabled -and $mutationState.mode -eq 'code_and_rules') `
    -Message 'Expected mutation state to honor explicit code_and_rules mode.'

function New-RuleHarnessTestConfig {
    param(
        [Parameter(Mandatory)]
        [object]$SourceConfig
    )

    $clone = ($SourceConfig | ConvertTo-Json -Depth 30) | ConvertFrom-Json
    $clone.validation.harnessTestScript = 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1'
    $clone.validation.registryPath = 'tools/rule-harness/validation-registry.json'
    $clone.history.statePath = 'Temp/RuleHarnessState/history.json'
    $clone
}

function Initialize-RuleHarnessMutationRepo {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [switch]$IncludeRegistry,
        [switch]$FailTargetedTests,
        [switch]$IncludeBarService
    )

    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'Assets/Scripts/Features/Bar') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'tools/rule-harness/tests') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'Tests/Bar') -Force | Out-Null

    Set-Content -Path (Join-Path $RepoPath 'CLAUDE.md') -Value '# Fixture' -Encoding UTF8
    Set-Content -Path (Join-Path $RepoPath 'Assets/Scripts/Features/Bar/README.md') -Value '# Bar' -Encoding UTF8
    Set-Content -Path (Join-Path $RepoPath 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1') -Value 'Write-Host ''fixture harness tests passed''' -Encoding UTF8
    if ($IncludeBarService) {
        Set-Content -Path (Join-Path $RepoPath 'Assets/Scripts/Features/Bar/BarService.cs') -Value 'namespace Features.Bar { public sealed class BarService { } }' -Encoding UTF8
    }

    $targetedTestContent = if ($FailTargetedTests) {
        'throw ''targeted validation failed'''
    }
    else {
        'Write-Host ''targeted validation passed'''
    }
    Set-Content -Path (Join-Path $RepoPath 'Tests/Bar/Run-BarValidation.ps1') -Value $targetedTestContent -Encoding UTF8

    if ($IncludeRegistry) {
        New-Item -ItemType Directory -Path (Join-Path $RepoPath 'tools/rule-harness') -Force | Out-Null
        $registry = @'
{
  "$schema": "./validation-registry.schema.json",
  "schemaVersion": 1,
  "features": {
    "Bar": {
      "scripts": [
        "Tests/Bar/Run-BarValidation.ps1"
      ],
      "smoke": [],
      "requiredForKinds": [
        "code_fix",
        "mixed_fix"
      ]
    }
  }
}
'@
        Set-Content -Path (Join-Path $RepoPath 'tools/rule-harness/validation-registry.json') -Value $registry -Encoding UTF8
    }

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

function New-RuleHarnessBootstrapFinding {
    param(
        [Parameter(Mandatory)]
        [string]$OwnerDoc
    )

    [pscustomobject]@{
        findingType = 'missing_rule'
        severity = 'high'
        ownerDoc = $OwnerDoc
        title = 'Missing feature bootstrap root'
        message = "Feature 'Bar' has no root-level *Setup.cs or *Bootstrap.cs file."
        confidence = 'high'
        source = 'agent_review'
        evidence = @()
        remediationKind = 'code_fix'
    }
}

$missingRegistryRepo = Join-Path $scratchRoot 'mutation-missing-registry'
Initialize-RuleHarnessMutationRepo -RepoPath $missingRegistryRepo
$missingRegistryConfig = New-RuleHarnessTestConfig -SourceConfig $config
$missingRegistryFinding = New-RuleHarnessBootstrapFinding -OwnerDoc 'Assets/Scripts/Features/Bar/README.md'
$missingRegistryBatches = Get-RuleHarnessPlannedBatches `
    -ReviewedFindings @(ConvertTo-RuleHarnessReviewedFindings -Findings @($missingRegistryFinding)) `
    -DocEdits @() `
    -RepoRoot $missingRegistryRepo
$missingRegistryResult1 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $missingRegistryBatches `
    -InitialStaticFindings @($missingRegistryFinding) `
    -RepoRoot $missingRegistryRepo `
    -Config $missingRegistryConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition ($missingRegistryResult1.skippedBatches[0].reasonCode -eq 'missing-validation-registry') `
    -Message 'Expected code batch without registry entry to skip before apply.'
Assert-RuleHarness `
    -Condition (-not (Test-Path -LiteralPath (Join-Path $missingRegistryRepo 'Assets/Scripts/Features/Bar/BarSetup.cs'))) `
    -Message 'Expected missing-registry batch to leave no scaffold file behind.'
Assert-RuleHarness `
    -Condition ($missingRegistryBatches[0].riskScore -eq 30 -and $missingRegistryBatches[0].riskLabel -eq 'medium' -and $missingRegistryBatches[0].ownershipStatus -eq 'accepted') `
    -Message 'Expected scaffold batch metadata to include risk score and accepted ownership.'
$missingRegistryResult2 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $missingRegistryBatches `
    -InitialStaticFindings @($missingRegistryFinding) `
    -RepoRoot $missingRegistryRepo `
    -Config $missingRegistryConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition ($missingRegistryResult2.skippedBatches[0].attemptCount -ge 2) `
    -Message 'Expected repeated missing-registry batch to record repeated history attempts.'
$missingRegistryHistory = Read-RuleHarnessHistoryState -RepoRoot $missingRegistryRepo -Config $missingRegistryConfig
Assert-RuleHarness `
    -Condition (@($missingRegistryHistory.entries.Keys).Count -ge 1) `
    -Message 'Expected mutation history to persist skipped batch state.'

$failingValidationRepo = Join-Path $scratchRoot 'mutation-failing-validation'
Initialize-RuleHarnessMutationRepo -RepoPath $failingValidationRepo -IncludeRegistry -FailTargetedTests
$failingValidationConfig = New-RuleHarnessTestConfig -SourceConfig $config
$failingValidationFinding = New-RuleHarnessBootstrapFinding -OwnerDoc 'Assets/Scripts/Features/Bar/README.md'
$failingValidationBatches = Get-RuleHarnessPlannedBatches `
    -ReviewedFindings @(ConvertTo-RuleHarnessReviewedFindings -Findings @($failingValidationFinding)) `
    -DocEdits @() `
    -RepoRoot $failingValidationRepo
$failingValidationResult1 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $failingValidationBatches `
    -InitialStaticFindings @($failingValidationFinding) `
    -RepoRoot $failingValidationRepo `
    -Config $failingValidationConfig `
    -MutationState $mutationState
$failingValidationResult2 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $failingValidationBatches `
    -InitialStaticFindings @($failingValidationFinding) `
    -RepoRoot $failingValidationRepo `
    -Config $failingValidationConfig `
    -MutationState $mutationState
$failingValidationResult3 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $failingValidationBatches `
    -InitialStaticFindings @($failingValidationFinding) `
    -RepoRoot $failingValidationRepo `
    -Config $failingValidationConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition ($failingValidationResult1.failed -and $failingValidationResult2.failed) `
    -Message 'Expected targeted test failure to fail the first two attempts.'
Assert-RuleHarness `
    -Condition ($failingValidationResult3.skippedBatches[0].reasonCode -eq 'max-attempts-reached') `
    -Message 'Expected the third repeated targeted test failure to be suppressed by max-attempts.'
Assert-RuleHarness `
    -Condition (-not (Test-Path -LiteralPath (Join-Path $failingValidationRepo 'Assets/Scripts/Features/Bar/BarSetup.cs'))) `
    -Message 'Expected repeated failed code batch to leave no scaffold file behind.'

$ownershipRejectRepo = Join-Path $scratchRoot 'mutation-ownership-reject'
Initialize-RuleHarnessMutationRepo -RepoPath $ownershipRejectRepo -IncludeRegistry
$ownershipRejectConfig = New-RuleHarnessTestConfig -SourceConfig $config
$ownershipRejectFinding = New-RuleHarnessBootstrapFinding -OwnerDoc 'agent/architecture.md'
$ownershipRejectBatch = [pscustomobject]@{
    id = 'batch-ownership'
    kind = 'code_fix'
    targetFiles = @('Assets/Scripts/Features/Bar/BarSetup.cs')
    reason = 'Add missing root setup scaffold for ownership test.'
    validation = @('rule_harness_tests', 'targeted_tests', 'static_scan')
    expectedFindingsResolved = @('missing_rule|agent/architecture.md|Missing feature bootstrap root')
    status = 'planned'
    featureNames = @('Bar')
    ownerDocs = @('agent/architecture.md')
    sourceFindingTypes = @('missing_rule')
    fingerprint = $null
    riskScore = $null
    riskLabel = $null
    ownershipStatus = 'pending'
    operations = @(
        [pscustomobject]@{
            type = 'write_file'
            targetPath = 'Assets/Scripts/Features/Bar/BarSetup.cs'
            content = 'namespace Features.Bar { public sealed class BarSetup { } }'
        }
    )
}
$ownershipRejectResult = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($ownershipRejectBatch) `
    -InitialStaticFindings @($ownershipRejectFinding) `
    -RepoRoot $ownershipRejectRepo `
    -Config $ownershipRejectConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition ($ownershipRejectResult.skippedBatches[0].reasonCode -eq 'ownership-preflight-rejected') `
    -Message 'Expected feature-owned scaffold batch with global owner doc to fail ownership preflight.'

$riskThresholdRepo = Join-Path $scratchRoot 'mutation-risk-threshold'
Initialize-RuleHarnessMutationRepo -RepoPath $riskThresholdRepo -IncludeRegistry -IncludeBarService
$riskThresholdConfig = New-RuleHarnessTestConfig -SourceConfig $config
$riskThresholdBatch = [pscustomobject]@{
    id = 'batch-risk'
    kind = 'code_fix'
    targetFiles = @('Assets/Scripts/Features/Bar/BarService.cs')
    reason = 'Modify existing code file.'
    validation = @('rule_harness_tests', 'targeted_tests', 'static_scan')
    expectedFindingsResolved = @('missing_rule|Assets/Scripts/Features/Bar/README.md|Synthetic')
    status = 'planned'
    featureNames = @('Bar')
    ownerDocs = @('Assets/Scripts/Features/Bar/README.md')
    sourceFindingTypes = @('missing_rule')
    fingerprint = $null
    riskScore = $null
    riskLabel = $null
    ownershipStatus = 'pending'
    operations = @(
        [pscustomobject]@{
            type = 'write_file'
            targetPath = 'Assets/Scripts/Features/Bar/BarService.cs'
            content = 'namespace Features.Bar { public sealed class BarService { public int Value => 1; } }'
        }
    )
}
$riskThresholdResult = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($riskThresholdBatch) `
    -InitialStaticFindings @($missingRegistryFinding) `
    -RepoRoot $riskThresholdRepo `
    -Config $riskThresholdConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition ($riskThresholdResult.skippedBatches[0].reasonCode -eq 'risk-threshold-exceeded') `
    -Message 'Expected existing code file edits above the threshold to skip before apply.'

Write-Host 'Rule harness fixture tests passed.'
