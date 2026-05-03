# Skill Trigger Inventory

> generated: 2026-04-25
> updated: 2026-04-27
> scope: first pass for `docs/ops`, `docs/plans`, `docs/design`, `AGENTS.md`, `.codex/skills/rule-*`

This inventory separates rules that need an always-on skill trigger from rules that should remain only in owner documents.

## Summary

| status | count | meaning |
|---|---:|---|
| covered | 11 | Already has a direct skill trigger or a newly added trigger |
| description-needs-review | 0 | Existing skill likely covers it, but the description may be too broad or indirect |
| new-skill-candidate | 2 | Repeated action lane may need its own skill if under-triggering continues |
| docs-only | 5 | Keep in owner docs; not a skill trigger |

## Inventory

| candidate | owner document | trigger examples | current coverage | recommended action |
|---|---|---|---|---|
| Plan authoring rereview loop | `docs/ops/plan_authoring_review_workflow.md` | "계획해줘", `docs/plans/*.md`, Phase, Acceptance, TODO, 과한점/부족한점, closeout | covered by `rule-plan-authoring` | Keep new skill. Verify next-session trigger metadata. |
| Document ownership / SSOT routing | `docs/ops/document_management_workflow.md`, `docs/index.md` | SSOT, 문서 소유권, 문서 어디, owner, entry, reference | covered by `rule-operations` | Keep in `rule-operations`. |
| Codex patch operation | `rule-operations/codex_patching.md` | 패치, 한 패치 한 책임, 적용 순서, 문서 동기화 | covered by `rule-operations` | Keep in `rule-operations`. |
| Validation / clean levels | `docs/playtest/*`, validation owner docs, rule skill refs | 검증, compile-clean, static-clean, runtime smoke, WebGL smoke | covered by `rule-validation` | Keep in `rule-validation`; consider adding `WebGL smoke checklist` phrase if missed. |
| Unity scene/prefab/MCP workflow | `docs/ops/unity_ui_authoring_workflow.md`, `tools/unity-mcp/README.md` | Unity, scene, prefab, MCP, Play Mode, compile error, UI authoring | covered by `rule-unity` and `jg-unity-workflow` | Keep both; use `jg-unity-workflow` for JG-specific ordering. |
| Architecture / layer dependency | design architecture docs, rule architecture refs | architecture, layer, dependency, port, UseCase, Bootstrap | covered by `rule-architecture` | Keep in `rule-architecture`. |
| Reporting closeout / blocked / mismatch / success | `docs/ops/acceptance_reporting_guardrails.md`, `docs/ops/document_management_workflow.md` | blocked, mismatch, success, acceptance, closeout, residual | covered by `rule-operations` | Description strengthened. Consider `rule-acceptance-reporting` only if misses continue. |
| Stitch data and translation workflow | `docs/ops/stitch_data_workflow.md`, `docs/ops/stitch_structured_handoff_contract.md`, `tools/stitch-unity/README.md` | Stitch, handoff, source freeze, execution contract, translation, presentation contract | covered by `jg-stitch-workflow`; Unity handoff continues through `jg-unity-workflow` | Keep `jg-stitch-workflow` as the thin router. Use `jg-stitch-unity-import` for the repeated source-to-Unity candidate import loop. |
| Stitch to Unity UI import execution | `docs/ops/stitch_data_workflow.md`, `docs/ops/unity_ui_authoring_workflow.md`, `docs/ops/acceptance_reporting_guardrails.md` | 가져와줘, import, port, UI Toolkit pilot, runtime replacement, UXML, USS, UIDocument, PanelSettings, preview scene, capture, visual fidelity | covered by `jg-stitch-unity-import` | Keep as a focused execution skill for source-to-candidate import. |
| Unity UI authoring policy | `docs/ops/unity_ui_authoring_workflow.md` | Unity UI, UI authoring, UI Toolkit candidate, new prefab, workflow policy | covered by `jg-unity-workflow` | Current UI work starts from UI Toolkit candidate surfaces. |
| Stitch/UI Toolkit UX route | `docs/ops/unity_ui_authoring_workflow.md`, `.codex/skills/jg-stitch-unity-import/SKILL.md`, `.codex/skills/jg-unity-workflow/SKILL.md` | UI Toolkit candidate, Stitch-driven UX, source freeze, capture evidence | covered by `jg-stitch-unity-import` and `jg-unity-workflow` | New UX/UI work starts from Stitch source freeze and UI Toolkit candidate surface. |
| Skill trigger coverage for new rules | `docs/ops/skill_trigger_matrix.md`, `docs/ops/skill_routing_registry.md` | new rule, 규칙 추가, skill trigger checked, behavior trigger | covered by `rule-plan-authoring` and `rule-operations` | Keep route fixtures in the owner matrix/registry and use closeout wording for actual trigger checks. |
| WebGL / Firebase / deployment context | `docs/playtest/webgl_smoke_checklist.md`, `docs/ops/firebase_hosting.md` | WebGL, Firebase, hosting, 배포, 실기 검증 | covered by `rule-context` and `rule-validation` | Keep split: deployment in `rule-context`, verification in `rule-validation`. |
| Error handling / forbidden patterns | pattern owner docs | 금지 패턴, 예외 처리, ErrorCode, logging, event chain | covered by `rule-patterns` | Keep in `rule-patterns`. |
| MVP fun validation / playtest | `docs/design/game_design.md`, `docs/playtest/runtime_validation_checklist.md` | MVP 재미 검증, playtest, 재미 체크리스트 | new-skill-candidate | Consider `rule-playtest` only if playtest work repeatedly misses measurement/SSOT routing. |
| Nova1492 resource integration | `docs/plans/nova1492_content_residual_plan.md`, `docs/design/world_design.md` | Nova1492 resource, 원본 리소스, import, staging asset | new-skill-candidate | Keep docs-only for now; create skill only if this becomes a recurring lane. |
| Game design product direction | `docs/design/game_design.md` | 게임 방향, MVP 범위, 코어 재미, Nova x Clash Royale | docs-only | Product SSOT should remain a design doc; use existing context/design discussion instead of a trigger skill unless action workflow emerges. |
| Unit/module design rules | `docs/design/unit_module_design.md`, `docs/design/module_data_structure.md` | 유닛 모듈, 금지조합, 조합 검증, 데이터 구조 | docs-only | Keep in design docs unless implementation repeatedly misses it. |
| UI foundations design rules | `docs/design/ui_foundations.md` | Garage layout, mobile resolution, design tokens, UI roles | docs-only | Keep as design reference; Unity UI tasks already route through `jg-unity-workflow`. |
| Historical discussions | `docs/discussions/*` | rationale, historical decisions | docs-only | No skill trigger needed. |

## Immediate Recommendations

1. Keep `rule-plan-authoring` and `jg-stitch-unity-import` as focused skills for recurring action loops.
2. Do not create more skills until a repeated miss is observed or the inventory confirms a distinct action loop.
3. In Phase 3, promote `skill trigger checked: ...` from interim plan rule into the owner closeout workflow.
4. In Phase 4, start with advisory lint rather than hard fail.
5. Next-session check should confirm the updated skill descriptions appear in available skill metadata.

## Skill Trigger Checked

- `skill trigger checked: added to rule-operations, rule-plan-authoring, jg-unity-workflow`
- `skill trigger checked: corrected Stitch coverage to jg-stitch-workflow on 2026-04-26`
- `skill trigger checked: added jg-stitch-unity-import on 2026-04-27`
