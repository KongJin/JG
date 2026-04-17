param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$RoomNamePrefix = "CodexSmoke",
    [string]$RoomNameInputPath = "/Canvas/LobbyPageRoot/RoomListPanel/RoomNameInput/Field",
    [string]$CreateRoomButtonPath = "/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomButton",
    [string]$RoomDetailPanelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel",
    [string]$BannerMessagePath = "/Canvas/SceneErrorPresenter/Banner/BannerMessage",
    [string]$ReadyButtonPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton",
    [string]$ReadyLabelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton/Label",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$SaveButtonPath = "/Canvas/GaragePageRoot/ResultPane/SaveButton",
    [string]$SaveLabelPath = "/Canvas/GaragePageRoot/ResultPane/SaveButton/Text (TMP)",
    [string]$RosterStatusPath = "/Canvas/GaragePageRoot/ResultPane/RosterStatus",
    [string]$OutputPath = "artifacts/unity/garage-ready-flow-smoke.png",
    [int]$TimeoutSec = 90
)

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
            $logs = Get-McpRecentLogs -Root $root -Limit 80
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
    Wait-ForCondition `
        -Description "Save Draft button to become interactable" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiButtonInfo -Path $SaveButtonPath).interactable }

    Invoke-McpUiInvoke -Root $root -Path $SaveButtonPath -Method "click" | Out-Null

    Wait-ForCondition `
        -Description "Save Draft request to finish" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Path $SaveLabelPath) -ne "Saving..." }

    Start-Sleep -Milliseconds 500
}

function Ensure-RoomCreated {
    if ((Get-McpUiButtonInfo -Path $ReadyButtonPath).activeInHierarchy) {
        return $null
    }

    $lastBannerText = $null

    foreach ($attempt in 1..3) {
        $roomName = "{0}-{1}-{2}" -f $RoomNamePrefix, (Get-Date -Format "yyyyMMddHHmmssfff"), (Get-Random -Minimum 1000 -Maximum 9999)
        Set-McpInputFieldValue -Path $RoomNameInputPath -Value $roomName
        Invoke-McpUiInvoke -Root $root -Path $CreateRoomButtonPath -Method "click" | Out-Null

        try {
            Wait-ForCondition `
                -Description "room creation result" `
                -TimeoutSecValue 8 `
                -Condition {
                    (Get-McpUiActiveInHierarchy -Path $RoomDetailPanelPath) -or
                    (Get-McpUiActiveInHierarchy -Path $BannerMessagePath)
                }
        }
        catch {
            Start-Sleep -Seconds 1
            continue
        }

        if (Get-McpUiActiveInHierarchy -Path $RoomDetailPanelPath) {
            return $roomName
        }

        if (Get-McpUiActiveInHierarchy -Path $BannerMessagePath) {
            $lastBannerText = Get-McpUiTextValue -Path $BannerMessagePath
        }

        Start-Sleep -Seconds 1
    }

    if ([string]::IsNullOrWhiteSpace($lastBannerText)) {
        $lastBannerText = "RoomDetailPanel never became active."
    }

    throw "Room creation did not succeed. Last banner: $lastBannerText"
}

function Ensure-ReadyBaseline {
    $readyLabel = Get-McpUiTextValue -Path $ReadyLabelPath
    if ($readyLabel -in @("Ready", "Cancel")) {
        return
    }

    $filledSlotIndexes = New-Object System.Collections.Generic.List[int]

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
        $filledSlotIndexes.Add($slot) | Out-Null

        $readyLabel = Get-McpUiTextValue -Path $ReadyLabelPath
        if ($readyLabel -in @("Ready", "Cancel")) {
            return ,$filledSlotIndexes
        }
    }

    throw "Ready never unlocked after filling empty Garage slots. Last Ready label: '$readyLabel'."
}

try {
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec

    if ($health.State.isPlaying) {
        Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
    }

    if ($health.State.activeScenePath -ne $ScenePath) {
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $ScenePath -TimeoutSec $TimeoutSec | Out-Null
        Wait-McpSceneActive -Root $root -SceneName $sceneName -TimeoutSec $TimeoutSec -PollSec 0.5 | Out-Null
    }

    $play = Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $TimeoutSec
    $startedPlayHere = $true

    if (-not [string]::IsNullOrWhiteSpace($LoginLoadingPanelPath)) {
        Wait-McpUiInactive -Root $root -Path $LoginLoadingPanelPath -TimeoutMs ($TimeoutSec * 1000) | Out-Null
    }

    Wait-ForPhotonLobbyReady
    $roomName = Ensure-RoomCreated
    $filledSlots = Ensure-ReadyBaseline

    $before = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Path $ReadyButtonPath
        saveLabel = Get-McpUiTextValue -Path $SaveLabelPath
        rosterStatus = Get-McpUiTextValue -Path $RosterStatusPath
    }

    Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/RosterListPane/GarageSlot1" -Method "click" | Out-Null
    Invoke-McpUiInvoke -Root $root -Path "/Canvas/GaragePageRoot/UnitEditorPane/FRAMECard/FRAMEValuePanel/FRAMENextButton" -Method "click" | Out-Null

    Wait-ForCondition `
        -Description "Ready to be blocked by an unsaved Garage draft" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Path $ReadyLabelPath) -eq "Save Garage Draft" }

    $dirty = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Path $ReadyButtonPath
        saveLabel = Get-McpUiTextValue -Path $SaveLabelPath
        rosterStatus = Get-McpUiTextValue -Path $RosterStatusPath
    }

    Save-CurrentGarageDraft

    Wait-ForCondition `
        -Description "Ready to return after saving the Garage draft" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Path $ReadyLabelPath) -eq "Ready" }

    $afterSave = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Path $ReadyButtonPath
        saveLabel = Get-McpUiTextValue -Path $SaveLabelPath
        rosterStatus = Get-McpUiTextValue -Path $RosterStatusPath
    }

    Invoke-McpUiInvoke -Root $root -Path $ReadyButtonPath -Method "click" | Out-Null

    Wait-ForCondition `
        -Description "Ready toggle to switch on" `
        -TimeoutSecValue $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Path $ReadyLabelPath) -eq "Cancel" }

    $afterReady = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Path $ReadyButtonPath
        saveLabel = Get-McpUiTextValue -Path $SaveLabelPath
        rosterStatus = Get-McpUiTextValue -Path $RosterStatusPath
    }

    $capture = Invoke-McpScreenshotCapture -Root $root -OutputPath $OutputPath -Overwrite

    [PSCustomObject]@{
        success = $true
        root = $root
        scene = $sceneName
        roomName = $roomName
        autoFilledSlots = $filledSlots
        play = $play
        before = $before
        dirty = $dirty
        afterSave = $afterSave
        afterReady = $afterReady
        screenshot = $capture
    }
}
finally {
    if ($startedPlayHere) {
        try {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }
        catch {
            Write-Warning ("Failed to stop Play Mode after Garage ready flow smoke: {0}" -f $_.Exception.Message)
        }
    }
}
