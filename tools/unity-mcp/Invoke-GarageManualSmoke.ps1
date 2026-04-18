param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$GarageOpenButtonPath = "/Canvas/LobbyPageRoot/GarageTabButton",
    [string]$LobbyRootPath = "/Canvas/LobbyPageRoot",
    [string]$GarageRootPath = "/Canvas/GaragePageRoot",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$LoginErrorPanelPath = "/Canvas/LoginLoadingOverlay/ErrorPanel",
    [string]$OutputPath = "artifacts/unity/garage-manual-smoke.png",
    [string]$ResultPath = "artifacts/unity/garage-manual-smoke-result.json",
    [int]$TimeoutSec = 90
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

function Resolve-AbsolutePath {
    param([string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $PathValue))
}

function Ensure-ParentDirectory {
    param([string]$PathValue)

    $directory = Split-Path -Parent $PathValue
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

function Get-PageStateSnapshot {
    param(
        [string]$RootValue,
        [string]$LobbyRootPathValue,
        [string]$GarageRootPathValue
    )

    return [PSCustomObject]@{
        lobby = Get-McpUiElementState -Root $RootValue -Path $LobbyRootPathValue
        garage = Get-McpUiElementState -Root $RootValue -Path $GarageRootPathValue
    }
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$sceneName = [System.IO.Path]::GetFileNameWithoutExtension($ScenePath)
$resultAbsolutePath = Resolve-AbsolutePath -PathValue $ResultPath
$startedPlayHere = $false

try {
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
    $prePlayHealth = $health.State
    $stoppedPreExistingPlay = $false

    if ($health.State.isPlaying) {
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        $stoppedPreExistingPlay = $true
    }

    if ($health.State.activeScenePath -ne $ScenePath) {
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $ScenePath -TimeoutSec $TimeoutSec | Out-Null
        Wait-McpSceneActive -Root $root -SceneName $sceneName -TimeoutSec $TimeoutSec -PollSec 0.5 | Out-Null
    }

    $play = Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $TimeoutSec
    $startedPlayHere = $true

    $uiStateBefore = Get-McpUiStateSummary `
        -Root $root `
        -PathPrefixes @(
            $LobbyRootPath,
            $GarageRootPath,
            "/Canvas/LoginLoadingOverlay"
        ) `
        -ComponentTypes @("Button", "TMP_Text", "TMP_InputField") `
        -MaxItems 60 `
        -IncludeInactive

    $pageStatesBefore = Get-PageStateSnapshot -RootValue $root -LobbyRootPathValue $LobbyRootPath -GarageRootPathValue $GarageRootPath
    $invoke = Invoke-McpUiInvoke -Root $root -Path $GarageOpenButtonPath -Method "click"
    $garageReady = Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    $lobbyHidden = Wait-McpUiInactive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    $loadingPanelWait = $null
    $loadingPanelState = $null
    $errorPanelState = $null

    if (-not [string]::IsNullOrWhiteSpace($LoginLoadingPanelPath)) {
        try {
            $loadingPanelWait = Wait-McpUiInactive -Root $root -Path $LoginLoadingPanelPath -TimeoutMs ($TimeoutSec * 1000)
        }
        catch {
            $loadingPanelWait = [PSCustomObject]@{
                ok = $false
                message = $_.Exception.Message
            }
        }

        try {
            $loadingPanelState = Get-McpUiElementState -Root $root -Path $LoginLoadingPanelPath
        }
        catch {
            $loadingPanelState = [PSCustomObject]@{
                ok = $false
                message = $_.Exception.Message
            }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($LoginErrorPanelPath)) {
        try {
            $errorPanelState = Get-McpUiElementState -Root $root -Path $LoginErrorPanelPath
        }
        catch {
            $errorPanelState = [PSCustomObject]@{
                ok = $false
                message = $_.Exception.Message
            }
        }
    }

    $capture = Invoke-McpScreenshotCapture -Root $root -OutputPath $OutputPath -Overwrite
    $garageState = Get-McpUiElementState -Root $root -Path $GarageRootPath
    $pageStatesAfter = Get-PageStateSnapshot -RootValue $root -LobbyRootPathValue $LobbyRootPath -GarageRootPathValue $GarageRootPath
    $consoleSummary = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

    $report = [PSCustomObject]@{
        success = $true
        generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
        root = $root
        scene = $sceneName
        scenePath = $ScenePath
        resultPath = $resultAbsolutePath
        uiPaths = [PSCustomObject]@{
            garageOpenButton = $GarageOpenButtonPath
            lobbyRoot = $LobbyRootPath
            garageRoot = $GarageRootPath
            loginLoadingPanel = $LoginLoadingPanelPath
            loginErrorPanel = $LoginErrorPanelPath
        }
        prePlayHealth = $prePlayHealth
        stoppedPreExistingPlay = $stoppedPreExistingPlay
        play = $play
        uiStateBefore = $uiStateBefore
        pageStates = [PSCustomObject]@{
            beforeInvoke = $pageStatesBefore
            afterInvoke = $pageStatesAfter
        }
        invoke = $invoke
        garageReady = $garageReady
        lobbyHidden = $lobbyHidden
        loadingPanelWait = $loadingPanelWait
        loadingPanelState = $loadingPanelState
        errorPanelState = $errorPanelState
        garageState = $garageState
        screenshot = $capture
        consoleSummary = $consoleSummary
        debugHints = @(
            [PSCustomObject]@{
                condition = "Builder edits do not show up in GameView."
                hint = "Run Assets/Refresh -> Tools/Codex/Build Codex Lobby Scene -> scene/save before re-running the smoke."
            }
        )
    }

    Ensure-ParentDirectory -PathValue $resultAbsolutePath
    ($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
    $report | ConvertTo-Json -Depth 8
}
finally {
    if ($startedPlayHere) {
        try {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }
        catch {
            Write-Warning ("Failed to stop Play Mode after Garage smoke: {0}" -f $_.Exception.Message)
        }
    }
}
