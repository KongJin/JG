param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = "",
    [switch]$SkipReviewCapture,
    [string]$ReviewCaptureArtifactPath = "",
    [string]$CompiledContractDebugPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\engine\StitchUnityCommon.ps1"

$context = Get-StitchUnitySurfaceContext -SurfaceId $SurfaceId -MapPath $MapPath -CompiledContractDebugPath $CompiledContractDebugPath
$mapResult = $context.MapResult
$map = $context.Map
$contracts = $context.Contracts

$translationArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "translationResult"
$preflightArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "preflightResult"
$pipelineArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "pipelineResult" -ArtifactPath $ArtifactPath

$preflight = Get-StitchUnityPreflightObject -Map $map -ContractBundle $contracts -ContractSource $context.ContractSource
if (-not [string]::IsNullOrWhiteSpace($preflightArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $preflightArtifactPath -InputObject $preflight
}

if (-not $preflight.ready) {
    $blockingCodes = @($preflight.blockingIssues | ForEach-Object { [string]$_.code })
    $blockingSummary = if ($blockingCodes.Count -gt 0) {
        " blockingIssues='$([string]::Join(', ', $blockingCodes))'."
    }
    else {
        "."
    }

    if (-not [string]::IsNullOrWhiteSpace($pipelineArtifactPath)) {
        $blockedResult = [PSCustomObject]@{
            schemaVersion = "1.0.0"
            success = $false
            surfaceId = [string]$map.surfaceId
            strategyMode = [string]$map.strategyMode
            translationStrategy = [string]$map.translationStrategy
            terminalVerdict = "blocked"
            blockedReason = [string]$preflight.blockedReason
            stageStatus = [PSCustomObject]@{
                preflight = "blocked"
                dependencies = "not-run"
                translation = "not-run"
                reviewCapture = "not-run"
                pipeline = "written"
            }
            inputs = [PSCustomObject]@{
                sourceKind = [string]$context.ContractSource.sourceKind
                mapPath = $mapResult.Path
                manifestPath = [string]$context.ContractRefs.manifestPath
                presentationPath = [string]$context.ContractRefs.presentationPath
                target = $map.target
            }
            artifacts = [PSCustomObject]@{
                translationResult = $translationArtifactPath
                preflightResult = $preflightArtifactPath
                reviewCapture = ""
                pipelineResult = $pipelineArtifactPath
            }
            preflight = $preflight
            contractSource = $context.ContractSource
        }

        Write-StitchUnityArtifact -PathValue $pipelineArtifactPath -InputObject $blockedResult
    }

    throw "Surface preflight failed. strategyMode='$($preflight.strategyMode)' targetExists='$($preflight.targetExists)'.$blockingSummary"
}

$translationStrategy = [string]$map.translationStrategy
$translationResult = $null
$dependencyResults = @()
$root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null
$dependencyResults = @(Invoke-StitchUnityDependencies -Root $root -Map $map)

$requiredDependencyFailures = @($dependencyResults | Where-Object { $_.required -and -not $_.ok })
$optionalDependencyFailures = @($dependencyResults | Where-Object { (-not $_.required) -and -not $_.ok })
$reviewCaptureResult = [PSCustomObject]@{
    attempted = $false
    supported = $false
    status = if ($SkipReviewCapture) { "skipped" } else { "not-requested" }
    surfaceId = [string]$map.surfaceId
    routeKind = ""
    menuPath = ""
    artifactPath = ""
    relativeArtifactPath = ""
    reason = if ($SkipReviewCapture) { "skip-review-capture-requested" } else { "review-capture-not-run-yet" }
    consoleErrors = $null
}

try {
    switch ($translationStrategy) {
        "contract-complete-translator-v1" {
            $translationResult = Invoke-StitchUnityContractCompleteTranslation -Root $root -Map $map -ContractBundle $contracts
        }
        default {
            throw "Unsupported translationStrategy '$translationStrategy'."
        }
    }

    Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null
    if (-not $SkipReviewCapture) {
        $reviewCaptureResult = Invoke-StitchUnityReviewCapture -Root $root -Map $map -ArtifactPath $ReviewCaptureArtifactPath
    }
}
catch {
    if (-not [string]::IsNullOrWhiteSpace($pipelineArtifactPath)) {
        $blockedResult = [PSCustomObject]@{
            schemaVersion = "1.0.0"
            success = $false
            surfaceId = [string]$map.surfaceId
            strategyMode = [string]$map.strategyMode
            translationStrategy = $translationStrategy
            terminalVerdict = "blocked"
            blockedReason = $_.Exception.Message
            stageStatus = [PSCustomObject]@{
                preflight = if ($preflight.ready) { "passed" } else { "blocked" }
                dependencies = if (@($requiredDependencyFailures).Count -gt 0) { "failed" } elseif (@($optionalDependencyFailures).Count -gt 0) { "warning" } else { "passed" }
                translation = if ($null -ne $translationResult) { "partial" } else { "blocked" }
                reviewCapture = [string]$reviewCaptureResult.status
                pipeline = "written"
            }
            inputs = [PSCustomObject]@{
                sourceKind = [string]$context.ContractSource.sourceKind
                mapPath = $mapResult.Path
                manifestPath = [string]$context.ContractRefs.manifestPath
                presentationPath = [string]$context.ContractRefs.presentationPath
                target = $map.target
            }
            artifacts = [PSCustomObject]@{
                translationResult = $translationArtifactPath
                preflightResult = $preflightArtifactPath
                reviewCapture = [string]$reviewCaptureResult.artifactPath
                pipelineResult = $pipelineArtifactPath
            }
            dependencies = [PSCustomObject]@{
                requiredFailures = @($requiredDependencyFailures)
                optionalFailures = @($optionalDependencyFailures)
                results = @($dependencyResults)
            }
            preflight = $preflight
            contractSource = $context.ContractSource
        }

        Write-StitchUnityArtifact -PathValue $pipelineArtifactPath -InputObject $blockedResult
    }

    throw
}

$result = [PSCustomObject]@{
    schemaVersion = "1.0.0"
    success = $true
    surfaceId = [string]$map.surfaceId
    strategyMode = [string]$map.strategyMode
    translationStrategy = $translationStrategy
    terminalVerdict = ""
    blockedReason = ""
    summary = [PSCustomObject]@{
        preflightReady = [bool]$preflight.ready
        dependencyCount = @($dependencyResults).Count
        requiredDependencyFailureCount = @($requiredDependencyFailures).Count
        optionalDependencyFailureCount = @($optionalDependencyFailures).Count
        translationProducedResult = ($null -ne $translationResult)
        reviewCaptureAttempted = [bool]$reviewCaptureResult.attempted
        reviewCaptureSupported = [bool]$reviewCaptureResult.supported
        targetKind = [string]$map.target.kind
        targetAssetPath = [string]$map.target.assetPath
    }
    stageStatus = [PSCustomObject]@{
        preflight = if ($preflight.ready) { "passed" } else { "failed" }
        dependencies = if (@($requiredDependencyFailures).Count -gt 0) { "failed" } elseif (@($optionalDependencyFailures).Count -gt 0) { "warning" } else { "passed" }
        translation = if ($null -ne $translationResult) { "passed" } else { "not-implemented" }
        reviewCapture = [string]$reviewCaptureResult.status
        pipeline = "written"
    }
    inputs = [PSCustomObject]@{
        sourceKind = [string]$context.ContractSource.sourceKind
        mapPath = $mapResult.Path
        manifestPath = [string]$context.ContractRefs.manifestPath
        presentationPath = [string]$context.ContractRefs.presentationPath
        target = $map.target
    }
    artifacts = [PSCustomObject]@{
        translationResult = $translationArtifactPath
        preflightResult = $preflightArtifactPath
        reviewCapture = [string]$reviewCaptureResult.artifactPath
        pipelineResult = $pipelineArtifactPath
    }
    dependencies = [PSCustomObject]@{
        requiredFailures = @($requiredDependencyFailures)
        optionalFailures = @($optionalDependencyFailures)
        results = @($dependencyResults)
    }
    contractSource = $context.ContractSource
    translation = $translationResult
    reviewCapture = $reviewCaptureResult
}

if (-not [string]::IsNullOrWhiteSpace($translationArtifactPath) -and $null -ne $translationResult) {
    Write-StitchUnityArtifact -PathValue $translationArtifactPath -InputObject $translationResult
}

if (-not [string]::IsNullOrWhiteSpace($pipelineArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $pipelineArtifactPath -InputObject $result
}

$result | ConvertTo-Json -Depth 20
