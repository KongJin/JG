---
name: jg-doc-lifecycle
description: "JG 문서/skill owner lifecycle 라우터. Triggers: owner 이동, stale path/doc_id, skill route/trigger, progress slim, docs/plans 삭제/압축/병합/정리."
---

# JG Document Lifecycle

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-doc-lifecycle
> role: skill-entry
> owner_scope: JG document lifecycle read order, owner routing, stale trace cleanup, closeout entrypoint
> upstream: repo.agents, docs.index, ops.document-management-workflow, ops.cohesion-coupling-policy, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails, ops.skill-routing-registry, ops.skill-trigger-matrix
> artifacts: none

Use this skill for JG repo document lifecycle, owner routing, docs/plans cleanup, skill route/trigger, stale trace, and closeout work.
This skill is a router. It does not own document policy.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, skills, generated artifacts, or closeout files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths and active document registry.
3. Read owner doc `docs/ops/document_management_workflow.md`.
4. Read `docs/ops/cohesion_coupling_policy.md` when judging whether content belongs together or should split.
5. Read `docs/ops/plan_authoring_review_workflow.md` when creating, deleting, compressing, or substantially changing `docs/plans/*`.
6. Read `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, residual, or closeout language.
7. Read `docs/ops/skill_routing_registry.md` and `docs/ops/skill_trigger_matrix.md` when skill route or trigger wording changes.

## Route

1. Let `ops.document-management-workflow` own SSOT, role, lifecycle, stale trace, recurrence carryover, closeout, validation, and deletion criteria.
2. Let `ops.cohesion-coupling-policy` own cohesion/coupling definitions and owner-boundary review gates.
3. Let `plans.progress` own current state, active owner, next action, and short residuals.
4. Let `ops.skill-routing-registry` and `ops.skill-trigger-matrix` own skill route names and trigger fixtures only.
5. Keep repo-local skills thin; if a skill starts carrying policy body, move that body to its owner doc.

## Closeout

For large document or skill work, leave owner impact, doc lifecycle checked, and skill trigger checked only when those checks actually ran.
