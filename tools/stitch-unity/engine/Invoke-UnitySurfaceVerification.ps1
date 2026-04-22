param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$UnityBridgeUrl = "",
    [string]$InspectionPath = "",
    [string]$ArtifactPath = "",
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/StitchUnityCommon.ps1"

$context = Get-StitchUnitySurfaceContext -SurfaceId $SurfaceId -MapPath $MapPath

$inspection = $null
if (-not [string]::IsNullOrWhiteSpace($InspectionPath)) {
    $inspection = Read-StitchUnityJsonFile -PathValue $InspectionPath
}
else {
    $root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
    Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null
    $inspection = Get-StitchUnitySurfaceInspectionObject -Root $root -Map $context.Map -ContractBundle $context.Contracts
    $inspection.mapPath = $context.MapResult.Path
}

$verification = Get-StitchUnitySurfaceVerificationObject -Map $context.Map -Inspection $inspection

$resolvedArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $context.Map -ArtifactName "verificationResult" -ArtifactPath $ArtifactPath

if (-not [string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $resolvedArtifactPath -InputObject $verification
}

if ($AsJson) {
    $verification | ConvertTo-Json -Depth 20
}
else {
    $verification
}
