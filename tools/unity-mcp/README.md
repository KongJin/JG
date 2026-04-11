# Unity MCP (JG)

This folder provides the Unity Editor bridge plus the Codex MCP server wrapper used in this repo.

## What it adds

- Unity bridge: `Assets/Editor/UnityMcp/UnityMcpBridge.cs`
- MCP stdio server: `tools/unity-mcp/server.js`
- Registration helper: `tools/unity-mcp/register-codex-mcp.ps1`

The bridge listens on the port from `ProjectSettings/UnityMcpPort.txt` and currently exposes **36 HTTP routes**:

- 6 `GET` routes for health, scene, console, hierarchy, compile status
- 8 `POST` routes for play / scene / compile / screenshot / build
- 8 `POST` routes for input and UI button invoke
- 13 `POST` routes for scene, gameobject, component, prefab, asset editing
- 1 `POST` route for menu execution

See [`docs/ops/unity_mcp.md`](../../docs/ops/unity_mcp.md) for the full route inventory and request body examples.

## Prerequisites

- Unity project open in the Editor
- Node.js installed (`node -v`)
- Codex CLI installed (`codex --version`)

## Quick start

1. Open this project in Unity.
2. Confirm the bridge is running:
   - Unity menu: `Tools > Unity MCP > Print Status`
   - PowerShell: `Invoke-RestMethod ("http://127.0.0.1:{0}/health" -f (Get-Content .\ProjectSettings\UnityMcpPort.txt))`
3. Optionally open a scene with current file names:

```powershell
$port = Get-Content .\ProjectSettings\UnityMcpPort.txt
Invoke-RestMethod -Uri ("http://127.0.0.1:{0}/scene/open" -f $port) -Method Post -ContentType "application/json" -Body '{"scenePath":"Assets/Scenes/ExampleScene.unity","saveCurrentSceneIfDirty":true}'
```

4. Register the MCP server:
   - `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\register-codex-mcp.ps1`
5. Verify registration:
   - `codex mcp list`
   - `codex mcp get unity`

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Change `ProjectSettings/UnityMcpPort.txt` if `51234` is already in use.
- `register-codex-mcp.ps1` no longer needs a pinned `UNITY_MCP_BASE_URL` by default; `server.js` follows the port file at runtime.
- Passing `-UnityBridgeUrl` overrides the port file explicitly.
