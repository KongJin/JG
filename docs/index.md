# Docs Index

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: `docs/` owner tree, plan lifecycle tree, document placement entrypoint
> upstream: repo.agents
> artifacts: none

이 문서는 `docs/`의 루트 registry다. 루트 진입점은 [`../AGENTS.md`](../AGENTS.md), 현재 상태 SSOT는 [`plans/current/progress.md`](./plans/current/progress.md)다.
사람과 agent는 `doc_id`를 stable owner identifier로 보고, 실제 파일 위치는 아래 tree registry를 따른다.
`doc_id` prefix는 역사적 owner id를 보존하므로 일부 subtree와 이름이 완전히 같지 않다. 예를 들어 `owners/ui-workflow/*`는 `ops.*`, `owners/validation/*`는 `playtest.*`를 유지한다.

이 인덱스는 경로와 작성 위치만 안내한다. 규칙 본문은 각 owner 문서에 둔다.

## Quick Start

- 현재 상태 확인: [`plans/current/progress.md`](./plans/current/progress.md)
- Plan Mode / Codex 운영: `rule-operations` route -> [`owners/operations/document_management_workflow.md`](./owners/operations/document_management_workflow.md)와 [`owners/operations/codex_coding_guardrails.md`](./owners/operations/codex_coding_guardrails.md)
- Unity UI / prefab / scene 작업: progress -> [`owners/ui-workflow/unity-ui-authoring-workflow.md`](./owners/ui-workflow/unity-ui-authoring-workflow.md) -> [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md)
- Stitch / handoff 작업: [`owners/design/ui_reference_workflow.md`](./owners/design/ui_reference_workflow.md) -> [`owners/ui-workflow/stitch_data_workflow.md`](./owners/ui-workflow/stitch_data_workflow.md) -> [`owners/ui-workflow/stitch_structured_handoff_contract.md`](./owners/ui-workflow/stitch_structured_handoff_contract.md)
- 코딩 구현 / 버그 수정 / 리팩터 / 테스트 보강: [`owners/operations/codex_coding_guardrails.md`](./owners/operations/codex_coding_guardrails.md) -> relevant owner doc -> concrete code/tests
- GameScene / WebGL 검증: progress -> [`owners/validation/runtime_validation_checklist.md`](./owners/validation/runtime_validation_checklist.md) or [`owners/validation/webgl_smoke_checklist.md`](./owners/validation/webgl_smoke_checklist.md)
- 문서 / workflow 정리: [`owners/operations/document_management_workflow.md`](./owners/operations/document_management_workflow.md) -> [`owners/operations/cohesion-coupling-policy.md`](./owners/operations/cohesion-coupling-policy.md)
- Skill route / trigger 정리: [`owners/operations/skill_routing_registry.md`](./owners/operations/skill_routing_registry.md) -> [`owners/operations/skill_trigger_matrix.md`](./owners/operations/skill_trigger_matrix.md)

## 상태 규칙

- `active`: 현재 작업 기준으로 직접 참고하는 문서
- `draft`: 방향은 유효하지만 세부안은 아직 확정 전인 문서
- `paused`: 이유가 있어 실행을 멈춘 계획 문서
- `historical`: 당시 판단 기록은 남기되 현재 구현 기준으로 쓰지 않는 문서
- `reference`: 절차나 운영 방법처럼 필요할 때 다시 보는 문서

## Owner Tree

```text
docs/
  index.md
  owners/
    operations/      # 문서 운영, Codex 절차, acceptance, skill route, Firebase reference
    ui-workflow/     # Unity UI authoring, Stitch data, structured handoff contract
    design/          # 게임/세계관/UI/유닛/데이터 설계 판단
    architecture/    # 구조 분석과 architecture reference
    validation/      # runtime/WebGL 수동 검증 owner
  plans/
    current/         # 현재 상태 SSOT
    active/          # multi-session 실행 계획
    reference/       # 재사용 가능한 완료/참고 계획
    historical/      # 현재 판단 근거가 아닌 보존 기록
```

## New Document Placement

