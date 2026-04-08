Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Import-Module (Join-Path $repoRoot 'tools/rule-harness/RuleHarness.psm1') -Force
$config = Get-RuleHarnessConfig -ConfigPath (Join-Path $repoRoot 'tools/rule-harness/config.json')
$fixturesRoot = Join-Path $PSScriptRoot 'fixtures'
$scratchRoot = Join-Path $repoRoot 'Temp/RuleHarnessFixtureTests'
$testArchitectureOwnerDoc = 'docs/rules/architecture-rules.md'
$testGovernanceOwnerDoc = 'docs/governance/rule-governance.md'

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

$applyAllowedRoot = Join-Path $scratchRoot 'apply-allowed'
Copy-Item -LiteralPath (Join-Path $fixturesRoot 'apply-allowed') -Destination $applyAllowedRoot -Recurse -Force
$applyAllowedResult = Invoke-RuleHarnessDocEdits `
    -RepoRoot $applyAllowedRoot `
    -Config $config `
    -Edits @(
        [pscustomobject]@{
            targetPath = 'docs/rules/feature-rules.md'
            searchText = 'legacy-rule.md'
            replaceText = 'current-rule.md'
            reason = 'Fix moved rule doc path.'
        }
    )
Assert-RuleHarness `
    -Condition ($applyAllowedResult.edits[0].status -eq 'applied') `
    -Message 'Expected CLAUDE-referenced rule doc edit to apply.'
Assert-RuleHarness `
    -Condition ((Get-Content -Path (Join-Path $applyAllowedRoot 'docs/rules/feature-rules.md') -Raw) -match 'current-rule\.md') `
    -Message 'Expected rule doc content to be updated.'

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

$dynamicGlobalDocRoot = Join-Path $scratchRoot 'dynamic-global-doc'
New-Item -ItemType Directory -Path (Join-Path $dynamicGlobalDocRoot 'docs/ops') -Force | Out-Null
Set-Content -Path (Join-Path $dynamicGlobalDocRoot 'CLAUDE.md') -Value 'See `/docs/ops/firebase_hosting.md`.' -Encoding UTF8
Set-Content -Path (Join-Path $dynamicGlobalDocRoot 'docs/ops/firebase_hosting.md') -Value '# Ops' -Encoding UTF8
$dynamicGlobalDocResult = Invoke-RuleHarnessDocEdits `
    -RepoRoot $dynamicGlobalDocRoot `
    -Config $config `
    -Edits @(
        [pscustomobject]@{
            targetPath = 'docs/ops/firebase_hosting.md'
            searchText = '# Ops'
            replaceText = '# Ops Guide'
            reason = 'Verify CLAUDE-referenced global docs are allowlisted dynamically.'
        }
    )
Assert-RuleHarness `
    -Condition ($dynamicGlobalDocResult.edits[0].status -eq 'applied') `
    -Message 'Expected CLAUDE-referenced global doc edit to apply without a static path mapping.'

$movedArchitectureRoot = Join-Path $scratchRoot 'moved-architecture-owner'
Copy-Item -LiteralPath (Join-Path $fixturesRoot 'unity-in-application') -Destination $movedArchitectureRoot -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $movedArchitectureRoot 'docs/rules') -Force | Out-Null
Set-Content -Path (Join-Path $movedArchitectureRoot 'CLAUDE.md') -Value 'Read `/docs/rules/architecture-rules.md` for layer boundaries.' -Encoding UTF8
Set-Content -Path (Join-Path $movedArchitectureRoot 'docs/rules/architecture-rules.md') -Value '# Architecture Rules' -Encoding UTF8
$movedArchitectureFindings = Get-RuleHarnessStaticFindings -RepoRoot $movedArchitectureRoot -Config $config
Assert-RuleHarness `
    -Condition (@($movedArchitectureFindings | Where-Object { $_.title -eq 'Unity API used in Application' -and $_.ownerDoc -eq 'docs/rules/architecture-rules.md' }).Count -ge 1) `
    -Message 'Expected architecture violations to resolve ownerDoc from the current CLAUDE entrypoint, not a hardcoded legacy path.'

