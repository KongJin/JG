param(
    [string] $SourceRoot = "C:\Program Files (x86)\Nova1492",
    [string] $PartCatalogPath = "artifacts/nova1492/nova_part_catalog.csv",
    [string] $XfiManifestPath = "artifacts/nova1492/nova_unitpart_xfi_manifest.csv",
    [string] $XfiProposalPath = "artifacts/nova1492/nova_xfi_alignment_proposal.csv",
    [string] $OutputDirectory = "artifacts/nova1492/assembly-profile"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-RelativePath {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Replace("/", "\").ToLowerInvariant()
}

function ConvertTo-VectorText {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "0;0;0"
    }

    return $Value
}

function Get-SlotMode {
    param(
        [string] $Slot,
        [string] $AssemblyForm
    )

    if ($Slot -eq "Firepower" -and $AssemblyForm -eq "Humanoid") {
        return "shell"
    }

    if ($AssemblyForm -eq "Shoulder") {
        return "pair"
    }

    return "single"
}

function Get-SourceSlotCode {
    param(
        [string] $Slot,
        [string] $AssemblyForm,
        [object] $Proposal
    )

    if ($null -ne $Proposal -and
        $Proposal.qualityFlag -eq "xfi_named_attach_socket_candidate" -and
        -not [string]::IsNullOrWhiteSpace($Proposal.primarySocketName)) {
        return $Proposal.primarySocketName
    }

    switch ($Slot) {
        "Mobility" { return "legs" }
        "Frame" { return "body" }
        "Firepower" {
            if ($AssemblyForm -eq "Shoulder") {
                return "lshd|rshd"
            }

            return "top"
        }
        default { return "" }
    }
}

function Get-AnchorMode {
    param(
        [string] $Slot,
        [string] $AssemblyForm
    )

    if ($Slot -eq "Mobility") {
        return "LegBodySocket"
    }

    switch ($AssemblyForm) {
        "Tower" { return "FrameTopSocket" }
        "Shoulder" { return "ShoulderPair" }
        "Humanoid" { return "HumanoidShellBoundsCenter" }
        default { return "ManualOffset" }
    }
}

function Get-Confidence {
    param(
        [string] $AnchorMode,
        [object] $Proposal
    )

    if ($AnchorMode -eq "ManualOffset") {
        return "blocked"
    }

    if ($null -eq $Proposal) {
        return "review"
    }

    switch ($Proposal.qualityFlag) {
        "xfi_body_top_socket_candidate" { return "derived" }
        "xfi_leg_body_socket_candidate" { return "derived" }
        "xfi_named_attach_socket_candidate" { return "derived" }
        "xfi_weapon_direction_only" { return "review" }
        default { return "review" }
    }
}

function Get-LocalOffset {
    param([object] $Proposal)

    if ($null -ne $Proposal -and -not [string]::IsNullOrWhiteSpace($Proposal.proposedSocketOffset)) {
        return $Proposal.proposedSocketOffset
    }

    return "0;0;0"
}

function Get-LocalRotation {
    param([object] $Proposal)

    if ($null -ne $Proposal -and
        $Proposal.qualityFlag -eq "xfi_named_attach_socket_candidate" -and
        -not [string]::IsNullOrWhiteSpace($Proposal.proposedSocketEuler)) {
        return $Proposal.proposedSocketEuler
    }

    return "0;0;0"
}

function Get-XfiClass {
    param([object] $Xfi)

    if ($null -eq $Xfi) {
        return "missing"
    }

    if ($Xfi.parseStatus -ne "parsed") {
        return $Xfi.parseStatus
    }

    if ([int]$Xfi.transformCount -gt 0) {
        return "transform-bearing"
    }

    if ([int]$Xfi.directionRangeCount -gt 0) {
        return "direction-only"
    }

    return "metadata-only"
}

function Get-Notes {
    param(
        [object] $Catalog,
        [object] $Proposal,
        [string] $XfiClass,
        [string] $AnchorMode,
        [hashtable] $HighRiskByPartId
    )

    $notes = New-Object System.Collections.Generic.List[string]
    if ($HighRiskByPartId.ContainsKey($Catalog.partId)) {
        $notes.Add($HighRiskByPartId[$Catalog.partId]) | Out-Null
    }

    if ($AnchorMode -eq "ManualOffset") {
        $notes.Add("No generic anchor mode was inferred; requires source or visual review before use.") | Out-Null
    }

    if ($XfiClass -eq "direction-only") {
        $notes.Add("XFI has direction ranges but no transform matrix; do not promote current bounds/pivot offset as source truth.") | Out-Null
    }

    if ($null -ne $Proposal -and -not [string]::IsNullOrWhiteSpace($Proposal.reviewReason)) {
        $notes.Add($Proposal.reviewReason) | Out-Null
    }

    return ($notes -join " ")
}

function Read-SlotEvidence {
    param([string] $Root)

    $commonPath = Join-Path $Root "datan\common"
    if (-not (Test-Path -LiteralPath $commonPath)) {
        return @()
    }

    $suffixToSlot = @{
        LArm = "larm"
        RArm = "rarm"
        LBack = "lback"
        RBack = "rback"
        Top = "top"
        Front = "front"
    }

    return @(
        Get-ChildItem -LiteralPath $commonPath -File |
            Where-Object { $_.Name -match "_(LArm|RArm|LBack|RBack|Top|Front)\.GX$" } |
            ForEach-Object {
                $suffix = [regex]::Match($_.Name, "_(LArm|RArm|LBack|RBack|Top|Front)\.GX$", [Text.RegularExpressions.RegexOptions]::IgnoreCase).Groups[1].Value
                $normalizedSuffix = $suffix.Substring(0, 1).ToUpperInvariant() + $suffix.Substring(1)
                [pscustomobject]@{
                    source_slot_code = $suffixToSlot[$normalizedSuffix]
                    suffix = $normalizedSuffix
                    file_name = $_.Name
                    source_relative_path = ("datan\common\{0}" -f $_.Name)
                }
            }
    )
}

foreach ($path in @($PartCatalogPath, $XfiManifestPath, $XfiProposalPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required input not found: $path"
    }
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Source root not found: $SourceRoot"
}

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
}