- 정책/규칙/운영 방법: `docs/owners/operations/`
- UI 제작 workflow와 Stitch/Unity handoff: `docs/owners/ui-workflow/`
- 제품/세계관/UI/유닛 설계 판단: `docs/owners/design/`
- architecture 분석/구조 reference: `docs/owners/architecture/`
- runtime/WebGL/manual smoke 검증 기준: `docs/owners/validation/`
- 현재 상태 한 줄 또는 현재 focus: `docs/plans/current/progress.md`
- 여러 세션에 걸친 실행 계획: `docs/plans/active/`
- 닫혔지만 재사용할 closeout/reference: `docs/plans/reference/`
- 현재 판단 근거가 아닌 과거 기록: `docs/plans/historical/`

새 문서가 어느 subtree에도 명확히 속하지 않으면 먼저 기존 owner 문서에 짧게 흡수할 수 있는지 확인한다.

## Doc ID Registry

| doc_id | 파일명 | 상태 |
|---|---|---|
| docs.index | index.md | active |
| plans.progress | plans/current/progress.md | active |
| ops.cohesion-coupling-policy | owners/operations/cohesion-coupling-policy.md | active |
| ops.unity-ui-authoring-workflow | owners/ui-workflow/unity-ui-authoring-workflow.md | active |
| ops.codex-coding-guardrails | owners/operations/codex_coding_guardrails.md | active |
| ops.presentation-layer-guardrails | owners/operations/presentation_layer_guardrails.md | active |
| ops.stitch-data-workflow | owners/ui-workflow/stitch_data_workflow.md | active |
| ops.stitch-structured-handoff-contract | owners/ui-workflow/stitch_structured_handoff_contract.md | active |
| ops.document-management-workflow | owners/operations/document_management_workflow.md | active |
| ops.plan-authoring-review-workflow | owners/operations/plan_authoring_review_workflow.md | active |
| ops.skill-routing-registry | owners/operations/skill_routing_registry.md | active |
| ops.skill-trigger-matrix | owners/operations/skill_trigger_matrix.md | active |
| ops.acceptance-reporting-guardrails | owners/operations/acceptance_reporting_guardrails.md | active |
| ops.firebase-hosting | owners/operations/firebase_hosting.md | reference |
| architecture.anti-pattern-analysis | owners/architecture/anti_pattern_analysis.md | reference |
| design.game-design | owners/design/game_design.md | active |
| design.world-design | owners/design/world_design.md | active |
| design.ui-foundations | owners/design/ui_foundations.md | active |
| design.ui-reference-workflow | owners/design/ui_reference_workflow.md | active |
| design.unit-module-design | owners/design/unit_module_design.md | active |
| design.module-data-structure | owners/design/module_data_structure.md | active |
| playtest.runtime-validation-checklist | owners/validation/runtime_validation_checklist.md | active |
| playtest.webgl-smoke-checklist | owners/validation/webgl_smoke_checklist.md | active |
| plans.game-scene-flow-validation-closeout | plans/active/game_scene_flow_validation_closeout_plan.md | active |
| plans.webgl-audio-closeout | plans/active/webgl-audio-closeout.md | active |
| plans.audio-sfx-mcp-pipeline | plans/active/audio_sfx_mcp_pipeline_plan.md | active |
| plans.nova1492-content-residual | plans/active/nova1492-content-residual-plan.md | active |
| plans.non-stitch-ui-stitch-reimport | plans/reference/non_stitch_ui_stitch_reimport_plan.md | reference |
| plans.uitk-page-routing-refactor | plans/reference/uitk-page-routing-refactor.md | reference |
| plans.agent-workflow-skill-adoption | plans/reference/agent_workflow_skill_adoption_plan.md | reference |
| plans.technical-debt-recurrence-prevention | plans/reference/technical_debt_recurrence_prevention_plan.md | reference |
| plans.nova1492-assembly-profile-recovery | plans/historical/nova1492_assembly_profile_recovery_plan.md | historical |

## Status Label Entries

