# Unity MCP

Unity MCP in this repo is a `diagnostic + manual automation` bridge.
For Lobby/Garage UI work, the runtime SSOT is `Assets/Scenes/CodexLobbyScene.unity`.
Lobby/Garage layout recovery is done by direct MCP scene/prefab repair against that scene, not by code-driven scene regeneration.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- Workflow gate: `tools/unity-mcp/Invoke-CodexLobbyUiWorkflowGate.ps1`
- Canonical page-switch smoke: `tools/unity-mcp/Invoke-LobbyGaragePageSwitchSmoke.ps1`
- Feature smoke: `tools/unity-mcp/Invoke-GameSceneSummonSmoke.ps1`
- Feature smoke: `tools/unity-mcp/Invoke-GameScenePlacementWaveSmoke.ps1`
- Optional supervised smoke: `tools/unity-mcp/Invoke-GarageReadyFlowSmoke.ps1`

`rule-harness` continues to use Unity MCP only for compile/status refresh plus generic diagnostics. Scene-specific runtime smoke stays out of harness scope.

## Core Routes

These are the routes the current workflow depends on.

- `GET /health`
- `POST /scene/open`
- `POST /scene/save`
- `POST /play/start`
- `POST /play/stop`
- `POST /play/wait-for-play`
- `POST /play/wait-for-stop`
- `POST /ui/invoke`
- `POST /ui/get-state`
- `POST /ui/wait-for-active`
- `POST /ui/wait-for-inactive`
- `POST /screenshot/capture`
- `GET /scene/verify-codex-lobby-contract`

`/menu/execute` still exists, but it is manual-only and non-authoritative for Lobby/Garage recovery.

## Recommended Workflow

Use this order for Lobby/Garage UI work:

1. `Invoke-CodexLobbyUiWorkflowGate.ps1`
2. `Invoke-LobbyGaragePageSwitchSmoke.ps1`
3. feature smoke only when the change reaches scene transition, network/bootstrap, or WebGL flow

The rule is:

- verify scene contract first
- repair the scene directly when contract fails
- trust the gate and canonical smoke over ad hoc captures
- keep Ready/Save rule checks in EditMode or domain tests, not in canonical smoke

## SSOT Guardrails

- Never overwrite an open `.unity` scene on disk while Unity has that scene loaded.
- Prefer MCP scene/prefab repair over direct YAML replacement for Lobby/Garage UI work.
- If a disk-level restore is truly required, switch away from the scene or close Unity first, then restore the file, then reopen it in Unity.
- The open-scene popup (`The following open scene(s) have been changed on disk`) is treated as a workflow violation, not as a prompt to accept casually.

## CodexLobbyScene Contract

For Lobby/Garage UI work, scene serialization is the runtime truth.

- Runtime SSOT: `Assets/Scenes/CodexLobbyScene.unity`
- Contract audit route: `GET /scene/verify-codex-lobby-contract`
- Repair path: use MCP to modify the scene or prefabs directly until the contract passes again

Required sentinel nodes:

- `/Canvas/LobbyPageRoot`
- `/Canvas/GaragePageRoot`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot/GarageMobileTabBar`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost`
- `/Canvas/GaragePageRoot/MobileSaveButton`
- `/Canvas/GaragePageRoot/GarageContentRow/RosterListPane/MobileSlotGrid`
- `/Canvas/LobbyPageRoot/RoomListPanel`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/ListHeaderRow`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface/EmptyStateText`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard/GarageTabButton`
- `/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton`

Representative serialized reference checks:

- `/Canvas/GaragePageRoot::GaragePageController._responsiveRoot`
- `/Canvas/GaragePageRoot::GaragePageController._desktopContentRoot`
- `/Canvas/GaragePageRoot::GaragePageController._mobileContentRoot`
- `/Canvas/GaragePageRoot::GaragePageController._mobileBodyHost`
- `/Canvas/GaragePageRoot::GaragePageController._desktopSlotHost`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSlotHost`
- `/Canvas/GaragePageRoot::GaragePageController._rightRailRoot`
- `/Canvas/GaragePageRoot::GaragePageController._mobileTabBar`
- `/Canvas/GaragePageRoot::GaragePageController._mobileEditTabButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobilePreviewTabButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSummaryTabButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSaveButton`
- `/LobbyView::LobbyView._lobbyPageRoot`
- `/LobbyView::LobbyView._garagePageRoot`
- `/LobbyView::LobbyView._roomListView`
- `/LobbyView::LobbyView._roomDetailView`
- `/LobbyView::LobbyView._garageSummaryView`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListContent`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomItemPrefab`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListCountText`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListEmptyStateText`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard::LobbyGarageSummaryView._statusPillText`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard::LobbyGarageSummaryView._headlineText`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard::LobbyGarageSummaryView._bodyText`

Contract 운영 원칙:

- page contract owner는 scene/prefab 쪽에서 유지하고, runtime controller는 상태 렌더와 focus 전환까지만 담당한다.
- 특정 controller 하나에 serialized ref를 계속 몰아 넣어 scene contract를 대표하게 만들지 않는다.
- smoke/debug 전용 진입점은 가능하면 별도 bridge component로 분리한다.

The contract route is considered healthy when it returns:

- `success = true`
- `sceneSaved = true`
- empty `missingSentinels`
- empty `missingReferences`

## PowerShell Helpers

`McpHelpers.ps1` exposes the helpers the remaining workflow depends on:

- `Get-UnityMcpBaseUrl`
- `Wait-McpBridgeHealthy`
- `Invoke-EditorProjectSync.ps1`
- `Invoke-McpCompileRequestAndWait`
- `Invoke-McpSceneOpenAndWait`
- `Invoke-McpPlayStartAndWaitForBridge`
- `Invoke-McpPlayStopAndWait`
- `Assert-McpNoOpenSceneDiskWrite`
- `Get-McpCodexLobbyContract`
- `Invoke-McpPrepareCodexLobbyPlaySession`
- `Get-McpPageStateSnapshot`
- `Wait-McpPhotonLobbyReady`
- `Get-McpConsoleSummary`
- `Get-McpUiTextValue`
- `Get-McpUiButtonInfo`
- `Get-McpUiActiveInHierarchy`
- `Invoke-McpSetUiValue`
- `Invoke-McpUiInvoke`
- `Wait-McpUiActive`
- `Wait-McpUiInactive`
- `Invoke-McpScreenshotCapture`

Use `Assert-McpNoOpenSceneDiskWrite` before any script that would touch a `.unity` file on disk outside the editor bridge.
If the target matches `health.activeScenePath`, the helper fails fast and tells you to use MCP repair or switch scenes first.

When Unity already has the project open and you need generated `.csproj` files refreshed for editor tests or IDE sync, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-EditorProjectSync.ps1
```

