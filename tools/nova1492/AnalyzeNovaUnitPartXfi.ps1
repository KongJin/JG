param(
    [string] $SourceRoot = "External/Nova1492Raw",
    [string] $ClassificationPath = "artifacts/nova1492/gx_asset_classification.csv",
    [string] $PartCatalogPath = "artifacts/nova1492/nova_part_catalog.csv",
    [string] $OutputCsvPath = "artifacts/nova1492/nova_unitpart_xfi_manifest.csv",
    [string] $OutputMarkdownPath = "artifacts/nova1492/nova_unitpart_xfi_report.md",
    [string] $PartDescriptionPath = "External/Nova1492Raw/datan/kr/nvpartdesc.dat"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-Columns {
    param([string] $Line)

    return @(
        $Line -split "," |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Test-IntegerLine {
    param([string] $Line)

    $columns = @(ConvertTo-Columns $Line)
    if ($columns.Count -ne 1) {
        return $false
    }

    $ignored = 0
    return [int]::TryParse($columns[0], [ref] $ignored)
}

function Test-Float4Line {
    param([string] $Line)

    $columns = @(ConvertTo-Columns $Line)
    if ($columns.Count -ne 4) {
        return $false
    }

    foreach ($column in $columns) {
        $ignored = 0.0
        if (-not [double]::TryParse($column, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref] $ignored)) {
            return $false
        }
    }

    return $true
}

function ConvertTo-Vector3Text {
    param([string[]] $MatrixRows)

    if ($MatrixRows.Count -lt 4) {
        return ""
    }

    $columns = @(ConvertTo-Columns $MatrixRows[3])
    if ($columns.Count -lt 3) {
        return ""
    }

    return ("{0};{1};{2}" -f $columns[0], $columns[1], $columns[2])
}

function Read-Xfi {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    $rawLines = Get-Content -LiteralPath $Path
    $lines = @(
        $rawLines |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )

    if ($lines.Count -eq 0) {
        return [pscustomobject]@{
            header = ""
            headerKind = "empty"
            attachSlot = ""
            attachVariant = ""
            transformCount = 0
            transformTranslations = ""
            directionRangeCount = 0
            directionRanges = ""
            parseStatus = "empty"
            rawLineCount = $rawLines.Count
        }
    }

    $headerColumns = @(ConvertTo-Columns $lines[0])
    $header = ($headerColumns -join "|")
    $attachSlot = ""
    $attachVariant = ""
    $headerKind = "numeric_slot_code"
    if ($headerColumns.Count -gt 0) {
        $ignored = 0
        if (-not [int]::TryParse($headerColumns[0], [ref] $ignored)) {
            $headerKind = "named_attach_slot"
            $attachSlot = $headerColumns[0]
            if ($headerColumns.Count -gt 1) {
                $attachVariant = ($headerColumns[1..($headerColumns.Count - 1)] -join "|")
            }
        }
    }

    $cursor = 1
    $matrixRows = New-Object System.Collections.Generic.List[string]
    $directionRows = New-Object System.Collections.Generic.List[string]
    $parseStatus = "parsed"

    if ($headerKind -eq "named_attach_slot" -and $cursor -lt $lines.Count -and (Test-IntegerLine $lines[$cursor])) {
        $declaredCount = [int](@(ConvertTo-Columns $lines[$cursor]))[0]
        $lookahead = $cursor + 1
        if ($declaredCount -gt 0 -and $lookahead -lt $lines.Count -and (Test-Float4Line $lines[$lookahead])) {
            $cursor++
            for ($i = 0; $i -lt $declaredCount * 4 -and $cursor -lt $lines.Count; $i++) {
                if (-not (Test-Float4Line $lines[$cursor])) {
                    $parseStatus = "partial_matrix"
                    break
                }

                $matrixRows.Add($lines[$cursor])
                $cursor++
            }
        }
        else {
            $cursor++
            for ($i = 0; $i -lt $declaredCount -and $cursor -lt $lines.Count; $i++) {
                $directionRows.Add($lines[$cursor])
                $cursor++
            }
        }
    }
    else {
        while ($cursor + 3 -lt $lines.Count) {
            if (-not (Test-Float4Line $lines[$cursor]) -or
                -not (Test-Float4Line $lines[$cursor + 1]) -or
                -not (Test-Float4Line $lines[$cursor + 2]) -or
                -not (Test-Float4Line $lines[$cursor + 3])) {
                break
            }

            for ($i = 0; $i -lt 4; $i++) {
                $matrixRows.Add($lines[$cursor + $i])
            }

            $cursor += 4
        }
    }

    if ($directionRows.Count -eq 0 -and $cursor -lt $lines.Count -and (Test-IntegerLine $lines[$cursor])) {
        $directionCount = [int](@(ConvertTo-Columns $lines[$cursor]))[0]
        $cursor++
        for ($i = 0; $i -lt $directionCount -and $cursor -lt $lines.Count; $i++) {
            $directionRows.Add($lines[$cursor])
            $cursor++
        }
    }

    $translations = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i + 3 -lt $matrixRows.Count; $i += 4) {
        $translations.Add((ConvertTo-Vector3Text -MatrixRows @($matrixRows[$i], $matrixRows[$i + 1], $matrixRows[$i + 2], $matrixRows[$i + 3])))
    }

    return [pscustomobject]@{
        header = $header
        headerKind = $headerKind
        attachSlot = $attachSlot
        attachVariant = $attachVariant
        transformCount = [int]($matrixRows.Count / 4)
        transformTranslations = ($translations -join "|")
        directionRangeCount = $directionRows.Count
        directionRanges = (($directionRows | ForEach-Object { (@(ConvertTo-Columns $_)) -join ":" }) -join "|")
        parseStatus = $parseStatus
        rawLineCount = $rawLines.Count
    }
}

function Get-PartSlot {
    param([string] $Category)

    switch ($Category) {
        "UnitParts/Bodies" { return "Frame" }
        "UnitParts/Bases" { return "Frame" }
        "UnitParts/ArmWeapons" { return "Firepower" }
        "UnitParts/Legs" { return "Mobility" }
        "UnitParts/Accessories" { return "Accessory" }
        default { return "" }
    }
}

function Get-InferredPartCode {
    param(
        [string] $Stem,
        [string] $Category
    )

    $normalized = $Stem -replace "^(g_|n_|s_|ss0_)", ""
    $match = [regex]::Match($normalized, "^(legs|body|arm)(\d+)")
    if (-not $match.Success) {
        return ""
    }

    $number = [int]$match.Groups[2].Value
    switch ($match.Groups[1].Value) {
        "legs" { return (1000 + $number).ToString([Globalization.CultureInfo]::InvariantCulture) }
        "body" { return (2000 + $number).ToString([Globalization.CultureInfo]::InvariantCulture) }
        "arm" { return (3000 + $number).ToString([Globalization.CultureInfo]::InvariantCulture) }
        default { return "" }
    }
}

function Read-PartDescriptions {
    param([string] $Path)

    $byCode = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $byCode
    }

    $encoding = [Text.Encoding]::GetEncoding(949)
    foreach ($line in [IO.File]::ReadAllLines($Path, $encoding)) {
        if ($line.TrimStart().StartsWith("//")) {
            continue
        }

        $columns = $line -split "`t"
        if ($columns.Count -lt 3) {
            continue
        }

        $code = $columns[0].Trim()
        $variant = $columns[1].Trim()
        $name = $columns[2].Trim()
        $description = if ($columns.Count -gt 3) { ($columns[3..($columns.Count - 1)] -join "`t").Trim() } else { "" }
        if ($code -notmatch "^\d+$" -or [string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        if (-not $byCode.ContainsKey($code) -or $variant -eq "0") {
            $byCode[$code] = [pscustomobject]@{
                code = $code
                variant = $variant
                name = $name
                description = $description
            }
        }
    }

    return $byCode
}

if (-not (Test-Path -LiteralPath $ClassificationPath)) {
    throw "Classification CSV not found: $ClassificationPath"
}

$sourceRootFull = (Resolve-Path -LiteralPath $SourceRoot).Path
$classificationRows = Import-Csv -LiteralPath $ClassificationPath
$catalogBySource = @{}
if (Test-Path -LiteralPath $PartCatalogPath) {
    foreach ($row in (Import-Csv -LiteralPath $PartCatalogPath)) {
        $catalogBySource[$row.source_relative_path.ToLowerInvariant()] = $row
    }
}

$partDescriptions = Read-PartDescriptions -Path $PartDescriptionPath
$unitCategories = @(
    "UnitParts/Accessories",
    "UnitParts/ArmWeapons",
    "UnitParts/Bases",
    "UnitParts/Bodies",
    "UnitParts/Legs"
)

$output = New-Object System.Collections.Generic.List[object]
foreach ($row in $classificationRows) {
    if ($unitCategories -notcontains $row.category) {
        continue
    }

    $relative = $row.source_relative_path
    $relativeForPath = $relative -replace "\\", [IO.Path]::DirectorySeparatorChar
    $sourcePath = Join-Path $sourceRootFull $relativeForPath
    $stem = [IO.Path]::GetFileNameWithoutExtension($relative)
    $directory = [IO.Path]::GetDirectoryName($sourcePath)
    $xfiPath = Join-Path $directory ($stem + ".xfi")
    if (-not (Test-Path -LiteralPath $xfiPath)) {
        $xfiPath = Join-Path $directory ($stem + ".XFI")
    }

    $xfi = Read-Xfi -Path $xfiPath
    $catalog = $null
    $catalogKey = $relative.ToLowerInvariant()
    if ($catalogBySource.ContainsKey($catalogKey)) {
        $catalog = $catalogBySource[$catalogKey]
    }

    $inferredCode = Get-InferredPartCode -Stem $stem -Category $row.category
    $code = $inferredCode
    if ($catalog -and -not [string]::IsNullOrWhiteSpace($catalog.originalCode)) {
        $code = $catalog.originalCode
    }

    $partName = if ($catalog -and -not [string]::IsNullOrWhiteSpace($catalog.originalName)) { $catalog.originalName } else { "" }
    $partDescription = ""
    if (-not [string]::IsNullOrWhiteSpace($code) -and $partDescriptions.ContainsKey($code)) {
        if ([string]::IsNullOrWhiteSpace($partName)) {
            $partName = $partDescriptions[$code].name
        }

        $partDescription = $partDescriptions[$code].description
    }

    $output.Add([pscustomobject]@{
        slot = Get-PartSlot -Category $row.category
        category = $row.category
        partId = if ($catalog) { $catalog.partId } else { "" }
        displayName = if ($catalog) { $catalog.displayName } else { "" }
        inferredOriginalCode = $code
        originalNameKr = $partName
        originalDescriptionKr = $partDescription
        source_relative_path = $relative
        model_path = $row.model_path
        texture_output = $row.texture_output
        vertices = $row.vertices
        triangles = $row.triangles
        xfi_path = if ($xfi -ne $null) { $xfiPath.Substring($sourceRootFull.Length).TrimStart("\", "/").Replace("\", "/") } else { "" }
        xfi_header = if ($xfi -ne $null) { $xfi.header } else { "" }
        xfi_header_kind = if ($xfi -ne $null) { $xfi.headerKind } else { "missing" }
        attachSlot = if ($xfi -ne $null) { $xfi.attachSlot } else { "" }
        attachVariant = if ($xfi -ne $null) { $xfi.attachVariant } else { "" }
        transformCount = if ($xfi -ne $null) { $xfi.transformCount } else { 0 }
        transformTranslations = if ($xfi -ne $null) { $xfi.transformTranslations } else { "" }
        directionRangeCount = if ($xfi -ne $null) { $xfi.directionRangeCount } else { 0 }
        directionRanges = if ($xfi -ne $null) { $xfi.directionRanges } else { "" }
        parseStatus = if ($xfi -ne $null) { $xfi.parseStatus } else { "missing_xfi" }
    }) | Out-Null
}

$outputDirectory = Split-Path -Parent $OutputCsvPath
if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$output | Sort-Object category, source_relative_path | Export-Csv -LiteralPath $OutputCsvPath -NoTypeInformation -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Nova1492 UnitPart XFI Analysis")
$lines.Add("")
$lines.Add(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date)))
$lines.Add("")
$lines.Add('- source root: `External/Nova1492Raw/`')
$lines.Add('- classification: `artifacts/nova1492/gx_asset_classification.csv`')
$lines.Add('- part catalog: `artifacts/nova1492/nova_part_catalog.csv`')
$lines.Add('- manifest: `artifacts/nova1492/nova_unitpart_xfi_manifest.csv`')
$lines.Add('- source names: `External/Nova1492Raw/datan/kr/nvpartdesc.dat`')
$lines.Add("")
$lines.Add("## Coverage")
$lines.Add("")
$lines.Add("| metric | count |")
$lines.Add("|---|---:|")
$lines.Add("| UnitPart rows | $($output.Count) |")
$lines.Add("| with XFI | $(@($output | Where-Object { $_.parseStatus -ne 'missing_xfi' }).Count) |")
$lines.Add("| missing XFI | $(@($output | Where-Object { $_.parseStatus -eq 'missing_xfi' }).Count) |")
$lines.Add("| mapped Korean name by inferred code | $(@($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_.originalNameKr) }).Count) |")
$lines.Add("")
$lines.Add("## Category Counts")
$lines.Add("")
$lines.Add("| category | count | with XFI | mapped names |")
$lines.Add("|---|---:|---:|---:|")
foreach ($group in ($output | Group-Object category | Sort-Object Name)) {
    $withXfi = @($group.Group | Where-Object { $_.parseStatus -ne "missing_xfi" }).Count
    $names = @($group.Group | Where-Object { -not [string]::IsNullOrWhiteSpace($_.originalNameKr) }).Count
    $lines.Add(("| `{0}` | {1} | {2} | {3} |" -f $group.Name, $group.Count, $withXfi, $names))
}

$lines.Add("")
$lines.Add("## XFI Header Counts")
$lines.Add("")
$lines.Add("| header | count |")
$lines.Add("|---|---:|")
foreach ($group in ($output | Group-Object xfi_header | Sort-Object -Property @{ Expression = "Count"; Descending = $true }, Name)) {
    $header = if ([string]::IsNullOrWhiteSpace($group.Name)) { "(missing)" } else { $group.Name }
    $lines.Add(("| `{0}` | {1} |" -f $header, $group.Count))
}

$lines.Add("")
$lines.Add("## Interpretation")
$lines.Add("")
$lines.Add('- `.GX` contains the mesh stream; `.xfi` is text metadata used for UnitPart attachment semantics.')
$lines.Add('- Numeric XFI headers such as `0`, `1`, and `4` line up with Mobility/Frame/Firepower style parts.')
$lines.Add('- Named XFI headers such as `body`, `front`, `top`, `larm`, `rarm`, `lshd`, and `rshd` expose explicit attachment sockets.')
$lines.Add('- Matrix rows provide candidate socket transforms. The fourth row of each 4x4 matrix is captured as `transformTranslations`.')
$lines.Add('- Direction rows such as `0:90:105` are preserved as `directionRanges`; these likely represent facing/animation angle bands.')

Set-Content -LiteralPath $OutputMarkdownPath -Value $lines -Encoding UTF8

[pscustomobject]@{
    success = $true
    rows = $output.Count
    withXfi = @($output | Where-Object { $_.parseStatus -ne "missing_xfi" }).Count
    missingXfi = @($output | Where-Object { $_.parseStatus -eq "missing_xfi" }).Count
    mappedNames = @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_.originalNameKr) }).Count
    manifest = $OutputCsvPath
    report = $OutputMarkdownPath
} | ConvertTo-Json -Depth 4
