---
name: unity-importer
description: "Configure Unity import settings for textures, audio, models, compression, max size, and asset import options."
---

# Unity Importer Skills

Use this module to change import **settings** for textures, audio, and models that already exist in the project.

> **Batch-first**: Prefer the batch setters when configuring `2+` assets of the same category.

## Guardrails

**Mode**: Full-Auto required

**DO NOT** (common hallucinations):
- `importer_import` does not exist -> use `asset_import` in the `asset` module to bring files into the project
- `importer_set_format` does not exist -> use the specific texture/audio/model setters
- `importer_get_settings` does not exist -> use the category-specific getters
- Settings changes do not always apply instantly in memory. Reimport may still be required

**Routing**:
- File import or refresh -> `asset`
- Texture settings -> `texture_*`
- Audio settings -> `audio_*`
- Model settings -> `model_*`
- Alternative importer bridge skills -> `texture_set_import_settings`, `audio_set_import_settings`, `model_set_import_settings`
- Force importer refresh -> `asset_reimport` or `asset_reimport_batch`

## Skills

### Texture Route

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `texture_get_settings` | Read texture importer settings | `assetPath` |
| `texture_set_settings` | Set texture importer settings | `assetPath`, `textureType?`, `maxSize?`, `filterMode?`, `compression?`, `mipmapEnabled?`, `sRGB?`, `readable?`, `wrapMode?` |
| `texture_set_settings_batch` | Batch texture settings | `items` |
| `texture_set_import_settings` | Alternative texture import bridge | similar texture fields |

Common texture decisions:
- UI sprites -> `textureType="Sprite"`, usually `mipmapEnabled=false`
- Pixel art -> `filterMode="Point"`
- Runtime CPU reads -> `readable=true` only when necessary

### Audio Route

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `audio_get_settings` | Read audio importer settings | `assetPath` |
| `audio_set_settings` | Set audio importer settings | `assetPath`, `forceToMono?`, `loadInBackground?`, `loadType?`, `compressionFormat?`, `quality?` |
| `audio_set_settings_batch` | Batch audio settings | `items` |
| `audio_set_import_settings` | Alternative audio import bridge | similar audio fields |

Common audio decisions:
- Long BGM -> `loadType="Streaming"`
- Short SFX -> `loadType="DecompressOnLoad"`
- Memory-sensitive SFX libraries -> consider `forceToMono=true`

### Model Route

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `model_get_settings` | Read model importer settings | `assetPath` |
| `model_set_settings` | Set model importer settings | `assetPath`, `globalScale?`, `meshCompression?`, `isReadable?`, `generateSecondaryUV?`, `animationType?`, `importAnimation?`, `importCameras?`, `importLights?`, `materialImportMode?` |
| `model_set_settings_batch` | Batch model settings | `items` |
| `model_set_import_settings` | Alternative model import bridge | similar model fields |

Common model decisions:
- Characters -> `animationType="Humanoid"` when retargeting is required
- Static props -> disable cameras/lights/animation imports when unused
- Baked-lighting meshes -> enable secondary UVs when appropriate

## Reimport Rule

After importer changes, use reimport when you need Unity to fully refresh the asset:

| Skill | Use |
|-------|-----|
| `asset_reimport` | Reimport one asset |
| `asset_reimport_batch` | Reimport assets matching a search scope |

## Minimal Example

```python
import unity_skills

unity_skills.call_skill("texture_set_settings_batch", items=[
    {"assetPath": "Assets/UI/icon_play.png", "textureType": "Sprite", "mipmapEnabled": False},
    {"assetPath": "Assets/UI/icon_pause.png", "textureType": "Sprite", "mipmapEnabled": False}
])

unity_skills.call_skill("audio_set_settings",
    assetPath="Assets/Audio/bgm.mp3",
    loadType="Streaming",
    compressionFormat="Vorbis",
    quality=0.7
)

unity_skills.call_skill("asset_reimport", assetPath="Assets/Audio/bgm.mp3")
```

## Exact Signatures

Exact names, parameters, defaults, and returns are defined by `GET /skills/schema` or `unity_skills.get_skill_schema()`, not by this file.
Load `IMPORT_REFERENCE.md` for extended asset search/query helpers, platform overrides, rig/animation details, and importer-side best practices.
