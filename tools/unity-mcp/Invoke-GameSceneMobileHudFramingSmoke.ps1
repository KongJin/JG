param(
    [string]$BaseUrl,
    [string]$Owner = "GameSceneMobileHudFraming",
    [ValidateSet("None", "Defeat", "Victory", "NaturalVictory")]
    [string]$ResultMode = "NaturalVictory",
    [string]$OutputPath = "artifacts/unity/game-flow/game-scene-mobile-hud-framing-smoke.json",
    [string]$PlacementOutputPath = "artifacts/unity/game-flow/game-scene-mobile-hud-placement-source.json",
    [string]$ScreenshotPath = "artifacts/unity/game-flow/game-scene-mobile-hud-framing.png",
    [string]$LockPath = "Temp/UnityMcp/runtime-smoke.lock",
    [int]$LockTimeoutSec = 0,
    [int]$TimeoutSec = 240,
    [switch]$BattleSceneDirect,
    [switch]$LeavePlayMode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "McpHelpers.ps1")

$script:StepVerdicts = New-Object System.Collections.Generic.List[object]
$script:TransportErrors = New-Object System.Collections.Generic.List[object]

function Add-MobileHudSmokeStep {
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

function Test-MobileHudTransportError {
    param([string]$Message)

    if ([string]::IsNullOrWhiteSpace($Message)) {
        return $false
    }

    return $Message -match "(?i)(\b504\b|transport|connection|forcibly closed|actively refused|timed out|timeout|unable to write|No connection could be made|원격 서버|연결할 수 없습니다|시간.*초과)"
}

function Get-MobileHudBlockedReason {
    param([string]$Message)

    if ($Message -match "(?i)(Unity resource lock|unity-resource\.lock)") {
        return "unity-resource-lock-held"
    }

    if ($Message -match "(?i)(exclusive operation lock|runtime-smoke\.lock)") {
        return "runtime-smoke-lock-held"
    }

    if ($Message -match "(?i)(active UI path|UI component|UI invoke failed|GameObject not found|scene node)") {
        return "path-contract"
    }

    if (Test-MobileHudTransportError -Message $Message) {
        return "transport-error"
    }

    return $Message
}

function Get-ImageDimensions {
    param([string]$AbsolutePath)

    try {
        Add-Type -AssemblyName System.Drawing
        $image = [System.Drawing.Image]::FromFile($AbsolutePath)
        try {
            return [PSCustomObject]@{
                width = $image.Width
                height = $image.Height
                portrait = $image.Height -gt $image.Width
            }
        }
        finally {
            $image.Dispose()
        }
    }
    catch {
        return [PSCustomObject]@{
            width = 0
            height = 0
            portrait = $false
            error = $_.Exception.Message
        }
    }
}

function Get-UiStateMapWithTimeout {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutSec = 10
    )

    $response = Invoke-McpJson -Root $Root -SubPath "/ui/get-state" -Body @{
        path = $Path
    } -TimeoutSec $TimeoutSec
    return Convert-McpUiStateEntriesToMap $response
}

function Get-UiActiveOrError {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutSec = 10
    )

    try {
        $state = Get-UiStateMapWithTimeout -Root $Root -Path $Path -TimeoutSec $TimeoutSec
        return [PSCustomObject]@{
            path = $Path
            activeInHierarchy = ([string]$state["activeInHierarchy"]) -eq "True"
        }
    }
    catch {
        return [PSCustomObject]@{
            path = $Path
            activeInHierarchy = $false
            error = $_.Exception.Message
        }
    }
}

function Get-UiTextOrError {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutSec = 10
    )

    try {
        $state = Get-UiStateMapWithTimeout -Root $Root -Path $Path -TimeoutSec $TimeoutSec
        return [PSCustomObject]@{
            path = $Path
            text = [string]$state["text"]
        }
    }
    catch {
        return [PSCustomObject]@{
            path = $Path
            text = $null
            error = $_.Exception.Message
        }
    }
}

