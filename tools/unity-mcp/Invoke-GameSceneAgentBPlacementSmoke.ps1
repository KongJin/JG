param(
    [string]$BaseUrl,
    [ValidateSet("None", "Defeat", "Victory")]
    [string]$ResultMode = "Defeat",
    [string]$OutputPath = "artifacts/unity/game-scene-agent-b-placement-smoke.json",
    [int]$TimeoutSec = 120,
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
        [string]$CustomMethod
    )

    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($path in $Paths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        try {
            $response = Invoke-McpUiInvoke -Root $Root -Path $path -Method $Method -CustomMethod $CustomMethod
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

            $timestamp = [datetime]::Parse([string]$item.timestampUtc).ToUniversalTime()
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

try {
    Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec | Out-Null
    Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000 | Out-Null

    $prepare = Invoke-McpPrepareLobbyPlaySession -Root $root -TimeoutSec $TimeoutSec
    $result.evidence.prepareLobby = $prepare

    Wait-McpPhotonLobbyReady -Root $root -TimeoutSec $TimeoutSec | Out-Null

    $createRoom = Invoke-UiInvokeAny -Root $root -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomListPanel/CreateRoomButton",
        "CreateRoomButton"
    )
    $result.evidence.createRoom = $createRoom

    Wait-McpUiActive -Root $root -Path "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel" -TimeoutMs ($TimeoutSec * 1000) | Out-Null

    $ready = Invoke-UiInvokeAny -Root $root -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/ReadyButton",
        "ReadyButton"
    )
    $result.evidence.ready = $ready

    $start = Invoke-UiInvokeAny -Root $root -Paths @(
        "/LobbyCanvas/LobbyPageRoot/RoomDetailPanel/StartGameButton",
        "StartGameButton"
    )
    $result.evidence.start = $start

    Wait-McpSceneActive -Root $root -SceneName "BattleScene" -TimeoutSec $TimeoutSec -PollSec 0.25 | Out-Null
    Wait-McpUiComponent -Root $root -Path "UnitSlot-0" -ComponentType "UnitSlotView" -TimeoutMs ($TimeoutSec * 1000) | Out-Null

    $slotClick = Invoke-UiInvokeAny -Root $root -Paths @(
        "UnitSlot-0",
        "/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0"
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
            "/GameSceneRoot",
            "GameSceneRoot"
        )
        $result.evidence.resultTrigger = $forced
    }
    elseif ($ResultMode -eq "Victory") {
        $forced = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceVictoryForMcpSmoke" -Paths @(
            "/GameSceneRoot",
            "GameSceneRoot"
        )
        $result.evidence.resultTrigger = $forced
    }

    if ($ResultMode -ne "None") {
        Wait-McpUiActive -Root $root -Path "ResultPanel" -TimeoutMs ($TimeoutSec * 1000) | Out-Null
    }

    Start-Sleep -Milliseconds 500
    $newLogs = Get-NewConsoleItems -Root $root -StartedAtUtc $startedAtUtc
    $newErrors = @($newLogs | Where-Object { $_.type -eq "Error" -or $_.type -eq "Exception" -or $_.type -eq "Assert" })
    $result.evidence.newErrorCount = @($newErrors).Count
    $result.evidence.hasSummonLog = Test-NewLogContains -Items $newLogs -Pattern "*Summons:*1*"
    $result.evidence.hasUnitKillLog = Test-NewLogContains -Items $newLogs -Pattern "*Unit Kills:*1*"
    $result.evidence.hasResultLog = Test-NewLogContains -Items $newLogs -Pattern "*[GameEnd]*Game Result*"

    $result.success = @($newErrors).Count -eq 0
    $result.terminalVerdict = if ($result.success) { "success" } else { "mismatch" }
    if (-not $result.success) {
        $result.blockedReason = "new-console-errors"
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
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 | Out-Null
        }
        catch {
            $result.evidence.stopPlayModeError = $_.Exception.Message
        }
    }

    Ensure-McpParentDirectory -PathValue $OutputPath
    $result | ConvertTo-Json -Depth 12 | Set-Content -Path $OutputPath -Encoding UTF8
    $result | ConvertTo-Json -Depth 8
}
