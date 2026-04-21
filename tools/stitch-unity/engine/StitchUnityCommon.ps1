Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-StitchUnityRepoRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
}

$script:StitchUnityRepoRoot = Get-StitchUnityRepoRoot
$script:StitchUnityMcpHelpersPath = Join-Path $script:StitchUnityRepoRoot "tools\unity-mcp\McpHelpers.ps1"
. $script:StitchUnityMcpHelpersPath

function Resolve-StitchUnityRepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $script:StitchUnityRepoRoot $PathValue))
}

function Read-StitchUnityJsonFile {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-StitchUnityRepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "JSON file not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw | ConvertFrom-Json
}

function Get-StitchUnityRequiredProperty {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $InputObject -or $null -eq $InputObject.PSObject.Properties[$Name]) {
        throw "Required property '$Name' is missing."
    }

    return $InputObject.PSObject.Properties[$Name].Value
}

function Get-StitchUnityOptionalPropertyValue {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name,
        [object]$Default = $null
    )

    if ($null -eq $InputObject) {
        return $Default
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return $property.Value
}

function Get-StitchUnityOptionalArray {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $value = Get-StitchUnityOptionalPropertyValue -InputObject $InputObject -Name $Name
    if ($null -eq $value) {
        return @()
    }

    return @($value)
}

function Get-StitchUnityMapPath {
    param(
        [string]$SurfaceId,
        [string]$MapPath
    )

    if (-not [string]::IsNullOrWhiteSpace($MapPath)) {
        return Resolve-StitchUnityRepoPath -PathValue $MapPath
    }

    if ([string]::IsNullOrWhiteSpace($SurfaceId)) {
        throw "SurfaceId or MapPath is required."
    }

    return Resolve-StitchUnityRepoPath -PathValue ".stitch/contracts/mappings/$SurfaceId.unity-map.json"
}

function Get-StitchUnityMapObject {
    param(
        [string]$SurfaceId,
        [string]$MapPath
    )

    $resolvedMapPath = Get-StitchUnityMapPath -SurfaceId $SurfaceId -MapPath $MapPath
    $map = Read-StitchUnityJsonFile -PathValue $resolvedMapPath
    if ([string]::IsNullOrWhiteSpace([string]$map.surfaceId)) {
        throw "unity-map surfaceId is required. path=$resolvedMapPath"
    }

    return [PSCustomObject]@{
        Path = $resolvedMapPath
        Map = $map
    }
}

function Get-StitchUnityContractBundle {
    param([Parameter(Mandatory = $true)][object]$Map)

    $contractRefs = Get-StitchUnityRequiredProperty -InputObject $Map -Name "contractRefs"
    $manifestPath = Resolve-StitchUnityRepoPath -PathValue ([string](Get-StitchUnityRequiredProperty -InputObject $contractRefs -Name "manifestPath"))
    $blueprintPath = Resolve-StitchUnityRepoPath -PathValue ([string](Get-StitchUnityRequiredProperty -InputObject $contractRefs -Name "blueprintPath"))
    $intakePath = ""
    if ($null -ne $contractRefs.PSObject.Properties["intakePath"] -and -not [string]::IsNullOrWhiteSpace([string]$contractRefs.intakePath)) {
        $intakePath = Resolve-StitchUnityRepoPath -PathValue ([string]$contractRefs.intakePath)
    }

    return [PSCustomObject]@{
        manifestPath = $manifestPath
        intakePath = $intakePath
        blueprintPath = $blueprintPath
        manifest = Read-StitchUnityJsonFile -PathValue $manifestPath
        intake = if ([string]::IsNullOrWhiteSpace($intakePath)) { $null } else { Read-StitchUnityJsonFile -PathValue $intakePath }
        blueprint = Read-StitchUnityJsonFile -PathValue $blueprintPath
    }
}