$profilePath = Join-Path $OutputDirectory "nova_assembly_profile.csv"
$reviewPath = Join-Path $OutputDirectory "nova_assembly_profile_manual_review.csv"
$slotEvidencePath = Join-Path $OutputDirectory "nova_assembly_slot_evidence.csv"
$reportPath = Join-Path $OutputDirectory "nova_assembly_profile_report.md"

$xfiBySource = @{}
$xfiByPartId = @{}
foreach ($row in (Import-Csv -LiteralPath $XfiManifestPath)) {
    $sourceKey = Normalize-RelativePath $row.source_relative_path
    if (-not [string]::IsNullOrWhiteSpace($sourceKey)) {
        $xfiBySource[$sourceKey] = $row
    }

    if (-not [string]::IsNullOrWhiteSpace($row.partId)) {
        $xfiByPartId[$row.partId] = $row
    }
}

$proposalByPartId = @{}
foreach ($row in (Import-Csv -LiteralPath $XfiProposalPath)) {
    if (-not [string]::IsNullOrWhiteSpace($row.partId)) {
        $proposalByPartId[$row.partId] = $row
    }
}

$highRiskByPartId = @{
    "nova_fire_arm15_hdkn" = "Initial humanoid shell review target."
    "nova_fire_arm32_sppoo" = "Initial humanoid shell review target."
    "nova_fire_arm39_hmsk" = "Initial humanoid shell review target."
    "nova_mob_legs3_ktpr" = "Known leg single-part visual quality risk; route to GX audit if unit capture still mismatches."
    "nova_mob_legs34_dpns" = "Known leg single-part visual quality risk; route to GX audit if unit capture still mismatches."
    "nova_mob_legs49_otrs" = "Known leg single-part visual quality risk; route to GX audit if unit capture still mismatches."
    "nova_mob_legs51_ppo" = "Known leg single-part visual quality risk; route to GX audit if unit capture still mismatches."
    "nova_mob_g_legs57_ppo" = "Known leg single-part visual quality risk; route to GX audit if unit capture still mismatches."
}

