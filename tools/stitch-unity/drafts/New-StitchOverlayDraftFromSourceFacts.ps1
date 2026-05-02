param(
    [string]$SurfaceId = "account-delete-confirm",
    [string]$HtmlPath = "",
    [string]$ImagePath = "",
    [string]$DraftPath = "",
    [switch]$NoActionButton
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $PathValue))
}

function Write-JsonIfRequested {
    param(
        [string]$PathValue,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return
    }

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    $directoryPath = Split-Path -Parent $resolvedPath
    if (-not [string]::IsNullOrWhiteSpace($directoryPath) -and -not (Test-Path -LiteralPath $directoryPath)) {
        New-Item -ItemType Directory -Path $directoryPath -Force | Out-Null
    }

    $json = $InputObject | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($resolvedPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

$result = [PSCustomObject][ordered]@{
    success = $false
    terminalVerdict = "blocked"
    blockedReason = "legacy-ugui-translator-disabled"
    artifactKind = "stitch-contract-draft-disabled"
    surfaceId = $SurfaceId
    htmlPath = $HtmlPath
    imagePath = $ImagePath
    draftPath = $DraftPath
    noActionButton = [bool]$NoActionButton
    acceptedRoute = "UITK candidate surface"
    message = "The old Stitch overlay contract draft generator emitted UGUI/TMP presentation contracts and is disabled. Use source facts plus a UI Toolkit UXML/USS candidate workflow instead."
}

Write-JsonIfRequested -PathValue $DraftPath -InputObject $result
$result | ConvertTo-Json -Depth 8
exit 1
