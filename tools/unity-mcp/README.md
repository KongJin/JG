# Unity MCP

Unity MCP in this repo is a `diagnostic + manual automation` bridge.
This file is the canonical runtime-automation usage note for the repo.

For Lobby/Garage UI work, `Assets/Scenes/CodexLobbyScene.unity` is the final SSOT.
`CodexLobbySceneBuilder` exists as a repair/reseed tool, not as the day-to-day source of truth.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- UI overview capture: `tools/unity-mcp/Invoke-UiOverviewCapture.ps1`
- Manual Garage smoke: `tools/unity-mcp/Invoke-GarageManualSmoke.ps1`
- Lobby/Garage page-switch smoke: `tools/unity-mcp/Invoke-LobbyGaragePageSwitchSmoke.ps1`
- Verified Lobby/Garage workflow gate: `tools/unity-mcp/Invoke-CodexLobbyUiWorkflowGate.ps1`
- Garage Ready flow smoke: `tools/unity-mcp/Invoke-GarageReadyFlowSmoke.ps1`
- Lobby -> GameScene summon smoke: `tools/unity-mcp/Invoke-GameSceneSummonSmoke.ps1`

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

`/menu/execute` is manual-only and non-authoritative for Lobby/Garage rebuild success.
Use the dedicated verified rebuild path instead.

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

## Scene-Builder Change Routine

When you change a scene builder or other editor automation script, use this exact loop:

1. Edit the builder/editor script.
2. Run `Assets/Refresh`.
3. Run the relevant builder menu item, such as `Tools/Codex/Build Codex Lobby Scene`.
4. Call `scene/save`.
5. Only then run Play Mode smoke/capture.

Failure symptom:

- If hierarchy or GameView still looks old after code changes, assume stale editor assembly first, not wrong scene code first.

For deterministic Lobby/Garage recovery, prefer the dedicated verified rebuild route instead of manual menu execution.

## CodexLobbyScene Contract

For Lobby/Garage UI work, the scene serialization is the runtime truth.

- Runtime SSOT: `Assets/Scenes/CodexLobbyScene.unity`
- Repair tool: `CodexLobbySceneBuilder.BuildCodexLobbySceneForAutomation()`
- Verified rebuild route: `POST /scene/rebuild-codex-lobby`
- Contract audit route: `GET /scene/verify-codex-lobby-contract`

Use the verified rebuild route after compile/reload has already stabilized.
The canonical place for that stabilization is `Invoke-CodexLobbyUiWorkflowGate.ps1`.

Sentinel nodes currently required by the contract:

- `/Canvas/LobbyPageRoot`
- `/Canvas/GaragePageRoot`
- `/Canvas/LobbyPageRoot/RoomListPanel`
- `/Canvas/LobbyPageRoot/RoomListPanel/ListHeaderRow`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomListSurface`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomListSurface/EmptyStateText`
- `/Canvas/LobbyPageRoot/GarageTabButton`
- `/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton`

Representative serialized reference checks:

- `/LobbyView::LobbyView._lobbyPageRoot`
- `/LobbyView::LobbyView._garagePageRoot`
- `/LobbyView::LobbyView._roomListView`
- `/LobbyView::LobbyView._roomDetailView`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListContent`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomItemPrefab`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListCountText`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListEmptyStateText`

The dedicated rebuild route is authoritative only when it returns:

- `success = true`
- `sceneSaved = true`
- empty `missingSentinels`
- empty `missingReferences`

## Quick Start

Prefer running scripts with `-ExecutionPolicy Bypass` so PowerShell does not block helper loading:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UiOverviewCapture.ps1
```

For interactive use, start the shell session like this before dot-sourcing helpers:

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
. .\tools\unity-mcp\McpHelpers.ps1
```

## PowerShell Helpers

`McpHelpers.ps1` exposes stable helper functions:

- `Get-UnityMcpBaseUrl`
- `Wait-McpBridgeHealthy`
- `Invoke-McpSceneOpenAndWait`
- `Invoke-McpCompileRequestAndWait`
- `Invoke-McpPlayStartAndWaitForBridge`
- `Invoke-McpPlayStopAndWait`
- `Invoke-McpCodexLobbyVerifiedRebuild`
- `Get-McpCodexLobbyContract`
- `Get-McpRecentLogs`
- `Get-McpRecentErrors`
- `Get-McpConsoleSummary`
- `Get-McpUiState`
- `Get-McpUiStateSummary`
- `Get-McpUiElementState`
- `Invoke-McpUiInvoke`
- `Wait-McpUiActive`
- `Wait-McpUiInactive`
- `Wait-McpUiText`
- `Wait-McpUiComponent`
- `Invoke-McpScreenshotCapture`

