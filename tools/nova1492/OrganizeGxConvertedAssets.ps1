param(
    [string]$ManifestPath = "artifacts/nova1492/gx_conversion_manifest.csv",
    [string]$ConvertedRoot = "Assets/Art/Nova1492/GXConverted"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-SafeName {
    param([string]$Text)
    $safe = [Regex]::Replace($Text, "[^A-Za-z0-9_.-]+", "_").Trim("_")
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return "gx_asset"
    }

    return $safe
}

function Get-GxCategory {
    param([string]$RelativePath)

    $name = [System.IO.Path]::GetFileNameWithoutExtension($RelativePath)
    $lower = $name.ToLowerInvariant()

    if ($lower -match "^(mob|mop|king|gmb|gmc|mb\d|mbnew)" -or $lower -match "mob|king") {
        return "Characters/MobAndBoss"
    }

    if ($lower -match "(^arm\d|(^|_)arm\d|armtower|_larm|_rarm|larm|rarm|_top|_front|_rback|_lback|ss0_arm|포탑|팔형|어깨|타워)") {
        return "UnitParts/ArmWeapons"
    }

    if ($lower -match "(body|ss0_body|n_body)") {
        return "UnitParts/Bodies"
    }

    if ($lower -match "(legs|ss0_legs|leg|다리)") {
        return "UnitParts/Legs"
    }

    if ($lower -match "(^base|_base|ss0_base|^b\d+|badak|allinone|^all$|기지)") {
        return "UnitParts/Bases"
    }

    if ($lower -match "(^acp|acpunique|^aqr$|^ari$|^amr|^ap\d|rune|access|unique|bigpack|cap|gem|leo|lib|psc|qumax|tau|trmax|vir|booster|부스터)") {
        return "UnitParts/Accessories"
    }

    if ($lower -match "(missile|missil|mobmis|mis\d|rocket|bullet|laser|shot|proj|bazuka|barrel|siegem|shootingm|canisterm|공중미사일)") {
        return "Effects/Projectiles"
    }

    if ($lower -match "(dam|explode|expl|exp|eff|effect|aura|storm|curse|fire|area|up|down|poison|star|guard|angel|power|speed|blaze|bomb|burn|ice|smoke|flame|heal|attack|defense|defence|attup|deup|srup|acup|barrior|barrier|death|despera|devil|doom|dual|freeze|shield|shadow|stop|warm|blind|sacrify|wave|shock|파도|분화구|날아감)") {
        return "Effects/CombatEffects"
    }

    if ($lower -match "(tree|나무|dol|gil|soccer|xmas|parasol|tube|net|기계|장식|grass|rock|ramp|wall|gate|terrain|ground)") {
        return "Environment/Props"
    }

    if ($lower -match "(^item|key|num|deck|skill|spanner|pendant|logo|reinforce|scan|spread|^sp$|^sp_|hp\d|watt\d|pw\d|lb|eb|gb|bs|bu\d|guild|recycle)") {
        return "ItemsAndUi/Icons"
    }

    return "Unknown/Review"
}

function Get-DepthRelativeToModels {
    param([string]$Category)
    return ($Category -split "[/\\]").Count
}

function Resolve-ExistingModelPath {
    param(
        [string]$ModelRoot,
        [string]$OriginalObjPath
    )

    if (Test-Path -LiteralPath $OriginalObjPath) {
        return (Resolve-Path -LiteralPath $OriginalObjPath).Path
    }

    $name = [System.IO.Path]::GetFileName($OriginalObjPath)
    $matches = @(Get-ChildItem -LiteralPath $ModelRoot -Recurse -File -Filter $name -ErrorAction SilentlyContinue)
    if ($matches.Count -gt 0) {
        return $matches[0].FullName
    }

    return $null
}

function Move-WithMeta {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        return
    }

    New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($DestinationPath)) | Out-Null
    if ((Resolve-Path -LiteralPath $SourcePath).Path -ne $DestinationPath) {
        Move-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
    }

    $sourceMeta = "$SourcePath.meta"
    $destinationMeta = "$DestinationPath.meta"
    if (Test-Path -LiteralPath $sourceMeta) {
        Move-Item -LiteralPath $sourceMeta -Destination $destinationMeta -Force
    }
}

$repoRoot = (Resolve-Path -LiteralPath ".").Path
$convertedRootFull = (Resolve-Path -LiteralPath $ConvertedRoot).Path
if (-not $convertedRootFull.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Converted root is outside repo: $convertedRootFull"
}

$modelRoot = Join-Path $convertedRootFull "Models"
$textureRoot = Join-Path $convertedRootFull "Textures"
$rows = Import-Csv -LiteralPath $ManifestPath
$converted = @($rows | Where-Object { $_.status -eq "converted" })
$classificationRows = New-Object System.Collections.Generic.List[object]

