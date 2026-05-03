---
name: jg-no-silent-fallback
description: "JG silent fallback 방지 라우터. Triggers: fallback, silent fail, runtime repair, hidden lookup, contract 누락, pending/review 성공 처리."
---

# JG No Silent Fallback

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-no-silent-fallback
> role: skill-entry
> owner_scope: JG silent fallback, runtime repair, missing-contract masking prevention routing
> upstream: repo.agents, docs.index, ops.codex-coding-guardrails, ops.cohesion-coupling-policy, ops.acceptance-reporting-guardrails, plans.technical-debt-recurrence-prevention
> artifacts: none

Use this skill when a task mentions fallback, silent fail, runtime repair, hidden lookup, missing contract, pending/review data as success, or a broken preview/output being shown as normal.
This skill is a router. It does not own fail-closed policy.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, code, scenes, prefabs, skills, generated artifacts, or evidence files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read `docs/ops/codex_coding_guardrails.md`, especially Fail-Closed Contract Rule.
4. Read `docs/ops/cohesion_coupling_policy.md` before judging hidden coupling or fallback ownership.
5. Read `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, root-cause, or recurrence language.
6. Read `docs/plans/technical_debt_recurrence_prevention_plan.md` when the issue is runtime repair or default-value masking recurrence.
7. Read the concrete files, tests, scene/prefab contracts, generated assets, reports, or captures showing the missing contract.

## Route

1. Let `ops.codex-coding-guardrails` own fail-closed behavior and production contract rules.
2. Let `ops.cohesion-coupling-policy` own hidden coupling and owner-boundary judgment.
3. Let `ops.acceptance-reporting-guardrails` own acceptance, blocked, mismatch, root-cause, and recurrence wording.
4. Route implementation through `jg-coding-guardrails`, Unity work through `jg-unity-workflow`, and verified investigation through `jg-issue-investigation`.

## Report Shape

When useful, report `Missing contract`, `Current masking path`, `Verdict`, `Required move`, and `Validation`.
Keep detailed fail-closed criteria in owner docs.
