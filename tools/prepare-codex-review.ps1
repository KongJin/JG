<#
.SYNOPSIS
    현재 git diff + 프로젝트 규칙을 합쳐 Codex 리뷰 프롬프트를 생성한다.
.EXAMPLE
    .\tools\prepare-codex-review.ps1
    .\tools\prepare-codex-review.ps1 -Base "HEAD~3"
    .\tools\prepare-codex-review.ps1 -Focus "프렌들리 파이어 관계 배율이 올바른지"
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

# --- Gather diff (git warnings on stderr must not abort) ---
$ErrorActionPreference = "Continue"
$diffStat = git -C $repoRoot diff --stat $Base 2>&1 | Where-Object { $_ -is [string] }
$diffContent = git -C $repoRoot diff $Base 2>&1 | Where-Object { $_ -is [string] }
$diffStat = ($diffStat -join "`n")
$diffContent = ($diffContent -join "`n")

# Include staged changes if Base is HEAD
if ($Base -eq "HEAD") {
    $stagedStat = git -C $repoRoot diff --cached --stat 2>&1 | Where-Object { $_ -is [string] }
    $stagedContent = git -C $repoRoot diff --cached 2>&1 | Where-Object { $_ -is [string] }
    $stagedStat = ($stagedStat -join "`n")
    $stagedContent = ($stagedContent -join "`n")
    if ($stagedContent) {
        $diffStat = "$diffStat`n$stagedStat"
        $diffContent = "$diffContent`n$stagedContent"
    }
}

if (-not $diffContent) {
    Write-Warning "No diff found against $Base. Nothing to review."
    return
}

# --- Detect touched features ---
$changedFiles = git -C $repoRoot diff --name-only $Base 2>&1 | Where-Object { $_ -is [string] }
if ($Base -eq "HEAD") {
    $stagedFiles = git -C $repoRoot diff --cached --name-only 2>&1 | Where-Object { $_ -is [string] }
    if ($stagedFiles) {
        $changedFiles = ($changedFiles -join "`n") + "`n" + ($stagedFiles -join "`n")
    } else {
        $changedFiles = ($changedFiles -join "`n")
    }
} else {
    $changedFiles = ($changedFiles -join "`n")
}
$ErrorActionPreference = "Stop"

$featurePattern = 'Assets/Scripts/Features/(\w+)/'
$touchedFeatures = @()
foreach ($line in ($changedFiles -split "`n")) {
    $m = [regex]::Match($line, $featurePattern)
    if ($m.Success -and $touchedFeatures -notcontains $m.Groups[1].Value) {
        $touchedFeatures += $m.Groups[1].Value
    }
}

# --- Collect relevant READMEs ---
$readmeContents = @{}
foreach ($feat in $touchedFeatures) {
    $readmePath = Join-Path $repoRoot "Assets/Scripts/Features/$feat/README.md"
    if (Test-Path $readmePath) {
        $readmeContents[$feat] = Get-Content -Path $readmePath -Raw -Encoding UTF8
    }
}

# --- Load anti_patterns.md ---
$antiPatternsPath = Join-Path $repoRoot "agent/anti_patterns.md"
$antiPatterns = ""
if (Test-Path $antiPatternsPath) {
    $antiPatterns = Get-Content -Path $antiPatternsPath -Raw -Encoding UTF8
}

# --- Truncate diff if too large (keep under ~60k chars for prompt) ---
$maxDiffChars = 60000
$diffTruncated = $false
if ($diffContent.Length -gt $maxDiffChars) {
    $diffContent = $diffContent.Substring(0, $maxDiffChars)
    $diffTruncated = $true
}

# --- Build prompt ---
$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine("# Codex Review Request")
[void]$sb.AppendLine()
[void]$sb.AppendLine("아래 diff를 이 프로젝트의 아키텍처 규칙 기준으로 리뷰해 주세요.")
[void]$sb.AppendLine("문제점을 우선으로 보고하고, 각 항목에 심각도(critical/warning/info)를 붙여 주세요.")
[void]$sb.AppendLine()

if ($Focus) {
    [void]$sb.AppendLine("## Review Focus")
    [void]$sb.AppendLine($Focus)
    [void]$sb.AppendLine()
}

[void]$sb.AppendLine("## Changed Features")
foreach ($feat in $touchedFeatures) {
    [void]$sb.AppendLine("- $feat")
}
[void]$sb.AppendLine()

[void]$sb.AppendLine("## Diff Summary")
[void]$sb.AppendLine('```')
[void]$sb.AppendLine($diffStat)
[void]$sb.AppendLine('```')
[void]$sb.AppendLine()

[void]$sb.AppendLine("## Full Diff")
if ($diffTruncated) {
    [void]$sb.AppendLine("(truncated to ${maxDiffChars} chars)")
}
[void]$sb.AppendLine('```diff')
[void]$sb.AppendLine($diffContent)
[void]$sb.AppendLine('```')
[void]$sb.AppendLine()

# --- Append rules ---
[void]$sb.AppendLine("---")
[void]$sb.AppendLine()
[void]$sb.AppendLine("# Review Criteria")
[void]$sb.AppendLine()

if ($antiPatterns) {
    [void]$sb.AppendLine("## Anti Patterns (violations = critical)")
    [void]$sb.AppendLine($antiPatterns.TrimEnd())
    [void]$sb.AppendLine()
}

if ($readmeContents.Count -gt 0) {
    [void]$sb.AppendLine("## Feature READMEs (check consistency)")
    foreach ($kv in $readmeContents.GetEnumerator()) {
        [void]$sb.AppendLine("### $($kv.Key)")
        [void]$sb.AppendLine($kv.Value.TrimEnd())
        [void]$sb.AppendLine()
    }
}

[void]$sb.AppendLine("## Output Format")
[void]$sb.AppendLine('```')
[void]$sb.AppendLine("### Findings")
[void]$sb.AppendLine("- [critical/warning/info] 설명")
[void]$sb.AppendLine()
[void]$sb.AppendLine("### Assumptions (검증 필요)")
[void]$sb.AppendLine("- ...")
[void]$sb.AppendLine()
[void]$sb.AppendLine("### Recommended Fixes")
[void]$sb.AppendLine("- ...")
[void]$sb.AppendLine('```')

$prompt = $sb.ToString()

# --- Save ---
if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outFile = Join-Path $ArtifactsDir "codex-review-$timestamp.md"
$prompt | Out-File -FilePath $outFile -Encoding UTF8 -NoNewline

# --- Clipboard ---
$prompt | Set-Clipboard

# --- Summary ---
$fileCount = @($changedFiles -split "`n" | Where-Object { $_.Trim() }).Count
Write-Host "[prepare-codex-review] $fileCount files changed across features: $($touchedFeatures -join ', ')" -ForegroundColor Green
Write-Host "[prepare-codex-review] Saved: $outFile" -ForegroundColor Green
Write-Host "[prepare-codex-review] Copied to clipboard ($($prompt.Length) chars)" -ForegroundColor Green
if ($diffTruncated) {
    Write-Host "[prepare-codex-review] Warning: diff was truncated (>${maxDiffChars} chars)" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Next: paste into Codex or run 'codex' and provide the prompt." -ForegroundColor Cyan
