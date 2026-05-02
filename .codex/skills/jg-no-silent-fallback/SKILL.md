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

Use this skill when a task mentions fallback, silent fail, runtime repair, hidden lookup, missing contract, pending/review data being treated as success, or a broken preview/output being shown as normal.
This skill routes the work to the active owner docs and makes the fail-closed check explicit. Do not restate owner policy as a new source of truth here.

If the current collaboration mode is `Plan Mode`, use this skill for inspection and planning only. Do not mutate docs, code, scenes, prefabs, tools, generated assets, or evidence files.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read owner doc `docs/ops/codex_coding_guardrails.md`, especially the fail-closed contract rule.
4. Read owner doc `docs/ops/cohesion_coupling_policy.md` before judging hidden coupling, runtime lookup, dynamic repair, or fallback ownership.
5. Read owner doc `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, root-cause, or recurrence language.
6. Read `docs/plans/technical_debt_recurrence_prevention_plan.md` when the issue involves production fallback, runtime repair, or default-value masking.
7. Read the relevant feature, Unity, Stitch, design, or plan owner doc for the concrete surface being changed.
8. Read the concrete files, tests, scene/prefab contracts, generated assets, reports, or captures that show the missing contract.

## Fail-Closed Review Flow

1. Name the missing contract.
   - Identify the exact socket, serialized reference, asset catalog field, network field, profile status, UI token, or data invariant that is absent or unapproved.
   - Do not treat directional metadata, bounds, generated defaults, `pending`, `review`, or `unsure` as equivalent to an approved contract.

2. Classify the proposed fallback.
   - `domain default`: a real gameplay/domain rule that remains valid without missing data.
   - `compat adapter`: a narrow compatibility parser or adapter with direct tests and explicit owner.
   - `explicit unavailable state`: UI/report/test output that tells the user the contract is missing.
   - `silent repair`: code that makes the missing contract look valid. This is not acceptable as production behavior.

3. Make production behavior fail closed.
   - Missing required contract becomes `blocked`, `mismatch`, an explicit placeholder, or a rejected build/test path.
   - It must not become normal preview, normal gameplay, normal data promotion, or acceptance success.

4. Put the owner where the truth belongs.
   - Scene/prefab wiring belongs in serialized contracts or setup owners.
   - Asset/profile completeness belongs in catalog/profile data and validation.
   - Transport compatibility belongs in adapters/helpers.
   - UI unavailability belongs in explicit state rendering, not in visual approximation.

5. Add a regression check when code changes.
   - Prefer a direct test or validation that proves the known incomplete state fails closed.
   - If no correct seam exists, report the testability residual instead of adding a shallow test.
   - Search `fallback|Fallback|pending|review|unsure|auto_ok` in the touched surface and classify new hits.

6. Close out honestly.
   - Mechanical pass is not acceptance.
   - If approved evidence is still missing, report `blocked` or `mismatch`, not success.
   - For recurrence-tracked rule or skill changes, follow `ops.document-management-workflow` for `rules:lint` and closeout shard handling.

## Report Shape

When reporting, prefer this compact structure:

- `Missing contract`: the absent or unapproved source of truth
- `Current masking path`: fallback, lookup, default, bounds, repair, or promotion that hides the absence
- `Verdict`: domain default / compat adapter / explicit unavailable state / silent repair
- `Required move`: fail-closed behavior, owner route, and regression check
- `Validation`: search, test, lint, capture, or owner-doc evidence checked

## Boundaries

- This skill does not ban real domain defaults.
- This skill does ban making incomplete runtime, asset, scene, prefab, profile, or visual contract state look successful.
- This skill routes implementation through `jg-coding-guardrails`, owner-boundary review through `jg-coupling-review`, Unity work through `jg-unity-workflow`, and investigation through `jg-issue-investigation`.

## References

- `AGENTS.md`
- `docs/index.md`
- `docs/ops/codex_coding_guardrails.md`
- `docs/ops/cohesion_coupling_policy.md`
- `docs/ops/acceptance_reporting_guardrails.md`
- `docs/plans/technical_debt_recurrence_prevention_plan.md`
