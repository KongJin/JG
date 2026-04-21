param(
    [Parameter(Mandatory = $true)][string]$ScreenManifestPath,
    [string]$ScreenIntakePath = "",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = "artifacts/unity/stitch-garage-translation-result.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/McpHelpers.ps1"
. "$PSScriptRoot/McpPrefabPackHelpers.ps1"

function Get-RequiredProperty {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $InputObject -or $null -eq $InputObject.PSObject.Properties[$Name]) {
        throw "Required property '$Name' is missing."
    }

    return $InputObject.PSObject.Properties[$Name].Value
}

function Resolve-StagePath {
    param(
        [Parameter(Mandatory = $true)][string]$StageRootPath,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    if ($RelativePath.StartsWith("/")) {
        return $RelativePath
    }

    $trimmed = $RelativePath.Trim("/")
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return $StageRootPath
    }

    return "$StageRootPath/$trimmed"
}

function Resolve-HierarchyChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$AbsoluteRootPath,
        [Parameter(Mandatory = $true)][string]$NodePath
    )

    if ($NodePath.StartsWith($AbsoluteRootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $NodePath
    }

    $rootName = "/" + (($AbsoluteRootPath.Trim("/") -split "/") | Select-Object -Last 1)
    if ($NodePath.StartsWith($rootName, [System.StringComparison]::OrdinalIgnoreCase)) {
        $suffix = $NodePath.Substring($rootName.Length)
        return "$AbsoluteRootPath$suffix"
    }

    return Resolve-StagePath -StageRootPath $AbsoluteRootPath -RelativePath $NodePath
}

function Get-McpSceneHierarchyNode {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path,
        [int]$Depth = 8,
        [int]$TimeoutSec = 15,
        [double]$PollSec = 0.5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $response = Invoke-McpGetJsonWithTransientRetry -Root $Root -SubPath "/scene/hierarchy?depth=$Depth&includeComponents=false"
        if ($null -ne $response -and $null -ne $response.nodes -and $response.nodes.Count -gt 0) {
            $queue = [System.Collections.Generic.Queue[object]]::new()
            foreach ($node in $response.nodes) {
                $queue.Enqueue($node)
            }

            while ($queue.Count -gt 0) {
                $node = $queue.Dequeue()
                if ([string]$node.path -eq $Path) {
                    return $node
                }

                foreach ($child in @($node.children)) {
                    $queue.Enqueue($child)
                }
            }
        }

        Start-Sleep -Seconds $PollSec
    }

    throw "Hierarchy node not found: $Path"
}

function Test-McpGameObjectExists {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    try {
        $response = Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/gameobject/find" -Body @{
            path = $Path
            lightweight = $true
        } -TimeoutSec 15

        return ($null -ne $response -and $response.found)
    }
    catch {
        return $false
    }
}

function Set-McpParent {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ParentPath
    )

    Invoke-McpPostJson -Root $Root -SubPath "/gameobject/set-parent" -Body @{
        path = $Path
        parentPath = $ParentPath
    } | Out-Null
}

function Set-McpSibling {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][int]$SiblingIndex
    )

    Invoke-McpPostJson -Root $Root -SubPath "/gameobject/set-sibling" -Body @{
        path = $Path
        siblingIndex = $SiblingIndex
    } | Out-Null
}

function Set-McpPrefabProperty {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$ChildPath,
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Value
    )

    Invoke-McpPostJson -Root $Root -SubPath "/prefab/set" -Body @{
        assetPath = $AssetPath
        childPath = $ChildPath
        componentType = $ComponentType
        propertyName = $PropertyName
        value = $Value
    } | Out-Null
}

function Test-McpPrefabChildExists {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$ChildPath
    )

    try {
        $response = Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/get" -Body @{
            assetPath = $AssetPath
            childPath = $ChildPath
            lightweight = $true
        } -TimeoutSec 15

        return ($null -ne $response -and $response.found)
    }
    catch {
        return $false
    }
}

function New-McpPrefabGameObject {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][string]$Name,
        [string[]]$Components = @()
    )

    Invoke-McpPostJson -Root $Root -SubPath "/prefab/create" -Body @{
        assetPath = $AssetPath
        parentPath = $ParentPath
        name = $Name
        components = $Components
    } | Out-Null
}

function Remove-McpPrefabGameObjectIfExists {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$ChildPath
    )

    if (-not (Test-McpPrefabChildExists -Root $Root -AssetPath $AssetPath -ChildPath $ChildPath)) {
        return
    }

    Invoke-McpPostJson -Root $Root -SubPath "/prefab/destroy" -Body @{
        assetPath = $AssetPath
        childPath = $ChildPath
    } | Out-Null
}