$profiles = New-Object System.Collections.Generic.List[object]
foreach ($catalog in (Import-Csv -LiteralPath $PartCatalogPath)) {
    $sourceKey = Normalize-RelativePath $catalog.source_relative_path
    $xfi = $null
    if ($xfiByPartId.ContainsKey($catalog.partId)) {
        $xfi = $xfiByPartId[$catalog.partId]
    }
    elseif ($xfiBySource.ContainsKey($sourceKey)) {
        $xfi = $xfiBySource[$sourceKey]
    }

    $proposal = if ($proposalByPartId.ContainsKey($catalog.partId)) { $proposalByPartId[$catalog.partId] } else { $null }
    $anchorMode = Get-AnchorMode -Slot $catalog.slot -AssemblyForm $catalog.assemblyForm
    $slotMode = Get-SlotMode -Slot $catalog.slot -AssemblyForm $catalog.assemblyForm
    $sourceSlotCode = Get-SourceSlotCode -Slot $catalog.slot -AssemblyForm $catalog.assemblyForm -Proposal $proposal
    $confidence = Get-Confidence -AnchorMode $anchorMode -Proposal $proposal
    $xfiClass = Get-XfiClass -Xfi $xfi
    $evidence = @(
        "catalog:$PartCatalogPath",
        "xfi:$XfiManifestPath",
        "proposal:$XfiProposalPath",
        ("source:{0}" -f $catalog.source_relative_path)
    ) -join "; "

    $profiles.Add([pscustomobject]@{
        part_id = $catalog.partId
        source_relative_path = $catalog.source_relative_path
        display_name_ko = $catalog.displayName
        category = $catalog.slot
        catalog_category = $catalog.category
        assembly_form = $catalog.assemblyForm
        mobility_surface = $catalog.mobilitySurface
        source_slot_code = $sourceSlotCode
        slot_mode = $slotMode
        anchor_mode = $anchorMode
        local_offset = Get-LocalOffset -Proposal $proposal
        local_rotation = Get-LocalRotation -Proposal $proposal
        local_scale = "1;1;1"
        confidence = $confidence
        evidence_path = $evidence
        review_result = "pending"
        xfi_class = $xfiClass
        xfi_header_kind = if ($null -ne $xfi) { $xfi.xfi_header_kind } else { "missing" }
        xfi_transform_count = if ($null -ne $xfi) { $xfi.transformCount } else { 0 }
        xfi_direction_range_count = if ($null -ne $xfi) { $xfi.directionRangeCount } else { 0 }
        quality_flag = if ($null -ne $proposal) { $proposal.qualityFlag } else { "" }
        notes = Get-Notes -Catalog $catalog -Proposal $proposal -XfiClass $xfiClass -AnchorMode $anchorMode -HighRiskByPartId $highRiskByPartId
    }) | Out-Null
}

$slotEvidence = @(Read-SlotEvidence -Root $SourceRoot)
$profiles | Sort-Object category, assembly_form, part_id | Export-Csv -LiteralPath $profilePath -NoTypeInformation -Encoding UTF8
$slotEvidence | Sort-Object source_slot_code, file_name | Export-Csv -LiteralPath $slotEvidencePath -NoTypeInformation -Encoding UTF8

$profiles |
    Select-Object part_id, display_name_ko, category, assembly_form, mobility_surface, anchor_mode, confidence, review_result, notes |
    Sort-Object category, assembly_form, part_id |
    Export-Csv -LiteralPath $reviewPath -NoTypeInformation -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Nova1492 Assembly Profile Report") | Out-Null
