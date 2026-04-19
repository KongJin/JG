param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$GarageOpenButtonPath = "/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard/GarageTabButton",
    [string]$BackToLobbyButtonPath = "/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton",
    [string]$LobbyRootPath = "/Canvas/LobbyPageRoot",
    [string]$GarageRootPath = "/Canvas/GaragePageRoot",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$LobbyOutputPath = "artifacts/unity/lobby-page-smoke-lobby-initial.png",
    [string]$GarageOutputPath = "artifacts/unity/lobby-page-smoke-garage.png",
    [string]$ReturnedLobbyOutputPath = "artifacts/unity/lobby-page-smoke-lobby-returned.png",
    [string]$ResultPath = "artifacts/unity/lobby-garage-page-switch-result.json",
    [int]$TimeoutSec = 90,
    [int]$UiSettleMs = 500
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$resultAbsolutePath = Resolve-McpAbsolutePath -PathValue $ResultPath
$startedPlayHere = $false

try {
    $session = Invoke-McpPrepareCodexLobbyPlaySession `
        -Root $root `
        -ScenePath $ScenePath `
        -LoginLoadingPanelPath $LoginLoadingPanelPath `
        -TimeoutSec $TimeoutSec
    $startedPlayHere = $true
    Start-Sleep -Milliseconds $UiSettleMs
    $lobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $LobbyOutputPath -Overwrite
    $lobbyStates = Get-McpPageStateSnapshot -Root $root -LobbyRootPath $LobbyRootPath -GarageRootPath $GarageRootPath

    $garageInvoke = Invoke-McpUiInvoke -Root $root -Path $GarageOpenButtonPath -Method "click"
    $garageReady = Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    $lobbyHidden = Wait-McpUiInactive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $garageCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $GarageOutputPath -Overwrite
    $garageStates = Get-McpPageStateSnapshot -Root $root -LobbyRootPath $LobbyRootPath -GarageRootPath $GarageRootPath

    $backInvoke = Invoke-McpUiInvoke -Root $root -Path $BackToLobbyButtonPath -Method "click"
    $lobbyReturned = Wait-McpUiActive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    $garageHiddenAfterReturn = Wait-McpUiInactive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $returnedLobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $ReturnedLobbyOutputPath -Overwrite
    $returnedLobbyStates = Get-McpPageStateSnapshot -Root $root -LobbyRootPath $LobbyRootPath -GarageRootPath $GarageRootPath

    $consoleSummary = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

    $report = [PSCustomObject]@{
        success = $true
        generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
        scenePath = $ScenePath
        resultPath = $resultAbsolutePath
        uiPaths = [PSCustomObject]@{
            garageOpenButton = $GarageOpenButtonPath
            backToLobbyButton = $BackToLobbyButtonPath
            lobbyRoot = $LobbyRootPath
            garageRoot = $GarageRootPath
        }
        stoppedPreExistingPlay = $session.stoppedPreExistingPlay
        captures = [PSCustomObject]@{
            lobbyInitial = $lobbyCapture.relativePath
            garage = $garageCapture.relativePath
            lobbyReturned = $returnedLobbyCapture.relativePath
        }
        pageStates = [PSCustomObject]@{
            lobbyInitial = $lobbyStates
            garageCapture = $garageStates
            lobbyReturned = $returnedLobbyStates
        }
        transitions = [PSCustomObject]@{
            toGarage = [PSCustomObject]@{
                invoked = Test-McpResponseSuccess -Response $garageInvoke
                garageReady = Test-McpResponseSuccess -Response $garageReady
                lobbyHidden = Test-McpResponseSuccess -Response $lobbyHidden
            }
            toLobby = [PSCustomObject]@{
                invoked = Test-McpResponseSuccess -Response $backInvoke
                lobbyReturned = Test-McpResponseSuccess -Response $lobbyReturned
                garageHidden = Test-McpResponseSuccess -Response $garageHiddenAfterReturn
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
        lobbyScreenshot = $lobbyCapture.relativePath
        garageScreenshot = $garageCapture.relativePath
        returnedLobbyScreenshot = $returnedLobbyCapture.relativePath
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
            Write-Warning ("Failed to stop Play Mode after page-switch smoke: {0}" -f $_.Exception.Message)
        }
    }
}