$reviewedFindings = ConvertTo-RuleHarnessReviewedFindings -Findings @(
    [pscustomobject]@{
        findingType = 'broken_reference'
        severity = 'medium'
        ownerDoc = 'docs/rules/feature-rules.md'
        title = 'Broken markdown reference'
        message = 'Rule doc still points to a moved file.'
        confidence = 'high'
        source = 'agent_review'
        evidence = @()
    },
    [pscustomobject]@{
        findingType = 'missing_rule'
        severity = 'high'
        ownerDoc = $testArchitectureOwnerDoc
        title = 'Missing feature bootstrap root'
        message = "Feature 'Bar' has no root-level *Setup.cs or *Bootstrap.cs file."
        confidence = 'high'
        source = 'agent_review'
        evidence = @([pscustomobject]@{ path = 'Assets/Scripts/Features/Bar'; line = $null; snippet = 'Expected root-level Setup/Bootstrap file' })
    }
)
$plannedBatches = Get-RuleHarnessPlannedBatches `
    -ReviewedFindings $reviewedFindings `
    -DocEdits @(
        [pscustomobject]@{
            targetPath = 'docs/rules/feature-rules.md'
            searchText = 'legacy-rule.md'
            replaceText = 'current-rule.md'
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
    Add-Content -Path 'docs/rules/feature-rules.md' -Value "`nDirty change"
}
finally {
    Pop-Location
}
$dirtyTargets = Get-RuleHarnessDirtyTargetPaths `
    -RepoRoot $dirtyRepoRoot `
    -TargetFiles @('docs/rules/feature-rules.md')
Assert-RuleHarness `
    -Condition ('docs/rules/feature-rules.md' -in $dirtyTargets) `
    -Message 'Expected dirty target detection to return modified rule doc.'

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
        [switch]$IncludeBarService,
        [switch]$SkipRunnerScript,
        [switch]$IncludeFeatureTestAsset
    )

    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'Assets/Scripts/Features/Bar') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'docs/rules') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'tools/rule-harness/tests') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'Tests/Bar') -Force | Out-Null

    Set-Content -Path (Join-Path $RepoPath 'CLAUDE.md') -Value 'Read `/docs/rules/architecture-rules.md`.' -Encoding UTF8
    Set-Content -Path (Join-Path $RepoPath 'docs/rules/architecture-rules.md') -Value '# Architecture Rules' -Encoding UTF8
    Set-Content -Path (Join-Path $RepoPath 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1') -Value 'Write-Host ''fixture harness tests passed''' -Encoding UTF8
    if ($IncludeBarService) {
        Set-Content -Path (Join-Path $RepoPath 'Assets/Scripts/Features/Bar/BarService.cs') -Value 'namespace Features.Bar { public sealed class BarService { } }' -Encoding UTF8
    }

    if (-not $SkipRunnerScript) {
        $targetedTestContent = if ($FailTargetedTests) {
            'throw ''targeted validation failed'''
        }
        else {
            'Write-Host ''targeted validation passed'''
        }
        Set-Content -Path (Join-Path $RepoPath 'Tests/Bar/Run-BarValidation.ps1') -Value $targetedTestContent -Encoding UTF8
    }
    if ($IncludeFeatureTestAsset) {
        New-Item -ItemType Directory -Path (Join-Path $RepoPath 'Tests/Bar/Domain') -Force | Out-Null
        Set-Content -Path (Join-Path $RepoPath 'Tests/Bar/Domain/BarDomainTests.cs') -Value 'namespace Tests.Bar.Domain { public sealed class BarDomainTests { } }' -Encoding UTF8
    }

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

function Initialize-RuleHarnessFixtureRepo {
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        [Parameter(Mandatory)]
        [string]$FixtureName
    )

    Copy-Item -LiteralPath (Join-Path $fixturesRoot $FixtureName) -Destination $RepoPath -Recurse -Force
    New-Item -ItemType Directory -Path (Join-Path $RepoPath 'tools') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot 'tools/rule-harness') -Destination (Join-Path $RepoPath 'tools/rule-harness') -Recurse -Force

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
        evidence = @([pscustomobject]@{ path = 'Assets/Scripts/Features/Bar'; line = $null; snippet = 'Expected root-level Setup/Bootstrap file' })
        remediationKind = 'code_fix'
    }
}

