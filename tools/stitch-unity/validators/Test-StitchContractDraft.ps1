param(
    [Parameter(Mandatory = $true)][string]$DraftPath,
    [string]$SurfaceId = "",
    [string]$TargetAssetPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$result = [PSCustomObject][ordered]@{
    success = $false
    terminalVerdict = "blocked"
    blockedReason = "legacy-ugui-translator-disabled"
    artifactKind = "stitch-contract-draft-validator-disabled"
    draftPath = $DraftPath
    surfaceId = $SurfaceId
    targetAssetPath = $TargetAssetPath
    acceptedRoute = "UITK candidate surface"
    issues = @(
        [PSCustomObject][ordered]@{
            code = "legacy-contract-draft-disabled"
            path = "contracts"
            message = "The old Stitch contract draft validator accepted UGUI/TMP presentation contracts and is disabled. Use source facts plus a UI Toolkit UXML/USS candidate workflow instead."
        }
    )
}

$result | ConvertTo-Json -Depth 8
exit 1
