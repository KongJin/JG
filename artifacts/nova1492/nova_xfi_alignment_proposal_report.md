# Nova1492 XFI Alignment Proposal

> generated: 2026-04-27 23:49:38

- input alignment: `artifacts/nova1492/nova_part_alignment.csv`
- input xfi manifest: `artifacts/nova1492/nova_unitpart_xfi_manifest.csv`
- output proposal: `artifacts/nova1492/nova_xfi_alignment_proposal.csv`

## Summary

| metric | count |
|---|---:|
| proposal rows | 312 |
| rows with proposed socket offset | 138 |

## Quality Flags

| quality | count |
|---|---:|
| xfi_body_top_socket_candidate | 65 |
| xfi_leg_body_socket_candidate | 61 |
| xfi_named_attach_socket_candidate | 12 |
| xfi_reference_only | 88 |
| xfi_weapon_direction_only | 86 |

## Promotion Notes

- This file is a proposal, not runtime truth.
- `xfi_weapon_direction_only` rows preserve direction metadata only; do not promote a runtime attach offset until the parent body socket model is available.
- `xfi_body_top_socket_candidate` rows can be promoted into the dedicated frame top socket field.
- `xfi_leg_body_socket_candidate` rows can be promoted into the dedicated XFI attach socket field.
- `xfi_named_attach_socket_candidate` rows should be preserved in Unity data until parent body slot mapping is promoted.
