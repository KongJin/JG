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

function Convert-StitchUnityRepoRelativePath {
    param([Parameter(Mandatory = $true)][string]$AbsolutePath)

    $repoRootWithSeparator = $script:StitchUnityRepoRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $normalizedAbsolutePath = [System.IO.Path]::GetFullPath($AbsolutePath)
    if ($normalizedAbsolutePath.StartsWith($repoRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $normalizedAbsolutePath.Substring($repoRootWithSeparator.Length).Replace('\', '/')
    }

    return $normalizedAbsolutePath.Replace('\', '/')
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
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $InputObject) {
        return $null
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
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

function Get-StitchUnityComponentCatalogPath {
    return Resolve-StitchUnityRepoPath -PathValue ".stitch/contracts/components/shared-ui.component-catalog.json"
}

function Get-StitchUnityProfileGeneratorScriptPath {
    return Resolve-StitchUnityRepoPath -PathValue "tools/stitch-unity/presentations/Generate-StitchPresentationProfile.ps1"
}

function Get-StitchUnityProfileGeneratorProbe {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $generatorPath = Get-StitchUnityProfileGeneratorScriptPath
    if (-not (Test-Path -LiteralPath $generatorPath)) {
        throw "Profile generator script not found: $generatorPath"
    }

    $json = & powershell -NoProfile -ExecutionPolicy Bypass -File $generatorPath -SurfaceId $SurfaceId -CanGenerateOnly 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Profile generator probe failed for '$SurfaceId': $json"
    }

    return ($json | Out-String | ConvertFrom-Json)
}

function Get-StitchUnityGeneratedProfile {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $probe = Get-StitchUnityProfileGeneratorProbe -SurfaceId $SurfaceId
    if (-not [bool]$probe.supported) {
        return $null
    }

    $generatorPath = Get-StitchUnityProfileGeneratorScriptPath
    $json = & powershell -NoProfile -ExecutionPolicy Bypass -File $generatorPath -SurfaceId $SurfaceId 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Profile generation failed for '$SurfaceId': $json"
    }

    $result = $json | Out-String | ConvertFrom-Json
    if ($null -eq $result.PSObject.Properties["profile"] -or $null -eq $result.profile) {
        throw "Profile generation returned no in-memory profile for '$SurfaceId'."
    }

    return $result.profile
}

function Get-StitchUnityOptionalJsonFile {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-StitchUnityRepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        return $null
    }

    return Read-StitchUnityJsonFile -PathValue $resolvedPath
}

function Get-StitchUnityCompiledContractDefinition {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $profile = Get-StitchUnityGeneratedProfile -SurfaceId $SurfaceId
    if ($null -eq $profile) {
        return $null
    }
    $compiler = Get-StitchUnityOptionalPropertyValue -InputObject $profile -Name "compiler"
    if ($null -eq $compiler) {
        return $null
    }

    $family = [string](Get-StitchUnityOptionalPropertyValue -InputObject $profile -Name "family")

    $componentCatalogPath = Get-StitchUnityComponentCatalogPath
    $componentCatalog = if (Test-Path -LiteralPath $componentCatalogPath) {
        Read-StitchUnityJsonFile -PathValue $componentCatalogPath
    }
    else {
        $null
    }

    return [PSCustomObject]@{
        familyId = $family
        profilePath = ""
        profile = $profile
        compiler = $compiler
        componentCatalogPath = $componentCatalogPath
        componentCatalog = $componentCatalog
    }
}

function New-StitchUnityBlockManifestObject {
    param([Parameter(Mandatory = $true)][object]$Definition)

    $manifestBlock = [ordered]@{
        blockId = [string](Get-StitchUnityRequiredProperty -InputObject $Definition -Name "blockId")
        role = [string](Get-StitchUnityRequiredProperty -InputObject $Definition -Name "role")
        sourceName = [string](Get-StitchUnityRequiredProperty -InputObject $Definition -Name "sourceName")
        children = @(Get-StitchUnityOptionalArray -InputObject $Definition -Name "children")
    }

    $componentComposition = @(Get-StitchUnityOptionalArray -InputObject $Definition -Name "componentComposition")
    if ($componentComposition.Count -gt 0) {
        $manifestBlock.componentComposition = @($componentComposition)
    }

    $notes = @(Get-StitchUnityOptionalArray -InputObject $Definition -Name "notes")
    if ($notes.Count -gt 0) {
        $manifestBlock.notes = @($notes)
    }

    return [PSCustomObject]$manifestBlock
}

function New-StitchUnityMapBlockObject {
    param([Parameter(Mandatory = $true)][object]$Definition)

    $blockId = [string](Get-StitchUnityRequiredProperty -InputObject $Definition -Name "blockId")
    $mapBlock = [ordered]@{
        blockId = $blockId
        hostPath = [string](Get-StitchUnityRequiredProperty -InputObject $Definition -Name "hostPath")
    }

    $aliases = @(Get-StitchUnityOptionalArray -InputObject $Definition -Name "aliases")
    if ($aliases.Count -gt 0) {
        $mapBlock.aliases = @($aliases)
    }

    $requiredComponents = @(Get-StitchUnityOptionalArray -InputObject $Definition -Name "requiredComponents")
    if ($requiredComponents.Count -gt 0) {
        $mapBlock.requiredComponents = @($requiredComponents)
    }

    $notes = @(Get-StitchUnityOptionalArray -InputObject $Definition -Name "notes")
    if ($notes.Count -gt 0) {
        $mapBlock.notes = @($notes)
    }

    return [PSCustomObject]$mapBlock
}

function Compile-StitchUnityContracts {
    param([Parameter(Mandatory = $true)][object]$Definition)

    $profile = $Definition.profile
    $compiler = $Definition.compiler
    $defaults = Get-StitchUnityRequiredProperty -InputObject $profile -Name "defaults"
    $surfaceId = [string](Get-StitchUnityRequiredProperty -InputObject $profile -Name "surfaceId")
    $manifestDefinition = Get-StitchUnityRequiredProperty -InputObject $compiler -Name "manifest"
    $mapDefinition = Get-StitchUnityRequiredProperty -InputObject $compiler -Name "map"
    $source = Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "source"
    $target = Get-StitchUnityRequiredProperty -InputObject $mapDefinition -Name "target"
    $artifactPaths = Get-StitchUnityRequiredProperty -InputObject $mapDefinition -Name "artifactPaths"
    $manifestBlockDefinitions = @(Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "blocks")
    $mapBlockDefinitions = @(Get-StitchUnityRequiredProperty -InputObject $mapDefinition -Name "blocks")

    $manifestPath = "in-memory://compiled/{0}.screen-manifest.json" -f $surfaceId
    $mapPath = "in-memory://compiled/{0}.unity-map.json" -f $surfaceId
    $presentationAbsolutePath = Resolve-StitchUnityRepoPath -PathValue ([string](Get-StitchUnityRequiredProperty -InputObject $defaults -Name "outputPath"))
    $presentationPath = Convert-StitchUnityRepoRelativePath -AbsolutePath $presentationAbsolutePath

    $manifestBlocks = @()
    $mapBlocks = [ordered]@{}
    $compiledBlockIds = New-Object System.Collections.Generic.List[string]

    foreach ($blockDefinition in $manifestBlockDefinitions) {
        $manifestBlocks += (New-StitchUnityBlockManifestObject -Definition $blockDefinition)

        $blockId = [string](Get-StitchUnityRequiredProperty -InputObject $blockDefinition -Name "blockId")
        $compiledBlockIds.Add($blockId)
    }

    foreach ($blockDefinition in $mapBlockDefinitions) {
        $mapBlock = New-StitchUnityMapBlockObject -Definition $blockDefinition
        $blockId = [string]$mapBlock.blockId
        if ($mapBlocks.Contains($blockId)) {
            throw "compiler.map.blocks contains duplicate blockId '$blockId' for surface '$surfaceId'."
        }

        $mapBlockData = [ordered]@{
            hostPath = [string]$mapBlock.hostPath
        }
        if ($null -ne $mapBlock.PSObject.Properties["aliases"]) {
            $mapBlockData.aliases = @($mapBlock.aliases)
        }
        if ($null -ne $mapBlock.PSObject.Properties["requiredComponents"]) {
            $mapBlockData.requiredComponents = @($mapBlock.requiredComponents)
        }
        if ($null -ne $mapBlock.PSObject.Properties["notes"]) {
            $mapBlockData.notes = @($mapBlock.notes)
        }

        $mapBlocks[$blockId] = [PSCustomObject]$mapBlockData
    }

    $manifest = [PSCustomObject][ordered]@{
        schemaVersion = "1.1.0"
        contractKind = "screen-manifest"
        setId = [string](Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "setId")
        surfaceId = $surfaceId
        surfaceRole = [string](Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "surfaceRole")
        status = [string](Get-StitchUnityOptionalPropertyValue -InputObject $manifestDefinition -Name "status")
        source = $source
        ctaPriority = @(Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "ctaPriority")
        states = Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "states"
        blocks = @($manifestBlocks)
        validation = Get-StitchUnityRequiredProperty -InputObject $manifestDefinition -Name "validation"
        notes = @(Get-StitchUnityOptionalArray -InputObject $manifestDefinition -Name "notes")
    }
    if ([string]::IsNullOrWhiteSpace([string]$manifest.status)) {
        $manifest.status = "accepted"
    }

    $translationStrategy = [string](Get-StitchUnityRequiredProperty -InputObject $mapDefinition -Name "translationStrategy")
    $strategyMode = [string](Get-StitchUnityRequiredProperty -InputObject $mapDefinition -Name "strategyMode")
    $mapNotes = @(Get-StitchUnityOptionalArray -InputObject $mapDefinition -Name "notes")
    $map = [PSCustomObject][ordered]@{
        schemaVersion = "1.0.0"
        contractKind = "unity-surface-map"
        surfaceId = $surfaceId
        target = $target
        contractRefs = [PSCustomObject][ordered]@{
            manifestPath = $manifestPath
            presentationPath = $presentationPath
        }
        translationStrategy = $translationStrategy
        strategyMode = $strategyMode
        artifactPaths = $artifactPaths
        blocks = [PSCustomObject]$mapBlocks
        notes = @($mapNotes)
    }

    return [PSCustomObject]@{
        manifest = $manifest
        map = $map
        contractBundle = [PSCustomObject]@{
            manifestPath = $manifestPath
            manifest = $manifest
            presentationPath = $presentationPath
            presentation = Get-StitchUnityOptionalJsonFile -PathValue $presentationPath
        }
        contractSource = [PSCustomObject]@{
            sourceKind = "compiled-family"
            generatedFromFamily = $true
            familyId = [string]$Definition.familyId
            profileGenerated = $true
            profilePath = ""
            sharedUiCatalogPath = Convert-StitchUnityRepoRelativePath -AbsolutePath ([string]$Definition.componentCatalogPath)
            sourceRefs = [PSCustomObject]@{
                htmlPath = [string](Get-StitchUnityRequiredProperty -InputObject $defaults -Name "htmlPath")
                imagePath = [string](Get-StitchUnityRequiredProperty -InputObject $defaults -Name "imagePath")
                presentationPath = $presentationPath
            }
            compiledContractSummary = [PSCustomObject]@{
                mapPath = $mapPath
                manifestPath = $manifestPath
                blockIds = @($compiledBlockIds)
                targetAssetPath = [string](Get-StitchUnityRequiredProperty -InputObject $target -Name "assetPath")
                translationStrategy = $translationStrategy
                strategyMode = $strategyMode
            }
        }
    }
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
    $presentationPathValue = [string](Get-StitchUnityOptionalPropertyValue -InputObject $contractRefs -Name "presentationPath")
    $presentationPath = ""
    $presentation = $null

    if (-not [string]::IsNullOrWhiteSpace($presentationPathValue)) {
        $presentationPath = Resolve-StitchUnityRepoPath -PathValue $presentationPathValue
        $presentation = Read-StitchUnityJsonFile -PathValue $presentationPath
    }

    return [PSCustomObject]@{
        manifestPath = $manifestPath
        manifest = Read-StitchUnityJsonFile -PathValue $manifestPath
        presentationPath = $presentationPath
        presentation = $presentation
    }
}

function Get-StitchUnityContractRefObject {
    param([Parameter(Mandatory = $true)][object]$ContractBundle)

    $refs = [ordered]@{
        manifestPath = $ContractBundle.manifestPath
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$ContractBundle.presentationPath)) {
        $refs.presentationPath = $ContractBundle.presentationPath
    }

    return [PSCustomObject]$refs
}

