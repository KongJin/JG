param(
    [string]$UnityBridgeUrl,
    [int]$TimeoutMs = 120000,
    [int]$PollIntervalMs = 250,
    [int]$PostMenuDelaySec = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot\McpHelpers.ps1

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl

Write-Host "Using Unity MCP bridge: $root" -ForegroundColor Cyan

$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0))
Write-Host ("Editor healthy. scene={0} playing={1} compiling={2}" -f $health.State.activeScene, $health.State.isPlaying, $health.State.isCompiling) -ForegroundColor DarkGray

$compile = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/compile/wait" -Body @{
    timeoutMs = $TimeoutMs
    pollIntervalMs = $PollIntervalMs
    requestFirst = $true
    cleanBuildCache = $false
} -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0))

if (-not $compile.ok) {
    throw ("Compile wait failed. timedOut={0} isCompiling={1}" -f $compile.timedOut, $compile.isCompiling)
}

$menu = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/menu/execute" -Body @{
    menuPath = "Assets/Open C# Project"
} -TimeoutSec 30

if (-not (Test-McpResponseSuccess -Response $menu)) {
    throw ("Open C# Project menu failed: {0}" -f ($menu | ConvertTo-Json -Compress))
}

Start-Sleep -Seconds $PostMenuDelaySec

$status = Invoke-McpGetJsonWithTransientRetry -Root $root -SubPath "/compile/status" -TimeoutSec 30

[PSCustomObject]@{
    success = $true
    baseUrl = $root
    compile = $compile
    menu = $menu
    compileStatus = $status
    note = "Unity editor sync requested via current editor instance. Generated .csproj files should now be refreshed."
} | ConvertTo-Json -Depth 6
