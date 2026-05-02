# Nova1492 Assembly Profile Report

> generated: 2026-05-02 16:04:57

- source root: `C:\Program Files (x86)\Nova1492`
- profile: `artifacts\nova1492\assembly-profile\nova_assembly_profile.csv`
- manual review: `artifacts\nova1492\assembly-profile\nova_assembly_profile_manual_review.csv`
- slot evidence: `artifacts\nova1492\assembly-profile\nova_assembly_slot_evidence.csv`

## Counts

| metric | count |
|---|---:|
| profile rows | 114 |
| slot-specific GX evidence files | 56 |
| pending manual review | 114 |
| review confidence | 37 |
| derived confidence | 77 |
| blocked confidence | 0 |

## Anchor Modes

| anchor_mode | count |
|---|---:|
| FrameTopSocket | 34 |
| LegBodySocket | 49 |
| ShoulderPair | 31 |

## XFI Classes

| xfi_class | count |
|---|---:|
| direction-only | 37 |
| transform-bearing | 77 |

## Guardrail

- Direction-only firepower XFI rows are not source placement truth. Do not use unproven adapters or shell/bounds fallback.
- Known broken-looking single parts stay in the profile with review notes, but their visual acceptance belongs to GX audit if the part is broken before assembly.
- Actual acceptance remains blocked until latest capture and manual visual review record match.
