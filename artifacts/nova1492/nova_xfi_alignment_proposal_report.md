# Nova1492 XFI Alignment Proposal

> generated: 2026-04-30 23:26:35

- input alignment: `artifacts/nova1492/nova_part_alignment.csv`
- input xfi manifest: `artifacts/nova1492/nova_unitpart_xfi_manifest.csv`
- output proposal: `artifacts/nova1492/nova_xfi_alignment_proposal.csv`

## Summary

| metric | count |
|---|---:|
| proposal rows | 222 |
| rows with proposed socket offset | 124 |

## Quality Flags

| quality | count |
|---|---:|
| xfi_body_top_socket_candidate | 64 |
| xfi_leg_body_socket_candidate | 60 |
| xfi_reference_only | 13 |
| xfi_weapon_direction_only | 85 |

## Promotion Notes

- This file is a proposal, not runtime truth.
- `xfi_weapon_direction_only` rows preserve direction metadata only; do not promote a runtime attach offset until the parent body socket model is available.
- `xfi_body_top_socket_candidate` rows can be promoted into the dedicated frame top socket field.
- `xfi_leg_body_socket_candidate` rows can be promoted into the dedicated XFI attach socket field.
- `xfi_named_attach_socket_candidate` rows should be preserved in Unity data until parent body slot mapping is promoted.
