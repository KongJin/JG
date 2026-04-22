---
name: jg-stitch-workflow
description: Project-specific Stitch workflow for the JG repo. Use whenever Codex works with `.stitch/DESIGN.md`, `.stitch/prompt-briefs`, `.stitch/contracts`, Stitch screen generation/editing, or translating Stitch outputs into Unity scene-owned layouts in this repository. In JG, this skill is a thin router: it points to the active owner docs for prompt-brief refinement and structured handoff preparation, then hands off Unity implementation to `jg-unity-workflow`.
---

# JG Stitch Workflow

> 마지막 업데이트: 2026-04-23
> 상태: active
> doc_id: skill.jg-stitch-workflow
> role: skill-entry
> owner_scope: JG Stitch lane read order, owner doc routing, artifact entrypoint
> upstream: repo.agents, docs.index, ops.cohesion-coupling-policy, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/contracts/`

Use this skill only as a router for the JG Stitch lane.
Do not restate workflow policy or artifact rules here.
Resolve current paths through `docs/index.md`, then follow the owner docs.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate `.stitch` artifacts or handoff outputs from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve the current owner paths.
3. Read owner doc `ops.cohesion-coupling-policy` when the task needs owner boundaries, cohesion/coupling judgment, or responsibility splitting.
4. Read owner doc `ops.stitch-data-workflow`.
5. Read owner doc `ops.stitch-structured-handoff-contract` when the task writes, reviews, or translates a contract.
6. Read reference doc `ops.stitch-handoff-completeness-checklist` when the task validates contract completeness.
7. Read owner doc `design.ui-reference-workflow` when the task needs Stitch visual principles or reference usage rules.
8. Read owner doc `plans.stitch-ui-ux-overhaul` when the task touches set-level planning or `.stitch` inventory.
9. Read the relevant `.stitch/contracts/screens/*.json` file and, when present, its `.stitch/contracts/blueprints/*.json` base before regenerating or translating a surface.

## Active Artifacts

- `.stitch/DESIGN.md`
- `.stitch/prompt-briefs/*.md`
- `.stitch/contracts/blueprints/*.json`
- `.stitch/contracts/screens/*.json`
- legacy/full fallback `.stitch/contracts/*.json`

## Task Routing

- `brief update`: refine an existing prompt brief
- `screen generation/edit`: update Stitch output as needed, then reflect the decision in blueprint + screen manifest JSON
- `contract translation`: create or update `.stitch/contracts/screens/*.json` and, when reuse matters, `.stitch/contracts/blueprints/*.json`
- `unity handoff`: stop after the contract is precise enough, then switch to `jg-unity-workflow`
- In `Plan Mode`, stop at routing, inspection, and contract review. Do not rewrite `.stitch/contracts/*.json`, prompt briefs, or handoff artifacts.

## Practical Loop

Use this loop when the user wants to translate one accepted screen carefully:

1. Lock the accepted baseline screen first. Do not mix a candidate variant into the same pass.
2. Keep the accepted Stitch `png` visible while reading the intake and manifest.
3. Write or review the `screen intake` from screen-derived facts only.
4. Update the `screen manifest` only after the intake is stable.
5. Before handing off to Unity, compare `accepted screen -> intake -> manifest` and remove any guessed CTA, state, or block meaning that is not grounded in the source.

## Fidelity Hints

When reviewing or updating Stitch-side artifacts, preserve at least these source decisions:

- first read and second read order
- block direction such as `strip`, `row`, `stack`, `single scroll body`
- emphasis posture: what is primary, secondary, and auxiliary
- whether preview / summary / empty state feels finished or placeholder-like
- primary CTA posture: width, persistence, and visual weight

If the screen clearly communicates one of these and the intake/manifest does not, fix the Stitch-side artifact before handing off to Unity.

## Closeout

1. The relevant prompt brief matches the current intent.
2. The active structured contract under `.stitch/contracts/screens/*.json` and its referenced blueprint are updated when needed.
3. The contract passes `ops.stitch-handoff-completeness-checklist`.
4. If Unity implementation happened, the Unity evidence path is newer than the contract it implements.

## References

- `docs/index.md`
- `ops.cohesion-coupling-policy`
- `ops.stitch-data-workflow`
- `ops.stitch-structured-handoff-contract`
- `ops.stitch-handoff-completeness-checklist`
- `design.ui-reference-workflow`
- `plans.stitch-ui-ux-overhaul`
- `.stitch/DESIGN.md`
