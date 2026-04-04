# JG_LobbyScene → (선택) JG_GameScene 시작 스킬 선택까지 MCP 단계 테스트. 스크린샷 + /console/logs + JSON 요약
# 전제: MCP 브리지, Photon, 씬 입력 기본값(방 이름 등)
# 플레이 안정화 → /health 로 JG_LobbyScene 확인 → PhotonWaitSec(기본 3초) 후 버튼 시퀀스.
# JSON: -ResultJsonPath (기본 Temp/UnityMcp/last-lobby-scene-test.json), -WriteJsonToStdout

[CmdletBinding()]
param(
    [string]$BaseUrl,
    [int]$PhotonWaitSec = 3,
    [int]$LobbySceneActiveTimeoutSec = 90,
    [int]$AfterCreateWaitSec = 5,
    [int]$AfterReadyWaitSec = 2,
    [int]$AfterStartWaitSec = 10,
    [int]$PlayModeReadyTimeoutSec = 120,
    [double]$PlayModePollSec = 0.5,
    [string]$ResultJsonPath = "",
    [switch]$WriteJsonToStdout,
    [switch]$CompleteGameSceneStartSkills,
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
    Join-Path $PSScriptRoot "..\Temp\UnityMcp\last-lobby-scene-test.json"
}

function Add-TestStep {
    param(
        [string]$Name,
        [bool]$Ok,
        [string]$Detail = $null,
        [long]$ElapsedMs = -1
    )
    $row = [ordered]@{
        name = $Name
        ok   = $Ok
    }
    if (-not [string]::IsNullOrEmpty($Detail)) {
        $row.detail = $Detail
    }
    if ($ElapsedMs -ge 0) {
        $row.elapsedMs = $ElapsedMs
    }
    [void]$script:TestSteps.Add([PSCustomObject]$row)
    if (-not $Ok) {
        $script:TestOk = $false
    }
}

function Write-TestResultJson {
    param(
        [string]$Root,
        [string]$JsonPath,
        [bool]$ToStdout,
        [bool]$CompleteSkills,
        [bool]$NoStop
    )
    if ([string]::IsNullOrWhiteSpace($JsonPath)) {
        $JsonPath = Get-DefaultResultJsonPath
    }
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
            completeGameSceneStartSkills = [bool]$CompleteSkills
            noStopPlay                   = [bool]$NoStop
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
    if ($ToStdout) {
        Write-Output $json
    }
}

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)
    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) { return $ExplicitBaseUrl.TrimEnd("/") }
    $portFile = Join-Path $PSScriptRoot "..\ProjectSettings\UnityMcpPort.txt"
    if (Test-Path $portFile) {
        $t = (Get-Content $portFile -Raw).Trim()
        if ($t -match '^\d+$') { return "http://127.0.0.1:$t" }
    }
    return "http://127.0.0.1:51234"
}

function Invoke-McpJson {
    param([string]$Root, [string]$SubPath, [object]$Body = $null)
    $uri = "$Root$SubPath"
    if ($null -eq $Body) { return Invoke-RestMethod -Method Post -Uri $uri }
    return Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body ($Body | ConvertTo-Json -Compress)
}

function Get-McpPlayModeChanging {
    param([object]$State)
    if ($null -ne $State.PSObject.Properties["isPlayModeChanging"]) { return [bool]$State.isPlayModeChanging }
    if ($null -ne $State.PSObject.Properties["isPlayingOrWillChange"]) { return [bool]$State.isPlayingOrWillChange }
    return $false
}

function Wait-McpPlayModeReady {
    param([string]$Root, [int]$TimeoutSec, [double]$PollSec)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $s = Invoke-RestMethod -Uri "$Root/health" -Method Get -ErrorAction Stop
            if ($s.isPlaying -and -not (Get-McpPlayModeChanging -State $s) -and -not $s.isCompiling) {
                $sw.Stop()
                return @{ State = $s; ElapsedMs = $sw.ElapsedMilliseconds }
            }
        }
        catch { }
        Start-Sleep -Seconds $PollSec
    }
    $sw.Stop()
    throw "Play mode did not stabilize within ${TimeoutSec}s."
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

function Write-McpConsoleLogs {
    param([string]$Root, [string]$Label, [int]$Limit = 80)
    Write-Host "`n=== Console logs: $Label ===" -ForegroundColor Magenta
    try {
        $c = Invoke-RestMethod -Uri "$Root/console/logs?limit=$Limit" -Method Get
        Write-Host "count=$($c.count)"
        foreach ($i in $c.items) {
            Write-Host "[$($i.type)] $($i.message)"
        }
    }
    catch {
        Write-Host "console/logs failed: $($_.Exception.Message)"
    }
}

