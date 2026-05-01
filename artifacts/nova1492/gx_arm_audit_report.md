# Nova1492 Arm Weapon GX Audit Report

> generated: 2026-05-01 11:21:43

- converter version: `gx-pipeline-v22`
- audited rows: 52
- high risk: 0
- review risk: 3
- pass: 49
- manifest: `artifacts/nova1492/gx_arm_audit_manifest.csv`
- hierarchy: `artifacts/nova1492/gx_arm_audit_hierarchy.csv`

## High Risk

| part | name | verdict | flags | counts | pose | dropped | bounds |
|---|---|---|---|---:|---|---:|---|

## Review Risk

| part | name | verdict | flags | counts | pose | dropped | bounds |
|---|---|---|---|---:|---|---:|---|
| `nova_fire_arm24_bzk` | 바주카 | review | `depth_outlier;center_z_outlier` | 566/444 | 10/detected/false | 0 | `-0.808673;-0.426651;-0.855639|0.948104;1.26147;4.982535` |
| `nova_fire_arm32_sppoo` | 스핏파이어 | review | `center_x_outlier` | 705/386 | 7/detected/false | 0 | `-1.102856;-0.538849;-0.816583|0.596122;0.877524;1.134423` |
| `nova_fire_s_arm52_bzk` | 바주카S | review | `depth_outlier;center_z_outlier` | 566/444 | 10/detected/false | 0 | `-0.808673;-0.426651;-0.855639|0.948104;1.26147;4.982535` |

## GX Pose Recovery Candidates

| part | name | verdict | flags | counts | pose | dropped | bounds |
|---|---|---|---|---:|---|---:|---|

## Notes

- `high` means a mechanical issue must be investigated before visual acceptance.
- `review` means the row is mechanically convertible but should be included in manual capture review.
- `gx_pose_unapplied_candidate` means the GX has 1782 pose blocks, but the default converter did not apply them because that part still needs visual proof or a part-specific hierarchy rule.
- `--gx-pose-mode force` is an investigation option only; do not promote it to default until capture comparison passes.
- Visual acceptance remains blocked until the capture sheet is manually compared.
