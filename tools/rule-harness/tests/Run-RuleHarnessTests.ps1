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

Write-Host 'Rule harness fixture tests passed.'