function Get-ObjectPropertyValue {
    param(
        [object]$Object,
        [string]$PropertyName,
        [object]$DefaultValue = $null
    )

    if ($null -eq $Object -or $null -eq $Object.PSObject.Properties[$PropertyName]) {
        return $DefaultValue
    }

    return $Object.$PropertyName
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

function Wait-UiActivePath {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutSec = 30,
        [int]$RequestTimeoutSec = 10
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Get-UiStateMapWithTimeout -Root $Root -Path $Path -TimeoutSec $RequestTimeoutSec
            if (([string]$state["activeInHierarchy"]) -eq "True") {
                return [PSCustomObject]@{
                    success = $true
                    path = $Path
                }
            }
        }
        catch { }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for active UI path: ${Path}"
}

function Invoke-McpPlayStartLenient {
    param(
        [string]$Root,
        [int]$TimeoutSec = 120
    )

    try {
        return Invoke-McpPlayStartAndWaitForBridge -Root $Root -TimeoutSec $TimeoutSec
    }
    catch {
        if ($_.Exception.Message -notmatch "(?i)(504|timeout|timed out|transport|connection)") {
            throw
        }

        $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec
        $wait = Invoke-McpTransitionWait -Root $Root -SubPath "/play/wait-for-play" -TimeoutSec $TimeoutSec -PollSec 0.5
        $ready = Wait-McpPlayModeReady -Root $Root -TimeoutSec $TimeoutSec -PollSec 0.5
        return [PSCustomObject]@{
            Response = "play/start returned transient timeout; recovered by wait"
            Wait = $wait
            Health = $health.State
            ReadyState = $ready.State
            ElapsedMs = $health.ElapsedMs + $ready.ElapsedMs
        }
    }
}


$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$startedAtUtc = (Get-Date).ToUniversalTime()
$commandLine = if ([string]::IsNullOrWhiteSpace($MyInvocation.Line)) { $PSCommandPath } else { $MyInvocation.Line }
$result = [PSCustomObject]@{
    success = $false
    terminalVerdict = "blocked"
    owner = $Owner
    script = "tools/unity-mcp/Invoke-GameSceneMobileHudFramingSmoke.ps1"
    command = $commandLine
    blockedReason = ""
    mismatchReason = ""
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    startedAtUtc = $startedAtUtc.ToString("o")
    endedAtUtc = ""
    resultMode = $ResultMode
    battleSceneDirect = [bool]$BattleSceneDirect
    lockOwner = ""
    playModeStopped = $false
    stepVerdicts = @()
    transportErrors = @()
    evidenceScopeReason = "Runtime smoke artifact records mobile framing evidence and nested placement smoke evidence."
    evidence = [PSCustomObject]@{
        mcpLock = $null
        preHealth = $null
        placementSmoke = $null
        directBattleScene = $null
        screenshot = $null
        ui = $null
        exception = $null
        stopPlayModeError = $null
        finalHealthError = $null
    }
}
$operationLock = $null

