---
name: jg-unity-workflow
description: "JG Unity workflow. Triggers: scenes, prefabs, UITK, UI policy, Stitch handoff, MCP, compile, Play Mode, meta GUID, runtime smoke."
---

# JG Unity Workflow

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: skill.jg-unity-workflow
> role: skill-entry
> owner_scope: JG Unity lane read order, owner doc routing, MCP and validation entrypoint
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails, ops.unity-ui-authoring-workflow
> artifacts: none

Use this skill for JG-specific Unity execution order and sources of truth.
Keep generic Unity serialization, MCP/CLI theory, and broad engine rules in `rule-unity`.
If a document name moved, resolve the current path through `docs/index.md` first and then follow the owner doc by `doc_id`.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate scenes, prefabs, or runtime evidence from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` when you need the current doc routes.
3. Read owner doc `ops.cohesion-coupling-policy` when the task needs owner boundaries, cohesion/coupling judgment, or responsibility splitting.
4. If the task touches Unity UI or UX authoring, UI Toolkit candidate surfaces, legacy runtime UI scripts, new UI prefabs, or workflow policy checks, read owner doc `ops.unity-ui-authoring-workflow` before any implementation.
5. If the task depends on Stitch handoff, source freeze, execution contracts, source visual contracts, translation, or `.stitch` artifacts, read owner doc `ops.stitch-data-workflow` before translating them into Unity work.
6. If the task touches Unity MCP, Play Mode automation, or runtime smoke, read `tools/unity-mcp/README.md` as execution reference.
7. If the task creates or substantially rewrites a plan doc, read owner doc `ops.plan-authoring-review-workflow` before editing the plan.
8. If the task needs acceptance, blocked/mismatch wording, or closeout judgment, read owner doc `ops.acceptance-reporting-guardrails`.
9. If the task depends on current project priorities or recent recovery work, skim the relevant plan in `docs/plans/`.
10. If a task clearly needs extra architecture or initialization docs and they exist, read them. Do not stop if they are absent.

## JG Defaults

- Prefer Unity MCP and in-editor changes over direct `.unity` or `.prefab` YAML edits.
- In `Plan Mode`, inspect Unity paths and compile truth only. Do not run mutation flows through MCP, scene/prefab edits, or smoke scripts that rewrite evidence.
- Treat serialized scene and prefab state as runtime truth for scene-owned wiring.
- Do not reintroduce code-driven builder or rebuild loops when the repo expects MCP prefab or scene authoring.
- Never overwrite an open scene on disk. If Unity already has the scene loaded, keep the work inside MCP or switch scenes first.
- Stop play mode before script edits that need recompilation, then wait for compile to settle before testing again.
- Run `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1` before closeout when the task changes Unity UI authoring surfaces.
- New Stitch-driven UI starts as an `Assets/UI` UI Toolkit candidate surface first; existing runtime UI scripts are compatibility code until replaced.
- If a generated `.csproj` looks stale, confirm Unity/Bee compile truth and Unity Test Runner inclusion first. Do not patch the generated `.csproj` file directly.

## Task Routing

- `scene/prefab authoring`: use MCP first, inspect current hierarchy and serialized refs, then mutate the smallest possible surface.
- `code-only`: edit scripts normally, then refresh compile state before Play Mode validation.
- `mixed`: inspect the scene contract first, then keep scene edits and script edits easy to localize.
- `plan authoring`: after drafting or substantial plan edits, run the repeat re-review loop from `ops.plan-authoring-review-workflow` before closeout.
- `acceptance closeout`: use `ops.acceptance-reporting-guardrails` for mechanical vs acceptance separation and blocked/mismatch judgment.

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
- For Lobby and Garage Stitch imports, prefer the `jg-stitch-unity-import` UI Toolkit candidate route before touching legacy runtime prefabs.
- For GameScene UI and HUD work, default to MCP prefab or scene authoring, not builder regeneration.
- If a smoke script depends on stale UI paths or fragile runtime clone names, repair the automation contract instead of forcing the scene back to match the old path.
- When a reset renames a hierarchy node, keep translators and smoke helpers compatible with both legacy and current paths until every caller and contract document has migrated.

## Stitch Translation Loop

When Unity work implements an accepted Stitch screen, use this loop:

1. Lock the accepted source freeze first.
2. Open the accepted Stitch `png/html` alongside the Unity surface when practical.
3. Confirm the execution contracts were derived from that source freeze.
4. Confirm the real target asset path before touching hierarchy.
5. Read the execution contracts before touching hierarchy so required refs and source-derived presentation values are known up front.
6. Build or repair the smallest UI Toolkit candidate surface that can carry the contract.
7. Compare Unity output against the source screen before calling the pass done.

Semantic contract alone is not enough for overlay recovery.
If the surface depends on scrim, dialog bounds, typography weight, CTA labels, or emphasis hierarchy, close the source-derived presentation contract first.
Otherwise the translator usually only recreates a skeleton surface.

## Source Visual Contract Rules

- Treat `screen manifest` as semantic meaning only.
- Treat `unity-map` as binding only.
- Treat `presentation-contract` as source-derived presentation only.
- Do not hand-author literal presentation values and then label them as source-derived truth.
- If `extractionStatus != resolved`, do not call the translation pass done.
- `pending-source-derivation` is a valid intermediate state, not a translation-ready success state.

## Troubleshooting

- `missing child path`
  - `/prefab/get` may throw 500 instead of returning a clean miss. Use a missing-path-safe wrapper and verify the actual hierarchy again.
- `missing component on prefab/set`
  - `/prefab/set` fails if the target component is absent. Ensure the component before applying the property.
- `stale console errors`
  - Compare console timestamps so old failures do not get mistaken for the latest translation run.
- `Prefab Mode capture vs runtime view`
  - `sceneview/capture` in Prefab Mode is good for structure and presentation sanity checks, but it is not the same as runtime/mobile framing.
- `source re-read`
  - In supported Stitch screen structures, source fact and presentation profile tools rebuild the execution inputs from html/png before candidate work continues. Do not treat optional profile dumps as hand-maintained owner files.
- `skeleton-only translation`
  - When semantic contract + map succeed but the UI still looks like a placeholder, the missing layer is usually source-derived presentation extraction.

## Guardrails From Recent Import Work

- New intermediate layers must record their status explicitly instead of silently becoming runtime truth.
- If a presentation field is still uncertain, keep it in `unresolvedDerivedFields` and leave the contract in `pending-source-derivation`.
- Do not use translator-side constants or fallback to paper over missing presentation data.

## Fidelity Checks

For Stitch-driven UI translation, verify more than hierarchy:

- `structure fidelity`: block order and direction still match the source
- `emphasis fidelity`: first read, selected state, and focused work area still dominate
- `completion fidelity`: preview / summary / empty states do not look like placeholders
- `CTA fidelity`: primary save or commit action keeps its intended weight and persistence

If structure is technically valid but one of the checks above clearly regressed, keep the pass open.

## Layout Cautions

Root-surface translation often fails on layout math rather than missing refs.
Watch for these first:

- stretch anchors mixed with fixed height `sizeDelta` on layout-group children
- scroll content missing `ContentSizeFitter` or preferred-height behavior
- a source `strip` or `row` accidentally translated into a vertical list
- a sticky dock accidentally translated into an inline button block

## Architecture Guardrails

- `*Setup` and `*Root` classes are wiring-only entry points.
- `async void` is forbidden; use thin wrappers that delegate to `Task` methods.
- `Resources.Load`, `transform.Find`, and runtime child traversal are restricted to approved seams only.
- `scene registry` objects are scene-owned contract components, not runtime repair targets.
- `FindFirstObjectByType<*SceneRegistry>` is forbidden.
- `AddComponent<*SceneRegistry>` is forbidden.
- Runtime-spawned object arrival must use explicit bootstrap registration or a scene-local registrar, never global registry lookup fallback.

## Refactor Checklist

- If a change needs a registry, add it to the scene or prefab contract and wire it through serialized references.
- If a runtime-spawned object must announce arrival, route it through explicit bootstrap registration or a scene-local registrar.
- Do not restore hidden scene repair paths just to make a smoke or temporary flow pass.
- If a generated `.csproj` looks stale, treat Unity/Bee compile inputs and Unity Test Runner inclusion as the canonical execution source of truth.

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
- `ops.cohesion-coupling-policy`
- `ops.stitch-data-workflow`
- `ops.unity-ui-authoring-workflow`
- `tools/unity-mcp/README.md`
- `docs/plans/progress.md`
