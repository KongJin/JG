---
name: jg-game-design
description: >-
  Project-specific game and product design router for the JG repo. Use this skill whenever Codex is asked about JG game direction, product judgment, MVP scope, core fun, fun hypotheses, Nova1492 world/tone/story, rights or naming release gates, Garage UX, battle UX, unit/module design, stats, cost, balance, Unit/Garage data structure, or Korean requests like "게임 방향", "제품 판단", "MVP 범위", "핵심 재미", "재미 가설", "Nova1492 세계관", "권리", "이름 release gate", "Garage UX", "전투 UX", "유닛/모듈 설계", "스탯", "비용", "밸런스", or "Unit/Garage data structure". This skill routes design judgment through the active design SSOT docs before any implementation route.
---

# JG Game Design

> 마지막 업데이트: 2026-04-30
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
3. Read `docs/design/game_design.md` for product direction, MVP scope, core fun, and fun hypotheses.
4. Read conditional owner docs as needed:
   - `docs/design/world_design.md` for Nova1492 tone, story, naming, rights, and release-gate questions.
   - `docs/design/unit_module_design.md` for unit/module rules, stats, cost, roles, and balance.
   - `docs/design/module_data_structure.md` for Unity C# data structure and implementation-facing shape.
   - `docs/design/ui_foundations.md` for Lobby/Garage UI/UX, layout, tokens, and component vocabulary.
   - `docs/ops/acceptance_reporting_guardrails.md` before using `success`, `blocked`, `mismatch`, or product/fun acceptance language.

## Decision Flow

1. Classify the request as product direction, world/naming, unit/module balance, implementation data shape, Garage/UI, validation, or release gate.
2. Separate product intent from implementation route.
3. Prefer current active design SSOT over old code, generated content, or historical implementation.
4. If the request conflicts with active design, report the mismatch and route any document change through `jg-doc-lifecycle`.
5. If implementation is requested after the design judgment, route execution through `jg-coding-guardrails`, `jg-unity-workflow`, `jg-stitch-workflow`, or `jg-stitch-unity-import` as appropriate.

## Boundaries

- This skill is a router. It does not own game design, world design, data structure, or UI foundation truth.
- Do not copy large design tables, formulas, layout contracts, or world rules into the skill body.
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

