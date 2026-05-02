---
name: jg-coding-guardrails
description: "JG 구현/버그수정/리팩터/테스트 가드레일. Triggers: 코딩 오류, 가정, 명확화, 성공 기준, 단순한 변경, TDD, 회귀 테스트, 검증 기준."
---

# JG Coding Guardrails

> 마지막 업데이트: 2026-05-02
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
5. If the task changes skill route or trigger wording, read `docs/ops/skill_routing_registry.md` and `docs/ops/skill_trigger_matrix.md`.
6. Read `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, root-cause, or acceptance language.
7. Read the relevant lane owner docs for architecture, Unity, Stitch, validation, product design, or feature-local contracts.
8. If the task mentions fallback, silent fail, runtime repair, hidden lookup, missing contract, or pending/review data being treated as success, read `.codex/skills/jg-no-silent-fallback/SKILL.md`.
9. Read the concrete files, tests, logs, scene/prefab contracts, or artifacts being changed.

## Working Flow

1. Lock the target and success criteria.
2. Identify assumptions that are already answerable from repo evidence.
3. Ask only when unresolved ambiguity would affect product direction, architecture, API/schema, scene contract, UX, or another hard-to-reverse choice.
4. For refactors, split the work into small behavior-preserving steps with a feedback loop before editing.
5. Choose the smallest cohesive change that satisfies the target.
6. Make surgical edits only inside the affected owner boundary.
7. Clean up only unused code created by this change.
8. Validate with the narrowest meaningful compile, lint, test, smoke, or static check.
9. Report mechanical status separately from actual acceptance.

## Boundaries

- This skill covers general coding behavior. It does not replace architecture, Unity, Stitch, validation, document lifecycle, or feature owner docs.
- Route document lifecycle execution through `jg-doc-lifecycle`.
- Route cohesion/coupling review through `jg-coupling-review`.
- Route Unity scene, prefab, UI Toolkit, MCP, or runtime smoke work through `jg-unity-workflow` and the relevant Unity skills.
- Route root-cause investigation through `jg-issue-investigation`.
- Route silent fallback, runtime repair, missing-contract masking, and pending/review success claims through `jg-no-silent-fallback`.

## References

- `AGENTS.md`
- `docs/index.md`
- `docs/ops/codex_coding_guardrails.md`
- `docs/ops/document_management_workflow.md`
- `docs/ops/acceptance_reporting_guardrails.md`
- `.codex/skills/jg-no-silent-fallback/SKILL.md`
