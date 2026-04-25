# Nova1492 GX Sample Analysis: Iv_exp_blue

> generated: 2026-04-25

## Sample Set

| file | bytes | note |
|---|---:|---|
| `datan\common\Iv_exp_blue.GX` | 4632 | Binary GX sample analyzed here. |
| `datan\common\Iv_exp_blue.DAE` | 30989 | Matching COLLADA export used as ground truth. |
| `datan\common\Iv_exp_blue.BMP` | 196664 | Texture referenced by both formats. |
| `datan\common\Iv_exp_blue.XFI` | 9 | Tiny text sidecar: `-1,` then `0,`. |

## Confirmed Findings

- The `.GX` file is not opaque compression.
- It contains readable ASCII object/material names:
  - `Iv_exp_blue.GX`
  - `Iv_exp`
  - `Iv_exp_blue.BMP`
- It contains repeated textual chunk/type markers such as `4294901778`, equivalent to `0xFFFF0012`.
- It contains little-endian `float32` transform and mesh streams that match the `.DAE` export.
- The copied `.DAE` appears to be an export derived from the same GX data, not a separate unrelated model.

## Matched Geometry Streams

The first confirmed mesh stream in `Iv_exp_blue.GX` matches the DAE geometry:

| stream | offset | type | observed shape |
|---|---:|---|---|
| transform matrix | `0x78` | `float32[16]` | Matches the DAE node matrix prefix. |
| positions | `0x13F` | `float32 xyz` | Starts with DAE vertex positions. |
| normals | `0x49F` | `float32 xyz` | Starts with normal triplets such as `1, 0, 0`. |
| uvs | `0x7FF` | `float32 uv` | Matches DAE UVs with V flipped (`gxV = 1 - daeV`). |
| indices | `0xA3F` | little-endian `uint16` | First 132 indices form 44 triangles, max index `71`. |

Example position match:

```text
GX offset 0x13F:
0.095550, 0.695760, -0.286650
0.095550, 0.695760,  0.286650
0.095550,-0.654903,  0.286650
0.095550,-0.654903, -0.286650
```

The first index values are:

```text
0, 1, 2, 2, 3, 0
```

This triangulates the first four GX vertices into two triangles and corresponds to the first two DAE polygon entries.

## Implication

Nova1492 likely has many more model-like assets than the four standard `.DAE` files. The `.GX` files appear to be the native model/effect geometry format. A converter prototype is plausible.

## Next Converter Steps

1. Parse chunk markers and names instead of relying on hard-coded offsets.
2. Infer mesh section counts and boundaries from nearby metadata.
3. Export a minimal OBJ from the confirmed position/index stream.
4. Compare the generated OBJ against `Iv_exp_blue.DAE`.
5. Repeat against a non-DAE sample such as `acptwr.GX` or `AcpUnique01.GX`.
