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

$mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath

$inspection = $null
if (-not [string]::IsNullOrWhiteSpace($InspectionPath)) {
    $inspection = Read-StitchUnityJsonFile -PathValue $InspectionPath
}
else {
    $root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
    Wait-McpBridgeHealthy -Root $root -TimeoutSec 30 | Out-Null
    $contracts = Get-StitchUnityContractBundle -Map $mapResult.Map
    $inspection = Get-StitchUnitySurfaceInspectionObject -Root $root -Map $mapResult.Map -ContractBundle $contracts
    $inspection.mapPath = $mapResult.Path
}

$verification = Get-StitchUnitySurfaceVerificationObject -Map $mapResult.Map -Inspection $inspection

$resolvedArtifactPath = $ArtifactPath
if ([string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    $resolvedArtifactPath = Get-StitchUnityArtifactPath -Map $mapResult.Map -Name "verificationResult"
}

if (-not [string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $resolvedArtifactPath -InputObject $verification
}

if ($AsJson) {
    $verification | ConvertTo-Json -Depth 20
}
else {
    $verification
}