function Get-StitchUnitySurfaceContext {
    param(
        [string]$SurfaceId,
        [string]$MapPath,
        [string]$CompiledContractDebugPath = ""
    )

    if ([string]::IsNullOrWhiteSpace($MapPath) -and -not [string]::IsNullOrWhiteSpace($SurfaceId)) {
        $compiledDefinition = Get-StitchUnityCompiledContractDefinition -SurfaceId $SurfaceId
        if ($null -ne $compiledDefinition) {
            $compiled = Compile-StitchUnityContracts -Definition $compiledDefinition

            if (-not [string]::IsNullOrWhiteSpace($CompiledContractDebugPath)) {
                $debugPayload = [PSCustomObject]@{
                    manifest = $compiled.manifest
                    map = $compiled.map
                    contractSource = $compiled.contractSource
                }
                Write-StitchUnityArtifact -PathValue $CompiledContractDebugPath -InputObject $debugPayload
            }

            return [PSCustomObject]@{
                MapResult = [PSCustomObject]@{
                    Path = [string]$compiled.contractSource.compiledContractSummary.mapPath
                    Map = $compiled.map
                    SourceKind = "compiled-family"
                }
                Map = $compiled.map
                Contracts = $compiled.contractBundle
                ContractRefs = Get-StitchUnityContractRefObject -ContractBundle $compiled.contractBundle
                ContractSource = $compiled.contractSource
            }
        }
    }

    $mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath
    $contracts = Get-StitchUnityContractBundle -Map $mapResult.Map

    return [PSCustomObject]@{
        MapResult = $mapResult
        Map = $mapResult.Map
        Contracts = $contracts
        ContractRefs = Get-StitchUnityContractRefObject -ContractBundle $contracts
        ContractSource = [PSCustomObject]@{
            sourceKind = "file"
            generatedFromFamily = $false
            familyId = ""
            profileGenerated = $false
            profilePath = ""
            sharedUiCatalogPath = Get-StitchUnityComponentCatalogPath
            sourceRefs = $null
            compiledContractSummary = $null
        }
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

function Get-StitchUnityManifestBlocks {
    param([Parameter(Mandatory = $true)][object]$ContractBundle)

    return @(Get-StitchUnityOptionalArray -InputObject $ContractBundle.manifest -Name "blocks")
}

function Get-StitchUnityOrderedBlockEntries {
    param(
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][object]$ContractBundle
    )

    $mapEntriesById = @{}
    foreach ($entry in @(Get-StitchUnityBlockEntries -Map $Map)) {
        $mapEntriesById[[string]$entry.blockId] = $entry
    }

    $orderedEntries = @()
    $missingMapBlocks = @()
    $manifestBlocks = @(Get-StitchUnityManifestBlocks -ContractBundle $ContractBundle)

    for ($index = 0; $index -lt $manifestBlocks.Count; $index++) {
        $manifestBlock = $manifestBlocks[$index]
        $blockId = [string](Get-StitchUnityRequiredProperty -InputObject $manifestBlock -Name "blockId")

        if ($mapEntriesById.ContainsKey($blockId)) {
            $mapEntry = $mapEntriesById[$blockId]
            $orderedEntries += [PSCustomObject]@{
                blockId = $blockId
                mapping = $mapEntry.mapping
                manifestOrder = $index
            }
            $mapEntriesById.Remove($blockId)
            continue
        }

        $missingMapBlocks += $blockId
    }

    $extraMapBlocks = @($mapEntriesById.Keys | Sort-Object | ForEach-Object { [string]$_ })

    return [PSCustomObject]@{
        manifestBlockCount = $manifestBlocks.Count
        orderedEntries = @($orderedEntries)
        missingMapBlocks = @($missingMapBlocks)
        extraMapBlocks = @($extraMapBlocks)
    }
}

function Test-StitchUnityPresentationContractRequired {
    param([Parameter(Mandatory = $true)][object]$Map)

    return ([string]$Map.translationStrategy -eq "contract-complete-translator-v1")
}

function Get-StitchUnityPresentationContractState {
    param([Parameter(Mandatory = $true)][object]$ContractBundle)

    $presentation = $ContractBundle.presentation
    $extractionStatus = if ($null -ne $presentation) {
        [string](Get-StitchUnityOptionalPropertyValue -InputObject $presentation -Name "extractionStatus")
    }
    else {
        ""
    }

    return [PSCustomObject]@{
        exists = ($null -ne $presentation)
        extractionStatus = $extractionStatus
        unresolvedDerivedFields = if ($null -ne $presentation) {
            @(Get-StitchUnityOptionalArray -InputObject $presentation -Name "unresolvedDerivedFields")
        }
        else {
            @()
        }
    }
}

function Get-StitchUnityCandidatePaths {
    param([Parameter(Mandatory = $true)][object]$BlockMapping)

    $paths = New-Object System.Collections.Generic.List[string]

    $hostPath = [string](Get-StitchUnityRequiredProperty -InputObject $BlockMapping -Name "hostPath")
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

function Resolve-StitchUnityArtifactOutputPath {
    param(
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][string]$ArtifactName,
        [string]$ArtifactPath = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($ArtifactPath)) {
        return Resolve-StitchUnityRepoPath -PathValue $ArtifactPath
    }

    return Get-StitchUnityArtifactPath -Map $Map -Name $ArtifactName
}

function Test-StitchUnityTargetAssetExists {
    param([Parameter(Mandatory = $true)][object]$Target)

    $kind = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "kind")
    $assetPath = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "assetPath")
    $resolvedAssetPath = Resolve-StitchUnityRepoPath -PathValue $assetPath

    return [PSCustomObject]@{
        kind = $kind
        assetPath = $assetPath
        resolvedAssetPath = $resolvedAssetPath
        exists = (Test-Path -LiteralPath $resolvedAssetPath)
    }
}