This uses the current editor instance through MCP and performs:

1. bridge health check
2. compile/reload stabilization
3. `Assets/Open C# Project` menu execution

## Workflow Gate

Run the required Lobby/Garage gate like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-CodexLobbyUiWorkflowGate.ps1
```

The gate performs:

1. compile/reload stabilization
2. contract verification
3. canonical page-switch smoke
4. machine-readable result writeout

Outputs:

- `artifacts/unity/codex-lobby-ui-workflow-result.json`
- `artifacts/unity/lobby-garage-page-switch-result.json`

Use this as the required gate for Lobby/Garage UI changes.
Do not run parallel Play Mode smokes against the same editor instance for this workflow.

## Canonical Smoke

Run the canonical page-switch smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-LobbyGaragePageSwitchSmoke.ps1
```

Outputs:

- `artifacts/unity/lobby-page-smoke-lobby-initial.png`
- `artifacts/unity/lobby-page-smoke-garage.png`
- `artifacts/unity/lobby-page-smoke-lobby-returned.png`
- `artifacts/unity/lobby-garage-page-switch-result.json`

This is the default runtime proof for the current Lobby/Garage UI contract.

## Feature Smoke

Run the lobby -> game -> summon smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameSceneSummonSmoke.ps1
```

Run the placement -> wave/core -> outcome smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameScenePlacementWaveSmoke.ps1 -PostSummonWaitSec 20 -OutcomeTimeoutSec 180
```

Outputs:

- `artifacts/unity/game-scene-placement-initial.png`
- `artifacts/unity/game-scene-placement-after-drag.png`
- `artifacts/unity/game-scene-placement-final.png`
- `artifacts/unity/game-scene-placement-wave-result.json`

Feature smoke is for regressions beyond page switching. It does not replace the workflow gate.
The main retained acceptance path is:

1. workflow gate
2. canonical page-switch smoke
3. `GameScene` summon smoke when the change reaches lobby-to-game flow

Use `Invoke-GameScenePlacementWaveSmoke.ps1` when you need to observe more than summon availability:

- attempt placement drag and capture the current automation failure mode
- fall back to `/ui/invoke` summon
- poll wave/core HUD plus end overlay until `Victory!` or `Defeat!`

Current interpretation rule:

- `dragDidSummon = false` with `dragPlacementErrorText = "배치 영역 밖입니다!"` means placement drag automation is still unstable
- `waveEndOverlayActive = true` plus `returnToLobbyButtonActive = true` is the authoritative outcome signal
- do not treat `ResultText` alone as outcome proof while the overlay is inactive
- `GameScene` UI/HUD polish도 기본값은 MCP scene/prefab repair이며, code-driven builder/rebuild 경로를 새 authoring 루프로 다시 도입하지 않는다

`Invoke-GarageReadyFlowSmoke.ps1` is no longer a required regression gate.
Keep it only as an optional supervised script when investigating Ready/Save UX, and prefer EditMode tests for those rules.

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Screenshot capture requires Play Mode and a project-relative output path.
- `ui/button/invoke` remains as a legacy alias, but new consumers should use `ui/invoke`.
- `server.js` continues to expose the stable manual-automation routes as MCP tools.
