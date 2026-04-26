param(
    [string]$UnityBridgeUrl,
    [string]$FrameSearchText = "body23",
    [string]$FirepowerSearchText = "arm43",
    [string]$MobilitySearchText = "legs24",
    [string]$ScreenshotPath = "artifacts/unity/garage-nova-parts-panel-smoke.png",
    [string]$OutputPath = "artifacts/unity/garage-nova-parts-panel-smoke.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot\..\unity-mcp\McpHelpers.ps1

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
$root = $health.Root

if (-not $health.State.isPlaying) {
    Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec 90 | Out-Null
}

Start-Sleep -Seconds 2

Invoke-McpUiInvoke -Root $root -Path "/LobbyCanvas/LobbyGarageNavBar/GarageTabButton" | Out-Null
Wait-McpUiActive -Root $root -Path "/LobbyCanvas/GaragePageRoot/MobileContentRoot/MobileBodyHost/GarageNovaPartsPanelView" -TimeoutMs 15000 | Out-Null

$panelRoot = "/LobbyCanvas/GaragePageRoot/MobileContentRoot/MobileBodyHost/GarageNovaPartsPanelView"
$title = Get-McpUiTextValue -Root $root -Path "$panelRoot/TitleText"
$countBefore = Get-McpUiTextValue -Root $root -Path "$panelRoot/CountText"

Invoke-McpSetUiValue -Root $root -Path "$panelRoot/SearchInput" -Value $FrameSearchText
Start-Sleep -Seconds 1

$frameRowName = Get-McpUiTextValue -Root $root -Path "$panelRoot/NovaPartRow1/NameText"
$frameRowDetail = Get-McpUiTextValue -Root $root -Path "$panelRoot/NovaPartRow1/DetailText"

Invoke-McpUiInvoke -Root $root -Path "$panelRoot/NovaPartRow1" | Out-Null
Invoke-McpUiInvoke -Root $root -Path "$panelRoot/ApplyButton" | Out-Null
Start-Sleep -Seconds 1

$frameValue = Get-McpUiTextValue -Root $root -Path "/LobbyCanvas/GaragePageRoot/MobileContentRoot/MobileBodyHost/GarageUnitEditorView/FrameSelectorView/ValueText"

Invoke-McpUiInvoke -Root $root -Path "$panelRoot/FirepowerFilterButton" | Out-Null
Invoke-McpSetUiValue -Root $root -Path "$panelRoot/SearchInput" -Value $FirepowerSearchText
Start-Sleep -Seconds 1
$firepowerRowName = Get-McpUiTextValue -Root $root -Path "$panelRoot/NovaPartRow1/NameText"
$firepowerRowDetail = Get-McpUiTextValue -Root $root -Path "$panelRoot/NovaPartRow1/DetailText"
Invoke-McpUiInvoke -Root $root -Path "$panelRoot/NovaPartRow1" | Out-Null
Invoke-McpUiInvoke -Root $root -Path "$panelRoot/ApplyButton" | Out-Null
Start-Sleep -Seconds 1
$firepowerValue = Get-McpUiTextValue -Root $root -Path "/LobbyCanvas/GaragePageRoot/MobileContentRoot/MobileBodyHost/GarageUnitEditorView/FirepowerSelectorView/ValueText"

Invoke-McpUiInvoke -Root $root -Path "$panelRoot/MobilityFilterButton" | Out-Null
Invoke-McpSetUiValue -Root $root -Path "$panelRoot/SearchInput" -Value $MobilitySearchText
Start-Sleep -Seconds 1
$mobilityRowName = Get-McpUiTextValue -Root $root -Path "$panelRoot/NovaPartRow1/NameText"
$mobilityRowDetail = Get-McpUiTextValue -Root $root -Path "$panelRoot/NovaPartRow1/DetailText"
Invoke-McpUiInvoke -Root $root -Path "$panelRoot/NovaPartRow1" | Out-Null
Invoke-McpUiInvoke -Root $root -Path "$panelRoot/ApplyButton" | Out-Null
Start-Sleep -Seconds 1
$mobilityValue = Get-McpUiTextValue -Root $root -Path "/LobbyCanvas/GaragePageRoot/MobileContentRoot/MobileBodyHost/GarageUnitEditorView/MobilitySelectorView/ValueText"

$screenshot = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/screenshot/capture" -Body @{
    outputPath = $ScreenshotPath
    overwrite = $true
} -TimeoutSec 60 -RequestTimeoutSec 60
$console = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

$result = [PSCustomObject]@{
    success = $true
    title = $title
    countBefore = $countBefore
    frameSearchText = $FrameSearchText
    frameRowNameAfterSearch = $frameRowName
    frameRowDetailAfterSearch = $frameRowDetail
    frameValueAfterApply = $frameValue
    firepowerSearchText = $FirepowerSearchText
    firepowerRowNameAfterSearch = $firepowerRowName
    firepowerRowDetailAfterSearch = $firepowerRowDetail
    firepowerValueAfterApply = $firepowerValue
    mobilitySearchText = $MobilitySearchText
    mobilityRowNameAfterSearch = $mobilityRowName
    mobilityRowDetailAfterSearch = $mobilityRowDetail
    mobilityValueAfterApply = $mobilityValue
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
