param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$HtmlPath = "",
    [string]$ImagePath = "",
    [string]$TargetAssetPath = "",
    [string]$DraftPath = "",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = "",
    [switch]$WriteJsonArtifacts,
    [switch]$SkipReviewCapture,
    [string]$ReviewCaptureArtifactPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\..\engine\StitchUnityCommon.ps1"

try {
    $context = Get-StitchUnitySurfaceContext -SurfaceId $SurfaceId -MapPath $MapPath -HtmlPath $HtmlPath -ImagePath $ImagePath -TargetAssetPath $TargetAssetPath -DraftPath $DraftPath
}
catch {
    $fallbackSurfaceId = if (-not [string]::IsNullOrWhiteSpace($SurfaceId)) { $SurfaceId } else { "unknown-surface" }
    $fallbackPipelineArtifactPath = if ($WriteJsonArtifacts -or -not [string]::IsNullOrWhiteSpace($ArtifactPath)) {
        if (-not [string]::IsNullOrWhiteSpace($ArtifactPath)) {
            Resolve-StitchUnityRepoPath -PathValue $ArtifactPath
        }
        else {
            Resolve-StitchUnityRepoPath -PathValue ("artifacts/unity/{0}-pipeline-result.json" -f $fallbackSurfaceId)
        }
    }
    else {
        ""
    }

    if (-not [string]::IsNullOrWhiteSpace($fallbackPipelineArtifactPath)) {
        $blockedResult = [PSCustomObject]@{
            schemaVersion = "1.0.0"
            success = $false
            surfaceId = $fallbackSurfaceId
            strategyMode = ""
            translationStrategy = ""
            terminalVerdict = "blocked"
            blockedReason = $_.Exception.Message
            stageStatus = [PSCustomObject]@{
                validation = if (-not [string]::IsNullOrWhiteSpace($DraftPath)) { "blocked" } else { "not-run" }
                preflight = "not-run"
                dependencies = "not-run"
                translation = "not-run"
                reviewCapture = "not-run"
                pipeline = "written"
            }
            inputs = [PSCustomObject]@{
                sourceKind = if (-not [string]::IsNullOrWhiteSpace($DraftPath)) { "llm-draft" } else { "" }
                draftPath = $DraftPath
                target = [PSCustomObject]@{
                    kind = if (-not [string]::IsNullOrWhiteSpace($TargetAssetPath)) { "prefab" } else { "" }
                    assetPath = $TargetAssetPath
                }
            }
            artifacts = [PSCustomObject]@{
                reviewCapture = ""
                pipelineResult = $fallbackPipelineArtifactPath
            }
        }

        Write-StitchUnityArtifact -PathValue $fallbackPipelineArtifactPath -InputObject $blockedResult
    }

    throw
}
$mapResult = $context.MapResult
$map = $context.Map
$contracts = $context.Contracts

$pipelineArtifactPath = if ($WriteJsonArtifacts -or -not [string]::IsNullOrWhiteSpace($ArtifactPath)) {
    Resolve-StitchUnityArtifactOutputPath -Map $map -ArtifactName "pipelineResult" -ArtifactPath $ArtifactPath
}
else {
    ""
}

$preflight = Get-StitchUnityPreflightObject -Map $map -ContractBundle $contracts -ContractSource $context.ContractSource

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
                validation = if ([string]$context.ContractSource.sourceKind -eq "llm-draft") { "passed" } else { "not-run" }
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
$translation = $null
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
            $translation = Invoke-StitchUnityContractCompleteTranslation -Root $root -Map $map -ContractBundle $contracts
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
                validation = if ([string]$context.ContractSource.sourceKind -eq "llm-draft") { "passed" } else { "not-run" }
                preflight = if ($preflight.ready) { "passed" } else { "blocked" }
                dependencies = if (@($requiredDependencyFailures).Count -gt 0) { "failed" } elseif (@($optionalDependencyFailures).Count -gt 0) { "warning" } else { "passed" }
                translation = if ($null -ne $translation) { "partial" } else { "blocked" }
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
        translationProduced = ($null -ne $translation)
        reviewCaptureAttempted = [bool]$reviewCaptureResult.attempted
        reviewCaptureSupported = [bool]$reviewCaptureResult.supported
        targetKind = [string]$map.target.kind
        targetAssetPath = [string]$map.target.assetPath
    }
    stageStatus = [PSCustomObject]@{
        validation = if ([string]$context.ContractSource.sourceKind -eq "llm-draft") { "passed" } else { "not-run" }
        preflight = if ($preflight.ready) { "passed" } else { "failed" }
        dependencies = if (@($requiredDependencyFailures).Count -gt 0) { "failed" } elseif (@($optionalDependencyFailures).Count -gt 0) { "warning" } else { "passed" }
        translation = if ($null -ne $translation) { "passed" } else { "not-implemented" }
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
        reviewCapture = [string]$reviewCaptureResult.artifactPath
        pipelineResult = $pipelineArtifactPath
    }
    dependencies = [PSCustomObject]@{
        requiredFailures = @($requiredDependencyFailures)
        optionalFailures = @($optionalDependencyFailures)
        results = @($dependencyResults)
    }
    contractSource = $context.ContractSource
    translation = $translation
    reviewCapture = $reviewCaptureResult
}

if (-not [string]::IsNullOrWhiteSpace($pipelineArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $pipelineArtifactPath -InputObject $result
}

$result | ConvertTo-Json -Depth 20
