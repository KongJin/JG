param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$InventoryJsonPath = "artifacts/unity/prefab-management-inventory.json",
    [string]$InventoryMarkdownPath = "artifacts/unity/prefab-management-inventory.md",
    [string]$ApprovalManifestPath = "artifacts/unity/prefab-management-approved-new-prefabs.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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

function Resolve-RepoPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function Get-MetaGuid {
    param([string]$AssetPath)

    $metaPath = Resolve-RepoPath "$AssetPath.meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        return ""
    }

    $text = Get-Content -LiteralPath $metaPath -Raw
    $match = [regex]::Match($text, "(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
    if (-not $match.Success) {
        return ""
    }

    return $match.Groups[1].Value.ToLowerInvariant()
}

function Get-GeneratedBaseId {
    param([string]$PrefabPath)

    $name = [System.IO.Path]::GetFileNameWithoutExtension($PrefabPath)
    return [regex]::Replace($name, "_[0-9a-fA-F]{8}$", "")
}

function Get-ResourceMigrationStatus {
    param([string]$Path)

    switch -Regex ($Path) {
        '^Assets/Resources/PlayerHealthHudView\.prefab$' { return "native-candidate" }
        '^Assets/Resources/(EnemyHealthBar|DamageNumber)\.prefab$' { return "gameplay-feedback-candidate" }
        '^Assets/Resources/(BattleEntity|EnemyCharacter|EnemyCharacterCore|PlayerCharacter|ProjectilePhysicsAdapter|ZoneEffect)\.prefab$' { return "not-ui/gameplay-runtime" }
        default { return "" }
    }
}

function Get-PrefabClass {
    param([string]$Path)

    if ($Path -match '^Assets/Resources/.+\.prefab$') {
        $migration = Get-ResourceMigrationStatus -Path $Path
        if ($migration -match '^native-candidate|^gameplay-feedback') {
            return "resources-native-ui"
        }

        return "gameplay-runtime"
    }

    if ($Path -match '^Assets/Prefabs/.+\b(Battle|Projectile|Effect|Enemy|Player)\b.+\.prefab$') {
        return "gameplay-runtime"
    }

    return "unknown"
}

function Get-DeclaredApprovalRecords {
    return @()
}

function Get-ReferencedPrefabGuids {
    $guids = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    $dataRoot = Resolve-RepoPath "Assets/Data/Garage"
    if (-not (Test-Path -LiteralPath $dataRoot)) {
        return $guids
    }

    foreach ($file in Get-ChildItem -LiteralPath $dataRoot -Recurse -File -Include *.asset) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($match in [regex]::Matches($text, "guid:\s*([0-9a-fA-F]{32})")) {
            [void]$guids.Add($match.Groups[1].Value.ToLowerInvariant())
        }
    }

    return $guids
}

$prefabRoots = @("Assets/Prefabs", "Assets/Resources")
$prefabFiles = @()
foreach ($root in $prefabRoots) {
    $absoluteRoot = Resolve-RepoPath $root
    if (-not (Test-Path -LiteralPath $absoluteRoot)) {
        continue
    }

    $prefabFiles += Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File -Filter *.prefab
}

$approvedRecords = @(Get-DeclaredApprovalRecords)
$approvedPaths = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($record in $approvedRecords) {
    [void]$approvedPaths.Add($record.assetPath)
}

$referencedGuids = Get-ReferencedPrefabGuids
$records = New-Object System.Collections.Generic.List[object]

