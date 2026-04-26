param(
    [string]$UnityBridgeUrl,
    [int]$MenuTimeoutSec = 300
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
    Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 | Out-Null
}

$compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000
if (-not (Test-McpResponseSuccess -Response $compile)) {
    throw ("Compile wait failed before validation: {0}" -f ($compile | ConvertTo-Json -Compress -Depth 8))
}

$menuPath = "Tools/Nova1492/Validate Playable Part Closeout"
Write-Host ("Executing menu: {0}" -f $menuPath) -ForegroundColor Cyan

$response = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/menu/execute" -Body @{
    menuPath = $menuPath
} -TimeoutSec $MenuTimeoutSec -RequestTimeoutSec $MenuTimeoutSec

if (-not (Test-McpResponseSuccess -Response $response)) {
    throw ("Menu failed: {0} -> {1}" -f $menuPath, ($response | ConvertTo-Json -Compress -Depth 8))
}

$status = Invoke-McpGetJsonWithTransientRetry -Root $root -SubPath "/compile/status" -TimeoutSec 30

[PSCustomObject]@{
    success = $true
    baseUrl = $root
    menuPath = $menuPath
    response = $response
    compileStatus = $status
    reportPath = "artifacts/nova1492/nova_part_validation_closeout_report.md"
} | ConvertTo-Json -Depth 10
