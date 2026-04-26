param(
    [ValidateSet("A", "B", "C", "Phase5", "Any")]
    [string]$Agent = "Any",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [switch]$RunCloseoutPack,
    [switch]$StagedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

function Get-AgentScope {
    param([string]$Name)

    switch ($Name) {
        "A" {
            return [PSCustomObject]@{
                Plan = "docs/plans/game_scene_agent_a_runtime_core_plan.md"
                Patterns = @(
                    "^Assets/Scripts/Features/Player/",
                    "^Assets/Scripts/Features/Enemy/",
                    "^Assets/Scripts/Features/Wave/(Application|Domain|Infrastructure)/",
                    "^Assets/Scripts/Features/Unit/(Domain|Application|Infrastructure|UnitSetup\.cs|PlacementArea\.cs)",
                    "^Assets/Editor/DirectTests/",
                    "^artifacts/unity/(game-scene-agent-a|gamescene-runtime).*\.(json|log|png)$",
                    "^artifacts/unity/unity-ui-authoring-workflow-policy-a\.json$",
                    "^docs/plans/game_scene_agent_a_runtime_core_plan\.md$",
                    "^docs/plans/progress\.md$"
                )
                OutOfScopeHint = "HUD/input/presentation prefab work belongs to Agent B unless it is a compile compatibility note."
            }
        }
        "B" {
            return [PSCustomObject]@{
                Plan = "docs/plans/game_scene_agent_b_hud_input_validation_plan.md"
                Patterns = @(
                    "^Assets/Scripts/Features/(Unit|Wave|Combat|Player)/Presentation/",
                    "^Assets/Scripts/Shared/Ui/",
                    "^Assets/Prefabs/Features/Battle/",
                    "^Assets/Prefabs/Features/Result/",
                    "^tools/unity-mcp/(Invoke-GameSceneAgentBPlacementSmoke\.ps1|McpHelpers\.ps1)$",
                    "^docs/playtest/runtime_validation_checklist\.md$",
                    "^docs/plans/game_scene_agent_b_hud_input_validation_plan\.md$",
                    "^artifacts/unity/(game-scene-agent-b|placement-area-view).*\.(json|log|png)$",
                    "^artifacts/unity/unity-ui-authoring-workflow-policy-b\.json$"
                )
                OutOfScopeHint = "Runtime orchestration, authoritative gameplay state, sync source of truth, and GameSceneRoot are Agent A/Phase5 handoff lanes."
            }
        }
        "C" {
            return [PSCustomObject]@{
                Plan = "docs/plans/game_scene_agent_c_unit_identity_terms_plan.md"
                Patterns = @(
                    "^Assets/Scripts/Features/Garage/",
                    "^Assets/Scripts/Features/Unit/Presentation/((UnitSlotView|UnitSlotsContainer)\.cs|.+(Identity|Callsign|Formatter).+\.cs)$",
                    "^Assets/Scripts/Features/Wave/Presentation/WaveEndView\.cs$",
                    "^Assets/Prefabs/Features/Battle/",
                    "^Assets/Prefabs/Features/Result/",
                    "^docs/design/(game_design|world_design|unit_module_design)\.md$",
                    "^docs/plans/game_scene_agent_c_unit_identity_terms_plan\.md$",
                    "^docs/plans/progress\.md$",
                    "^artifacts/unity/game-scene-agent-c.*\.(json|log|png)$",
                    "^artifacts/unity/unity-ui-authoring-workflow-policy-c\.json$"
                )
                OutOfScopeHint = "Runtime scoring/storage, operation record persistence, and HUD layout/input ownership belong to Agent A/B unless C is only changing shared copy labels."
            }
        }
        "Phase5" {
            return [PSCustomObject]@{
                Plan = "docs/plans/game_scene_phase5_multiplayer_sync_plan.md"
                Patterns = @(
                    "^Assets/Scripts/Features/.+/(Infrastructure|Application|Domain)/",
                    "^Assets/Editor/DirectTests/",
                    "^docs/plans/game_scene_phase5_multiplayer_sync_plan\.md$",
                    "^docs/playtest/runtime_validation_checklist\.md$",
                    "^artifacts/unity/(game-scene-phase5|phase5|multiplayer-sync).*\.(json|log|png)$",
                    "^artifacts/unity/unity-ui-authoring-workflow-policy-phase5\.json$"
                )
                OutOfScopeHint = "Phase5 should prove sync/smoke behavior without reviving unrelated HUD polish or product scope."
            }
        }
        default {
            return [PSCustomObject]@{
                Plan = "docs/plans/progress.md"
                Patterns = @("^")
                OutOfScopeHint = "Any mode reports all dirty files and skips owner scope enforcement."
            }
        }
    }
}

function Get-PlanAcceptanceLines {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }

    $lines = @(Get-Content -LiteralPath $Path)
    $hits = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "acceptance|Acceptance|검증|Validation|Closeout|소유하지 않는다|Agent .*owns|Agent .*소유") {
            $hits.Add(("{0}: {1}" -f ($i + 1), $lines[$i].Trim()))
        }
    }

    return @($hits | Select-Object -First 24)
}

