<#
.SYNOPSIS
    코드/씬 diff를 분리하여 Codex 리뷰 프롬프트 2개를 생성한다.
.EXAMPLE
    .\tools\prepare-codex-review-split.ps1
    .\tools\prepare-codex-review-split.ps1 -Focus "다운/구조 시스템 버그 수정"
#>
[CmdletBinding()]
param(
    [string]$Base = "HEAD",
    [string]$Focus = "",
    [string]$ArtifactsDir = ""
)

$ErrorActionPreference = "Stop"
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $scriptDir "artifacts" }
$repoRoot = (git -C $scriptDir rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) { $repoRoot = Split-Path $scriptDir }

# --- Gather diffs ---
$ErrorActionPreference = "Continue"

# Code only (exclude scene, assets, settings)
$codeDiffStat = git -C $repoRoot diff --stat $Base -- ':(exclude)*.unity' ':(exclude)*.asset' ':(exclude)*.inputactions' ':(exclude).claude/*' 2>&1 | Where-Object { $_ -is [string] }
$codeDiffContent = git -C $repoRoot diff $Base -- ':(exclude)*.unity' ':(exclude)*.asset' ':(exclude)*.inputactions' ':(exclude).claude/*' 2>&1 | Where-Object { $_ -is [string] }
$codeDiffStat = ($codeDiffStat -join "`n")
$codeDiffContent = ($codeDiffContent -join "`n")

# Scene only
$sceneDiffStat = git -C $repoRoot diff --stat $Base -- '*.unity' 2>&1 | Where-Object { $_ -is [string] }
$sceneDiffContent = git -C $repoRoot diff $Base -- '*.unity' 2>&1 | Where-Object { $_ -is [string] }
$sceneDiffStat = ($sceneDiffStat -join "`n")
$sceneDiffContent = ($sceneDiffContent -join "`n")

# Include staged if Base is HEAD
if ($Base -eq "HEAD") {
    $stagedCode = git -C $repoRoot diff --cached -- ':(exclude)*.unity' ':(exclude)*.asset' ':(exclude)*.inputactions' ':(exclude).claude/*' 2>&1 | Where-Object { $_ -is [string] }
    $stagedScene = git -C $repoRoot diff --cached -- '*.unity' 2>&1 | Where-Object { $_ -is [string] }
    if ($stagedCode) { $codeDiffContent = "$codeDiffContent`n" + ($stagedCode -join "`n") }
    if ($stagedScene) { $sceneDiffContent = "$sceneDiffContent`n" + ($stagedScene -join "`n") }
}

$ErrorActionPreference = "Stop"

if (-not $codeDiffContent -and -not $sceneDiffContent) {
    Write-Warning "No diff found against $Base."
    return
}

# --- Detect touched features ---
$ErrorActionPreference = "Continue"
$changedFiles = git -C $repoRoot diff --name-only $Base 2>&1 | Where-Object { $_ -is [string] }
$ErrorActionPreference = "Stop"

$featurePattern = 'Assets/Scripts/Features/(\w+)/'
$touchedFeatures = @()
foreach ($line in ($changedFiles -join "`n") -split "`n") {
    $m = [regex]::Match($line, $featurePattern)
    if ($m.Success -and $touchedFeatures -notcontains $m.Groups[1].Value) {
        $touchedFeatures += $m.Groups[1].Value
    }
}

# --- Load anti_patterns ---
$antiPatternsPath = Join-Path $repoRoot "agent/anti_patterns.md"
$antiPatterns = ""
if (Test-Path $antiPatternsPath) {
    $antiPatterns = Get-Content -Path $antiPatternsPath -Raw -Encoding UTF8
}

# --- Load READMEs ---
$readmeSection = ""
foreach ($feat in $touchedFeatures) {
    $readmePath = Join-Path $repoRoot "Assets/Scripts/Features/$feat/README.md"
    if (Test-Path $readmePath) {
        $content = Get-Content -Path $readmePath -Raw -Encoding UTF8
        $readmeSection += "### $feat`n$($content.TrimEnd())`n`n"
    }
}

# --- Ensure output dir ---
if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$codeFocus = if ($Focus) { "$Focus (code)" } else { "Code changes review" }
$sceneFocus = if ($Focus) { "$Focus (scene wiring)" } else { "Scene wiring review" }

# --- Code review file ---
if ($codeDiffContent) {
    $featList = ($touchedFeatures | ForEach-Object { "- $_" }) -join "`n"
    $codePrompt = @"
# Codex Review Request (Code Only)

아래 diff를 이 프로젝트의 아키텍처 규칙 기준으로 리뷰해 주세요.
문제점을 우선으로 보고하고, 각 항목에 심각도(critical/warning/info)를 붙여 주세요.

## Review Focus
$codeFocus

## Changed Features
$featList

## Diff Summary
``````
$($codeDiffStat.TrimEnd())
``````

## Full Diff
``````diff
$($codeDiffContent.TrimEnd())
``````

---

# Review Criteria

## Anti Patterns (violations = critical)
$($antiPatterns.TrimEnd())

## Feature READMEs (check consistency)
$readmeSection

## Output Format
``````
### Findings
- [critical/warning/info] 설명

### Assumptions (검증 필요)
- ...

### Recommended Fixes
- ...
``````
"@

    $codeFile = Join-Path $ArtifactsDir "codex-review-code-$timestamp.md"
    $codePrompt | Out-File -FilePath $codeFile -Encoding UTF8 -NoNewline
    Write-Host "[code]  Saved: $codeFile ($($codePrompt.Length) chars)" -ForegroundColor Green
}

# --- Scene review file ---
if ($sceneDiffContent) {
    $scenePrompt = @"
# Codex Review Request (Scene Only)

아래 씬 diff를 리뷰해 주세요.
컴포넌트 추가/연결, SerializeField 누락, UI 구조를 중심으로 확인해 주세요.

## Review Focus
$sceneFocus

## Diff Summary
``````
$($sceneDiffStat.TrimEnd())
``````

## Full Diff
``````diff
$($sceneDiffContent.TrimEnd())
``````

## Output Format
``````
### Findings
- [critical/warning/info] 설명

### Recommended Fixes
- ...
``````
"@

    $sceneFile = Join-Path $ArtifactsDir "codex-review-scene-$timestamp.md"
    $scenePrompt | Out-File -FilePath $sceneFile -Encoding UTF8 -NoNewline
    Write-Host "[scene] Saved: $sceneFile ($($scenePrompt.Length) chars)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Next: paste each file into Codex separately." -ForegroundColor Cyan
