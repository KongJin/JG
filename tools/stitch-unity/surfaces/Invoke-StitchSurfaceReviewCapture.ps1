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
$map = $context.Map
$root = Get-StitchUnityMcpRoot -UnityBridgeUrl $UnityBridgeUrl
$result = Invoke-StitchUnityReviewCapture -Root $root -Map $map -ArtifactPath $ArtifactPath

if (-not [bool]$result.supported) {
    throw "No TempScene SceneView review capture route is configured for surface '$([string]$map.surfaceId)'."
}

$result | ConvertTo-Json -Depth 20
