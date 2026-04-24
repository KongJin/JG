# Unity MCP

> 마지막 업데이트: 2026-04-23
> 상태: active
> doc_id: tools.unity-mcp-readme
> role: reference
> owner_scope: Unity MCP 실행 reference, helper route, verification command guide
> upstream: repo.agents, docs.index, ops.unity-ui-authoring-workflow, plans.mcp-improvement
> artifacts: `tools/unity-mcp/`, `Assets/Editor/UnityMcp/`, `artifacts/unity/`

Unity MCP in this repo is a `diagnostic + manual automation` bridge.
The prior Lobby/Garage `LobbyScene.unity` workflow is now a historical route.
While the repo is rebuilding UI from scratch, default to `prefab-first reset`:
accepted Stitch handoff -> presentation contract review -> baseline prefab wiring -> new scene assembly -> fresh contract/translation pipeline.
Unity UI/UX authoring policy 본문 owner는 `ops.unity-ui-authoring-workflow`이고, current path는 `docs/index.md`에서 해석한다. 이 문서는 실행 reference만 담당한다.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- Prefab-pack helper module: `tools/unity-mcp/McpPrefabPackHelpers.ps1`
- Workflow policy check: `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- Workflow gate: `tools/unity-mcp/Invoke-CodexLobbyUiWorkflowGate.ps1` - legacy scene route only
- Shared surface translation entry: `tools/stitch-unity/surfaces/Invoke-StitchSurfaceTranslation.ps1`

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
5. For reset work, continue with `prefab wiring review -> new scene assembly -> fresh contract/translation pipeline`

If compile errors remain, `play/start`, the workflow gate, and verification helpers can fail with misleading timeout symptoms. Treat that as a compile-clean failure first, not as an MCP failure.

## Recommended Workflow

This order assumes compile-clean state and completed script reload.

Use this order for current Lobby/Garage reset work:

1. `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
2. confirm accepted handoff + required presentation refs
3. rebuild baseline prefab wiring first
4. assemble a new scene
5. run fresh contract/translation pipeline only after the new scene exists

Historical note:

- `Invoke-CodexLobbyUiWorkflowGate.ps1` is not the default route while `Assets/Scenes/LobbyScene.unity` is absent.
- If you intentionally revive a concrete Lobby/Garage authoring scene later, add a dedicated runtime proof on top of the gate instead of reviving the old smoke scripts.
- Keep the historical route summary in [HISTORICAL_LOBBY_SCENE_ROUTE.md](./HISTORICAL_LOBBY_SCENE_ROUTE.md).

## SSOT Guardrails

- Never overwrite an open `.unity` scene on disk while Unity has that scene loaded.
- Prefer MCP scene/prefab repair over direct YAML replacement for Lobby/Garage UI work.
- If a disk-level restore is truly required, switch away from the scene or close Unity first, then restore the file, then reopen it in Unity.
- The open-scene popup (`The following open scene(s) have been changed on disk`) is treated as a workflow violation, not as a prompt to accept casually.

## Historical Lobby/Garage Route

This route is historical while `Assets/Scenes/LobbyScene.unity` is absent.
If a concrete Lobby/Garage authoring scene is intentionally revived later:

- use `GET /scene/verify-lobby-contract` and the legacy gate again
- keep scene serialization as the runtime truth for that route
- re-check the historical route note in [HISTORICAL_LOBBY_SCENE_ROUTE.md](./HISTORICAL_LOBBY_SCENE_ROUTE.md) before reviving old acceptance proofs

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

Use this for repeated Stitch-to-Unity prefab imports so surface generation stays behind the shared generator entry and reusable prefab-pack helpers.

Current translation entry:

- `tools/stitch-unity/surfaces/Invoke-StitchSurfaceTranslation.ps1`
  - shared public entry for accepted Stitch surface translation
  - current policy disables the prior constant-owned surface generator route
  - only contract-complete translators should be reintroduced under a new strategy id

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
- presentation responsibility lint when `*PageController` classes changed
- fresh `CodexLobby` workflow gate evidence for Lobby/Garage source changes when the legacy scene route is active

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
4. machine-readable result writeout

Outputs:

- `artifacts/unity/lobby-ui-workflow-result.json`
- `Temp/PresentationLayoutOwnershipValidator/presentation-layout-ownership.json`

The workflow gate is a `compile-clean after reload` validation step. It is not the recovery path for unresolved compile errors.
The workflow gate is also not an exemption path for runtime layout authoring inside `Features.*.Presentation`; those violations fail before contract review.

Use this as the required gate for Lobby/Garage UI changes only when the legacy scene route is intentionally active.

## Runtime Proof

Legacy runtime smoke scripts were removed from the active toolset.
Current runtime proof should be surface-specific and generated from the active contract or translation pipeline instead of relying on old scene-route smoke scripts.

Recommended runtime proof order:

1. workflow gate when a legacy authoring scene is truly active
2. surface contract review
3. translation artifact review
4. pipeline result review

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Screenshot capture requires Play Mode and a project-relative output path.
- `ui/button/invoke` remains as a legacy alias, but new consumers should use `ui/invoke`.
- `server.js` continues to expose the stable manual-automation routes as MCP tools.
