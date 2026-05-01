# JG Audio MCP

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: tools.audio-mcp
> role: reference
> owner_scope: JG Audio MCP wrapper setup, tool list, optional MCP registration, environment variables
> upstream: plans.audio-sfx-mcp-pipeline
> artifacts: none

Local stdio MCP helper for JG audio asset import and SoundCatalog sync.
Suno generation is handled directly by `sandraschi/suno-mcp` during audio
sessions, while this helper keeps the Unity-side file naming and catalog update
repeatable.

## Tools

- `audio_plan_sfx_batch`
- `audio_import_downloaded_assets`
- `audio_sync_sound_catalog`

## Environment

- `SUNO_MCP_COMMAND`: optional local command for direct `sandraschi/suno-mcp` registration.
- `SUNO_MCP_PYTHONPATH`: optional local Python path for direct `sandraschi/suno-mcp` registration.

Do not commit Suno credentials, browser session files, cookies, or downloaded
account state.

## Build

```powershell
npm run audio:mcp:build
```

## MCP Registration

Keep `.mcp.json` lean during normal work. For generation sessions, register a
direct Suno MCP. The original `sandraschi/suno-mcp` GitHub page can be used as
the behavior reference; this workspace currently uses the accessible
`MeroZemory/suno-multi-mcp` fork installed under `.codex-local/`.

```json
{
  "mcpServers": {
    "suno": {
      "type": "stdio",
      "command": ".codex-local/suno-multi-mcp/.venv/Scripts/python.exe",
      "args": ["-m", "suno_mcp.server"],
      "env": {
        "PYTHONPATH": ".codex-local/suno-multi-mcp/src"
      }
    }
  }
}
```

Register this helper only when you want MCP access to the Unity import/sync
steps:

```json
{
  "mcpServers": {
    "audio-unity": {
      "type": "stdio",
      "command": "node",
      "args": ["tools/audio-mcp/dist/server.js"]
    }
  }
}
```

## Typical Flow

1. `audio_plan_sfx_batch` with `writeManifest: true`.
2. Use `sandraschi/suno-mcp` directly to generate/download each prompt.
3. Put downloaded files in `artifacts/audio/inbox` using soundKey filenames,
   for example `ui_click.mp3` or `garage_save.wav`.
4. `audio_import_downloaded_assets` to copy files into `Assets/Audio/UI` or
   `Assets/Audio/SFX`.
5. Refresh Unity assets so `.meta` files are generated.
6. `audio_sync_sound_catalog` once target audio files have `.meta` GUIDs.

`artifacts/audio/inbox/` is gitignored because it may contain temporary Suno
account downloads.
