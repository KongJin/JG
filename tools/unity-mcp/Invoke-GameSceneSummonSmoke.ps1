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
$sceneName = [System.IO.Path]::GetFileNameWithoutExtension($ScenePath)
$startedPlayHere = $false

function Convert-McpUiStateEntriesToMap {
    param([object]$Response)

    $map = @{}
    foreach ($entry in $Response.state) {
        if ($entry -match '^\[(.*?),\s?(.*)\]$') {
            $map[$matches[1]] = $matches[2]
        }
    }

    return $map
}

function Get-McpUiStateMap {
    param([string]$Path)

    return Convert-McpUiStateEntriesToMap (Get-McpUiElementState -Root $root -Path $Path)
}

function Get-McpUiTextValue {
    param([string]$Path)

    $state = Get-McpUiStateMap -Path $Path
    return [string]$state["text"]
}

function Get-McpUiButtonInfo {
    param([string]$Path)

    $state = Get-McpUiStateMap -Path $Path

    return [PSCustomObject]@{
        path = [string]$state["path"]
        activeInHierarchy = ([string]$state["activeInHierarchy"]) -eq "True"
        interactable = ([string]$state["interactable"]) -eq "True"
    }
}

function Get-McpUiActiveInHierarchy {
    param([string]$Path)

    $state = Get-McpUiStateMap -Path $Path
    return ([string]$state["activeInHierarchy"]) -eq "True"
}

function Wait-ForCondition {
    param(
        [scriptblock]$Condition,
        [string]$Description,
        [int]$TimeoutSecValue = 20,
        [int]$PollMs = 250
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSecValue)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) {
            return
        }

        Start-Sleep -Milliseconds $PollMs
    }

    throw "Timed out waiting for $Description."
}

function Wait-ForPhotonLobbyReady {
    Wait-ForCondition `
        -Description "Photon lobby join log" `
        -TimeoutSecValue $TimeoutSec `
        -Condition {
            $logs = Get-McpRecentLogs -Root $root -Limit 120
            foreach ($item in $logs.items) {
                if ($item.message -like "*Joined lobby. Ready for matchmaking.*") {
                    return $true
                }
            }

            return $false
        }
}

function Set-McpInputFieldValue {
    param(
        [string]$Path,
        [string]$Value
    )

    Invoke-RestMethod `
        -Method Post `
        -Uri "$root/ui/set-value" `
        -ContentType "application/json" `
        -Body (@{
            path = $Path
            value = $Value
        } | ConvertTo-Json -Compress) | Out-Null
}

function Save-CurrentGarageDraft {
    $saveButtonPath = "/Canvas/GaragePageRoot/ResultPane/SaveButton"
    $saveLabelPath = "/Canvas/GaragePageRoot/ResultPane/SaveButton/Text (TMP)"

    Wait-ForCondition `
        -Description "Save Draft button to become interactable" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiButtonInfo -Path $saveButtonPath).interactable }

    Invoke-McpUiInvoke -Root $root -Path $saveButtonPath -Method "click" | Out-Null

    Wait-ForCondition `
        -Description "Save Draft request to finish" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Path $saveLabelPath) -ne "Saving..." }

    Start-Sleep -Milliseconds 500
}

function Ensure-RoomCreated {
    $roomNameInputPath = "/Canvas/LobbyPageRoot/RoomListPanel/RoomNameInput/Field"
    $createRoomButtonPath = "/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomButton"
    $roomDetailPanelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel"
    $bannerMessagePath = "/Canvas/SceneErrorPresenter/Banner/BannerMessage"

    if (Get-McpUiActiveInHierarchy -Path $roomDetailPanelPath) {
        return "already-in-room"
    }

    $roomName = "{0}-{1}-{2}" -f $RoomNamePrefix, (Get-Date -Format "yyyyMMddHHmmssfff"), (Get-Random -Minimum 1000 -Maximum 9999)
    Set-McpInputFieldValue -Path $roomNameInputPath -Value $roomName
    Invoke-McpUiInvoke -Root $root -Path $createRoomButtonPath -Method "click" | Out-Null

    Wait-ForCondition `
        -Description "room creation result" `
        -TimeoutSecValue 10 `
        -Condition {
            (Get-McpUiActiveInHierarchy -Path $roomDetailPanelPath) -or
            (Get-McpUiActiveInHierarchy -Path $bannerMessagePath)
        }

    if (-not (Get-McpUiActiveInHierarchy -Path $roomDetailPanelPath)) {
        throw "Room creation failed: $(Get-McpUiTextValue -Path $bannerMessagePath)"
    }

    return $roomName
}

