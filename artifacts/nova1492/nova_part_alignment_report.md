# Nova1492 Part Alignment Report

> generated: 2026-04-26 23:14:12

## Summary

- alignment catalog: `Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset`
- alignment csv: `artifacts/nova1492/nova_part_alignment.csv`
- visual catalog loaded: true
- total entries: 321
- duplicate ids: 0
- missing prefab: 0
- missing renderer: 2
- static balance: pass

## Slot Summary

| slot | total | auto_ok | needs_review | missing_prefab | missing_renderer |
|---|---:|---:|---:|---:|---:|
| Frame | 100 | 99 | 1 | 0 | 0 |
| Firepower | 160 | 157 | 1 | 0 | 2 |
| Mobility | 61 | 61 | 0 | 0 | 0 |

## Fixed Sample Combination

| slot | requested id | resolved id | quality | socket offset | socket euler |
|---|---|---|---|---|---|
| Frame | `nova_frame_body23_ms` | `nova_frame_body23_ms` | auto_ok | `0;0.141021;0` | `0;0;0` |
| Firepower | `nova_fire_arm43_przso` | `nova_fire_arm43_przso` | auto_ok | `0;-0.107882;0` | `0;0;90` |
| Mobility | `nova_mob_legs24_sts` | `nova_mob_legs24_sts` | auto_ok | `0;0.2888;0` | `0;0;0` |

## Review Candidates

| slot | id | quality | reason | bounds | normalized scale |
|---|---|---|---|---|---:|
| Firepower | `nova_fire_front` | missing_renderer | bounds max dimension is zero | `0;0;0` | 1 |
| Firepower | `nova_fire_front_cbb7fd7f` | missing_renderer | bounds max dimension is zero | `0;0;0` | 1 |
| Firepower | `nova_fire_review` | needs_review_flat | min/max ratio 0 is below 0.04 | `0.82;0;0.82` | 1 |
| Frame | `nova_frame_base_overwork` | needs_review_flat | min/max ratio 0 is below 0.04 | `0.376661;0.95;0` | 1 |