function Set-McpPrefabParent {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$ChildPath,
        [Parameter(Mandatory = $true)][string]$ParentPath
    )

    Invoke-McpPostJson -Root $Root -SubPath "/prefab/set-parent" -Body @{
        assetPath = $AssetPath
        childPath = $ChildPath
        parentPath = $ParentPath
    } | Out-Null
}

function Set-McpPrefabSibling {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$ChildPath,
        [Parameter(Mandatory = $true)][int]$SiblingIndex
    )

    Invoke-McpPostJson -Root $Root -SubPath "/prefab/set-sibling" -Body @{
        assetPath = $AssetPath
        childPath = $ChildPath
        siblingIndex = $SiblingIndex
    } | Out-Null
}

function Get-BlockDefinition {
    param(
        [Parameter(Mandatory = $true)][object]$Blueprint,
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string]$BlockId
    )

    $blueprintBlock = @($Blueprint.blocks | Where-Object { $_.blockId -eq $BlockId }) | Select-Object -First 1
    if ($null -eq $blueprintBlock) {
        throw "Blueprint block '$BlockId' was not found."
    }

    $block = $blueprintBlock.PSObject.Copy()
    $blockOverrides = $Manifest.blockOverrides
    if ($null -ne $blockOverrides -and $null -ne $blockOverrides.PSObject.Properties[$BlockId]) {
        $override = $blockOverrides.PSObject.Properties[$BlockId].Value
        foreach ($property in $override.PSObject.Properties) {
            $block.PSObject.Properties.Remove($property.Name) | Out-Null
            $block | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value
        }
    }

    return $block
}

function Get-IntakeCtaLabel {
    param(
        [Parameter(Mandatory = $true)][object]$Intake,
        [Parameter(Mandatory = $true)][string]$CtaId
    )

    $cta = @($Intake.ctaPriority | Where-Object { $_.id -eq $CtaId }) | Select-Object -First 1
    if ($null -eq $cta) {
        return $null
    }

    return [string]$cta.label
}

function Convert-ToPrefabChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$SceneRootPath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    if (-not $TargetPath.StartsWith("/")) {
        return $TargetPath.Trim("/")
    }

    if ($TargetPath.StartsWith($SceneRootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $TargetPath.Substring($SceneRootPath.Length).Trim("/")
    }

    return $TargetPath.Trim("/")
}