$lines.Add("") | Out-Null
$lines.Add(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date))) | Out-Null
$lines.Add("") | Out-Null
$lines.Add(("- source root: ``{0}``" -f $SourceRoot)) | Out-Null
$lines.Add(("- profile: ``{0}``" -f $profilePath)) | Out-Null
$lines.Add(("- manual review: ``{0}``" -f $reviewPath)) | Out-Null
$lines.Add(("- slot evidence: ``{0}``" -f $slotEvidencePath)) | Out-Null
$lines.Add("") | Out-Null
$lines.Add("## Counts") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| metric | count |") | Out-Null
$lines.Add("|---|---:|") | Out-Null
$lines.Add(("| profile rows | {0} |" -f $profiles.Count)) | Out-Null
$lines.Add(("| slot-specific GX evidence files | {0} |" -f $slotEvidence.Count)) | Out-Null
$lines.Add(("| pending manual review | {0} |" -f @($profiles | Where-Object { $_.review_result -eq "pending" }).Count)) | Out-Null
$lines.Add(("| review confidence | {0} |" -f @($profiles | Where-Object { $_.confidence -eq "review" }).Count)) | Out-Null
$lines.Add(("| derived confidence | {0} |" -f @($profiles | Where-Object { $_.confidence -eq "derived" }).Count)) | Out-Null
$lines.Add(("| blocked confidence | {0} |" -f @($profiles | Where-Object { $_.confidence -eq "blocked" }).Count)) | Out-Null
$lines.Add("") | Out-Null
$lines.Add("## Anchor Modes") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| anchor_mode | count |") | Out-Null
$lines.Add("|---|---:|") | Out-Null
foreach ($group in ($profiles | Group-Object anchor_mode | Sort-Object Name)) {
    $lines.Add(("| `{0}` | {1} |" -f $group.Name, $group.Count)) | Out-Null
}

$lines.Add("") | Out-Null
$lines.Add("## XFI Classes") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("| xfi_class | count |") | Out-Null
$lines.Add("|---|---:|") | Out-Null
foreach ($group in ($profiles | Group-Object xfi_class | Sort-Object Name)) {
    $lines.Add(("| `{0}` | {1} |" -f $group.Name, $group.Count)) | Out-Null
}

$lines.Add("") | Out-Null
$lines.Add("## Guardrail") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("- Direction-only firepower XFI rows are seeded as review data, not source placement truth.") | Out-Null
$lines.Add("- Known broken-looking single parts stay in the profile with review notes, but their visual acceptance belongs to GX audit if the part is broken before assembly.") | Out-Null
$lines.Add("- Actual acceptance remains blocked until latest capture and manual visual review record `match`.") | Out-Null

Set-Content -LiteralPath $reportPath -Value $lines -Encoding UTF8

$expectedRows = 144
if ($profiles.Count -ne $expectedRows) {
    throw "Unexpected assembly profile row count: expected $expectedRows, got $($profiles.Count)"
}

$manualOffsetsWithoutEvidence = @(
    $profiles |
        Where-Object {
            $_.anchor_mode -eq "ManualOffset" -and [string]::IsNullOrWhiteSpace($_.evidence_path)
        }
)
if ($manualOffsetsWithoutEvidence.Count -gt 0) {
    throw "ManualOffset row without evidence: $($manualOffsetsWithoutEvidence[0].part_id)"
}

foreach ($expectedHumanoid in @("nova_fire_arm15_hdkn", "nova_fire_arm32_sppoo", "nova_fire_arm39_hmsk")) {
    $row = $profiles | Where-Object { $_.part_id -eq $expectedHumanoid } | Select-Object -First 1
    if ($null -eq $row -or $row.anchor_mode -ne "HumanoidShellBoundsCenter" -or $row.confidence -ne "review") {
        throw "Expected humanoid shell review profile for $expectedHumanoid"
    }
}

[pscustomobject]@{
    success = $true
    rows = $profiles.Count
    slotEvidenceRows = $slotEvidence.Count
    profile = $profilePath
    manualReview = $reviewPath
    slotEvidence = $slotEvidencePath
    report = $reportPath
} | ConvertTo-Json -Depth 4