- `active`: [`progress.md`](./plans/current/progress.md) - current state SSOT
- `active`: [`cohesion-coupling-policy.md`](./owners/operations/cohesion-coupling-policy.md) - cohesion/coupling policy
- `active`: [`unity-ui-authoring-workflow.md`](./owners/ui-workflow/unity-ui-authoring-workflow.md) - Unity UI authoring workflow
- `active`: [`codex_coding_guardrails.md`](./owners/operations/codex_coding_guardrails.md) - Codex coding guardrails
- `active`: [`presentation_layer_guardrails.md`](./owners/operations/presentation_layer_guardrails.md) - Presentation Layer guardrails
- `active`: [`stitch_data_workflow.md`](./owners/ui-workflow/stitch_data_workflow.md) - Stitch data workflow
- `active`: [`stitch_structured_handoff_contract.md`](./owners/ui-workflow/stitch_structured_handoff_contract.md) - Stitch handoff contract
- `active`: [`document_management_workflow.md`](./owners/operations/document_management_workflow.md) - document lifecycle policy
- `active`: [`plan_authoring_review_workflow.md`](./owners/operations/plan_authoring_review_workflow.md) - plan authoring workflow
- `active`: [`skill_routing_registry.md`](./owners/operations/skill_routing_registry.md) - skill route registry
- `active`: [`skill_trigger_matrix.md`](./owners/operations/skill_trigger_matrix.md) - skill trigger matrix
- `active`: [`acceptance_reporting_guardrails.md`](./owners/operations/acceptance_reporting_guardrails.md) - acceptance reporting guardrails
- `reference`: [`firebase_hosting.md`](./owners/operations/firebase_hosting.md) - Firebase hosting reference
- `reference`: [`anti_pattern_analysis.md`](./owners/architecture/anti_pattern_analysis.md) - anti-pattern analysis reference
- `active`: [`game_design.md`](./owners/design/game_design.md) - game design SSOT
- `active`: [`world_design.md`](./owners/design/world_design.md) - world/naming design SSOT
- `active`: [`ui_foundations.md`](./owners/design/ui_foundations.md) - UI foundations SSOT
- `active`: [`ui_reference_workflow.md`](./owners/design/ui_reference_workflow.md) - UI reference workflow
- `active`: [`unit_module_design.md`](./owners/design/unit_module_design.md) - unit/module design
- `active`: [`module_data_structure.md`](./owners/design/module_data_structure.md) - module data structure
- `active`: [`runtime_validation_checklist.md`](./owners/validation/runtime_validation_checklist.md) - runtime validation checklist
- `active`: [`webgl_smoke_checklist.md`](./owners/validation/webgl_smoke_checklist.md) - WebGL smoke checklist
- `active`: [`game_scene_flow_validation_closeout_plan.md`](./plans/active/game_scene_flow_validation_closeout_plan.md) - GameScene flow closeout
- `active`: [`webgl-audio-closeout.md`](./plans/active/webgl-audio-closeout.md) - WebGL audio closeout
- `active`: [`audio_sfx_mcp_pipeline_plan.md`](./plans/active/audio_sfx_mcp_pipeline_plan.md) - audio SFX MCP pipeline
- `active`: [`nova1492-content-residual-plan.md`](./plans/active/nova1492-content-residual-plan.md) - Nova1492 content residual
- `reference`: [`uitk-page-routing-refactor.md`](./plans/reference/uitk-page-routing-refactor.md) - UITK page routing refactor
- `reference`: [`agent_workflow_skill_adoption_plan.md`](./plans/reference/agent_workflow_skill_adoption_plan.md) - agent workflow skill adoption
- `reference`: [`technical_debt_recurrence_prevention_plan.md`](./plans/reference/technical_debt_recurrence_prevention_plan.md) - technical debt recurrence prevention
- `reference`: [`non_stitch_ui_stitch_reimport_plan.md`](./plans/reference/non_stitch_ui_stitch_reimport_plan.md) - Non-Stitch UI handoff reference
- `historical`: [`nova1492_assembly_profile_recovery_plan.md`](./plans/historical/nova1492_assembly_profile_recovery_plan.md) - Nova1492 assembly profile recovery record

## Owner 요약

- 전역 진입점은 [`../AGENTS.md`](../AGENTS.md), `docs/` 내부 registry는 이 문서가 맡는다.
- 문서 역할 분리, 참조 규칙, entry 문서 기본값은 [`owners/operations/document_management_workflow.md`](./owners/operations/document_management_workflow.md)를 따른다.
- 규칙 본문과 lane별 closeout 기준은 각 active owner 문서에서만 유지한다.
