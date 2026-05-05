---
name: jg-issue-investigation
description: "JG 원인조사 가드레일. Triggers: 버그 원인, 왜 안 돼, 가설 검증, 성능 저하, 재발 방지, maybe/seems류 추정 표현."
---

# JG Issue Investigation

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-issue-investigation
> role: skill-entry
> owner_scope: JG issue/root-cause investigation routing and hypothesis verification entrypoint
> upstream: repo.agents, docs.index, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill when investigating causes, regressions, recurrence, performance issues, or uncertain hypotheses in the JG repo.
This skill is a router. It does not own root-cause or acceptance-reporting policy.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, code, scenes, prefabs, skills, generated artifacts, or evidence files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read owner doc `docs/owners/operations/acceptance_reporting_guardrails.md`, especially Root Cause Investigation, Recurrence Check, and Fresh Evidence Discipline.
4. Read the concrete code, docs, logs, tests, artifacts, scene/prefab contracts, captures, or runs involved in the issue.

## Route

1. Use `ops.acceptance-reporting-guardrails` for hypothesis verification, cause wording, blocked/mismatch/success language, and recurrence checks.
2. Route implementation through `jg-coding-guardrails` only after evidence points to a fix path.
3. Route owner-boundary or hidden-dependency findings through `jg-coupling-review`.
4. Route missing-contract masking through `jg-no-silent-fallback`.
5. Route Unity visual/capture issues through `jg-unity-workflow` for fresh evidence.

## Report Shape

When useful, report `확인된 사실`, `가설`, `검증`, `판정`, and `남은 불확실성`.
Do not report an unverified hypothesis as root cause.
