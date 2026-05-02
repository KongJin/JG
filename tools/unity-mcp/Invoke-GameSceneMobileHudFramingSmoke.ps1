param(
    [string]$BaseUrl,
    [string]$Owner = "GameSceneMobileHudFraming",
    [ValidateSet("None", "Defeat", "Victory", "NaturalVictory")]
    [string]$ResultMode = "NaturalVictory",
    [string]$OutputPath = "artifacts/unity/game-flow/game-scene-mobile-hud-framing-smoke.json",
    [string]$PlacementOutputPath = "artifacts/unity/game-flow/game-scene-mobile-hud-placement-source.json",
    [string]$ScreenshotPath = "artifacts/unity/game-flow/game-scene-mobile-hud-framing.png",
    [string]$LockPath = "Temp/UnityMcp/runtime-smoke.lock",
    [int]$LockTimeoutSec = 0,
    [int]$TimeoutSec = 240,
    [switch]$BattleSceneDirect,
    [switch]$LeavePlayMode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "McpHelpers.ps1")

$result = [PSCustomObject][ordered]@{
    success = $false
    terminalVerdict = "blocked"
    blockedReason = "ugui-smoke-contract-disabled"
    owner = $Owner
    resultMode = $ResultMode
    outputPath = $OutputPath
    placementOutputPath = $PlacementOutputPath
    screenshotPath = $ScreenshotPath
    lockPath = $LockPath
    lockTimeoutSec = $LockTimeoutSec
    timeoutSec = $TimeoutSec
    battleSceneDirect = [bool]$BattleSceneDirect
    leavePlayMode = [bool]$LeavePlayMode
    bridgeUrl = $BaseUrl
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    evidence = [PSCustomObject][ordered]@{
        migrationRequired = "This legacy mobile HUD framing smoke targeted UGUI/Canvas objects and is intentionally disabled. Replace it with a UIDocument/VisualElement UITK framing smoke before using this lane for acceptance."
        acceptedUiRuntime = "UITK"
        disabledUiRuntime = "UGUI"
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Ensure-McpParentDirectory -PathValue $OutputPath
    $result | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
}

$result | ConvertTo-Json -Depth 8
