param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$GarageOpenButtonPath = "/Canvas/LobbyPageRoot/GarageTabButton",
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
    $timings = [ordered]@{}

    $stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
    $stepWatch.Stop()
    $timings.bridgeHealthyMs = $stepWatch.ElapsedMilliseconds
    $prePlayHealth = $health.State
    $stoppedPreExistingPlay = $false

    if ($health.State.isPlaying) {
        $stepWatch.Restart()
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        $stepWatch.Stop()
        $timings.preExistingPlayStopMs = $stepWatch.ElapsedMilliseconds
        $stoppedPreExistingPlay = $true
    }

    if ($prePlayHealth.activeScenePath -ne $ScenePath) {
        $stepWatch.Restart()
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $ScenePath -TimeoutSec $TimeoutSec | Out-Null
        Wait-McpSceneActive -Root $root -SceneName $sceneName -TimeoutSec $TimeoutSec -PollSec 0.5 | Out-Null
        $stepWatch.Stop()
        $timings.sceneOpenMs = $stepWatch.ElapsedMilliseconds
    }

    $stepWatch.Restart()
    $play = Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $TimeoutSec
    $stepWatch.Stop()
    $timings.playReadyMs = $stepWatch.ElapsedMilliseconds
    $startedPlayHere = $true

    $loadingPanelWait = $null
    if (-not [string]::IsNullOrWhiteSpace($LoginLoadingPanelPath)) {
        $stepWatch.Restart()
        try {
            $loadingPanelWait = Wait-McpUiInactive -Root $root -Path $LoginLoadingPanelPath -TimeoutMs ($TimeoutSec * 1000)
        }
        catch {
            $loadingPanelWait = [PSCustomObject]@{
                ok = $false
                message = $_.Exception.Message
            }
        }
        $stepWatch.Stop()
        $timings.loginOverlayWaitMs = $stepWatch.ElapsedMilliseconds
    }

    $summaryPrefixes = @(
        $LobbyRootPath,
        $GarageRootPath,
        "/Canvas/LoginLoadingOverlay"
    )
    $summaryComponents = @("Button", "TMP_Text", "TMP_InputField")

    $uiBefore = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80 -IncludeInactive
    $initialStates = Get-PageStateSnapshot -RootValue $root -LobbyRootPathValue $LobbyRootPath -GarageRootPathValue $GarageRootPath

    $stepWatch.Restart()
    Start-Sleep -Milliseconds $UiSettleMs
    $lobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $LobbyOutputPath -Overwrite
    $lobbySummary = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80 -IncludeInactive
    $lobbyStates = Get-PageStateSnapshot -RootValue $root -LobbyRootPathValue $LobbyRootPath -GarageRootPathValue $GarageRootPath
    $stepWatch.Stop()
    $timings.initialLobbyCaptureMs = $stepWatch.ElapsedMilliseconds

    $stepWatch.Restart()
    $garageInvoke = Invoke-McpUiInvoke -Root $root -Path $GarageOpenButtonPath -Method "click"
    $garageReady = Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    $lobbyHidden = Wait-McpUiInactive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $garageCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $GarageOutputPath -Overwrite
    $garageSummary = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80 -IncludeInactive
    $garageStates = Get-PageStateSnapshot -RootValue $root -LobbyRootPathValue $LobbyRootPath -GarageRootPathValue $GarageRootPath
    $stepWatch.Stop()
    $timings.garageCaptureMs = $stepWatch.ElapsedMilliseconds

    $stepWatch.Restart()
    $backInvoke = Invoke-McpUiInvoke -Root $root -Path $BackToLobbyButtonPath -Method "click"
    $lobbyReturned = Wait-McpUiActive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    $garageHiddenAfterReturn = Wait-McpUiInactive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $returnedLobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $ReturnedLobbyOutputPath -Overwrite
    $returnedLobbySummary = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80 -IncludeInactive
    $returnedLobbyStates = Get-PageStateSnapshot -RootValue $root -LobbyRootPathValue $LobbyRootPath -GarageRootPathValue $GarageRootPath
    $stepWatch.Stop()
    $timings.returnedLobbyCaptureMs = $stepWatch.ElapsedMilliseconds

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
            backToLobbyButton = $BackToLobbyButtonPath
            lobbyRoot = $LobbyRootPath
            garageRoot = $GarageRootPath
            loginLoadingPanel = $LoginLoadingPanelPath
        }
        timingsMs = [PSCustomObject]$timings
        prePlayHealth = $prePlayHealth
        stoppedPreExistingPlay = $stoppedPreExistingPlay
        play = $play
        loadingPanelWait = $loadingPanelWait
        uiBefore = $uiBefore
        pageStates = [PSCustomObject]@{
            initial = $initialStates
            lobbyCapture = $lobbyStates
            garageCapture = $garageStates
            afterReturn = $returnedLobbyStates
        }
        captures = [PSCustomObject]@{
            lobbyInitial = $lobbyCapture
            garage = $garageCapture
            lobbyReturned = $returnedLobbyCapture
        }
        transitions = [PSCustomObject]@{
            toGarage = [PSCustomObject]@{
                invoke = $garageInvoke
                garageReady = $garageReady
                lobbyHidden = $lobbyHidden
                summary = $garageSummary
            }
            toLobby = [PSCustomObject]@{
                invoke = $backInvoke
                lobbyReturned = $lobbyReturned
                garageHidden = $garageHiddenAfterReturn
                summary = $returnedLobbySummary
            }
        }
        lobbySummary = $lobbySummary
        consoleSummary = $consoleSummary
        debugHints = @(
            [PSCustomObject]@{
                condition = "Button path mismatch or old hierarchy keeps appearing."
                hint = "Run Assets/Refresh -> Tools/Codex/Build Codex Lobby Scene -> scene/save before rerunning this smoke."
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
            Write-Warning ("Failed to stop Play Mode after page-switch smoke: {0}" -f $_.Exception.Message)
        }
    }
}
