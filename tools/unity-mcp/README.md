# Unity MCP

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: tools.unity-mcp-readme
> role: reference
> owner_scope: Unity MCP 실행 reference, helper route, verification command guide
> upstream: repo.agents, docs.index, ops.unity-ui-authoring-workflow
> artifacts: `tools/unity-mcp/`, `Assets/Editor/UnityMcp/`, `artifacts/unity/`

Unity MCP in this repo is a `diagnostic + manual automation` bridge.
While the repo is rebuilding UI from scratch, default to `UI Toolkit candidate surface`:
accepted Stitch handoff -> source visual contract review -> UI Toolkit candidate surface -> preview scene/capture -> fresh contract/translation pipeline.
Unity UI/UX authoring policy 본문 owner는 `ops.unity-ui-authoring-workflow`이고, current path는 `docs/index.md`에서 해석한다. 이 문서는 실행 reference만 담당한다.

- Bridge core: `Assets/Editor/UnityMcp/`
- MCP stdio wrapper: `tools/unity-mcp/server.js`
- Helper module: `tools/unity-mcp/McpHelpers.ps1`
- Workflow policy check: `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- Stitch source/contract tooling: `tools/stitch-unity/`

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
- `GET /uitk/state`
- `POST /uitk/get-state`
- `POST /uitk/set-value`
- `POST /uitk/invoke`
- `POST /uitk/wait-for-element`
- `POST /sceneview/capture`
`/menu/execute` still exists, but it is manual-only and non-authoritative for Lobby/Garage recovery.

UGUI/Canvas automation routes under `/ui/*`, `/snapshot/ui`, and `/explore/interactive` are disabled for this project.
Use the `/uitk/*` routes for `UIDocument` and `VisualElement` inspection or interaction.

## MCP Preflight

Before using Unity MCP for prefab authoring, scene assembly, Play Mode automation, screenshots, or smoke:

1. Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
2. If there are compile errors, fix them before entering any Unity MCP workflow
3. After the fix, wait for compile and script reload to settle with `Invoke-McpCompileRequestAndWait` or `Invoke-EditorProjectSync.ps1`
4. Confirm `/health` reports `isCompiling = false`
5. For reset work, continue with `UI Toolkit candidate surface -> preview capture -> fresh contract/translation pipeline`

If compile errors remain, `play/start`, the workflow gate, and verification helpers can fail with misleading timeout symptoms. Treat that as a compile-clean failure first, not as an MCP failure.

## MCP Operation Guardrails

Unity Editor Play Mode, MCP scene changes, screenshots, and runtime smoke share one editor state. Only one runtime or UI/UX lane should own those operations at a time.
Other lanes can continue static review, docs, or compile-readonly work, but they should not start Play Mode or capture smoke evidence until the active MCP lane is finished.

Runtime smoke helpers should take the shared Unity resource lock plus their lane-specific MCP operation lock unless a human is intentionally running a supervised manual check.
Use lane-specific evidence paths, for example:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameScenePlacementSmoke.ps1 -Owner GameSceneUIUX -OutputPath artifacts/unity/game-scene-placement-smoke.json
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameSceneMobileHudFramingSmoke.ps1 -Owner GameSceneMobileHudFraming -OutputPath artifacts/unity/game-flow/game-scene-mobile-hud-framing-smoke.json -PlacementOutputPath artifacts/unity/game-flow/game-scene-mobile-hud-placement-source.json -ScreenshotPath artifacts/unity/game-flow/game-scene-mobile-hud-framing.png -LeavePlayMode
```

`Invoke-GameScenePlacementSmoke.ps1` and `Invoke-GameSceneMobileHudFramingSmoke.ps1` are now fail-closed legacy artifacts.
They still document the old `/LobbyCanvas` and `/BattleHudCanvas` contract, but return `blockedReason = ugui-smoke-contract-disabled` until a UI Toolkit runtime smoke replaces them.

If a helper reports that `Temp/UnityMcp/unity-resource.lock` or `Temp/UnityMcp/runtime-smoke.lock` is held, treat the smoke as `blocked` for this lane instead of stopping the other lane's Play Mode session.
If the lock holder process no longer exists, helpers may clear that stale lock and continue; live process locks remain authoritative.
Nested helpers must run under the parent helper's lock and record that fact in their artifact instead of competing for the same lock.
The old placement and mobile HUD smoke path contracts are not accepted evidence anymore; build replacement checks against UIDocument/VisualElement selectors.
Mobile HUD framing snapshots should use bounded `/uitk/get-state` requests so a stuck UI query does not hold the runtime smoke lock indefinitely.

## Recommended Workflow

This order assumes compile-clean state and completed script reload.

Use this order for current Lobby/Garage UI Toolkit candidate work:

1. `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
2. confirm accepted handoff + source visual contract
3. create or update the UXML/USS candidate surface
4. capture the preview scene
5. run runtime replacement only after the candidate evidence is accepted

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
- `Invoke-UnityMcpEditModeTests.ps1`
- `Invoke-McpSceneOpenAndWait`
- `Invoke-McpPlayStartAndWaitForBridge`
- `Invoke-McpPlayStopAndWait`
- `Assert-McpNoOpenSceneDiskWrite`
- `Invoke-McpPrepareLobbyPlaySession`
- `Wait-McpPhotonLobbyReady`
- `Get-McpConsoleSummary`
- `Get-McpUitkState`
- `Get-McpUitkElementState`
- `Set-McpUitkElementValue`
- `Invoke-McpUitkElement`
- `Wait-McpUitkElement`

The older `Get-McpUi*`, `Invoke-McpUi*`, and `Wait-McpUi*` helpers are disabled UGUI-era compatibility traces.
They fail fast instead of calling `/ui/*`; use UITK helpers or `Invoke-McpGameObjectMethod`.

Current UI translation stance:

- `tools/stitch-unity/` remains useful for source facts and contract derivation.
- legacy runtime prefab translators are outside the current UI Toolkit candidate route unless a new strategy explicitly owns them.
- accepted Stitch review는 `UI Toolkit preview scene -> POST /sceneview/capture`를 기본 route로 본다.
- `POST /screenshot/capture`는 현재 Stitch-to-UI Toolkit candidate review 표준 경로가 아니다. 일반 수동 점검용 route로만 본다.

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
  - UI Toolkit candidate와 Prefab Mode staging proof에 쓰는 capture route다.
  - Prefab Mode SceneView capture는 runtime/mobile framing과 동일하지 않으므로 staging proof로만 본다.

한 줄 기준:

`prefab/get`은 missing-path safe wrapper가 필요하고, `prefab/set`은 ensure-component 이후에만 호출하는 것이 기본이다.

When Unity already has the project open and you need generated `.csproj` files refreshed for editor tests or IDE sync, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-EditorProjectSync.ps1
```

This uses the current editor instance through MCP and performs:

1. bridge health check
2. compile/reload stabilization
3. `Assets/Open C# Project` menu execution

When Unity is already open and you need targeted EditMode tests, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityMcpEditModeTests.ps1 -TestName Tests.Editor.ArchitectureGuardrailReflectionTests -OutputPath artifacts/unity/architecture-guardrail-reflection-tests.xml
```

The wrapper owns the Unity resource lock, stops an existing Play Mode session before running EditMode tests, and waits for compile to settle. Pass `-PreservePlayMode` only when the current Play Mode session must not be interrupted; in that case the test run blocks instead of stopping the editor.

## Workflow Policy Check

Run the workflow policy check like this:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
```

For a scoped UI Toolkit candidate pass, pass the changed paths as a PowerShell array:

```powershell
$changed = @(
  'Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uxml',
  'Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uss',
  'artifacts/unity/operation-memory-shared-shell-uitk-candidate-report.md'
)
.\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1 -ChangedFile $changed
```

Outputs:

- `artifacts/unity/unity-ui-authoring-workflow-policy.json`

The policy check reads the current changed files from git and enforces:

- route classification for `scene/prefab authoring`, `mixed`, `lobby-ui`, `game-scene-ui`
- route classification for `uitk-candidate` when `Assets/UI/UIToolkit/**` UXML/USS/asset files change
- no new UI prefab creation by default
- UI Toolkit candidate evidence for fresh source changes; policy pass is not runtime acceptance

It does not replace the workflow SSOT.
Keep the policy body in owner doc `ops.unity-ui-authoring-workflow` and use this README as the execution reference. Resolve the current file path through `docs/index.md`.

## Runtime Proof

Current runtime proof should be surface-specific and generated from the active contract or translation pipeline.

Recommended runtime proof order:

1. surface contract review
2. translation artifact review
3. runtime smoke only after the active surface contract exists
4. pipeline result review

## Notes

- The bridge is editor-only and auto-starts on script reload.
- Screenshot capture requires Play Mode and a project-relative output path.
- `ui/button/invoke` and the rest of the UGUI `/ui/*` automation surface are disabled; use `/uitk/*`.
- `server.js` continues to expose the stable manual-automation routes as MCP tools.
