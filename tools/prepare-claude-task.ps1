<#
.SYNOPSIS
    Claude VS Code 확장에 붙여넣을 작업 프롬프트를 생성한다.
.EXAMPLE
    .\tools\prepare-claude-task.ps1 -TaskFile C:\Users\SOL\Desktop\plan_c.txt
    .\tools\prepare-claude-task.ps1 -TaskFile .\plan.txt -Files "Assets/Scripts/Features/Combat/README.md","Assets/Scripts/Features/Player/README.md"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TaskFile,

    [string[]]$Files = @(),

    [string]$ArtifactsDir = ""
)

$ErrorActionPreference = "Stop"
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $scriptDir "artifacts" }
$repoRoot = (git -C $scriptDir rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) { $repoRoot = Split-Path $scriptDir }

# --- Validate task file ---
if (-not (Test-Path $TaskFile)) {
    Write-Error "Task file not found: $TaskFile"
    return
}

$taskContent = Get-Content -Path $TaskFile -Raw -Encoding UTF8

# --- Detect referenced features from task content ---
$featurePattern = 'Features/(\w+)'
$mentionedFeatures = @()
$allMatches = [regex]::Matches($taskContent, $featurePattern)
foreach ($m in $allMatches) {
    $feat = $m.Groups[1].Value
    if ($mentionedFeatures -notcontains $feat) {
        $mentionedFeatures += $feat
    }
}

# Also detect from -Files parameter
foreach ($f in $Files) {
    $fileMatches = [regex]::Matches($f, $featurePattern)
    foreach ($m in $fileMatches) {
        $feat = $m.Groups[1].Value
        if ($mentionedFeatures -notcontains $feat) {
            $mentionedFeatures += $feat
        }
    }
}

# --- Build README list ---
$readmeList = @()
foreach ($feat in $mentionedFeatures) {
    $readmePath = "Assets/Scripts/Features/$feat/README.md"
    $fullPath = Join-Path $repoRoot $readmePath
    if (Test-Path $fullPath) {
        $readmeList += $readmePath
    }
}

# --- Build prompt ---
$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine('아래 작업 지시를 읽고 구현을 시작해.')
[void]$sb.AppendLine()
[void]$sb.AppendLine('---')
[void]$sb.AppendLine()
[void]$sb.AppendLine($taskContent.TrimEnd())
[void]$sb.AppendLine()
[void]$sb.AppendLine('---')
[void]$sb.AppendLine()

if ($readmeList.Count -gt 0) {
    [void]$sb.AppendLine('## 반드시 먼저 읽을 파일')
    foreach ($r in $readmeList) {
        [void]$sb.AppendLine("- ``$r``")
    }
    [void]$sb.AppendLine()
}

if ($Files.Count -gt 0) {
    [void]$sb.AppendLine('## 참고 파일')
    foreach ($f in $Files) {
        [void]$sb.AppendLine("- ``$f``")
    }
    [void]$sb.AppendLine()
}

$prompt = $sb.ToString()

# --- Save ---
if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outFile = Join-Path $ArtifactsDir "claude-task-$timestamp.md"
$prompt | Out-File -FilePath $outFile -Encoding UTF8 -NoNewline

# --- Clipboard ---
$prompt | Set-Clipboard

Write-Host "[prepare-claude-task] Saved: $outFile" -ForegroundColor Green
Write-Host "[prepare-claude-task] Copied to clipboard ($($prompt.Length) chars)" -ForegroundColor Green
Write-Host ""
Write-Host "Next: Ctrl+V in Claude VS Code panel and send." -ForegroundColor Cyan
