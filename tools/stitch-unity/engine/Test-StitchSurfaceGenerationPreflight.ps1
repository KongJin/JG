param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [string]$ArtifactPath = "",
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/StitchUnityCommon.ps1"

$context = Get-StitchUnitySurfaceContext -SurfaceId $SurfaceId -MapPath $MapPath
$preflight = Get-StitchUnityPreflightObject -Map $context.Map -ContractBundle $context.Contracts

$resolvedArtifactPath = Resolve-StitchUnityArtifactOutputPath -Map $context.Map -ArtifactName "preflightResult" -ArtifactPath $ArtifactPath

if (-not [string]::IsNullOrWhiteSpace($resolvedArtifactPath)) {
    Write-StitchUnityArtifact -PathValue $resolvedArtifactPath -InputObject $preflight
}

if ($AsJson) {
    $preflight | ConvertTo-Json -Depth 20
}
else {
    $preflight
}
