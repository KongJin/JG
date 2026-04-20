# Unity MCP

> 마지막 업데이트: 2026-04-20
> 상태: active
> doc_id: tools.unity-mcp-readme
> role: reference
> owner_scope: Unity MCP 실행 reference, helper route, smoke command guide
> upstream: repo.agents, docs.index, ops.unity-ui-authoring-workflow, plans.mcp-improvement
> artifacts: `tools/unity-mcp/`, `Assets/Editor/UnityMcp/`, `artifacts/unity/`

Unity MCP in this repo is a `diagnostic + manual automation` bridge.
The prior Lobby/Garage `LobbyScene.unity` workflow is now a historical route.
While the repo is rebuilding UI from scratch, default to `prefab-first reset`:
accepted Stitch handoff -> presentation contract review -> baseline prefab wiring -> new scene assembly -> fresh contract/smoke.
Unity UI/UX authoring policy 본문 owner는 `ops.unity-ui-authoring-workflow`이고, current path는 `docs/index.md`에서 해석한다. 이 문서는 실행 reference만 담당한다.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- Prefab-pack helper module: `tools/unity-mcp/McpPrefabPackHelpers.ps1`
- Workflow policy check: `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- Workflow gate: `tools/unity-mcp/Invoke-CodexLobbyUiWorkflowGate.ps1` - legacy scene route only
- Canonical page-switch smoke: `tools/unity-mcp/Invoke-LobbyGaragePageSwitchSmoke.ps1` - legacy scene route only
- Feature smoke: `tools/unity-mcp/Invoke-GarageSettingsOverlaySmoke.ps1`
- Feature smoke: `tools/unity-mcp/Invoke-GameSceneSummonSmoke.ps1`
- Feature smoke: `tools/unity-mcp/Invoke-GameScenePlacementWaveSmoke.ps1`
- Optional supervised smoke: `tools/unity-mcp/Invoke-GarageReadyFlowSmoke.ps1`

`rule-harness` continues to use Unity MCP only for compile/status refresh plus generic diagnostics. Scene-specific runtime smoke stays out of harness scope.

## Core Routes

These are the routes the current workflow depends on.

- `GET /health`
- `POST /scene/open`
- `POST /scene/save`
- `POST /prefab/open-stage`
- `GET /prefab/current-stage`
- `POST /prefab/close-stage`
- `POST /play/start`
- `POST /play/stop`
- `POST /play/wait-for-play`
- `POST /play/wait-for-stop`
- `POST /ui/invoke`
- `POST /ui/get-state`
- `POST /ui/wait-for-active`
- `POST /ui/wait-for-inactive`
- `POST /screenshot/capture`
- `POST /sceneview/capture`
- `GET /validation/verify-presentation-layout-ownership`
- `GET /scene/verify-lobby-contract`

`/menu/execute` still exists, but it is manual-only and non-authoritative for Lobby/Garage recovery.

## MCP Preflight

Before using Unity MCP for prefab authoring, scene assembly, Play Mode automation, screenshots, or smoke:

1. Run `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
2. If there are compile errors, fix them before entering any Unity MCP workflow
3. After the fix, wait for compile and script reload to settle with `Invoke-McpCompileRequestAndWait` or `Invoke-EditorProjectSync.ps1`
4. Confirm `/health` reports `isCompiling = false`
5. For reset work, continue with `prefab wiring review -> new scene assembly -> fresh contract/smoke`

If compile errors remain, `play/start`, the workflow gate, and smoke scripts can fail with misleading timeout symptoms. Treat that as a compile-clean failure first, not as an MCP failure.

## Recommended Workflow

This order assumes compile-clean state and completed script reload.

Use this order for current Lobby/Garage reset work:

1. `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
2. confirm accepted handoff + required presentation refs
3. rebuild baseline prefab wiring first
4. assemble a new scene
5. run fresh contract/smoke only after the new scene exists

Historical note:

- `Invoke-CodexLobbyUiWorkflowGate.ps1` and `Invoke-LobbyGaragePageSwitchSmoke.ps1` are not the default route while `Assets/Scenes/LobbyScene.unity` is absent.
- If you intentionally revive a concrete Lobby/Garage authoring scene later, those scripts can become active again.

## SSOT Guardrails

- Never overwrite an open `.unity` scene on disk while Unity has that scene loaded.
- Prefer MCP scene/prefab repair over direct YAML replacement for Lobby/Garage UI work.
- If a disk-level restore is truly required, switch away from the scene or close Unity first, then restore the file, then reopen it in Unity.
- The open-scene popup (`The following open scene(s) have been changed on disk`) is treated as a workflow violation, not as a prompt to accept casually.

## LobbyScene Contract

This section describes the historical Lobby/Garage scene route.
Use it only after a concrete `Assets/Scenes/LobbyScene.unity` authoring scene exists again.

For Lobby/Garage UI work, scene serialization is the runtime truth.

- Runtime SSOT: `Assets/Scenes/LobbyScene.unity`
- Contract audit route: `GET /scene/verify-lobby-contract`
- Repair path: use MCP to modify the scene or prefabs directly until the contract passes again

Required sentinel nodes:

- `/Canvas/LobbyPageRoot`
- `/Canvas/GaragePageRoot`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot/GarageMobileTabBar`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent`
- `/Canvas/GaragePageRoot/GarageHeaderRow/SettingsButton`
- `/Canvas/GaragePageRoot/GarageSettingsOverlay`
- `/Canvas/GaragePageRoot/GarageSettingsOverlay/AccountCard`
- `/Canvas/GaragePageRoot/MobileSaveDock`
- `/Canvas/GaragePageRoot/MobileSaveDock/MobileSaveButton`
- `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent/RosterListPane/MobileSlotGrid`
- `/Canvas/LobbyPageRoot/RoomListPanel`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/ListHeaderRow`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface/EmptyStateText`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard`
- `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard/GarageTabButton`
- `/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton`