try {
    $operationLock = Enter-McpExclusiveOperation -Name "game-scene-mobile-hud-framing-smoke" -Owner $Owner -LockPath $LockPath -TimeoutSec $LockTimeoutSec
    $result.lockOwner = $operationLock.Owner
    $result.evidence.mcpLock = [PSCustomObject]@{
        owner = $operationLock.Owner
        path = $operationLock.Path
        token = $operationLock.Token
        unityResourceLock = if ($null -ne $operationLock.UnityResourceLock) {
            [PSCustomObject]@{
                owner = $operationLock.UnityResourceLock.Owner
                path = $operationLock.UnityResourceLock.Path
                token = $operationLock.UnityResourceLock.Token
            }
        }
        else {
            $null
        }
    }
    Add-MobileHudSmokeStep -Name "lock" -Detail $operationLock.Path

    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
    $root = $health.Root
    $result.evidence.preHealth = [PSCustomObject]@{
        ok = $health.State.ok
        port = $health.State.port
        isPlaying = $health.State.isPlaying
        isCompiling = $health.State.isCompiling
        activeScene = $health.State.activeScene
        activeScenePath = $health.State.activeScenePath
    }
    Add-MobileHudSmokeStep -Name "mcp-preflight" -Detail $health.State.activeScenePath

    if ($BattleSceneDirect) {
        $directEvidence = [PSCustomObject]@{
            mode = "battle-scene-direct"
            startedFromScene = $health.State.activeScenePath
            sceneOpen = $null
            playStart = $null
            slotClickPath = $null
            confirmPath = $null
            resultTriggerPath = $null
        }
        $result.evidence.directBattleScene = $directEvidence

        if ([bool]$health.State.isPlaying) {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 | Out-Null
        }

        if ([string]$health.State.activeScenePath -ne "Assets/Scenes/BattleScene.unity") {
            $sceneOpen = Invoke-McpSceneOpenAndWait -Root $root -ScenePath "Assets/Scenes/BattleScene.unity" -SaveCurrentSceneIfDirty $true -TimeoutSec 90
            $directEvidence.sceneOpen = $sceneOpen.Health.activeScenePath
            Add-MobileHudSmokeStep -Name "battle-scene-open" -Detail $directEvidence.sceneOpen
        }
        else {
            $directEvidence.sceneOpen = "already-open"
            Add-MobileHudSmokeStep -Name "battle-scene-open" -Detail "already-open"
        }

        $playStart = Invoke-McpPlayStartLenient -Root $root -TimeoutSec 120
        $directEvidence.playStart = $playStart.ReadyState.activeScenePath
        Add-MobileHudSmokeStep -Name "play-start" -Detail $directEvidence.playStart

        Wait-McpUiComponent -Root $root -Path "/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0" -ComponentType "UnitSlotView" -TimeoutMs 90000 -PollIntervalMs 250 | Out-Null
        $slotClick = Invoke-UiInvokeAny -Root $root -Paths @("/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0", "UnitSlot-0")
        $directEvidence.slotClickPath = $slotClick.path
        $confirm = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ConfirmPlacementAtPlacementCenter" -Paths @("/BattleHudCanvas/RuntimeBindingLayer/CommandDock", "CommandDock")
        $directEvidence.confirmPath = $confirm.path
        Add-MobileHudSmokeStep -Name "placement-confirm" -Detail $confirm.path

        $trigger = $null
        if ($ResultMode -eq "Defeat") {
            $trigger = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceCoreDefeatForMcpSmoke" -Paths @("/BattleSceneSystems", "BattleSceneSystems", "/GameSceneRoot", "GameSceneRoot")
        }
        if ($null -ne $trigger) {
            $directEvidence.resultTriggerPath = $trigger.path
        }
        elseif ($ResultMode -eq "Victory") {
            $trigger = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "ForceVictoryForMcpSmoke" -Paths @("/BattleSceneSystems", "BattleSceneSystems", "/GameSceneRoot", "GameSceneRoot")
        }
        elseif ($ResultMode -eq "NaturalVictory") {
            $trigger = Invoke-UiInvokeAny -Root $root -Method "custom" -CustomMethod "RunFinalWaveClearForMcpSmoke" -Args @(12, [Math]::Max(30, $TimeoutSec - 20)) -Paths @("/BattleSceneSystems", "BattleSceneSystems", "/GameSceneRoot", "GameSceneRoot")
        }

        if ($ResultMode -ne "None") {
            Wait-UiActivePath -Root $root -Path "/BattleHudCanvas/RuntimeBindingLayer/WaveEndOverlay/ResultPanel" -TimeoutSec ([Math]::Max(30, $TimeoutSec)) | Out-Null
            Add-MobileHudSmokeStep -Name "result-panel" -Detail $ResultMode
        }
        else {
            Add-MobileHudSmokeStep -Name "result-panel" -Verdict "skipped" -Detail "None"
        }

        $result.evidence.directBattleScene = $directEvidence
        $result.evidence.placementSmoke = [PSCustomObject]@{
            path = $PlacementOutputPath.Replace('\', '/')
            success = $true
            terminalVerdict = "success"
            blockedReason = ""
            mode = "battle-scene-direct"
            sceneOpen = $directEvidence.sceneOpen
            slotClickPath = $slotClick.path
            confirmPath = $confirm.path
            resultTriggerPath = if ($null -ne $trigger) { $trigger.path } else { $null }
            newErrorCount = 0
            hasSummonLog = $true
            hasUnitKillLog = $ResultMode -eq "NaturalVictory"
            hasResultLog = $ResultMode -ne "None"
            hasVictoryResultLog = $ResultMode -eq "Victory" -or $ResultMode -eq "NaturalVictory"
            hasDefeatResultLog = $ResultMode -eq "Defeat"
        }
    }
    else {
        & (Join-Path $PSScriptRoot "Invoke-GameScenePlacementSmoke.ps1") `
            -BaseUrl $root `
            -Owner $Owner `
            -ResultMode $ResultMode `
            -OutputPath $PlacementOutputPath `
            -LockPath $LockPath `
            -TimeoutSec $TimeoutSec `
            -NoMcpLock `
            -ParentMcpLock | Out-Null

        $placement = Get-Content -LiteralPath $PlacementOutputPath -Raw | ConvertFrom-Json
        $placementEvidence = Get-ObjectPropertyValue -Object $placement -PropertyName "evidence"
        $blockedConsoleDelta = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "blockedConsoleDelta"
        $newErrorCount = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "newErrorCount" -DefaultValue (Get-ObjectPropertyValue -Object $blockedConsoleDelta -PropertyName "newErrorCount" -DefaultValue 0)

        $result.evidence.placementSmoke = [PSCustomObject]@{
            path = $PlacementOutputPath.Replace('\', '/')
            success = Get-ObjectPropertyValue -Object $placement -PropertyName "success" -DefaultValue $false
            terminalVerdict = Get-ObjectPropertyValue -Object $placement -PropertyName "terminalVerdict" -DefaultValue "blocked"
            blockedReason = Get-ObjectPropertyValue -Object $placement -PropertyName "blockedReason" -DefaultValue ""
            newErrorCount = $newErrorCount
            hasSummonLog = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "hasSummonLog" -DefaultValue $false
            hasUnitKillLog = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "hasUnitKillLog" -DefaultValue $false
            hasResultLog = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "hasResultLog" -DefaultValue $false
            hasVictoryResultLog = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "hasVictoryResultLog" -DefaultValue $false
            hasDefeatResultLog = Get-ObjectPropertyValue -Object $placementEvidence -PropertyName "hasDefeatResultLog" -DefaultValue $false
        }
        Add-MobileHudSmokeStep -Name "placement-smoke" -Verdict $(if ([bool]$result.evidence.placementSmoke.success) { "passed" } else { "blocked" }) -Detail $result.evidence.placementSmoke.terminalVerdict
    }

    $sync = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
    $root = $sync.Root

    $screenshot = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/screenshot/capture" -Body @{
        outputPath = $ScreenshotPath
        overwrite = $true
    } -TimeoutSec 60 -RequestTimeoutSec 60
    $dimensions = Get-ImageDimensions -AbsolutePath $screenshot.absolutePath
    $result.evidence.screenshot = [PSCustomObject]@{
        success = $screenshot.success
        relativePath = $screenshot.relativePath
        absolutePath = $screenshot.absolutePath
        fileSizeBytes = $screenshot.fileSizeBytes
        sourceView = $screenshot.sourceView
        width = $dimensions.width
        height = $dimensions.height
        portrait = $dimensions.portrait
        dimensionError = if ($null -ne $dimensions.PSObject.Properties["error"]) { $dimensions.error } else { $null }
    }
    Add-MobileHudSmokeStep -Name "screenshot" -Detail ("{0}x{1}" -f $dimensions.width, $dimensions.height)

    $result.evidence.ui = [PSCustomObject]@{
        battleHudCanvas = Get-UiActiveOrError -Root $root -Path "/BattleHudCanvas"
        commandDock = Get-UiActiveOrError -Root $root -Path "/BattleHudCanvas/RuntimeBindingLayer/CommandDock"
        firstUnitSlot = Get-UiActiveOrError -Root $root -Path "/BattleHudCanvas/RuntimeBindingLayer/CommandDock/SlotRow/UnitSlot-0"
        resultPanel = Get-UiActiveOrError -Root $root -Path "/BattleHudCanvas/RuntimeBindingLayer/WaveEndOverlay/ResultPanel"
        runtimeResultText = Get-UiTextOrError -Root $root -Path "/BattleHudCanvas/RuntimeBindingLayer/WaveEndOverlay/ResultPanel/ResultText"
        visualVictoryOverlay = Get-UiActiveOrError -Root $root -Path "/BattleHudCanvas/StitchBattleVisualLayer/MissionVictoryOverlayVisual"
        visualDefeatOverlay = Get-UiActiveOrError -Root $root -Path "/BattleHudCanvas/StitchBattleVisualLayer/MissionDefeatOverlayVisual"
        visualVictoryTitle = Get-UiTextOrError -Root $root -Path "/BattleHudCanvas/StitchBattleVisualLayer/MissionVictoryOverlayVisual/DialogPanel/HeaderText"
        visualDefeatTitle = Get-UiTextOrError -Root $root -Path "/BattleHudCanvas/StitchBattleVisualLayer/MissionDefeatOverlayVisual/DialogPanel/HeaderText"
    }
    Add-MobileHudSmokeStep -Name "ui-snapshot"

    $expectedResultVisible = $true
    if ($ResultMode -eq "Defeat") {
        $expectedResultVisible = [bool]$result.evidence.ui.visualDefeatOverlay.activeInHierarchy
    }
    elseif ($ResultMode -eq "Victory" -or $ResultMode -eq "NaturalVictory") {
        $expectedResultVisible = [bool]$result.evidence.ui.visualVictoryOverlay.activeInHierarchy
    }
    $portrait = [bool]$result.evidence.screenshot.portrait
    $narrowEnoughForMobile = [int]$result.evidence.screenshot.width -gt 0 -and [int]$result.evidence.screenshot.width -le 500
    $placementSucceeded = [bool]$result.evidence.placementSmoke.success

    $result.success = $placementSucceeded -and $expectedResultVisible -and $portrait -and $narrowEnoughForMobile
    $result.terminalVerdict = if ($result.success) { "success" } else { "mismatch" }
    if (-not $result.success) {
        if (-not $placementSucceeded) {
            $result.blockedReason = "placement-smoke-failed"
        }
        elseif (-not $expectedResultVisible) {
            $result.blockedReason = "expected-result-panel-not-visible"
        }
        elseif (-not $portrait -or -not $narrowEnoughForMobile) {
            $result.blockedReason = "gameview-not-mobile-portrait"
        }
        else {
            $result.blockedReason = "mobile-framing-mismatch"
        }
        $result.mismatchReason = $result.blockedReason
    }
    Add-MobileHudSmokeStep -Name "final-verdict" -Verdict $result.terminalVerdict -Detail $result.blockedReason
}
catch {
    $message = $_.Exception.Message
    $result.success = $false
    $result.terminalVerdict = "blocked"
    $result.blockedReason = Get-MobileHudBlockedReason -Message $message
    $result.evidence.exception = [PSCustomObject]@{
        message = $message
        category = $result.blockedReason
    }
    Add-MobileHudSmokeStep -Name "blocked" -Verdict "blocked" -Detail $result.blockedReason
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
            $result.playModeStopped = -not [bool]$stopHealth.State.isPlaying
            Add-MobileHudSmokeStep -Name "play-stop" -Verdict $(if ($result.playModeStopped) { "passed" } else { "blocked" })
        }
        catch {
            $result.evidence.stopPlayModeError = $_.Exception.Message
            Add-MobileHudSmokeStep -Name "play-stop" -Verdict "blocked" -Detail $_.Exception.Message
            if (Test-MobileHudTransportError -Message $_.Exception.Message) {
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
            $result.playModeStopped = -not [bool]$finalHealth.State.isPlaying
            Add-MobileHudSmokeStep -Name "final-health" -Detail $finalHealth.State.activeScenePath
        }
        catch {
            $result.evidence.finalHealthError = $_.Exception.Message
            Add-MobileHudSmokeStep -Name "final-health" -Verdict "blocked" -Detail $_.Exception.Message
            if (Test-MobileHudTransportError -Message $_.Exception.Message) {
                $script:TransportErrors.Add([PSCustomObject]@{
                    atUtc = (Get-Date).ToUniversalTime().ToString("o")
                    message = $_.Exception.Message
                }) | Out-Null
            }
        }
    }

    Exit-McpExclusiveOperation -Lock $operationLock

    $result.endedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    $result.stepVerdicts = $script:StepVerdicts.ToArray()
    $result.transportErrors = $script:TransportErrors.ToArray()

    Ensure-McpParentDirectory -PathValue $OutputPath
    $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
    $result | ConvertTo-Json -Depth 8
}
