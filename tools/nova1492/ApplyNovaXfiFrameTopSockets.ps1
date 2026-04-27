param(
    [string] $ProposalPath = "artifacts/nova1492/nova_xfi_alignment_proposal.csv",
    [string] $AlignmentAssetPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset",
    [string] $ReportPath = "artifacts/nova1492/nova_xfi_frame_top_socket_apply_report.md"
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

function Update-EntryBlock {
    param(
        [string[]] $Block,
        [hashtable] $Candidates
    )

    $partLine = $Block | Where-Object { $_ -match "^\s+- partId:" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($partLine)) {
        return ,$Block
    }

    $partId = ($partLine -replace "^\s+- partId:\s*", "").Trim()
    $cleaned = @(
        $Block |
            Where-Object {
                $_ -notmatch "^\s+hasFrameTopSocket:" -and
                $_ -notmatch "^\s+frameTopSocketOffset:" -and
                $_ -notmatch "^\s+xfiSocketQuality:" -and
                $_ -notmatch "^\s+xfiSocketName:"
            }
    )

    if (-not $Candidates.ContainsKey($partId)) {
        return ,$cleaned
    }

    $candidate = $Candidates[$partId]
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

    $insert = @(
        "    hasFrameTopSocket: 1",
        ("    frameTopSocketOffset: {0}" -f (ConvertTo-YamlVector $candidate.proposedSocketOffset)),
        ("    xfiSocketQuality: {0}" -f $candidate.qualityFlag),
        ("    xfiSocketName: {0}" -f $candidate.primarySocketName)
    )

    $before = if ($insertAfter -gt 0) { $cleaned[0..$insertAfter] } else { @($cleaned[0]) }
    $after = if ($insertAfter + 1 -lt $cleaned.Count) { $cleaned[($insertAfter + 1)..($cleaned.Count - 1)] } else { @() }
    return ,@($before + $insert + $after)
}

if (-not (Test-Path -LiteralPath $ProposalPath)) {
    throw "Proposal CSV not found: $ProposalPath"
}

if (-not (Test-Path -LiteralPath $AlignmentAssetPath)) {
    throw "Alignment asset not found: $AlignmentAssetPath"
}

$repoRoot = (Resolve-Path -LiteralPath ".").Path
$assetFullPath = (Resolve-Path -LiteralPath $AlignmentAssetPath).Path
if (-not $assetFullPath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Alignment asset is outside repo: $assetFullPath"
}

$candidateRows = @(
    Import-Csv -LiteralPath $ProposalPath |
        Where-Object {
            $_.qualityFlag -eq "xfi_body_top_socket_candidate" -and
            -not [string]::IsNullOrWhiteSpace($_.proposedSocketOffset)
        }
)

$candidates = @{}
foreach ($row in $candidateRows) {
    $candidates[$row.partId] = $row
}

$lines = @(Get-Content -LiteralPath $AlignmentAssetPath)
$output = New-Object System.Collections.Generic.List[string]
$currentBlock = New-Object System.Collections.Generic.List[string]
$inEntry = $false

foreach ($line in $lines) {
    if ($line -match "^\s+- partId:") {
        if ($inEntry) {
            foreach ($updatedLine in (Update-EntryBlock -Block @($currentBlock) -Candidates $candidates)) {
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
    foreach ($updatedLine in (Update-EntryBlock -Block @($currentBlock) -Candidates $candidates)) {
        $output.Add($updatedLine) | Out-Null
    }
}

Set-Content -LiteralPath $AlignmentAssetPath -Value $output -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Nova1492 XFI Frame Top Socket Apply Report")
$lines.Add("")
$lines.Add(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date)))
$lines.Add("")
$lines.Add('- input proposal: `artifacts/nova1492/nova_xfi_alignment_proposal.csv`')
$lines.Add('- target asset: `Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset`')
$lines.Add("")
$lines.Add("## Result")
$lines.Add("")
$lines.Add("| metric | count |")
$lines.Add("|---|---:|")
$lines.Add("| frame top socket candidates applied | $($candidateRows.Count) |")
$lines.Add("")
$lines.Add('Only `xfi_body_top_socket_candidate` rows were applied. Leg, named attach, and weapon direction proposals remain reference-only until the preview contract has dedicated fields for them.')

Set-Content -LiteralPath $ReportPath -Value $lines -Encoding UTF8

[pscustomobject]@{
    success = $true
    applied = $candidateRows.Count
    asset = $AlignmentAssetPath
    report = $ReportPath
} | ConvertTo-Json -Depth 4
