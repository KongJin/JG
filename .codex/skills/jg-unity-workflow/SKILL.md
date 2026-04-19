---
name: jg-unity-workflow
description: Project-specific Unity workflow for the JG repo. Use when Codex works on Unity scenes, prefabs, MCP editor automation, runtime smoke scripts, or Unity-facing docs in this repository, especially under `Assets/Scenes`, `Assets/Resources`, `Assets/Editor/UnityMcp`, or `tools/unity-mcp`.
---

# JG Unity Workflow

Use this skill for JG-specific Unity execution order and sources of truth.
Keep generic Unity serialization, MCP/CLI theory, and broad engine rules in `rule-unity`.

## Read First

1. Read `AGENTS.md`.
2. If the task touches Unity MCP, Play Mode automation, scene repair, or runtime smoke, read `tools/unity-mcp/README.md`.
3. If you need faster doc routing, read `references/jg-doc-map.md`.
4. If the task depends on current project priorities or recent recovery work, skim the relevant plan in `docs/plans/`.
5. If a task clearly needs extra architecture or initialization docs and they exist, read them. Do not stop if they are absent.

## JG Defaults

- Prefer Unity MCP and in-editor changes over direct `.unity` or `.prefab` YAML edits.
- Treat serialized scene and prefab state as runtime truth for scene-owned wiring.
- Do not reintroduce code-driven builder or rebuild loops when the repo already expects MCP scene or prefab repair.
- Never overwrite an open scene on disk. If Unity already has the scene loaded, keep the work inside MCP or switch scenes first.
- Stop play mode before script edits that need recompilation, then wait for compile to settle before testing again.

## Task Routing

- `scene/prefab authoring`: use MCP first, inspect current hierarchy and serialized refs, then mutate the smallest possible surface.
- `code-only`: edit scripts normally, then refresh compile state before Play Mode validation.
- `mixed`: inspect the scene contract first, then keep scene edits and script edits easy to localize.

## MCP Loop

### 1. Preflight

- Check `/health`.
- Confirm the correct scene plus `isPlaying` and `isCompiling`.
- Inspect exact target paths and component types with `/scene/hierarchy`, `/gameobject/find`, `/component/get`, and related helpers.
- Treat examples, templates, and copied payloads as placeholders until they are verified against the local hierarchy.

### 2. Mutation

- Prefer the smallest change set that solves the task.
- Preserve existing serialized references unless rewiring is the task.
- When adding helper visuals or wrapper objects, make sure they do not accidentally block input or hide the intended node.

### 3. Postflight

- Save through MCP.
- Re-read the changed node or component when the change could easily land on the wrong target.
- Inspect `/console/logs` or `/console/errors`.
- Capture the right evidence for the task: screenshot, contract check, or smoke output.

## JG Workflow Rules

- Use the existing PowerShell helpers under `tools/unity-mcp` when a defined workflow already exists.
- For Lobby and Garage work, follow the repo's authored flow: contract or workflow gate first, then canonical smoke, then feature smoke only when needed.
- For GameScene UI and HUD work, default to MCP scene or prefab repair, not builder regeneration.
- If a smoke script depends on stale UI paths or fragile runtime clone names, repair the automation contract instead of forcing the scene back to match the old path.

## Validation Order

Use this stack unless the task clearly needs more:

1. Structural or static checks first.
2. Direct EditMode coverage for policy, mapper, presenter, and domain behavior.
3. Wiring or runtime smoke after the cheaper checks pass.

Do not rely on reflection or smoke as the primary safety net when direct editor coverage is practical.

## References

- `AGENTS.md`
- `tools/unity-mcp/README.md`
- `docs/plans/progress.md`
- `references/jg-doc-map.md`
