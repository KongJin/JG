# Nova1492 Mobility Alignment Investigation

> generated: 2026-04-29
> purpose: compact-safe handoff note for mobility parts that visually attach in the wrong position in Garage preview

## Context

Road Runner (`nova_mob_legs1_rdrn`) now renders as an assembled GX frame-tree model in Garage preview. After expanding the conversion to the Garage catalog, several mobility parts still appear offset when combined with a body/frame.

This note preserves the investigation state before context compaction. Do not treat it as a final fix.

## Affected Parts

| Korean name | primary partId | variants found | source GX |
|---|---|---|---|
| 크롤러 | `nova_mob_legs15_krr` | `nova_mob_n_legs42_krr` | `datan\common\legs15_krr.gx` |
| 스파이더 | `nova_mob_legs20_spod` | `nova_mob_s_legs33_spod` | `datan\common\legs20_spod.gx` |
| 탱커 | `nova_mob_legs23_tk` | `nova_mob_s_legs30_tk` | `datan\common\legs23_tk.gx` |
| 캐터필러 | `nova_mob_legs3_ktpr` | none found | `datan\common\legs3_ktpr.gx` |
| 델피누스 | `nova_mob_legs34_dpns` | none found | `datan\common\legs34_dpns.gx` |
| 옵테릭스 | `nova_mob_legs49_otrs` | none found | `datan\common\legs49_otrs.gx` |
| 피파울 | `nova_mob_legs51_ppo` | `nova_mob_g_legs57_ppo` | `datan\common\legs51_ppo.gx` |

## Current Evidence

All listed parts are mobility/legs parts and use the converter's leg assembly path:

- parser mode: `gx_xfi_leg_assembly`
- XFI shape: numeric header `0`, one transform matrix, seven direction ranges
- suspected issue is not primarily broken mesh extraction, because some affected parts have no dropped helper mesh blocks

Key comparison:

| part | bounds Y size | current socket Y | XFI socket Y | delta Y |
|---|---:|---:|---:|---:|
| 크롤러 `nova_mob_legs15_krr` | 0.550300 | 0.275150 | 0.000260 | 0.274890 |
| 스파이더 `nova_mob_legs20_spod` | 0.683336 | 0.341668 | 0.000046 | 0.341622 |
| 탱커 `nova_mob_legs23_tk` | 0.658617 | 0.329309 | 0.002129 | 0.327180 |
| 캐터필러 `nova_mob_legs3_ktpr` | 0.468897 | 0.234449 | -0.002380 | 0.236829 |
| 델피누스 `nova_mob_legs34_dpns` | 0.549831 | 0.274915 | 0.000046 | 0.274869 |
| 옵테릭스 `nova_mob_legs49_otrs` | 0.820373 | 0.410187 | 0.000000 | 0.410187 |
| 피파울 `nova_mob_legs51_ppo` | 0.252517 | 0.126258 | 0.000000 | 0.126258 |
| 로드런너 `nova_mob_legs1_rdrn` | 0.245853 | 0.122926 | -0.000192 | 0.123118 |
| 스트라이더 `nova_mob_legs11_strod` | 0.572855 | 0.286427 | 0.000000 | 0.286427 |

The `current socket Y` values are approximately half of normalized bounds height. That strongly suggests the current runtime socket is bounds-derived, not XFI-derived.

## Suspected Root Cause

There are two coordinate-space assumptions fighting each other.

1. Preview prefab generation normalizes every part around renderer bounds center.

   File:
   `Assets/Editor/AssetTools/Nova1492PlayablePartGenerationTool.cs`

   Relevant behavior:

   - `NormalizePreviewChild(...)` computes renderer bounds
   - sets `child.transform.localScale = Vector3.one * scale`
   - sets `child.transform.localPosition = -bounds.center * scale`

   This is good for standalone part thumbnails, but it changes the model's original GX pivot/socket relationship.

2. Unit assembly prefers `SocketOffset` before XFI attach socket.

   File:
   `Assets/Scripts/Features/Garage/Presentation/GarageUnitPreviewAssembly.cs`

   Relevant behavior:

   - `ResolveMobilitySocketOffset(...)`
   - if `mobilityAlignment.SocketOffset.sqrMagnitude > 0.000001f`, returns `SocketOffset`
   - only falls back to `XfiAttachSocketOffset` when `SocketOffset` is near zero

   Current `SocketOffset` values are nonzero bounds-derived offsets, so XFI attach sockets are ignored.

## Why These Parts Look Wrong

The affected parts have geometry where the body attach point does not coincide with the top of the bounding box after normalization. Examples:

- crawler/spider/tanker have tall or wide leg/tread forms, so bounds top is visually far from the intended body socket
- opteryx has a large vertical bounds span, producing the largest delta Y among the listed set
- pipaul has an XFI socket at zero but still gets moved by half-bounds

Road Runner happened to look acceptable after the frame-tree fix, but the same bounds-derived placement rule is still fragile.

## Coordinate Conversion Direction

Yes, coordinate-space conversion should be possible.

The next implementation should convert the XFI attach socket into the normalized preview prefab coordinate space, or preserve a separate assembly prefab/pivot that does not recenter around bounds.

Candidate formula to test:

```text
normalizedSocket = (rawXfiSocket - rawBoundsCenter) * previewNormalizeScale
```

But this must be verified against the exact coordinate space used by:

- GX OBJ export after node transform accumulation
- Unity imported model bounds
- generated preview prefab child transform
- runtime assembly root transform

Safer implementation options:

1. Assembly-specific prefab path:
   - Keep thumbnail/part-preview prefabs bounds-centered.
   - Add or generate assembly prefabs that preserve original GX pivot and apply only scale.
   - Unit assembly uses assembly prefab, not thumbnail prefab.

2. XFI-first socket path:
   - Keep current prefab structure.
   - Store a converted XFI socket offset in `NovaPartAlignmentCatalog`.
   - For mobility, prefer XFI attach socket over bounds-derived `SocketOffset`.
   - Use bounds-derived socket only as fallback when XFI is absent or flagged invalid.

Recommended next test:

- Pick one affected part: `nova_mob_legs15_krr` or `nova_mob_legs20_spod`.
- Generate before/after runtime GameView captures in Garage preview.
- Compare against Road Runner to avoid regressing the known-good case.

## Relevant Files

- `tools/nova1492/GxObjConverter/Program.cs`
- `tools/nova1492/BuildNovaXfiAlignmentProposal.ps1`
- `tools/nova1492/PromoteNovaXfiToUnityData.ps1`
- `Assets/Editor/AssetTools/Nova1492PlayablePartGenerationTool.cs`
- `Assets/Scripts/Features/Garage/Presentation/GarageUnitPreviewAssembly.cs`
- `Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset`
- `artifacts/nova1492/nova_part_alignment.csv`
- `artifacts/nova1492/nova_xfi_alignment_proposal.csv`
- `artifacts/nova1492/gx_conversion_manifest.csv`

## Current State

- This investigation has not changed runtime placement yet.
- This note exists to preserve the part list and root-cause analysis through context compaction.
- Last known compile after related catalog work was clean before this note was created.
