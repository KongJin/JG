param(
    [string] $AlignmentPath = "artifacts/nova1492/nova_part_alignment.csv",
    [string] $XfiManifestPath = "artifacts/nova1492/nova_unitpart_xfi_manifest.csv",
    [string] $OutputCsvPath = "artifacts/nova1492/nova_xfi_alignment_proposal.csv",
    [string] $OutputMarkdownPath = "artifacts/nova1492/nova_xfi_alignment_proposal_report.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-Vector {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parts = @($Value -split ";" | ForEach-Object { $_.Trim() })
    if ($parts.Count -ne 3) {
        return $null
    }

    return [pscustomobject]@{
        x = [double]::Parse($parts[0], [Globalization.CultureInfo]::InvariantCulture)
        y = [double]::Parse($parts[1], [Globalization.CultureInfo]::InvariantCulture)
        z = [double]::Parse($parts[2], [Globalization.CultureInfo]::InvariantCulture)
    }
}

function ConvertFrom-Vector {
    param($Vector)

    if ($null -eq $Vector) {
        return ""
    }

    return ("{0};{1};{2}" -f
        $Vector.x.ToString("0.######", [Globalization.CultureInfo]::InvariantCulture),
        $Vector.y.ToString("0.######", [Globalization.CultureInfo]::InvariantCulture),
        $Vector.z.ToString("0.######", [Globalization.CultureInfo]::InvariantCulture))
}

function Multiply-Vector {
    param($Vector, [double] $Scale)

    if ($null -eq $Vector) {
        return $null
    }

    return [pscustomobject]@{
        x = $Vector.x * $Scale
        y = $Vector.y * $Scale
        z = $Vector.z * $Scale
    }
}

function Get-VectorAt {
    param(
        [string] $VectorList,
        [int] $Index
    )

    if ([string]::IsNullOrWhiteSpace($VectorList)) {
        return $null
    }

    $items = @($VectorList -split "\|")
    if ($Index -lt 0 -or $Index -ge $items.Count) {
        return $null
    }

    return ConvertTo-Vector $items[$Index]
}

function Get-Proposal {
    param($Alignment, $Xfi)

    $normalizedScale = [double]::Parse($Alignment.normalizedScale, [Globalization.CultureInfo]::InvariantCulture)
    $slot = $Alignment.slot
    $header = $Xfi.xfi_header
    $quality = "xfi_reference_only"
    $reason = "XFI parsed but no direct single-field alignment mapping is proven."
    $socket = $null
    $euler = ConvertTo-Vector $Alignment.socketEuler
    $primarySocketName = ""

    if ($slot -eq "Frame" -and $Xfi.category -eq "UnitParts/Bodies") {
        $rawTopSocket = Get-VectorAt -VectorList $Xfi.transformTranslations -Index 2
        if ($null -ne $rawTopSocket) {
            $socket = Multiply-Vector -Vector $rawTopSocket -Scale $normalizedScale
            $quality = "xfi_body_top_socket_candidate"
            $reason = "Body XFI transform index 2 appears to be the top/firepower socket. Runtime still needs a frame-top socket field before direct asset promotion."
            $primarySocketName = "body.top"
        }
    }
    elseif ($slot -eq "Mobility" -and $Xfi.category -eq "UnitParts/Legs") {
        $rawBodySocket = Get-VectorAt -VectorList $Xfi.transformTranslations -Index 0
        if ($null -ne $rawBodySocket) {
            $socket = Multiply-Vector -Vector $rawBodySocket -Scale $normalizedScale
            $quality = "xfi_leg_body_socket_candidate"
            $reason = "Leg XFI transform index 0 appears to be the body attach socket."
            $primarySocketName = "legs.body"
        }
    }
    elseif ($slot -eq "Firepower" -and $Xfi.category -eq "UnitParts/ArmWeapons") {
        if ($Xfi.xfi_header_kind -eq "named_attach_slot" -and [int]$Xfi.transformCount -gt 0) {
            $rawAttachSocket = Get-VectorAt -VectorList $Xfi.transformTranslations -Index 0
            if ($null -ne $rawAttachSocket) {
                $socket = Multiply-Vector -Vector $rawAttachSocket -Scale $normalizedScale
                $quality = "xfi_named_attach_socket_candidate"
                $reason = "Named firepower XFI exposes an explicit attach slot and transform."
                $primarySocketName = $header
            }
        }
        elseif ($header -eq "4") {
            $quality = "xfi_weapon_direction_only"
            $reason = "Weapon XFI carries direction ranges but no transform matrix; preserve metadata without promoting a runtime attach offset."
            $primarySocketName = "weapon.direction"
        }
    }

    return [pscustomobject]@{
        partId = $Alignment.partId
        slot = $slot
        category = $Xfi.category
        xfiHeader = $header
        xfiHeaderKind = $Xfi.xfi_header_kind
        originalNameKr = $Xfi.originalNameKr
        primarySocketName = $primarySocketName
        currentSocketOffset = $Alignment.socketOffset
        proposedSocketOffset = ConvertFrom-Vector $socket
        currentSocketEuler = $Alignment.socketEuler
        proposedSocketEuler = ConvertFrom-Vector $euler
        normalizedScale = $Alignment.normalizedScale
        xfiTransformCount = $Xfi.transformCount
        xfiDirectionRangeCount = $Xfi.directionRangeCount
        qualityFlag = $quality
        reviewReason = $reason
        source_relative_path = $Xfi.source_relative_path
    }
}