function Capture-McpStep {
    param([string]$Root, [string]$Name)
    $rel = "Temp/UnityMcp/Screenshots/lobby-scene-test-$Name.png"
    try {
        $r = Invoke-McpJson -Root $Root -SubPath "/screenshot/capture" -Body @{ outputPath = $rel; overwrite = $true }
        Write-Host "Screenshot: $($r.relativePath)" -ForegroundColor Green
        if ($r.relativePath) {
            [void]$script:TestScreenshots.Add([string]$r.relativePath)
        }
        return $true
    }
    catch {
        Write-Host "screenshot $Name failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Invoke-McpUiButton {
    param([string]$Root, [string]$Path)
    Invoke-McpJson -Root $Root -SubPath "/ui/button/invoke" -Body @{ path = $Path }
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
    Write-Host "Before: scene=$($h.activeScene) playing=$($h.isPlaying) compiling=$($h.isCompiling)"

    Write-McpConsoleLogs -Root $root -Label "before flow" -Limit 40

    $lobbyScenePath = "Assets/Scenes/JG_LobbyScene.unity"
    if ($h.isPlaying -and $h.activeScene -ne "JG_LobbyScene") {
        $script:CurrentStep = "stop_play_wrong_scene"
        Write-Host "Stopping play (was $($h.activeScene))..." -ForegroundColor Yellow
        Invoke-McpJson -Root $root -SubPath "/play/stop"
        $stopDeadline = (Get-Date).AddSeconds(90)
        while ((Get-Date) -lt $stopDeadline) {
            try {
                $h = Invoke-RestMethod -Uri "$root/health" -Method Get -ErrorAction Stop
                if (-not $h.isPlaying -and -not (Get-McpPlayModeChanging -State $h)) { break }
            }
            catch { }
            Start-Sleep -Seconds $PlayModePollSec
        }
        Add-TestStep -Name "stop_play_for_lobby" -Ok (-not $h.isPlaying) -Detail "was_not_lobby_scene"
    }

    if (-not $h.isPlaying) {
        $script:CurrentStep = "scene_open_play_start"
        Invoke-McpJson -Root $root -SubPath "/scene/open" -Body @{
            scenePath               = $lobbyScenePath
            saveCurrentSceneIfDirty = $true
        }
        Invoke-McpJson -Root $root -SubPath "/play/start"
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true
    }
    else {
        Write-Host "Already playing JG_LobbyScene — skip open/start." -ForegroundColor Gray
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true -Detail "skipped_already_in_lobby_play"
    }

    $script:CurrentStep = "play_mode_ready"
    Write-Host "Wait play mode ready..." -ForegroundColor Yellow
    $pm = Wait-McpPlayModeReady -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "play_mode_ready" -Ok $true -ElapsedMs $pm.ElapsedMs

    $script:CurrentStep = "lobby_scene_active"
    Write-Host "Wait active scene JG_LobbyScene (timeout ${LobbySceneActiveTimeoutSec}s)..." -ForegroundColor Yellow
    $ls = Wait-McpActiveScene -Root $root -SceneName "JG_LobbyScene" -TimeoutSec $LobbySceneActiveTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "lobby_scene_active" -Ok $true -ElapsedMs $ls.ElapsedMs

    $script:CurrentStep = "lobby_settle_wait"
    Write-Host "Lobby scene active. Waiting ${PhotonWaitSec}s before Create Room..." -ForegroundColor Yellow
    $swSettle = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds $PhotonWaitSec
    $swSettle.Stop()
    Add-TestStep -Name "lobby_settle_wait" -Ok $true -Detail "seconds=$PhotonWaitSec" -ElapsedMs $swSettle.ElapsedMilliseconds

    Capture-McpStep -Root $root -Name "01-after-lobby-settle" | Out-Null
    Write-McpConsoleLogs -Root $root -Label "after play + lobby scene + settle wait" -Limit 50

    $script:CurrentStep = "create_room"
    Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body @{ path = "/Canvas/lobby/RoomListView/Header/CreateRoomButton" }
    Write-Host "CreateRoom invoked." -ForegroundColor Yellow
    Add-TestStep -Name "create_room" -Ok $true
    Start-Sleep -Seconds $AfterCreateWaitSec
    Capture-McpStep -Root $root -Name "02-after-create-room" | Out-Null
    Write-McpConsoleLogs -Root $root -Label "after CreateRoom" -Limit 50

    $script:CurrentStep = "ready"
    Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body @{ path = "/Canvas/lobby/RoomDetailPanel/ReadyButton" }
    Add-TestStep -Name "ready" -Ok $true
    Start-Sleep -Seconds $AfterReadyWaitSec
    Capture-McpStep -Root $root -Name "03-after-ready" | Out-Null
    Write-McpConsoleLogs -Root $root -Label "after Ready" -Limit 50

    $script:CurrentStep = "start"
    Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body @{ path = "/Canvas/lobby/RoomDetailPanel/StartGameButton" }
    Write-Host "StartGame invoked. Waiting ${AfterStartWaitSec}s..." -ForegroundColor Yellow
    Add-TestStep -Name "start" -Ok $true
    Start-Sleep -Seconds $AfterStartWaitSec
    Capture-McpStep -Root $root -Name "04-after-start-wait" | Out-Null
    Write-McpConsoleLogs -Root $root -Label "after Start + wait" -Limit 80

    $h2 = Invoke-RestMethod -Uri "$root/health" -Method Get
    $script:LastHealth = $h2
    Write-Host "`nAfter: scene=$($h2.activeScene) playing=$($h2.isPlaying)" -ForegroundColor Green

    for ($i = 1; $i -le 5; $i++) {
        if ($h2.activeScene -eq "JG_GameScene") { break }
        Start-Sleep -Seconds 3
        $h2 = Invoke-RestMethod -Uri "$root/health" -Method Get
        $script:LastHealth = $h2
        Write-Host "Poll $i : scene=$($h2.activeScene)"
    }

    $inGame = ($h2.activeScene -eq "JG_GameScene")
    Add-TestStep -Name "game_scene" -Ok $inGame -Detail "activeScene=$($h2.activeScene)"
    if (-not $inGame -and $null -eq $script:Failure) {
        $script:Failure = @{ step = "game_scene"; message = "Expected JG_GameScene, was $($h2.activeScene)." }
    }

    if ($inGame) {
        Capture-McpStep -Root $root -Name "05-game-scene" | Out-Null
        Write-McpConsoleLogs -Root $root -Label "on JG_GameScene" -Limit 60

        if ($CompleteGameSceneStartSkills) {
            $script:CurrentStep = "start_skills"
            $p0 = "/StartSkillSelectionCanvas/Panel/ButtonGrid/SkillButton0"
            $p1 = "/StartSkillSelectionCanvas/Panel/ButtonGrid/SkillButton1"
            $pConfirm = "/StartSkillSelectionCanvas/Panel/ConfirmButton"
            Write-Host "`n--- Game scene: start skill selection (2 picks + confirm) ---" -ForegroundColor Cyan
            Start-Sleep -Seconds 1
            try {
                Invoke-McpUiButton -Root $root -Path $p0
                Start-Sleep -Milliseconds 400
                Invoke-McpUiButton -Root $root -Path $p1
                Start-Sleep -Milliseconds 400
                Invoke-McpUiButton -Root $root -Path $pConfirm
                Start-Sleep -Seconds 2
                Capture-McpStep -Root $root -Name "06-after-start-skills-confirmed" | Out-Null
                Write-McpConsoleLogs -Root $root -Label "after start skill confirm" -Limit 80
                Add-TestStep -Name "start_skills" -Ok $true
            }
            catch {
                Write-Host "Game scene skill UI invoke failed: $($_.Exception.Message)" -ForegroundColor Red
                Add-TestStep -Name "start_skills" -Ok $false -Detail $_.Exception.Message
                $script:Failure = @{ step = "start_skills"; message = $_.Exception.Message }
            }
        }
    }
    elseif ($CompleteGameSceneStartSkills) {
        Add-TestStep -Name "start_skills" -Ok $false -Detail "skipped_not_in_game_scene"
        $script:TestOk = $false
        $script:Failure = @{ step = "game_scene"; message = "Expected JG_GameScene for start skills." }
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

    Write-Host "`nDone. PNG under Temp/UnityMcp/Screenshots/lobby-scene-test-*.png" -ForegroundColor Cyan
    if (-not $CompleteGameSceneStartSkills) {
        Write-Host "Tip: 게임 씬에서 스킬 2개+확인까지 포함하려면 -CompleteGameSceneStartSkills" -ForegroundColor DarkGray
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
    Write-TestResultJson -Root $root -JsonPath $resultPathResolved -ToStdout:$WriteJsonToStdout -CompleteSkills:$CompleteGameSceneStartSkills -NoStop:$NoStopPlay
}

if (-not $script:TestOk) {
    exit 1
}
