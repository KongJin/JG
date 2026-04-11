# Unity MCP: LobbyScene 플레이 → 방 생성 → Ready → Start → Photon LoadLevel 로 게임 씬까지.
# 전제: 에디터에서 MCP 브리지 실행 중, Photon 앱 설정·네트워크 사용 가능.
# JSON: -ResultJsonPath (기본 Temp/UnityMcp/last-lobby-to-game.json), -WriteJsonToStdout

[CmdletBinding()]
param(
    [string]$BaseUrl,
    [int]$PhotonWaitSec = 3,
    [int]$LobbySceneActiveTimeoutSec = 90,
    [int]$PlayModeReadyTimeoutSec = 120,
    [double]$PlayModePollSec = 0.5,
    [int]$AfterCreateWaitSec = 5,
    [int]$AfterReadyWaitSec = 2,
    [string]$ResultJsonPath = "",
    [switch]$WriteJsonToStdout,
    [switch]$Screenshot,
    [switch]$NoStopPlay
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "mcp-test-common.ps1")

$script:TestSteps = New-Object System.Collections.ArrayList
$script:TestScreenshots = New-Object System.Collections.ArrayList
$script:TestOk = $true
$script:Failure = $null
$script:CurrentStep = "init"
$script:LastHealth = $null

function Get-DefaultResultJsonPath {
    Join-Path $PSScriptRoot "..\Temp\UnityMcp\last-lobby-to-game.json"
}

function Add-TestStep {
    param([string]$Name, [bool]$Ok, [string]$Detail = $null, [long]$ElapsedMs = -1)
    $row = [ordered]@{ name = $Name; ok = $Ok }
    if (-not [string]::IsNullOrEmpty($Detail)) { $row.detail = $Detail }
    if ($ElapsedMs -ge 0) { $row.elapsedMs = $ElapsedMs }
    [void]$script:TestSteps.Add([PSCustomObject]$row)
    if (-not $Ok) { $script:TestOk = $false }
}

