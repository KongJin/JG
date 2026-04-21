param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
    [string]$GarageTabButtonPath = "/Canvas/LobbyGarageNavBar/GarageTabButton",
    [string]$GarageRootPath = "/Canvas/GaragePageRoot",
    [string]$SettingsButtonPath = "/Canvas/GaragePageRoot/GarageHeaderRow/SettingsButton",
    [string]$SettingsOverlayPath = "/Canvas/GaragePageRoot/GarageSettingsOverlay",
    [string]$SettingsCloseButtonPath = "/Canvas/GaragePageRoot/GarageSettingsOverlay/AccountCard/SettingsCloseButton",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$GarageBeforeOutputPath = "artifacts/unity/garage-settings-smoke-before-open.png",
    [string]$SettingsOpenOutputPath = "artifacts/unity/garage-settings-smoke-open.png",
    [string]$SettingsClosedOutputPath = "artifacts/unity/garage-settings-smoke-closed.png",
    [string]$ResultPath = "artifacts/unity/garage-settings-smoke-result.json",
    [int]$TimeoutSec = 90,
    [int]$UiSettleMs = 500
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

Assert-McpSceneAssetExistsForWorkflow -ScenePath $ScenePath -WorkflowName "Invoke-GarageSettingsOverlaySmoke.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$resultAbsolutePath = Resolve-McpAbsolutePath -PathValue $ResultPath
$startedPlayHere = $false

try {
    $session = Invoke-McpPrepareLobbyPlaySession `
        -Root $root `
        -ScenePath $ScenePath `
        -LoginLoadingPanelPath $LoginLoadingPanelPath `
        -TimeoutSec $TimeoutSec
    $startedPlayHere = $true

    Invoke-McpUiInvoke -Root $root -Path $GarageTabButtonPath -Method "click" | Out-Null
    Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000) | Out-Null
    Start-Sleep -Milliseconds $UiSettleMs

    $beforeCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $GarageBeforeOutputPath -Overwrite

    $openInvoke = Invoke-McpUiInvoke -Root $root -Path $SettingsButtonPath -Method "click"
    $overlayReady = Wait-McpUiActive -Root $root -Path $SettingsOverlayPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $openCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $SettingsOpenOutputPath -Overwrite

    $closeInvoke = Invoke-McpUiInvoke -Root $root -Path $SettingsCloseButtonPath -Method "click"
    $overlayClosed = Wait-McpUiInactive -Root $root -Path $SettingsOverlayPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $closedCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $SettingsClosedOutputPath -Overwrite

    $consoleSummary = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

    $report = [PSCustomObject]@{
        success = $true
        generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
        scenePath = $ScenePath
        resultPath = $resultAbsolutePath
        uiPaths = [PSCustomObject]@{
            garageTabButton = $GarageTabButtonPath
            settingsButton = $SettingsButtonPath
            settingsOverlay = $SettingsOverlayPath
            settingsCloseButton = $SettingsCloseButtonPath
        }
        stoppedPreExistingPlay = $session.stoppedPreExistingPlay
        captures = [PSCustomObject]@{
            garageBeforeOpen = $beforeCapture.relativePath
            settingsOpen = $openCapture.relativePath
            settingsClosed = $closedCapture.relativePath
        }
        transitions = [PSCustomObject]@{
            openSettings = [PSCustomObject]@{
                invoked = Test-McpResponseSuccess -Response $openInvoke
                overlayReady = Test-McpResponseSuccess -Response $overlayReady
            }
            closeSettings = [PSCustomObject]@{
                invoked = Test-McpResponseSuccess -Response $closeInvoke
                overlayClosed = Test-McpResponseSuccess -Response $overlayClosed
            }
        }
        warningCount = $consoleSummary.warningCount
        errorCount = $consoleSummary.errorCount
        warnings = $consoleSummary.warnings
        errors = $consoleSummary.errors
    }

    Ensure-McpParentDirectory -PathValue $resultAbsolutePath
    ($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8

    [PSCustomObject]@{
        success = $true
        scenePath = $ScenePath
        resultPath = $resultAbsolutePath
        garageScreenshot = $beforeCapture.relativePath
        settingsOpenScreenshot = $openCapture.relativePath
        settingsClosedScreenshot = $closedCapture.relativePath
        warningCount = $consoleSummary.warningCount
        errorCount = $consoleSummary.errorCount
    } | ConvertTo-Json -Depth 5
}
finally {
    if ($startedPlayHere) {
        try {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }
        catch {
            Write-Warning ("Failed to stop Play Mode after settings smoke: {0}" -f $_.Exception.Message)
        }
    }
}
