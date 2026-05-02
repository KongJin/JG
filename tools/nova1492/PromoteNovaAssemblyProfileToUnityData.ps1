param(
    [string] $ProfilePath = "artifacts/nova1492/assembly-profile/nova_assembly_profile.csv",
    [string] $AlignmentAssetPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset",
    [string] $ReportPath = "artifacts/nova1492/assembly-profile/nova_assembly_profile_unity_promotion_report.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-YamlScalar {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ("'{0}'" -f $Value.Replace("'", "''"))
}

function ConvertTo-YamlVector {
    param([string] $Value, [string] $Fallback)

    $source = if ([string]::IsNullOrWhiteSpace($Value)) { $Fallback } else { $Value }
    $parts = @($source -split ";" | ForEach-Object { $_.Trim() })
    if ($parts.Count -ne 3) {
        throw "Expected vector in x;y;z format, got: $source"
    }

    return ("{{x: {0}, y: {1}, z: {2}}}" -f $parts[0], $parts[1], $parts[2])
}

function Remove-AssemblyProfileFields {
    param([string[]] $Block)

    return @(
        $Block |
            Where-Object {
                $_ -notmatch "^\s+assemblySourceSlotCode:" -and
                $_ -notmatch "^\s+assemblySlotMode:" -and
                $_ -notmatch "^\s+assemblyAnchorMode:" -and
                $_ -notmatch "^\s+assemblyLocalOffset:" -and
                $_ -notmatch "^\s+assemblyLocalEuler:" -and
                $_ -notmatch "^\s+assemblyLocalScale:" -and
                $_ -notmatch "^\s+assemblyConfidence:" -and
                $_ -notmatch "^\s+assemblyEvidencePath:" -and
                $_ -notmatch "^\s+assemblyReviewResult:" -and
                $_ -notmatch "^\s{6,}\S"
            }
    )
}

function Update-EntryBlock {
    param(
        [string[]] $Block,
        [hashtable] $ProfileByPartId,
        [hashtable] $Counters
    )

    $partLine = $Block | Where-Object { $_ -match "^\s+- partId:" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($partLine)) {
        return ,$Block
    }

    $partId = ($partLine -replace "^\s+- partId:\s*", "").Trim()
    $Counters.assetEntries++
    $cleaned = @(Remove-AssemblyProfileFields -Block $Block)
    if (-not $ProfileByPartId.ContainsKey($partId)) {
        $Counters.entriesWithoutProfile++
        return ,$cleaned
    }

    $profile = $ProfileByPartId[$partId]
    $insertAfter = -1
    for ($i = 0; $i -lt $cleaned.Count; $i++) {
        if ($cleaned[$i] -match "^\s+reviewReason:") {
            $insertAfter = $i
            break
        }
    }

    if ($insertAfter -lt 0) {
        throw "reviewReason line not found for partId=$partId"
    }

    $Counters.profilePromoted++
    if ($profile.confidence -eq "review") {
        $Counters.reviewConfidence++
    }
    elseif ($profile.confidence -eq "derived") {
        $Counters.derivedConfidence++
    }

    $insert = New-Object System.Collections.Generic.List[string]
    $insert.Add(("    assemblySourceSlotCode: {0}" -f (ConvertTo-YamlScalar $profile.source_slot_code))) | Out-Null
    $insert.Add(("    assemblySlotMode: {0}" -f (ConvertTo-YamlScalar $profile.slot_mode))) | Out-Null
    $insert.Add(("    assemblyAnchorMode: {0}" -f (ConvertTo-YamlScalar $profile.anchor_mode))) | Out-Null
    $insert.Add(("    assemblyLocalOffset: {0}" -f (ConvertTo-YamlVector -Value $profile.local_offset -Fallback "0;0;0"))) | Out-Null
    $insert.Add(("    assemblyLocalEuler: {0}" -f (ConvertTo-YamlVector -Value $profile.local_rotation -Fallback "0;0;0"))) | Out-Null
    $insert.Add(("    assemblyLocalScale: {0}" -f (ConvertTo-YamlVector -Value $profile.local_scale -Fallback "1;1;1"))) | Out-Null
    $insert.Add(("    assemblyConfidence: {0}" -f (ConvertTo-YamlScalar $profile.confidence))) | Out-Null
    $insert.Add(("    assemblyEvidencePath: {0}" -f (ConvertTo-YamlScalar $profile.evidence_path))) | Out-Null
    $insert.Add(("    assemblyReviewResult: {0}" -f (ConvertTo-YamlScalar $profile.review_result))) | Out-Null

    $before = if ($insertAfter -gt 0) { $cleaned[0..$insertAfter] } else { @($cleaned[0]) }
    $after = if ($insertAfter + 1 -lt $cleaned.Count) { $cleaned[($insertAfter + 1)..($cleaned.Count - 1)] } else { @() }
    return ,@($before + @($insert) + $after)
}

