# Unity MCP

Unity MCP in this repo is a `diagnostic + manual automation` bridge.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- Manual Garage smoke: `tools/unity-mcp/Invoke-GarageManualSmoke.ps1`

`rule-harness` continues to use Unity MCP only for compile/status refresh plus generic diagnostics. Scene-specific runtime smoke stays out of harness scope.

## Route Tiers

### Stable

These are the routes we treat as first-class and keep aligned across bridge, wrapper, and scripts.

- `GET /health`
- `GET /scene/current`
- `GET /scene/hierarchy`
- `POST /scene/open`
- `POST /scene/save`
- `POST /play/start`
- `POST /play/stop`
- `POST /play/wait-for-play`
- `POST /play/wait-for-stop`
- `GET /console/logs`
- `GET /console/errors`
- `GET /ui/state`
- `POST /ui/invoke`
- `POST /ui/get-state`
- `POST /ui/wait-for-active`
- `POST /ui/wait-for-inactive`
- `POST /ui/wait-for-text`
- `POST /ui/wait-for-component`
- `POST /screenshot/capture`

### Manual-Only

Useful for supervised editor work, but not part of the preferred automation loop.

- `/input/*`
- `/menu/execute`
- scene mutation helpers such as `/gameobject/*`, `/component/*`, `/prefab/*`, `/ui/create-*`, `/ui/set-rect`

### Diagnostic / Experimental

Helpful for debugging, but not treated as stable automation contracts.

- `/async/*`
- `/console/stream*`
- `/console/logs/filter`
- `/console/stats`
- `/ui/compare-screenshots`
- `/snapshot/*`
- `/eval/*`
- `/explore/*`

## Recommended Flow

For manual runtime automation, use this sequence:

1. `GET /health`
2. `POST /play/start`
3. `POST /play/wait-for-play`
4. `GET /ui/state` or `POST /ui/invoke`
5. `POST /screenshot/capture`
6. `POST /play/stop`
7. `POST /play/wait-for-stop`

Garage smoke follows exactly this pattern.

## PowerShell Helpers

`McpHelpers.ps1` exposes stable helper functions:

- `Get-UnityMcpBaseUrl`
- `Wait-McpBridgeHealthy`
- `Invoke-McpSceneOpenAndWait`
- `Invoke-McpPlayStartAndWaitForBridge`
- `Invoke-McpPlayStopAndWait`
- `Get-McpRecentLogs`
- `Get-McpRecentErrors`
- `Get-McpUiState`
- `Get-McpUiElementState`
- `Invoke-McpUiInvoke`
- `Wait-McpUiActive`
- `Wait-McpUiInactive`
- `Wait-McpUiText`
- `Wait-McpUiComponent`
- `Invoke-McpScreenshotCapture`

Example:

```powershell
. .\tools\unity-mcp\McpHelpers.ps1
$root = Get-UnityMcpBaseUrl
Invoke-McpPlayStartAndWaitForBridge -Root $root
Invoke-McpUiInvoke -Root $root -Path "/Canvas/TopTabs/GarageTabButton"
Wait-McpUiActive -Root $root -Path "/Canvas/GaragePageRoot"
Invoke-McpScreenshotCapture -Root $root -OutputPath "artifacts/unity/garage.png" -Overwrite
Invoke-McpPlayStopAndWait -Root $root
```

## Manual Smoke

Run the Garage smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GarageManualSmoke.ps1
```

Defaults:

- scene: `Assets/Scenes/CodexLobbyScene.unity`
- tab: `/Canvas/TopTabs/GarageTabButton`
- root: `/Canvas/GaragePageRoot`
- screenshot: `artifacts/unity/garage-manual-smoke.png`

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Screenshot capture requires Play Mode and a project-relative output path.
- `ui/button/invoke` remains as a legacy alias, but new consumers should use `ui/invoke`.
- `server.js` now exposes the stable manual-automation routes directly as MCP tools.
