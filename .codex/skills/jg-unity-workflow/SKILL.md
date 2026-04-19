---
name: jg-unity-workflow
description: Project-specific Unity workflow for the JG repo. Use when Codex needs to inspect or modify Unity scenes, prefabs, feature wiring, scene contracts, Unity MCP flows, or Unity-facing docs in this repository. Especially use for tasks involving Assets/Scenes, Assets/Resources prefabs, Assets/Editor/UnityMcp, or agent initialization/architecture docs.
---

# JG Unity Workflow

Follow this workflow for Unity tasks in this repo.

## Read The Right Rules First

1. Read `AGENTS.md` first.
2. If the task involves Unity MCP or editor automation, read `Assets/Editor/UnityMcp/README.md` before acting.
3. If the task changes scene-owned wiring or bootstrap order, read `agent/initialization_order.md`.
4. If the task touches feature boundaries or ports, read `agent/architecture.md` and `agent/anti_patterns.md`.

If one of the `agent/*.md` documents is missing in the workspace, do not stop. Fall back to `AGENTS.md` and the nearest scene/runtime contract already checked into the repo.

## Architecture Guardrails

- `*Setup` and `*Root` classes are wiring-only entry points. They may compose objects, pass dependencies, and coordinate initialization order, but they must not become the place that decides game policy.
- Do not add `Update`, long-running gameplay coroutines, or durable mutable workflow state to `*Setup` or `*Root` classes.
- Presentation views render state and forward UI intents. If a view is deciding eligibility, validation, relock, or save policy, move that logic into a presenter, controller, or application/domain collaborator.
- `async void` is forbidden except for thin UI event wrappers that immediately delegate to a `Task` method.
- Prefer explicit seams over scene-global access. Do not add new singleton/static scene dependencies. Existing ones should be hidden behind a small port or adapter before further reuse.
- `Resources.Load`, `transform.Find`, and runtime child traversal are exceptions, not defaults. If they are unavoidable, isolate them in a dedicated loader/adapter and keep the allowlist small.
- Transport adapters should not own transport, payload codec, and policy decisions at the same time. Split command dispatch, callback translation, mapping, and serialization when an adapter starts to grow.

## Refactor Checklist

Before editing a suspicious class, quickly answer these questions:

1. Is this class deciding what should happen, instead of just wiring collaborators together?
2. Is this `Setup` or `Root` holding onto workflow state that should live elsewhere?
3. Is this adapter mixing transport, parsing, and policy?
4. Is this view making product or domain decisions?
5. Can the new responsibility be extracted without changing scene/prefab contracts?

## Prefer The Safe Path

- Prefer Unity MCP and in-editor changes over direct `.unity` or `.prefab` YAML edits.
- Use direct YAML edits only when MCP cannot perform the operation cleanly.
- If an open scene must be edited on disk, warn that Unity may show a reload popup for externally modified scenes.
- Do not assume script edits apply during play mode. Stop play, wait for compile, then test again.

## Unity MCP Routine

1. Check `/health` first.
2. Confirm `isPlaying` and `isCompiling` before scene or script work.
3. Use `/scene/hierarchy`, `/gameobject/find`, `/component/get`, and `/component/set` to inspect wiring before changing it.
4. After edits, inspect `/console/logs` or `/console/errors`, not just the scene view.
5. For lobby-to-game smoke tests, prefer the existing PowerShell helpers in `tools/`.

## Scene And Prefab Contract Rules

- Treat Inspector references as the source of truth. Fix missing wiring in the scene or prefab, not with runtime fallback.
- If bootstrap order or cross-feature init assumptions change, update `agent/initialization_order.md`.
- If a task modifies scene hierarchy only for readability, preserve runtime behavior and serialized references unless the user explicitly asks for refactoring.

## JG-Specific Checks

- `JG_GameScene` changes usually need a pass over:
  - `agent/initialization_order.md`
- For MCP smoke tests, use the existing scripts under `tools/` instead of inventing ad hoc flows when possible.

## Validation Order

Use this validation stack unless the task clearly needs more:

1. `LayerDependencyAnalyzer` and rule harness for `static-clean` structure checks.
2. Direct EditMode tests in the predefined editor assembly for policy, domain, mapper, and presenter behavior.
3. Reflection or wiring smoke only for Assembly-CSharp exposure and thin availability checks.

Do not treat reflection tests as the primary safety net for business rules when direct editor tests are possible.
Do not introduce a custom test `asmdef` for this repo's editor tests. Keep test code under `Assets/Editor/` and let the predefined `Assembly-CSharp-Editor` assembly own direct EditMode coverage.

## Useful Project References

- Read `references/jg-doc-map.md` when you need the shortest path to the right project docs or validation scripts.
