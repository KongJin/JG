param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [int]$LargeFileLineThreshold = 320,
    [int]$MaxItemsPerSection = 30,
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

function Get-CsFiles {
    param([string]$Root)

    $assetsScripts = Join-Path $Root "Assets\Scripts"
    if (-not (Test-Path -LiteralPath $assetsScripts)) {
        return @()
    }

    return @(
        Get-ChildItem -LiteralPath $assetsScripts -Recurse -File -Filter "*.cs" |
            Where-Object { $_.FullName -notmatch "\\(obj|bin|Library|Temp)\\" }
    )
}

function Get-ClassNames {
    param([string]$Text)

    return @(
        [regex]::Matches($Text, "\b(?:class|struct|interface)\s+([A-Za-z_][A-Za-z0-9_]*)") |
            ForEach-Object { $_.Groups[1].Value }
    )
}

function Get-CodeReferenceCount {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Token
    )

    $assetsScripts = Join-Path $Root "Assets\Scripts"
    if (-not (Test-Path -LiteralPath $assetsScripts)) {
        return 0
    }

    $rg = Get-Command rg -ErrorAction SilentlyContinue
    if ($null -ne $rg) {
        return @(& $rg.Source --fixed-strings --glob "*.cs" $Token $assetsScripts 2>$null).Count
    }

    $count = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $assetsScripts -Recurse -File -Filter "*.cs" -ErrorAction SilentlyContinue)) {
        $matches = @(Select-String -LiteralPath $file.FullName -SimpleMatch -Pattern $Token -AllMatches -ErrorAction SilentlyContinue)
        foreach ($match in $matches) {
            $count += @($match.Matches).Count
        }
    }

    $count
}

$csFiles = @(Get-CsFiles -Root $RepoRoot)
$largeFiles = New-Object System.Collections.Generic.List[object]
$helperTypes = New-Object System.Collections.Generic.List[object]
$nestedTernary = New-Object System.Collections.Generic.List[object]
$rootRuntimeVisualHelpers = New-Object System.Collections.Generic.List[object]

foreach ($file in $csFiles) {
    $relativePath = Get-WorkflowRepoPath -RepoRoot $RepoRoot -Path $file.FullName
    $lines = @(Get-Content -LiteralPath $file.FullName)
    if ($lines.Count -ge $LargeFileLineThreshold) {
        $largeFiles.Add([PSCustomObject]@{
            File = $relativePath
            Lines = $lines.Count
        })
    }

    $text = ($lines -join "`n")
    foreach ($className in @(Get-ClassNames -Text $text)) {
        if ($className -match "(Helper|Applier|Writer|Controller|Evaluator|Resolver|Factory)$") {
            $referenceCount = Get-CodeReferenceCount -Root $RepoRoot -Token $className
            $helperTypes.Add([PSCustomObject]@{
                Type = $className
                File = $relativePath
                ReferenceLines = $referenceCount
                OneUseCandidate = ($referenceCount -le 3)
            })
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $withoutNullOperators = $line -replace "\?\?", "" -replace "\?\.", ""
        if ($withoutNullOperators -match "(?<!\?)\?(?!\?).+:") {
            $nestedTernary.Add([PSCustomObject]@{
                File = $relativePath
                Line = $i + 1
                Text = $line.Trim()
            })
        }
    }

    $isFeatureRootFile = $relativePath -match "^Assets/Scripts/Features/[^/]+/[^/]+\.cs$"
    $looksRuntimeVisualOnly = $text -match "\b(LineRenderer|Canvas|RectTransform|Material|Shader\.Find|new GameObject|View|Preview)\b"
    if ($isFeatureRootFile -and $looksRuntimeVisualOnly) {
        $rootRuntimeVisualHelpers.Add([PSCustomObject]@{
            File = $relativePath
            Signal = "feature-root runtime visual code"
        })
    }
}

$result = [PSCustomObject]@{
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    repoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
    largeFileLineThreshold = $LargeFileLineThreshold
    largeFiles = @($largeFiles.ToArray())
    helperTypes = @($helperTypes.ToArray())
    nestedTernary = @($nestedTernary.ToArray())
    rootRuntimeVisualHelpers = @($rootRuntimeVisualHelpers.ToArray())
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 20
    return
}

Write-WorkflowSection "Large C# Files"
foreach ($item in @($largeFiles | Sort-Object Lines -Descending | Select-Object -First $MaxItemsPerSection)) {
    Write-Host ("{0} lines={1}" -f $item.File, $item.Lines)
}

Write-WorkflowSection "One-Use Or Broad Helper Candidates"
foreach ($item in @($helperTypes | Sort-Object OneUseCandidate, ReferenceLines, Type -Descending | Select-Object -First $MaxItemsPerSection)) {
    $mark = if ($item.OneUseCandidate) { "one-use?" } else { "check" }
    Write-Host ("{0} {1} refs={2} file={3}" -f $mark, $item.Type, $item.ReferenceLines, $item.File)
}

Write-WorkflowSection "Nested Ternary Candidates"
foreach ($item in @($nestedTernary | Select-Object -First $MaxItemsPerSection)) {
    Write-Host ("{0}:{1} {2}" -f $item.File, $item.Line, $item.Text)
}

Write-WorkflowSection "Feature-Root Runtime Visual Helper Candidates"
foreach ($item in @($rootRuntimeVisualHelpers | Sort-Object File | Select-Object -First $MaxItemsPerSection)) {
    Write-Host ("{0} - {1}" -f $item.File, $item.Signal)
}

if ($largeFiles.Count -eq 0 -and $helperTypes.Count -eq 0 -and $nestedTernary.Count -eq 0 -and $rootRuntimeVisualHelpers.Count -eq 0) {
    Write-Host "No obvious simplification candidates found."
}
