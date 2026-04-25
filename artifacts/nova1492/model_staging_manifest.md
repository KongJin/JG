# Nova1492 Model Staging Manifest

> generated: 2026-04-25

## Scope

- Imported the four discovered COLLADA model files from `datan/common`.
- Imported the matching BMP textures required by those models.
- Rewrote copied `.DAE` texture references from the original `F:\NOVA1492\...` authoring path to local Unity project relative paths.
- Left adjacent `.GX` and `.XFI` files out of this pass because they remain custom/unknown format candidates.

## Files

| category | source | Unity path | bytes |
|---|---|---|---:|
| effect-model | `datan\common\Iv_exp_blue.DAE` | `Assets/Art/Nova1492/Effects/Models/Iv_exp_blue.DAE` | 30989 |
| effect-model | `datan\common\Iv_exp_orange.DAE` | `Assets/Art/Nova1492/Effects/Models/Iv_exp_orange.DAE` | 31007 |
| effect-model | `datan\common\Iv_exp_red.DAE` | `Assets/Art/Nova1492/Effects/Models/Iv_exp_red.DAE` | 30980 |
| effect-model | `datan\common\Iv_exp_yellow.DAE` | `Assets/Art/Nova1492/Effects/Models/Iv_exp_yellow.DAE` | 31007 |
| effect-model-texture | `datan\common\Iv_exp_blue.BMP` | `Assets/Art/Nova1492/Effects/Textures/Iv_exp_blue.BMP` | 196664 |
| effect-model-texture | `datan\common\Iv_exp_orange.BMP` | `Assets/Art/Nova1492/Effects/Textures/Iv_exp_orange.BMP` | 196664 |
| effect-model-texture | `datan\common\Iv_exp_red.BMP` | `Assets/Art/Nova1492/Effects/Textures/Iv_exp_red.BMP` | 196664 |
| effect-model-texture | `datan\common\Iv_exp_yellow.BMP` | `Assets/Art/Nova1492/Effects/Textures/Iv_exp_yellow.BMP` | 196664 |

## Verification

- Unity generated `.meta` files for the new model and texture assets while the editor was open.
- No copied `.DAE` file retains the original `F:\NOVA1492` texture reference.