function Get-StitchUnityStrategyMode {
    param([Parameter(Mandatory = $true)][object]$Map)

    return [string](Get-StitchUnityRequiredProperty -InputObject $Map -Name "strategyMode")
}

function Get-StitchUnityDependencies {
    param([Parameter(Mandatory = $true)][object]$Map)

    return @(Get-StitchUnityOptionalArray -InputObject $Map -Name "dependencies")
}

function Get-StitchUnityMcpRoot {
    param([string]$UnityBridgeUrl)

    return Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
}

function Get-StitchUnityReviewCaptureRouteConfig {
    param([Parameter(Mandatory = $true)][object]$Map)

    $surfaceId = [string](Get-StitchUnityRequiredProperty -InputObject $Map -Name "surfaceId")

    switch ($surfaceId) {
        "account-delete-confirm" {
            return [PSCustomObject]@{
                supported = $true
                surfaceId = $surfaceId
                menuPath = "Tools/Scene/Prepare Set C Overlay Runtime Review/Account Delete Confirm"
                routeKind = "temp-scene-sceneview"
            }
        }
        "common-error-dialog" {
            return [PSCustomObject]@{
                supported = $true
                surfaceId = $surfaceId
                menuPath = "Tools/Scene/Prepare Set C Overlay Runtime Review/Common Error Dialog"
                routeKind = "temp-scene-sceneview"
            }
        }
        default {
            return [PSCustomObject]@{
                supported = $false
                surfaceId = $surfaceId
                menuPath = ""
                routeKind = ""
                reason = "no-temp-scene-review-route"
            }
        }
    }
}

