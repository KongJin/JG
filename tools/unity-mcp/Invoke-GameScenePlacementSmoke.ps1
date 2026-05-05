param(
    [string]$BaseUrl,
    [ValidateSet("None", "Defeat", "Victory", "NaturalVictory")]
    [string]$ResultMode = "Defeat",
    [string]$Owner = "GameSceneUIUX",
    [string]$OutputPath = "artifacts/unity/current/game-scene-placement-smoke.json",
    [string]$LockPath = "Temp/UnityMcp/runtime-smoke.lock",
    [int]$LockTimeoutSec = 0,
    [int]$TimeoutSec = 120,
    [switch]$NoMcpLock,
    [switch]$ParentMcpLock,
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
    lockPath = $LockPath
    lockTimeoutSec = $LockTimeoutSec
    timeoutSec = $TimeoutSec
    noMcpLock = [bool]$NoMcpLock
    parentMcpLock = [bool]$ParentMcpLock
    leavePlayMode = [bool]$LeavePlayMode
    bridgeUrl = $BaseUrl
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    evidence = [PSCustomObject][ordered]@{
        migrationRequired = "This legacy GameScene placement smoke targeted UGUI/Canvas objects and is intentionally disabled. Replace it with a UIDocument/VisualElement UITK smoke before using this lane for acceptance."
        acceptedUiRuntime = "UITK"
        disabledUiRuntime = "UGUI"
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Ensure-McpParentDirectory -PathValue $OutputPath
    $result | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
}

$result | ConvertTo-Json -Depth 8
