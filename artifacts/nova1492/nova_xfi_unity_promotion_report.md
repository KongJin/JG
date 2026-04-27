# Nova1492 XFI Unity Promotion Report

> generated: 2026-04-27 23:49:40

- input XFI manifest: artifacts/nova1492/nova_unitpart_xfi_manifest.csv
- input proposal: artifacts/nova1492/nova_xfi_alignment_proposal.csv
- input part catalog: artifacts/nova1492/nova_part_catalog.csv
- target asset: Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset

## Result

| metric | count |
|---|---:|
| asset entries scanned | 321 |
| XFI metadata promoted | 317 |
| XFI matched by source path fallback | 9 |
| entries without parsed XFI | 4 |
| frame body.top sockets promoted | 65 |
| mobility legs.body sockets promoted | 61 |
| named attach sockets preserved | 12 |
| weapon direction-only metadata preserved | 86 |
| reference-only metadata preserved | 93 |

## Notes

- body.top and legs.body sockets are promoted to runtime-readable fields.
- named attach sockets are preserved in Unity data, but not used by runtime placement until parent body slot mapping is promoted.
- boundsSize and boundsCenter were removed from the alignment asset because preview placement no longer uses bounds-derived sockets.
