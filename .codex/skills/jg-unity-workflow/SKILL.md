---
name: jg-unity-workflow
description: Project-specific Unity workflow for the JG repo. Use when Codex works on Unity scenes, prefabs, MCP editor automation, runtime smoke scripts, or Unity-facing docs in this repository, especially under `Assets/Scenes`, `Assets/Resources`, `Assets/Editor/UnityMcp`, or `tools/unity-mcp`.
---

# JG Unity Workflow

> 마지막 업데이트: 2026-04-20
> 상태: active
> doc_id: skill.jg-unity-workflow
> role: skill-entry
> owner_scope: JG Unity lane read order, owner doc routing, MCP and validation entrypoint
> upstream: repo.agents, docs.index, ops.unity-ui-authoring-workflow
> artifacts: `tools/unity-mcp/`, `artifacts/unity/`, `Assets/Scenes/`, `Assets/Prefabs/`

Use this skill for JG-specific Unity execution order and sources of truth.
Keep generic Unity serialization, MCP/CLI theory, and broad engine rules in `rule-unity`.
If a document name moved, resolve the current path through `docs/index.md` first and then follow the owner doc by `doc_id`.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` when you need the current doc routes.
3. If the task touches Unity UI or UX authoring, read owner doc `ops.unity-ui-authoring-workflow` before any implementation.
4. If the task depends on Stitch handoff or `.stitch` artifacts, read owner doc `ops.stitch-data-workflow` before translating them into Unity work.
5. If the task touches Unity MCP, Play Mode automation, scene repair, or runtime smoke, read `tools/unity-mcp/README.md` as execution reference.
6. If the task depends on current project priorities or recent recovery work, skim the relevant plan in `docs/plans/`.
7. If a task clearly needs extra architecture or initialization docs and they exist, read them. Do not stop if they are absent.

## JG Defaults

- Prefer Unity MCP and in-editor changes over direct `.unity` or `.prefab` YAML edits.
- Treat serialized scene and prefab state as runtime truth for scene-owned wiring.
- Do not reintroduce code-driven builder or rebuild loops when the repo already expects MCP scene or prefab repair.
- Never overwrite an open scene on disk. If Unity already has the scene loaded, keep the work inside MCP or switch scenes first.
- Stop play mode before script edits that need recompilation, then wait for compile to settle before testing again.
- Run `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1` before closeout when the task changes Unity UI authoring surfaces.

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

## Fast Search

- Scene hierarchy or object names: `rg -n "GameSceneBootstrap|JG_GameScene|ObjectiveCore|WaveSystems|StatusSystems|PlayerSceneTickers" Assets/Scenes Assets/Scripts`
- Scene or runtime contract hints: `rg -n "씬 의존성|씬 계약|initialization|late-join|Runtime Lookup|Setup|Bootstrap|Root" Assets/Scripts/Features/<Name> Assets/Scripts/Shared`
- Unity MCP entrypoints and API usage: `rg -n "UnityMcp|Invoke-|/scene/|/component/" Assets/Editor tools/unity-mcp tools`
- GameScene lanes often needed together: `Assets/Scripts/Features/Player/`, `Assets/Scripts/Features/Wave/`, `Assets/Scripts/Features/Skill/`, plus scene-facing contracts under `Combat/` and `Enemy/`

## Validation Order

Use this stack unless the task clearly needs more:

1. Structural or static checks first.
2. Direct EditMode coverage for policy, mapper, presenter, and domain behavior.
3. Wiring or runtime smoke after the cheaper checks pass.

Do not rely on reflection or smoke as the primary safety net when direct editor coverage is practical.

## References

- `AGENTS.md`
- `docs/index.md`
- `ops.stitch-data-workflow`
- `ops.unity-ui-authoring-workflow`
- `tools/unity-mcp/README.md`
- `docs/plans/progress.md`
