param(
    [string]$BaseUrl,
    [ValidateSet("None", "Defeat", "Victory", "NaturalVictory")]
    [string]$ResultMode = "Defeat",
    [string]$Owner = "GameSceneUIUX",
    [string]$OutputPath = "artifacts/unity/game-scene-placement-smoke.json",
    [string]$LockPath = "Temp/UnityMcp/runtime-smoke.lock",
    [int]$LockTimeoutSec = 0,
    [int]$TimeoutSec = 120,
    [switch]$NoMcpLock,
    [switch]$ParentMcpLock,
    [switch]$LeavePlayMode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "McpHelpers.ps1")

function ConvertTo-SafeArray {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

$script:StepVerdicts = New-Object System.Collections.Generic.List[object]
$script:TransportErrors = New-Object System.Collections.Generic.List[object]
$script:SmokePathContractVersion = "2026-04-27.1"
$script:SmokeUiPaths = [ordered]@{
    roomNameInput = @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/RoomNameInput",
        "RoomNameInput"
    )
    displayNameInput = @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/DisplayNameInput",
        "DisplayNameInput"
    )
    capacityInput = @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/CapacityInput",
        "CapacityInput"
    )
    createRoomButton = @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/CreateRoomButton",
        "CreateRoomButton"
    )
    roomDetailPanel = @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel",
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel",
        "RoomDetailPanel"
    )
    readyButton = @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel/ReadyButton",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/ReadyButton",
        "ReadyButton"
    )
    readyButtonLabel = @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel/ReadyButton/Label",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/ReadyButton/Label",
        "ReadyButton/Label"
    )
    startGameButton = @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel/StartGameButton",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/StartGameButton",
        "StartGameButton"
    )
    unitSlot0 = @(
        "/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0",
        "UnitSlot-0"
    )
    commandDock = @(
        "/BattleHudCanvas/RuntimeBindingLayer/CommandDock",
        "CommandDock"
    )
    battleSceneSystems = @(
        "/BattleSceneSystems",
        "BattleSceneSystems",
        "/GameSceneRoot",
        "GameSceneRoot"
    )
    resultPanel = @(
        "/BattleHudCanvas/RuntimeBindingLayer/WaveEndOverlay/ResultPanel",
        "ResultPanel"
    )
}

function Add-SmokeStep {
    param(
        [string]$Name,
        [string]$Verdict = "passed",
        [string]$Detail = ""
    )

    $entry = [ordered]@{
        name = $Name
        verdict = $Verdict
        atUtc = (Get-Date).ToUniversalTime().ToString("o")
    }

    if (-not [string]::IsNullOrWhiteSpace($Detail)) {
        $entry.detail = $Detail
    }

    $script:StepVerdicts.Add([PSCustomObject]$entry) | Out-Null
}

function Test-SmokeTransportError {
    param([string]$Message)

    if ([string]::IsNullOrWhiteSpace($Message)) {
        return $false
    }

    return $Message -match "(?i)(\b504\b|transport|connection|forcibly closed|actively refused|timed out|timeout|unable to write|No connection could be made|원격 서버|연결할 수 없습니다|시간.*초과)"
}

function Get-SmokeBlockedReason {
    param([string]$Message)

    if ($Message -match "(?i)(active UI path|UI component|UI invoke failed|GameObject not found|scene node)") {
        return "path-contract"
    }

    if ($Message -match "(?i)(Active scene did not become|scene transition|LoadLevel|LoadScene)") {
        return "scene-transition"
    }

    if (Test-SmokeTransportError -Message $Message) {
        return "transport-error"
    }

    if ($Message -match "(?i)(exclusive operation lock|runtime-smoke\.lock)") {
        return "runtime-smoke-lock-held"
    }

    return $Message
}

function Invoke-UiInvokeAny {
    param(
        [string]$Root,
        [string[]]$Paths,
        [string]$Method = "click",
        [string]$CustomMethod,
        [object[]]$Args
    )

    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($path in $Paths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        try {
            $response = Invoke-McpUiInvoke -Root $Root -Path $path -Method $Method -CustomMethod $CustomMethod -Args $Args
            if (Test-McpResponseSuccess -Response $response) {
                return [PSCustomObject]@{
                    success = $true
                    path = $path
                    response = $response
                }
            }

            $errors.Add("${path}: response.success=false")
        }
        catch {
            $errors.Add("${path}: $($_.Exception.Message)")
        }
    }

    throw "UI invoke failed for every candidate. $($errors -join ' | ')"
}

