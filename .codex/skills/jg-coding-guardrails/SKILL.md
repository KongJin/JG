---
name: jg-coding-guardrails
description: "JG 구현/버그수정/리팩터/테스트 가드레일. Triggers: 코딩 오류, 모호한 제품 범위, 가정, 명확화, 성공 기준, TDD, 회귀 테스트."
---

# JG Coding Guardrails

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-coding-guardrails
> role: skill-entry
> owner_scope: JG coding implementation guardrail routing and validation-first workflow entrypoint
> upstream: repo.agents, docs.index, ops.codex-coding-guardrails, ops.document-management-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill for JG implementation, bugfix, refactor, and validation work.
This skill is a router. It does not own the guardrail policy body.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, code, scenes, prefabs, skills, generated artifacts, or evidence files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read owner doc `docs/owners/operations/codex_coding_guardrails.md`.
4. Read `docs/owners/operations/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, root-cause, or acceptance language.
5. Read `docs/owners/operations/document_management_workflow.md` if the task changes docs, rules, skill triggers, or closeout language.
6. Read the relevant lane owner docs and concrete files, tests, logs, contracts, or artifacts being changed.

## Route

1. Use `ops.codex-coding-guardrails` for assumption handling, clarification, mutation gate, refactor slicing, validation-first execution, and forward-rule capture.
2. Use `jg-no-silent-fallback` when the task mentions fallback, silent fail, runtime repair, hidden lookup, missing contract, or pending/review success.
3. Use `jg-issue-investigation` when the task is primarily cause finding or hypothesis verification.
4. Use `jg-coupling-review` when owner boundary, seam, interface, module depth, or mock/test surface judgment matters.
5. Use domain workflow skills for Unity, Stitch, game design, or document lifecycle before implementing in that lane.

## Boundary

Keep implementation habits and detailed policy in owner docs. This skill should stay limited to trigger recognition, read order, and route selection.
