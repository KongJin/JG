param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

$changedFiles = @(Get-WorkflowChangedFiles -RepoRoot $RepoRoot)
$progressPath = Join-Path $RepoRoot "docs\plans\progress.md"
$progress = if (Test-Path -LiteralPath $progressPath) { Get-Content -LiteralPath $progressPath -Raw } else { "" }

$agentAChanges = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @(
    "^Assets/Scripts/Features/Player/",
    "^Assets/Scripts/Features/Enemy/",
    "^Assets/Scripts/Features/Wave/",
    "^Assets/Scripts/Features/Unit/(Domain|Application|Infrastructure|UnitSetup\.cs)"
))
$agentBChanges = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @(
    "^Assets/Scripts/Features/.+/Presentation/",
    "^Assets/Scripts/Shared/Ui/",
    "^tools/unity-mcp/",
    "^docs/playtest/runtime_validation_checklist\.md$"
))
$docsChanges = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^docs/", "^AGENTS\.md$", "^\.codex/skills/"))
$artifactChanges = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^artifacts/"))

$suggestions = New-Object System.Collections.Generic.List[object]

if ($agentAChanges.Count -gt 0 -and $agentBChanges.Count -gt 0) {
    $suggestions.Add([PSCustomObject]@{
        Priority = 1
        Lane = "review"
        Suggestion = "Run Agent A/B pass reviews and avoid broad runtime/presentation edits until owner mismatches are classified."
        Command = ".\tools\workflow\Invoke-AgentPassReview.ps1 -Agent B"
    })
}

if ($artifactChanges.Count -gt 0) {
    $suggestions.Add([PSCustomObject]@{
        Priority = 2
        Lane = "evidence"
        Suggestion = "Check whether generated artifacts are scoped to the current pass before using them as closeout evidence."
        Command = ".\tools\workflow\Test-GeneratedArtifactScope.ps1"
    })
}

if ($changedFiles.Count -gt 0) {
    $suggestions.Add([PSCustomObject]@{
        Priority = 3
        Lane = "validation"
        Suggestion = "Run a closeout pack selected from the current dirty files."
        Command = ".\tools\workflow\Invoke-CloseoutPack.ps1 -PlanOnly"
    })
}

if ($progress -match "Set B Garage" -or $docsChanges.Count -gt 0) {
    $suggestions.Add([PSCustomObject]@{
        Priority = 4
        Lane = "simplification"
        Suggestion = "Scan for the next low-risk simplification candidate before adding new plan text."
        Command = ".\tools\workflow\Find-SimplificationCandidates.ps1"
    })
}

if ($suggestions.Count -eq 0) {
    $suggestions.Add([PSCustomObject]@{
        Priority = 9
        Lane = "status"
        Suggestion = "No dirty-worktree-specific suggestion. Start from docs/plans/progress.md current focus."
        Command = "Get-Content docs\plans\progress.md"
    })
}

Write-WorkflowSection "Worktree Signals"
Write-Host ("changedFiles={0} agentA={1} agentB={2} docs={3} artifacts={4}" -f $changedFiles.Count, $agentAChanges.Count, $agentBChanges.Count, $docsChanges.Count, $artifactChanges.Count)

Write-WorkflowSection "Next Work Suggestions"
foreach ($item in @($suggestions | Sort-Object Priority)) {
    Write-Host ("[{0}] {1}" -f $item.Lane, $item.Suggestion)
    Write-Host ("    {0}" -f $item.Command) -ForegroundColor Gray
}
