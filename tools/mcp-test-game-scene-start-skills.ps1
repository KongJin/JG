# JG_GameScene 플레이 중일 때만: 시작 스킬 2개 선택 + 확인 (MCP 버튼 invoke)
# 로비 없이 게임 씬만 열어 둔 뒤 또는 -CompleteGameSceneStartSkills 대신 수동 진입 후 사용.

[CmdletBinding()]
param(
    [string]$BaseUrl,
    [switch]$NoStopPlay
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "mcp-test-common.ps1")

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$h = Invoke-McpGetJson -Root $root -SubPath "/health"
if (-not $h.ok) { throw "MCP /health not ok." }
if (-not $h.isPlaying) { throw "Play mode is off. Enter play on JG_GameScene first." }
if ($h.activeScene -ne "JG_GameScene") {
    throw "Active scene is '$($h.activeScene)', expected JG_GameScene."
}

Write-Host "MCP: $root | scene=JG_GameScene | start skill UI invoke" -ForegroundColor Cyan

$p0 = Get-McpUiPath -Key "Game.StartSkillButton0"
$p1 = Get-McpUiPath -Key "Game.StartSkillButton1"
$pConfirm = Get-McpUiPath -Key "Game.StartSkillConfirm"

$pick0 = Invoke-McpButton -Root $root -Path $p0.Path -FallbackName $p0.FallbackName -Label "Start Skill Button 0"
Start-Sleep -Milliseconds 400
$pick1 = Invoke-McpButton -Root $root -Path $p1.Path -FallbackName $p1.FallbackName -Label "Start Skill Button 1"
Start-Sleep -Milliseconds 400
$confirm = Invoke-McpButton -Root $root -Path $pConfirm.Path -FallbackName $pConfirm.FallbackName -Label "Start Skill Confirm"
Start-Sleep -Seconds 1
Write-Host "Invoked paths: $($pick0.path), $($pick1.path), confirm=$($confirm.path)" -ForegroundColor Yellow

$r = Invoke-McpJson -Root $root -SubPath "/screenshot/capture" -Body @{
    outputPath = "Temp/UnityMcp/Screenshots/game-scene-after-start-skills.png"
    overwrite  = $true
}
Write-Host "Screenshot: $($r.relativePath)" -ForegroundColor Green

Write-McpRecentConsole -Root $root -Label "after start skill confirm" -LogLimit 60 -ErrorLimit 20

if (-not $NoStopPlay) {
    Invoke-McpJson -Root $root -SubPath "/play/stop"
    Write-Host "Play stopped." -ForegroundColor Gray
}
