param(
    [string]$UnityBridgeUrl,
    [int]$MenuTimeoutSec = 600
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot\..\unity-mcp\McpHelpers.ps1

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
$root = $health.Root

Write-Host ("Using Unity MCP bridge: {0}" -f $root) -ForegroundColor Cyan
Write-Host ("Editor scene={0} playing={1} compiling={2}" -f $health.State.activeScene, $health.State.isPlaying, $health.State.isCompiling) -ForegroundColor DarkGray

if ($health.State.isPlaying) {
    throw "Unity is in Play Mode; stop Play Mode before generating Nova1492 part assets."
}

$compile = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/compile/wait" -Body @{
    timeoutMs = 120000
    pollIntervalMs = 250
    requestFirst = $false
    cleanBuildCache = $false
} -TimeoutSec 130

if (-not (Test-McpResponseSuccess -Response $compile)) {
    throw ("Compile wait failed before generation: {0}" -f ($compile | ConvertTo-Json -Compress -Depth 8))
}

$menus = @(
    "Tools/Nova1492/Create Full Part Preview Prefabs",
    "Tools/Nova1492/Generate Playable Part Assets"
)

$results = @()
foreach ($menuPath in $menus) {
    Write-Host ("Executing menu: {0}" -f $menuPath) -ForegroundColor Cyan

    $response = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/menu/execute" -Body @{
        menuPath = $menuPath
    } -TimeoutSec $MenuTimeoutSec -RequestTimeoutSec $MenuTimeoutSec

    if (-not (Test-McpResponseSuccess -Response $response)) {
        throw ("Menu failed: {0} -> {1}" -f $menuPath, ($response | ConvertTo-Json -Compress -Depth 8))
    }

    $wait = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/compile/wait" -Body @{
        timeoutMs = 120000
        pollIntervalMs = 250
        requestFirst = $false
        cleanBuildCache = $false
    } -TimeoutSec 130

    if (-not (Test-McpResponseSuccess -Response $wait)) {
        throw ("Compile wait failed after {0}: {1}" -f $menuPath, ($wait | ConvertTo-Json -Compress -Depth 8))
    }

    $results += [PSCustomObject]@{
        menuPath = $menuPath
        response = $response
        compileWait = $wait
    }
}

$status = Invoke-McpGetJsonWithTransientRetry -Root $root -SubPath "/compile/status" -TimeoutSec 30

[PSCustomObject]@{
    success = $true
    baseUrl = $root
    results = $results
    compileStatus = $status
} | ConvertTo-Json -Depth 10