function Get-StitchUnityBlockEntries {
    param([Parameter(Mandatory = $true)][object]$Map)

    $entries = @()
    foreach ($property in $Map.blocks.PSObject.Properties) {
        $entries += [PSCustomObject]@{
            blockId = [string]$property.Name
            mapping = $property.Value
        }
    }

    return $entries
}

function Get-StitchUnityCandidatePaths {
    param([Parameter(Mandatory = $true)][object]$BlockMapping)

    $paths = New-Object System.Collections.Generic.List[string]

    $hostPath = [string](Get-StitchUnityOptionalPropertyValue -InputObject $BlockMapping -Name "hostPath" -Default "")
    if (-not [string]::IsNullOrWhiteSpace($hostPath)) {
        $paths.Add($hostPath)
    }

    foreach ($alias in @(Get-StitchUnityOptionalArray -InputObject $BlockMapping -Name "aliases")) {
        if ([string]::IsNullOrWhiteSpace([string]$alias)) {
            continue
        }

        if (-not $paths.Contains([string]$alias)) {
            $paths.Add([string]$alias)
        }
    }

    return @($paths)
}

function Get-StitchUnityArtifactPath {
    param(
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Map.PSObject.Properties["artifactPaths"]) {
        return ""
    }

    $artifactPaths = $Map.artifactPaths
    if ($null -eq $artifactPaths.PSObject.Properties[$Name]) {
        return ""
    }

    $pathValue = [string]$artifactPaths.PSObject.Properties[$Name].Value
    if ([string]::IsNullOrWhiteSpace($pathValue)) {
        return ""
    }

    return Resolve-StitchUnityRepoPath -PathValue $pathValue
}

function Get-StitchUnityMcpRoot {
    param([string]$UnityBridgeUrl)

    return Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
}

function Invoke-StitchUnityTargetLookup {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Target,
        [Parameter(Mandatory = $true)][string]$PathValue
    )

    $kind = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "kind")
    if ($kind -eq "prefab") {
        return Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/get" -Body @{
            assetPath = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "assetPath")
            childPath = $PathValue
            lightweight = $false
        } -TimeoutSec 20
    }

    if ($kind -eq "scene") {
        return Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/gameobject/find" -Body @{
            path = $PathValue
            lightweight = $false
        } -TimeoutSec 20
    }

    throw "Unsupported target kind: $kind"
}

function Get-StitchUnityComponentPropertyValue {
    param(
        [Parameter(Mandatory = $true)][object]$Node,
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$PropertyName
    )

    foreach ($component in @($Node.components)) {
        if ([string]$component.typeName -ne $ComponentType) {
            continue
        }

        foreach ($property in @($component.properties)) {
            if ([string]$property.name -eq $PropertyName) {
                return $property.value
            }
        }
    }

    return $null
}

