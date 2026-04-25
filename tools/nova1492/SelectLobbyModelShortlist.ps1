param(
    [string] $ClassificationPath = "artifacts/nova1492/gx_asset_classification.csv",
    [string] $OutputCsvPath = "artifacts/nova1492/lobby_model_shortlist.csv",
    [string] $OutputMarkdownPath = "artifacts/nova1492/lobby_model_shortlist.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ClassificationPath)) {
    throw "Classification CSV not found: $ClassificationPath"
}

$rows = Import-Csv -LiteralPath $ClassificationPath
$bySource = @{}
foreach ($row in $rows) {
    $key = $row.source_relative_path.ToLowerInvariant()
    if (-not $bySource.ContainsKey($key)) {
        $bySource[$key] = $row
    }
}

$selections = @(
    @{ slot = "frame_body"; priority = 1; source = "datan\common\body23_ms.gx"; rationale = "Balanced torso silhouette, high enough detail for first Garage preview." },
    @{ slot = "frame_body"; priority = 2; source = "datan\common\body25_bosro.gx"; rationale = "Broad body candidate for checking camera framing and center mass." },
    @{ slot = "frame_body"; priority = 3; source = "datan\common\body37_ktn.gx"; rationale = "Angular body candidate with readable top shape." },
    @{ slot = "frame_body"; priority = 4; source = "datan\common\body11_kn.gx"; rationale = "Medium-detail fallback body below the first-pass triangle cap." },
    @{ slot = "frame_body"; priority = 5; source = "datan\common\B0002.GX"; rationale = "Base-shaped silhouette for comparing body versus platform framing." },

    @{ slot = "firepower"; priority = 1; source = "datan\common\arm43_przso.gx"; rationale = "Strong top-mounted weapon profile within the first-pass triangle cap." },
    @{ slot = "firepower"; priority = 2; source = "datan\common\arm20_rkto.gx"; rationale = "Launcher-like profile for firepower readability." },
    @{ slot = "firepower"; priority = 3; source = "datan\common\arm6_msg.gx"; rationale = "Compact weapon candidate for dense Garage card framing." },
    @{ slot = "firepower"; priority = 4; source = "datan\common\arm31_skokr.gx"; rationale = "Medium-detail alternative for slot-to-slot contrast." },
    @{ slot = "firepower"; priority = 5; source = "datan\common\0913missile.GX"; rationale = "Projectile candidate for thumbnail or muzzle detail comparison." },

    @{ slot = "mobility"; priority = 1; source = "datan\common\legs24_sts.gx"; rationale = "Readable lower-body silhouette, useful for first assembled unit preview." },
    @{ slot = "mobility"; priority = 2; source = "datan\common\legs20_spod.gx"; rationale = "Spider/stance-like profile for mobility contrast." },
    @{ slot = "mobility"; priority = 3; source = "datan\common\legs7_hb.gx"; rationale = "Heavier lower-body option below the first-pass triangle cap." },
    @{ slot = "mobility"; priority = 4; source = "datan\common\legs13_krz.gx"; rationale = "Alternate leg profile with comparable budget to the top candidates." },
    @{ slot = "mobility"; priority = 5; source = "datan\common\legs25_kd.gx"; rationale = "Lower-detail fallback candidate for mobile preview safety." },

    @{ slot = "ambient_prop"; priority = 1; source = "datan\common\tube.GX"; rationale = "Simple mechanical prop candidate for a workshop background accent." },
    @{ slot = "ambient_prop"; priority = 2; source = "datan\common\parasol.GX"; rationale = "Distinct prop silhouette for quick scale and material sanity checks." },
    @{ slot = "ambient_prop"; priority = 3; source = "datan\common\tree01.gx"; rationale = "Low-poly environmental prop for non-Garage decorative comparison." },
    @{ slot = "ambient_prop"; priority = 4; source = "datan\common\soccer.gx"; rationale = "Small object candidate that should not dominate Lobby UI hierarchy." },
    @{ slot = "ambient_prop"; priority = 5; source = "datan\common\dol05.GX"; rationale = "Compact prop fallback for background-placement tests." }
)

$shortlist = foreach ($selection in $selections) {
    $key = $selection.source.ToLowerInvariant()
    if (-not $bySource.ContainsKey($key)) {
        throw "Selected source not found in classification CSV: $($selection.source)"
    }

    $row = $bySource[$key]
    if ($row.category -eq "Unknown/Review") {
        throw "Selected source is still Unknown/Review: $($selection.source)"
    }

    [pscustomobject] @{
        slot = $selection.slot
        priority = $selection.priority
        category = $row.category
        source_relative_path = $row.source_relative_path
        model_path = $row.model_path
        vertices = [int] $row.vertices
        triangles = [int] $row.triangles
        texture_output = $row.texture_output
        rationale = $selection.rationale
        status = "shortlisted"
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

$shortlist | Export-Csv -LiteralPath $OutputCsvPath -NoTypeInformation -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# LobbyScene Nova1492 Model Shortlist")
$lines.Add("")
$lines.Add('> Generated by `tools/nova1492/SelectLobbyModelShortlist.ps1` from `artifacts/nova1492/gx_asset_classification.csv`.')
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| slot | count | triangle range |")
$lines.Add("|---|---:|---|")
foreach ($group in ($shortlist | Group-Object slot | Sort-Object Name)) {
    $triangles = @($group.Group | ForEach-Object { [int] $_.triangles })
    $lines.Add("| $($group.Name) | $($group.Count) | $($triangles | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum)-$($triangles | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum) |")
}

$lines.Add("")
$lines.Add("## Candidates")
$lines.Add("")
$lines.Add("| slot | priority | source | category | vertices | triangles | rationale |")
$lines.Add("|---|---:|---|---|---:|---:|---|")
foreach ($item in ($shortlist | Sort-Object slot, priority)) {
    $source = $item.source_relative_path.Replace("\", "/")
    $lines.Add("| $($item.slot) | $($item.priority) | ``$source`` | $($item.category) | $($item.vertices) | $($item.triangles) | $($item.rationale) |")
}

$lines.Add("")
$lines.Add("## First-Pass Policy")
$lines.Add("")
$lines.Add('- Do not include `Unknown/Review` assets.')
$lines.Add("- Keep first Garage preview candidates under 350 triangles.")
$lines.Add('- Treat `ambient_prop` as scene comparison material only; do not place it in the runtime Lobby UI until a capture review passes.')
$lines.Add("- Use explicit serialized references or a small catalog in the next phase; do not add runtime asset scans.")

Set-Content -LiteralPath $OutputMarkdownPath -Value $lines -Encoding UTF8

Write-Host "Wrote $OutputCsvPath"
Write-Host "Wrote $OutputMarkdownPath"