function Write-TestResultJson {
    param([string]$Root, [string]$JsonPath, [bool]$ToStdout, [bool]$Shot, [bool]$NoStop)
    if ([string]::IsNullOrWhiteSpace($JsonPath)) { $JsonPath = Get-DefaultResultJsonPath }
    $dir = Split-Path -Parent $JsonPath
    if (-not [string]::IsNullOrEmpty($dir) -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $fh = $null
    if ($null -ne $script:LastHealth) {
        $fh = [ordered]@{
            activeScene = [string]$script:LastHealth.activeScene
            isPlaying   = [bool]$script:LastHealth.isPlaying
        }
    }
    $payload = [ordered]@{
        schemaVersion = 1
        ok            = [bool]$script:TestOk
        finishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        mcpBaseUrl    = $Root
        flags         = [ordered]@{
            screenshot = [bool]$Shot
            noStopPlay = [bool]$NoStop
        }
        steps         = @($script:TestSteps.ToArray())
        finalHealth   = $fh
        screenshots   = @($script:TestScreenshots.ToArray())
    }
    if (-not $script:TestOk -and $null -ne $script:Failure) {
        $payload.failure = $script:Failure
    }
    $json = ($payload | ConvertTo-Json -Depth 10)
    Set-Content -Path $JsonPath -Value $json -Encoding utf8
    Write-Host "Result JSON: $JsonPath" -ForegroundColor DarkCyan
    if ($ToStdout) { Write-Output $json }
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$resultPathResolved = if ([string]::IsNullOrWhiteSpace($ResultJsonPath)) { Get-DefaultResultJsonPath } else { $ResultJsonPath }

try {
    $script:CurrentStep = "health_precheck"
    Write-Host "MCP: $root" -ForegroundColor Cyan
    $h = Invoke-McpGetJson -Root $root -SubPath "/health"
    if (-not $h.ok) { throw "MCP /health not ok." }
    $script:LastHealth = $h
    Add-TestStep -Name "health_precheck" -Ok $true -Detail "scene=$($h.activeScene)"
    Write-Host "Active scene (before): $($h.activeScene)" -ForegroundColor Gray
    Write-McpRecentConsole -Root $root -Label "before lobby flow" -LogLimit 30 -ErrorLimit 10

    $lobbyScenePath = "Assets/Scenes/LobbyScene.unity"
    if ($h.isPlaying -and $h.activeScene -ne "LobbyScene") {
        $script:CurrentStep = "stop_play_wrong_scene"
        Write-Host "Stopping play (scene=$($h.activeScene), need LobbyScene)..." -ForegroundColor Yellow
        $stopResult = Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 -PollSec $PlayModePollSec
        $h = $stopResult.StoppedState
        if ($h.isPlaying) { throw "Could not exit play mode to open lobby scene." }
        Add-TestStep -Name "stop_play_for_lobby" -Ok $true
    }

    if (-not $h.isPlaying) {
        $script:CurrentStep = "scene_open_play_start"
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $lobbyScenePath -SaveCurrentSceneIfDirty $true -TimeoutSec 60 -PollSec $PlayModePollSec | Out-Null
        Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec | Out-Null
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true
    }
    else {
        Write-Host "Already in play on LobbyScene — skipping scene/open and play/start." -ForegroundColor Gray
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true -Detail "skipped_already_in_lobby_play"
    }

    $script:CurrentStep = "play_mode_ready"
    Write-Host "Waiting for play mode + first frame (poll /health, timeout ${PlayModeReadyTimeoutSec}s)..." -ForegroundColor Yellow
    $pm = Wait-McpPlayModeReady -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "play_mode_ready" -Ok $true -ElapsedMs $pm.ElapsedMs

    $script:CurrentStep = "lobby_scene_active"
    Write-Host "Waiting for active scene LobbyScene (timeout ${LobbySceneActiveTimeoutSec}s)..." -ForegroundColor Yellow
    $ls = Wait-McpSceneActive -Root $root -SceneName "LobbyScene" -TimeoutSec $LobbySceneActiveTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "lobby_scene_active" -Ok $true -ElapsedMs $ls.ElapsedMs

    $script:CurrentStep = "lobby_settle_wait"
    Write-Host "Lobby scene active. Waiting ${PhotonWaitSec}s before Create Room..." -ForegroundColor Yellow
    $swS = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds $PhotonWaitSec
    $swS.Stop()
    Add-TestStep -Name "lobby_settle_wait" -Ok $true -Detail "seconds=$PhotonWaitSec" -ElapsedMs $swS.ElapsedMilliseconds

    $script:CurrentStep = "create_room"
    $createSpec = Get-McpUiPath -Key "Lobby.CreateRoom"
    $createResult = Invoke-McpButton -Root $root -Path $createSpec.Path -FallbackName $createSpec.FallbackName -Label "Create Room"
    Add-TestStep -Name "create_room" -Ok $true
    Write-Host "CreateRoom invoked: $($createResult.path)" -ForegroundColor Yellow
    Write-Host "CreateRoom invoked. Waiting ${AfterCreateWaitSec}s..." -ForegroundColor Yellow
    Start-Sleep -Seconds $AfterCreateWaitSec
    Write-McpRecentConsole -Root $root -Label "after Create Room" -LogLimit 40 -ErrorLimit 10

    $script:CurrentStep = "ready"
    $readySpec = Get-McpUiPath -Key "Lobby.Ready"
    $readyResult = Invoke-McpButton -Root $root -Path $readySpec.Path -FallbackName $readySpec.FallbackName -Label "Ready"
    Add-TestStep -Name "ready" -Ok $true
    Write-Host "Ready invoked: $($readyResult.path)" -ForegroundColor Yellow
    Start-Sleep -Seconds $AfterReadyWaitSec
    Write-McpRecentConsole -Root $root -Label "after Ready" -LogLimit 40 -ErrorLimit 10

    $script:CurrentStep = "start"
    $startSpec = Get-McpUiPath -Key "Lobby.StartGame"
    $startResult = Invoke-McpButton -Root $root -Path $startSpec.Path -FallbackName $startSpec.FallbackName -Label "Start Game"
    Add-TestStep -Name "start" -Ok $true
    Write-Host "StartGame invoked: $($startResult.path)" -ForegroundColor Yellow
    Write-Host "StartGame invoked. Waiting for LoadLevel..." -ForegroundColor Yellow
    Start-Sleep -Seconds 6
    Write-McpRecentConsole -Root $root -Label "after Start Game" -LogLimit 60 -ErrorLimit 15

    $h2 = Invoke-McpGetJson -Root $root -SubPath "/health"
    $script:LastHealth = $h2
    Write-Host "Active scene (after): $($h2.activeScene)  isPlaying: $($h2.isPlaying)" -ForegroundColor Green
    $inGame = ($h2.activeScene -eq "GameScene")
    Add-TestStep -Name "game_scene" -Ok $inGame -Detail "activeScene=$($h2.activeScene)"
    if (-not $inGame) {
        $script:Failure = @{ step = "game_scene"; message = "Expected GameScene, was $($h2.activeScene)." }
    }

    if ($Screenshot) {
        $script:CurrentStep = "screenshot"
        try {
            $cap = @{
                outputPath = "Temp/UnityMcp/Screenshots/lobby-to-game-flow.png"
                overwrite  = $true
            }
            $r = Invoke-McpJson -Root $root -SubPath "/screenshot/capture" -Body $cap
            Write-Host "Screenshot: $($r.relativePath)" -ForegroundColor Green
            if ($r.relativePath) { [void]$script:TestScreenshots.Add([string]$r.relativePath) }
            Add-TestStep -Name "screenshot" -Ok $true
        }
        catch {
            Add-TestStep -Name "screenshot" -Ok $false -Detail $_.Exception.Message
            if ($null -eq $script:Failure) {
                $script:Failure = @{ step = "screenshot"; message = $_.Exception.Message }
            }
        }
    }

    if (-not $NoStopPlay) {
        $script:CurrentStep = "play_stop"
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 -PollSec $PlayModePollSec | Out-Null
        Write-Host "Play stopped." -ForegroundColor Gray
        Add-TestStep -Name "play_stop" -Ok $true
    }
    else {
        Add-TestStep -Name "play_stop" -Ok $true -Detail "skipped_no_stop_play"
    }
}
catch {
    $script:TestOk = $false
    if ($null -eq $script:Failure) {
        $script:Failure = @{ step = $script:CurrentStep; message = $_.Exception.Message }
    }
    Add-TestStep -Name $script:CurrentStep -Ok $false -Detail $_.Exception.Message
    Write-Host "FATAL: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Write-TestResultJson -Root $root -JsonPath $resultPathResolved -ToStdout:$WriteJsonToStdout -Shot:$Screenshot -NoStop:$NoStopPlay
}

if (-not $script:TestOk) {
    exit 1
}
