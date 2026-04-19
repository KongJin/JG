# Unity MCP Refactor Plan

> Last updated: 2026-04-18
> Status: active reference

Unity MCP is not being retired. In this repo it is now defined as a `diagnostic + manual automation` tool.

이 문서는 "무엇을 더 붙일까"보다 "무엇을 핵심으로 남길까"를 설명하는 운영 기준 문서다.

## Target Role

- Keep `rule-harness` usage limited to compile/status refresh plus generic diagnostics.
- Keep scene-specific runtime smoke out of harness scope.
- Keep supervised Play/UI/screenshot flows stable enough for repeatable runtime verification.
- Treat `CodexLobbyScene.unity` as the Lobby/Garage runtime SSOT, not the builder output.

## Current Validation Stack

Lobby/Garage 기준 기본 검증 순서는 아래로 고정한다.

1. contract / required-field audit
2. EditMode or domain tests
3. thin smoke

핵심 원칙:

- scene contract는 `wiring / sentinel roots / serialized refs`를 본다.
- EditMode tests는 `Ready/Save`, roster validation, room rule, 초기 계산 같은 순수 규칙을 본다.
- smoke는 끝까지 연결되는 핵심 사용자 흐름만 본다.

## Current Direction

### 1. Bridge Core

- Use `UnityMcpBridge.RunOnMainThreadAsync(...)` as the required boundary for Unity Editor API access.
- Use a shared editor-state snapshot for canonical state fields:
  - `isPlaying`
  - `isPlayModeChanging`
  - `isCompiling`
  - `activeScene`
  - `activeScenePath`
- Keep success/error payloads aligned around:
  - success: `success`, `message`, `path`, `name`
  - error: `error`, `detail`, `stackTrace`, `hint`

### 2. Stable Automation Routes

Stable routes:

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

Legacy compatibility:

- `POST /ui/button/invoke` remains as a thin alias.
- legacy duplicated handler registration is removed from bridge startup; canonical handlers own the overlapping routes.

### 3. Tooling Alignment

- `tools/unity-mcp/server.js` should expose stable Play/UI/screenshot routes directly as MCP tools.
- `tools/unity-mcp/McpHelpers.ps1` should call stable routes and use explicit wait helpers for Play start/stop.
- `tools/unity-mcp/Invoke-CodexLobbyUiWorkflowGate.ps1` is the required Lobby/Garage entrypoint.
- `tools/unity-mcp/Invoke-LobbyGaragePageSwitchSmoke.ps1` is the canonical runtime smoke script.
- `tools/unity-mcp/Invoke-GameSceneSummonSmoke.ps1` is the retained feature smoke for lobby-to-game bootstrap.
- `Invoke-GarageReadyFlowSmoke.ps1` is optional supervised coverage only and no longer a required regression gate.
- Lobby/Garage repair is scene-owned: verify contract first, then use MCP scene/prefab edits instead of builder-style full regeneration.
- Open scene on-disk overwrite is out of policy. If a direct `.unity` restore is unavoidable, switch scenes or close Unity before touching the file.

### 4. Route Tiers

Stable:

- health, scene current/open/save/hierarchy
- console logs/errors
- play lifecycle
- ui state/invoke/wait
- screenshot capture

Manual-only:

- input simulation
- menu execution
- scene mutation helpers such as create/set/destroy flows

Diagnostic / experimental:

- async monitors
- streaming/filter/stats console helpers
- screenshot comparison
- snapshot/eval/explore helpers

## Acceptance Checks

- compile-clean succeeds
- `GET /scene/verify-codex-lobby-contract` succeeds
- workflow gate succeeds
- page-switch smoke succeeds
- docs, helpers, and remaining smoke scripts describe the same core path

## Notes

- The goal is not unlimited scene automation.
- The goal is a reliable supervised workflow for diagnosis and repeatable runtime capture.
- Decorative UI hierarchy, child label paths, and ad hoc debug snapshots are intentionally out of the canonical smoke contract.
