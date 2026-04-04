# Unity MCP: JG_LobbyScene 플레이 → 방 생성 → Ready → Start → Photon LoadLevel 로 게임 씬까지.
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

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)
    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) {
        return $ExplicitBaseUrl.TrimEnd("/")
    }
    $portFile = Join-Path $PSScriptRoot "..\ProjectSettings\UnityMcpPort.txt"
    $defaultPort = 51234
    if (Test-Path $portFile) {
        $portText = (Get-Content -Path $portFile -Raw).Trim()
        if ($portText -match '^\d+$') {
            return "http://127.0.0.1:$portText"
        }
    }
    return "http://127.0.0.1:$defaultPort"
}

function Invoke-McpJson {
    param(
        [string]$Root,
        [string]$SubPath,
        [object]$Body = $null
    )
    $uri = "$Root$SubPath"
    if ($null -eq $Body) {
        return Invoke-RestMethod -Method Post -Uri $uri
    }
    $json = $Body | ConvertTo-Json -Compress
    return Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body $json
}

function Get-McpPlayModeChanging {
    param([object]$State)
    if ($null -ne $State -and $null -ne $State.PSObject.Properties["isPlayModeChanging"]) {
        return [bool]$State.isPlayModeChanging
    }
    if ($null -ne $State -and $null -ne $State.PSObject.Properties["isPlayingOrWillChange"]) {
        return [bool]$State.isPlayingOrWillChange
    }
    return $false
}

function Wait-McpPlayModeReady {
    param([string]$Root, [int]$TimeoutSec, [double]$PollSec)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $s = Invoke-RestMethod -Uri "$Root/health" -Method Get -ErrorAction Stop
            $isPlayModeChanging = Get-McpPlayModeChanging -State $s
            if ($s.isPlaying -and -not $isPlayModeChanging -and -not $s.isCompiling) {
                $sw.Stop()
                return @{ State = $s; ElapsedMs = $sw.ElapsedMilliseconds }
            }
        }
        catch { }
        Start-Sleep -Seconds $PollSec
    }
    $sw.Stop()
    throw "Play mode did not stabilize within ${TimeoutSec}s (isPlaying / isPlayModeChanging / isCompiling)."
}

function Wait-McpActiveScene {
    param([string]$Root, [string]$SceneName, [int]$TimeoutSec, [double]$PollSec)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $s = Invoke-RestMethod -Uri "$Root/health" -Method Get -ErrorAction Stop
            if ($s.activeScene -eq $SceneName) {
                $sw.Stop()
                return @{ State = $s; ElapsedMs = $sw.ElapsedMilliseconds }
            }
        }
        catch { }
        Start-Sleep -Seconds $PollSec
    }
    $sw.Stop()
    throw "Active scene did not become '$SceneName' within ${TimeoutSec}s."
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$resultPathResolved = if ([string]::IsNullOrWhiteSpace($ResultJsonPath)) { Get-DefaultResultJsonPath } else { $ResultJsonPath }

