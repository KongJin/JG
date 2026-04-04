# JG_GameScene 플레이 중일 때만: 시작 스킬 2개 선택 + 확인 (MCP 버튼 invoke)
# 로비 없이 게임 씬만 열어 둔 뒤 또는 -CompleteGameSceneStartSkills 대신 수동 진입 후 사용.

[CmdletBinding()]
param(
    [string]$BaseUrl,
    [switch]$NoStopPlay
)

$ErrorActionPreference = "Stop"

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)
    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) { return $ExplicitBaseUrl.TrimEnd("/") }
    $portFile = Join-Path $PSScriptRoot "..\ProjectSettings\UnityMcpPort.txt"
    if (Test-Path $portFile) {
        $t = (Get-Content $portFile -Raw).Trim()
        if ($t -match '^\d+$') { return "http://127.0.0.1:$t" }
    }
    return "http://127.0.0.1:51234"
}

function Invoke-McpJson {
    param([string]$Root, [string]$SubPath, [object]$Body = $null)
    $uri = "$Root$SubPath"
    if ($null -eq $Body) { return Invoke-RestMethod -Method Post -Uri $uri }
    return Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body ($Body | ConvertTo-Json -Compress)
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$h = Invoke-RestMethod -Uri "$root/health" -Method Get
if (-not $h.ok) { throw "MCP /health not ok." }
if (-not $h.isPlaying) { throw "Play mode is off. Enter play on JG_GameScene first." }
if ($h.activeScene -ne "JG_GameScene") {
    throw "Active scene is '$($h.activeScene)', expected JG_GameScene."
}

Write-Host "MCP: $root | scene=JG_GameScene | start skill UI invoke" -ForegroundColor Cyan

$p0 = "/StartSkillSelectionCanvas/Panel/ButtonGrid/SkillButton0"
$p1 = "/StartSkillSelectionCanvas/Panel/ButtonGrid/SkillButton1"
$pConfirm = "/StartSkillSelectionCanvas/Panel/ConfirmButton"

Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body @{ path = $p0 }
Start-Sleep -Milliseconds 400
Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body @{ path = $p1 }
Start-Sleep -Milliseconds 400
Invoke-McpJson -Root $root -SubPath "/ui/button/invoke" -Body @{ path = $pConfirm }
Start-Sleep -Seconds 1

$r = Invoke-McpJson -Root $root -SubPath "/screenshot/capture" -Body @{
    outputPath = "Temp/UnityMcp/Screenshots/game-scene-after-start-skills.png"
    overwrite  = $true
}
Write-Host "Screenshot: $($r.relativePath)" -ForegroundColor Green

try {
    $c = Invoke-RestMethod -Uri "$root/console/logs?limit=60" -Method Get
    Write-Host "Console count=$($c.count)" -ForegroundColor Gray
    foreach ($i in $c.items) { Write-Host "[$($i.type)] $($i.message)" }
}
catch { Write-Host "console/logs: $($_.Exception.Message)" }

if (-not $NoStopPlay) {
    Invoke-McpJson -Root $root -SubPath "/play/stop"
    Write-Host "Play stopped." -ForegroundColor Gray
}
