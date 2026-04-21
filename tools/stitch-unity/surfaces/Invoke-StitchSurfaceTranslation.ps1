param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\engine\StitchUnityCommon.ps1"

$mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath
$map = $mapResult.Map
$contracts = Get-StitchUnityContractBundle -Map $map

$translationArtifactPath = Get-StitchUnityArtifactPath -Map $map -Name "translationResult"
$inspectionArtifactPath = Get-StitchUnityArtifactPath -Map $map -Name "inspectionResult"
$verificationArtifactPath = Get-StitchUnityArtifactPath -Map $map -Name "verificationResult"
$pipelineArtifactPath = $ArtifactPath
if ([string]::IsNullOrWhiteSpace($pipelineArtifactPath)) {
    $pipelineArtifactPath = Get-StitchUnityArtifactPath -Map $map -Name "pipelineResult"
}

$translationStrategy = [string]$map.translationStrategy
$translationResult = $null

switch ($translationStrategy) {
    "unity-mcp-garage-manifest-v1" {
        $translatorPath = Resolve-StitchUnityRepoPath -PathValue "tools/unity-mcp/Invoke-StitchGarageManifestTranslation.ps1"
        $translationRaw = & $translatorPath `
            -ScreenManifestPath $contracts.manifestPath `
            -ScreenIntakePath $contracts.intakePath `
            -UnityBridgeUrl $UnityBridgeUrl `
            -ArtifactPath $translationArtifactPath

        $translationResult = if ($translationRaw -is [string]) {
            $translationRaw | ConvertFrom-Json
        }
        else {
            $translationRaw
        }
    }
    "unity-mcp-overlay-manifest-v1" {
        $translatorPath = Resolve-StitchUnityRepoPath -PathValue "tools/unity-mcp/Invoke-StitchOverlayManifestTranslation.ps1"
        $translationRaw = & $translatorPath `
            -ScreenManifestPath $contracts.manifestPath `
            -UnityBridgeUrl $UnityBridgeUrl `
            -ArtifactPath $translationArtifactPath

        $translationResult = if ($translationRaw -is [string]) {
            $translationRaw | ConvertFrom-Json
        }
        else {
            $translationRaw
        }
    }
    default {
        throw "Unsupported translationStrategy '$translationStrategy'."
    }
}

$root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null

$inspection = Get-StitchUnitySurfaceInspectionObject -Root $root -Map $map -ContractBundle $contracts
$inspection.mapPath = $mapResult.Path
if (-not [string]::IsNullOrWhiteSpace($inspectionArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $inspectionArtifactPath -InputObject $inspection
}

$verification = Get-StitchUnitySurfaceVerificationObject -Map $map -Inspection $inspection
if (-not [string]::IsNullOrWhiteSpace($verificationArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $verificationArtifactPath -InputObject $verification
}

$result = [PSCustomObject]@{
    success = $true
    surfaceId = [string]$map.surfaceId
    mapPath = $mapResult.Path
    translationStrategy = $translationStrategy
    target = $map.target
    contractRefs = [PSCustomObject]@{
        manifestPath = $contracts.manifestPath
        intakePath = $contracts.intakePath
        blueprintPath = $contracts.blueprintPath
    }
    artifacts = [PSCustomObject]@{
        translationResult = $translationArtifactPath
        inspectionResult = $inspectionArtifactPath
        verificationResult = $verificationArtifactPath
        pipelineResult = $pipelineArtifactPath
    }
    translation = $translationResult
    inspectionSummary = [PSCustomObject]@{
        missingBlocks = @($inspection.missingBlocks)
        blockCount = @($inspection.blocks).Count
    }
    verificationSummary = [PSCustomObject]@{
        ok = $verification.ok
        errorCount = @($verification.errors).Count
        warningCount = @($verification.warnings).Count
    }
}

if (-not [string]::IsNullOrWhiteSpace($pipelineArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $pipelineArtifactPath -InputObject $result
}

$result | ConvertTo-Json -Depth 20