foreach ($file in @($prefabFiles | Sort-Object FullName)) {
    $path = ConvertTo-RepoPath $file.FullName
    $guid = Get-MetaGuid -AssetPath $path
    $class = Get-PrefabClass -Path $path
    $resourceMigration = Get-ResourceMigrationStatus -Path $path
    $isGenerated = $class -eq "generated-preview"
    $baseId = if ($isGenerated) { Get-GeneratedBaseId -PrefabPath $path } else { "" }
    $isHashSuffix = $isGenerated -and ([System.IO.Path]::GetFileNameWithoutExtension($path) -match "_[0-9a-fA-F]{8}$")
    $isReferenced = (-not [string]::IsNullOrWhiteSpace($guid)) -and $referencedGuids.Contains($guid)
    $lifecycleStatus = "active"

    if ($class -eq "generated-preview") {
        $lifecycleStatus = if ($isReferenced) { "playable" } elseif ($isHashSuffix) { "duplicate-candidate" } else { "active" }
    }
    elseif ($class -eq "gameplay-runtime") {
        $lifecycleStatus = "not-ui/generated"
    }
    elseif ($class -eq "resources-native-ui") {
        $lifecycleStatus = $resourceMigration
    }

    $approvalStatus = if ($approvedPaths.Contains($path)) {
        "approved-declared"
    }
    else {
        ""
    }

                $records.Add([PSCustomObject][ordered]@{
        assetPath = $path
        guid = $guid
        class = $class
        lifecycleStatus = $lifecycleStatus
        approvalStatus = $approvalStatus
        generatedBaseId = $baseId
        resourceMigrationStatus = $resourceMigration
        referencedByGarageData = $isReferenced
        lastWriteUtc = $file.LastWriteTimeUtc.ToString("o")
    })
}

$duplicateGroups = @(
    $records |
        Where-Object { $_.class -eq "generated-preview" -and -not [string]::IsNullOrWhiteSpace($_.generatedBaseId) } |
        Group-Object generatedBaseId |
        Where-Object { $_.Count -gt 1 } |
        ForEach-Object {
            [PSCustomObject][ordered]@{
                generatedBaseId = $_.Name
                count = $_.Count
                prefabPaths = @($_.Group | ForEach-Object { $_.assetPath } | Sort-Object)
            }
        } |
        Sort-Object generatedBaseId
)

$summaryByClass = @(
    $records |
        Group-Object class |
        Sort-Object Name |
        ForEach-Object { [PSCustomObject][ordered]@{ class = $_.Name; count = $_.Count } }
)

$summaryByLifecycle = @(
    $records |
        Group-Object lifecycleStatus |
        Sort-Object Name |
        ForEach-Object { [PSCustomObject][ordered]@{ lifecycleStatus = $_.Name; count = $_.Count } }
)

$recordArray = @($records.ToArray())
$totalPrefabs = $recordArray.Count
$generatedPreviewPrefabCount = @($recordArray | Where-Object { $_.class -eq "generated-preview" }).Count
$resourcesPrefabCount = @($recordArray | Where-Object { $_.assetPath -match '^Assets/Resources/' }).Count
$duplicateCandidateGroupCount = @($duplicateGroups).Count
$approvedNewPrefabTargetCount = @($approvedRecords).Count

$inventory = [ordered]@{
    schemaVersion = "1.0.0"
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    repoRoot = (ConvertTo-RepoPath $RepoRoot)
    inputs = [ordered]@{
        prefabRoots = $prefabRoots
        mappingRoot = ".stitch/contracts/mappings"
        garageDataRoot = "Assets/Data/Garage"
    }
    summary = [ordered]@{
        totalPrefabs = $totalPrefabs
        generatedPreviewPrefabs = $generatedPreviewPrefabCount
        resourcesPrefabs = $resourcesPrefabCount
        duplicateCandidateGroups = $duplicateCandidateGroupCount
        approvedNewPrefabTargets = $approvedNewPrefabTargetCount
        byClass = $summaryByClass
        byLifecycleStatus = $summaryByLifecycle
    }
    approvedNewPrefabTargets = $approvedRecords
    duplicateCandidateGroups = $duplicateGroups
    prefabs = $recordArray
}

$approvalManifest = [ordered]@{
    schemaVersion = "1.0.0"
    generatedAt = $inventory.generatedAt
    ownerPlan = "docs/plans/prefab_management_gap_closeout_plan.md"
    prefabs = $approvedRecords
}

