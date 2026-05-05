# Nova1492 Artifacts

This directory is the active evidence root for Nova1492 generated content handoff.

Use `current-index.json` first. It separates current pipeline inputs from historical source inventory so agents do not treat old bulk manifests as the present decision basis.

## Current Root

The root keeps the files still used by the current content handoff and Unity promotion flow, including:

- `gx_asset_classification.csv`
- `nova_unitpart_xfi_manifest.csv`
- `nova_xfi_alignment_proposal.csv`
- `nova_part_catalog.csv`
- `nova_part_alignment.csv`
- `gx_conversion_manifest.csv`
- `gx_pipeline_state.csv`
- audit manifests, hierarchy CSVs, manual review CSVs, and reports

PNG capture tools overwrite fixed evidence paths. Audit captures emit contact sheets instead of per-part PNG files; Garage humanoid samples keep one 3-view strip per sample, capped by the six fixed sample IDs. Use `tools/workflow/Limit-PngArtifacts.ps1` only for manual capture cleanup.

## Archive

Historical source inventory and broad staging evidence were compressed into:

- `archive/large-source-manifests-20260505.zip`
- `archive/large-source-manifests-20260505.manifest.json`

Those files are not current acceptance evidence. Open the manifest only when you explicitly need to recover an archived source inventory or staging report.
