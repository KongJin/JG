---
name: jg-stitch-unity-import
description: "JG Stitch-to-Unity import workflow. Use for importing/comparing Stitch screens into UITK candidates, UXML/USS, UIDocument, previews, fidelity closeout."
---

# JG Stitch Unity Import

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-stitch-unity-import
> role: skill-entry
> owner_scope: accepted Stitch screen을 Unity candidate/runtime route로 연결하는 라우터
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill when importing, comparing, or preparing an accepted Stitch screen for Unity UI Toolkit candidates, UXML/USS, UIDocument, preview scenes, captures, or fidelity closeout.
This skill is a router. It does not own source-freeze, Unity authoring, or acceptance policy.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate `.stitch`, Unity assets, scene/prefab files, generated evidence, docs, or skill files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read `docs/ops/stitch_data_workflow.md` for source freeze, active contracts, and handoff ownership.
4. Read `docs/ops/stitch_structured_handoff_contract.md` for manifest/map/presentation completeness.
5. Read `docs/ops/unity_ui_authoring_workflow.md` for UI Toolkit candidate, runtime replacement, and evidence gates.
6. Read `docs/design/ui_foundations.md` for Lobby/Garage layout, tokens, and component vocabulary.
7. Read `docs/ops/acceptance_reporting_guardrails.md` before reporting pilot success, runtime success, blocked, mismatch, or accepted.
8. Read `tools/stitch-unity/README.md` and `tools/unity-mcp/README.md` for execution commands when needed.

## Route

1. Default to UI Toolkit candidate/pilot for "bring over", "try", or visual comparison requests.
2. Treat runtime replacement as a separate pass unless target, binding, and acceptance evidence are explicitly locked.
3. Keep source freeze and contract ownership in Stitch owner docs; keep Unity authoring and evidence ownership in Unity owner docs.
4. Route owner-boundary questions through `jg-coupling-review`.
5. Route actual scene/prefab/MCP execution through `jg-unity-workflow`.

## Report Shape

When useful, report source identity, route, output assets, runtime target touched/untouched, evidence, known gaps, and acceptance verdict.
Never report a pilot as accepted runtime replacement.
