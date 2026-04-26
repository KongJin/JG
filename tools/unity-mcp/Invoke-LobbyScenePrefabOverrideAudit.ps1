param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
    [string]$ResultJsonPath = "artifacts/unity/lobby-scene-prefab-override-audit.json",
    [string]$ResultMarkdownPath = "artifacts/unity/lobby-scene-prefab-override-audit.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function ConvertTo-RepoPath {
    param([string]$Path)

    $root = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd("\", "/")
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($root.Length).TrimStart("\", "/") -replace "\\", "/")
    }

    return $fullPath -replace "\\", "/"
}

function Get-GuidToAssetPathMap {
    $map = @{}
    $assetRoot = Resolve-RepoPath "Assets"
    foreach ($metaFile in Get-ChildItem -LiteralPath $assetRoot -Recurse -File -Filter "*.meta") {
        $text = Get-Content -LiteralPath $metaFile.FullName -Raw
        $match = [regex]::Match($text, "(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
        if (-not $match.Success) {
            continue
        }

        $assetFullPath = $metaFile.FullName.Substring(0, $metaFile.FullName.Length - ".meta".Length)
        $map[$match.Groups[1].Value.ToLowerInvariant()] = ConvertTo-RepoPath $assetFullPath
    }

    return $map
}

function Get-PropertyFamily {
    param([string]$PropertyPath)

    if ($PropertyPath -match '^m_Name$') { return "name" }
    if ($PropertyPath -match '^m_IsActive$') { return "active" }
    if ($PropertyPath -match '^m_text$|^m_Text$|^m_TextMesh') { return "text" }
    if ($PropertyPath -match '^m_Color(\.|$)|m_FontColor|m_FaceColor') { return "color" }
    if ($PropertyPath -match 'Sprite|Material|Texture|FontAsset|FontSharedMaterial') { return "asset-reference" }
    if ($PropertyPath -match 'm_Anchor|m_AnchoredPosition|m_SizeDelta|m_Pivot|m_LocalPosition|m_LocalRotation|m_LocalScale|m_LocalEulerAnglesHint') { return "layout" }
    return "other"
}

function Get-Classification {
    param(
        [int]$VisualOverrideCount,
        [int]$InternalActiveOverrideCount,
        [int]$InternalLayoutTargetCount,
        [int]$UnknownOverrideCount
    )

    if ($VisualOverrideCount -gt 0 -or $UnknownOverrideCount -gt 0) {
        return "warning"
    }

    if ($InternalActiveOverrideCount -gt 0 -or $InternalLayoutTargetCount -gt 0) {
        return "review-candidate"
    }

    return "allowed-candidate"
}

function Get-Note {
    param(
        [string]$Classification,
        [int]$VisualOverrideCount,
        [int]$InternalActiveOverrideCount,
        [int]$InternalLayoutTargetCount,
        [int]$UnknownOverrideCount
    )

    if ($Classification -eq "warning") {
        return "Visual/text/color/asset-reference or unknown prefab overrides need review before acceptance. visual=$VisualOverrideCount unknown=$UnknownOverrideCount"
    }

    if ($Classification -eq "review-candidate") {
        return "Root placement/default active is allowed, but internal active/layout override candidates should be confirmed. internalActive=$InternalActiveOverrideCount internalLayoutTargets=$InternalLayoutTargetCount"
    }

    return "Only root name, placement, and default active override families were found."
}

$sceneAbsolutePath = Resolve-RepoPath $ScenePath
if (-not (Test-Path -LiteralPath $sceneAbsolutePath)) {
    throw "Scene not found: $ScenePath"
}

$guidToPath = Get-GuidToAssetPathMap
$lines = Get-Content -LiteralPath $sceneAbsolutePath
$instances = New-Object System.Collections.Generic.List[object]
$current = $null
$currentModification = $null

foreach ($line in $lines) {
    if ($line -match '^--- !u!1001 &([0-9]+)') {
        if ($null -ne $current) {
            if ($null -ne $currentModification) {
                $current.Modifications.Add($currentModification) | Out-Null
                $currentModification = $null
            }
            $instances.Add($current) | Out-Null
        }

        $current = [PSCustomObject]@{
            fileId = $matches[1]
            sourceGuid = ""
            modifications = New-Object System.Collections.Generic.List[object]
        }
        continue
    }

    if ($null -eq $current) {
        continue
    }

    if ($line -match '^\s{2}m_SourcePrefab:\s+\{fileID:\s*100100000,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}') {
        $current.sourceGuid = $matches[1].ToLowerInvariant()
        continue
    }

    if ($line -match '^\s{4}- target:\s+\{fileID:\s*([-0-9]+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3\}') {
        if ($null -ne $currentModification) {
            $current.modifications.Add($currentModification) | Out-Null
        }

        $currentModification = [PSCustomObject]@{
            targetFileId = $matches[1]
            targetGuid = $matches[2].ToLowerInvariant()
            propertyPath = ""
            value = ""
            objectReference = ""
            family = ""
        }
        continue
    }

    if ($null -ne $currentModification -and $line -match '^\s{6}propertyPath:\s*(.*)$') {
        $currentModification.propertyPath = $matches[1].Trim()
        $currentModification.family = Get-PropertyFamily -PropertyPath $currentModification.propertyPath
        continue
    }

    if ($null -ne $currentModification -and $line -match '^\s{6}value:\s*(.*)$') {
        $currentModification.value = $matches[1].Trim()
        continue
    }

    if ($null -ne $currentModification -and $line -match '^\s{6}objectReference:\s*(.*)$') {
        $currentModification.objectReference = $matches[1].Trim()
        continue
    }

    if ($line -match '^--- !u!') {
        if ($null -ne $currentModification) {
            $current.modifications.Add($currentModification) | Out-Null
            $currentModification = $null
        }
        if ($null -ne $current) {
            $instances.Add($current) | Out-Null
            $current = $null
        }
    }
}

if ($null -ne $current) {
    if ($null -ne $currentModification) {
        $current.modifications.Add($currentModification) | Out-Null
    }
    $instances.Add($current) | Out-Null
}

$surfaceRecords = New-Object System.Collections.Generic.List[object]
foreach ($instance in @($instances.ToArray() | Where-Object { -not [string]::IsNullOrWhiteSpace($_.sourceGuid) })) {
    $assetPath = if ($guidToPath.ContainsKey($instance.sourceGuid)) { $guidToPath[$instance.sourceGuid] } else { "" }
    if ($assetPath -notmatch '^Assets/Prefabs/Features/.+\.prefab$') {
        continue
    }

    $mods = @($instance.modifications.ToArray())
    $nameOverride = @($mods | Where-Object { $_.propertyPath -eq "m_Name" } | Select-Object -First 1)
    $surfaceName = if (@($nameOverride).Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($nameOverride[0].value)) {
        [string]$nameOverride[0].value
    }
    else {
        [System.IO.Path]::GetFileNameWithoutExtension($assetPath)
    }

    $activeOverrides = @($mods | Where-Object { $_.family -eq "active" })
    $layoutOverrides = @($mods | Where-Object { $_.family -eq "layout" })
    $visualOverrides = @($mods | Where-Object { $_.family -in @("text", "color", "asset-reference") })
    $unknownOverrides = @($mods | Where-Object { $_.family -eq "other" })
    $layoutTargetCount = @($layoutOverrides | Select-Object -ExpandProperty targetFileId -Unique).Count
    $internalLayoutTargetCount = [Math]::Max(0, $layoutTargetCount - 1)
    $internalActiveOverrideCount = [Math]::Max(0, @($activeOverrides).Count - 1)
    $classification = Get-Classification `
        -VisualOverrideCount @($visualOverrides).Count `
        -InternalActiveOverrideCount $internalActiveOverrideCount `
        -InternalLayoutTargetCount $internalLayoutTargetCount `
        -UnknownOverrideCount @($unknownOverrides).Count

    $surfaceRecords.Add([PSCustomObject][ordered]@{
        surfaceName = $surfaceName
        prefabAssetPath = $assetPath
        sourceGuid = $instance.sourceGuid
        overrideCount = @($mods).Count
        activeOverrideCount = @($activeOverrides).Count
        visualOverrideCount = @($visualOverrides).Count
        internalActiveOverrideCount = $internalActiveOverrideCount
        internalLayoutTargetCount = $internalLayoutTargetCount
        unknownOverrideCount = @($unknownOverrides).Count
        classification = $classification
        note = Get-Note `
            -Classification $classification `
            -VisualOverrideCount @($visualOverrides).Count `
            -InternalActiveOverrideCount $internalActiveOverrideCount `
            -InternalLayoutTargetCount $internalLayoutTargetCount `
            -UnknownOverrideCount @($unknownOverrides).Count
        propertyFamilies = @(
            $mods |
                Group-Object family |
                Sort-Object Name |
                ForEach-Object { [PSCustomObject][ordered]@{ family = $_.Name; count = $_.Count } }
        )
        warningOverrides = @(
            $mods |
                Where-Object { $_.family -in @("text", "color", "asset-reference", "other") } |
                Select-Object targetFileId, propertyPath, family, value, objectReference
        )
    })
}

$records = @($surfaceRecords.ToArray() | Sort-Object surfaceName)
$warningRecords = @($records | Where-Object { $_.classification -eq "warning" })
$reviewRecords = @($records | Where-Object { $_.classification -eq "review-candidate" })
$allowedRecords = @($records | Where-Object { $_.classification -eq "allowed-candidate" })

$report = [ordered]@{
    schemaVersion = "1.0.0"
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    source = $ScenePath
    purpose = "warning evidence for plans.prefab-management-gap-closeout"
    summary = [ordered]@{
        surfaceCount = @($records).Count
        allowedCandidateCount = @($allowedRecords).Count
        reviewCandidateCount = @($reviewRecords).Count
        warningCount = @($warningRecords).Count
        visualOverrideCount = @($records | Measure-Object -Property visualOverrideCount -Sum).Sum
    }
    surfaces = $records
}

$jsonAbsolute = Resolve-RepoPath $ResultJsonPath
$markdownAbsolute = Resolve-RepoPath $ResultMarkdownPath
New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($jsonAbsolute)) | Out-Null
($report | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $jsonAbsolute -Encoding UTF8

$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# LobbyScene Prefab Override Audit")
[void]$md.AppendLine("")
[void]$md.AppendLine("> generatedAt: $($report.generatedAt)")
[void]$md.AppendLine(("> source: ``{0}``" -f $ScenePath))
[void]$md.AppendLine('> purpose: warning evidence for `plans.prefab-management-gap-closeout`')
[void]$md.AppendLine("")
[void]$md.AppendLine('This is a read-only YAML audit of prefab instance overrides currently present in `LobbyScene`.')
[void]$md.AppendLine("It is workflow policy evidence, but starts as warning/review evidence rather than a hard gate.")
[void]$md.AppendLine("")
[void]$md.AppendLine("## Summary")
[void]$md.AppendLine("")
[void]$md.AppendLine("- surfaces: $($report.summary.surfaceCount)")
[void]$md.AppendLine("- allowed candidates: $($report.summary.allowedCandidateCount)")
[void]$md.AppendLine("- review candidates: $($report.summary.reviewCandidateCount)")
[void]$md.AppendLine("- warnings: $($report.summary.warningCount)")
[void]$md.AppendLine("- visual/text/color/asset-reference overrides: $($report.summary.visualOverrideCount)")
[void]$md.AppendLine("")
[void]$md.AppendLine("| Surface | Prefab asset | Override count | Active overrides | Visual overrides | Classification | Note |")
[void]$md.AppendLine("|---|---|---:|---:|---:|---|---|")
foreach ($record in $records) {
    [void]$md.AppendLine(("| `{0}` | `{1}` | {2} | {3} | {4} | {5} | {6} |" -f $record.surfaceName, $record.prefabAssetPath, $record.overrideCount, $record.activeOverrideCount, $record.visualOverrideCount, $record.classification, $record.note))
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## Property Families")
[void]$md.AppendLine("")
[void]$md.AppendLine("Warnings are triggered by text/color/asset-reference/unknown property overrides. Review candidates are internal active/layout overrides that may still be intentional scene state.")
[void]$md.AppendLine("")
[void]$md.AppendLine("## Follow-Up")
[void]$md.AppendLine("")
if (@($warningRecords).Count -eq 0) {
    [void]$md.AppendLine("- No visual/text/color/asset-reference warning override appeared in this pass.")
}
else {
    [void]$md.AppendLine("- Review warning surfaces before acceptance: $([string]::Join(', ', @($warningRecords | ForEach-Object { $_.surfaceName }))).")
}
if (@($reviewRecords).Count -gt 0) {
    [void]$md.AppendLine("- Confirm review-candidate active/layout overrides are intentional scene-level state: $([string]::Join(', ', @($reviewRecords | ForEach-Object { $_.surfaceName }))).")
}

$md.ToString() | Set-Content -LiteralPath $markdownAbsolute -Encoding UTF8

[PSCustomObject]@{
    success = $true
    resultJsonPath = $ResultJsonPath
    resultMarkdownPath = $ResultMarkdownPath
    surfaceCount = $report.summary.surfaceCount
    reviewCandidateCount = $report.summary.reviewCandidateCount
    warningCount = $report.summary.warningCount
    visualOverrideCount = $report.summary.visualOverrideCount
} | ConvertTo-Json -Depth 4
