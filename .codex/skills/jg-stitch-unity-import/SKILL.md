---
name: jg-stitch-unity-import
description: Project-specific Stitch-to-Unity UI import workflow for the JG repo. Use this skill whenever the user asks to bring, import, port, translate, reimport, apply, compare, or review a Stitch screen in Unity, especially for UI Toolkit pilots, runtime replacement candidates, prefab-first reset surfaces, Set A/B/C/D/E screens, accepted source html/png, UXML/USS, UIDocument, PanelSettings, preview scenes, capture evidence, scoped workflow policy, or visual fidelity closeout. This skill turns an accepted Stitch source into a Unity candidate surface without confusing pilot success with active runtime acceptance.
---

# JG Stitch Unity Import

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: skill.jg-stitch-unity-import
> role: skill-entry
> owner_scope: accepted Stitch screen을 Unity 후보 surface로 가져오는 반복 실행 루틴
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `.stitch/`, `Assets/UI/`, `Assets/Scenes/`, `Assets/Prefabs/`, `artifacts/unity/`

Use this skill for the execution loop that sits between the thin Stitch router and the Unity authoring router.
It does not own policy, source-freeze rules, or acceptance definitions.
Resolve current paths through `docs/index.md`, route cohesion/coupling questions through `ops.cohesion-coupling-policy`, then follow the active owner docs.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate `.stitch`, Unity assets, scene/prefab files, generated evidence, docs, or skill files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md`.
3. Read `ops.cohesion-coupling-policy` for owner boundaries.
4. Read `ops.stitch-data-workflow` for source freeze and contract ownership.
5. Read `ops.unity-ui-authoring-workflow` for allowed Unity authoring routes and evidence.
6. Read `ops.acceptance-reporting-guardrails` before using `success`, `blocked`, `mismatch`, or `accepted`.
7. Read `jg-stitch-workflow` when the task starts from prompt briefs, `.stitch` sources, or contract preparation.
8. Read `jg-unity-workflow` when the task touches Unity scenes, prefabs, Play Mode, MCP, or workflow policy.

## Default Route

- Default to a **candidate pilot first** when the user asks to "bring over" or "try" a Stitch screen.
- Treat runtime replacement as a separate pass unless the user explicitly asks for replacement and the active target, binding, and acceptance evidence are clear.
- Prefer UI Toolkit pilot surfaces for WebGL-friendly mobile UI experiments, while keeping existing uGUI runtime surfaces intact until replacement acceptance is locked.
- Use prefab-first reset only when an existing Unity route has been intentionally discarded or the owner plan already chooses it.

## Import Loop

1. **Lock the source**
   - Confirm the accepted Stitch `html/png` or active source freeze.
   - Do not mix multiple variants in one import pass.
   - Extract first-read order: header/slot/nav/editor/preview/summary/CTA or the equivalent blocks.

2. **Choose the Unity route**
   - `UI Toolkit pilot`: create UXML/USS plus PanelSettings, UIDocument, preview scene, capture, and report.
   - `uGUI/prefab patch`: use MCP scene/prefab authoring and existing serialized refs.
   - `prefab-first reset`: rebuild from source-derived contracts and keep scene assembly as the later step.
   - Record whether the pass is `pilot`, `runtime candidate`, or `active replacement`.

3. **Translate safely**
   - Do not copy Tailwind/CSS directly into USS.
   - Convert to UI Toolkit-safe layout: flex, explicit mobile reference size, real child elements for decoration, and fixed persistent docks.
   - Avoid unsupported USS patterns: `display:grid`, gradients, `box-shadow`, `calc()`, pseudo-elements, and `z-index`.
   - Keep visible text in the source language and keep icon placeholders explicit if real icon assets are not wired.

4. **Generate evidence**
   - Capture the Unity result at the target mobile frame when practical.
   - Write a compact report with source, outputs, capture path, scope, preserved reading order, known gaps, and visual review.
   - If this is a pilot, state `not runtime replacement`, `static sample`, `binding remaining`, and `visual gaps`.

5. **Validate and close out**
   - Run compile/reload checks needed by the changed surface.
   - Inspect Unity console errors after capture or Play Mode evidence.
   - Run `Invoke-UnityUiAuthoringWorkflowPolicy.ps1` with an explicit `-ChangedFile` list for this pass so unrelated dirty worktree state does not pollute the verdict.
   - Run `npm run --silent rules:lint` when docs, skills, or managed artifacts changed.
   - If a plan doc changed, run the plan rereview loop and leave `plan rereview: clean` or an explicit residual.

## Acceptance Language

- `pilot success`: candidate surface renders and evidence exists, but active runtime is unchanged.
- `runtime success`: active target was replaced or patched, binding works, and fresh runtime capture/smoke matches the locked acceptance.
- `mismatch`: comparison was performed and the Unity result does not match the source hierarchy, density, preview completeness, or CTA posture.
- `blocked`: source, target, binding, route, evidence, or policy is not sufficient to judge acceptance.

Never report a pilot as accepted runtime replacement.
Never fix a policy/tooling blocker inside the same surface import and call the surface accepted.

## Report Checklist

Use this checklist in the final report or artifact:

- Source freeze path or Stitch screen identity
- Route: UI Toolkit pilot, uGUI patch, or prefab-first reset
- Output assets and capture path
- Runtime target touched or explicitly untouched
- First-read hierarchy preserved or changed
- Static/binding/model-preview gaps
- Compile, console, scoped workflow policy, and docs lint results
- Acceptance verdict: pilot success, runtime success, mismatch, or blocked

## Test Prompts

See `evals/evals.json` for the first three test prompts:

- Garage UI Toolkit pilot import
- Lobby runtime replacement request
- SetB visual review of an imported candidate

## References

- `docs/index.md`
- `ops.cohesion-coupling-policy`
- `ops.stitch-data-workflow`
- `ops.unity-ui-authoring-workflow`
- `ops.acceptance-reporting-guardrails`
- `.codex/skills/jg-stitch-workflow/SKILL.md`
- `.codex/skills/jg-unity-workflow/SKILL.md`