function Find-SceneNodeByName {
    param(
        [object]$Node,
        [string]$Name
    )

    if ($null -eq $Node) {
        return $null
    }

    if ($null -ne $Node.PSObject.Properties["name"] -and [string]$Node.name -eq $Name) {
        return $Node
    }

    if ($null -ne $Node.PSObject.Properties["children"]) {
        foreach ($child in ConvertTo-SafeArray $Node.children) {
            if ($child -is [string]) {
                continue
            }

            $found = Find-SceneNodeByName -Node $child -Name $Name
            if ($null -ne $found) {
                return $found
            }
        }
    }

    return $null
}

function Get-SceneNodeByName {
    param(
        [string]$Root,
        [string]$Name
    )

    $hierarchy = Invoke-McpGetJson -Root $Root -SubPath "/scene/hierarchy" -TimeoutSec 15
    foreach ($node in ConvertTo-SafeArray $hierarchy.nodes) {
        $found = Find-SceneNodeByName -Node $node -Name $Name
        if ($null -ne $found) {
            return $found
        }
    }

    return $null
}

function Wait-SceneNodeActive {
    param(
        [string]$Root,
        [string]$Name,
        [bool]$ExpectedActive,
        [int]$TimeoutSec,
        [int]$PollMs = 250
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastNode = $null
    while ((Get-Date) -lt $deadline) {
        $lastNode = Get-SceneNodeByName -Root $Root -Name $Name
        if ($null -ne $lastNode) {
            $active = [bool]$lastNode.activeSelf
            if ($active -eq $ExpectedActive) {
                return $lastNode
            }
        }

        Start-Sleep -Milliseconds $PollMs
    }

    $stateText = if ($null -eq $lastNode) { "missing" } else { "activeSelf=$($lastNode.activeSelf)" }
    throw "Timed out waiting for scene node '${Name}' activeSelf=${ExpectedActive}. Last state: ${stateText}."
}

function Wait-UiActiveAny {
    param(
        [string]$Root,
        [string[]]$Paths,
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    $errors = New-Object System.Collections.Generic.List[string]
    while ((Get-Date) -lt $deadline) {
        foreach ($path in $Paths) {
            if ([string]::IsNullOrWhiteSpace($path)) {
                continue
            }

            try {
                $active = Get-McpUiActiveInHierarchy -Root $Root -Path $path
                if ($active) {
                    return [PSCustomObject]@{
                        success = $true
                        path = $path
                    }
                }
            }
            catch {
                if ($errors.Count -lt 6) {
                    $errors.Add("${path}: $($_.Exception.Message)")
                }
            }
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for active UI path. $($errors -join ' | ')"
}

function Wait-UiComponentAny {
    param(
        [string]$Root,
        [string[]]$Paths,
        [string]$ComponentType,
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    $errors = New-Object System.Collections.Generic.List[string]
    while ((Get-Date) -lt $deadline) {
        foreach ($path in $Paths) {
            if ([string]::IsNullOrWhiteSpace($path)) {
                continue
            }

            try {
                $remainingMs = [Math]::Max(250, [int]($deadline - (Get-Date)).TotalMilliseconds)
                $response = Wait-McpUiComponent `
                    -Root $Root `
                    -Path $path `
                    -ComponentType $ComponentType `
                    -TimeoutMs ([Math]::Min(1000, $remainingMs)) `
                    -PollIntervalMs 100

                return [PSCustomObject]@{
                    success = $true
                    path = $path
                    response = $response
                }
            }
            catch {
                if ($errors.Count -lt 8) {
                    $errors.Add("${path}: $($_.Exception.Message)")
                }
            }
        }
    }

    throw "Timed out waiting for UI component ${ComponentType}. $($errors -join ' | ')"
}

function Wait-UiButtonInteractableAny {
    param(
        [string]$Root,
        [string[]]$Paths,
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    $states = New-Object System.Collections.Generic.List[string]
    while ((Get-Date) -lt $deadline) {
        foreach ($path in $Paths) {
            if ([string]::IsNullOrWhiteSpace($path)) {
                continue
            }

            try {
                $button = Get-McpUiButtonInfo -Root $Root -Path $path
                if ($button.activeInHierarchy -and $button.interactable) {
                    return [PSCustomObject]@{
                        success = $true
                        path = $path
                        activeInHierarchy = $button.activeInHierarchy
                        interactable = $button.interactable
                    }
                }

                if ($states.Count -lt 8) {
                    $states.Add(("{0}: active={1} interactable={2}" -f $path, $button.activeInHierarchy, $button.interactable))
                }
            }
            catch {
                if ($states.Count -lt 8) {
                    $states.Add("${path}: $($_.Exception.Message)")
                }
            }
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for interactable UI button. $($states -join ' | ')"
}

function Wait-UiTextAny {
    param(
        [string]$Root,
        [string[]]$Paths,
        [string]$ExpectedText,
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    $states = New-Object System.Collections.Generic.List[string]
    while ((Get-Date) -lt $deadline) {
        foreach ($path in $Paths) {
            if ([string]::IsNullOrWhiteSpace($path)) {
                continue
            }

            try {
                $text = Get-McpUiTextValue -Root $Root -Path $path
                if ([string]::Equals($text, $ExpectedText, [System.StringComparison]::OrdinalIgnoreCase)) {
                    return [PSCustomObject]@{
                        success = $true
                        path = $path
                        text = $text
                    }
                }

                if ($states.Count -lt 8) {
                    $states.Add(("{0}: text='{1}'" -f $path, $text))
                }
            }
            catch {
                if ($states.Count -lt 8) {
                    $states.Add("${path}: $($_.Exception.Message)")
                }
            }
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for UI text '${ExpectedText}'. $($states -join ' | ')"
}

function Wait-SceneActiveForSmoke {
    param(
        [string]$Root,
        [string]$SceneName,
        [int]$TimeoutSec = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null
    $currentRoot = $Root
    while ((Get-Date) -lt $deadline) {
        try {
            $health = Wait-McpBridgeHealthy -Root $currentRoot -TimeoutSec 10
            $currentRoot = $health.Root
            if ($health.State.activeScene -eq $SceneName) {
                return [PSCustomObject]@{
                    success = $true
                    root = $currentRoot
                    activeScene = $health.State.activeScene
                    activeScenePath = $health.State.activeScenePath
                }
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Active scene did not become '${SceneName}' within ${TimeoutSec}s. Last error: $lastError"
}

function Sync-McpRootForSmoke {
    param(
        [string]$Root,
        [int]$TimeoutSec = 30
    )

    $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec
    return [PSCustomObject]@{
        root = $health.Root
        state = $health.State
    }
}

function Set-UiValueIfPresent {
    param(
        [string]$Root,
        [string[]]$Paths,
        [string]$Value
    )

    foreach ($path in $Paths) {
        try {
            Invoke-McpSetUiValue -Root $Root -Path $path -Value $Value
            return [PSCustomObject]@{
                success = $true
                path = $path
                value = $Value
            }
        }
        catch {
        }
    }

    return [PSCustomObject]@{
        success = $false
        value = $Value
    }
}

function Get-NewConsoleItems {
    param(
        [string]$Root,
        [datetime]$StartedAtUtc,
        [int]$Limit = 200
    )

    $logs = Get-McpRecentLogs -Root $Root -Limit $Limit
    return @(
        foreach ($item in ConvertTo-SafeArray $logs.items) {
            if ($null -eq $item.PSObject.Properties["timestampUtc"]) {
                continue
            }

            $parsedTimestamp = [datetime]::Parse(
                [string]$item.timestampUtc,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::AssumeUniversal)
            $timestamp = $parsedTimestamp.ToUniversalTime()
            if ($timestamp -ge $StartedAtUtc) {
                $item
            }
        }
    )
}

function Test-NewLogContains {
    param(
        [object[]]$Items,
        [string]$Pattern
    )

    foreach ($item in ConvertTo-SafeArray $Items) {
        if ($null -ne $item.PSObject.Properties["message"] -and [string]$item.message -like $Pattern) {
            return $true
        }
    }

    return $false
}

function Wait-NewLogContains {
    param(
        [string]$Root,
        [datetime]$StartedAtUtc,
        [string]$Pattern,
        [int]$TimeoutSec = 30,
        [int]$Limit = 1000
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $items = Get-NewConsoleItems -Root $Root -StartedAtUtc $StartedAtUtc -Limit $Limit
        if (Test-NewLogContains -Items $items -Pattern $Pattern) {
            return [PSCustomObject]@{
                success = $true
                pattern = $Pattern
            }
        }

        Start-Sleep -Milliseconds 250
    }

    return [PSCustomObject]@{
        success = $false
        pattern = $Pattern
    }
}

function Wait-PhotonLobbyReadyForSmoke {
    param(
        [string]$Root,
        [int]$TimeoutSec = 90,
        [int]$LogLimit = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            Wait-McpBridgeHealthy -Root $Root -TimeoutSec 10 | Out-Null
            $logs = Get-McpRecentLogs -Root $Root -Limit $LogLimit
            foreach ($item in ConvertTo-SafeArray $logs.items) {
                if ($null -ne $item.PSObject.Properties["message"] -and $item.message -like "*Joined lobby. Ready for matchmaking.*") {
                    return [PSCustomObject]@{
                        success = $true
                        message = "Photon lobby ready."
                    }
                }
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for Photon lobby ready log. Last error: $lastError"
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$startedAtUtc = (Get-Date).ToUniversalTime()
$commandLine = if ([string]::IsNullOrWhiteSpace($MyInvocation.Line)) { $PSCommandPath } else { $MyInvocation.Line }
$result = [ordered]@{
    success = $false
    terminalVerdict = "blocked"
    owner = $Owner
    script = "tools/unity-mcp/Invoke-GameScenePlacementSmoke.ps1"
    command = $commandLine
    blockedReason = ""
    mismatchReason = ""
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:sszzz")
    startedAtUtc = $startedAtUtc.ToString("o")
    endedAtUtc = ""
    resultMode = $ResultMode
    activeScenePath = ""
    lockOwner = ""
    playModeStarted = $false
    playModeStopped = $false
    stepVerdicts = @()
    transportErrors = @()
    evidenceScopeReason = "Runtime smoke artifact records execution evidence rather than source file ownership."
    evidence = [ordered]@{
        pathContractVersion = $script:SmokePathContractVersion
        pathCandidates = $script:SmokeUiPaths
    }
}
$operationLock = $null

try {
    if (-not $NoMcpLock) {
        $operationLock = Enter-McpExclusiveOperation -Name "game-scene-placement-smoke" -Owner $Owner -LockPath $LockPath -TimeoutSec $LockTimeoutSec
        $result.lockOwner = $operationLock.Owner
        $result.evidence.mcpLock = @{
            owner = $operationLock.Owner
            path = $operationLock.Path
            token = $operationLock.Token
        }
        Add-SmokeStep -Name "lock" -Detail $operationLock.Path
    }
    else {
        $lockSkipReason = if ($ParentMcpLock) { "ParentMcpLock" } else { "NoMcpLock" }
        $result.lockOwner = $lockSkipReason
        $result.evidence.mcpLock = @{
            owner = $Owner
            path = $LockPath
            skipped = $true
            reason = $lockSkipReason
        }
        Add-SmokeStep -Name "lock" -Verdict "skipped" -Detail $lockSkipReason
    }

    $rootSync = Sync-McpRootForSmoke -Root $root -TimeoutSec $TimeoutSec
    $root = $rootSync.root
    $result.activeScenePath = [string]$rootSync.state.activeScenePath
    Add-SmokeStep -Name "mcp-preflight" -Detail $result.activeScenePath
    Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000 | Out-Null
    Add-SmokeStep -Name "compile"

    $prepare = Invoke-McpPrepareLobbyPlaySession `
        -Root $root `
        -TimeoutSec $TimeoutSec `
        -LoginLoadingPanelPath "/LobbyCanvas/Overlays/SetCLoginLoadingOverlayRoot/LoadingPanel"
    $result.evidence.prepareLobby = $prepare
    $result.playModeStarted = $true
    Add-SmokeStep -Name "lobby-play-session"
    $rootSync = Sync-McpRootForSmoke -Root $root -TimeoutSec 30
    $root = $rootSync.root
    $result.activeScenePath = [string]$rootSync.state.activeScenePath

    $lobbyReady = Wait-PhotonLobbyReadyForSmoke -Root $root -TimeoutSec $TimeoutSec
    $result.evidence.lobbyReady = $lobbyReady
    Add-SmokeStep -Name "lobby-ready"
    $rootSync = Sync-McpRootForSmoke -Root $root -TimeoutSec 30
    $root = $rootSync.root
    Start-Sleep -Seconds 2

    $roomSuffix = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
    $result.evidence.roomNameInput = Set-UiValueIfPresent -Root $root -Value "mcp-$roomSuffix" -Paths $script:SmokeUiPaths.roomNameInput
    $result.evidence.displayNameInput = Set-UiValueIfPresent -Root $root -Value "MCP Smoke" -Paths $script:SmokeUiPaths.displayNameInput
    $result.evidence.capacityInput = Set-UiValueIfPresent -Root $root -Value "2" -Paths $script:SmokeUiPaths.capacityInput

    $createRoom = Invoke-UiInvokeAny -Root $root -Paths $script:SmokeUiPaths.createRoomButton
    $result.evidence.createRoom = $createRoom
    Add-SmokeStep -Name "room-create" -Detail $createRoom.path

    $roomDetailActive = Wait-UiActiveAny -Root $root -TimeoutMs ($TimeoutSec * 1000) -Paths $script:SmokeUiPaths.roomDetailPanel
    $result.evidence.roomDetailActive = $roomDetailActive
    Add-SmokeStep -Name "room-detail-active" -Detail $roomDetailActive.path

    $ready = Invoke-UiInvokeAny -Root $root -Paths $script:SmokeUiPaths.readyButton
    $result.evidence.ready = $ready
    Add-SmokeStep -Name "room-ready" -Detail $ready.path

    $readyStateApplied = Wait-UiTextAny -Root $root -ExpectedText "Cancel" -TimeoutMs ($TimeoutSec * 1000) -Paths $script:SmokeUiPaths.readyButtonLabel
    $result.evidence.readyStateApplied = $readyStateApplied
    Add-SmokeStep -Name "ready-state-applied" -Detail $readyStateApplied.path

    $startButtonReady = Wait-UiButtonInteractableAny -Root $root -TimeoutMs ($TimeoutSec * 1000) -Paths $script:SmokeUiPaths.startGameButton
    $result.evidence.startButtonReady = $startButtonReady
    Add-SmokeStep -Name "start-button-ready" -Detail $startButtonReady.path

    $start = Invoke-UiInvokeAny -Root $root -Paths $script:SmokeUiPaths.startGameButton
    $result.evidence.start = $start
    Add-SmokeStep -Name "room-start" -Detail $start.path

    $battleSceneActive = Wait-SceneActiveForSmoke -Root $root -SceneName "BattleScene" -TimeoutSec $TimeoutSec
    $result.evidence.battleSceneActive = $battleSceneActive
    $root = $battleSceneActive.root
    $result.activeScenePath = [string]$battleSceneActive.activeScenePath
    Add-SmokeStep -Name "battle-scene-active" -Detail $battleSceneActive.activeScenePath
    $slotReady = Wait-UiComponentAny -Root $root -ComponentType "UnitSlotView" -TimeoutMs ($TimeoutSec * 1000) -Paths $script:SmokeUiPaths.unitSlot0
    $result.evidence.slotReady = $slotReady
    Add-SmokeStep -Name "slot-ready" -Detail $slotReady.path

    $slotClick = Invoke-UiInvokeAny -Root $root -Paths $script:SmokeUiPaths.unitSlot0
    $result.evidence.slotClick = $slotClick
    Add-SmokeStep -Name "slot-click" -Detail $slotClick.path

    $previewBeforeConfirm = Wait-SceneNodeActive -Root $root -Name "PlacementPreviewVisuals" -ExpectedActive $true -TimeoutSec 10
    $result.evidence.previewBeforeConfirm = @{
        path = $previewBeforeConfirm.path
        activeSelf = $previewBeforeConfirm.activeSelf
    }

    $confirm = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ConfirmPlacementAtPlacementCenter" -Paths $script:SmokeUiPaths.commandDock
    $result.evidence.confirmPlacement = $confirm
    Add-SmokeStep -Name "placement-confirm" -Detail $confirm.path

    $previewAfterConfirm = Wait-SceneNodeActive -Root $root -Name "PlacementPreviewVisuals" -ExpectedActive $false -TimeoutSec 10
    $result.evidence.previewAfterConfirm = @{
        path = $previewAfterConfirm.path
        activeSelf = $previewAfterConfirm.activeSelf
    }
    Add-SmokeStep -Name "placement-preview-hidden"

    if ($ResultMode -eq "Defeat") {
        $forced = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceCoreDefeatForMcpSmoke" -Paths $script:SmokeUiPaths.battleSceneSystems
        $result.evidence.resultTrigger = $forced
        Add-SmokeStep -Name "result-trigger" -Detail "Defeat"
    }
    elseif ($ResultMode -eq "Victory") {
        $forced = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceVictoryForMcpSmoke" -Paths $script:SmokeUiPaths.battleSceneSystems
        $result.evidence.resultTrigger = $forced
        Add-SmokeStep -Name "result-trigger" -Detail "Victory"
    }
    elseif ($ResultMode -eq "NaturalVictory") {
        $naturalVictory = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "RunFinalWaveClearForMcpSmoke" -Args @(12, [Math]::Max(30, $TimeoutSec - 20)) -Paths $script:SmokeUiPaths.battleSceneSystems
        $result.evidence.resultTrigger = $naturalVictory
        Add-SmokeStep -Name "result-trigger" -Detail "NaturalVictory"
    }
    else {
        Add-SmokeStep -Name "result-trigger" -Verdict "skipped" -Detail "None"
    }

    if ($ResultMode -ne "None") {
        $resultPanel = Wait-UiActiveAny -Root $root -TimeoutMs ($TimeoutSec * 1000) -Paths $script:SmokeUiPaths.resultPanel
        $result.evidence.resultPanel = $resultPanel
        Add-SmokeStep -Name "result-panel" -Detail $resultPanel.path

        $expectedPattern = if ($ResultMode -eq "Defeat") {
            "*Result:*Defeat*"
        }
        else {
            "*Result:*Victory*"
        }

        $result.evidence.expectedResultLogObserved = Wait-NewLogContains `
            -Root $root `
            -StartedAtUtc $startedAtUtc `
            -Pattern $expectedPattern `
            -TimeoutSec ([Math]::Min(60, [Math]::Max(10, $TimeoutSec / 2)))
        Add-SmokeStep -Name "result-log" -Verdict $(if ($result.evidence.expectedResultLogObserved.success) { "passed" } else { "mismatch" }) -Detail $expectedPattern
    }

    Start-Sleep -Milliseconds 500
    $newLogs = Get-NewConsoleItems -Root $root -StartedAtUtc $startedAtUtc -Limit 1000
    $newErrors = @($newLogs | Where-Object { $_.type -eq "Error" -or $_.type -eq "Exception" -or $_.type -eq "Assert" })
    $result.evidence.newErrorCount = @($newErrors).Count
    $result.evidence.hasSummonLog = Test-NewLogContains -Items $newLogs -Pattern "*Summons:*1*"
    $result.evidence.hasUnitKillLog = Test-NewLogContains -Items $newLogs -Pattern "*Unit Kills:*"
    $result.evidence.hasResultLog = Test-NewLogContains -Items $newLogs -Pattern "*[GameEnd]*Game Result*"
    $result.evidence.hasVictoryResultLog = Test-NewLogContains -Items $newLogs -Pattern "*Result:*Victory*"
    $result.evidence.hasDefeatResultLog = Test-NewLogContains -Items $newLogs -Pattern "*Result:*Defeat*"
    Add-SmokeStep -Name "console-delta" -Verdict $(if (@($newErrors).Count -eq 0) { "passed" } else { "mismatch" }) -Detail ("newErrorCount={0}" -f @($newErrors).Count)

    $hasExpectedResult =
        $ResultMode -eq "None" -or
        ($ResultMode -eq "Defeat" -and $result.evidence.hasDefeatResultLog) -or
        (($ResultMode -eq "Victory" -or $ResultMode -eq "NaturalVictory") -and $result.evidence.hasVictoryResultLog)

    $result.success = @($newErrors).Count -eq 0 -and $hasExpectedResult
    $result.terminalVerdict = if ($result.success) { "success" } else { "mismatch" }
    if (-not $result.success) {
        $result.mismatchReason = if (@($newErrors).Count -ne 0) { "new-console-errors" } else { "expected-result-log-missing" }
        $result.blockedReason = $result.mismatchReason
    }
}
catch {
    $message = $_.Exception.Message
    $result.success = $false
    $result.terminalVerdict = "blocked"
    $result.blockedReason = Get-SmokeBlockedReason -Message $message
    $result.evidence.exception = [ordered]@{
        message = $message
        category = $result.blockedReason
    }
    try {
        $blockedLogs = Get-NewConsoleItems -Root $root -StartedAtUtc $startedAtUtc -Limit 1000
        $blockedErrors = @($blockedLogs | Where-Object { $_.type -eq "Error" -or $_.type -eq "Exception" -or $_.type -eq "Assert" })
        $result.evidence.blockedConsoleDelta = [ordered]@{
            newErrorCount = @($blockedErrors).Count
            items = @(
                $blockedLogs |
                    Select-Object -Last 80 |
                    ForEach-Object {
                        [PSCustomObject]@{
                            type = $_.type
                            message = $_.message
                            timestampUtc = $_.timestampUtc
                        }
                    }
            )
        }
    }
    catch {
        $result.evidence.blockedConsoleDeltaError = $_.Exception.Message
    }
    Add-SmokeStep -Name "blocked" -Verdict "blocked" -Detail $result.blockedReason

    if ($result.blockedReason -eq "transport-error") {
        $script:TransportErrors.Add([PSCustomObject]@{
            atUtc = (Get-Date).ToUniversalTime().ToString("o")
            message = $message
        }) | Out-Null
    }
}
finally {
    if ($LeavePlayMode) {
        try {
            Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/play/stop" -Body @{} -TimeoutSec 30 -RequestTimeoutSec 30 | Out-Null
            Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/play/wait-for-stop" -Body @{ timeoutMs = 90000; pollIntervalMs = 500 } -TimeoutSec 100 -RequestTimeoutSec 100 | Out-Null
            $stopHealth = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
            $root = $stopHealth.Root
            $result.evidence.finalHealth = [ordered]@{
                ok = $stopHealth.State.ok
                isPlaying = $stopHealth.State.isPlaying
                isCompiling = $stopHealth.State.isCompiling
                activeScene = $stopHealth.State.activeScene
                activeScenePath = $stopHealth.State.activeScenePath
            }
            $result.activeScenePath = [string]$stopHealth.State.activeScenePath
            $result.playModeStopped = -not [bool]$stopHealth.State.isPlaying
            Add-SmokeStep -Name "play-stop" -Verdict $(if ($result.playModeStopped) { "passed" } else { "blocked" })
        }
        catch {
            $result.evidence.stopPlayModeError = $_.Exception.Message
            Add-SmokeStep -Name "play-stop" -Verdict "blocked" -Detail $_.Exception.Message
            if (Test-SmokeTransportError -Message $_.Exception.Message) {
                $script:TransportErrors.Add([PSCustomObject]@{
                    atUtc = (Get-Date).ToUniversalTime().ToString("o")
                    message = $_.Exception.Message
                }) | Out-Null
            }
        }
    }
    else {
        try {
            $finalHealth = Wait-McpBridgeHealthy -Root $root -TimeoutSec 10
            $root = $finalHealth.Root
            $result.evidence.finalHealth = [ordered]@{
                ok = $finalHealth.State.ok
                isPlaying = $finalHealth.State.isPlaying
                isCompiling = $finalHealth.State.isCompiling
                activeScene = $finalHealth.State.activeScene
                activeScenePath = $finalHealth.State.activeScenePath
            }
            $result.activeScenePath = [string]$finalHealth.State.activeScenePath
            $result.playModeStopped = -not [bool]$finalHealth.State.isPlaying
        }
        catch {
            $result.evidence.finalHealthError = $_.Exception.Message
        }
    }

    Exit-McpExclusiveOperation -Lock $operationLock

    $result["endedAtUtc"] = (Get-Date).ToUniversalTime().ToString("o")
    $result["stepVerdicts"] = $script:StepVerdicts.ToArray()
    $result["transportErrors"] = $script:TransportErrors.ToArray()

    Ensure-McpParentDirectory -PathValue $OutputPath
    $result | ConvertTo-Json -Depth 12 | Set-Content -Path $OutputPath -Encoding UTF8
    $result | ConvertTo-Json -Depth 8
}