$missingRegistryRepo = Join-Path $scratchRoot 'mutation-missing-registry'
Initialize-RuleHarnessMutationRepo -RepoPath $missingRegistryRepo
$missingRegistryConfig = New-RuleHarnessTestConfig -SourceConfig $config
$missingRegistryFinding = New-RuleHarnessBootstrapFinding -OwnerDoc $testArchitectureOwnerDoc
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
    -Condition (-not $missingRegistryResult1.failed -and @($missingRegistryResult1.appliedBatches).Count -eq 1) `
    -Message 'Expected code batch without registry entry to proceed through the repair loop.'
Assert-RuleHarness `
    -Condition (@($missingRegistryResult1.discoveredValidationPlan | Where-Object { $_.source -eq 'feature_runner' -and $_.runnable }).Count -eq 1) `
    -Message 'Expected feature-local runner discovery even without a registry entry.'
Assert-RuleHarness `
    -Condition (Test-Path -LiteralPath (Join-Path $missingRegistryRepo 'Assets/Scripts/Features/Bar/BarSetup.cs')) `
    -Message 'Expected discovered validation plan to allow the scaffold batch to apply.'
Assert-RuleHarness `
    -Condition ($missingRegistryBatches[0].riskScore -eq 30 -and $missingRegistryBatches[0].riskLabel -eq 'medium' -and $missingRegistryBatches[0].ownershipStatus -eq 'accepted') `
    -Message 'Expected scaffold batch metadata to include risk score and accepted ownership.'
Assert-RuleHarness `
    -Condition (@($missingRegistryResult1.validationResults | Where-Object { $_.validation -eq 'targeted_tests' -and $_.status -eq 'passed' }).Count -ge 1) `
    -Message 'Expected auto-discovered feature runner to execute.'
$missingRegistryHistory = Read-RuleHarnessHistoryState -RepoRoot $missingRegistryRepo -Config $missingRegistryConfig
Assert-RuleHarness `
    -Condition (@($missingRegistryHistory.entries.Keys).Count -ge 1) `
    -Message 'Expected mutation history to persist batch state after apply.'

$fallbackValidationRepo = Join-Path $scratchRoot 'mutation-fallback-validation'
Initialize-RuleHarnessMutationRepo -RepoPath $fallbackValidationRepo -SkipRunnerScript -IncludeFeatureTestAsset
$fallbackValidationConfig = New-RuleHarnessTestConfig -SourceConfig $config
$fallbackValidationFinding = New-RuleHarnessBootstrapFinding -OwnerDoc $testArchitectureOwnerDoc
$fallbackValidationBatches = Get-RuleHarnessPlannedBatches `
    -ReviewedFindings @(ConvertTo-RuleHarnessReviewedFindings -Findings @($fallbackValidationFinding)) `
    -DocEdits @() `
    -RepoRoot $fallbackValidationRepo
$fallbackValidationResult = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $fallbackValidationBatches `
    -InitialStaticFindings @($fallbackValidationFinding) `
    -RepoRoot $fallbackValidationRepo `
    -Config $fallbackValidationConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition (-not $fallbackValidationResult.failed -and @($fallbackValidationResult.appliedBatches).Count -eq 1) `
    -Message 'Expected a batch with no runner script to fall back to inferred validation instead of skipping.'
Assert-RuleHarness `
    -Condition (@($fallbackValidationResult.discoveredValidationPlan | Where-Object { $_.source -eq 'feature_test_assets' -and $_.confidence -eq 'medium' }).Count -eq 1) `
    -Message 'Expected discovered validation plan to downgrade confidence when only feature test assets are available.'
Assert-RuleHarness `
    -Condition (@($fallbackValidationResult.validationResults | Where-Object { $_.validation -eq 'targeted_tests' -and $_.status -eq 'skipped' }).Count -ge 1) `
    -Message 'Expected fallback validation to skip runnable targeted tests and rely on inferred checks.'

$failingValidationRepo = Join-Path $scratchRoot 'mutation-failing-validation'
Initialize-RuleHarnessMutationRepo -RepoPath $failingValidationRepo -IncludeRegistry -FailTargetedTests
$failingValidationConfig = New-RuleHarnessTestConfig -SourceConfig $config
$failingValidationFinding = New-RuleHarnessBootstrapFinding -OwnerDoc $testArchitectureOwnerDoc
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
Assert-RuleHarness `
    -Condition ($failingValidationResult1.failed -and [int]$failingValidationResult1.retryAttempts -eq 1) `
    -Message 'Expected one same-run reflective retry after the first failed validation attempt.'
Assert-RuleHarness `
    -Condition (@($failingValidationResult1.learningTrace | Where-Object { $_.batchId -eq $failingValidationBatches[0].id }).Count -eq 2) `
    -Message 'Expected the failing batch to record exactly two learning-trace attempts.'
