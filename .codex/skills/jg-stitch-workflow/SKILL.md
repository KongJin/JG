---
name: jg-stitch-workflow
description: Project-specific Stitch workflow for the JG repo. Use whenever Codex works with `.stitch/DESIGN.md`, `.stitch/prompt-briefs`, `.stitch/designs`, `.stitch/handoff`, Stitch screen generation/editing, or translating Stitch outputs into Unity scene-owned layouts in this repository. In JG, this is the single entry point for Stitch work and it directly owns prompt-brief refinement, `.stitch/DESIGN.md` upkeep guidance, design export organization, and handoff preparation.
---

# JG Stitch Workflow

> 마지막 업데이트: 2026-04-20
> 상태: active
> doc_id: skill.jg-stitch-workflow
> role: skill-entry
> owner_scope: JG Stitch lane read order, owner doc routing, artifact entrypoint
> upstream: repo.agents, docs.index, ops.stitch-data-workflow
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/designs/`, `.stitch/handoff/`

Use this skill for JG-specific Stitch execution order and data ownership.
Keep this lane limited to prompt brief refinement, design export curation, and accepted handoff preparation.
Keep Unity runtime implementation rules in `jg-unity-workflow`.
If a document name moved, resolve the current path through `docs/index.md` first and then follow the owner doc by `doc_id`.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve the current owner paths.
3. Read owner doc `ops.stitch-data-workflow`.
4. Read reference doc `ops.stitch-handoff-completeness-checklist` when the task writes, reviews, or translates a handoff.
5. Read owner doc `design.ui-reference-workflow` when the task needs Stitch visual principles or reference usage rules.
6. Read owner doc `plans.stitch-ui-ux-overhaul` when the task touches set-level planning or `.stitch` inventory.
7. Read the relevant handoff file under `.stitch/handoff/` before regenerating or translating a screen.

## JG Defaults

- Treat `.stitch/DESIGN.md` as the visual working SSOT inside the Stitch lane.
- Treat `.stitch/prompt-briefs/*.md` as the canonical screen input briefs.
- Treat `.stitch/designs/*.{html,png}` as raw exports and evidence, not runtime truth.
- Treat `.stitch/handoff/*.md` as the required bridge into Unity implementation.
- Do not create parallel prompt-brief or handoff files for the same surface unless the set structure itself changed.

## Task Routing

- `brief update`: refine an existing prompt brief before new generation
- `screen generation/edit`: use Stitch to refresh or add a screen, then store exports under `.stitch/designs/`
- `handoff translation`: update `.stitch/handoff/*.md` from accepted Stitch output
- `unity handoff`: stop once the handoff is precise enough, then switch to `jg-unity-workflow` for scene, prefab, MCP, and runtime validation work

## JG Workflow Rules

- In JG, this skill routes prompt-brief refinement, `.stitch/DESIGN.md` upkeep guidance, design export organization, and handoff preparation through the owner docs and active `.stitch` artifacts.
- Prefer updating existing `.stitch` artifacts in place over producing `v2` or duplicate files.
- Keep set naming stable: `set-a-*`, `set-b-*`, and so on.
- In handoff docs, role labels such as `baseline`, `supporting state`, and `supporting overlay` must be clearer than local export filenames.
- If a visual decision needs to live longer than one session, promote it into repo docs, not chat history.
- Final runtime layout authority always returns to Unity scenes and prefabs, and that implementation lane starts in `jg-unity-workflow`.

## Validation Before Closeout

1. The relevant prompt brief matches the current intent.
2. The chosen html/png export exists under `.stitch/designs/`.
3. The handoff reflects the accepted export, not an older draft.
4. The handoff passes `ops.stitch-handoff-completeness-checklist` for baseline labeling, CTA hierarchy, Unity targets, and validation focus.
5. If Unity implementation happened, the Unity evidence path is newer than the Stitch artifact it implements.

## References

- `docs/index.md`
- `ops.stitch-data-workflow`
- `ops.stitch-handoff-completeness-checklist`
- `design.ui-reference-workflow`
- `plans.stitch-ui-ux-overhaul`
- `.stitch/DESIGN.md`
- `.stitch/handoff/INDEX.md`
