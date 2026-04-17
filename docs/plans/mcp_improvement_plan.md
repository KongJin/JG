# Unity MCP Refactor Plan

> Last updated: 2026-04-17
> Status: in progress

Unity MCP is not being retired. In this repo it is now defined as a `diagnostic + manual automation` tool.

## Target Role

- Keep `rule-harness` usage limited to compile/status refresh plus generic diagnostics.
- Keep scene-specific runtime smoke out of harness scope.
- Make Play/UI/screenshot flows stable enough for supervised manual automation such as Garage capture.

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
- `tools/unity-mcp/Invoke-GarageManualSmoke.ps1` is the reference supervised smoke script.

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

- Play lifecycle succeeds in sequence:
  - `play/start`
  - `play/wait-for-play`
  - `play/stop`
  - `play/wait-for-stop`
- Garage manual automation succeeds in sequence:
  - open `Assets/Scenes/CodexLobbyScene.unity`
  - enter Play Mode
  - invoke `GarageTabButton`
  - wait for `GaragePageRoot`
  - capture screenshot
  - exit Play Mode
- Wrapper, helpers, and docs describe the same stable/manual/experimental split.

## Notes

- The goal is not unlimited scene automation.
- The goal is a reliable supervised workflow for diagnosis and repeatable runtime capture.