function Get-GarageSlotNamesFromPrefabYaml {
    param([Parameter(Mandatory = $true)][string]$PrefabPath)

    $yaml = Get-Content -Path $PrefabPath -Raw
    $matches = [regex]::Matches($yaml, '(?m)^\s*m_Name:\s+(GarageSlot\d+)\s*$')
    $names = @($matches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    if ($names.Count -eq 0) {
        throw "Could not derive Garage slot item names from prefab YAML: $PrefabPath"
    }

    return $names | Sort-Object {
        if ($_ -match '^GarageSlot(\d+)$') { [int]$Matches[1] } else { [int]::MaxValue }
    }
}

function Ensure-GarageSlotStrip {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$PrefabPath,
        [Parameter(Mandatory = $true)][string]$SlotPaneChildPath,
        [Parameter(Mandatory = $true)][object]$SlotBlock,
        [Parameter(Mandatory = $true)][string]$SceneRootPath
    )

    if ($SlotBlock.layout.axis -ne "horizontal") {
        return [PSCustomObject]@{
            changed = $false
            rowPath = ""
            itemPaths = @()
            reason = "slot-selector axis is not horizontal"
        }
    }

    $slotPanePath = Resolve-StagePath -StageRootPath $SceneRootPath -RelativePath $SlotPaneChildPath
    $rowChildPath = "$SlotPaneChildPath/SlotStripRow"
    $rowPath = "$slotPanePath/SlotStripRow"
    $legacyContainerChildPath = "$SlotPaneChildPath/MobileSlotGrid"

    $hasLegacyContainer = Test-McpPrefabChildExists -Root $Root -AssetPath $PrefabPath -ChildPath $legacyContainerChildPath
    $hasSlotStripRow = Test-McpPrefabChildExists -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath

    if (-not $hasLegacyContainer -and -not $hasSlotStripRow) {
        throw "Expected slot container was not found in prefab asset: $legacyContainerChildPath or $rowChildPath"
    }

    $currentContainerChildPath = if ($hasLegacyContainer) { $legacyContainerChildPath } else { $rowChildPath }
    $rowCreated = $false
    if (-not $hasSlotStripRow) {
        New-McpPrefabGameObject -Root $Root -AssetPath $PrefabPath -Name "SlotStripRow" -ParentPath $SlotPaneChildPath -Components @(
            "RectTransform",
            "ContentSizeFitter",
            "LayoutElement",
            "HorizontalLayoutGroup"
        )
        $rowCreated = $true
    }

    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "RectTransform" -PropertyName "m_AnchorMin" -Value "(0,1)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "RectTransform" -PropertyName "m_AnchorMax" -Value "(1,1)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "RectTransform" -PropertyName "m_Pivot" -Value "(0.5,1)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "RectTransform" -PropertyName "m_AnchoredPosition" -Value "(0,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "RectTransform" -PropertyName "m_SizeDelta" -Value "(0,0)"

    $gap = [string](Get-RequiredProperty -InputObject $SlotBlock.layout -Name "gap")
    $padding = [int](Get-RequiredProperty -InputObject $SlotBlock.layout -Name "padding")
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value $gap
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$padding)
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$padding)
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$padding)
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value ([string]$padding)
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "true"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "ContentSizeFitter" -PropertyName "m_HorizontalFit" -Value "0"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "ContentSizeFitter" -PropertyName "m_VerticalFit" -Value "2"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "118"

    $slotNames = Get-GarageSlotNamesFromPrefabYaml -PrefabPath $PrefabPath
    $movedItemPaths = @()
    foreach ($slotName in $slotNames) {
        $slotItemChildPath = "$currentContainerChildPath/$slotName"
        if (-not (Test-McpPrefabChildExists -Root $Root -AssetPath $PrefabPath -ChildPath $slotItemChildPath)) {
            continue
        }

        if ($currentContainerChildPath -ne $rowChildPath) {
            Set-McpPrefabParent -Root $Root -AssetPath $PrefabPath -ChildPath $slotItemChildPath -ParentPath $rowChildPath
        }

        $movedItemPaths += "$rowPath/$slotName"
    }

    if ($movedItemPaths.Count -eq 0) {
        throw "No Garage slot items were moved into the strip row."
    }

    if ($currentContainerChildPath -ne $rowChildPath) {
        Remove-McpPrefabGameObjectIfExists -Root $Root -AssetPath $PrefabPath -ChildPath $currentContainerChildPath
    }

    if ($rowCreated) {
        Set-McpPrefabSibling -Root $Root -AssetPath $PrefabPath -ChildPath $rowChildPath -SiblingIndex 0
    }

    return [PSCustomObject]@{
        changed = ($rowCreated -or $currentContainerChildPath -ne $rowChildPath)
        rowPath = $rowPath
        itemPaths = $movedItemPaths
        reason = "slot-selector axis is horizontal"
    }
}

function Apply-GarageSaveDockLayout {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$PrefabPath,
        [Parameter(Mandatory = $true)][string]$SceneRootPath,
        [Parameter(Mandatory = $true)][string]$SaveDockPath,
        [Parameter(Mandatory = $true)][string]$SaveButtonPath,
        [Parameter(Mandatory = $true)][object]$SaveDockBlock,
        [string]$SaveButtonLabel
    )

    $padding = [int](Get-RequiredProperty -InputObject $SaveDockBlock.layout -Name "padding")
    $doublePadding = $padding * 2
    $sizeDelta = "(-$doublePadding,-$doublePadding)"

    $saveDockChildPath = Convert-ToPrefabChildPath -SceneRootPath $SceneRootPath -TargetPath $SaveDockPath
    $saveButtonChildPath = Convert-ToPrefabChildPath -SceneRootPath $SceneRootPath -TargetPath $SaveButtonPath

    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveDockChildPath -ComponentType "RectTransform" -PropertyName "m_AnchorMin" -Value "(0,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveDockChildPath -ComponentType "RectTransform" -PropertyName "m_AnchorMax" -Value "(1,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveDockChildPath -ComponentType "RectTransform" -PropertyName "m_Pivot" -Value "(0.5,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveDockChildPath -ComponentType "RectTransform" -PropertyName "m_AnchoredPosition" -Value "(0,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveDockChildPath -ComponentType "RectTransform" -PropertyName "m_SizeDelta" -Value "(0,78)"

    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveButtonChildPath -ComponentType "RectTransform" -PropertyName "m_AnchorMin" -Value "(0,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveButtonChildPath -ComponentType "RectTransform" -PropertyName "m_AnchorMax" -Value "(1,1)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveButtonChildPath -ComponentType "RectTransform" -PropertyName "m_Pivot" -Value "(0.5,0.5)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveButtonChildPath -ComponentType "RectTransform" -PropertyName "m_AnchoredPosition" -Value "(0,0)"
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath $saveButtonChildPath -ComponentType "RectTransform" -PropertyName "m_SizeDelta" -Value $sizeDelta

    if (-not [string]::IsNullOrWhiteSpace($SaveButtonLabel)) {
        Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath "$saveButtonChildPath/Text (TMP)" -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $SaveButtonLabel
    }
}

function Apply-GarageSettingsLabel {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$PrefabPath,
        [Parameter(Mandatory = $true)][string]$SceneRootPath,
        [Parameter(Mandatory = $true)][string]$SettingsButtonPath,
        [string]$SettingsLabel
    )

    if ([string]::IsNullOrWhiteSpace($SettingsLabel)) {
        return
    }

    $settingsButtonChildPath = Convert-ToPrefabChildPath -SceneRootPath $SceneRootPath -TargetPath $SettingsButtonPath
    Set-McpPrefabProperty -Root $Root -AssetPath $PrefabPath -ChildPath "$settingsButtonChildPath/Text (TMP)" -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $SettingsLabel
}

