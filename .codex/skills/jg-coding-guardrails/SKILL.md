---
name: jg-coding-guardrails
description: >-
  Project-specific coding guardrails for the JG repo. Use this skill whenever Codex is asked to implement code, fix bugs, refactor, add or adjust tests, define validation criteria, reduce LLM coding errors, handle assumptions before coding, keep changes simple, make surgical edits, or respond to Korean requests involving "LLM coding 오류", "가정", "단순함", "외과적 변화", "검증 기준", "코딩 오류", "과잉 구현", "추측 구현", or "테스트/검증". This skill routes implementation work through the repo's coding guardrails SSOT before following the relevant architecture, Unity, document, or feature owner docs.
---

# JG Coding Guardrails

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: skill.jg-coding-guardrails
> role: skill-entry
> owner_scope: JG coding implementation guardrail routing and validation-first workflow entrypoint
> upstream: repo.agents, docs.index, ops.codex-coding-guardrails, ops.document-management-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill as the entrypoint for JG implementation, bugfix, refactor, and validation work.
Do not restate the guardrail policy here. Resolve current paths through `docs/index.md`, then follow `ops.codex-coding-guardrails` and the relevant lane owner docs.

If the current collaboration mode is `Plan Mode`, use this skill for inspection and planning only. Do not mutate docs, code, scenes, prefabs, skills, generated artifacts, or evidence files.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read owner doc `docs/ops/codex_coding_guardrails.md`.
4. Read `docs/ops/document_management_workflow.md` if the task changes docs, rules, skill triggers, or closeout language.
5. Read `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, root-cause, or acceptance language.
6. Read the relevant lane owner docs for architecture, Unity, Stitch, validation, product design, or feature-local contracts.
7. Read the concrete files, tests, logs, scene/prefab contracts, or artifacts being changed.

## Working Flow

1. Lock the target and success criteria.
2. Identify assumptions that are already answerable from repo evidence.
3. Ask only when unresolved ambiguity would affect product direction, architecture, API/schema, scene contract, UX, or another hard-to-reverse choice.
4. Choose the smallest cohesive change that satisfies the target.
5. Make surgical edits only inside the affected owner boundary.
6. Clean up only unused code created by this change.
7. Validate with the narrowest meaningful compile, lint, test, smoke, or static check.
8. Report mechanical status separately from actual acceptance.

## Boundaries

- This skill covers general coding behavior. It does not replace architecture, Unity, Stitch, validation, document lifecycle, or feature owner docs.
- Route document lifecycle execution through `jg-doc-lifecycle`.
- Route cohesion/coupling review through `jg-coupling-review`.
- Route Unity scene, prefab, UI Toolkit, MCP, or runtime smoke work through `jg-unity-workflow` and the relevant Unity skills.
- Route root-cause investigation through `jg-issue-investigation`.

## References

- `AGENTS.md`
- `docs/index.md`
- `docs/ops/codex_coding_guardrails.md`
- `docs/ops/document_management_workflow.md`
- `docs/ops/acceptance_reporting_guardrails.md`