Assert-RuleHarness `
    -Condition (@($failingValidationResult1.memoryUpdates).Count -ge 1) `
    -Message 'Expected repeated validation failure to create an advisory memory update.'
Assert-RuleHarness `
    -Condition (-not (Test-Path -LiteralPath (Join-Path $failingValidationRepo 'Assets/Scripts/Features/Bar/BarSetup.cs'))) `
    -Message 'Expected repeated failed code batch to leave no scaffold file behind.'
Push-Location $failingValidationRepo
try {
    git commit --allow-empty -m 'advance-commit-2' | Out-Null
}
finally {
    Pop-Location
}
$failingValidationResult2 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $failingValidationBatches `
    -InitialStaticFindings @($failingValidationFinding) `
    -RepoRoot $failingValidationRepo `
    -Config $failingValidationConfig `
    -MutationState $mutationState
Push-Location $failingValidationRepo
try {
    git commit --allow-empty -m 'advance-commit-3' | Out-Null
}
finally {
    Pop-Location
}
$failingValidationResult3 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches $failingValidationBatches `
    -InitialStaticFindings @($failingValidationFinding) `
    -RepoRoot $failingValidationRepo `
    -Config $failingValidationConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition (@($failingValidationResult3.promotionCandidates | Where-Object { $_.targetDoc -eq $testArchitectureOwnerDoc }).Count -ge 1) `
    -Message 'Expected recurring feature-local failures to propose promotion to the architecture rule doc.'

$ownershipRejectRepo = Join-Path $scratchRoot 'mutation-ownership-reject'
Initialize-RuleHarnessMutationRepo -RepoPath $ownershipRejectRepo -IncludeRegistry
$ownershipRejectConfig = New-RuleHarnessTestConfig -SourceConfig $config
$ownershipRejectFinding = New-RuleHarnessBootstrapFinding -OwnerDoc $testGovernanceOwnerDoc
$ownershipRejectBatch = [pscustomobject]@{
    id = 'batch-ownership'
    kind = 'code_fix'
    targetFiles = @('Assets/Scripts/Features/Bar/BarSetup.cs')
    reason = 'Add missing root setup scaffold for ownership test.'
    validation = @('rule_harness_tests', 'targeted_tests', 'static_scan')
    expectedFindingsResolved = @("missing_rule|$testArchitectureOwnerDoc|Missing feature bootstrap root")
    status = 'planned'
    featureNames = @('Bar')
    ownerDocs = @($testGovernanceOwnerDoc)
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
    -Message 'Expected feature-owned scaffold batch with a non-architecture global owner doc to fail ownership preflight.'

$riskThresholdRepo = Join-Path $scratchRoot 'mutation-risk-threshold'
Initialize-RuleHarnessMutationRepo -RepoPath $riskThresholdRepo -IncludeRegistry -IncludeBarService
$riskThresholdConfig = New-RuleHarnessTestConfig -SourceConfig $config
$riskThresholdBatch = [pscustomobject]@{
    id = 'batch-risk'
    kind = 'code_fix'
    targetFiles = @('Assets/Scripts/Features/Bar/BarService.cs')
    reason = 'Modify existing code file.'
    validation = @('rule_harness_tests', 'targeted_tests', 'static_scan')
    expectedFindingsResolved = @("missing_rule|$testArchitectureOwnerDoc|Synthetic")
    status = 'planned'
    featureNames = @('Bar')
    ownerDocs = @($testArchitectureOwnerDoc)
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

$globalPromotionRepo = Join-Path $scratchRoot 'mutation-global-promotion'
New-Item -ItemType Directory -Path (Join-Path $globalPromotionRepo 'docs/rules') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $globalPromotionRepo 'docs/governance') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $globalPromotionRepo 'tools/rule-harness/tests') -Force | Out-Null
Set-Content -Path (Join-Path $globalPromotionRepo 'CLAUDE.md') -Value @'
# Fixture