function Ensure-ReadyBaseline {
    $readyLabelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton/Label"

    $readyLabel = Get-McpUiTextValue -Path $readyLabelPath
    if ($readyLabel -in @("Ready", "Cancel")) {
        return
    }

    foreach ($slot in 1..6) {
        $slotTitlePath = "/Canvas/GaragePageRoot/RosterListPane/GarageSlot{0}/Title" -f $slot
        $slotTitle = Get-McpUiTextValue -Path $slotTitlePath
        if ($slotTitle -ne "Empty Hangar") {
            continue
        }

        Invoke-McpUiInvoke -Root $root -Path ("/Canvas/GaragePageRoot/RosterListPane/GarageSlot{0}" -f $slot) -Method "click" | Out-Null
        Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/UnitEditorPane/FRAMECard/FRAMEValuePanel/FRAMENextButton" -Method "click" | Out-Null
        Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/UnitEditorPane/FIREPOWERCard/FIREPOWERValuePanel/FIREPOWERNextButton" -Method "click" | Out-Null
        Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/UnitEditorPane/MOBILITYCard/MOBILITYValuePanel/MOBILITYNextButton" -Method "click" | Out-Null
        Save-CurrentGarageDraft

        $readyLabel = Get-McpUiTextValue -Path $readyLabelPath
        if ($readyLabel -in @("Ready", "Cancel")) {
            return
        }
    }

    throw "Ready never unlocked after filling empty Garage slots."
}

try {
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec

    if ($health.State.isPlaying) {
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
    }

    Invoke-McpSceneOpenAndWait -Root $root -ScenePath $ScenePath -TimeoutSec $TimeoutSec | Out-Null
    Wait-McpSceneActive -Root $root -SceneName $sceneName -TimeoutSec $TimeoutSec -PollSec 0.5 | Out-Null

    Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $TimeoutSec | Out-Null
    $startedPlayHere = $true

    Wait-McpUiInactive -Root $root -Path "/Canvas/LoginLoadingOverlay/LoadingPanel" -TimeoutMs ($TimeoutSec * 1000) | Out-Null
    Wait-ForPhotonLobbyReady

    $roomName = Ensure-RoomCreated
    Ensure-ReadyBaseline

    $readyButtonPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton"
    $readyLabelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton/Label"
    $startGameButtonPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/StartGameButton"

    if ((Get-McpUiTextValue -Path $readyLabelPath) -eq "Ready") {
        Invoke-McpUiInvoke -Root $root -Path $readyButtonPath -Method "click" | Out-Null
        Wait-ForCondition `
            -Description "Ready toggle to switch on" `
            -TimeoutSecValue 30 `
            -Condition { (Get-McpUiTextValue -Path $readyLabelPath) -eq "Cancel" }
    }

    Wait-ForCondition `
        -Description "StartGame button to become interactable" `
        -TimeoutSecValue 30 `
        -Condition { (Get-McpUiButtonInfo -Path $startGameButtonPath).interactable }

    Invoke-McpUiInvoke -Root $root -Path $startGameButtonPath -Method "click" | Out-Null

    Wait-ForCondition `
        -Description "GameScene active" `
        -TimeoutSecValue $TimeoutSec `
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
