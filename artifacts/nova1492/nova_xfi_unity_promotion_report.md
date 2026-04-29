# Nova1492 XFI Unity Promotion Report

> generated: 2026-04-29 09:24:55

- input XFI manifest: artifacts/nova1492/nova_unitpart_xfi_manifest.csv
- input proposal: artifacts/nova1492/nova_xfi_alignment_proposal.csv
- input part catalog: artifacts/nova1492/nova_part_catalog.csv
- target asset: Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset

## Result

| metric | count |
|---|---:|
| asset entries scanned | 222 |
| XFI metadata promoted | 222 |
| XFI matched by source path fallback | 0 |
| entries without parsed XFI | 0 |
| frame body.top sockets promoted | 64 |
| mobility legs.body sockets promoted | 60 |
| named attach sockets preserved | 0 |
| weapon direction-only metadata preserved | 85 |
| reference-only metadata preserved | 13 |

## Notes

- body.top and legs.body sockets are promoted to runtime-readable fields.
- named attach sockets are preserved in Unity data, but not used by runtime placement until parent body slot mapping is promoted.
- boundsSize and boundsCenter were removed from the alignment asset because preview placement no longer uses bounds-derived sockets.
