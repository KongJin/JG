param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
    [string]$RoomNamePrefix = "CodexSmoke",
    [string]$RoomNameInputPath = "/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/RoomNameInput/Field",
    [string]$CreateRoomButtonPath = "/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/CreateRoomButton",
    [string]$RoomDetailPanelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel",
    [string]$BannerMessagePath = "/Canvas/SceneErrorPresenter/Banner/BannerMessage",
    [string]$ReadyButtonPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton",
    [string]$ReadyLabelPath = "/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton/Label",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$SaveButtonPath = "/Canvas/GaragePageRoot/MobileSaveDock/MobileSaveButton",
    [string]$RosterStatusPath = "/Canvas/GaragePageRoot/MobileSaveDock/MobileSaveStatusText",
    [string]$OutputPath = "artifacts/unity/garage-ready-flow-smoke.png",
    [int]$TimeoutSec = 90
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

Assert-McpSceneAssetExistsForWorkflow -ScenePath $ScenePath -WorkflowName "Invoke-GarageReadyFlowSmoke.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$startedPlayHere = $false
$garageContentRoot = "/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent"
$garageSlotGridRoot = "$garageContentRoot/RosterListPane/MobileSlotGrid"
$garageTabBarRoot = "$garageContentRoot/GarageMobileTabBar"
$garageEditorRoot = "$garageContentRoot/UnitEditorPane"

function Get-GarageSlotPath {
    param([int]$Slot)

    return "$garageSlotGridRoot/GarageSlot$Slot"
}

function Get-GarageSlotTitlePath {
    param([int]$Slot)

    return "{0}/Title" -f (Get-GarageSlotPath -Slot $Slot)
}

function Invoke-GaragePartCycle {
    param(
        [ValidateSet("frame", "firepower", "mobility")]
        [string]$Part
    )

    $tabPath, $buttonPath = switch ($Part) {
        "frame" { "$garageTabBarRoot/EditTabButton", "$garageEditorRoot/FRAMECard/FRAMEValuePanel/FRAMENextButton" }
        "firepower" { "$garageTabBarRoot/PreviewTabButton", "$garageEditorRoot/FIREPOWERCard/FIREPOWERValuePanel/FIREPOWERNextButton" }
        "mobility" { "$garageTabBarRoot/SummaryTabButton", "$garageEditorRoot/MOBILITYCard/MOBILITYValuePanel/MOBILITYNextButton" }
    }

    Invoke-McpUiInvoke -Root $root -Path $tabPath -Method "click" | Out-Null
    Start-Sleep -Milliseconds 150
    Invoke-McpUiInvoke -Root $root -Path $buttonPath -Method "click" | Out-Null
    Start-Sleep -Milliseconds 150
}

function Save-CurrentGarageDraft {
    Wait-McpCondition `
        -Description "Save Draft button to become interactable" `
        -TimeoutSec $TimeoutSec `
        -Condition { (Get-McpUiButtonInfo -Root $root -Path $SaveButtonPath).interactable }

    Invoke-McpUiInvoke -Root $root -Path $SaveButtonPath -Method "click" | Out-Null

    Wait-McpCondition `
        -Description "Save Draft request to finish" `
        -TimeoutSec $TimeoutSec `
        -Condition { -not (Get-McpUiButtonInfo -Root $root -Path $SaveButtonPath).interactable }

    Start-Sleep -Milliseconds 500
}

