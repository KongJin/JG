# Unity MCP

> 마지막 업데이트: 2026-04-24
> 상태: active
> doc_id: tools.unity-mcp-readme
> role: reference
> owner_scope: Unity MCP 실행 reference, helper route, verification command guide
> upstream: repo.agents, docs.index, ops.unity-ui-authoring-workflow, plans.mcp-improvement
> artifacts: `tools/unity-mcp/`, `Assets/Editor/UnityMcp/`, `artifacts/unity/`

Unity MCP in this repo is a `diagnostic + manual automation` bridge.
While the repo is rebuilding UI from scratch, default to `prefab-first reset`:
accepted Stitch handoff -> presentation contract review -> baseline prefab wiring -> new scene assembly -> fresh contract/translation pipeline.
Unity UI/UX authoring policy 본문 owner는 `ops.unity-ui-authoring-workflow`이고, current path는 `docs/index.md`에서 해석한다. 이 문서는 실행 reference만 담당한다.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- Prefab-pack helper module: `tools/unity-mcp/McpPrefabPackHelpers.ps1`
- Workflow policy check: `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
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
- `POST /sceneview/capture`
- `GET /validation/verify-presentation-layout-ownership`
`/menu/execute` still exists, but it is manual-only and non-authoritative for Lobby/Garage recovery.

## MCP Preflight

Before using Unity MCP for prefab authoring, scene assembly, Play Mode automation, screenshots, or smoke:

1. Run `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
2. If there are compile errors, fix them before entering any Unity MCP workflow
3. After the fix, wait for compile and script reload to settle with `Invoke-McpCompileRequestAndWait` or `Invoke-EditorProjectSync.ps1`
4. Confirm `/health` reports `isCompiling = false`
5. For reset work, continue with `prefab wiring review -> new scene assembly -> fresh contract/translation pipeline`

If compile errors remain, `play/start`, the workflow gate, and verification helpers can fail with misleading timeout symptoms. Treat that as a compile-clean failure first, not as an MCP failure.

## MCP Operation Guardrails

Unity Editor Play Mode, MCP scene changes, screenshots, and runtime smoke share one editor state. Only one runtime or UI/UX lane should own those operations at a time.
Other lanes can continue static review, docs, or compile-readonly work, but they should not start Play Mode or capture smoke evidence until the active MCP lane is finished.

Runtime smoke helpers should take the MCP operation lock unless a human is intentionally running a supervised manual check.
Use lane-specific evidence paths, for example:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameScenePlacementSmoke.ps1 -Owner GameSceneUIUX -OutputPath artifacts/unity/game-scene-placement-smoke.json
```

If a helper reports that `Temp/UnityMcp/runtime-smoke.lock` is held, treat the smoke as `blocked` for this lane instead of stopping the other lane's Play Mode session.

## Recommended Workflow

This order assumes compile-clean state and completed script reload.

Use this order for current Lobby/Garage reset work:

1. `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
2. confirm accepted handoff + required presentation refs
3. rebuild baseline prefab wiring first
4. assemble a new scene
5. run fresh contract/translation pipeline only after the new scene exists

## SSOT Guardrails

- Never overwrite an open `.unity` scene on disk while Unity has that scene loaded.
- Prefer MCP scene/prefab repair over direct YAML replacement for Lobby/Garage UI work.
- If a disk-level restore is truly required, switch away from the scene or close Unity first, then restore the file, then reopen it in Unity.
- The open-scene popup (`The following open scene(s) have been changed on disk`) is treated as a workflow violation, not as a prompt to accept casually.

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
- accepted Stitch prefab review는 `TempScene review prep -> POST /sceneview/capture`를 기본 route로 본다
- `POST /screenshot/capture`는 현재 Stitch prefab review 표준 경로가 아니다. 일반 수동 점검용 route로만 본다.

Use `Assert-McpNoOpenSceneDiskWrite` before any script that would touch a `.unity` file on disk outside the editor bridge.
If the target matches `health.activeScenePath`, the helper fails fast and tells you to use MCP repair or switch scenes first.

Prefab Mode can now be opened through MCP as well:

- `POST /prefab/open-stage` with `assetPath`
- `GET /prefab/current-stage`
- `POST /prefab/close-stage`
- `POST /sceneview/capture` to save the current SceneView, including Prefab Mode context when open

Use this when prefab-authoring work benefits from an explicit Prefab Mode stage instead of editing only through asset read/write routes.

## Prefab Authoring Caveats

이번 reset lane에서 확인된 caveat는 아래와 같다.

- `GET /prefab/get`
  - missing child path에서 조용히 `found=false`를 주지 않고 500으로 실패할 수 있다.
  - 따라서 existence check helper는 500을 missing-path로 흡수해야 한다.
- `POST /prefab/set`
  - target component가 실제로 붙어 있지 않으면 바로 실패한다.
  - property apply 전에 required component를 먼저 ensure 해야 한다.
- `/console/logs`와 `/console/errors`
  - stale error가 남아 있을 수 있으니 timestamp로 현재 실패와 과거 실패를 구분해야 한다.
- `POST /sceneview/capture`
  - current Stitch prefab review 기본 capture route다.
  - Prefab Mode SceneView capture는 runtime/mobile framing과 동일하지 않으므로 staging proof로만 본다.

한 줄 기준:

`prefab/get`은 missing-path safe wrapper가 필요하고, `prefab/set`은 ensure-component 이후에만 호출하는 것이 기본이다.

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
- prefab-first reset evidence for Lobby/Garage source changes while no authoring scene exists

It does not replace the workflow SSOT.
Keep the policy body in owner doc `ops.unity-ui-authoring-workflow` and use this README as the execution reference. Resolve the current file path through `docs/index.md`.

## Runtime Proof

Legacy runtime smoke scripts were removed from the active toolset.
Current runtime proof should be surface-specific and generated from the active contract or translation pipeline instead of relying on old scene-route smoke scripts.

Recommended runtime proof order:

1. surface contract review
2. translation artifact review
3. runtime smoke only after the active surface contract exists
4. pipeline result review

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Screenshot capture requires Play Mode and a project-relative output path.
- `ui/button/invoke` remains as a legacy alias, but new consumers should use `ui/invoke`.
- `server.js` continues to expose the stable manual-automation routes as MCP tools.
