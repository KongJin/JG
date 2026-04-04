# Quick MCP compile endpoint smoke test. Run from repo root or any cwd (uses script dir for port file).
$ErrorActionPreference = "Stop"
$portFile = Join-Path $PSScriptRoot "..\ProjectSettings\UnityMcpPort.txt"
$port = if (Test-Path $portFile) { (Get-Content $portFile -Raw).Trim() } else { "51234" }
$base = "http://127.0.0.1:$port"

Write-Host "--- GET /health" -ForegroundColor Cyan
$h = Invoke-RestMethod -Uri "$base/health" -Method Get
$h | Format-List ok, isCompiling, isPlaying, activeScene, port

Write-Host "`n--- GET /compile/status" -ForegroundColor Cyan
Invoke-RestMethod -Uri "$base/compile/status" -Method Get | Format-List

Write-Host "`n--- POST /compile/request {}" -ForegroundColor Cyan
$r = Invoke-RestMethod -Method Post -Uri "$base/compile/request" -ContentType "application/json" -Body "{}"
$r | Format-List

Start-Sleep -Seconds 1
Write-Host "`n--- GET /compile/status (after request)" -ForegroundColor Cyan
Invoke-RestMethod -Uri "$base/compile/status" -Method Get | Format-List

Write-Host "`n--- POST /compile/wait idle (requestFirst:false, timeout 90s)" -ForegroundColor Cyan
$waitBody = @{
    requestFirst     = $false
    timeoutMs        = 90000
    pollIntervalMs   = 100
} | ConvertTo-Json -Compress
$w = Invoke-RestMethod -Method Post -Uri "$base/compile/wait" -ContentType "application/json" -Body $waitBody -TimeoutSec 120
$w | Format-List

Write-Host "`n--- Done" -ForegroundColor Green