$scope = Get-AgentScope -Name $Agent
$changedFiles = @(Get-WorkflowChangedFiles -RepoRoot $RepoRoot -StagedOnly:$StagedOnly)
$inScope = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns $scope.Patterns)
$outOfScope = @(Get-WorkflowPathsOutside -Paths $changedFiles -Patterns $scope.Patterns)
$artifactFiles = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^artifacts/(unity|rules)/.*\.json$"))
$newCsFiles = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^Assets/.+\.cs$") | Where-Object {
    $status = (Invoke-WorkflowGit -RepoRoot $RepoRoot -Arguments @("status", "--short", "--", $_)).Lines | Select-Object -First 1
    $status -match "^\?\?"
})

Write-WorkflowSection ("Agent {0} Pass Review" -f $Agent)
Write-Host ("plan: {0}" -f $scope.Plan)
Write-Host ("changedFiles={0} inScope={1} outOfScope={2}" -f $changedFiles.Count, $inScope.Count, $outOfScope.Count)

Write-WorkflowSection "Plan Signals"
foreach ($line in @(Get-PlanAcceptanceLines -Path (Join-Path $RepoRoot $scope.Plan))) {
    Write-Host $line
}

Write-WorkflowSection "In Scope Changed Files"
foreach ($path in @($inScope | Select-Object -First 60)) {
    Write-Host ("  {0}" -f $path)
}
if ($inScope.Count -gt 60) {
    Write-Host ("  ... {0} more" -f ($inScope.Count - 60))
}

Write-WorkflowSection "Out Of Scope Changed Files"
if ($outOfScope.Count -eq 0) {
    Write-Host "none"
}
else {
    Write-Host $scope.OutOfScopeHint -ForegroundColor Yellow
    foreach ($path in @($outOfScope | Select-Object -First 80)) {
        Write-Host ("  {0}" -f $path) -ForegroundColor Yellow
    }
    if ($outOfScope.Count -gt 80) {
        Write-Host ("  ... {0} more" -f ($outOfScope.Count - 80)) -ForegroundColor Yellow
    }
}

Write-WorkflowSection "Review Caveats"
if ($artifactFiles.Count -gt 0) {
    Write-Host "generated artifact changed; run Test-GeneratedArtifactScope before using it as closeout evidence." -ForegroundColor Yellow
}
if ($newCsFiles.Count -gt 0) {
    Write-Host "new C# files detected; confirm Unity project sync or compile-clean." -ForegroundColor Yellow
    foreach ($path in $newCsFiles) {
        Write-Host ("  {0}" -f $path) -ForegroundColor Yellow
    }
}
if ($Agent -ne "Any" -and $outOfScope.Count -gt 0) {
    Write-Host "owner scope mismatch candidate; classify as handoff, residual, or separate owner change before closeout." -ForegroundColor Yellow
}
if ($artifactFiles.Count -eq 0 -and $newCsFiles.Count -eq 0 -and ($Agent -eq "Any" -or $outOfScope.Count -eq 0)) {
    Write-Host "no automatic caveat detected."
}

if ($RunCloseoutPack) {
    Write-WorkflowSection "Closeout Pack"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "tools\workflow\Invoke-CloseoutPack.ps1") -RepoRoot $RepoRoot -ChangedFile ($inScope -join ",") -Agent $Agent
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
