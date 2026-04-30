# Quick MCP compile endpoint smoke test. Run from repo root or any cwd (uses script dir for port file).
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "unity-mcp\McpHelpers.ps1")
$base = Get-UnityMcpBaseUrl -ExplicitBaseUrl ""
$health = Wait-McpBridgeHealthy -Root $base -TimeoutSec 60
$base = $health.Root

Write-Host "--- GET /health" -ForegroundColor Cyan
$h = Invoke-McpGetJson -Root $base -SubPath "/health"
$h | Format-List ok, isCompiling, isPlaying, activeScene, port

Write-Host "`n--- GET /compile/status" -ForegroundColor Cyan
Invoke-McpGetJson -Root $base -SubPath "/compile/status" | Format-List

Write-Host "`n--- POST /compile/request {}" -ForegroundColor Cyan
$r = Invoke-McpJson -Root $base -SubPath "/compile/request" -Body @{}
$r | Format-List

Start-Sleep -Seconds 1
Write-Host "`n--- GET /compile/status (after request)" -ForegroundColor Cyan
Invoke-McpGetJson -Root $base -SubPath "/compile/status" | Format-List

Write-Host "`n--- POST /compile/wait idle (requestFirst:false, timeout 90s)" -ForegroundColor Cyan
$w = Invoke-McpJson -Root $base -SubPath "/compile/wait" -Body @{
    requestFirst     = $false
    timeoutMs        = 90000
    pollIntervalMs   = 100
} -TimeoutSec 120
$w | Format-List

Write-Host "`n--- Done" -ForegroundColor Green
