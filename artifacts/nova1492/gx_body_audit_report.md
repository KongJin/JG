# Nova1492 Body GX Audit Report

> generated: 2026-05-01 10:20:27

- converter version: `gx-pipeline-v20`
- audited rows: 43
- high risk: 0
- review risk: 2
- pass: 41
- manifest: `artifacts/nova1492/gx_body_audit_manifest.csv`
- hierarchy: `artifacts/nova1492/gx_body_audit_hierarchy.csv`

## High Risk

| part | name | verdict | flags | counts | pose | dropped | bounds |
|---|---|---|---|---:|---|---:|---|

## Review Risk

| part | name | verdict | flags | counts | pose | dropped | bounds |
|---|---|---|---|---:|---|---:|---|
| `nova_frame_n_body42_kdd` | 쿼더덱N | review | `center_x_outlier` | 265/204 | 1/detected/false | 0 | `-0.800103;-0.130986;-1.366657|0.737365;0.648877;0.499684` |
| `nova_frame_n_body49_rzmt` | 레지먼트N | review | `center_x_outlier` | 118/108 | 1/detected/false | 0 | `-0.128576;-0.035078;-0.356227|0.17011;0.457166;0.49161` |

## GX Pose Recovery Candidates

| part | name | verdict | flags | counts | pose | dropped | bounds |
|---|---|---|---|---:|---|---:|---|

## Notes

- `high` means a mechanical issue must be investigated before visual acceptance.
- `review` means the row is mechanically convertible but should be included in manual capture review.
- `gx_pose_unapplied_candidate` means the GX has 1782 pose blocks, but the default converter did not apply them because that part still needs visual proof or a part-specific hierarchy rule.
- `--gx-pose-mode force` is an investigation option only; do not promote it to default until capture comparison passes.
- Visual acceptance remains blocked until the capture sheet is manually compared.
