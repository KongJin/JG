---
name: jg-unity-workflow
description: "JG Unity owner workflow. Triggers: JG scene/prefab authoring, UITK/UI policy, Stitch handoff, MCP evidence, Unity compile/smoke routing."
---

# JG Unity Workflow

> 마지막 업데이트: 2026-05-04
> 상태: active
> doc_id: skill.jg-unity-workflow
> role: skill-entry
> owner_scope: JG Unity lane read order, owner doc routing, MCP and validation entrypoint
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails, ops.unity-ui-authoring-workflow
> artifacts: none

Use this skill for JG Unity scene/prefab authoring, UI Toolkit/UI policy, Stitch handoff execution, MCP evidence, compile, and smoke routing.
This skill is a router. It does not own Unity policy, engine mechanics, or acceptance definitions.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate scenes, prefabs, Unity assets, runtime evidence, docs, or skill files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read `docs/owners/ui-workflow/unity-ui-authoring-workflow.md` for Unity UI/UX authoring routes, forbidden paths, and evidence gates.
4. Read `docs/owners/operations/cohesion-coupling-policy.md` when owner boundaries, responsibility split, runtime contract, or hidden dependency matters.
5. Read `docs/owners/ui-workflow/stitch_data_workflow.md` and `docs/owners/ui-workflow/stitch_structured_handoff_contract.md` when the Unity work depends on Stitch source freeze or execution contracts.
6. Read `docs/owners/operations/acceptance_reporting_guardrails.md`, especially Fresh Evidence Discipline, before using acceptance, blocked, mismatch, closeout, visual/capture, or fresh-evidence language.
7. Read `tools/unity-mcp/README.md` for MCP routes, compile preflight, locks, helper commands, and execution caveats.
8. Read `docs/plans/current/progress.md` or relevant active plan when current priority, residual, or runtime smoke owner matters.

## Route

1. UI/UX work starts from `ops.unity-ui-authoring-workflow`; use the policy script before closeout when Unity UI authoring surfaces changed.
2. Generic Unity serialization, GUID/meta, Unity API, batchmode, or broad engine mechanics route to `rule-unity` as reference, with repo owner docs still taking priority.
3. Stitch-to-Unity candidate work routes through `jg-stitch-unity-import` before runtime replacement.
4. Code-only implementation still uses `jg-coding-guardrails`; root-cause work uses `jg-issue-investigation`; hidden fallback uses `jg-no-silent-fallback`.
5. Scene/prefab work prefers MCP and serialized contract inspection over direct YAML edits.
6. If Unity already has a scene open, follow `tools/unity-mcp/README.md` open-scene disk-write guardrails.

## Architecture Guardrails

This is a routing checklist only; detailed policy remains in the owner docs.

- `*Setup` and `*Root` classes are wiring-only entry points.
- `async void` is forbidden in production flow code unless Unity's event API requires that exact signature.
- `Resources.Load`, `transform.Find`, and runtime child traversal are not scene/prefab contract repair paths.
- `FindFirstObjectByType<*SceneRegistry>` is forbidden for scene registry recovery.
- `AddComponent<*SceneRegistry>` is forbidden for scene registry recovery.

## Refactor Checklist

1. Keep scene/prefab serialized contracts separate from runtime code changes.
2. Verify the narrowest direct EditMode or workflow check that covers the changed contract.
3. Report mechanical pass and runtime acceptance separately.

## Boundary

Keep detailed MCP commands in `tools/unity-mcp/README.md`, UI authoring policy in `ops.unity-ui-authoring-workflow`, acceptance wording in `ops.acceptance-reporting-guardrails`, and owner-boundary judgment in `ops.cohesion-coupling-policy`.
