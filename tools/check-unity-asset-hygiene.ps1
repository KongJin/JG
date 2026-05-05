param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$SkipResourcesAllowlist
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$assetsRoot = Join-Path $RepoRoot "Assets"
$issues = New-Object System.Collections.Generic.List[string]
$excludedAssetPathPrefixes = @(
    "Assets/FromStore/"
)

function ConvertTo-RepoPath {
    param([string]$Path)

    $root = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd("\", "/")
    $fullPath = [System.IO.Path]::GetFullPath($Path)

    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $fullPath.Substring($root.Length).TrimStart("\", "/")
    }
    else {
        $relative = $fullPath
    }

    return $relative -replace "\\", "/"
}

function Test-IsExcludedAssetPath {
    param([string]$Path)

    $repoPath = (ConvertTo-RepoPath $Path).Replace("\", "/")
    foreach ($prefix in @($excludedAssetPathPrefixes)) {
        if ($repoPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

if (-not (Test-Path -LiteralPath $assetsRoot)) {
    throw "Assets folder not found: $assetsRoot"
}

$guidToPaths = @{}
$metaFiles = @(
    Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Filter "*.meta" |
        Where-Object { -not (Test-IsExcludedAssetPath -Path $_.FullName) }
)

foreach ($metaFile in $metaFiles) {
    $text = Get-Content -LiteralPath $metaFile.FullName -Raw
    $matches = [regex]::Matches($text, "(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
    $repoPath = ConvertTo-RepoPath $metaFile.FullName

    if ($matches.Count -ne 1) {
        $issues.Add("Expected exactly one guid in $repoPath, found $($matches.Count).")
        continue
    }

    $guid = $matches[0].Groups[1].Value.ToLowerInvariant()
    if (-not $guidToPaths.ContainsKey($guid)) {
        $guidToPaths[$guid] = New-Object System.Collections.Generic.List[string]
    }

    $guidToPaths[$guid].Add($repoPath)
}

foreach ($entry in $guidToPaths.GetEnumerator()) {
    if ($entry.Value.Count -gt 1) {
        $issues.Add("Duplicate Unity guid $($entry.Key): $($entry.Value -join ', ')")
    }
}

$assetFiles = @(
    Get-ChildItem -LiteralPath $assetsRoot -Recurse -File |
        Where-Object { $_.Name -notlike "*.meta" -and -not (Test-IsExcludedAssetPath -Path $_.FullName) }
)

foreach ($assetFile in $assetFiles) {
    $metaPath = "$($assetFile.FullName).meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        $issues.Add("Missing meta file for $(ConvertTo-RepoPath $assetFile.FullName).")
    }
}

$assetDirectories = @(
    Get-ChildItem -LiteralPath $assetsRoot -Recurse -Directory |
        Where-Object { -not (Test-IsExcludedAssetPath -Path $_.FullName) }
)
foreach ($assetDirectory in $assetDirectories) {
    $metaPath = "$($assetDirectory.FullName).meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        $issues.Add("Missing meta file for directory $(ConvertTo-RepoPath $assetDirectory.FullName).")
    }
}

if (-not $SkipResourcesAllowlist) {
    $resourcesRoot = Join-Path $assetsRoot "Resources"
    $allowedResources = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    @(
        "Assets/Resources/BattleEntity.prefab",
        "Assets/Resources/DOTweenSettings.asset",
        "Assets/Resources/Enemy/BasicEnemy.asset",
        "Assets/Resources/Enemy/CoreHunterEnemy.asset",
        "Assets/Resources/Enemy/CoreSiegeEnemy.asset",
        "Assets/Resources/Enemy/FastEnemy.asset",
        "Assets/Resources/EnemyCharacter.prefab",
        "Assets/Resources/EnemyCharacterCore.prefab",
        "Assets/Resources/PlayerCharacter.prefab",
        "Assets/Resources/ProjectilePhysicsAdapter.prefab",
        "Assets/Resources/RoundedRectMaterial.mat",
        # Legacy audio Resources assets remain until the audio pool/template migration moves or deletes them.
        # Runtime host creation now uses Lobby carry-over or a scene-owned SoundPlayer contract.
        "Assets/Resources/Shared/Sound/SoundPlayer.prefab",
        "Assets/Resources/Shared/Sound/SoundPlayerRuntimeConfig.asset",
        "Assets/Resources/Wave/DefaultWaveTable.asset",
        "Assets/Resources/ZoneEffect.prefab"
    ) | ForEach-Object { [void]$allowedResources.Add($_) }

    if (Test-Path -LiteralPath $resourcesRoot) {
        $resourcesFiles = Get-ChildItem -LiteralPath $resourcesRoot -Recurse -File |
            Where-Object { $_.Name -notlike "*.meta" }

        foreach ($resourcesFile in $resourcesFiles) {
            $repoPath = ConvertTo-RepoPath $resourcesFile.FullName
            if (-not $allowedResources.Contains($repoPath)) {
                $issues.Add("Unexpected Assets/Resources asset: $repoPath. Prefer serialized scene/prefab references or update the allowlist with a clear reason.")
            }
        }
    }
}

if ($issues.Count -gt 0) {
    Write-Host "Unity asset hygiene check failed:"
    foreach ($issue in $issues) {
        Write-Host " - $issue"
    }
    exit 1
}

Write-Host "Unity asset hygiene check passed. metaFiles=$($metaFiles.Count), assetFiles=$($assetFiles.Count)"