Example:

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
. .\tools\unity-mcp\McpHelpers.ps1
$root = Get-UnityMcpBaseUrl
Invoke-McpPlayStartAndWaitForBridge -Root $root
Invoke-McpUiInvoke -Root $root -Path "/Canvas/LobbyPageRoot/GarageTabButton"
Wait-McpUiActive -Root $root -Path "/Canvas/GaragePageRoot"
Invoke-McpScreenshotCapture -Root $root -OutputPath "artifacts/unity/garage.png" -Overwrite
Invoke-McpPlayStopAndWait -Root $root
```

## Manual Smoke

Run the verified Lobby/Garage workflow gate like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-CodexLobbyUiWorkflowGate.ps1
```

This serial gate performs:

1. script reload / compile stabilization
2. dedicated verified rebuild for `CodexLobbyScene`
3. scene contract verification
4. Lobby/Garage page-switch smoke
5. machine-readable report writeout

Outputs:

- `artifacts/unity/codex-lobby-ui-workflow-result.json`
- `artifacts/unity/lobby-garage-page-switch-result.json`
- page-switch screenshots generated by the smoke

Use this as the required gate for Lobby/Garage UI changes.
Do not run parallel Play Mode smokes against the same editor instance for this workflow.

Run the UI overview capture like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UiOverviewCapture.ps1
```

Outputs:

- `artifacts/unity/ui-overview-lobby.png`
- `artifacts/unity/ui-overview-garage.png`
- `artifacts/unity/ui-overview-report.json`

The report includes:

- stage timings for bridge readiness, Play Mode, login overlay wait, and each capture
- compact UI summaries instead of the full `/ui/state` dump
- grouped console warnings/errors with benign known warnings split out
- current scene path, pre-Play health snapshot, and page root active/inactive state snapshots
- the exact button/root paths used for the page-switch flow
- refresh/build/save debugging hints for stale editor assembly cases

This script now follows the page-switch flow:

- capture Lobby-only state first
- open Garage through the Lobby-side `Garage` button
- capture Garage-only state
- return to Lobby through `Back To Lobby`
- verify the page roots swap active/inactive states correctly

Run the canonical page-switch smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-LobbyGaragePageSwitchSmoke.ps1
```

Outputs:

- `artifacts/unity/lobby-page-smoke-lobby-initial.png`
- `artifacts/unity/lobby-page-smoke-garage.png`
- `artifacts/unity/lobby-page-smoke-lobby-returned.png`
- `artifacts/unity/lobby-garage-page-switch-result.json`

This is the preferred verification path for the current Lobby/Garage UI contract.

Run the Garage smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GarageManualSmoke.ps1
```

Run the draft -> save -> ready smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GarageReadyFlowSmoke.ps1
```

Run the lobby -> game -> summon smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameSceneSummonSmoke.ps1
```

Defaults:

- scene: `Assets/Scenes/CodexLobbyScene.unity`
- Garage open button: `/Canvas/LobbyPageRoot/GarageTabButton`
- Back-to-Lobby button: `/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton`
- root: `/Canvas/GaragePageRoot`
- screenshot: `artifacts/unity/garage-manual-smoke.png`
- result: `artifacts/unity/garage-manual-smoke-result.json`
- UI overview screenshots: `artifacts/unity/ui-overview-lobby.png`, `artifacts/unity/ui-overview-garage.png`
- UI overview report: `artifacts/unity/ui-overview-report.json`

Ready-flow smoke highlights:

- creates a room after filling the room-name field
- auto-fills empty Garage slots until Ready unlocks
- verifies unsaved draft changes relock Ready
- verifies Save restores Ready and Ready toggles to `Cancel`
- captures `artifacts/unity/garage-ready-flow-smoke.png`

UI change verification note:

- Pair scene hierarchy inspection with a Play Mode capture every time you change Lobby/Garage UI structure.
- Do not trust old GameView or hierarchy output until you have completed `Assets/Refresh -> Build -> scene/save`.
- For critical verification, do not stop at menu execution success. Require a successful rebuild response plus a passing scene contract.

GameScene summon smoke highlights:

- creates a room and ensures the Ready baseline from the Garage dashboard
- starts the match and waits for `GameScene`
- uses `/ui/invoke` on `/HudCanvas/UnitSummonUi/SlotRow/UnitSlotTemplate(Clone)` as the stable summon trigger
- verifies `BattleEntity(Clone)` appears under `/RuntimeRoot/UnitsRoot/`
- captures `artifacts/unity/game-scene-summon-smoke.png` and `artifacts/unity/game-scene-summon-smoke-result.json`

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Screenshot capture requires Play Mode and a project-relative output path.
- `ui/button/invoke` remains as a legacy alias, but new consumers should use `ui/invoke`.
- `server.js` now exposes the stable manual-automation routes directly as MCP tools.