function Get-StitchUnityDefaultReviewCaptureArtifactPath {
    param([Parameter(Mandatory = $true)][object]$Map)

    $translationArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $Map -ArtifactName "translationResult"
    if (-not [string]::IsNullOrWhiteSpace($translationArtifactPath) -and $translationArtifactPath -match "-translation-result\.json$") {
        return ($translationArtifactPath -replace "-translation-result\.json$", "-scene-capture.png")
    }

    $surfaceId = [string](Get-StitchUnityRequiredProperty -InputObject $Map -Name "surfaceId")
    return Resolve-StitchUnityRepoPath -PathValue ("artifacts/unity/{0}-scene-capture.png" -f $surfaceId)
}

function Invoke-StitchUnityReviewCapture {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Map,
        [string]$ArtifactPath = ""
    )

    $route = Get-StitchUnityReviewCaptureRouteConfig -Map $Map
    if (-not [bool]$route.supported) {
        return [PSCustomObject]@{
            attempted = $false
            supported = $false
            status = "not-configured"
            surfaceId = [string]$route.surfaceId
            routeKind = ""
            menuPath = ""
            artifactPath = ""
            relativeArtifactPath = ""
            reason = [string]$route.reason
            consoleErrors = $null
            generatedAt = (Get-Date).ToString("o")
        }
    }

    $captureArtifactPath = if (-not [string]::IsNullOrWhiteSpace($ArtifactPath)) {
        Resolve-StitchUnityRepoPath -PathValue $ArtifactPath
    }
    else {
        Get-StitchUnityDefaultReviewCaptureArtifactPath -Map $Map
    }

    $relativeArtifactPath = Convert-StitchUnityRepoRelativePath -AbsolutePath $captureArtifactPath

    Wait-McpBridgeHealthy -Root $Root -TimeoutSec 30 | Out-Null
    $menuResult = Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/menu/execute" -Body @{
        menuPath = [string]$route.menuPath
    } -TimeoutSec 90

    $captureResult = Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/sceneview/capture" -Body @{
        outputPath = $relativeArtifactPath
        overwrite = $true
        superSize = 1
    } -TimeoutSec 90

    $consoleErrors = Get-McpRecentErrors -Root $Root -Limit 10
    $status = if ($null -ne $consoleErrors -and $null -ne $consoleErrors.PSObject.Properties["count"] -and [int]$consoleErrors.count -gt 0) {
        "warning"
    }
    else {
        "passed"
    }

    return [PSCustomObject]@{
        attempted = $true
        supported = $true
        status = $status
        surfaceId = [string]$route.surfaceId
        routeKind = [string]$route.routeKind
        menuPath = [string]$route.menuPath
        artifactPath = $captureArtifactPath
        relativeArtifactPath = $relativeArtifactPath
        reason = ""
        menu = $menuResult
        capture = $captureResult
        consoleErrors = $consoleErrors
        generatedAt = (Get-Date).ToString("o")
    }
}

function Get-StitchUnityPresentationElements {
    param([Parameter(Mandatory = $true)][object]$ContractBundle)

    if ($null -eq $ContractBundle.presentation) {
        return @()
    }

    $extractionStatus = [string](Get-StitchUnityOptionalPropertyValue -InputObject $ContractBundle.presentation -Name "extractionStatus")
    if (-not [string]::IsNullOrWhiteSpace($extractionStatus) -and $extractionStatus -ne "resolved") {
        return @()
    }

    return @(Get-StitchUnityOptionalArray -InputObject $ContractBundle.presentation -Name "elements")
}

function Get-StitchUnityTargetRootName {
    param([Parameter(Mandatory = $true)][object]$Target)

    $assetPath = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "assetPath")
    return [System.IO.Path]::GetFileNameWithoutExtension($assetPath)
}

function Get-StitchUnityPrefabNode {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [string]$ChildPath = ""
    )

    $body = @{
        assetPath = $AssetPath
    }

    if (-not [string]::IsNullOrWhiteSpace($ChildPath)) {
        $body.childPath = $ChildPath
    }

    try {
        return Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/get" -Body $body -TimeoutSec 60
    }
    catch {
        return [PSCustomObject]@{
            found = $false
            path = ""
            name = ""
            components = @()
        }
    }
}

function Test-StitchUnityScenePathExists {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $result = Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/gameobject/find" -Body @{
        path = $Path
    } -TimeoutSec 30

    return ($null -ne $result -and [bool]$result.found)
}

function Remove-StitchUnityScenePathIfExists {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (-not (Test-StitchUnityScenePathExists -Root $Root -Path $Path)) {
        return
    }

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/gameobject/destroy" -Body @{
        path = $Path
        autoSave = $false
    } -TimeoutSec 60 | Out-Null
}

function Get-StitchUnityRequiredComponentTypeName {
    param([Parameter(Mandatory = $true)][string]$ComponentName)

    switch ($ComponentName) {
        "Image" { return "UnityEngine.UI.Image" }
        "Button" { return "UnityEngine.UI.Button" }
        "HorizontalLayoutGroup" { return "UnityEngine.UI.HorizontalLayoutGroup" }
        "VerticalLayoutGroup" { return "UnityEngine.UI.VerticalLayoutGroup" }
        "LayoutElement" { return "UnityEngine.UI.LayoutElement" }
        "CanvasGroup" { return "UnityEngine.CanvasGroup" }
        "ContentSizeFitter" { return "UnityEngine.UI.ContentSizeFitter" }
        "TextMeshProUGUI" { return "TMPro.TextMeshProUGUI" }
        "TMP_Text" { return "TMPro.TextMeshProUGUI" }
        default { return $ComponentName }
    }
}

function New-StitchUnityPrefabTargetIfMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Target
    )

    $targetState = Test-StitchUnityTargetAssetExists -Target $Target
    if ($targetState.exists) {
        return [PSCustomObject]@{
            created = $false
            assetPath = $targetState.assetPath
            scenePath = ""
            rootName = Get-StitchUnityTargetRootName -Target $Target
        }
    }

    if (-not (Test-StitchUnityScenePathExists -Root $Root -Path "/Canvas")) {
        throw "Scene path '/Canvas' is required before a prefab target can be generated."
    }

    $rootName = Get-StitchUnityTargetRootName -Target $Target
    $scenePath = "/Canvas/$rootName"
    Remove-StitchUnityScenePathIfExists -Root $Root -Path $scenePath

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/ui/create-panel" -Body @{
        name = $rootName
        parent = "/Canvas"
        autoSave = $false
    } -TimeoutSec 60 | Out-Null

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/save" -Body @{
        gameObjectPath = $scenePath
        savePath = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "assetPath")
        destroySceneObject = $true
        connectSceneObject = $false
    } -TimeoutSec 60 | Out-Null

    return [PSCustomObject]@{
        created = $true
        assetPath = [string](Get-StitchUnityRequiredProperty -InputObject $Target -Name "assetPath")
        scenePath = $scenePath
        rootName = $rootName
    }
}

function Get-StitchUnityPrefabComponentTypeNames {
    param([Parameter(Mandatory = $true)][object]$PrefabNode)

    if ($null -eq $PrefabNode -or -not $PrefabNode.found) {
        return @()
    }

    return @(
        foreach ($component in @($PrefabNode.components)) {
            if ($null -eq $component) {
                continue
            }

            if (-not [string]::IsNullOrWhiteSpace([string]$component.typeName)) {
                [string]$component.typeName
            }
        }
    )
}

function Add-StitchUnityPrefabComponentIfMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [AllowEmptyString()][string]$ChildPath = "",
        [Parameter(Mandatory = $true)][string]$ComponentName,
        [Parameter(Mandatory = $true)][string[]]$ExistingComponentTypeNames
    )

    if ($ExistingComponentTypeNames -contains $ComponentName) {
        return $false
    }

    $componentType = Get-StitchUnityRequiredComponentTypeName -ComponentName $ComponentName
    $body = @{
        assetPath = $AssetPath
        componentType = $componentType
    }
    if (-not [string]::IsNullOrWhiteSpace($ChildPath)) {
        $body.childPath = $ChildPath
    }

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/add-component" -Body $body -TimeoutSec 60 | Out-Null

    return $true
}

function Ensure-StitchUnityPrefabHostPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$HostPath,
        [string[]]$RequiredComponents = @()
    )

    $createdPaths = New-Object System.Collections.Generic.List[string]
    $addedComponents = New-Object System.Collections.Generic.List[string]

    $segments = @($HostPath -split "/" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($segments.Count -eq 0) {
        throw "HostPath is empty."
    }

    $currentPath = ""
    for ($i = 0; $i -lt $segments.Count; $i++) {
        $segment = [string]$segments[$i]
        $currentPath = if ([string]::IsNullOrWhiteSpace($currentPath)) { $segment } else { "$currentPath/$segment" }
        $parentPath = if ($i -eq 0) { "" } else { ($segments[0..($i - 1)] -join "/") }
        $node = Get-StitchUnityPrefabNode -Root $Root -AssetPath $AssetPath -ChildPath $currentPath

        if (-not $node.found) {
            $components = @("RectTransform")
            if ($i -eq ($segments.Count - 1)) {
                $components += @($RequiredComponents)
            }

            Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/create" -Body @{
                assetPath = $AssetPath
                parentPath = $parentPath
                name = $segment
                components = $components
            } -TimeoutSec 60 | Out-Null

            $createdPaths.Add($currentPath)
            $node = Get-StitchUnityPrefabNode -Root $Root -AssetPath $AssetPath -ChildPath $currentPath
        }

        if ($i -eq ($segments.Count - 1)) {
            $existingComponentTypeNames = @(Get-StitchUnityPrefabComponentTypeNames -PrefabNode $node)
            foreach ($requiredComponent in @($RequiredComponents)) {
                $requiredComponentName = [string]$requiredComponent
                if ([string]::IsNullOrWhiteSpace($requiredComponentName)) {
                    continue
                }

                $added = Add-StitchUnityPrefabComponentIfMissing `
                    -Root $Root `
                    -AssetPath $AssetPath `
                    -ChildPath $currentPath `
                    -ComponentName $requiredComponentName `
                    -ExistingComponentTypeNames $existingComponentTypeNames
                if ($added) {
                    $addedComponents.Add("$currentPath::$requiredComponentName")
                    $existingComponentTypeNames += $requiredComponentName
                }
            }
        }
    }

    return [PSCustomObject]@{
        hostPath = $HostPath
        createdPaths = @($createdPaths)
        addedComponents = @($addedComponents)
        finalNode = Get-StitchUnityPrefabNode -Root $Root -AssetPath $AssetPath -ChildPath $HostPath
    }
}

function Ensure-StitchUnityPrefabNodeComponents {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [AllowEmptyString()][string]$ChildPath = "",
        [string[]]$RequiredComponents = @()
    )

    $node = Get-StitchUnityPrefabNode -Root $Root -AssetPath $AssetPath -ChildPath $ChildPath
    if (-not $node.found) {
        throw "Prefab node not found for component ensure. childPath='$ChildPath'"
    }

    $existingComponentTypeNames = @(Get-StitchUnityPrefabComponentTypeNames -PrefabNode $node)
    $addedComponents = New-Object System.Collections.Generic.List[string]
    foreach ($requiredComponent in @($RequiredComponents)) {
        $requiredComponentName = [string]$requiredComponent
        if ([string]::IsNullOrWhiteSpace($requiredComponentName)) {
            continue
        }

        $added = Add-StitchUnityPrefabComponentIfMissing `
            -Root $Root `
            -AssetPath $AssetPath `
            -ChildPath $ChildPath `
            -ComponentName $requiredComponentName `
            -ExistingComponentTypeNames $existingComponentTypeNames
        if ($added) {
            $addedComponentPath = $requiredComponentName
            if (-not [string]::IsNullOrWhiteSpace($ChildPath)) {
                $addedComponentPath = "$ChildPath::$requiredComponentName"
            }

            $addedComponents.Add($addedComponentPath)
            $existingComponentTypeNames += $requiredComponentName
        }
    }

    return [PSCustomObject]@{
        childPath = $ChildPath
        addedComponents = @($addedComponents)
        finalNode = Get-StitchUnityPrefabNode -Root $Root -AssetPath $AssetPath -ChildPath $ChildPath
    }
}

function Set-StitchUnityPrefabProperty {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [string]$ChildPath = "",
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [string]$Value = "",
        [string]$AssetReferencePath = "",
        [string]$AutoWireType = ""
    )

    $body = @{
        assetPath = $AssetPath
        componentType = $ComponentType
        propertyName = $PropertyName
    }

    if (-not [string]::IsNullOrWhiteSpace($ChildPath)) {
        $body.childPath = $ChildPath
    }

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $body.value = $Value
    }

    if (-not [string]::IsNullOrWhiteSpace($AssetReferencePath)) {
        $body.assetReferencePath = $AssetReferencePath
    }

    if (-not [string]::IsNullOrWhiteSpace($AutoWireType)) {
        $body.autoWireType = $AutoWireType
    }

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/set" -Body $body -TimeoutSec 60 | Out-Null
}

