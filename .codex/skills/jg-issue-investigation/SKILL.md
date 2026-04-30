---
name: jg-issue-investigation
description: >-
  Project-specific issue investigation guardrail for the JG repo. Use this skill whenever Codex is asked to find a bug cause, diagnose a problem, identify root cause, verify a hypothesis, explain why something failed, prevent recurrence, or handle uncertain phrases such as "아마", "추정", "probably", "likely", or "seems" in a cause analysis. This skill routes investigation reporting through the repo owner docs so unverified hypotheses are not reported as rootCause or success.
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

## References

- `AGENTS.md`
- `docs/index.md`
- `ops.acceptance-reporting-guardrails`