foreach ($path in @($ProfilePath, $AlignmentAssetPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file not found: $path"
    }
}

$profileByPartId = @{}
foreach ($row in (Import-Csv -LiteralPath $ProfilePath)) {
    if (-not [string]::IsNullOrWhiteSpace($row.part_id)) {
        $profileByPartId[$row.part_id] = $row
    }
}

$counters = @{
    assetEntries = 0
    profilePromoted = 0
    entriesWithoutProfile = 0
    reviewConfidence = 0
    derivedConfidence = 0
}

$lines = @(Get-Content -LiteralPath $AlignmentAssetPath)
$output = New-Object System.Collections.Generic.List[string]
$currentBlock = New-Object System.Collections.Generic.List[string]
$inEntry = $false

foreach ($line in $lines) {
    if ($line -match "^\s+- partId:") {
        if ($inEntry) {
            foreach ($updatedLine in (Update-EntryBlock -Block @($currentBlock) -ProfileByPartId $profileByPartId -Counters $counters)) {
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
    foreach ($updatedLine in (Update-EntryBlock -Block @($currentBlock) -ProfileByPartId $profileByPartId -Counters $counters)) {
        $output.Add($updatedLine) | Out-Null
    }
}

Set-Content -LiteralPath $AlignmentAssetPath -Value $output -Encoding UTF8

$reportDirectory = Split-Path -Parent $ReportPath
if ($reportDirectory -and -not (Test-Path -LiteralPath $reportDirectory)) {
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add("# Nova1492 Assembly Profile Unity Promotion Report") | Out-Null
$report.Add("") | Out-Null
$report.Add(("> generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date))) | Out-Null
$report.Add("") | Out-Null
$report.Add(("- input profile: ``{0}``" -f $ProfilePath)) | Out-Null
$report.Add(("- target alignment asset: ``{0}``" -f $AlignmentAssetPath)) | Out-Null
$report.Add("") | Out-Null
$report.Add("## Result") | Out-Null
$report.Add("") | Out-Null
$report.Add("| metric | count |") | Out-Null
$report.Add("|---|---:|") | Out-Null
$report.Add(("| asset entries scanned | {0} |" -f $counters.assetEntries)) | Out-Null
$report.Add(("| profile rows promoted | {0} |" -f $counters.profilePromoted)) | Out-Null
$report.Add(("| entries without profile | {0} |" -f $counters.entriesWithoutProfile)) | Out-Null
$report.Add(("| review confidence | {0} |" -f $counters.reviewConfidence)) | Out-Null
$report.Add(("| derived confidence | {0} |" -f $counters.derivedConfidence)) | Out-Null
$report.Add("") | Out-Null
$report.Add("## Guardrail") | Out-Null
$report.Add("") | Out-Null
$report.Add("- Promoted fields are metadata; manual visual acceptance still depends on latest captures and review CSV.") | Out-Null
Set-Content -LiteralPath $ReportPath -Value $report -Encoding UTF8

if ($counters.profilePromoted -ne $profileByPartId.Count) {
    throw "Not every profile row was promoted. profileRows=$($profileByPartId.Count) promoted=$($counters.profilePromoted)"
}

[pscustomobject]@{
    success = $true
    assetEntries = $counters.assetEntries
    profilePromoted = $counters.profilePromoted
    entriesWithoutProfile = $counters.entriesWithoutProfile
    report = $ReportPath
} | ConvertTo-Json -Depth 4
