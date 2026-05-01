---
name: jg-issue-investigation
description: "JG 원인조사 가드레일. Triggers: 버그 원인, 왜 안 돼, 가설 검증, 성능 저하, 재발 방지, maybe/seems류 추정 표현."
---

# JG Issue Investigation

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: skill.jg-issue-investigation
> role: skill-entry
> owner_scope: JG issue/root-cause investigation routing and hypothesis verification entrypoint
> upstream: repo.agents, docs.index, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill when investigating causes, root causes, regressions, recurrence, or uncertain hypotheses in the JG repo.
Every cause-finding task must run the hypothesis verification loop from `ops.acceptance-reporting-guardrails` before reporting a cause.
Do not restate the reporting policy here. Resolve current paths through `docs/index.md`, then read owner doc `ops.acceptance-reporting-guardrails`.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve the current owner path.
3. Read owner doc `ops.acceptance-reporting-guardrails` before reporting causes, hypotheses, `success`, `blocked`, `mismatch`, or `rootCause`.
4. Read the concrete code, docs, logs, tests, artifacts, or scene/prefab contracts involved in the issue.

## Investigation Flow

1. Build a feedback loop before making cause claims.
   - Prefer the narrowest repeatable signal that reaches the symptom: direct/EditMode test, CLI/tool invocation, captured artifact replay, scene/prefab contract probe, Playwright/browser check, or a documented manual/HITL checklist.
   - Make the loop sharper before debugging: assert the exact symptom, reduce unrelated setup, and make time/random/network/scene state as deterministic as practical.
   - If no believable loop can be built, stop the cause claim and report `blocked` with the missing environment, captured artifact, access, or temporary instrumentation needed.
2. Reproduce the user-described failure.
   - Confirm the loop shows the same symptom, not a nearby different failure.
   - Capture the concrete evidence: error text, wrong output, timing baseline, artifact diff, log line, scene/prefab state, or test failure.
   - For nondeterministic issues, raise the reproduction rate enough to test hypotheses, or report the remaining flake rate as uncertainty.
3. Separate observed facts from hypotheses.
4. Generate plausible hypotheses before testing the first one.
   - For each plausible hypothesis, state what evidence would confirm or reject it.
   - Prefer ranked hypotheses when the issue is broad, flaky, or performance-related.
5. Instrument narrowly.
   - Each probe must map to a hypothesis prediction.
   - Prefer debugger/inspection or targeted boundary logs over broad logging.
   - Temporary debug logs must have a unique searchable prefix and be removed before closeout.
6. Fix only after evidence points to a cause.
   - If a correct regression seam exists, turn the minimized repro into a failing check before or alongside the fix.
   - If no correct seam exists, report that as an architecture/testability finding rather than creating a shallow false-confidence test.
   - Re-run the original feedback loop after the fix path.
7. Cleanup and close out.
   - Remove temporary instrumentation and throwaway harnesses unless they are intentionally moved to a clearly owned debug artifact.
   - State which hypothesis was confirmed or rejected.
   - Keep mechanical checks, acceptance verdict, and root-cause verdict separate.

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
