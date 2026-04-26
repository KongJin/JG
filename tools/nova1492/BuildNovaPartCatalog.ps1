param(
    [string] $ClassificationPath = "artifacts/nova1492/gx_asset_classification.csv",
    [string] $OutputCsvPath = "artifacts/nova1492/nova_part_catalog.csv",
    [string] $OutputMarkdownPath = "artifacts/nova1492/nova_part_catalog_summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ClassificationPath)) {
    throw "Classification CSV not found: $ClassificationPath"
}

function Get-Slot([string] $category) {
    switch ($category) {
        "UnitParts/Bodies" { return "Frame" }
        "UnitParts/Bases" { return "Frame" }
        "UnitParts/ArmWeapons" { return "Firepower" }
        "UnitParts/Legs" { return "Mobility" }
        default { return $null }
    }
}

function Get-Prefix([string] $slot) {
    switch ($slot) {
        "Frame" { return "nova_frame" }
        "Firepower" { return "nova_fire" }
        "Mobility" { return "nova_mob" }
        default { throw "Unknown slot: $slot" }
    }
}

function Get-ShortHash([string] $value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = $sha.ComputeHash($bytes)
        return -join ($hash[0..3] | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $sha.Dispose()
    }
}

function ConvertTo-AsciiToken([string] $value) {
    $lower = $value.ToLowerInvariant()
    $token = [regex]::Replace($lower, "[^a-z0-9]+", "_").Trim("_")
    $token = [regex]::Replace($token, "_+", "_")
    if ([string]::IsNullOrWhiteSpace($token)) {
        return "review"
    }

    return $token
}

function ConvertTo-DisplayName([string] $stem) {
    $display = $stem -replace "[-_]+", " "
    $display = [regex]::Replace($display, "\s+", " ").Trim()
    if ([string]::IsNullOrWhiteSpace($display)) {
        return "Review Part"
    }

    return (Get-Culture).TextInfo.ToTitleCase($display.ToLowerInvariant())
}

function Get-Tier([int] $rank, [int] $count) {
    if ($count -le 1) {
        return 1
    }

    $tier = [int][Math]::Floor(($rank * 5.0) / $count) + 1
    if ($tier -lt 1) { return 1 }
    if ($tier -gt 5) { return 5 }
    return $tier
}

function Get-Stats([string] $slot, [int] $tier) {
    switch ($slot) {
        "Frame" {
            return @{
                baseHp = 420 + ($tier * 45)
                baseAttackSpeed = 1.25 - ($tier * 0.05)
                baseMoveRange = 4
                attackDamage = ""
                attackSpeed = ""
                range = ""
                hpBonus = ""
                moveRange = ""
                anchorRange = ""
            }
        }
        "Firepower" {
            return @{
                baseHp = ""
                baseAttackSpeed = ""
                baseMoveRange = ""
                attackDamage = 16 + ($tier * 8)
                attackSpeed = 1.45 - ($tier * 0.13)
                range = 4.0 + ($tier * 0.75)
                hpBonus = ""
                moveRange = ""
                anchorRange = ""
            }
        }
        "Mobility" {
            $moveRange = 6.2 - ($tier * 0.55)
            return @{
                baseHp = ""
                baseAttackSpeed = ""
                baseMoveRange = ""
                attackDamage = ""
                attackSpeed = ""
                range = ""
                hpBonus = 80 + ($tier * 50)
                moveRange = $moveRange
                anchorRange = $moveRange
            }
        }
        default {
            throw "Unknown slot: $slot"
        }
    }
}

