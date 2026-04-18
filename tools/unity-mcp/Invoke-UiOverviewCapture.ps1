param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$LobbyTabPath = "/Canvas/TopTabs/LobbyTabButton",
    [string]$GarageTabPath = "/Canvas/TopTabs/GarageTabButton",
    [string]$GarageRootPath = "/Canvas/GaragePageRoot",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$LoginErrorPanelPath = "/Canvas/LoginLoadingOverlay/ErrorPanel",
    [string]$LobbyOutputPath = "artifacts/unity/ui-overview-lobby.png",
    [string]$GarageOutputPath = "artifacts/unity/ui-overview-garage.png",
    [string]$ReportOutputPath = "artifacts/unity/ui-overview-report.json",
    [int]$TimeoutSec = 90,
    [int]$UiSettleMs = 500
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

function Resolve-ReportPath {
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

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$sceneName = [System.IO.Path]::GetFileNameWithoutExtension($ScenePath)
$reportAbsolutePath = Resolve-ReportPath -PathValue $ReportOutputPath
$startedPlayHere = $false

try {
    $timings = [ordered]@{}

    $stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
    $stepWatch.Stop()
    $timings.bridgeHealthyMs = $stepWatch.ElapsedMilliseconds

    if ($health.State.isPlaying) {
        $stepWatch.Restart()
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        $stepWatch.Stop()
        $timings.preExistingPlayStopMs = $stepWatch.ElapsedMilliseconds
    }

    if ($health.State.activeScenePath -ne $ScenePath) {
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
    $stepWatch.Restart()
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
    }
    $stepWatch.Stop()
    $timings.loginOverlayWaitMs = $stepWatch.ElapsedMilliseconds

    $summaryPrefixes = @(
        "/Canvas/LobbyPageRoot",
        "/Canvas/GaragePageRoot",
        "/Canvas/TopTabs",
        "/Canvas/LoginLoadingOverlay"
    )
    $summaryComponents = @("Button", "TMP_Text", "TMP_InputField")

    $uiBefore = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80

    $stepWatch.Restart()
    $lobbyInvoke = Invoke-McpUiInvoke -Root $root -Path $LobbyTabPath -Method "click"
    Start-Sleep -Milliseconds $UiSettleMs
    $lobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $LobbyOutputPath -Overwrite
    $lobbySummary = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80
    $stepWatch.Stop()
    $timings.lobbyCaptureMs = $stepWatch.ElapsedMilliseconds

    $stepWatch.Restart()
    $garageInvoke = Invoke-McpUiInvoke -Root $root -Path $GarageTabPath -Method "click"
    $garageReady = Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $garageCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $GarageOutputPath -Overwrite
    $garageSummary = Get-McpUiStateSummary -Root $root -PathPrefixes $summaryPrefixes -ComponentTypes $summaryComponents -MaxItems 80
    $stepWatch.Stop()
    $timings.garageCaptureMs = $stepWatch.ElapsedMilliseconds

    $garageState = Get-McpUiElementState -Root $root -Path $GarageRootPath
    $loginErrorState = $null
    if (-not [string]::IsNullOrWhiteSpace($LoginErrorPanelPath)) {
        try {
            $loginErrorState = Get-McpUiElementState -Root $root -Path $LoginErrorPanelPath
        }
        catch {
            $loginErrorState = [PSCustomObject]@{
                ok = $false
                message = $_.Exception.Message
            }
        }
    }

    $consoleSummary = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

    $report = [PSCustomObject]@{
        success = $true
        generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
        root = $root
        scene = $sceneName
        reportPath = $reportAbsolutePath
        timingsMs = [PSCustomObject]$timings
        play = $play
        loadingPanelWait = $loadingPanelWait
        uiBefore = $uiBefore
        lobby = [PSCustomObject]@{
            invoke = $lobbyInvoke
            summary = $lobbySummary
            screenshot = $lobbyCapture
        }
        garage = [PSCustomObject]@{
            invoke = $garageInvoke
            ready = $garageReady
            summary = $garageSummary
            state = $garageState
            screenshot = $garageCapture
        }
        loginErrorState = $loginErrorState
        consoleSummary = $consoleSummary
    }

    Ensure-ParentDirectory -PathValue $reportAbsolutePath
    ($report | ConvertTo-Json -Depth 8) | Set-Content -Path $reportAbsolutePath -Encoding UTF8

    [PSCustomObject]@{
        success = $true
        scene = $sceneName
        reportPath = $reportAbsolutePath
        lobbyScreenshot = $lobbyCapture.relativePath
        garageScreenshot = $garageCapture.relativePath
        timingsMs = [PSCustomObject]$timings
        warningCount = $consoleSummary.warningCount
        errorCount = $consoleSummary.errorCount
        benignCount = $consoleSummary.benignCount
    } | ConvertTo-Json -Depth 5
}
finally {
    if ($startedPlayHere) {
        try {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }
        catch {
            Write-Warning ("Failed to stop Play Mode after UI overview capture: {0}" -f $_.Exception.Message)
        }
    }
}
