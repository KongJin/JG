Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-StitchUnityRepoRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
}

$script:StitchUnityRepoRoot = Get-StitchUnityRepoRoot

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

function Write-StitchUnityJsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $resolvedPath = Resolve-StitchUnityRepoPath -PathValue $PathValue
    $directoryPath = Split-Path -Parent $resolvedPath
    if (-not [string]::IsNullOrWhiteSpace($directoryPath) -and -not (Test-Path -LiteralPath $directoryPath)) {
        New-Item -ItemType Directory -Path $directoryPath -Force | Out-Null
    }

    $json = $InputObject | ConvertTo-Json -Depth 30
    [System.IO.File]::WriteAllText($resolvedPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    return $resolvedPath
}

function Test-StitchUnityInMemoryPath {
    param([string]$PathValue)

    return (-not [string]::IsNullOrWhiteSpace($PathValue)) -and $PathValue.StartsWith("in-memory://", [System.StringComparison]::OrdinalIgnoreCase)
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

function New-StitchUnityLegacyTranslatorDisabledResult {
    param(
        [string]$SurfaceId = "",
        [string]$PathValue = ""
    )

    return [PSCustomObject][ordered]@{
        success = $false
        supported = $false
        terminalVerdict = "blocked"
        blockedReason = "legacy-ugui-translator-disabled"
        surfaceId = $SurfaceId
        path = $PathValue
        acceptedRoute = "UITK candidate surface"
        message = "The old Stitch-to-Unity contract translator emitted UGUI/TMP contracts and is disabled. Use source facts plus a UI Toolkit UXML/USS candidate workflow."
    }
}

function Assert-StitchUnityLegacyTranslatorDisabled {
    param([string]$SurfaceId = "")

    $suffix = if ([string]::IsNullOrWhiteSpace($SurfaceId)) { "" } else { " SurfaceId='$SurfaceId'." }
    throw "Legacy Stitch-to-Unity UGUI/TMP translation is disabled.$suffix Use Collect-StitchSourceFacts and build a UI Toolkit UXML/USS candidate instead."
}

function Get-StitchUnityMapPath {
    param(
        [string]$SurfaceId,
        [string]$MapPath
    )

    if (-not [string]::IsNullOrWhiteSpace($MapPath)) {
        return Resolve-StitchUnityRepoPath -PathValue $MapPath
    }

    Assert-StitchUnityLegacyTranslatorDisabled -SurfaceId $SurfaceId
}

function Get-StitchUnityComponentCatalogPath {
    Assert-StitchUnityLegacyTranslatorDisabled
}

function Get-StitchUnityProfileGeneratorScriptPath {
    return Resolve-StitchUnityRepoPath -PathValue "tools/stitch-unity/presentations/Generate-StitchPresentationProfile.ps1"
}

function Get-StitchUnityPresentationResolverScriptPath {
    return Resolve-StitchUnityRepoPath -PathValue "tools/stitch-unity/presentations/Resolve-StitchPresentationContract.ps1"
}

function Get-StitchUnityContractDraftValidatorScriptPath {
    return Resolve-StitchUnityRepoPath -PathValue "tools/stitch-unity/validators/Test-StitchContractDraft.ps1"
}

function Get-StitchUnityProfileGeneratorProbe {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    return New-StitchUnityLegacyTranslatorDisabledResult -SurfaceId $SurfaceId
}

function Get-StitchUnityGeneratedProfile {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    return $null
}

function Ensure-StitchUnityPresentationContract {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    Assert-StitchUnityLegacyTranslatorDisabled -SurfaceId $SurfaceId
}

function Get-StitchUnityCompiledContractDefinition {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    return $null
}

function Invoke-StitchUnityContractDraftValidation {
    param(
        [Parameter(Mandatory = $true)][string]$DraftPath,
        [string]$SurfaceId = "",
        [string]$TargetAssetPath = ""
    )

    Assert-StitchUnityLegacyTranslatorDisabled -SurfaceId $SurfaceId
}

function Get-StitchUnityDraftSurfaceContext {
    param(
        [Parameter(Mandatory = $true)][string]$DraftPath,
        [string]$SurfaceId = "",
        [string]$TargetAssetPath = ""
    )

    Assert-StitchUnityLegacyTranslatorDisabled -SurfaceId $SurfaceId
}

function Get-StitchUnitySurfaceContext {
    param(
        [string]$SurfaceId,
        [string]$MapPath,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = "",
        [string]$DraftPath = ""
    )

    Assert-StitchUnityLegacyTranslatorDisabled -SurfaceId $SurfaceId
}

function Get-StitchUnityPreflightObject {
    param(
        [Parameter(Mandatory = $true)][object]$Map,
        [Parameter(Mandatory = $true)][object]$ContractBundle,
        [object]$ContractSource = $null
    )

    Assert-StitchUnityLegacyTranslatorDisabled -SurfaceId ([string](Get-StitchUnityOptionalPropertyValue -InputObject $Map -Name "surfaceId"))
}
