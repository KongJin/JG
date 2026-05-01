# Nova1492 Body/Arm Review Candidate Precheck

> generated: 2026-05-01

This is a Codex precheck for audit `review` rows only. It does not replace
manual visual acceptance in `gx_body_manual_review.csv` or
`gx_arm_manual_review.csv`.

## Inputs

- Body audit: `artifacts/nova1492/gx_body_audit_manifest.csv`
- Arm audit: `artifacts/nova1492/gx_arm_audit_manifest.csv`
- Candidate sheet: `artifacts/nova1492/review-candidates/gx-body-arm-review-candidates.png`
- Variant comparison sheet: `artifacts/nova1492/review-candidates/gx-variant-comparison.png`

## Candidate Results

| category | part | name | audit flags | precheck | notes |
|---|---|---|---|---|---|
| body | `nova_frame_n_body42_kdd` | KDD N | `center_x_outlier` | plausible | Matches the base KDD family by count and similar bounds. No dropped blocks, no catalog mismatch. |
| body | `nova_frame_n_body49_rzmt` | RZMT N | `center_x_outlier` | plausible | Matches the base RZMT family by count and similar bounds. No dropped blocks, no catalog mismatch. |
| arm | `nova_fire_arm24_bzk` | Bazooka | `depth_outlier;center_z_outlier` | plausible | Long barrel explains the depth/center Z outlier. No dropped blocks, no catalog mismatch. |
| arm | `nova_fire_s_arm52_bzk` | Bazooka S | `depth_outlier;center_z_outlier` | plausible | Exact same count/bounds pattern as Bazooka. No dropped blocks, no catalog mismatch. |
| arm | `nova_fire_arm32_sppoo` | Spitfire | `center_x_outlier` | plausible, needs visual review | Asymmetric mesh blocks explain the center X outlier. No dropped blocks, no catalog mismatch. |

## Current Status

- No mechanical high-risk issue was found in these five candidates.
- No automatic `match` judgment is recorded.
- Acceptance remains blocked until manual visual review is filled in.
