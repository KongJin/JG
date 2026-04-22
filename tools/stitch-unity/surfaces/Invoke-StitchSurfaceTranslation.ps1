param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\engine\StitchUnityCommon.ps1"

$context = Get-StitchUnitySurfaceContext -SurfaceId $SurfaceId -MapPath $MapPath
$mapResult = $context.MapResult
$map = $context.Map
$contracts = $context.Contracts

$translationArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "translationResult"
$preflightArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "preflightResult"
$inspectionArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "inspectionResult"
$verificationArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "verificationResult"
$pipelineArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "pipelineResult" -ArtifactPath $ArtifactPath

$preflight = Get-StitchUnityPreflightObject -Map $map -ContractBundle $contracts
if (-not [string]::IsNullOrWhiteSpace($preflightArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $preflightArtifactPath -InputObject $preflight
}

if (-not $preflight.ready) {
    throw "Surface preflight failed. strategyMode='$($preflight.strategyMode)' targetExists='$($preflight.targetExists)'."
}

$translationStrategy = [string]$map.translationStrategy
$translationResult = $null
$dependencyResults = @()
$root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null
$dependencyResults = @(Invoke-StitchUnityDependencies -Root $root -Map $map)

switch ($translationStrategy) {
    "contract-complete-translator-v1" {
        throw "translationStrategy 'contract-complete-translator-v1' is reserved for a contract-complete translator. The prior constant-owned surface generator was removed, and this new translator is not implemented yet."
    }
    default {
        throw "Unsupported translationStrategy '$translationStrategy'."
    }
}

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
    contractRefs = $context.ContractRefs
    artifacts = [PSCustomObject]@{
        translationResult = $translationArtifactPath
        preflightResult = $preflightArtifactPath
        inspectionResult = $inspectionArtifactPath
        verificationResult = $verificationArtifactPath
        pipelineResult = $pipelineArtifactPath
    }
    preflight = $preflight
    dependencies = $dependencyResults
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
