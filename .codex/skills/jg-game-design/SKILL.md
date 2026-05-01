---
name: jg-game-design
description: "JG 게임/제품 디자인 라우터. Triggers: MVP, 핵심 재미, Nova1492 tone/rights, Garage/Battle UX, unit/module stats, balance, terminology."
---

# JG Game Design

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: skill.jg-game-design
> role: skill-entry
> owner_scope: JG game/product/design decision routing
> upstream: repo.agents, docs.index, design.game-design, design.world-design, design.unit-module-design, design.module-data-structure, design.ui-foundations
> artifacts: none

Use this skill as the entrypoint for JG product, game-design, world, unit/module, and Garage/Battle UX judgment.
Do not restate design truth here. Resolve current paths through `docs/index.md`, then read the active owner docs.

If the current collaboration mode is `Plan Mode`, use this skill for inspection and planning only. Do not mutate docs, code, scenes, prefabs, skills, generated artifacts, or evidence files.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read `docs/design/game_design.md` for product direction, MVP scope, core fun, fun hypotheses, and domain language.
4. Read conditional owner docs as needed:
   - `docs/design/world_design.md` for Nova1492 tone, story, naming, rights, and release-gate questions.
   - `docs/design/unit_module_design.md` for unit/module rules, stats, cost, roles, and balance.
   - `docs/design/module_data_structure.md` for Unity C# data structure and implementation-facing shape.
   - `docs/design/ui_foundations.md` for Lobby/Garage UI/UX, layout, tokens, and component vocabulary.
   - `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, or product/fun acceptance language.

## Decision Flow

1. Classify the request as product direction, domain language, world/naming, unit/module balance, implementation data shape, Garage/UI, validation, or release gate.
2. Separate product intent from implementation route.
3. Prefer current active design SSOT over old code, generated content, or historical implementation.
4. If domain terms are ambiguous, resolve user-facing meaning in `design.game-design`; route tone/copy to `design.world-design`, stats/rules to `design.unit-module-design`, data shape to `design.module-data-structure`, and UI labels/layout vocabulary to `design.ui-foundations`.
5. If the request conflicts with active design, report the mismatch and route any document change through `jg-doc-lifecycle`.
6. If implementation is requested after the design judgment, route execution through `jg-coding-guardrails`, `jg-unity-workflow`, `jg-stitch-workflow`, or `jg-stitch-unity-import` as appropriate.

## Boundaries

- This skill is a router. It does not own game design, world design, data structure, or UI foundation truth.
- Do not copy large design tables, formulas, layout contracts, or world rules into the skill body.
- Do not create a standalone glossary by default; prefer the current design owner document that owns the term's meaning.
- Do not make legal or rights approval claims for Nova1492 assets, names, or source material.
- Do not treat generated content presence, preview screenshots, compile pass, or code existence as product/design acceptance.
- Route cohesion/coupling judgment through `jg-coupling-review`.
- Route document lifecycle work through `jg-doc-lifecycle`.
- Route root-cause investigation through `jg-issue-investigation`.

## References

- `AGENTS.md`
- `docs/index.md`
- `docs/design/game_design.md`
- `docs/design/world_design.md`
- `docs/design/unit_module_design.md`
- `docs/design/module_data_structure.md`
- `docs/design/ui_foundations.md`
- `docs/ops/acceptance_reporting_guardrails.md`
