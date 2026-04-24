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

    return [PSCustomObject]@{
        manifestPath = $manifestPath
        manifest = Read-StitchUnityJsonFile -PathValue $manifestPath
    }
}

function Get-StitchUnityContractRefObject {
    param([Parameter(Mandatory = $true)][object]$ContractBundle)

    $refs = [ordered]@{
        manifestPath = $ContractBundle.manifestPath
    }

    return [PSCustomObject]$refs
}

function Get-StitchUnitySurfaceContext {
    param(
        [string]$SurfaceId,
        [string]$MapPath
    )

    $mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath
    $contracts = Get-StitchUnityContractBundle -Map $mapResult.Map

    return [PSCustomObject]@{
        MapResult = $mapResult
        Map = $mapResult.Map
        Contracts = $contracts
        ContractRefs = Get-StitchUnityContractRefObject -ContractBundle $contracts
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
        [Parameter(Mandatory = $true)][object]$ContractBundle
    )

    $targetState = Test-StitchUnityTargetAssetExists -Target $Map.target
    $strategyMode = Get-StitchUnityStrategyMode -Map $Map
    $dependencies = @(Get-StitchUnityDependencies -Map $Map)
    $roughEdges = @()

    if (-not $targetState.exists -and $strategyMode -eq "patch") {
        $roughEdges += [PSCustomObject]@{
            code = "missing-target-for-patch-mode"
            message = "Target asset is missing but strategyMode is patch. Surface generation would fail without an existing asset."
        }
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
        ready = ($strategyMode -ne "patch" -or $targetState.exists)
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
