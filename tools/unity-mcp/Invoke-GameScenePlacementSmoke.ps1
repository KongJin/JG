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
$result = [ordered]@{
    success = $false
    terminalVerdict = "blocked"
    blockedReason = ""
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:sszzz")
    resultMode = $ResultMode
    evidence = [ordered]@{}
}
$operationLock = $null

try {
    if (-not $NoMcpLock) {
        $operationLock = Enter-McpExclusiveOperation -Name "game-scene-placement-smoke" -Owner $Owner -LockPath $LockPath -TimeoutSec $LockTimeoutSec
        $result.evidence.mcpLock = @{
            owner = $operationLock.Owner
            path = $operationLock.Path
            token = $operationLock.Token
        }
    }

    $rootSync = Sync-McpRootForSmoke -Root $root -TimeoutSec $TimeoutSec
    $root = $rootSync.root
    Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000 | Out-Null

    $prepare = Invoke-McpPrepareLobbyPlaySession `
        -Root $root `
        -TimeoutSec $TimeoutSec `
        -LoginLoadingPanelPath "/LobbyCanvas/Overlays/SetCLoginLoadingOverlayRoot/LoadingPanel"
    $result.evidence.prepareLobby = $prepare
    $rootSync = Sync-McpRootForSmoke -Root $root -TimeoutSec 30
    $root = $rootSync.root

    $lobbyReady = Wait-PhotonLobbyReadyForSmoke -Root $root -TimeoutSec $TimeoutSec
    $result.evidence.lobbyReady = $lobbyReady
    $rootSync = Sync-McpRootForSmoke -Root $root -TimeoutSec 30
    $root = $rootSync.root
    Start-Sleep -Seconds 2

    $roomSuffix = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
    $result.evidence.roomNameInput = Set-UiValueIfPresent -Root $root -Value "mcp-$roomSuffix" -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/RoomNameInput",
        "RoomNameInput"
    )
    $result.evidence.displayNameInput = Set-UiValueIfPresent -Root $root -Value "MCP Smoke" -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/DisplayNameInput",
        "DisplayNameInput"
    )
    $result.evidence.capacityInput = Set-UiValueIfPresent -Root $root -Value "2" -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/CapacityInput",
        "CapacityInput"
    )

    $createRoom = Invoke-UiInvokeAny -Root $root -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/CreateRoomButton",
        "CreateRoomButton"
    )
    $result.evidence.createRoom = $createRoom

    $roomDetailActive = Wait-UiActiveAny -Root $root -TimeoutMs ($TimeoutSec * 1000) -Paths @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel",
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel",
        "RoomDetailPanel"
    )
    $result.evidence.roomDetailActive = $roomDetailActive

    $ready = Invoke-UiInvokeAny -Root $root -Paths @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel/ReadyButton",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/ReadyButton",
        "ReadyButton"
    )
    $result.evidence.ready = $ready
    Start-Sleep -Seconds 3

    $start = Invoke-UiInvokeAny -Root $root -Paths @(
        "/LobbyCanvas/Overlays/SetCRoomDetailPanelRoot/RoomDetailPanel/StartGameButton",
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/StartGameButton",
        "StartGameButton"
    )
    $result.evidence.start = $start

    $battleSceneActive = Wait-SceneActiveForSmoke -Root $root -SceneName "BattleScene" -TimeoutSec $TimeoutSec
    $result.evidence.battleSceneActive = $battleSceneActive
    $root = $battleSceneActive.root
    $slotReady = Wait-UiComponentAny -Root $root -ComponentType "UnitSlotView" -TimeoutMs ($TimeoutSec * 1000) -Paths @(
        "/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0",
        "UnitSlot-0"
    )
    $result.evidence.slotReady = $slotReady

    $slotClick = Invoke-UiInvokeAny -Root $root -Paths @(
        "/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0",
        "UnitSlot-0"
    )
    $result.evidence.slotClick = $slotClick

    $previewBeforeConfirm = Wait-SceneNodeActive -Root $root -Name "PlacementPreviewVisuals" -ExpectedActive $true -TimeoutSec 10
    $result.evidence.previewBeforeConfirm = @{
        path = $previewBeforeConfirm.path
        activeSelf = $previewBeforeConfirm.activeSelf
    }

    $confirm = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ConfirmPlacementAtPlacementCenter" -Paths @(
        "/BattleHudCanvas/RuntimeBindingLayer/CommandDock",
        "CommandDock"
    )
    $result.evidence.confirmPlacement = $confirm

    $previewAfterConfirm = Wait-SceneNodeActive -Root $root -Name "PlacementPreviewVisuals" -ExpectedActive $false -TimeoutSec 10
    $result.evidence.previewAfterConfirm = @{
        path = $previewAfterConfirm.path
        activeSelf = $previewAfterConfirm.activeSelf
    }

    if ($ResultMode -eq "Defeat") {
        $forced = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceCoreDefeatForMcpSmoke" -Paths @(
            "/BattleSceneSystems",
            "BattleSceneSystems",
            "/GameSceneRoot",
            "GameSceneRoot"
        )
        $result.evidence.resultTrigger = $forced
    }
    elseif ($ResultMode -eq "Victory") {
        $forced = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceVictoryForMcpSmoke" -Paths @(
            "/BattleSceneSystems",
            "BattleSceneSystems",
            "/GameSceneRoot",
            "GameSceneRoot"
        )
        $result.evidence.resultTrigger = $forced
    }
    elseif ($ResultMode -eq "NaturalVictory") {
        $naturalVictory = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "RunFinalWaveClearForMcpSmoke" -Args @(12, [Math]::Max(30, $TimeoutSec - 20)) -Paths @(
            "/BattleSceneSystems",
            "BattleSceneSystems",
            "/GameSceneRoot",
            "GameSceneRoot"
        )
        $result.evidence.resultTrigger = $naturalVictory
    }

    if ($ResultMode -ne "None") {
        $resultPanel = Wait-UiActiveAny -Root $root -TimeoutMs ($TimeoutSec * 1000) -Paths @(
            "/BattleHudCanvas/RuntimeBindingLayer/WaveEndOverlay/ResultPanel",
            "ResultPanel"
        )
        $result.evidence.resultPanel = $resultPanel

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

    $hasExpectedResult =
        $ResultMode -eq "None" -or
        ($ResultMode -eq "Defeat" -and $result.evidence.hasDefeatResultLog) -or
        (($ResultMode -eq "Victory" -or $ResultMode -eq "NaturalVictory") -and $result.evidence.hasVictoryResultLog)

    $result.success = @($newErrors).Count -eq 0 -and $hasExpectedResult
    $result.terminalVerdict = if ($result.success) { "success" } else { "mismatch" }
    if (-not $result.success) {
        $result.blockedReason = if (@($newErrors).Count -ne 0) { "new-console-errors" } else { "expected-result-log-missing" }
    }
}
catch {
    $result.success = $false
    $result.terminalVerdict = "blocked"
    $result.blockedReason = $_.Exception.Message
}
finally {
    if ($LeavePlayMode) {
        try {
            Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/play/stop" -Body @{} -TimeoutSec 30 -RequestTimeoutSec 30 | Out-Null
            Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/play/wait-for-stop" -Body @{ timeoutMs = 90000; pollIntervalMs = 500 } -TimeoutSec 100 -RequestTimeoutSec 100 | Out-Null
        }
        catch {
            $result.evidence.stopPlayModeError = $_.Exception.Message
        }
    }

    Exit-McpExclusiveOperation -Lock $operationLock

    Ensure-McpParentDirectory -PathValue $OutputPath
    $result | ConvertTo-Json -Depth 12 | Set-Content -Path $OutputPath -Encoding UTF8
    $result | ConvertTo-Json -Depth 8
}
