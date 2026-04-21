param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/StitchUnityCommon.ps1"

$mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath
$contracts = Get-StitchUnityContractBundle -Map $mapResult.Map

$result = [PSCustomObject]@{
    surfaceId = [string]$mapResult.Map.surfaceId
    mapPath = $mapResult.Path
    translationStrategy = [string]$mapResult.Map.translationStrategy
    target = $mapResult.Map.target
    manifestPath = $contracts.manifestPath
    intakePath = $contracts.intakePath
    blueprintPath = $contracts.blueprintPath
    artifactPaths = $mapResult.Map.artifactPaths
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 10
}
else {
    $result
}
