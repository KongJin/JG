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

$mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath
$contracts = Get-StitchUnityContractBundle -Map $mapResult.Map
$root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null

$inspection = Get-StitchUnitySurfaceInspectionObject -Root $root -Map $mapResult.Map -ContractBundle $contracts
$inspection.mapPath = $mapResult.Path

$resolvedArtifactPath = $ArtifactPath
if ([string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    $resolvedArtifactPath = Get-StitchUnityArtifactPath -Map $mapResult.Map -Name "inspectionResult"
}

if (-not [string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $resolvedArtifactPath -InputObject $inspection
}

if ($AsJson) {
    $inspection | ConvertTo-Json -Depth 20
}
else {
    $inspection
}
