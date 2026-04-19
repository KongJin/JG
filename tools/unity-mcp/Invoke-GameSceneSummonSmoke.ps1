param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$RoomNamePrefix = "CodexSummonSmoke",
    [string]$OutputPath = "artifacts/unity/game-scene-summon-smoke.png",
    [string]$ResultPath = "artifacts/unity/game-scene-summon-smoke-result.json",
    [int]$TimeoutSec = 90
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$startedPlayHere = $false

function Save-CurrentGarageDraft {
    $saveButtonPath = "/Canvas/GaragePageRoot/GarageContentRow/RightRail/ResultPane/SaveButton"

    Wait-McpCondition `
        -Description "Save Draft button to become interactable" `
        -TimeoutSec $TimeoutSec `
        -Condition { (Get-McpUiButtonInfo -Root $root -Path $saveButtonPath).interactable }

    Invoke-McpUiInvoke -Root $root -Path $saveButtonPath -Method "click" | Out-Null

    Wait-McpCondition `
        -Description "Save Draft request to finish" `
        -TimeoutSec $TimeoutSec `
        -Condition { -not (Get-McpUiButtonInfo -Root $root -Path $saveButtonPath).interactable }

    Start-Sleep -Milliseconds 500
}

function Ensure-RoomCreated {
    $roomNameInputPath = "/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/RoomNameInput/Field"
    $createRoomButtonPath = "/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/CreateRoomButton"
    $roomDetailPanelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel"
    $bannerMessagePath = "/Canvas/SceneErrorPresenter/Banner/BannerMessage"

    if (Get-McpUiActiveInHierarchy -Root $root -Path $roomDetailPanelPath) {
        return "already-in-room"
    }

    $roomName = "{0}-{1}-{2}" -f $RoomNamePrefix, (Get-Date -Format "yyyyMMddHHmmssfff"), (Get-Random -Minimum 1000 -Maximum 9999)
    Invoke-McpSetUiValue -Root $root -Path $roomNameInputPath -Value $roomName
    Invoke-McpUiInvoke -Root $root -Path $createRoomButtonPath -Method "click" | Out-Null

    Wait-McpCondition `
        -Description "room creation result" `
        -TimeoutSec 10 `
        -Condition {
            (Get-McpUiActiveInHierarchy -Root $root -Path $roomDetailPanelPath) -or
            (Get-McpUiActiveInHierarchy -Root $root -Path $bannerMessagePath)
        }

    if (-not (Get-McpUiActiveInHierarchy -Root $root -Path $roomDetailPanelPath)) {
        throw "Room creation failed: $(Get-McpUiTextValue -Root $root -Path $bannerMessagePath)"
    }

    return $roomName
}

function Ensure-ReadyBaseline {
    $readyLabelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton/Label"

    $readyLabel = Get-McpUiTextValue -Root $root -Path $readyLabelPath
    if ($readyLabel -in @("Ready", "Cancel")) {
        return
    }

    foreach ($slot in 1..6) {
        $slotTitlePath = "/Canvas/GaragePageRoot/GarageContentRow/RosterListPane/GarageSlot{0}/Title" -f $slot
        $slotTitle = Get-McpUiTextValue -Root $root -Path $slotTitlePath
        if ($slotTitle -ne "Empty Hangar") {
            continue
        }

        Invoke-McpUiInvoke -Root $root -Path ("/Canvas/GaragePageRoot/GarageContentRow/RosterListPane/GarageSlot{0}" -f $slot) -Method "click" | Out-Null
        Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/GarageContentRow/UnitEditorPane/FRAMECard/FRAMEValuePanel/FRAMENextButton" -Method "click" | Out-Null
        Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/GarageContentRow/UnitEditorPane/FIREPOWERCard/FIREPOWERValuePanel/FIREPOWERNextButton" -Method "click" | Out-Null
        Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/GarageContentRow/UnitEditorPane/MOBILITYCard/MOBILITYValuePanel/MOBILITYNextButton" -Method "click" | Out-Null
        Save-CurrentGarageDraft

        $readyLabel = Get-McpUiTextValue -Root $root -Path $readyLabelPath
        if ($readyLabel -in @("Ready", "Cancel")) {
            return
        }
    }

    throw "Ready never unlocked after filling empty Garage slots."
}

try {
    $session = Invoke-McpPrepareCodexLobbyPlaySession `
        -Root $root `
        -ScenePath $ScenePath `
        -LoginLoadingPanelPath "/Canvas/LoginLoadingOverlay/LoadingPanel" `
        -TimeoutSec $TimeoutSec
    $startedPlayHere = $true

    Wait-McpPhotonLobbyReady -Root $root -TimeoutSec $TimeoutSec -LogLimit 120

    $roomName = Ensure-RoomCreated
    Ensure-ReadyBaseline

    $readyButtonPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton"
    $readyLabelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton/Label"
    $startGameButtonPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/StartGameButton"

    if ((Get-McpUiTextValue -Root $root -Path $readyLabelPath) -eq "Ready") {
        Invoke-McpUiInvoke -Root $root -Path $readyButtonPath -Method "click" | Out-Null
        Wait-McpCondition `
            -Description "Ready toggle to switch on" `
            -TimeoutSec 30 `
            -Condition { (Get-McpUiTextValue -Root $root -Path $readyLabelPath) -eq "Cancel" }
    }

    Wait-McpCondition `
        -Description "StartGame button to become interactable" `
        -TimeoutSec 30 `
        -Condition { (Get-McpUiButtonInfo -Root $root -Path $startGameButtonPath).interactable }

    Invoke-McpUiInvoke -Root $root -Path $startGameButtonPath -Method "click" | Out-Null

    Wait-McpCondition `
        -Description "GameScene active" `
        -TimeoutSec $TimeoutSec `
        -Condition { try { (Invoke-McpGetJson -Root $root -SubPath "/scene/current").name -eq "GameScene" } catch { $false } }

    Start-Sleep -Seconds 2

    $invoke = Invoke-McpUiInvoke -Root $root -Path "/HudCanvas/UnitSummonUi/SlotRow/UnitSlotTemplate(Clone)" -Method "click"
    Start-Sleep -Seconds 2

    $battleEntity = Invoke-RestMethod `
        -Method Post `
        -Uri "$root/gameobject/find" `
        -ContentType "application/json" `
        -Body (@{
            name = "BattleEntity(Clone)"
            lightweight = $false
        } | ConvertTo-Json -Compress)

    $capture = Invoke-McpScreenshotCapture -Root $root -OutputPath $OutputPath -Overwrite
    $logs = Get-McpRecentLogs -Root $root -Limit 240
    $errors = Get-McpRecentErrors -Root $root -Limit 80

    $result = [PSCustomObject]@{
        success = $true
        root = $root
        play = $session.play
        roomName = $roomName
        invoke = $invoke
        battleEntity = $battleEntity
        screenshot = $capture
        recentLogCount = $logs.count
        recentErrorCount = $errors.count
        recentLogs = @($logs.items | Select-Object -Last 60 | ForEach-Object { "[{0}] {1}" -f $_.type, $_.message })
        recentErrors = @($errors.items | Select-Object -Last 40 | ForEach-Object { $_.message })
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $ResultPath -Parent) | Out-Null
    $result | ConvertTo-Json -Depth 12 | Set-Content -Path $ResultPath -Encoding UTF8
    $result | ConvertTo-Json -Depth 10
}
finally {
    if ($startedPlayHere) {
        try {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }
        catch {
            Write-Warning ("Failed to stop Play Mode after summon smoke: {0}" -f $_.Exception.Message)
        }
    }
}
