# Nova1492 GX Conversion Summary

> generated: 2026-04-30 11:10:54

- stage: `convert`
- converter version: `gx-pipeline-v14`
- source root: `C:\Program Files (x86)\Nova1492`
- output root: `Assets/Art/Nova1492/GXConverted`
- total GX files: 49
- converted: 0
- analyzed: 0
- skipped: 49
- failed: 0
- repair applied: 14
- needs review: 0
- manifest: `artifacts/nova1492/gx_conversion_manifest.csv`
- pipeline state: `artifacts/nova1492/gx_pipeline_state.csv`

## Notes

- Conversion uses a heuristic parser for the confirmed GX layout: split position, normal, UV, and uint16 index streams.
- Unit legs use an XFI-aware assembly pass that drops tiny direction/helper mesh planes while preserving parsed assembly blocks.
- Catalog-driven runs can use --changed-only to skip rows whose source hash, catalog row hash, and converter version are unchanged.
- Hierarchy repairs are rule-based and recorded in repair_rule/repair_reason instead of hidden per-part sculpture.
- UV V is flipped during OBJ export to match the DAE comparison sample.
- Failed rows are preserved in the manifest for later parser improvements.
