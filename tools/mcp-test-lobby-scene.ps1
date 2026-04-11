# LobbyScene → (선택) GameScene 시작 스킬 선택까지 MCP 단계 테스트. 스크린샷 + /console/logs + JSON 요약
# 전제: MCP 브리지, Photon, 씬 입력 기본값(방 이름 등)
# 플레이 안정화 → /health 로 LobbyScene 확인 → PhotonWaitSec(기본 3초) 후 버튼 시퀀스.
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

. (Join-Path $PSScriptRoot "mcp-test-common.ps1")

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

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$resultPathResolved = if ([string]::IsNullOrWhiteSpace($ResultJsonPath)) { Get-DefaultResultJsonPath } else { $ResultJsonPath }

try {
    $script:CurrentStep = "health_precheck"
    Write-Host "MCP: $root" -ForegroundColor Cyan
    $h = Invoke-McpGetJson -Root $root -SubPath "/health"
    if (-not $h.ok) { throw "MCP /health not ok." }
    $script:LastHealth = $h
    Add-TestStep -Name "health_precheck" -Ok $true -Detail "scene=$($h.activeScene)"
    Write-Host "Before: scene=$($h.activeScene) playing=$($h.isPlaying) compiling=$($h.isCompiling)"

    Write-McpRecentConsole -Root $root -Label "before flow" -LogLimit 40 -ErrorLimit 10

    $lobbyScenePath = "Assets/Scenes/LobbyScene.unity"
    if ($h.isPlaying -and $h.activeScene -ne "LobbyScene") {
        $script:CurrentStep = "stop_play_wrong_scene"
        Write-Host "Stopping play (was $($h.activeScene))..." -ForegroundColor Yellow
        $stopResult = Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 -PollSec $PlayModePollSec
        $h = $stopResult.StoppedState
        Add-TestStep -Name "stop_play_for_lobby" -Ok (-not $h.isPlaying) -Detail "was_not_lobby_scene"
    }

    if (-not $h.isPlaying) {
        $script:CurrentStep = "scene_open_play_start"
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $lobbyScenePath -SaveCurrentSceneIfDirty $true -TimeoutSec 60 -PollSec $PlayModePollSec | Out-Null
        Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec | Out-Null
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true
    }
    else {
        Write-Host "Already playing LobbyScene — skip open/start." -ForegroundColor Gray
        Add-TestStep -Name "scene_open_and_play_start" -Ok $true -Detail "skipped_already_in_lobby_play"
    }

    $script:CurrentStep = "play_mode_ready"
    Write-Host "Wait play mode ready..." -ForegroundColor Yellow
    $pm = Wait-McpPlayModeReady -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "play_mode_ready" -Ok $true -ElapsedMs $pm.ElapsedMs

    $script:CurrentStep = "lobby_scene_active"
    Write-Host "Wait active scene LobbyScene (timeout ${LobbySceneActiveTimeoutSec}s)..." -ForegroundColor Yellow
    $ls = Wait-McpSceneActive -Root $root -SceneName "LobbyScene" -TimeoutSec $LobbySceneActiveTimeoutSec -PollSec $PlayModePollSec
    Add-TestStep -Name "lobby_scene_active" -Ok $true -ElapsedMs $ls.ElapsedMs

    $script:CurrentStep = "lobby_settle_wait"
    Write-Host "Lobby scene active. Waiting ${PhotonWaitSec}s before Create Room..." -ForegroundColor Yellow
    $swSettle = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds $PhotonWaitSec
    $swSettle.Stop()
    Add-TestStep -Name "lobby_settle_wait" -Ok $true -Detail "seconds=$PhotonWaitSec" -ElapsedMs $swSettle.ElapsedMilliseconds

    Capture-McpStep -Root $root -Name "01-after-lobby-settle" | Out-Null
    Write-McpRecentConsole -Root $root -Label "after play + lobby scene + settle wait" -LogLimit 50 -ErrorLimit 12

    $script:CurrentStep = "create_room"
    $createSpec = Get-McpUiPath -Key "Lobby.CreateRoom"
    $createResult = Invoke-McpButton -Root $root -Path $createSpec.Path -FallbackName $createSpec.FallbackName -Label "Create Room"
    Write-Host "CreateRoom invoked: $($createResult.path)" -ForegroundColor Yellow
    Add-TestStep -Name "create_room" -Ok $true
    Start-Sleep -Seconds $AfterCreateWaitSec
    Capture-McpStep -Root $root -Name "02-after-create-room" | Out-Null
    Write-McpRecentConsole -Root $root -Label "after CreateRoom" -LogLimit 50 -ErrorLimit 12

    $script:CurrentStep = "ready"
    $readySpec = Get-McpUiPath -Key "Lobby.Ready"
    $readyResult = Invoke-McpButton -Root $root -Path $readySpec.Path -FallbackName $readySpec.FallbackName -Label "Ready"
    Write-Host "Ready invoked: $($readyResult.path)" -ForegroundColor Yellow
    Add-TestStep -Name "ready" -Ok $true
    Start-Sleep -Seconds $AfterReadyWaitSec
    Capture-McpStep -Root $root -Name "03-after-ready" | Out-Null
    Write-McpRecentConsole -Root $root -Label "after Ready" -LogLimit 50 -ErrorLimit 12

    $script:CurrentStep = "start"
    $startSpec = Get-McpUiPath -Key "Lobby.StartGame"
    $startResult = Invoke-McpButton -Root $root -Path $startSpec.Path -FallbackName $startSpec.FallbackName -Label "Start Game"
    Write-Host "StartGame invoked: $($startResult.path)" -ForegroundColor Yellow
    Write-Host "StartGame invoked. Waiting ${AfterStartWaitSec}s..." -ForegroundColor Yellow
    Add-TestStep -Name "start" -Ok $true
    Start-Sleep -Seconds $AfterStartWaitSec
    Capture-McpStep -Root $root -Name "04-after-start-wait" | Out-Null
    Write-McpRecentConsole -Root $root -Label "after Start + wait" -LogLimit 80 -ErrorLimit 20

    $h2 = Invoke-McpGetJson -Root $root -SubPath "/health"
    $script:LastHealth = $h2
    Write-Host "`nAfter: scene=$($h2.activeScene) playing=$($h2.isPlaying)" -ForegroundColor Green

    for ($i = 1; $i -le 5; $i++) {
        if ($h2.activeScene -eq "GameScene") { break }
        Start-Sleep -Seconds 3
        $h2 = Invoke-McpGetJson -Root $root -SubPath "/health"
        $script:LastHealth = $h2
        Write-Host "Poll $i : scene=$($h2.activeScene)"
    }

    $inGame = ($h2.activeScene -eq "GameScene")
    Add-TestStep -Name "game_scene" -Ok $inGame -Detail "activeScene=$($h2.activeScene)"
    if (-not $inGame -and $null -eq $script:Failure) {
        $script:Failure = @{ step = "game_scene"; message = "Expected GameScene, was $($h2.activeScene)." }
    }

    if ($inGame) {
        Capture-McpStep -Root $root -Name "05-game-scene" | Out-Null
        Write-McpRecentConsole -Root $root -Label "on GameScene" -LogLimit 60 -ErrorLimit 15

        if ($CompleteGameSceneStartSkills) {
            $script:CurrentStep = "start_skills"
            $p0 = Get-McpUiPath -Key "Game.StartSkillButton0"
            $p1 = Get-McpUiPath -Key "Game.StartSkillButton1"
            $pConfirm = Get-McpUiPath -Key "Game.StartSkillConfirm"
            Write-Host "`n--- Game scene: start skill selection (2 picks + confirm) ---" -ForegroundColor Cyan
            Start-Sleep -Seconds 1
            try {
                $pick0 = Invoke-McpButton -Root $root -Path $p0.Path -FallbackName $p0.FallbackName -Label "Start Skill Button 0"
                Start-Sleep -Milliseconds 400
                $pick1 = Invoke-McpButton -Root $root -Path $p1.Path -FallbackName $p1.FallbackName -Label "Start Skill Button 1"
                Start-Sleep -Milliseconds 400
                $confirm = Invoke-McpButton -Root $root -Path $pConfirm.Path -FallbackName $pConfirm.FallbackName -Label "Start Skill Confirm"
                Write-Host "Start skill picks: $($pick0.path), $($pick1.path), confirm=$($confirm.path)" -ForegroundColor Yellow
                Start-Sleep -Seconds 2
                Capture-McpStep -Root $root -Name "06-after-start-skills-confirmed" | Out-Null
                Write-McpRecentConsole -Root $root -Label "after start skill confirm" -LogLimit 80 -ErrorLimit 20
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
        $script:Failure = @{ step = "game_scene"; message = "Expected GameScene for start skills." }
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
