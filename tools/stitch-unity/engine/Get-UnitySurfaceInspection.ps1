param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = "",
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/StitchUnityCommon.ps1"

$context = Get-StitchUnitySurfaceContext -SurfaceId $SurfaceId -MapPath $MapPath
$root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null

$inspection = Get-StitchUnitySurfaceInspectionObject -Root $root -Map $context.Map -ContractBundle $context.Contracts
$inspection.mapPath = $context.MapResult.Path

$resolvedArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $context.Map -ArtifactName "inspectionResult" -ArtifactPath $ArtifactPath

if (-not [string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $resolvedArtifactPath -InputObject $inspection
}

if ($AsJson) {
    $inspection | ConvertTo-Json -Depth 20
}
else {
    $inspection
}
