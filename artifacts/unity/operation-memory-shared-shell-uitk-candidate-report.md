# Operation Memory / Shared Shell UITK Candidate Report

> generatedAt: 2026-04-28
> route: uitk-candidate
> runtime replacement: not touched

## Source Freeze

- Operation Memory: `Operation Memory - Accepted Dark Dock` / `753d889cc0874d69858fd17d98c66f7f`
- Shared Shell: `Nova1492 Shared Shell / Navigation Only` / `7a083f26ec05412ca84188517d17d13f`

## Candidate Assets

- `Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uxml`
- `Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uss`
- `Assets/UI/UIToolkit/Shared/TacticalTokens.uss`
- `Assets/UI/UIToolkit/Shared/SharedTopShell.uxml`
- `Assets/UI/UIToolkit/Shared/SharedNavigationBar.uxml`
- `Assets/UI/UIToolkit/Shared/SharedShellNavigation.uxml`
- `Assets/UI/UIToolkit/Shared/SharedShellNavigation.uss`

## Source Fact Evidence

- `artifacts/unity/operation-memory-accepted-dark-dock-source-facts.json`
- `artifacts/unity/nova1492-shared-shell-navigation-only-source-facts.json`

## Preserved Reading Order

- Operation Memory: header -> latest operation -> recent five operations -> unit trace/sync chips -> fixed return dock.
- Shared Shell: quiet top shell -> page workspace placeholder -> boundary note -> shared navigation bar.

## Validation

- UXML XML parse: passed.
- Compile check: passed with `tools/check-compile-errors.ps1`.
- Unity compile/reload: passed through MCP before preview generation.
- SceneView preview capture: generated but visually blank; do not use it for visual acceptance.
- GameView preview smoke: generated for both isolated preview scenes with console error count 0.
- Docs lint: run separately with `npm run --silent rules:lint`.
- Scoped Unity UI workflow policy: rerun after policy hardening; policy pass is not runtime acceptance.

## Preview Evidence

- Operation Memory scene: `Assets/Scenes/OperationMemoryUitkPreview.unity`
- Operation Memory GameView capture: `artifacts/unity/operation-memory-uitk-preview-gameview.png`
- Shared Shell scene: `Assets/Scenes/SharedShellUitkPreview.unity`
- Shared Shell GameView capture: `artifacts/unity/shared-shell-uitk-preview-gameview.png`
- Capture summary: `artifacts/unity/operation-memory-shared-shell-uitk-gameview-smoke.json`
- SceneView mismatch summary: `artifacts/unity/operation-memory-shared-shell-uitk-preview-capture.json`
- Candidate scoped policy: `artifacts/unity/ui-management-uitk-candidate-policy.json`
- Tooling/scenes scoped policy: `artifacts/unity/ui-management-tooling-policy.json`

## Current Verdict

- Pilot surface files and isolated preview evidence created.
- Runtime scene/prefab wiring is intentionally untouched.
- Candidate preview is evidence-ready, but this pass also hardened shared policy/tooling; do not promote it to runtime acceptance.
- The previous `Operation Memory - Return Dock` source was not accepted because the bright background and recent-list structure regressed.