function Invoke-GarageManifestTranslation {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][object]$Blueprint,
        [Parameter(Mandatory = $true)][object]$Intake
    )

    $targets = Get-RequiredProperty -InputObject $Manifest -Name "targets"
    $prefabPath = [string](Get-RequiredProperty -InputObject $targets -Name "prefabPath")
    $sceneRootPath = [string](@(Get-RequiredProperty -InputObject $targets -Name "sceneRoots")[0])
    $stageRootPath = $sceneRootPath

    $slotBlock = Get-BlockDefinition -Blueprint $Blueprint -Manifest $Manifest -BlockId "slot-selector"
    $saveDockBlock = Get-BlockDefinition -Blueprint $Blueprint -Manifest $Manifest -BlockId "save-dock"

    $slotPanePath = Resolve-StagePath -StageRootPath $stageRootPath -RelativePath ([string](Get-RequiredProperty -InputObject $slotBlock -Name "unityTargetPath"))
    $slotPaneChildPath = Convert-ToPrefabChildPath -SceneRootPath $sceneRootPath -TargetPath $slotPanePath
    $saveDockPath = Resolve-StagePath -StageRootPath $stageRootPath -RelativePath ([string](Get-RequiredProperty -InputObject $saveDockBlock -Name "unityTargetPath"))

    $saveCta = @($Manifest.ctaPriority | Where-Object { $_.id -eq "save-roster" }) | Select-Object -First 1
    $settingsCta = @($Manifest.ctaPriority | Where-Object { $_.id -eq "open-settings" }) | Select-Object -First 1
    if ($null -eq $saveCta -or $null -eq $settingsCta) {
        throw "Required Garage CTA mappings are missing from the manifest."
    }

    $saveButtonPath = Resolve-StagePath -StageRootPath $stageRootPath -RelativePath ([string]$saveCta.unityTargetPath)
    $settingsButtonPath = Resolve-StagePath -StageRootPath $stageRootPath -RelativePath ([string]$settingsCta.unityTargetPath)

    $slotStripResult = $null
    try {
        $slotStripResult = Ensure-GarageSlotStrip -Root $Root -PrefabPath $prefabPath -SlotPaneChildPath $slotPaneChildPath -SlotBlock $slotBlock -SceneRootPath $sceneRootPath
    }
    catch {
        $slotStripResult = [PSCustomObject]@{
            changed = $false
            rowPath = ""
            itemPaths = @()
            reason = $_.Exception.Message
        }
    }

    Apply-GarageSaveDockLayout -Root $Root -PrefabPath $prefabPath -SceneRootPath $stageRootPath -SaveDockPath $saveDockPath -SaveButtonPath $saveButtonPath -SaveDockBlock $saveDockBlock -SaveButtonLabel (Get-IntakeCtaLabel -Intake $Intake -CtaId "save-roster")
    Apply-GarageSettingsLabel -Root $Root -PrefabPath $prefabPath -SceneRootPath $stageRootPath -SettingsButtonPath $settingsButtonPath -SettingsLabel (Get-IntakeCtaLabel -Intake $Intake -CtaId "open-settings")

    return [PSCustomObject]@{
        prefabPath = $prefabPath
        stageRootPath = $stageRootPath
        slotPanePath = $slotPanePath
        saveDockPath = $saveDockPath
        saveButtonPath = $saveButtonPath
        settingsButtonPath = $settingsButtonPath
        slotStripResult = $slotStripResult
    }
}

if (-not (Test-Path -LiteralPath $ScreenManifestPath)) {
    throw "Manifest not found: $ScreenManifestPath"
}

