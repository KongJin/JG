param(
    [string] $XfiManifestPath = "artifacts/nova1492/nova_unitpart_xfi_manifest.csv",
    [string] $ProposalPath = "artifacts/nova1492/nova_xfi_alignment_proposal.csv",
    [string] $PartCatalogPath = "artifacts/nova1492/nova_part_catalog.csv",
    [string] $AlignmentAssetPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset",
    [string] $ReportPath = "artifacts/nova1492/nova_xfi_unity_promotion_report.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-YamlVector {
    param([string] $Value)

    $parts = @($Value -split ";" | ForEach-Object { $_.Trim() })
    if ($parts.Count -ne 3) {
        throw "Expected vector in x;y;z format, got: $Value"
    }

    return ("{{x: {0}, y: {1}, z: {2}}}" -f $parts[0], $parts[1], $parts[2])
}

function ConvertTo-YamlScalar {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ("'{0}'" -f $Value.Replace("'", "''"))
}

function ConvertTo-SourceKey {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ($Value -replace "/", "\").ToLowerInvariant()
}

function Remove-PromotedFields {
    param([string[]] $Block)

    return @(
        $Block |
            Where-Object {
                $_ -notmatch "^\s+boundsSize:" -and
                $_ -notmatch "^\s+boundsCenter:" -and
                $_ -notmatch "^\s+hasXfiMetadata:" -and
                $_ -notmatch "^\s+xfiPath:" -and
                $_ -notmatch "^\s+xfiHeader:" -and
                $_ -notmatch "^\s+xfiHeaderKind:" -and
                $_ -notmatch "^\s+xfiAttachSlot:" -and
                $_ -notmatch "^\s+xfiAttachVariant:" -and
                $_ -notmatch "^\s+xfiTransformCount:" -and
                $_ -notmatch "^\s+xfiTransformTranslations:" -and
                $_ -notmatch "^\s+xfiDirectionRangeCount:" -and
                $_ -notmatch "^\s+xfiDirectionRanges:" -and
                $_ -notmatch "^\s+hasXfiAttachSocket:" -and
                $_ -notmatch "^\s+xfiAttachSocketOffset:" -and
                $_ -notmatch "^\s+hasFrameTopSocket:" -and
                $_ -notmatch "^\s+frameTopSocketOffset:" -and
                $_ -notmatch "^\s+xfiSocketQuality:" -and
                $_ -notmatch "^\s+xfiSocketName:"
            }
    )
}

function Update-EntryBlock {
    param(
        [string[]] $Block,
        [hashtable] $XfiByPartId,
        [hashtable] $XfiBySourcePath,
        [hashtable] $SourcePathByPartId,
        [hashtable] $ProposalByPartId,
        [hashtable] $Counters
    )

    $partLine = $Block | Where-Object { $_ -match "^\s+- partId:" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($partLine)) {
        return ,$Block
    }

    $partId = ($partLine -replace "^\s+- partId:\s*", "").Trim()
    $Counters.assetEntries++

    $cleaned = Remove-PromotedFields -Block $Block
    $xfi = $null
    if ($XfiByPartId.ContainsKey($partId)) {
        $xfi = $XfiByPartId[$partId]
    }
    elseif ($SourcePathByPartId.ContainsKey($partId)) {
        $sourceKey = ConvertTo-SourceKey $SourcePathByPartId[$partId]
        if ($XfiBySourcePath.ContainsKey($sourceKey)) {
            $xfi = $XfiBySourcePath[$sourceKey]
            $Counters.xfiMatchedBySourcePath++
        }
    }

    if ($null -eq $xfi) {
        $Counters.entriesWithoutXfi++
        return ,$cleaned
    }

    $proposal = if ($ProposalByPartId.ContainsKey($partId)) { $ProposalByPartId[$partId] } else { $null }
    if ($null -eq $proposal -and -not [string]::IsNullOrWhiteSpace($xfi.partId) -and $ProposalByPartId.ContainsKey($xfi.partId)) {
        $proposal = $ProposalByPartId[$xfi.partId]
    }

    $insertAfter = -1
    for ($i = 0; $i -lt $cleaned.Count; $i++) {
        if ($cleaned[$i] -match "^\s+socketEuler:") {
            $insertAfter = $i
            break
        }
    }

    if ($insertAfter -lt 0) {
        throw "socketEuler line not found for partId=$partId"
    }

    $Counters.xfiMetadataPromoted++
    $insert = New-Object System.Collections.Generic.List[string]
    $insert.Add("    hasXfiMetadata: 1") | Out-Null
    $insert.Add(("    xfiPath: {0}" -f (ConvertTo-YamlScalar $xfi.xfi_path))) | Out-Null
    $insert.Add(("    xfiHeader: {0}" -f (ConvertTo-YamlScalar $xfi.xfi_header))) | Out-Null
    $insert.Add(("    xfiHeaderKind: {0}" -f (ConvertTo-YamlScalar $xfi.xfi_header_kind))) | Out-Null
    $insert.Add(("    xfiAttachSlot: {0}" -f (ConvertTo-YamlScalar $xfi.attachSlot))) | Out-Null
    $insert.Add(("    xfiAttachVariant: {0}" -f (ConvertTo-YamlScalar $xfi.attachVariant))) | Out-Null
    $insert.Add(("    xfiTransformCount: {0}" -f ([int]$xfi.transformCount))) | Out-Null
    $insert.Add(("    xfiTransformTranslations: {0}" -f (ConvertTo-YamlScalar $xfi.transformTranslations))) | Out-Null
    $insert.Add(("    xfiDirectionRangeCount: {0}" -f ([int]$xfi.directionRangeCount))) | Out-Null
    $insert.Add(("    xfiDirectionRanges: {0}" -f (ConvertTo-YamlScalar $xfi.directionRanges))) | Out-Null

    if ($null -ne $proposal) {
        if (-not [string]::IsNullOrWhiteSpace($proposal.qualityFlag)) {
            $insert.Add(("    xfiSocketQuality: {0}" -f (ConvertTo-YamlScalar $proposal.qualityFlag))) | Out-Null
        }

        if (-not [string]::IsNullOrWhiteSpace($proposal.primarySocketName)) {
            $insert.Add(("    xfiSocketName: {0}" -f (ConvertTo-YamlScalar $proposal.primarySocketName))) | Out-Null
        }

        if (-not [string]::IsNullOrWhiteSpace($proposal.proposedSocketOffset)) {
            if ($proposal.qualityFlag -eq "xfi_body_top_socket_candidate") {
                $insert.Add("    hasFrameTopSocket: 1") | Out-Null
                $insert.Add(("    frameTopSocketOffset: {0}" -f (ConvertTo-YamlVector $proposal.proposedSocketOffset))) | Out-Null
                $Counters.frameTopSockets++
            }
            elseif ($proposal.qualityFlag -eq "xfi_leg_body_socket_candidate" -or $proposal.qualityFlag -eq "xfi_named_attach_socket_candidate") {
                $insert.Add("    hasXfiAttachSocket: 1") | Out-Null
                $insert.Add(("    xfiAttachSocketOffset: {0}" -f (ConvertTo-YamlVector $proposal.proposedSocketOffset))) | Out-Null

                if ($proposal.qualityFlag -eq "xfi_leg_body_socket_candidate") {
                    $Counters.legBodySockets++
                }
                else {
                    $Counters.namedAttachSockets++
                }
            }
        }
        elseif ($proposal.qualityFlag -eq "xfi_weapon_direction_only") {
            $Counters.weaponDirectionOnly++
        }
        elseif ($proposal.qualityFlag -eq "xfi_reference_only") {
            $Counters.referenceOnly++
        }
    }

    $before = if ($insertAfter -gt 0) { $cleaned[0..$insertAfter] } else { @($cleaned[0]) }
    $after = if ($insertAfter + 1 -lt $cleaned.Count) { $cleaned[($insertAfter + 1)..($cleaned.Count - 1)] } else { @() }
    return ,@($before + @($insert) + $after)
}

foreach ($path in @($XfiManifestPath, $ProposalPath, $PartCatalogPath, $AlignmentAssetPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file not found: $path"
    }
}

$repoRoot = (Resolve-Path -LiteralPath ".").Path
$assetFullPath = (Resolve-Path -LiteralPath $AlignmentAssetPath).Path
if (-not $assetFullPath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Alignment asset is outside repo: $assetFullPath"
}

$xfiByPartId = @{}
$xfiBySourcePath = @{}
foreach ($row in (Import-Csv -LiteralPath $XfiManifestPath)) {
    if ($row.parseStatus -eq "parsed" -and -not [string]::IsNullOrWhiteSpace($row.source_relative_path)) {
        $sourceKey = ConvertTo-SourceKey $row.source_relative_path
        $xfiBySourcePath[$sourceKey] = $row
    }

    if ([string]::IsNullOrWhiteSpace($row.partId) -or $row.parseStatus -ne "parsed") {
        continue
    }

    $xfiByPartId[$row.partId] = $row
}

$sourcePathByPartId = @{}
foreach ($row in (Import-Csv -LiteralPath $PartCatalogPath)) {
    if ([string]::IsNullOrWhiteSpace($row.partId) -or [string]::IsNullOrWhiteSpace($row.source_relative_path)) {
        continue
    }

    $sourcePathByPartId[$row.partId] = $row.source_relative_path
}

$proposalByPartId = @{}
foreach ($row in (Import-Csv -LiteralPath $ProposalPath)) {
    if ([string]::IsNullOrWhiteSpace($row.partId)) {
        continue
    }

    $proposalByPartId[$row.partId] = $row
}

$counters = @{
    assetEntries = 0
    xfiMetadataPromoted = 0
    xfiMatchedBySourcePath = 0
    entriesWithoutXfi = 0
    frameTopSockets = 0
    legBodySockets = 0
    namedAttachSockets = 0
    weaponDirectionOnly = 0
    referenceOnly = 0
}

$lines = @(Get-Content -LiteralPath $AlignmentAssetPath)
$output = New-Object System.Collections.Generic.List[string]
$currentBlock = New-Object System.Collections.Generic.List[string]
$inEntry = $false

foreach ($line in $lines) {
    if ($line -match "^\s+- partId:") {
        if ($inEntry) {
            foreach ($updatedLine in (Update-EntryBlock -Block @($currentBlock) -XfiByPartId $xfiByPartId -XfiBySourcePath $xfiBySourcePath -SourcePathByPartId $sourcePathByPartId -ProposalByPartId $proposalByPartId -Counters $counters)) {
                $output.Add($updatedLine) | Out-Null
            }

            $currentBlock.Clear()
        }

        $inEntry = $true
        $currentBlock.Add($line) | Out-Null
        continue
    }

    if ($inEntry) {
        $currentBlock.Add($line) | Out-Null
    }
    else {
        $output.Add($line) | Out-Null
    }
}

if ($inEntry) {
    foreach ($updatedLine in (Update-EntryBlock -Block @($currentBlock) -XfiByPartId $xfiByPartId -XfiBySourcePath $xfiBySourcePath -SourcePathByPartId $sourcePathByPartId -ProposalByPartId $proposalByPartId -Counters $counters)) {
        $output.Add($updatedLine) | Out-Null
    }
}

Set-Content -LiteralPath $AlignmentAssetPath -Value $output -Encoding UTF8

$report = New-Object System.Collections.Generic.List[string]
$report.Add("# Nova1492 XFI Unity Promotion Report") | Out-Null
$report.Add("") | Out-Null
$report.Add(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date))) | Out-Null
$report.Add("") | Out-Null
$report.Add("- input XFI manifest: $XfiManifestPath") | Out-Null
$report.Add("- input proposal: $ProposalPath") | Out-Null
$report.Add("- input part catalog: $PartCatalogPath") | Out-Null
$report.Add("- target asset: $AlignmentAssetPath") | Out-Null
$report.Add("") | Out-Null
$report.Add("## Result") | Out-Null
$report.Add("") | Out-Null
$report.Add("| metric | count |") | Out-Null
$report.Add("|---|---:|") | Out-Null
$report.Add(("| asset entries scanned | {0} |" -f $counters.assetEntries)) | Out-Null
$report.Add(("| XFI metadata promoted | {0} |" -f $counters.xfiMetadataPromoted)) | Out-Null
$report.Add(("| XFI matched by source path fallback | {0} |" -f $counters.xfiMatchedBySourcePath)) | Out-Null
$report.Add(("| entries without parsed XFI | {0} |" -f $counters.entriesWithoutXfi)) | Out-Null
$report.Add(("| frame body.top sockets promoted | {0} |" -f $counters.frameTopSockets)) | Out-Null
$report.Add(("| mobility legs.body sockets promoted | {0} |" -f $counters.legBodySockets)) | Out-Null
$report.Add(("| named attach sockets preserved | {0} |" -f $counters.namedAttachSockets)) | Out-Null
$report.Add(("| weapon direction-only metadata preserved | {0} |" -f $counters.weaponDirectionOnly)) | Out-Null
$report.Add(("| reference-only metadata preserved | {0} |" -f $counters.referenceOnly)) | Out-Null
$report.Add("") | Out-Null
$report.Add("## Notes") | Out-Null
$report.Add("") | Out-Null
$report.Add("- body.top and legs.body sockets are promoted to runtime-readable fields.") | Out-Null
$report.Add("- named attach sockets are preserved in Unity data, but not used by runtime placement until parent body slot mapping is promoted.") | Out-Null
$report.Add("- boundsSize and boundsCenter were removed from the alignment asset because preview placement no longer uses bounds-derived sockets.") | Out-Null

Set-Content -LiteralPath $ReportPath -Value $report -Encoding UTF8

[pscustomobject]@{
    success = $true
    assetEntries = $counters.assetEntries
    xfiMetadataPromoted = $counters.xfiMetadataPromoted
    xfiMatchedBySourcePath = $counters.xfiMatchedBySourcePath
    entriesWithoutXfi = $counters.entriesWithoutXfi
    frameTopSockets = $counters.frameTopSockets
    legBodySockets = $counters.legBodySockets
    namedAttachSockets = $counters.namedAttachSockets
    weaponDirectionOnly = $counters.weaponDirectionOnly
    referenceOnly = $counters.referenceOnly
    asset = $AlignmentAssetPath
    report = $ReportPath
} | ConvertTo-Json -Depth 4