try {
    $script:CurrentStep = "health_precheck"
    Write-Host "MCP: $root" -ForegroundColor Cyan
    $h = Invoke-RestMethod -Uri "$root/health" -Method Get
    if (-not $h.ok) { throw "MCP /health not ok." }
    $script:LastHealth = $h
    Add-TestStep -Name "health_precheck" -Ok $true -Detail "scene=$($h.activeScene)"
    Write-Host "Active scene (before): $($h.activeScene)" -ForegroundColor Gray

    $lobbyScenePath = "Assets/Scenes/JG_LobbyScene.unity"
    if ($h.isPlaying -and $h.activeScene -ne "JG_LobbyScene") {
        $script:CurrentStep = "stop_play_wrong_scene"
        Write-Host "Stopping play (scene=$($h.activeScene), need JG_LobbyScene)..." -ForegroundColor Yellow
        Invoke-McpJson -Root $root -SubPath "/play/stop"
        $stopDeadline = (Get-Date).AddSeconds(90)
        while ((Get-Date) -lt $stopDeadline) {
            try {
                $h = Invoke-RestMethod -Uri "$root/health" -Method Get -ErrorAction Stop
                $isPlayModeChanging = Get-McpPlayModeChanging -State $h
                if (-not $h.isPlaying -and -not $isPlayModeChanging) { break }
            }
            catch { }
            Start-Sleep -Seconds $PlayModePollSec
        }
        if ($h.isPlaying) { throw "Could not exit play mode to open lobby scene." }
        Add-TestStep -Name "stop_play_for_lobby" -Ok $true
    }

    if (-not $h.isPlaying) {
        $script:CurrentStep = "scene_open_play_start"
        Invoke-McpJson -Root $root -SubPath "/scene/open" -Body @{
            scenePath                 = $lobbyScenePath
            saveCurrentSceneIfDirty   = $true
        }
        Invoke-McpJson -Root $root -SubPath "/play/start"
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true
    }
    else {
        Write-Host "Already in play on JG_LobbyScene — skipping scene/open and play/start." -ForegroundColor Gray
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true -Detail "skipped_already_in_lobby_play"
    }

    $script:CurrentStep = "play_mode_ready"
    Write-Host "Waiting for play mode + first frame (poll /health, timeout ${PlayModeReadyTimeoutSec}s)..." -ForegroundColor Yellow
    $pm = Wait-McpPlayModeReady -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "play_mode_ready" -Ok $true -ElapsedMs $pm.ElapsedMs

    $script:CurrentStep = "lobby_scene_active"
    Write-Host "Waiting for active scene JG_LobbyScene (timeout ${LobbySceneActiveTimeoutSec}s)..." -ForegroundColor Yellow
    $ls = Wait-McpActiveScene -Root $root -SceneName "JG_LobbyScene" -TimeoutSec $LobbySceneActiveTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "lobby_scene_active" -Ok $true -ElapsedMs $ls.ElapsedMs

    $script:CurrentStep = "lobby_settle_wait"
    Write-Host "Lobby scene active. Waiting ${PhotonWaitSec}s before Create Room..." -ForegroundColor Yellow
    $swS = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds $PhotonWaitSec
    $swS.Stop()
    Add-TestStep -Name "lobby_settle_wait" -Ok $true -Detail "seconds=$PhotonWaitSec" -ElapsedMs $swS.ElapsedMilliseconds

    $script:CurrentStep = "create_room"
    $invokeCreate = @{ path = "/Canvas/lobby/RoomListView/Header/CreateRoomButton" }
    Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body $invokeCreate
    Add-TestStep -Name "create_room" -Ok $true
    Write-Host "CreateRoom invoked. Waiting ${AfterCreateWaitSec}s..." -ForegroundColor Yellow
    Start-Sleep -Seconds $AfterCreateWaitSec

    $script:CurrentStep = "ready"
    $invokeReady = @{ path = "/Canvas/lobby/RoomDetailPanel/ReadyButton" }
    Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body $invokeReady
    Add-TestStep -Name "ready" -Ok $true
    Start-Sleep -Seconds $AfterReadyWaitSec

    $script:CurrentStep = "start"
    $invokeStart = @{ path = "/Canvas/lobby/RoomDetailPanel/StartGameButton" }
    Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body $invokeStart
    Add-TestStep -Name "start" -Ok $true
    Write-Host "StartGame invoked. Waiting for LoadLevel..." -ForegroundColor Yellow
    Start-Sleep -Seconds 6

    $h2 = Invoke-RestMethod -Uri "$root/health" -Method Get
    $script:LastHealth = $h2
    Write-Host "Active scene (after): $($h2.activeScene)  isPlaying: $($h2.isPlaying)" -ForegroundColor Green
    $inGame = ($h2.activeScene -eq "JG_GameScene")
    Add-TestStep -Name "game_scene" -Ok $inGame -Detail "activeScene=$($h2.activeScene)"
    if (-not $inGame) {
        $script:Failure = @{ step = "game_scene"; message = "Expected JG_GameScene, was $($h2.activeScene)." }
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
        Invoke-McpJson -Root $root -SubPath "/play/stop"
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
