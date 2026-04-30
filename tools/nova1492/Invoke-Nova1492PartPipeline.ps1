param(
    [ValidateSet("analyze", "convert", "unity-refresh")]
    [string]$Stage = "analyze",
    [string]$UnityBridgeUrl,
    [string]$CatalogPath = "artifacts/nova1492/nova_part_catalog.csv",
    [string]$SourceRoot = "C:\Program Files (x86)\Nova1492",
    [string]$OutputRoot = "Assets/Art/Nova1492/GXConverted",
    [string]$IncludeRelative,
    [string]$Category,
    [string]$PartId,
    [switch]$ChangedOnly,
    [switch]$Diagnostics,
    [int]$TimeoutSec = 600
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$converterProject = Join-Path $repoRoot "tools\nova1492\GxObjConverter\GxObjConverter.csproj"

function Invoke-GxObjConverter {
    $arguments = @(
        "run",
        "--project",
        $converterProject,
        "--",
        "--stage",
        $Stage,
        "--source-root",
        $SourceRoot,
        "--output-root",
        $OutputRoot,
        "--catalog",
        $CatalogPath
    )

    if (-not [string]::IsNullOrWhiteSpace($IncludeRelative)) {
        $arguments += @("--include-relative", $IncludeRelative)
    }

    if (-not [string]::IsNullOrWhiteSpace($Category)) {
        $arguments += @("--category", $Category)
    }

    if (-not [string]::IsNullOrWhiteSpace($PartId)) {
        $arguments += @("--part-id", $PartId)
    }

    if ($ChangedOnly) {
        $arguments += "--changed-only"
    }

    if ($Diagnostics) {
        $arguments += "--diagnostics"
    }

    Write-Host ("[Nova1492Pipeline] dotnet {0}" -f ($arguments -join " ")) -ForegroundColor Cyan
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GxObjConverter failed with exit code $LASTEXITCODE"
    }
}

function Invoke-UnityRefresh {
    . $PSScriptRoot\..\unity-mcp\McpHelpers.ps1

    $root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
    $root = $health.Root

    if ($health.State.isPlaying) {
        throw "Unity is in Play Mode; stop Play Mode before running unity-refresh."
    }

    Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/asset/refresh" -Body @{} -TimeoutSec 60 -RequestTimeoutSec 60 | Out-Null
    $compile = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/compile/wait" -Body @{
        timeoutMs = 120000
        pollIntervalMs = 250
        requestFirst = $false
        cleanBuildCache = $false
    } -TimeoutSec 130 -RequestTimeoutSec 130

    if (-not (Test-McpResponseSuccess -Response $compile)) {
        throw ("Compile wait failed before unity-refresh: {0}" -f ($compile | ConvertTo-Json -Compress -Depth 8))
    }

    $menus = if ($ChangedOnly) {
        @("Tools/Nova1492/Create Changed Part Prefabs From Pipeline")
    } else {
        @(
            "Tools/Nova1492/Create Full Part Preview Prefabs",
            "Tools/Nova1492/Create Full Part Assembly Prefabs",
            "Tools/Nova1492/Generate Playable Part Assets"
        )
    }

    $results = @()
    foreach ($menuPath in $menus) {
        Write-Host ("[Nova1492Pipeline] Executing Unity menu: {0}" -f $menuPath) -ForegroundColor Cyan
        $response = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/menu/execute" -Body @{
            menuPath = $menuPath
        } -TimeoutSec $TimeoutSec -RequestTimeoutSec $TimeoutSec

        if (-not (Test-McpResponseSuccess -Response $response)) {
            throw ("Unity menu failed: {0} -> {1}" -f $menuPath, ($response | ConvertTo-Json -Compress -Depth 8))
        }

        $wait = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/compile/wait" -Body @{
            timeoutMs = 120000
            pollIntervalMs = 250
            requestFirst = $false
            cleanBuildCache = $false
        } -TimeoutSec 130 -RequestTimeoutSec 130

        if (-not (Test-McpResponseSuccess -Response $wait)) {
            throw ("Compile wait failed after {0}: {1}" -f $menuPath, ($wait | ConvertTo-Json -Compress -Depth 8))
        }

        $results += [PSCustomObject]@{
            menuPath = $menuPath
            response = $response
            compileWait = $wait
        }
    }

    [PSCustomObject]@{
        success = $true
        baseUrl = $root
        changedOnly = [bool]$ChangedOnly
        results = $results
    } | ConvertTo-Json -Depth 10
}

Push-Location $repoRoot
try {
    if ($Stage -eq "unity-refresh") {
        Invoke-UnityRefresh
    } else {
        Invoke-GxObjConverter
    }
}
finally {
    Pop-Location
}
