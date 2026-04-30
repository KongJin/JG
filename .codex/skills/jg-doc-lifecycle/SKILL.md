---
name: jg-doc-lifecycle
description: >-
  Project-specific document cohesion and lifecycle workflow for the JG repo. Use this skill whenever Codex is asked about 문서 응집도, "응집도 skill" in a documentation context, whether content belongs in an existing document owner, or "이 내용이 어느 문서 owner에 있어야 하나"; and whenever Codex is asked to clean up, delete, compress, merge, split, rename, route, or review repo documentation, update `docs/plans/*`, slim `progress.md`, or remove stale paths/doc_id/owner references. This skill is the primary entrypoint for document lifecycle and document cohesion work; for broad code/scene/prefab/tool cohesion or coupling review, route through `jg-coupling-review`. It is a thin router that reads the active owner docs and applies the repo's SSOT, owner, cohesion, stale-trace, lint, and closeout flow without restating policy as a new source of truth.
---

# JG Document Lifecycle

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: skill.jg-doc-lifecycle
> role: skill-entry
> owner_scope: JG 문서 lifecycle read order, owner routing, stale trace cleanup, closeout entrypoint
> upstream: repo.agents, docs.index, ops.document-management-workflow, ops.cohesion-coupling-policy, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

Use this skill as the entrypoint for JG repo document lifecycle work.
Do not restate document policy as new truth here. Resolve current owner paths through `docs/index.md`, then follow the owner docs.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, skills, generated artifacts, or closeout files.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths and active document registry.
3. Read owner doc `ops.document-management-workflow` for SSOT, role, lifecycle, stale trace, and closeout rules.
4. Read owner doc `ops.cohesion-coupling-policy` before judging whether content belongs together or should be split.
5. Read owner doc `ops.plan-authoring-review-workflow` when the task creates, deletes, compresses, or substantially changes `docs/plans/*`.
6. Read owner doc `ops.acceptance-reporting-guardrails` before using `success`, `blocked`, `mismatch`, residual, or closeout language.
7. Read `plans.progress` when current state, priority, active owner, or next work may change.
8. Read the specific owner document that owns the topic before changing it.

## Lifecycle Flow

Use this flow for document cleanup, deletion, compression, merge, rename, or routing work.

1. Find the owner.
   - Follow `AGENTS.md -> docs/index.md -> relevant owner doc`.
   - Current state belongs in `plans.progress`.
   - Operational rules belong in `ops/*`.
   - Design decisions belong in `design/*`.
   - Execution closeout belongs in `plans/*`.

2. State the change reason in one sentence.
   - Example: `Merge Phase 5 multiplayer residual into GameScene actual-flow closeout.`
   - If that sentence naturally splits into multiple reasons, split the work or keep separate owners.

3. Choose the change boundary.
   - `primary owner`: the document whose reason-to-change is the real owner.
   - `secondary owner`: registry/progress/reference docs that must follow the primary change.
   - `out-of-scope`: docs, code, assets, or tools that should not move in this pass.

4. Prefer an existing owner over a new document.
   - Check whether `plans.progress` one line is enough.
   - Check whether a short section in an existing owner doc is enough.
   - Check whether a reference update is enough.
   - Create a new `docs/plans/*` only when multi-session handoff, persistent acceptance, or residual handling must be found later.

5. Preserve document roles.
   - `docs/index.md` routes and registers; it does not own policy body.
   - `plans.progress` owns current verdict and next blocker, not dated evidence logs.
   - `docs/plans/*` owns execution order, acceptance, residual, and closeout.
   - `docs/ops/*` and `docs/design/*` own rules and judgment criteria.

6. Search stale traces after delete, rename, merge, or owner movement.
   - Search old path, filename, `doc_id`, and owner id with `rg`.
   - Check active docs, repo-local skills, tool README files, and managed prompt/reference docs.
   - Remove or reroute stale active references before closeout.

7. Validate mechanically.
   - Run `npm run --silent rules:lint` after managed document or repo-local skill changes.
   - If rules-only or document lifecycle changes require closeout artifact sync, run `npm run --silent rules:sync-closeout` or leave an explicit blocked/residual reason if the artifact cannot be safely updated.
   - Do not report success when lint, policy, stale search, or closeout artifact sync is blocked.

8. Finish with lifecycle judgment.
   - Decide whether each affected plan/doc stays `active`, becomes `reference`, becomes `historical`, or is deleted.
   - For large document work, leave `owner impact` and `doc lifecycle checked`.
   - If a skill trigger changed, verify the actual skill file change and leave `skill trigger checked`.

## Closeout Checklist

Before final response or closeout, confirm:

- The reason-to-change is still one coherent reason per patch.
- The primary owner is clear and secondary owners are only following changes.
- Entry docs route only; owner docs keep policy and judgment.
- `plans.progress` stayed short and current.
- Deleted or merged paths have no active stale references.
- `npm run --silent rules:lint` was run, or the blocked reason is explicit.
- Closeout wording separates mechanical evidence from actual acceptance.
- Large document work includes `owner impact` and `doc lifecycle checked`.

## Boundaries

- This skill covers document lifecycle and owner routing.
- It can use `ops.cohesion-coupling-policy` to judge document cohesion, but it is not a broad code/scene/prefab coupling review skill.
- For Unity scene, prefab, runtime, MCP, or UI authoring work, route through `jg-unity-workflow` or the relevant Unity skill after the document lifecycle decision is made.
- For Stitch source and handoff artifacts, route through `jg-stitch-workflow` or `jg-stitch-unity-import` after the document lifecycle decision is made.

## References

- `AGENTS.md`
- `docs/index.md`
- `ops.document-management-workflow`
- `ops.cohesion-coupling-policy`
- `ops.plan-authoring-review-workflow`
- `ops.acceptance-reporting-guardrails`
- `plans.progress`
