---
name: jg-game-design
description: "JG 게임/제품 디자인 라우터. Triggers: MVP, 핵심 재미, Nova1492 tone/rights, Garage/Battle UX, unit/module stats, balance, terminology."
---

# JG Game Design

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: skill.jg-game-design
> role: skill-entry
> owner_scope: JG game/product/design decision routing
> upstream: repo.agents, docs.index, design.game-design, design.world-design, design.unit-module-design, design.module-data-structure, design.ui-foundations
> artifacts: none

Use this skill for JG product, game-design, world, unit/module, balance, terminology, and Garage/Battle UX judgment.
This skill is a router. It does not own design truth.
If the current collaboration mode is `Plan Mode`, use this skill for inspection/reference only. Do not mutate docs, code, scenes, prefabs, skills, generated artifacts, or evidence files from this lane.

## Read First

1. Read `AGENTS.md`.
2. Read `docs/index.md` to resolve current owner paths.
3. Read `docs/owners/design/game_design.md` for product direction, MVP scope, core fun, and domain language.
4. Read `docs/owners/design/world_design.md` for Nova1492 tone, story, naming, rights, and release-gate questions.
5. Read `docs/owners/design/unit_module_design.md` for unit/module rules, stats, cost, roles, and balance.
6. Read `docs/owners/design/module_data_structure.md` for implementation-facing data shape.
7. Read `docs/owners/design/ui_foundations.md` for Lobby/Garage UI/UX, layout, tokens, and component vocabulary.
8. Read `docs/owners/operations/acceptance_reporting_guardrails.md` before using product/design acceptance language.

## Route

1. Separate product/design judgment from implementation route.
2. Prefer active design owner docs over old code, generated content, previews, or historical implementation.
3. Route document changes through `jg-doc-lifecycle`.
4. Route implementation through `jg-coding-guardrails`, `jg-unity-workflow`, `jg-stitch-workflow`, or `jg-stitch-unity-import` after design judgment is clear.

## Boundary

Do not copy design tables, formulas, layout contracts, world rules, or glossary body into this skill.
