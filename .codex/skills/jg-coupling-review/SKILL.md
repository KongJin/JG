---
name: jg-coupling-review
description: "JG 응집도/결합도 리뷰 라우터. Triggers: owner boundary, responsibility split, dependency, interface/API shape, test seam, mock, split/merge 판단."
---

# JG Coupling Review

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-coupling-review
> role: skill-entry
> owner_scope: JG cohesion/coupling review routing, owner boundary judgment entrypoint
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.document-management-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill to route cohesion, coupling, owner boundary, seam, interface/API, mock, and test-surface review.
This skill is a router. It does not own cohesion/coupling policy.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, code, scenes, prefabs, tools, generated artifacts, or evidence files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read owner doc `docs/ops/cohesion-coupling-policy.md`.
4. Read `docs/ops/document_management_workflow.md` if the review becomes document lifecycle execution.
5. Read `docs/ops/acceptance_reporting_guardrails.md` before using acceptance, blocked, mismatch, residual, or root-cause language.
6. Read relevant lane owner docs and the concrete files, docs, scene/prefab contracts, scripts, or tool entrypoints being reviewed.

## Route

1. Let `ops.cohesion-coupling-policy` own the definition of cohesion, coupling, review gates, hidden dependency, and hard-fail boundaries.
2. Route document deletion, compression, registry, stale trace, or skill trigger work through `jg-doc-lifecycle`.
3. Route silent fallback, missing-contract masking, runtime repair, or hidden lookup through `jg-no-silent-fallback`.
4. Route implementation through the relevant workflow skill after the owner/seam verdict is clear.

## Report Shape

When useful, report `Verdict`, `Reason-to-change`, `Owner`, `Coupling observed`, `Recommended move`, and `Validation`.
Keep detailed judgment criteria in the owner doc, not in this skill.