function Set-StitchUnityPrefabRectFromContract {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [string]$ChildPath = "",
        [Parameter(Mandatory = $true)][object]$RectContract
    )

    $rectProperties = [ordered]@{
        anchorMin = "m_AnchorMin"
        anchorMax = "m_AnchorMax"
        pivot = "m_Pivot"
        anchoredPosition = "m_AnchoredPosition"
        sizeDelta = "m_SizeDelta"
    }

    foreach ($entry in $rectProperties.GetEnumerator()) {
        $value = [string](Get-StitchUnityOptionalPropertyValue -InputObject $RectContract -Name $entry.Key)
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        Set-StitchUnityPrefabProperty `
            -Root $Root `
            -AssetPath $AssetPath `
            -ChildPath $ChildPath `
            -ComponentType "RectTransform" `
            -PropertyName $entry.Value `
            -Value $value
    }
}

function Invoke-StitchUnityPresentationContract {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][object]$ContractBundle
    )

    $elements = @(Get-StitchUnityPresentationElements -ContractBundle $ContractBundle)
    $presentation = $ContractBundle.presentation
    $extractionStatus = if ($null -ne $presentation) { [string](Get-StitchUnityOptionalPropertyValue -InputObject $presentation -Name "extractionStatus") } else { "" }
    if (@($elements).Count -eq 0) {
        $emptyReason = "no-elements"
        if ($null -eq $presentation) {
            $emptyReason = "no-presentation-contract"
        }
        elseif ($extractionStatus -ne "resolved") {
            $emptyReason = "presentation-contract-not-source-derived-yet"
        }

        return [PSCustomObject]@{
            applied = $false
            extractionStatus = $extractionStatus
            elementCount = 0
            createdPaths = @()
            addedComponents = @()
            appliedProperties = @()
            elements = @()
            reason = $emptyReason
        }
    }

    $target = Get-StitchUnityRequiredProperty -InputObject $Map -Name "target"
    $assetPath = [string](Get-StitchUnityRequiredProperty -InputObject $target -Name "assetPath")
    $createdPaths = New-Object System.Collections.Generic.List[string]
    $addedComponents = New-Object System.Collections.Generic.List[string]
    $appliedProperties = New-Object System.Collections.Generic.List[string]
    $elementResults = @()

    foreach ($element in @($elements)) {
        $path = [string](Get-StitchUnityRequiredProperty -InputObject $element -Name "path")
        $components = @(Get-StitchUnityOptionalArray -InputObject $element -Name "components")
        $elementCreatedPaths = @()
        $elementAddedComponents = @()

        if ([string]::IsNullOrWhiteSpace($path)) {
            $componentResult = Ensure-StitchUnityPrefabNodeComponents -Root $Root -AssetPath $assetPath -ChildPath "" -RequiredComponents $components
            $elementAddedComponents = @($componentResult.addedComponents)
        }
        else {
            $pathResult = Ensure-StitchUnityPrefabHostPath -Root $Root -AssetPath $assetPath -HostPath $path -RequiredComponents $components
            $elementCreatedPaths = @($pathResult.createdPaths)
            $elementAddedComponents = @($pathResult.addedComponents)
        }

        foreach ($createdPath in @($elementCreatedPaths)) {
            if (-not $createdPaths.Contains([string]$createdPath)) {
                $createdPaths.Add([string]$createdPath)
            }
        }

        foreach ($addedComponent in @($elementAddedComponents)) {
            if (-not $addedComponents.Contains([string]$addedComponent)) {
                $addedComponents.Add([string]$addedComponent)
            }
        }

        $rectContract = Get-StitchUnityOptionalPropertyValue -InputObject $element -Name "rect"
        if ($null -ne $rectContract) {
            Set-StitchUnityPrefabRectFromContract -Root $Root -AssetPath $assetPath -ChildPath $path -RectContract $rectContract
        }

        $siblingIndexValue = Get-StitchUnityOptionalPropertyValue -InputObject $element -Name "siblingIndex"
        if ($null -ne $siblingIndexValue -and -not [string]::IsNullOrWhiteSpace($path)) {
            Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/prefab/set-sibling" -Body @{
                assetPath = $assetPath
                childPath = $path
                siblingIndex = [int]$siblingIndexValue
            } -TimeoutSec 60 | Out-Null
        }

        foreach ($property in @(Get-StitchUnityOptionalArray -InputObject $element -Name "properties")) {
            $componentType = [string](Get-StitchUnityRequiredProperty -InputObject $property -Name "componentType")
            $propertyName = [string](Get-StitchUnityRequiredProperty -InputObject $property -Name "propertyName")
            $value = [string](Get-StitchUnityOptionalPropertyValue -InputObject $property -Name "value")
            $assetReferencePath = [string](Get-StitchUnityOptionalPropertyValue -InputObject $property -Name "assetReferencePath")
            $autoWireType = [string](Get-StitchUnityOptionalPropertyValue -InputObject $property -Name "autoWireType")

            if ([string]::IsNullOrWhiteSpace($path)) {
                $propertyEnsureResult = Ensure-StitchUnityPrefabNodeComponents `
                    -Root $Root `
                    -AssetPath $assetPath `
                    -RequiredComponents @($componentType)
            }
            else {
                $propertyEnsureResult = Ensure-StitchUnityPrefabNodeComponents `
                    -Root $Root `
                    -AssetPath $assetPath `
                    -ChildPath $path `
                    -RequiredComponents @($componentType)
            }
            foreach ($addedComponent in @($propertyEnsureResult.addedComponents)) {
                if (-not $addedComponents.Contains([string]$addedComponent)) {
                    $addedComponents.Add([string]$addedComponent)
                }
            }

            Set-StitchUnityPrefabProperty `
                -Root $Root `
                -AssetPath $assetPath `
                -ChildPath $path `
                -ComponentType $componentType `
                -PropertyName $propertyName `
                -Value $value `
                -AssetReferencePath $assetReferencePath `
                -AutoWireType $autoWireType
            $appliedPropertyPath = "$componentType.$propertyName"
            if (-not [string]::IsNullOrWhiteSpace($path)) {
                $appliedPropertyPath = "$path::$componentType.$propertyName"
            }

            $appliedProperties.Add($appliedPropertyPath)
        }

        $finalNode = Get-StitchUnityPrefabNode -Root $Root -AssetPath $assetPath -ChildPath $path
        $elementResults += [PSCustomObject]@{
            path = $path
            createdPaths = @($elementCreatedPaths)
            addedComponents = @($elementAddedComponents)
            propertyCount = @(Get-StitchUnityOptionalArray -InputObject $element -Name "properties").Count
            finalNodeFound = [bool]$finalNode.found
            finalComponents = @(Get-StitchUnityPrefabComponentTypeNames -PrefabNode $finalNode)
        }
    }

    return [PSCustomObject]@{
        applied = $true
        extractionStatus = $extractionStatus
        elementCount = @($elements).Count
        createdPaths = @($createdPaths)
        addedComponents = @($addedComponents)
        appliedProperties = @($appliedProperties)
        elements = @($elementResults)
        reason = "applied"
    }
}

function Invoke-StitchUnityContractCompleteTranslation {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][object]$ContractBundle
    )

    $target = Get-StitchUnityRequiredProperty -InputObject $Map -Name "target"
    $assetPath = [string](Get-StitchUnityRequiredProperty -InputObject $target -Name "assetPath")
    $targetCreation = New-StitchUnityPrefabTargetIfMissing -Root $Root -Target $target
    $orderedBlockEntries = Get-StitchUnityOrderedBlockEntries -Map $Map -ContractBundle $ContractBundle

    if (@($orderedBlockEntries.missingMapBlocks).Count -gt 0 -or @($orderedBlockEntries.extraMapBlocks).Count -gt 0) {
        throw "Manifest/map block alignment failed. missingMapBlocks='$([string]::Join(', ', @($orderedBlockEntries.missingMapBlocks)))' extraMapBlocks='$([string]::Join(', ', @($orderedBlockEntries.extraMapBlocks)))'."
    }

    $blockResults = @()
    $createdPaths = New-Object System.Collections.Generic.List[string]
    $addedComponents = New-Object System.Collections.Generic.List[string]

    foreach ($entry in @($orderedBlockEntries.orderedEntries)) {
        $blockId = [string]$entry.blockId
        $mapping = $entry.mapping
        $hostPath = [string](Get-StitchUnityRequiredProperty -InputObject $mapping -Name "hostPath")
        $requiredComponents = @(Get-StitchUnityOptionalArray -InputObject $mapping -Name "requiredComponents")
        $pathResult = Ensure-StitchUnityPrefabHostPath -Root $Root -AssetPath $assetPath -HostPath $hostPath -RequiredComponents $requiredComponents

        foreach ($createdPath in @($pathResult.createdPaths)) {
            if (-not $createdPaths.Contains([string]$createdPath)) {
                $createdPaths.Add([string]$createdPath)
            }
        }

        foreach ($addedComponent in @($pathResult.addedComponents)) {
            if (-not $addedComponents.Contains([string]$addedComponent)) {
                $addedComponents.Add([string]$addedComponent)
            }
        }

        $blockResults += [PSCustomObject]@{
            blockId = $blockId
            manifestOrder = [int]$entry.manifestOrder
            hostPath = $hostPath
            createdPaths = @($pathResult.createdPaths)
            addedComponents = @($pathResult.addedComponents)
            finalNodeFound = [bool]$pathResult.finalNode.found
            finalComponents = @(Get-StitchUnityPrefabComponentTypeNames -PrefabNode $pathResult.finalNode)
        }
    }

    $presentationResult = Invoke-StitchUnityPresentationContract -Root $Root -Map $Map -ContractBundle $ContractBundle
    if ((Test-StitchUnityPresentationContractRequired -Map $Map) -and -not [bool]$presentationResult.applied) {
        throw "Presentation contract gate failed. reason='$([string]$presentationResult.reason)' extractionStatus='$([string]$presentationResult.extractionStatus)'."
    }

    foreach ($createdPath in @($presentationResult.createdPaths)) {
        if (-not $createdPaths.Contains([string]$createdPath)) {
            $createdPaths.Add([string]$createdPath)
        }
    }

    foreach ($addedComponent in @($presentationResult.addedComponents)) {
        if (-not $addedComponents.Contains([string]$addedComponent)) {
            $addedComponents.Add([string]$addedComponent)
        }
    }

    return [PSCustomObject]@{
        schemaVersion = "1.0.0"
        success = $true
        surfaceId = [string]$Map.surfaceId
        translationStrategy = [string]$Map.translationStrategy
        target = $target
        targetCreated = [bool]$targetCreation.created
        targetRootName = [string]$targetCreation.rootName
        contractRefs = Get-StitchUnityContractRefObject -ContractBundle $ContractBundle
        createdPaths = @($createdPaths)
        addedComponents = @($addedComponents)
        blocks = $blockResults
        presentation = $presentationResult
        generatedAt = (Get-Date).ToString("o")
    }
}

function Invoke-StitchUnityDependencies {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Map
    )

    $results = @()
    foreach ($dependency in @(Get-StitchUnityDependencies -Map $Map)) {
        $kind = [string](Get-StitchUnityRequiredProperty -InputObject $dependency -Name "kind")
        $id = [string](Get-StitchUnityRequiredProperty -InputObject $dependency -Name "id")
        $required = [bool](Get-StitchUnityRequiredProperty -InputObject $dependency -Name "required")

        switch ($kind) {
            "menu" {
                $menuPath = [string](Get-StitchUnityRequiredProperty -InputObject $dependency -Name "menuPath")
                try {
                    $response = Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/menu/execute" -Body @{
                        menuPath = $menuPath
                    } -TimeoutSec 90

                    $results += [PSCustomObject]@{
                        id = $id
                        kind = $kind
                        required = $required
                        ok = [bool]$response.success
                        menuPath = $menuPath
                        message = [string]$response.message
                    }
                }
                catch {
                    if ($required) {
                        throw
                    }

                    $results += [PSCustomObject]@{
                        id = $id
                        kind = $kind
                        required = $required
                        ok = $false
                        menuPath = $menuPath
                        message = $_.Exception.Message
                    }
                }
            }
            default {
                throw "Unsupported dependency kind '$kind'."
            }
        }
    }

    return $results
}

function Get-StitchUnityPreflightObject {
    param(
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][object]$ContractBundle,
        [object]$ContractSource = $null
    )

    $targetState = Test-StitchUnityTargetAssetExists -Target $Map.target
    $strategyMode = Get-StitchUnityStrategyMode -Map $Map
    $dependencies = @(Get-StitchUnityDependencies -Map $Map)
    $orderedBlockEntries = Get-StitchUnityOrderedBlockEntries -Map $Map -ContractBundle $ContractBundle
    $presentationState = Get-StitchUnityPresentationContractState -ContractBundle $ContractBundle
    $requiresPresentationContract = Test-StitchUnityPresentationContractRequired -Map $Map
    $roughEdges = @()
    $blockingIssues = @()

    if (-not $targetState.exists -and $strategyMode -eq "patch") {
        $issue = [PSCustomObject]@{
            code = "missing-target-for-patch-mode"
            message = "Target asset is missing but strategyMode is patch. Surface generation would fail without an existing asset."
        }
        $roughEdges += $issue
        $blockingIssues += $issue
    }

    if (-not $targetState.exists -and $strategyMode -eq "generate-or-patch") {
        $roughEdges += [PSCustomObject]@{
            code = "missing-target-generate-path"
            message = "Target asset is missing. The generator path must create the asset from contract instead of patching."
        }
    }

    if ($dependencies.Count -gt 0) {
        foreach ($dependency in $dependencies) {
            $roughEdges += [PSCustomObject]@{
                code = "dependency-declared"
                message = "Surface declares dependency '$([string]$dependency.id)' of kind '$([string]$dependency.kind)'."
            }
        }
    }

    if ($orderedBlockEntries.manifestBlockCount -eq 0) {
        $issue = [PSCustomObject]@{
            code = "manifest-has-no-blocks"
            message = "Screen manifest must declare at least one semantic block before translation."
        }
        $roughEdges += $issue
        $blockingIssues += $issue
    }

    if (@($orderedBlockEntries.missingMapBlocks).Count -gt 0) {
        $issue = [PSCustomObject]@{
            code = "manifest-blocks-missing-from-map"
            message = "unity-map is missing host bindings for manifest blocks: $([string]::Join(', ', @($orderedBlockEntries.missingMapBlocks)))."
            blockIds = @($orderedBlockEntries.missingMapBlocks)
        }
        $roughEdges += $issue
        $blockingIssues += $issue
    }

    if (@($orderedBlockEntries.extraMapBlocks).Count -gt 0) {
        $issue = [PSCustomObject]@{
            code = "map-blocks-not-declared-in-manifest"
            message = "unity-map declares blocks that are not present in manifest.blocks[]: $([string]::Join(', ', @($orderedBlockEntries.extraMapBlocks)))."
            blockIds = @($orderedBlockEntries.extraMapBlocks)
        }
        $roughEdges += $issue
        $blockingIssues += $issue
    }

    if ($requiresPresentationContract -and -not $presentationState.exists) {
        $issue = [PSCustomObject]@{
            code = "missing-presentation-contract"
            message = "translationStrategy '$([string]$Map.translationStrategy)' requires contractRefs.presentationPath and a source-derived presentation contract."
        }
        $roughEdges += $issue
        $blockingIssues += $issue
    }

    if ($requiresPresentationContract -and $presentationState.exists -and $presentationState.extractionStatus -ne "resolved") {
        $issue = [PSCustomObject]@{
            code = "presentation-contract-not-resolved"
            message = "Presentation contract must have extractionStatus='resolved' before translation. Current status is '$([string]$presentationState.extractionStatus)'."
        }
        $roughEdges += $issue
        $blockingIssues += $issue
    }

    if (@($presentationState.unresolvedDerivedFields).Count -gt 0) {
        $roughEdges += [PSCustomObject]@{
            code = "presentation-contract-has-unresolved-derived-fields"
            message = "Presentation contract still tracks unresolved derived fields: $([string]::Join(', ', @($presentationState.unresolvedDerivedFields)))."
            fieldIds = @($presentationState.unresolvedDerivedFields)
        }
    }

    return [PSCustomObject]@{
        schemaVersion = "1.0.0"
        surfaceId = [string]$Map.surfaceId
        translationStrategy = [string]$Map.translationStrategy
        strategyMode = $strategyMode
        target = $Map.target
        targetExists = $targetState.exists
        targetState = $targetState
        dependencies = $dependencies
        contractRefs = Get-StitchUnityContractRefObject -ContractBundle $ContractBundle
        contractSource = if ($null -ne $ContractSource) { $ContractSource } else {
            [PSCustomObject]@{
                sourceKind = "file"
                generatedFromFamily = $false
                familyId = ""
                profileGenerated = $false
                profilePath = ""
                sharedUiCatalogPath = ""
                sourceRefs = $null
                compiledContractSummary = $null
            }
        }
        contract = [PSCustomObject]@{
            manifestBlockCount = [int]$orderedBlockEntries.manifestBlockCount
            orderedBlockIds = @($orderedBlockEntries.orderedEntries | ForEach-Object { [string]$_.blockId })
            missingMapBlocks = @($orderedBlockEntries.missingMapBlocks)
            extraMapBlocks = @($orderedBlockEntries.extraMapBlocks)
            presentationRequired = [bool]$requiresPresentationContract
            presentationExists = [bool]$presentationState.exists
            presentationExtractionStatus = [string]$presentationState.extractionStatus
            unresolvedDerivedFields = @($presentationState.unresolvedDerivedFields)
        }
        blockingIssues = @($blockingIssues)
        ready = (($strategyMode -ne "patch" -or $targetState.exists) -and @($blockingIssues).Count -eq 0)
        roughEdges = $roughEdges
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