See `/docs/rules/architecture-rules.md`.
See `/docs/governance/rule-governance.md`.
'@ -Encoding UTF8
Set-Content -Path (Join-Path $globalPromotionRepo $testArchitectureOwnerDoc) -Value '# Architecture' -Encoding UTF8
Set-Content -Path (Join-Path $globalPromotionRepo $testGovernanceOwnerDoc) -Value '# Work Principles' -Encoding UTF8
Set-Content -Path (Join-Path $globalPromotionRepo 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1') -Value 'throw ''fixture harness tests failed''' -Encoding UTF8
Push-Location $globalPromotionRepo
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
$globalPromotionConfig = New-RuleHarnessTestConfig -SourceConfig $config
$globalPromotionConfig.mutation.requireCleanTargets = $false
$globalPromotionBatch = [pscustomobject]@{
    id = 'batch-global-promotion'
    kind = 'rule_fix'
    targetFiles = @($testArchitectureOwnerDoc)
    reason = 'Synthetic failing global rule batch.'
    validation = @('rule_harness_tests', 'static_scan')
    expectedFindingsResolved = @("doc_drift|$testArchitectureOwnerDoc|Synthetic global failure")
    status = 'planned'
    featureNames = @()
    ownerDocs = @($testArchitectureOwnerDoc)
    sourceFindingTypes = @('doc_drift')
    fingerprint = $null
    riskScore = $null
    riskLabel = $null
    ownershipStatus = 'pending'
    operations = @(
        [pscustomobject]@{
            type = 'write_file'
            targetPath = $testArchitectureOwnerDoc
            content = '# Architecture'
        }
    )
}
$globalPromotionFinding = [pscustomobject]@{
    findingType = 'doc_drift'
    severity = 'high'
    ownerDoc = $testArchitectureOwnerDoc
    title = 'Synthetic global failure'
    message = 'Synthetic global failure'
    confidence = 'high'
    source = 'agent_review'
    evidence = @()
    remediationKind = 'rule_fix'
}
$globalPromotionResult1 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($globalPromotionBatch) `
    -InitialStaticFindings @($globalPromotionFinding) `
    -RepoRoot $globalPromotionRepo `
    -Config $globalPromotionConfig `
    -MutationState $mutationState
Push-Location $globalPromotionRepo
try {
    git commit --allow-empty -m 'advance-global-2' | Out-Null
}
finally {
    Pop-Location
}
$globalPromotionResult2 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($globalPromotionBatch) `
    -InitialStaticFindings @($globalPromotionFinding) `
    -RepoRoot $globalPromotionRepo `
    -Config $globalPromotionConfig `
    -MutationState $mutationState
Push-Location $globalPromotionRepo
try {
    git commit --allow-empty -m 'advance-global-3' | Out-Null
}
finally {
    Pop-Location
}
$globalPromotionResult3 = Invoke-RuleHarnessMutationPlan `
    -PlannedBatches @($globalPromotionBatch) `
    -InitialStaticFindings @($globalPromotionFinding) `
    -RepoRoot $globalPromotionRepo `
    -Config $globalPromotionConfig `
    -MutationState $mutationState
Assert-RuleHarness `
    -Condition (@($globalPromotionResult3.promotionCandidates | Where-Object { $_.targetDoc -eq $testGovernanceOwnerDoc }).Count -ge 1) `
    -Message 'Expected recurring cross-feature/global issues to propose promotion to the correct global agent doc.'

$feedbackRepo = Join-Path $scratchRoot 'feedback-loop'
Initialize-RuleHarnessFixtureRepo -RepoPath $feedbackRepo -FixtureName 'missing-ssot'
New-Item -ItemType Directory -Path (Join-Path $feedbackRepo 'Temp') -Force | Out-Null
$feedbackSummaryPath = Join-Path $feedbackRepo 'Temp/feedback-summary.md'
$feedbackReport = Invoke-RuleHarness `
    -RepoRoot $feedbackRepo `
    -ConfigPath (Join-Path $feedbackRepo 'tools/rule-harness/config.json') `
    -DisableLlm `
    -DryRun `
    -SummaryPath $feedbackSummaryPath `
    -ReportPathHint 'Temp/RuleHarness/rule-harness-report.json'
Assert-RuleHarness `
    -Condition (@($feedbackReport.stageResults | Where-Object { $_.stage -eq 'diagnose' -and $_.status -eq 'skipped' }).Count -eq 1) `
    -Message 'Expected static-only run to mark diagnose stage as skipped.'
Assert-RuleHarness `
    -Condition (@($feedbackReport.actionItems | Where-Object { $_.kind -eq 'update-owner-doc' }).Count -ge 1) `
    -Message 'Expected finding-derived action item for owner doc update.'
Assert-RuleHarness `
    -Condition ((Get-Content -Path $feedbackSummaryPath -Raw) -match '### Stage Status' -and (Get-Content -Path $feedbackSummaryPath -Raw) -match '### Next Actions') `
    -Message 'Expected summary to include stage and next action sections.'
Assert-RuleHarness `
    -Condition ($feedbackReport.PSObject.Properties.Name -contains 'discoveredValidationPlan' -and $feedbackReport.PSObject.Properties.Name -contains 'retryAttempts') `
    -Message 'Expected report to expose self-improvement loop output fields.'

$llmFailureRepo = Join-Path $scratchRoot 'feedback-loop-llm-failure'
Initialize-RuleHarnessFixtureRepo -RepoPath $llmFailureRepo -FixtureName 'missing-ssot'
New-Item -ItemType Directory -Path (Join-Path $llmFailureRepo 'Temp') -Force | Out-Null
$llmFailureSummaryPath = Join-Path $llmFailureRepo 'Temp/feedback-summary.md'
$llmFailureReport = Invoke-RuleHarness `
    -RepoRoot $llmFailureRepo `
    -ConfigPath (Join-Path $llmFailureRepo 'tools/rule-harness/config.json') `
    -ApiKey 'fixture-key' `
    -ApiBaseUrl 'http://127.0.0.1:9' `
    -Model 'glm-5' `
    -DryRun `
    -SummaryPath $llmFailureSummaryPath `
    -ReportPathHint 'Temp/RuleHarness/rule-harness-report.json' `
    -LogPathHint 'Temp/RuleHarness/rule-harness.log'
Assert-RuleHarness `
    -Condition (@($llmFailureReport.stageResults | Where-Object { $_.stage -eq 'diagnose' -and $_.status -eq 'failed' }).Count -eq 1) `
    -Message 'Expected LLM failure run to mark diagnose stage as failed.'
Assert-RuleHarness `
    -Condition (@($llmFailureReport.stageResults | Where-Object { $_.stage -eq 'doc_sync' -and $_.status -eq 'failed' }).Count -eq 1) `
    -Message 'Expected LLM failure run to mark doc sync stage as failed.'
Assert-RuleHarness `
    -Condition (@($llmFailureReport.actionItems | Where-Object { $_.kind -eq 'check-llm-connectivity' -and $_.details -match 'Temp/RuleHarness/rule-harness-report.json' -and $_.details -match 'Temp/RuleHarness/rule-harness.log' }).Count -ge 1) `
    -Message 'Expected LLM failure action item to include report and log path hints.'
Assert-RuleHarness `
    -Condition (@($llmFailureReport.memoryUpdates | Where-Object { $_.scopePath -eq 'tools/rule-harness/README.md' }).Count -ge 1) `
    -Message 'Expected harness-originated failures to create advisory memory updates without becoming SSOT.'

$scheduledRepo = Join-Path $scratchRoot 'scheduled-status'
Initialize-RuleHarnessFixtureRepo -RepoPath $scheduledRepo -FixtureName 'missing-ssot'
Push-Location $scheduledRepo
try {
    & (Join-Path $scheduledRepo 'tools/rule-harness/run-rule-harness-scheduled.ps1') -DisableLlm -MutationMode 'code_and_rules'
}
finally {
    Pop-Location
}
$latestStatusPath = Join-Path $scheduledRepo 'Temp/RuleHarnessScheduled/latest-status.json'
Assert-RuleHarness `
    -Condition (Test-Path -LiteralPath $latestStatusPath) `
    -Message 'Expected scheduled wrapper to write latest-status.json.'
$latestStatus = Get-Content -Path $latestStatusPath -Raw | ConvertFrom-Json
Assert-RuleHarness `
    -Condition (-not [string]::IsNullOrWhiteSpace([string]$latestStatus.runDir) -and -not [string]::IsNullOrWhiteSpace([string]$latestStatus.reportPath) -and -not [string]::IsNullOrWhiteSpace([string]$latestStatus.logPath)) `
    -Message 'Expected latest-status.json to include core artifact paths.'
Assert-RuleHarness `
    -Condition (-not [bool]$latestStatus.llmEnabled) `
    -Message 'Expected scheduled latest status to record static-only execution when DisableLlm is used.'
Assert-RuleHarness `
    -Condition ($latestStatus.PSObject.Properties.Name -contains 'retryCount' -and $latestStatus.PSObject.Properties.Name -contains 'learnedAnything' -and $latestStatus.PSObject.Properties.Name -contains 'topPromotionCandidates') `
    -Message 'Expected scheduled latest status to include retry/promotion/learning feedback fields.'

Write-Host 'Rule harness fixture tests passed.'
