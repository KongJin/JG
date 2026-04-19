param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$RoomNamePrefix = "CodexPlacementSmoke",
    [string]$InitialScreenshotPath = "artifacts/unity/game-scene-placement-initial.png",
    [string]$DragScreenshotPath = "artifacts/unity/game-scene-placement-after-drag.png",
    [string]$FinalScreenshotPath = "artifacts/unity/game-scene-placement-final.png",
    [string]$ResultPath = "artifacts/unity/game-scene-placement-wave-result.json",
    [int]$TimeoutSec = 90,
    [int]$PostSummonWaitSec = 20,
    [int]$AdditionalSummonClicks = 0,
    [int]$AdditionalSummonClickIntervalSec = 2,
    [int]$OutcomeTimeoutSec = 0,
    [int]$OutcomePollIntervalSec = 5
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$startedPlayHere = $false

function Get-UiTextFromGameObject {
    param(
        [string]$Root,
        [string]$Path
    )

    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$Root/gameobject/find" `
        -ContentType "application/json" `
        -Body (@{
            path = $Path
            lightweight = $false
        } | ConvertTo-Json -Compress)

    foreach ($component in @($response.components)) {
        if ($component.typeName -ne "Text") {
            continue
        }

        foreach ($property in @($component.properties)) {
            if ($property.name -eq "m_Text") {
                return [string]$property.value
            }
        }
    }

    return ""
}

function Get-TransformPositionFromGameObject {
    param(
        [string]$Root,
        [string]$Path
    )

    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$Root/gameobject/find" `
        -ContentType "application/json" `
        -Body (@{
            path = $Path
            lightweight = $false
        } | ConvertTo-Json -Compress)

    foreach ($component in @($response.components)) {
        if ($component.typeName -ne "Transform") {
            continue
        }

        foreach ($property in @($component.properties)) {
            if ($property.name -eq "m_LocalPosition") {
                return [string]$property.value
            }
        }
    }

    return ""
}

function Get-GameObjectState {
    param(
        [string]$Root,
        [string]$Path
    )

    return Invoke-RestMethod `
        -Method Post `
        -Uri "$Root/gameobject/find" `
        -ContentType "application/json" `
        -Body (@{
            path = $Path
            lightweight = $false
        } | ConvertTo-Json -Compress)
}

function Get-FirstSummonSlotPath {
    param(
        [string]$Root
    )

    $hierarchy = Invoke-McpGetJson -Root $Root -SubPath "/scene/hierarchy"
    $slotRowPrefix = "/HudCanvas/UnitSummonUi/SlotRow/"

    function Find-NodePath {
        param([object[]]$Nodes)

        foreach ($node in @($Nodes)) {
            if ($null -eq $node) {
                continue
            }

            $path = [string]$node.path
            if ($path.StartsWith($slotRowPrefix, [System.StringComparison]::Ordinal)) {
                return $path
            }

            $children = @($node.children)
            if ($children.Count -gt 0) {
                $childResult = Find-NodePath -Nodes $children
                if (-not [string]::IsNullOrWhiteSpace($childResult)) {
                    return $childResult
                }
            }
        }

        return $null
    }

    $slotPath = Find-NodePath -Nodes @($hierarchy.nodes)
    if ([string]::IsNullOrWhiteSpace($slotPath)) {
        throw "No summon slot instance found under /HudCanvas/UnitSummonUi/SlotRow."
    }

    return $slotPath
}

function Get-WaveObservation {
    param(
        [string]$Root
    )

    $overlayState = Get-GameObjectState -Root $Root -Path "/HudCanvas/WaveUi/WaveEndOverlay/Panel"
    $returnButtonState = Get-GameObjectState -Root $Root -Path "/HudCanvas/WaveUi/WaveEndOverlay/Panel/ReturnToLobbyButton"
    $resultTextRaw = Get-UiTextFromGameObject -Root $Root -Path "/HudCanvas/WaveUi/WaveEndOverlay/Panel/ResultText"

    return [PSCustomObject]@{
        t = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        wave = Get-UiTextFromGameObject -Root $Root -Path "/HudCanvas/WaveUi/TopBar/WaveText"
        countdown = Get-UiTextFromGameObject -Root $Root -Path "/HudCanvas/WaveUi/TopBar/CountdownText"
        status = Get-UiTextFromGameObject -Root $Root -Path "/HudCanvas/WaveUi/TopBar/StatusText"
        core = Get-UiTextFromGameObject -Root $Root -Path "/HudCanvas/WaveUi/CoreHealthHud/HpText"
        enemyPosition = Get-TransformPositionFromGameObject -Root $Root -Path "/EnemyCharacter(Clone)"
        battleEntityPosition = Get-TransformPositionFromGameObject -Root $Root -Path "/RuntimeRoot/UnitsRoot/BattleEntity(Clone)"
        overlay = [bool]$overlayState.activeSelf
        returnButton = [bool]$returnButtonState.activeSelf
        result = if ($overlayState.activeSelf) { $resultTextRaw } else { "" }
        rawResultText = $resultTextRaw
    }
}

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

    Start-Sleep -Seconds 4

    $initialScreenshot = Invoke-McpScreenshotCapture -Root $root -OutputPath $InitialScreenshotPath -Overwrite

    $dragAttempt = Invoke-RestMethod `
        -Method Post `
        -Uri "$root/input/drag" `
        -ContentType "application/json" `
        -Body (@{
            startX = 0.24
            startY = 0.89
            endX = 0.27
            endY = 0.60
            normalized = $true
            button = 0
            steps = 20
        } | ConvertTo-Json -Compress)

    Start-Sleep -Seconds 2

    $dragUnitState = Invoke-RestMethod `
        -Method Post `
        -Uri "$root/gameobject/find" `
        -ContentType "application/json" `
        -Body (@{
            name = "BattleEntity(Clone)"
            lightweight = $false
        } | ConvertTo-Json -Compress)

    $dragPlacementError = Get-UiTextFromGameObject -Root $root -Path "/HudCanvas/PlacementErrorView/Text"
    $dragScreenshot = Invoke-McpScreenshotCapture -Root $root -OutputPath $DragScreenshotPath -Overwrite

    $summonSlotPath = Get-FirstSummonSlotPath -Root $root
    $summonInvokes = @()
    $clickSummon = Invoke-McpUiInvoke -Root $root -Path $summonSlotPath -Method "click"
    $summonInvokes += $clickSummon
    Start-Sleep -Seconds 2

    if ($AdditionalSummonClicks -gt 0) {
        foreach ($index in 1..$AdditionalSummonClicks) {
            $currentSummonSlotPath = Get-FirstSummonSlotPath -Root $root
            $additionalInvoke = Invoke-McpUiInvoke -Root $root -Path $currentSummonSlotPath -Method "click"
            $summonInvokes += $additionalInvoke

            if ($AdditionalSummonClickIntervalSec -gt 0) {
                Start-Sleep -Seconds $AdditionalSummonClickIntervalSec
            }
        }
    }

    $battleEntity = Invoke-RestMethod `
        -Method Post `
        -Uri "$root/gameobject/find" `
        -ContentType "application/json" `
        -Body (@{
            name = "BattleEntity(Clone)"
            lightweight = $false
        } | ConvertTo-Json -Compress)

    $postClickEnemyPosition = Get-TransformPositionFromGameObject -Root $root -Path "/EnemyCharacter(Clone)"
    $battleEntityPosition = Get-TransformPositionFromGameObject -Root $root -Path "/RuntimeRoot/UnitsRoot/BattleEntity(Clone)"

    if ($PostSummonWaitSec -gt 0) {
        Start-Sleep -Seconds $PostSummonWaitSec
    }

    $observations = @()
    $currentObservation = Get-WaveObservation -Root $root
    $observations += $currentObservation

    if ($OutcomeTimeoutSec -gt 0) {
        $deadline = (Get-Date).AddSeconds($OutcomeTimeoutSec)

        while ((Get-Date) -lt $deadline) {
            if ($currentObservation.overlay) {
                break
            }

            if ($OutcomePollIntervalSec -gt 0) {
                Start-Sleep -Seconds $OutcomePollIntervalSec
            }

            $currentObservation = Get-WaveObservation -Root $root
            $observations += $currentObservation
        }
    }

    $waveText = $currentObservation.wave
    $countdownText = $currentObservation.countdown
    $statusText = $currentObservation.status
    $coreHpText = $currentObservation.core
    $finalEnemyPosition = $currentObservation.enemyPosition
    $finalBattleEntityPosition = $currentObservation.battleEntityPosition
    $waveEndOverlay = [PSCustomObject]@{ activeSelf = $currentObservation.overlay }
    $returnButton = [PSCustomObject]@{ activeSelf = $currentObservation.returnButton }
    $outcomeResultText = $currentObservation.result
    $finalScreenshot = Invoke-McpScreenshotCapture -Root $root -OutputPath $FinalScreenshotPath -Overwrite
    $logs = Get-McpRecentLogs -Root $root -Limit 240
    $errors = Get-McpRecentErrors -Root $root -Limit 80

    $waveLoopAdvanced = $waveText -ne "Wave 1/5" -or $waveEndOverlay.activeSelf
    $dragDidSummon = [bool]$dragUnitState.found
    $coreHpChanged = $coreHpText -ne "1500 / 1500"
    $outcomeReached = [bool]$waveEndOverlay.activeSelf

    $result = [PSCustomObject]@{
        success = $dragDidSummon -and ($waveLoopAdvanced -or $coreHpChanged)
        root = $root
        play = $session.play
        roomName = $roomName
        dragAttempt = $dragAttempt
        dragDidSummon = $dragDidSummon
        dragPlacementErrorText = $dragPlacementError
        summonSlotPath = $summonSlotPath
        clickSummon = $clickSummon
        summonInvokes = $summonInvokes
        battleEntityFoundAfterClick = [bool]$battleEntity.found
        battleEntityPositionAfterClick = $battleEntityPosition
        enemyPositionAfterClick = $postClickEnemyPosition
        waveTextAfterWait = $waveText
        countdownTextAfterWait = $countdownText
        statusTextAfterWait = $statusText
        coreHpAfterWait = $coreHpText
        enemyPositionAfterWait = $finalEnemyPosition
        battleEntityPositionAfterWait = $finalBattleEntityPosition
        waveEndOverlayActive = [bool]$waveEndOverlay.activeSelf
        outcomeReached = $outcomeReached
        outcomeResultText = $outcomeResultText
        returnToLobbyButtonActive = [bool]$returnButton.activeSelf
        waveLoopAdvanced = $waveLoopAdvanced
        coreHpChanged = $coreHpChanged
        observations = $observations
        initialScreenshot = $initialScreenshot
        dragScreenshot = $dragScreenshot
        finalScreenshot = $finalScreenshot
        recentLogCount = $logs.count
        recentErrorCount = $errors.count
        recentLogs = @($logs.items | Select-Object -Last 80 | ForEach-Object { "[{0}] {1}" -f $_.type, $_.message })
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
            Write-Warning ("Failed to stop Play Mode after placement smoke: {0}" -f $_.Exception.Message)
        }
    }
}
