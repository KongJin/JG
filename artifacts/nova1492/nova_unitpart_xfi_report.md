# Nova1492 UnitPart XFI Analysis

> generated: 2026-04-29 09:24:46

- source root: `External/Nova1492Raw/`
- classification: `artifacts/nova1492/gx_asset_classification.csv`
- part catalog: `artifacts/nova1492/nova_part_catalog.csv`
- manifest: `artifacts/nova1492/nova_unitpart_xfi_manifest.csv`
- source names: `External/Nova1492Raw/datan/kr/nvpartdesc.dat`

## Coverage

| metric | count |
|---|---:|
| UnitPart rows | 383 |
| with XFI | 379 |
| missing XFI | 4 |
| mapped Korean name by inferred code | 145 |

## Category Counts

| category | count | with XFI | mapped names |
|---|---:|---:|---:|
| UnitParts/Accessories | 62 | 62 | 0 |
| UnitParts/ArmWeapons | 160 | 160 | 53 |
| UnitParts/Bases | 28 | 24 | 0 |
| UnitParts/Bodies | 72 | 72 | 43 |
| UnitParts/Legs | 61 | 61 | 49 |

## XFI Header Counts

| header | count |
|---|---:|
| 4 | 87 |
| 1 | 73 |
| -1 | 72 |
| 0 | 61 |
| body | 20 |
| larm|0 | 16 |
| rarm|0 | 16 |
| front|0 | 6 |
| top|0 | 6 |
| (missing) | 4 |
| lshd|1 | 4 |
| rshd|1 | 4 |
| 5 | 2 |
| 6 | 2 |
| legs | 2 |
| lshd | 2 |
| lshd_addon|0 | 2 |
| rshd | 2 |
| top | 2 |

## Interpretation

- `.GX` contains the mesh stream; `.xfi` is text metadata used for UnitPart attachment semantics.
- Numeric XFI headers such as `0`, `1`, and `4` line up with Mobility/Frame/Firepower style parts.
- Named XFI headers such as `body`, `front`, `top`, `larm`, `rarm`, `lshd`, and `rshd` expose explicit attachment sockets.
- Matrix rows provide candidate socket transforms. The fourth row of each 4x4 matrix is captured as `transformTranslations`.
- Direction rows such as `0:90:105` are preserved as `directionRanges`; these likely represent facing/animation angle bands.