function Convert-StitchUnityNumber {
    param([object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    $text = [string]$Value
    $match = [regex]::Match($text, '-?\d+(\.\d+)?')
    if (-not $match.Success) {
        return $null
    }

    return [double]::Parse($match.Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Convert-StitchUnityVector2 {
    param([object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    $text = [string]$Value
    $match = [regex]::Match($text, '\(\s*(-?\d+(\.\d+)?)\s*,\s*(-?\d+(\.\d+)?)\s*\)')
    if (-not $match.Success) {
        return $null
    }

    return [PSCustomObject]@{
        x = [double]::Parse($match.Groups[1].Value, [System.Globalization.CultureInfo]::InvariantCulture)
        y = [double]::Parse($match.Groups[3].Value, [System.Globalization.CultureInfo]::InvariantCulture)
    }
}

function Get-StitchUnityActualAxis {
    param([string[]]$ComponentTypes)

    if ($ComponentTypes -contains "HorizontalLayoutGroup") {
        return "horizontal"
    }

    if ($ComponentTypes -contains "VerticalLayoutGroup") {
        return "vertical"
    }

    return "none"
}

function Get-StitchUnityLayoutSnapshot {
    param([Parameter(Mandatory = $true)][object]$Node)

    $componentTypes = @(@($Node.components) | ForEach-Object { [string]$_.typeName })
    $sizeDelta = Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "RectTransform" -PropertyName "m_SizeDelta"
    $sizeDeltaVector = Convert-StitchUnityVector2 -Value $sizeDelta
    return [PSCustomObject]@{
        axis = Get-StitchUnityActualAxis -ComponentTypes $componentTypes
        preferredWidth = Convert-StitchUnityNumber (Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth")
        preferredHeight = Convert-StitchUnityNumber (Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight")
        minWidth = Convert-StitchUnityNumber (Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "LayoutElement" -PropertyName "m_MinWidth")
        minHeight = Convert-StitchUnityNumber (Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "LayoutElement" -PropertyName "m_MinHeight")
        spacing = Convert-StitchUnityNumber (Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing")
        verticalSpacing = Convert-StitchUnityNumber (Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing")
        sizeDelta = $sizeDelta
        sizeDeltaWidth = if ($null -ne $sizeDeltaVector) { $sizeDeltaVector.x } else { $null }
        sizeDeltaHeight = if ($null -ne $sizeDeltaVector) { $sizeDeltaVector.y } else { $null }
        anchorMin = Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "RectTransform" -PropertyName "m_AnchorMin"
        anchorMax = Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "RectTransform" -PropertyName "m_AnchorMax"
        pivot = Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "RectTransform" -PropertyName "m_Pivot"
        color = Get-StitchUnityComponentPropertyValue -Node $Node -ComponentType "Image" -PropertyName "m_Color"
    }
}

function Resolve-StitchUnityBlockNode {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Target,
        [Parameter(Mandatory = $true)][string]$BlockId,
        [Parameter(Mandatory = $true)][object]$BlockMapping
    )

    $candidatePaths = @(Get-StitchUnityCandidatePaths -BlockMapping $BlockMapping)
    if ($candidatePaths.Count -eq 0) {
        throw "Block '$BlockId' does not define hostPath or aliases."
    }

    foreach ($candidatePath in $candidatePaths) {
        try {
            $node = Invoke-StitchUnityTargetLookup -Root $Root -Target $Target -PathValue $candidatePath
            if ($null -ne $node -and $node.found) {
                $resolvedBy = if ([string]$candidatePath -eq [string]$BlockMapping.hostPath) { "hostPath" } else { "alias" }
                return [PSCustomObject]@{
                    found = $true
                    resolvedPath = $candidatePath
                    resolvedBy = $resolvedBy
                    node = $node
                }
            }
        }
        catch {
        }
    }

    return [PSCustomObject]@{
        found = $false
        resolvedPath = ""
        resolvedBy = ""
        node = $null
    }
}

function Get-StitchUnitySurfaceInspectionObject {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Map,
        [object]$ContractBundle = $null
    )

    if ($null -eq $ContractBundle) {
        $ContractBundle = Get-StitchUnityContractBundle -Map $Map
    }

    $blockResults = @()
    foreach ($entry in @(Get-StitchUnityBlockEntries -Map $Map)) {
        $blockMapping = $entry.mapping
        $resolved = Resolve-StitchUnityBlockNode -Root $Root -Target $Map.target -BlockId $entry.blockId -BlockMapping $blockMapping
        $componentTypes = @()
        $missingComponents = @()
        $layout = $null

        if ($resolved.found) {
            $componentTypes = @(@($resolved.node.components) | ForEach-Object { [string]$_.typeName })
            $layout = Get-StitchUnityLayoutSnapshot -Node $resolved.node

            foreach ($required in @(Get-StitchUnityOptionalArray -InputObject $blockMapping -Name "requiredComponents")) {
                if ([string]::IsNullOrWhiteSpace([string]$required)) {
                    continue
                }

                if ($componentTypes -notcontains [string]$required) {
                    $missingComponents += [string]$required
                }
            }
        }

        $blockResults += [PSCustomObject]@{
            blockId = $entry.blockId
            hostPath = [string](Get-StitchUnityOptionalPropertyValue -InputObject $blockMapping -Name "hostPath" -Default "")
            candidatePaths = @(Get-StitchUnityCandidatePaths -BlockMapping $blockMapping)
            found = $resolved.found
            resolvedPath = $resolved.resolvedPath
            resolvedBy = $resolved.resolvedBy
            componentTypes = $componentTypes
            missingComponents = $missingComponents
            expectedLayout = Get-StitchUnityOptionalPropertyValue -InputObject $blockMapping -Name "expectedLayout"
            actualLayout = $layout
            verificationTags = @(Get-StitchUnityOptionalArray -InputObject $blockMapping -Name "verificationTags")
            notes = @(Get-StitchUnityOptionalArray -InputObject $blockMapping -Name "notes")
        }
    }

    $missingBlocks = @($blockResults | Where-Object { -not $_.found } | ForEach-Object { $_.blockId })
    return [PSCustomObject]@{
        schemaVersion = "1.0.0"
        surfaceId = [string]$Map.surfaceId
        mapPath = ""
        target = $Map.target
        translationStrategy = [string]$Map.translationStrategy
        contractRefs = [PSCustomObject]@{
            manifestPath = $ContractBundle.manifestPath
            intakePath = $ContractBundle.intakePath
            blueprintPath = $ContractBundle.blueprintPath
        }
        blocks = $blockResults
        missingBlocks = $missingBlocks
        generatedAt = (Get-Date).ToString("o")
    }
}

function Get-StitchUnitySurfaceVerificationObject {
    param(
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][object]$Inspection
    )

    $errors = @()
    $warnings = @()
    $findings = @()

    $requiredBlocks = @()
    if ($null -ne $Map.PSObject.Properties["verification"] -and $null -ne $Map.verification.PSObject.Properties["requiredBlocks"]) {
        $requiredBlocks = @($Map.verification.requiredBlocks)
    }
    if ($requiredBlocks.Count -eq 0) {
        $requiredBlocks = @(@($Inspection.blocks) | ForEach-Object { $_.blockId })
    }

    foreach ($requiredBlockId in $requiredBlocks) {
        $block = @($Inspection.blocks | Where-Object { $_.blockId -eq $requiredBlockId }) | Select-Object -First 1
        if ($null -eq $block -or -not $block.found) {
            $errors += [PSCustomObject]@{
                code = "missing-block"
                blockId = $requiredBlockId
                message = "Required block '$requiredBlockId' was not found in the Unity target."
            }
        }
    }

    foreach ($block in @($Inspection.blocks)) {
        if (-not $block.found) {
            continue
        }

        foreach ($componentName in @($block.missingComponents)) {
            $errors += [PSCustomObject]@{
                code = "missing-component"
                blockId = $block.blockId
                message = "Block '$($block.blockId)' is missing required component '$componentName'."
            }
        }

        if ($null -ne $block.expectedLayout) {
            $expectedAxis = ""
            if ($null -ne $block.expectedLayout.PSObject.Properties["axis"]) {
                $expectedAxis = [string]$block.expectedLayout.axis
            }

            if (-not [string]::IsNullOrWhiteSpace($expectedAxis) -and $expectedAxis -ne "none") {
                $actualAxis = ""
                if ($null -ne $block.actualLayout) {
                    $actualAxis = [string]$block.actualLayout.axis
                }

                if (-not [string]::IsNullOrWhiteSpace($actualAxis) -and $actualAxis -ne $expectedAxis) {
                    $errors += [PSCustomObject]@{
                        code = "layout-axis-mismatch"
                        blockId = $block.blockId
                        message = "Block '$($block.blockId)' expected axis '$expectedAxis' but found '$actualAxis'."
                    }
                }
            }

            if ($null -ne $block.expectedLayout.PSObject.Properties["preferredHeight"]) {
                $expectedHeight = [double]$block.expectedLayout.preferredHeight
                $actualHeight = $null
                if ($null -ne $block.actualLayout) {
                    $actualHeight = $block.actualLayout.preferredHeight
                    if ($null -eq $actualHeight -and $null -ne $block.actualLayout.PSObject.Properties["sizeDeltaHeight"]) {
                        $sizeDeltaHeight = $block.actualLayout.sizeDeltaHeight
                        if ($null -ne $sizeDeltaHeight -and [Math]::Abs([double]$sizeDeltaHeight) -gt 0.5) {
                            $actualHeight = [double]$sizeDeltaHeight
                        }
                    }
                }

                if ($null -eq $actualHeight) {
                    $warnings += [PSCustomObject]@{
                        code = "missing-preferred-height"
                        blockId = $block.blockId
                        message = "Block '$($block.blockId)' expected preferredHeight '$expectedHeight' but no LayoutElement preferred height was found."
                    }
                }
                elseif ([Math]::Abs([double]$actualHeight - $expectedHeight) -gt 0.5) {
                    $warnings += [PSCustomObject]@{
                        code = "preferred-height-mismatch"
                        blockId = $block.blockId
                        message = "Block '$($block.blockId)' expected preferredHeight '$expectedHeight' but found '$actualHeight'."
                    }
                }
            }

            if ($null -ne $block.expectedLayout.PSObject.Properties["spacing"]) {
                $expectedSpacing = [double]$block.expectedLayout.spacing
                $actualSpacing = $null
                if ($null -ne $block.actualLayout) {
                    $actualSpacing = if ($null -ne $block.actualLayout.spacing) { $block.actualLayout.spacing } else { $block.actualLayout.verticalSpacing }
                }

                if ($null -eq $actualSpacing) {
                    $warnings += [PSCustomObject]@{
                        code = "missing-spacing"
                        blockId = $block.blockId
                        message = "Block '$($block.blockId)' expected spacing '$expectedSpacing' but no layout group spacing was found."
                    }
                }
                elseif ([Math]::Abs([double]$actualSpacing - $expectedSpacing) -gt 0.5) {
                    $warnings += [PSCustomObject]@{
                        code = "spacing-mismatch"
                        blockId = $block.blockId
                        message = "Block '$($block.blockId)' expected spacing '$expectedSpacing' but found '$actualSpacing'."
                    }
                }
            }
        }

        $findings += "Verified block '$($block.blockId)' via $($block.resolvedBy) at '$($block.resolvedPath)'."
    }

    $primaryCtas = @()
    if ($null -ne $Map.PSObject.Properties["verification"] -and $null -ne $Map.verification.PSObject.Properties["primaryCtas"]) {
        $primaryCtas = @($Map.verification.primaryCtas)
    }
    foreach ($primaryCtaBlockId in $primaryCtas) {
        $block = @($Inspection.blocks | Where-Object { $_.blockId -eq $primaryCtaBlockId }) | Select-Object -First 1
        if ($null -eq $block -or -not $block.found) {
            $errors += [PSCustomObject]@{
                code = "missing-primary-cta"
                blockId = $primaryCtaBlockId
                message = "Primary CTA block '$primaryCtaBlockId' was not found."
            }
        }
    }

    return [PSCustomObject]@{
        schemaVersion = "1.0.0"
        surfaceId = [string]$Map.surfaceId
        ok = ($errors.Count -eq 0)
        structural = ($errors.Count -eq 0)
        semantic = ($warnings.Count -eq 0)
        runtime = $false
        errors = @($errors)
        warnings = @($warnings)
        findings = @($findings)
        generatedAt = (Get-Date).ToString("o")
    }
}

function Write-StitchUnityArtifact {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    Ensure-McpParentDirectory -PathValue $PathValue
    $InputObject | ConvertTo-Json -Depth 20 | Set-Content -Path $PathValue -Encoding utf8
}
