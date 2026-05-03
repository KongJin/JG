---
name: jg-forward-rule-capture
description: "JG correction-to-rule capture. Triggers: 앞으로는, 다음부터, 같은 실수, 새 세션도, 규칙화, skill로 남겨."
---

# JG Forward Rule Capture

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-forward-rule-capture
> role: skill-entry
> owner_scope: 사용자 교정 문구를 새 세션에도 남는 owner 문서/skill 규칙으로 흡수하는 라우터
> upstream: repo.agents, docs.index, ops.codex-coding-guardrails, ops.document-management-workflow, ops.skill-routing-registry, ops.skill-trigger-matrix
> artifacts: none

Use this skill when the user says `앞으로는`, `다음부터`, `같은 실수`, `새 세션도`, `규칙화`, or asks to make a behavior durable in docs/skills.
Also use it before you write an assistant promise like `앞으로는 ...` after user correction.

This skill is a router. It does not own the policy body.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, skills, generated artifacts, or closeout files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve owner paths.
3. Read `docs/ops/codex_coding_guardrails.md`, especially `Forward Rule Capture`, for the behavior rule.
4. Read `docs/ops/document_management_workflow.md` for owner/lifecycle rules.
5. Read `docs/ops/skill_routing_registry.md` and `docs/ops/skill_trigger_matrix.md` if creating or changing a skill trigger.

## Route

1. Use `ops.codex-coding-guardrails` as the primary owner for assistant behavior such as ambiguity handling, asking before risky interpretation, and durable correction capture.
2. Use `ops.document-management-workflow` for lifecycle mechanics: owner choice, closeout, stale trace checks, and mutation-mode handling.
3. Use `ops.skill-routing-registry` and `ops.skill-trigger-matrix` only when the trigger surface or repo-local skill route changes.
4. Keep this skill thin. If this file starts restating policy body, move that body back to the owner document.