$resolvedManifestPath = (Resolve-Path -LiteralPath $ScreenManifestPath).Path
$manifest = Get-Content -Path $resolvedManifestPath -Raw | ConvertFrom-Json
$extends = [string](Get-RequiredProperty -InputObject $manifest -Name "extends")
if ($extends -ne "garage-workspace") {
    throw "Only garage-workspace manifests are supported. Current extends='$extends'."
}

$surfaceId = [string](Get-RequiredProperty -InputObject $manifest -Name "surfaceId")
if ($surfaceId -ne "garage-main-workspace") {
    throw "This first contract-driven garage translator currently supports only 'garage-main-workspace'. Current surfaceId='$surfaceId'."
}

if ([string]::IsNullOrWhiteSpace($ScreenIntakePath)) {
    $candidate = Join-Path (Split-Path -Parent $resolvedManifestPath) "..\intakes\$surfaceId.intake.json"
    $normalizedCandidate = [System.IO.Path]::GetFullPath($candidate)
    if (Test-Path -LiteralPath $normalizedCandidate) {
        $ScreenIntakePath = $normalizedCandidate
    }
    else {
        $ScreenIntakePath = Join-Path (Split-Path -Parent $resolvedManifestPath) "..\intakes\set-b-garage-main-workspace.intake.json"
    }
}

if (-not (Test-Path -LiteralPath $ScreenIntakePath)) {
    throw "Intake not found: $ScreenIntakePath"
}

$resolvedIntakePath = (Resolve-Path -LiteralPath $ScreenIntakePath).Path
$intake = Get-Content -Path $resolvedIntakePath -Raw | ConvertFrom-Json

$blueprintPath = Join-Path (Split-Path -Parent $resolvedManifestPath) "..\blueprints\$extends.blueprint.json"
$resolvedBlueprintPath = (Resolve-Path -LiteralPath $blueprintPath).Path
$blueprint = Get-Content -Path $resolvedBlueprintPath -Raw | ConvertFrom-Json

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
$compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000

if (-not (Test-McpResponseSuccess -Response $compile.Wait)) {
    throw "Unity compile wait failed before translation."
}

$translation = Invoke-GarageManifestTranslation -Root $root -Manifest $manifest -Blueprint $blueprint -Intake $intake
$slotBlock = Get-BlockDefinition -Blueprint $blueprint -Manifest $manifest -BlockId "slot-selector"
$saveDockBlock = Get-BlockDefinition -Blueprint $blueprint -Manifest $manifest -BlockId "save-dock"
$prefabRoot = Get-McpPrefabNode -Root $root -AssetPath $translation.prefabPath -ChildPath ""
$prefabSaveButton = Get-McpPrefabNode -Root $root -AssetPath $translation.prefabPath -ChildPath "MobileSaveDock/MobileSaveButton"
$prefabSlotStrip = $null
try {
    $prefabSlotStrip = Get-McpPrefabNode -Root $root -AssetPath $translation.prefabPath -ChildPath "GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent/RosterListPane/SlotStripRow"
}
catch {
}

$result = [PSCustomObject]@{
    success = $true
    partial = (-not $translation.slotStripResult.changed)
    manifestPath = $resolvedManifestPath
    intakePath = $resolvedIntakePath
    blueprintPath = $resolvedBlueprintPath
    surfaceId = $surfaceId
    extends = $extends
    prefabPath = $translation.prefabPath
    compileSucceeded = (Test-McpResponseSuccess -Response $compile.Wait)
    bridgeHealth = $health.State
    verifiedChildPaths = @(
        "GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent/RosterListPane/SlotStripRow",
        "MobileSaveDock",
        "MobileSaveDock/MobileSaveButton"
    )
    prefabChecks = [PSCustomObject]@{
        rootFound = ($null -ne $prefabRoot)
        slotStripFound = ($null -ne $prefabSlotStrip)
        saveButtonFound = ($null -ne $prefabSaveButton)
    }
    slotStripResult = $translation.slotStripResult
    appliedContract = [PSCustomObject]@{
        slotSelectorAxis = $slotBlock.layout.axis
        saveDockSticky = $saveDockBlock.layout.sticky
        saveLabel = (Get-IntakeCtaLabel -Intake $intake -CtaId "save-roster")
        settingsLabel = (Get-IntakeCtaLabel -Intake $intake -CtaId "open-settings")
    }
}

Ensure-McpParentDirectory -PathValue $ArtifactPath
$result | ConvertTo-Json -Depth 20 | Set-Content -Path $ArtifactPath -Encoding utf8
$result | ConvertTo-Json -Depth 20
