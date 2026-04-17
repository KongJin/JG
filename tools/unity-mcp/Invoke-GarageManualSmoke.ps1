param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$GarageTabPath = "/Canvas/TopTabs/GarageTabButton",
    [string]$GarageRootPath = "/Canvas/GaragePageRoot",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$LoginErrorPanelPath = "/Canvas/LoginLoadingOverlay/ErrorPanel",
    [string]$OutputPath = "artifacts/unity/garage-manual-smoke.png",
    [int]$TimeoutSec = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$sceneName = [System.IO.Path]::GetFileNameWithoutExtension($ScenePath)
$startedPlayHere = $false

try {
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec

    if ($health.State.isPlaying) {
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
    }

    if ($health.State.activeScenePath -ne $ScenePath) {
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $ScenePath -TimeoutSec $TimeoutSec | Out-Null
        Wait-McpSceneActive -Root $root -SceneName $sceneName -TimeoutSec $TimeoutSec -PollSec 0.5 | Out-Null
    }

    $play = Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $TimeoutSec
    $startedPlayHere = $true

    $uiStateBefore = Get-McpUiState -Root $root
    $invoke = Invoke-McpUiInvoke -Root $root -Path $GarageTabPath -Method "click"
    $garageReady = Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
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

    [PSCustomObject]@{
        success = $true
        root = $root
        scene = $sceneName
        play = $play
        uiStateBefore = $uiStateBefore
        invoke = $invoke
        garageReady = $garageReady
        loadingPanelWait = $loadingPanelWait
        loadingPanelState = $loadingPanelState
        errorPanelState = $errorPanelState
        garageState = $garageState
        screenshot = $capture
    }
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