$inventoryJsonAbsolute = Resolve-RepoPath $InventoryJsonPath
$inventoryMarkdownAbsolute = Resolve-RepoPath $InventoryMarkdownPath
$approvalManifestAbsolute = Resolve-RepoPath $ApprovalManifestPath

New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($inventoryJsonAbsolute)) | Out-Null
New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($approvalManifestAbsolute)) | Out-Null

($inventory | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $inventoryJsonAbsolute -Encoding UTF8
($approvalManifest | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath $approvalManifestAbsolute -Encoding UTF8

$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# Prefab Management Inventory")
[void]$md.AppendLine("")
[void]$md.AppendLine("> generated: $($inventory.generatedAt)")
[void]$md.AppendLine("")
[void]$md.AppendLine("## Summary")
[void]$md.AppendLine("")
[void]$md.AppendLine("- total prefabs: $($inventory.summary.totalPrefabs)")
[void]$md.AppendLine("- generated preview prefabs: $($inventory.summary.generatedPreviewPrefabs)")
[void]$md.AppendLine("- Resources prefabs: $($inventory.summary.resourcesPrefabs)")
[void]$md.AppendLine("- duplicate candidate groups: $($inventory.summary.duplicateCandidateGroups)")
[void]$md.AppendLine("- approved new prefab targets: $($inventory.summary.approvedNewPrefabTargets)")
[void]$md.AppendLine("")
[void]$md.AppendLine("## By Class")
[void]$md.AppendLine("")
[void]$md.AppendLine("| class | count |")
[void]$md.AppendLine("|---|---:|")
foreach ($item in $summaryByClass) {
    [void]$md.AppendLine("| $($item.class) | $($item.count) |")
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## By Lifecycle Status")
[void]$md.AppendLine("")
[void]$md.AppendLine("| lifecycle status | count |")
[void]$md.AppendLine("|---|---:|")
foreach ($item in $summaryByLifecycle) {
    [void]$md.AppendLine("| $($item.lifecycleStatus) | $($item.count) |")
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## Approved New Prefab Targets")
[void]$md.AppendLine("")
if (@($approvedRecords).Count -eq 0) {
    [void]$md.AppendLine("- none")
}
else {
    foreach ($record in $approvedRecords) {
        [void]$md.AppendLine(("- `{0}` - {1}; evidence: {2}" -f $record.assetPath, $record.status, ([string]::Join(', ', @($record.sourceEvidence)))))
    }
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## Resources Migration Status")
[void]$md.AppendLine("")
[void]$md.AppendLine("| prefab | migration status | class |")
[void]$md.AppendLine("|---|---|---|")
foreach ($record in @($records | Where-Object { $_.assetPath -match '^Assets/Resources/.+\.prefab$' } | Sort-Object assetPath)) {
    [void]$md.AppendLine(("| `{0}` | {1} | {2} |" -f $record.assetPath, $record.resourceMigrationStatus, $record.class))
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## Duplicate Candidate Groups")
[void]$md.AppendLine("")
[void]$md.AppendLine("| generated base id | count |")
[void]$md.AppendLine("|---|---:|")
foreach ($group in @($duplicateGroups | Select-Object -First 50)) {
    [void]$md.AppendLine(("| `{0}` | {1} |" -f $group.generatedBaseId, $group.count))
}
if (@($duplicateGroups).Count -gt 50) {
    [void]$md.AppendLine("")
    [void]$md.AppendLine("_Truncated to first 50 groups. See JSON for full prefab path lists._")
}

$md.ToString() | Set-Content -LiteralPath $inventoryMarkdownAbsolute -Encoding UTF8

[PSCustomObject]@{
    success = $true
    inventoryJsonPath = $InventoryJsonPath
    inventoryMarkdownPath = $InventoryMarkdownPath
    approvalManifestPath = $ApprovalManifestPath
    totalPrefabs = $inventory.summary.totalPrefabs
    duplicateCandidateGroups = $inventory.summary.duplicateCandidateGroups
    approvedNewPrefabTargets = $inventory.summary.approvedNewPrefabTargets
} | ConvertTo-Json -Depth 4
