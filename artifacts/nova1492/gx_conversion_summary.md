# Nova1492 GX Conversion Summary

> generated: 2026-04-29 23:02:32

- source root: `C:\Program Files (x86)\Nova1492`
- output root: `Assets/Art/Nova1492/GXConverted`
- total GX files: 145
- converted: 145
- failed: 0
- manifest: `artifacts/nova1492/gx_conversion_manifest.csv`

## Notes

- Conversion uses a heuristic parser for the confirmed GX layout: split position, normal, UV, and uint16 index streams.
- Unit legs use an XFI-aware assembly pass that drops tiny direction/helper mesh planes while preserving parsed assembly blocks.
- UV V is flipped during OBJ export to match the DAE comparison sample.
- Failed rows are preserved in the manifest for later parser improvements.