Representative serialized reference checks:

- `/Canvas/GaragePageRoot::GaragePageController._rosterListView`
- `/Canvas/GaragePageRoot::GaragePageController._unitEditorView`
- `/Canvas/GaragePageRoot::GaragePageController._resultPanelView`
- `/Canvas/GaragePageRoot::GaragePageController._unitPreviewView`
- `/Canvas/GaragePageRoot::GaragePageController._mobileContentRoot`
- `/Canvas/GaragePageRoot::GaragePageController._mobileBodyHost`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSlotHost`
- `/Canvas/GaragePageRoot::GaragePageController._rightRailRoot`
- `/Canvas/GaragePageRoot::GaragePageController._mobileTabBar`
- `/Canvas/GaragePageRoot::GaragePageController._mobileEditTabButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobileEditTabLabel`
- `/Canvas/GaragePageRoot::GaragePageController._mobilePreviewTabButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobilePreviewTabLabel`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSummaryTabButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSummaryTabLabel`
- `/Canvas/GaragePageRoot::GaragePageController._garageHeaderSummaryText`
- `/Canvas/GaragePageRoot::GaragePageController._settingsOpenButton`
- `/Canvas/GaragePageRoot::GaragePageController._settingsOpenButtonLabel`
- `/Canvas/GaragePageRoot::GaragePageController._settingsOverlayRoot`
- `/Canvas/GaragePageRoot::GaragePageController._settingsCloseButton`
- `/Canvas/GaragePageRoot::GaragePageController._settingsCloseButtonLabel`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSaveDockRoot`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSaveButton`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSaveButtonLabel`
- `/Canvas/GaragePageRoot::GaragePageController._mobileSaveStateText`
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
- 현재 Garage shell은 desktop/mobile 분기 없이 mobile-first 단일 구조를 기준으로 유지한다.

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
- `Get-McpLobbyContract`
- `Invoke-McpPrepareLobbyPlaySession`
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

`McpPrefabPackHelpers.ps1` centralizes the reusable prefab-pack authoring loop:

- common MCP UI authoring helpers for scratch prefab composition
- `New-McpScratchCanvas`
- `Invoke-McpPrefabPackGeneration`

Use this for repeated Stitch-to-Unity prefab imports so feature scripts only keep the set-specific seed builders and prefab map definitions.

Use `Assert-McpNoOpenSceneDiskWrite` before any script that would touch a `.unity` file on disk outside the editor bridge.
If the target matches `health.activeScenePath`, the helper fails fast and tells you to use MCP repair or switch scenes first.

Prefab Mode can now be opened through MCP as well:

- `POST /prefab/open-stage` with `assetPath`
- `GET /prefab/current-stage`
- `POST /prefab/close-stage`
- `POST /sceneview/capture` to save the current SceneView, including Prefab Mode context when open

Use this when prefab-authoring work benefits from an explicit Prefab Mode stage instead of editing only through asset read/write routes.

When Unity already has the project open and you need generated `.csproj` files refreshed for editor tests or IDE sync, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-EditorProjectSync.ps1
```

This uses the current editor instance through MCP and performs:

1. bridge health check
2. compile/reload stabilization
3. `Assets/Open C# Project` menu execution

## Workflow Policy Check

Run the workflow policy check like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
```

Outputs:

- `artifacts/unity/unity-ui-authoring-workflow-policy.json`

The policy check reads the current changed files from git and enforces:

- route classification for `scene/prefab authoring`, `presentation-code`, `mixed`, `lobby-ui`, `game-scene-ui`
- no new UI prefab creation by default
- presentation validator requirements when presentation code changed
- fresh `CodexLobby` workflow evidence for Lobby/Garage source changes

It does not replace the workflow SSOT.
Keep the policy body in owner doc `ops.unity-ui-authoring-workflow` and use this README as the execution reference. Resolve the current file path through `docs/index.md`.

## Workflow Gate

Run the required Lobby/Garage gate like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-CodexLobbyUiWorkflowGate.ps1
```

The gate performs:

1. compile/reload stabilization
2. presentation layout ownership verification
3. contract verification
4. canonical page-switch smoke
5. machine-readable result writeout

Outputs:

- `artifacts/unity/lobby-ui-workflow-result.json`
- `artifacts/unity/lobby-garage-page-switch-result.json`
- `Temp/PresentationLayoutOwnershipValidator/presentation-layout-ownership.json`

The workflow gate is a `compile-clean after reload` validation step. It is not the recovery path for unresolved compile errors.
The workflow gate is also not an exemption path for runtime layout authoring inside `Features.*.Presentation`; those violations fail before contract and smoke.

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
The canonical output contract is `390x844`.
The script now normalizes each screenshot to that exact frame with a centered crop/resize pass, so the artifact does not depend on the currently open GameView size.

## Feature Smoke

Run the Garage settings overlay smoke like this:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GarageSettingsOverlaySmoke.ps1
```

Outputs:

- `artifacts/unity/garage-settings-smoke-before-open.png`
- `artifacts/unity/garage-settings-smoke-open.png`
- `artifacts/unity/garage-settings-smoke-closed.png`
- `artifacts/unity/garage-settings-smoke-result.json`

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
3. Garage settings overlay smoke when the change touches Garage account/settings placement
4. `GameScene` summon smoke when the change reaches lobby-to-game flow

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
