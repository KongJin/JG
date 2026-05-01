---
name: jg-coupling-review
description: "JG 응집도/결합도 리뷰 라우터. Triggers: owner boundary, responsibility split, dependency, interface/API shape, test seam, mock, split/merge 판단."
---

# JG Coupling Review

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: skill.jg-coupling-review
> role: skill-entry
> owner_scope: JG 응집도/결합도 review read order, owner boundary 판정, ripple/seam/hidden dependency 점검
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.document-management-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill to review whether a change is cohesive and whether coupling is explicit, bounded, and owned.
Do not restate cohesion/coupling policy as new truth here. Resolve current paths through `docs/index.md`, then follow `ops.cohesion-coupling-policy`.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, code, scenes, prefabs, tools, or generated evidence.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read owner doc `ops.cohesion-coupling-policy`.
4. If the review touches document lifecycle, read `ops.document-management-workflow` and use `jg-doc-lifecycle` for deletion, compression, registry, and stale trace work.
5. If the review needs success, blocked, mismatch, or residual language, read `ops.acceptance-reporting-guardrails`.
6. If the review touches Unity scene/prefab/runtime/UI work, read `jg-unity-workflow` and the relevant owner docs it routes to.
7. If the review touches Stitch source/handoff/import work, read `jg-stitch-workflow` or `jg-stitch-unity-import`.
8. Read the concrete files, docs, scripts, scene/prefab contracts, or tool entrypoints being reviewed.

## Coupling Review Flow

Use this flow for owner boundary, cohesion, and coupling review.

1. Lock the change reason.
   - State why the change exists in one sentence.
   - If the sentence contains multiple reasons, flag a split-owner risk.

2. Zoom out before judging unfamiliar areas.
   - Build a quick map of the user/domain concept, public entrypoints, direct callers, downstream consumers, adapters/providers, scene/prefab/tool contracts, and tests or smoke checks that cross the area.
   - Use the repo's active design/ops/plan owner vocabulary instead of inventing a new glossary.
   - If you cannot name the current callers and likely failure owner, report `blocked` or continue exploration before recommending a split or seam.

3. Find the real owner.
   - For docs, resolve through `docs/index.md`.
   - For code, identify feature, layer, class, setup/root, presentation, adapter, or contract owner.
   - For Unity, distinguish serialized scene/prefab contract from runtime code.
   - For tools, distinguish workflow policy from script execution mechanics.

4. Map the current reference direction.
   - Identify who reads, calls, owns, or repairs whom.
   - Look for cycles, plan-to-plan mutual dependency, controller overreach, and provider details leaking into consumers.

5. Trace the ripple.
   - List the docs, files, scenes, prefabs, scripts, or artifacts that must change together.
   - For each item, ask whether it changes for the same reason.
   - If an item changes only because another owner leaks details, flag coupling.

6. Check the seam.
   - Code seams: interface, port, adapter, event, DTO, contract.
   - Unity seams: serialized reference, scene/prefab contract, explicit bootstrap registration, scene-local registrar.
   - Document seams: registry, owner doc, stable `doc_id`, reference link.
   - If no seam exists, treat the relationship as direct coupling.

7. Check interface design, module depth, and test surface.
   - Treat an interface as everything a caller or test must know: type shape, ordering, invariants, error modes, config, and performance expectations.
   - Before adding or moving a seam, name the caller-facing behavior it hides and the invariants, error modes, and ownership boundary it makes explicit.
   - A deep module has a small, stable public surface that hides meaningful rules or integration complexity. A shallow module mostly forwards provider vocabulary, config knobs, or call order to its callers.
   - If a module looks shallow, run the deletion test: deleting it should either remove unnecessary pass-through complexity or reveal that its rules would spread across callers.
   - The best test surface is the same interface callers use. If tests must reach past the interface to verify behavior, flag a seam or owner-shape problem.
   - Prefer mocks at hard external boundaries: network, filesystem, time, randomness, Unity runtime, third-party SDKs, or external services.
   - If tests mainly mock internal collaborators or verify call order between same-owner classes, review whether the module is too shallow, the interface is leaking implementation details, or the behavior belongs behind a different seam.
   - One concrete adapter is usually a hypothetical seam; prefer adding seams when direct coupling is real and variation, tests, or ownership justify the extra surface.

8. Detect hidden coupling.
   - Look for runtime lookup, fallback, `Resources.Load`, `transform.Find`, `AddComponent`, hidden repair, or script-side scene fixes.
   - For docs, look for old paths, duplicate decisions, stale `doc_id`, or the same decision explained in multiple bodies.
   - Do not accept "it works" as proof that hidden coupling is safe.

9. Separate success criteria.
   - Keep compile pass, direct tests, smoke, runtime acceptance, WebGL real-device evidence, preview/candidate evidence, and product acceptance separate.
   - If success criteria differ, do not report them as one success.

10. Check change frequency and failure owner.
   - Ask whether the pieces change together often.
   - Ask who fixes the failure when it breaks.
   - Different rhythm or different failure owner is a split or seam signal.

11. Make the verdict.
   - `keep together`: same reason-to-change and same owner/failure responsibility.
   - `split owner`: reason, success criteria, rhythm, or failure owner differs.
   - `add seam`: direct coupling is currently real, but a contract/adapter/registry can contain it.
   - `blocked`: the review lacks enough local evidence to judge.

12. Validate and close out.
    - Docs: stale reference search and `npm run --silent rules:lint`.
    - Code: static/compile/test path appropriate to the touched owner.
    - Unity: serialized contract inspection, MCP preflight, or smoke evidence as relevant.
    - Report `success`, `blocked`, `mismatch`, or `residual` separately.

## Review Output

When reporting, prefer this compact structure:

- `Verdict`: keep together / split owner / add seam / blocked
- `Reason-to-change`: one sentence
- `Owner`: primary owner plus secondary/out-of-scope if useful
- `Map`: caller/owner/interface/test-surface/mocking context when the area was unfamiliar or architecture-oriented
- `Coupling observed`: explicit references, ripple, hidden lookup, fallback, duplicate docs, or controller/setup overreach
- `Recommended move`: what to keep, split, or route through a seam
- `Validation`: what was checked and what remains unverified

## Boundaries

- This skill reviews coupling and cohesion. It does not own document deletion/compression mechanics; use `jg-doc-lifecycle` for document lifecycle execution.
- This skill may recommend code, scene, prefab, or docs changes, but it should route implementation through the relevant workflow skill.
- Do not create a generalized coupling score. The repo policy treats meaning-based owner judgment as a review gate, not a broad hard-fail metric.

## References

- `AGENTS.md`
- `docs/index.md`
- `ops.cohesion-coupling-policy`
- `ops.document-management-workflow`
- `ops.acceptance-reporting-guardrails`
- `.codex/skills/jg-doc-lifecycle/SKILL.md`
- `.codex/skills/jg-unity-workflow/SKILL.md`
- `.codex/skills/jg-stitch-workflow/SKILL.md`