foreach ($row in $converted) {
    $category = Get-GxCategory -RelativePath $row.source_relative_path
    $sourceObj = Resolve-ExistingModelPath -ModelRoot $modelRoot -OriginalObjPath $row.obj_path
    if ($null -eq $sourceObj) {
        $classificationRows.Add([pscustomobject]@{
            category = $category
            source_relative_path = $row.source_relative_path
            model_path = ""
            vertices = $row.vertices
            triangles = $row.triangles
            texture_output = $row.texture_output
            status = "missing_obj"
        })
        continue
    }

    $objName = [System.IO.Path]::GetFileName($sourceObj)
    $mtlName = [System.IO.Path]::GetFileNameWithoutExtension($sourceObj) + ".mtl"
    $sourceMtl = Join-Path ([System.IO.Path]::GetDirectoryName($sourceObj)) $mtlName

    $categoryDir = Join-Path $modelRoot ($category -replace "/", [System.IO.Path]::DirectorySeparatorChar)
    $destObj = Join-Path $categoryDir $objName
    $destMtl = Join-Path $categoryDir $mtlName

    Move-WithMeta -SourcePath $sourceObj -DestinationPath $destObj
    Move-WithMeta -SourcePath $sourceMtl -DestinationPath $destMtl

    if (Test-Path -LiteralPath $destMtl) {
        $depth = Get-DepthRelativeToModels -Category $category
        $prefixParts = @()
        for ($i = 0; $i -lt $depth + 1; $i++) {
            $prefixParts += ".."
        }

        $texturePrefix = ($prefixParts -join "/") + "/Textures/"
        $mtlText = Get-Content -LiteralPath $destMtl -Raw
        $mtlText = [Regex]::Replace($mtlText, "map_Kd\s+.+/Textures/", "map_Kd $texturePrefix")
        Set-Content -LiteralPath $destMtl -Value $mtlText -NoNewline -Encoding UTF8
    }

    $destObjFull = (Resolve-Path -LiteralPath $destObj).Path
    $modelPath = $destObjFull.Substring($repoRoot.Length).TrimStart("\", "/").Replace("\", "/")
    $classificationRows.Add([pscustomobject]@{
        category = $category
        source_relative_path = $row.source_relative_path
        model_path = $modelPath
        vertices = $row.vertices
        triangles = $row.triangles
        texture_output = $row.texture_output
        status = "organized"
    })
}

$classificationPath = "artifacts/nova1492/gx_asset_classification.csv"
$classificationRows | Sort-Object category, source_relative_path | Export-Csv -LiteralPath $classificationPath -NoTypeInformation -Encoding UTF8

$summaryPath = "artifacts/nova1492/gx_asset_classification_summary.md"
$grouped = $classificationRows | Group-Object category | Sort-Object Name
$textureCount = @(Get-ChildItem -LiteralPath $textureRoot -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -ne ".meta" }).Count

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine("# Nova1492 GX Asset Classification Summary")
[void]$builder.AppendLine()
[void]$builder.AppendLine(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date)))
[void]$builder.AppendLine()
[void]$builder.AppendLine('- source manifest: `artifacts/nova1492/gx_conversion_manifest.csv`')
[void]$builder.AppendLine('- classification manifest: `artifacts/nova1492/gx_asset_classification.csv`')
[void]$builder.AppendLine('- organized model root: `Assets/Art/Nova1492/GXConverted/Models/`')
[void]$builder.AppendLine('- texture root: `Assets/Art/Nova1492/GXConverted/Textures/`')
[void]$builder.AppendLine(("- converted assets classified: {0}" -f $classificationRows.Count))
[void]$builder.AppendLine(("- copied textures kept flat: {0}" -f $textureCount))
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Category Counts")
[void]$builder.AppendLine()
[void]$builder.AppendLine("| category | count |")
[void]$builder.AppendLine("|---|---:|")
foreach ($group in $grouped) {
    [void]$builder.AppendLine(("| `{0}` | {1} |" -f $group.Name, $group.Count))
}

[void]$builder.AppendLine()
[void]$builder.AppendLine("## Category Intent")
[void]$builder.AppendLine()
[void]$builder.AppendLine("| category | intent |")
[void]$builder.AppendLine("|---|---|")
[void]$builder.AppendLine("| `Characters/MobAndBoss` | Monster, boss, and mob-specific model pieces. |")
[void]$builder.AppendLine("| `Effects/CombatEffects` | Buff, damage, explosion, area, aura, and impact visuals. |")
[void]$builder.AppendLine("| `Effects/Projectiles` | Missiles, shots, and projectile-like meshes. |")
[void]$builder.AppendLine("| `Environment/Props` | Trees, decorations, terrain props, event props, and set dressing. |")
[void]$builder.AppendLine("| `ItemsAndUi/Icons` | Converted item, skill, deck, stat, and interface-icon meshes. |")
[void]$builder.AppendLine("| `UnitParts/Accessories` | Accessory, upgrade, rune, and special module-style parts. |")
[void]$builder.AppendLine("| `UnitParts/ArmWeapons` | Arm, weapon, turret-side, and mounted weapon parts. |")
[void]$builder.AppendLine("| `UnitParts/Bases` | Base, platform, all-in-one, and lower chassis candidates. |")
[void]$builder.AppendLine("| `UnitParts/Bodies` | Body and torso candidates. |")
[void]$builder.AppendLine("| `UnitParts/Legs` | Leg and locomotion candidates. |")
[void]$builder.AppendLine("| `Unknown/Review` | Converted assets whose filenames do not carry enough category signal. |")

Set-Content -LiteralPath $summaryPath -Value $builder.ToString() -Encoding UTF8

Write-Host "Classified $($classificationRows.Count) converted GX assets."
Write-Host "Classification: $classificationPath"
Write-Host "Summary: $summaryPath"