$rows = Import-Csv -LiteralPath $ClassificationPath
$candidates = foreach ($row in $rows) {
    $slot = Get-Slot $row.category
    if ($null -eq $slot) {
        continue
    }

    $triangles = [int]$row.triangles
    $vertices = [int]$row.vertices
    $sourcePath = $row.source_relative_path
    $modelPath = $row.model_path
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($sourcePath.Replace("\", "/"))
    $needsNameReview = $stem -notmatch "^[A-Za-z0-9._-]+$"

    [pscustomobject]@{
        slot = $slot
        category = $row.category
        source_relative_path = $sourcePath
        model_path = $modelPath
        vertices = $vertices
        triangles = $triangles
        sourceStem = $stem
        needsNameReview = $needsNameReview
    }
}

$candidates = @($candidates)
$idCounts = @{}
$output = New-Object System.Collections.Generic.List[object]

foreach ($slotGroup in ($candidates | Group-Object slot)) {
    $ordered = @($slotGroup.Group | Sort-Object triangles, source_relative_path)
    for ($i = 0; $i -lt $ordered.Count; $i++) {
        $item = $ordered[$i]
        $tier = Get-Tier -rank $i -count $ordered.Count
        $stats = Get-Stats -slot $item.slot -tier $tier
        $prefix = Get-Prefix $item.slot
        $token = ConvertTo-AsciiToken $item.sourceStem
        $partId = "$prefix`_$token"
        $needsNameReview = [bool]$item.needsNameReview

        if ($idCounts.ContainsKey($partId)) {
            $idCounts[$partId] += 1
            $partId = "$partId`_$(Get-ShortHash $item.source_relative_path)"
            $needsNameReview = $true
        }
        else {
            $idCounts[$partId] = 1
        }

        $displayName = ConvertTo-DisplayName $item.sourceStem
        if ($needsNameReview) {
            $displayName = "$($item.slot) $(Get-ShortHash $item.source_relative_path)"
        }

        $modelExists = Test-Path -LiteralPath $item.model_path

        $output.Add([pscustomobject]@{
            partId = $partId
            slot = $item.slot
            category = $item.category
            source_relative_path = $item.source_relative_path
            model_path = $item.model_path
            vertices = $item.vertices
            triangles = $item.triangles
            tier = $tier
            displayName = $displayName
            needsNameReview = $needsNameReview
            modelExists = $modelExists
            playableStatus = if ($modelExists) { "generated" } else { "blocked_missing_model" }
            baseHp = $stats.baseHp
            baseAttackSpeed = $stats.baseAttackSpeed
            baseMoveRange = $stats.baseMoveRange
            attackDamage = $stats.attackDamage
            attackSpeed = $stats.attackSpeed
            range = $stats.range
            hpBonus = $stats.hpBonus
            moveRange = $stats.moveRange
            anchorRange = $stats.anchorRange
        }) | Out-Null
    }
}

$outputCsvDirectory = Split-Path -Parent $OutputCsvPath
$outputMarkdownDirectory = Split-Path -Parent $OutputMarkdownPath
if ($outputCsvDirectory -and -not (Test-Path -LiteralPath $outputCsvDirectory)) {
    New-Item -ItemType Directory -Path $outputCsvDirectory | Out-Null
}
if ($outputMarkdownDirectory -and -not (Test-Path -LiteralPath $outputMarkdownDirectory)) {
    New-Item -ItemType Directory -Path $outputMarkdownDirectory | Out-Null
}

$output | Sort-Object slot, category, partId | Export-Csv -LiteralPath $OutputCsvPath -NoTypeInformation -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Nova1492 Part Catalog Summary")
$lines.Add("")
$lines.Add('> Generated by `tools/nova1492/BuildNovaPartCatalog.ps1` from `artifacts/nova1492/gx_asset_classification.csv`.')
$lines.Add("")
$lines.Add("## Counts")
$lines.Add("")
$lines.Add("| slot | count | missing models | name review | triangle range |")
$lines.Add("|---|---:|---:|---:|---|")
foreach ($group in ($output | Group-Object slot | Sort-Object Name)) {
    $triangles = @($group.Group | ForEach-Object { [int]$_.triangles })
    $min = ($triangles | Measure-Object -Minimum).Minimum
    $max = ($triangles | Measure-Object -Maximum).Maximum
    $missing = @($group.Group | Where-Object { -not $_.modelExists }).Count
    $review = @($group.Group | Where-Object { $_.needsNameReview }).Count
    $lines.Add("| $($group.Name) | $($group.Count) | $missing | $review | $min-$max |")
}

$lines.Add("")
$lines.Add("## Category Mapping")
$lines.Add("")
$lines.Add("| category | slot | count |")
$lines.Add("|---|---|---:|")
foreach ($group in ($output | Group-Object category | Sort-Object Name)) {
    $slot = ($group.Group | Select-Object -First 1).slot
    $lines.Add("| $($group.Name) | $slot | $($group.Count) |")
}

$lines.Add("")
$lines.Add("## Generated Stat Policy")
$lines.Add("")
$lines.Add('- Tier is generated per slot from triangle-count quantiles, `1..5`.')
$lines.Add("- Generated stats are a playable smoke baseline, not final balance.")
$lines.Add('- `needsNameReview=true` rows keep source-derived IDs but require later naming review before release.')

Set-Content -LiteralPath $OutputMarkdownPath -Value $lines -Encoding UTF8

$expectedCount = 321
if ($output.Count -ne $expectedCount) {
    throw "Unexpected Core catalog row count: expected $expectedCount, got $($output.Count)"
}

$duplicateIds = @($output | Group-Object partId | Where-Object { $_.Count -gt 1 })
if ($duplicateIds.Count -gt 0) {
    throw "Duplicate partId detected: $($duplicateIds[0].Name)"
}

$missingModels = @($output | Where-Object { -not $_.modelExists })
if ($missingModels.Count -gt 0) {
    throw "Missing model paths detected: $($missingModels.Count)"
}

Write-Host "Wrote $OutputCsvPath"
Write-Host "Wrote $OutputMarkdownPath"
Write-Host "Core catalog rows: $($output.Count)"