function Ensure-RoomCreated {
    if ((Get-McpUiButtonInfo -Root $root -Path $ReadyButtonPath).activeInHierarchy) {
        return $null
    }

    $lastBannerText = $null

    foreach ($attempt in 1..3) {
        $roomName = "{0}-{1}-{2}" -f $RoomNamePrefix, (Get-Date -Format "yyyyMMddHHmmssfff"), (Get-Random -Minimum 1000 -Maximum 9999)
        Invoke-McpSetUiValue -Root $root -Path $RoomNameInputPath -Value $roomName
        Invoke-McpUiInvoke -Root $root -Path $CreateRoomButtonPath -Method "click" | Out-Null

        try {
            Wait-McpCondition `
                -Description "room creation result" `
                -TimeoutSec 8 `
                -Condition {
                    (Get-McpUiActiveInHierarchy -Root $root -Path $RoomDetailPanelPath) -or
                    (Get-McpUiActiveInHierarchy -Root $root -Path $BannerMessagePath)
                }
        }
        catch {
            Start-Sleep -Seconds 1
            continue
        }

        if (Get-McpUiActiveInHierarchy -Root $root -Path $RoomDetailPanelPath) {
            return $roomName
        }

        if (Get-McpUiActiveInHierarchy -Root $root -Path $BannerMessagePath) {
            $lastBannerText = Get-McpUiTextValue -Root $root -Path $BannerMessagePath
        }

        Start-Sleep -Seconds 1
    }

    if ([string]::IsNullOrWhiteSpace($lastBannerText)) {
        $lastBannerText = "RoomDetailPanel never became active."
    }

    throw "Room creation did not succeed. Last banner: $lastBannerText"
}

function Ensure-ReadyBaseline {
    $readyLabel = Get-McpUiTextValue -Root $root -Path $ReadyLabelPath
    if ($readyLabel -in @("Ready", "Cancel")) {
        return
    }

    $filledSlotIndexes = New-Object System.Collections.Generic.List[int]

    foreach ($slot in 1..6) {
        $slotTitlePath = Get-GarageSlotTitlePath -Slot $slot
        $slotTitle = Get-McpUiTextValue -Root $root -Path $slotTitlePath
        if ($slotTitle -notin @("EMPTY", "Empty Hangar")) {
            continue
        }

        Invoke-McpUiInvoke -Root $root -Path (Get-GarageSlotPath -Slot $slot) -Method "click" | Out-Null
        Invoke-GaragePartCycle -Part frame
        Invoke-GaragePartCycle -Part firepower
        Invoke-GaragePartCycle -Part mobility
        Save-CurrentGarageDraft
        $filledSlotIndexes.Add($slot) | Out-Null

        $readyLabel = Get-McpUiTextValue -Root $root -Path $ReadyLabelPath
        if ($readyLabel -in @("Ready", "Cancel")) {
            return ,$filledSlotIndexes
        }
    }

    throw "Ready never unlocked after filling empty Garage slots. Last Ready label: '$readyLabel'."
}

try {
    $session = Invoke-McpPrepareLobbyPlaySession `
        -Root $root `
        -ScenePath $ScenePath `
        -LoginLoadingPanelPath $LoginLoadingPanelPath `
        -TimeoutSec $TimeoutSec
    $startedPlayHere = $true

    Wait-McpPhotonLobbyReady -Root $root -TimeoutSec $TimeoutSec -LogLimit 80
    $roomName = Ensure-RoomCreated
    $filledSlots = Ensure-ReadyBaseline

    $before = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Root $root -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Root $root -Path $ReadyButtonPath
        rosterStatus = Get-McpUiTextValue -Root $root -Path $RosterStatusPath
    }

    Invoke-McpUiInvoke -Root $root -Path (Get-GarageSlotPath -Slot 1) -Method "click" | Out-Null
    Invoke-GaragePartCycle -Part frame

    Wait-McpCondition `
        -Description "Ready to be blocked by an unsaved Garage draft" `
        -TimeoutSec $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Root $root -Path $ReadyLabelPath) -eq "Save Garage Draft" }

    $dirty = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Root $root -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Root $root -Path $ReadyButtonPath
        rosterStatus = Get-McpUiTextValue -Root $root -Path $RosterStatusPath
    }

    Save-CurrentGarageDraft

    Wait-McpCondition `
        -Description "Ready to return after saving the Garage draft" `
        -TimeoutSec $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Root $root -Path $ReadyLabelPath) -eq "Ready" }

    $afterSave = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Root $root -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Root $root -Path $ReadyButtonPath
        rosterStatus = Get-McpUiTextValue -Root $root -Path $RosterStatusPath
    }

    Invoke-McpUiInvoke -Root $root -Path $ReadyButtonPath -Method "click" | Out-Null

    Wait-McpCondition `
        -Description "Ready toggle to switch on" `
        -TimeoutSec $TimeoutSec `
        -Condition { (Get-McpUiTextValue -Root $root -Path $ReadyLabelPath) -eq "Cancel" }

    $afterReady = [PSCustomObject]@{
        readyLabel = Get-McpUiTextValue -Root $root -Path $ReadyLabelPath
        readyButton = Get-McpUiButtonInfo -Root $root -Path $ReadyButtonPath
        rosterStatus = Get-McpUiTextValue -Root $root -Path $RosterStatusPath
    }

    $capture = Invoke-McpScreenshotCapture -Root $root -OutputPath $OutputPath -Overwrite

    [PSCustomObject]@{
        success = $true
        root = $root
        scene = [System.IO.Path]::GetFileNameWithoutExtension($ScenePath)
        roomName = $roomName
        autoFilledSlots = $filledSlots
        play = $session.play
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
