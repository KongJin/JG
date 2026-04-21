param(
    [string]$SurfaceId = "",
    [string]$MapPath = "",
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/StitchUnityCommon.ps1"

$mapResult = Get-StitchUnityMapObject -SurfaceId $SurfaceId -MapPath $MapPath
$blocks = foreach ($entry in @(Get-StitchUnityBlockEntries -Map $mapResult.Map)) {
    [PSCustomObject]@{
        blockId = $entry.blockId
        hostPath = [string](Get-StitchUnityOptionalPropertyValue -InputObject $entry.mapping -Name "hostPath" -Default "")
        candidatePaths = @(Get-StitchUnityCandidatePaths -BlockMapping $entry.mapping)
        requiredComponents = @(Get-StitchUnityOptionalArray -InputObject $entry.mapping -Name "requiredComponents")
        expectedLayout = Get-StitchUnityOptionalPropertyValue -InputObject $entry.mapping -Name "expectedLayout"
        verificationTags = @(Get-StitchUnityOptionalArray -InputObject $entry.mapping -Name "verificationTags")
        stateBindings = @(Get-StitchUnityOptionalArray -InputObject $entry.mapping -Name "stateBindings")
        notes = @(Get-StitchUnityOptionalArray -InputObject $entry.mapping -Name "notes")
    }
}

$result = [PSCustomObject]@{
    surfaceId = [string]$mapResult.Map.surfaceId
    mapPath = $mapResult.Path
    target = $mapResult.Map.target
    translationStrategy = [string]$mapResult.Map.translationStrategy
    blocks = $blocks
    verification = $mapResult.Map.verification
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 12
}
else {
    $result
}
