param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
    [string]$ControllerPath = "/GarageSetBUitkDocument",
    [string]$FrameSearchText = "body23",
    [string]$FirepowerSearchText = "arm43",
    [string]$MobilitySearchText = "legs24",
    [string]$ScreenshotPath = "artifacts/unity/garage-nova-parts-panel-smoke.png",
    [string]$OutputPath = "artifacts/unity/garage-nova-parts-panel-smoke.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot\..\unity-mcp\McpHelpers.ps1

function Convert-ComponentPropertiesToMap {
    param([object]$Response)

    $map = @{}
    foreach ($property in @($Response.properties)) {
        $map[[string]$property.name] = [string]$property.value
    }

    return $map
}

function Get-GarageSetBUitkSmokeDriverState {
    param(
        [string]$Root,
        [string]$Path
    )

    $response = Invoke-McpJson -Root $Root -SubPath "/component/get" -Body @{
        gameObjectPath = $Path
        componentType = "GarageSetBUitkSmokeDriver"
        propertyNames = @(
            "_lastRenderStatus",
            "_lastInteractionStatus",
            "_selectedSlotIndex",
            "_focusedPart",
            "_partSearchText",
            "_isSettingsOpen"
        )
    }

    $fields = Convert-ComponentPropertiesToMap -Response $response
    return [PSCustomObject]@{
        lastRenderStatus = [string]$fields["_lastRenderStatus"]
        lastInteractionStatus = [string]$fields["_lastInteractionStatus"]
        selectedSlotIndex = [string]$fields["_selectedSlotIndex"]
        focusedPart = [string]$fields["_focusedPart"]
        partSearchText = [string]$fields["_partSearchText"]
        isSettingsOpen = [string]$fields["_isSettingsOpen"]
        raw = $response
    }
}

function Wait-GarageSetBUitkRendered {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutSec = 30
    )

    $state = $null
    Wait-McpCondition `
        -Description "GarageSetBUitkSmokeDriver render status" `
        -TimeoutSec $TimeoutSec `
        -Condition {
            $script:GarageSetBUitkWaitState = Get-GarageSetBUitkSmokeDriverState -Root $Root -Path $Path
            return -not [string]::IsNullOrWhiteSpace($script:GarageSetBUitkWaitState.lastRenderStatus)
        }

    $state = $script:GarageSetBUitkWaitState
    Remove-Variable -Name GarageSetBUitkWaitState -Scope Script -ErrorAction SilentlyContinue
    return $state
}

function Invoke-GarageSetBUitkSmokeDriverMethod {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Method,
        [object[]]$MethodArgs = @()
    )

    return Invoke-McpGameObjectMethod `
        -Root $Root `
        -Path $Path `
        -Method $Method `
        -InvokeArgs $MethodArgs
}

function Invoke-GaragePartSelectionSmokeStep {
    param(
        [string]$Root,
        [string]$ControllerPath,
        [string]$Slot,
        [string]$SearchText
    )

    Invoke-GarageSetBUitkSmokeDriverMethod `
        -Root $Root `
        -Path $ControllerPath `
        -Method "SelectFocusForMcpSmoke" `
        -MethodArgs @($Slot) | Out-Null

    Invoke-GarageSetBUitkSmokeDriverMethod `
        -Root $Root `
        -Path $ControllerPath `
        -Method "SetPartSearchForMcpSmoke" `
        -MethodArgs @($SearchText) | Out-Null

    Invoke-GarageSetBUitkSmokeDriverMethod `
        -Root $Root `
        -Path $ControllerPath `
        -Method "SelectVisiblePartForMcpSmoke" `
        -MethodArgs @($Slot, 0) | Out-Null

    Start-Sleep -Milliseconds 250
    $state = Get-GarageSetBUitkSmokeDriverState -Root $Root -Path $ControllerPath
    $expectedPrefix = "part:$Slot`:"
    if (-not $state.lastInteractionStatus.StartsWith($expectedPrefix, [System.StringComparison]::Ordinal)) {
        throw "Garage SetB UITK part selection failed for $Slot. Expected interaction prefix '$expectedPrefix', actual '$($state.lastInteractionStatus)'."
    }

    return [PSCustomObject]@{
        slot = $Slot
        searchText = $SearchText
        state = $state
    }
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
$root = $health.Root

$prepare = Invoke-McpPrepareLobbyPlaySession `
    -Root $root `
    -ScenePath $ScenePath `
    -LoginLoadingPanelPath "" `
    -TimeoutSec 90

Wait-McpComponent `
    -Root $root `
    -Path $ControllerPath `
    -ComponentType "GarageSetBUitkSmokeDriver" `
    -TimeoutMs 30000 | Out-Null

$initialState = Wait-GarageSetBUitkRendered -Root $root -Path $ControllerPath -TimeoutSec 30
$frameStep = Invoke-GaragePartSelectionSmokeStep -Root $root -ControllerPath $ControllerPath -Slot "Frame" -SearchText $FrameSearchText
$firepowerStep = Invoke-GaragePartSelectionSmokeStep -Root $root -ControllerPath $ControllerPath -Slot "Firepower" -SearchText $FirepowerSearchText
$mobilityStep = Invoke-GaragePartSelectionSmokeStep -Root $root -ControllerPath $ControllerPath -Slot "Mobility" -SearchText $MobilitySearchText
$finalState = Get-GarageSetBUitkSmokeDriverState -Root $root -Path $ControllerPath

$screenshot = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/screenshot/capture" -Body @{
    outputPath = $ScreenshotPath
    overwrite = $true
} -TimeoutSec 60 -RequestTimeoutSec 60
$console = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

$result = [PSCustomObject]@{
    success = $true
    scenePath = $ScenePath
    controllerPath = $ControllerPath
    prepare = $prepare
    initialState = $initialState
    frameSearchText = $FrameSearchText
    frameStep = $frameStep
    firepowerSearchText = $FirepowerSearchText
    firepowerStep = $firepowerStep
    mobilitySearchText = $MobilitySearchText
    mobilityStep = $mobilityStep
    finalState = $finalState
    screenshot = $screenshot
    console = $console
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $directory = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $result | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
}

$result | ConvertTo-Json -Depth 8
