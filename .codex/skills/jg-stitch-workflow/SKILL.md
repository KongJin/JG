---
name: jg-stitch-workflow
description: "JG Stitch workflow. Use for .stitch/DESIGN.md, prompt briefs, contracts, screen generation, and Unity handoff preparation."
---

# JG Stitch Workflow

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-stitch-workflow
> role: skill-entry
> owner_scope: JG Stitch lane read order, owner doc routing, artifact entrypoint
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract
> artifacts: none

Use this skill for `.stitch`, prompt briefs, source freeze, execution contracts, screen generation, and Unity handoff preparation.
This skill is a router. It does not own Stitch workflow policy or artifact rules.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate `.stitch` artifacts, handoff outputs, docs, skills, generated artifacts, or evidence files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read `docs/owners/ui-workflow/stitch_data_workflow.md`.
4. Read `docs/owners/ui-workflow/stitch_structured_handoff_contract.md` when the task writes, reviews, or translates contracts.
5. Read `docs/owners/design/ui_reference_workflow.md` when the task needs Stitch visual principles or reference usage.
6. Read `docs/owners/ui-workflow/unity-ui-authoring-workflow.md` before Unity handoff execution.
7. Read `docs/owners/operations/acceptance_reporting_guardrails.md` before using acceptance, blocked, mismatch, or closeout language.
8. Read `docs/owners/operations/plan_authoring_review_workflow.md` when creating or substantially rewriting plan docs.

## Route

1. Let `ops.stitch-data-workflow` own source freeze, working data, active/inactive artifact ownership, and Unity handoff operation.
2. Let `ops.stitch-structured-handoff-contract` own manifest/map/presentation contract structure and completeness.
3. Let `design.ui-reference-workflow` own why/how Stitch is used for UI exploration.
4. Switch to `jg-stitch-unity-import` or `jg-unity-workflow` when the task starts Unity candidate or runtime work.

## Boundary

Always preserve the owner route `source freeze -> execution contracts -> Unity candidate/output`.
Do not let stored contract files replace the accepted source freeze as the starting point.
