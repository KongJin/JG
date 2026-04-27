# Nova1492 Raw Source Full Mirror Report

> generated: 2026-04-27

## Result

- Source: `C:\Program Files (x86)\Nova1492`
- Destination: `External/Nova1492Raw/`
- Copied files: 6000
- Copied bytes: 617954272
- Robocopy failures: 0

The raw source mirror is intentionally outside `Assets/` so Unity does not import executable/plugin-like files such as `.exe` and `.dll`.

## Manifests

- `artifacts/nova1492/raw_source_full_manifest.csv`
- `artifacts/nova1492/raw_source_extension_counts.csv`

## 3D Candidate Check

- Raw `.GX` files: 871
- Raw `.DAE` files: 4
- Existing GX conversion manifest coverage: all 871 `.GX` source files are represented.
- Existing GX conversion failures: 6
- `.DAE` files are staged under `Assets/Art/Nova1492/Models/Collada/Effects/CombatEffects/`.

## Full 3D Staging

- Manifest: `artifacts/nova1492/all_3d_model_staging_manifest.csv`
- Raw GX source root: `Assets/Art/Nova1492/Source3D/GX/`
- Raw DAE source root: `Assets/Art/Nova1492/Source3D/DAE/`
- Unity-readable DAE model root: `Assets/Art/Nova1492/Models/Collada/`
- Existing converted GX OBJ root: `Assets/Art/Nova1492/GXConverted/Models/`
- Staged source rows: 875
- Missing staged source paths: 0
- Missing converted/readable model paths: 0

Staging status:

| kind | status | count |
|---|---|---:|
| raw GX source | converted | 865 |
| raw GX source | failed | 6 |
| raw DAE source | unity_readable_source | 4 |

Category split:

| category | count |
|---|---:|
| `Characters/MobAndBoss` | 129 |
| `Effects/CombatEffects` | 121 |
| `Effects/Projectiles` | 17 |
| `Environment/Props` | 65 |
| `ItemsAndUi/Icons` | 36 |
| `UnitParts/Accessories` | 64 |
| `UnitParts/ArmWeapons` | 160 |
| `UnitParts/Bases` | 28 |
| `UnitParts/Bodies` | 72 |
| `UnitParts/Legs` | 61 |
| `Unknown/Review` | 122 |

Known GX conversion failures:

| source | bytes | error |
|---|---:|---|
| `datan/common/11.GX` | 133 | `no_valid_mesh_stream` |
| `datan/common/ap1m.gx` | 135 | `no_valid_mesh_stream` |
| `datan/common/ap2m.gx` | 135 | `no_valid_mesh_stream` |
| `datan/common/mobmis37.gx` | 1142 | `no_valid_mesh_stream` |
| `datan/common/터짐.GX` | 135 | `no_valid_mesh_stream` |
| `datan/common/����.GX` | 135 | `no_valid_mesh_stream` |

Raw `.DAE` files:

| source |
|---|
| `datan/common/Iv_exp_blue.DAE` |
| `datan/common/Iv_exp_orange.DAE` |
| `datan/common/Iv_exp_red.DAE` |
| `datan/common/Iv_exp_yellow.DAE` |
