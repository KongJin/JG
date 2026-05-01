# Nova1492 Body/Arm Visual Sweep

> generated: 2026-05-01

This sweep reviewed the split contact sheets:

- `artifacts/nova1492/review-candidates/body-pages/body-page-01.png` through `body-page-06.png`
- `artifacts/nova1492/review-candidates/arm-pages/arm-page-01.png` through `arm-page-07.png`

It is a precheck only. Manual acceptance still belongs in:

- `artifacts/nova1492/gx_body_manual_review.csv`
- `artifacts/nova1492/gx_arm_manual_review.csv`

## Findings

| result | category | part | name | reason | evidence |
|---|---|---|---|---|---|
| excluded | arm | `nova_fire_arm23_rkog` | Recoil Gun | The model first rendered with a full grid/wire texture. `topn3.tga` and `n_topn3.tga` texture overrides removed the grid but still left a severe black plate, so the part was excluded from the playable catalog. Source GX and converted model artifacts are preserved. | `rkog-variant-comparison.png`; `rkog-variant-comparison-after.png`; final arm audit/capture rows exclude this part. |
| unsure | body | `nova_frame_body24_grbs` | Gravis | Iso view shows a small separated-looking lower shard. The N variant shows the same pattern and both use one kept mesh block with matching catalog counts, so it may be intended geometry or a thin face. | `body-page-02.png`; `body-page-06.png` |
| unsure | body | `nova_frame_n_body48_grbs` | Gravis N | Same pattern as base Gravis. No dropped blocks or count mismatch. | `body-page-06.png` |

## Non-Findings

- The previous bounds-review candidates `KDD N`, `RZMT N`, `Bazooka`, `Bazooka S`, and `Spitfire` did not show an obvious dropped-mesh or broken-hierarchy symptom in the sweep.
- Body and arm contact sheet generation remained mechanically complete.

## Next Step

Do not apply a global converter fix from this sweep alone.

Recommended next investigation target:

1. Keep `nova_fire_arm23_rkog` out of playable Garage unless an original-client comparison proves the black plate is intentional.
2. Continue manual review for the remaining `unsure` body rows.
