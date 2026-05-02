param(
    [Parameter(Mandatory = $true)][string]$SurfaceId,
    [string]$HtmlPath = "",
    [string]$ImagePath = "",
    [string]$PresentationOutputPath = "",
    [string]$TargetAssetPath = "",
    [switch]$CanGenerateOnly
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
    supported = $false
    terminalVerdict = "blocked"
    blockedReason = "legacy-ugui-translator-disabled"
    artifactKind = "stitch-presentation-profile-disabled"
    surfaceId = $SurfaceId
    htmlPath = $HtmlPath
    imagePath = $ImagePath
    targetAssetPath = $TargetAssetPath
    presentationOutputPath = $PresentationOutputPath
    acceptedRoute = "UITK candidate surface"
    message = "The old Stitch presentation profile generator fed the UGUI/TMP contract resolver and is disabled. Use Collect-StitchSourceFacts plus a UI Toolkit UXML/USS candidate workflow."
}

Write-JsonIfRequested -PathValue $PresentationOutputPath -InputObject $result
$result | ConvertTo-Json -Depth 8

if ($CanGenerateOnly.IsPresent) {
    exit 0
}

exit 1
