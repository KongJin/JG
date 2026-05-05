param(
    [string[]]$Roots = @(
        "artifacts/unity/current",
        "artifacts/nova1492/arm-captures",
        "artifacts/nova1492/body-captures",
        "artifacts/nova1492/leg-captures"
    ),
    [int]$MaxPngFiles = 6,
    [string[]]$PreserveNamePattern = @("*contact-sheet.png", "latest.png", "screen.png"),
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($MaxPngFiles -lt 1) {
    throw "MaxPngFiles must be at least 1."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

function Resolve-RepoPath {
    param([string]$Path)

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        [System.IO.Path]::GetFullPath($Path)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
    }

    if (-not $candidate.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to prune outside repo: $candidate"
    }

    return $candidate
}

function Test-IsPreservedPng {
    param([System.IO.FileInfo]$File)

    foreach ($pattern in $PreserveNamePattern) {
        if ($File.Name -like $pattern) {
            return $true
        }
    }

    return $false
}

function Limit-PngDirectory {
    param([string]$Directory)

    $files = @(Get-ChildItem -LiteralPath $Directory -Filter "*.png" -File -ErrorAction SilentlyContinue)
    if ($files.Count -le $MaxPngFiles) {
        return @()
    }

    $ranked = $files |
        Sort-Object `
            @{ Expression = { Test-IsPreservedPng -File $_ }; Descending = $true },
            @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true },
            @{ Expression = { $_.Name }; Descending = $false }

    $remove = @($ranked | Select-Object -Skip $MaxPngFiles)
    foreach ($file in $remove) {
        if (-not $WhatIf) {
            Remove-Item -LiteralPath $file.FullName -Force
        }
    }

    return $remove
}

$results = @()
foreach ($root in $Roots) {
    $fullRoot = Resolve-RepoPath -Path $root
    if (-not (Test-Path -LiteralPath $fullRoot)) {
        continue
    }

    $directories = @(Get-ChildItem -LiteralPath $fullRoot -Directory -Recurse -Force | ForEach-Object { $_.FullName })
    $directories = @($fullRoot) + $directories
    foreach ($directory in $directories) {
        $removed = @(Limit-PngDirectory -Directory $directory)
        if ($removed.Count -gt 0) {
            $results += [PSCustomObject]@{
                directory = (Resolve-Path -LiteralPath $directory -Relative)
                removedCount = $removed.Count
                maxPngFiles = $MaxPngFiles
                whatIf = [bool]$WhatIf
            }
        }
    }
}

$totalRemoved = 0
foreach ($result in @($results)) {
    $totalRemoved += $result.removedCount
}

[PSCustomObject]@{
    maxPngFiles = $MaxPngFiles
    roots = $Roots
    totalRemoved = $totalRemoved
    directories = @($results)
} | ConvertTo-Json -Depth 5
