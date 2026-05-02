# Nova1492 Assembly Profile Report

> generated: 2026-05-02 01:45:19

- source root: `C:\Program Files (x86)\Nova1492`
- profile: `artifacts\nova1492\assembly-profile\nova_assembly_profile.csv`
- manual review: `artifacts\nova1492\assembly-profile\nova_assembly_profile_manual_review.csv`
- slot evidence: `artifacts\nova1492\assembly-profile\nova_assembly_slot_evidence.csv`

## Counts

| metric | count |
|---|---:|
| profile rows | 144 |
| slot-specific GX evidence files | 56 |
| pending manual review | 144 |
| review confidence | 52 |
| derived confidence | 92 |
| blocked confidence | 0 |

## Anchor Modes

| anchor_mode | count |
|---|---:|
| FrameTopSocket | 34 |
| HumanoidShellBoundsCenter | 30 |
| LegBodySocket | 49 |
| ShoulderPair | 31 |

## XFI Classes

| xfi_class | count |
|---|---:|
| direction-only | 52 |
| transform-bearing | 92 |

## Guardrail

- Direction-only firepower XFI rows are seeded as review data, not source placement truth.
- Known broken-looking single parts stay in the profile with review notes, but their visual acceptance belongs to GX audit if the part is broken before assembly.
- Actual acceptance remains blocked until latest capture and manual visual review record match.