if (-not (Test-Path -LiteralPath $AlignmentPath)) {
    throw "Alignment CSV not found: $AlignmentPath"
}

if (-not (Test-Path -LiteralPath $XfiManifestPath)) {
    throw "XFI manifest not found: $XfiManifestPath"
}

$xfiByPartId = @{}
foreach ($row in (Import-Csv -LiteralPath $XfiManifestPath)) {
    if ([string]::IsNullOrWhiteSpace($row.partId)) {
        continue
    }

    $xfiByPartId[$row.partId] = $row
}

$rows = New-Object System.Collections.Generic.List[object]
foreach ($alignment in (Import-Csv -LiteralPath $AlignmentPath)) {
    if (-not $xfiByPartId.ContainsKey($alignment.partId)) {
        continue
    }

    $rows.Add((Get-Proposal -Alignment $alignment -Xfi $xfiByPartId[$alignment.partId])) | Out-Null
}

$rows | Sort-Object slot, category, partId | Export-Csv -LiteralPath $OutputCsvPath -NoTypeInformation -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Nova1492 XFI Alignment Proposal")
$lines.Add("")
$lines.Add(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date)))
$lines.Add("")
$lines.Add('- input alignment: `artifacts/nova1492/nova_part_alignment.csv`')
$lines.Add('- input xfi manifest: `artifacts/nova1492/nova_unitpart_xfi_manifest.csv`')
$lines.Add('- output proposal: `artifacts/nova1492/nova_xfi_alignment_proposal.csv`')
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| metric | count |")
$lines.Add("|---|---:|")
$lines.Add("| proposal rows | $($rows.Count) |")
$lines.Add("| rows with proposed socket offset | $(@($rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.proposedSocketOffset) }).Count) |")
$lines.Add("")
$lines.Add("## Quality Flags")
$lines.Add("")
$lines.Add("| quality | count |")
$lines.Add("|---|---:|")
foreach ($group in ($rows | Group-Object qualityFlag | Sort-Object Name)) {
    $lines.Add(("| `{0}` | {1} |" -f $group.Name, $group.Count))
}

$lines.Add("")
$lines.Add("## Promotion Notes")
$lines.Add("")
$lines.Add("- This file is a proposal, not runtime truth.")
$lines.Add('- `xfi_weapon_direction_only` rows preserve direction metadata only; do not promote a runtime attach offset until the parent body socket model is available.')
$lines.Add('- `xfi_body_top_socket_candidate` rows can be promoted into the dedicated frame top socket field.')
$lines.Add('- `xfi_leg_body_socket_candidate` rows can be promoted into the dedicated XFI attach socket field.')
$lines.Add('- `xfi_named_attach_socket_candidate` rows should be preserved in Unity data until parent body slot mapping is promoted.')

Set-Content -LiteralPath $OutputMarkdownPath -Value $lines -Encoding UTF8

[pscustomobject]@{
    success = $true
    rows = $rows.Count
    proposedSocketOffsets = @($rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.proposedSocketOffset) }).Count
    output = $OutputCsvPath
    report = $OutputMarkdownPath
} | ConvertTo-Json -Depth 4
