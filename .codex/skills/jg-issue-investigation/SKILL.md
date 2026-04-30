---
name: jg-issue-investigation
description: >-
  Project-specific issue investigation guardrail for the JG repo. Use this skill whenever Codex is asked to find a bug cause, diagnose a problem, identify root cause, verify a hypothesis, explain why something failed, prevent recurrence, debug uncertain behavior, or respond to Korean requests like "버그 원인", "문제 원인", "원인 파악", "왜 안 돼", "실패 원인", "가설 검증", or "재발 방지". Also use it for uncertain phrases such as "아마", "추정", "가능성", "보임", "보인다", "보여", "듯", "것 같", "maybe", "probably", "likely", "appears", or "seems" in a cause analysis. This skill routes investigation reporting through the repo owner docs so unverified hypotheses are checked before they are treated as rootCause or success.
---

# JG Issue Investigation

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: skill.jg-issue-investigation
> role: skill-entry
> owner_scope: JG issue/root-cause investigation routing and hypothesis verification entrypoint
> upstream: repo.agents, docs.index, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill when investigating causes, root causes, regressions, recurrence, or uncertain hypotheses in the JG repo.
Do not restate the reporting policy here. Resolve current paths through `docs/index.md`, then read owner doc `ops.acceptance-reporting-guardrails`.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve the current owner path.
3. Read owner doc `ops.acceptance-reporting-guardrails` before reporting causes, hypotheses, `success`, `blocked`, `mismatch`, or `rootCause`.
4. Read the concrete code, docs, logs, tests, artifacts, or scene/prefab contracts involved in the issue.

## Investigation Flow

1. Separate observed facts from hypotheses.
2. Verify each non-obvious hypothesis with local evidence before treating it as cause.
3. If the evidence is insufficient, report the result as `blocked` with the missing verification path.
4. Keep mechanical checks, acceptance verdict, and root-cause verdict separate.

## Report Shape

When reporting an investigation result, prefer this compact shape:

- `확인된 사실`: evidence from code, logs, tests, runs, docs, or artifacts
- `가설`: cause candidates that still need verification
- `검증`: what was checked and what evidence confirmed or rejected each hypothesis
- `판정`: confirmed cause, rejected hypothesis, blocked, or mismatch
- `남은 불확실성`: missing evidence, unresolved risk, or next verification path

Keep policy authority in `ops.acceptance-reporting-guardrails`; this skill only shapes the working habit and route.

## References

- `AGENTS.md`
- `docs/index.md`
- `ops.acceptance-reporting-guardrails`
