# Docs Index

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: `docs/` 탐색 경로, 문서 상태 라벨, 현재 owner 문서 진입점
> upstream: repo.agents
> artifacts: none

이 문서는 `docs/` 탐색용 인덱스다. 루트 진입점은 [`../AGENTS.md`](../AGENTS.md), 진행 상황 SSOT는 [`plans/progress.md`](./plans/progress.md)다.
이 레포에서 사람 기준 current path 해석은 `docs/index.md`를 공식 registry로 삼고, `doc_id`는 stable owner identifier로 사용한다.

이 인덱스는 registry 역할만 유지하고, 규칙 본문은 각 owner 문서에만 둔다.

## Quick Start

- Unity UI / prefab / scene 작업: [`plans/progress.md`](./plans/progress.md) -> current owner/residual route -> [`ops/unity-ui-authoring-workflow.md`](./ops/unity-ui-authoring-workflow.md) -> [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md) -> relevant contract/prefab
- Stitch / handoff 작업: [`design/ui_reference_workflow.md`](./design/ui_reference_workflow.md) -> [`ops/stitch_data_workflow.md`](./ops/stitch_data_workflow.md) -> [`ops/stitch_structured_handoff_contract.md`](./ops/stitch_structured_handoff_contract.md) -> [`../tools/stitch-unity/README.md`](../tools/stitch-unity/README.md)
- 코딩 구현 / 버그 수정 / 리팩터 / 테스트 보강: [`ops/codex_coding_guardrails.md`](./ops/codex_coding_guardrails.md) -> relevant lane owner doc -> concrete code/tests
- GameScene 검증 작업: [`plans/progress.md`](./plans/progress.md) -> current owner/residual route -> [`playtest/runtime_validation_checklist.md`](./playtest/runtime_validation_checklist.md) -> relevant smoke/test code
- 문서 / workflow 정리: [`ops/document_management_workflow.md`](./ops/document_management_workflow.md) -> [`ops/cohesion-coupling-policy.md`](./ops/cohesion-coupling-policy.md) -> relevant owner doc
- Skill route / trigger 정리: [`ops/document_management_workflow.md`](./ops/document_management_workflow.md) -> [`ops/skill_routing_registry.md`](./ops/skill_routing_registry.md) -> [`ops/skill_trigger_matrix.md`](./ops/skill_trigger_matrix.md) -> relevant skill entry

## 상태 규칙

- `active`: 현재 작업 기준으로 직접 참고하는 문서
- `draft`: 방향은 유효하지만 세부안은 아직 확정 전인 문서
- `paused`: 이유가 있어 실행을 멈춘 계획 문서
- `historical`: 당시 판단 기록은 남기되 현재 구현 기준으로 쓰지 않는 문서
- `reference`: 절차나 운영 방법처럼 필요할 때 다시 보는 문서

## Doc ID Registry

| doc_id | 파일명 | 상태 |
|---|---|---|
| docs.index | index.md | active |
| plans.progress | plans/progress.md | active |
| ops.cohesion-coupling-policy | ops/cohesion-coupling-policy.md | active |
| ops.unity-ui-authoring-workflow | ops/unity-ui-authoring-workflow.md | active |
| ops.codex-coding-guardrails | ops/codex_coding_guardrails.md | active |
| ops.presentation-layer-guardrails | ops/presentation_layer_guardrails.md | active |
| ops.stitch-data-workflow | ops/stitch_data_workflow.md | active |
| ops.stitch-structured-handoff-contract | ops/stitch_structured_handoff_contract.md | active |
| ops.document-management-workflow | ops/document_management_workflow.md | active |
| ops.plan-authoring-review-workflow | ops/plan_authoring_review_workflow.md | active |
| ops.skill-routing-registry | ops/skill_routing_registry.md | active |
| ops.skill-trigger-matrix | ops/skill_trigger_matrix.md | active |
| ops.acceptance-reporting-guardrails | ops/acceptance_reporting_guardrails.md | active |
| architecture.anti-pattern-analysis | architecture/anti_pattern_analysis.md | reference |
| design.game-design | design/game_design.md | active |
| design.world-design | design/world_design.md | active |
| design.ui-foundations | design/ui_foundations.md | active |
| design.ui-reference-workflow | design/ui_reference_workflow.md | active |
| design.unit-module-design | design/unit_module_design.md | active |
| design.module-data-structure | design/module_data_structure.md | active |
| plans.game-scene-flow-validation-closeout | plans/game_scene_flow_validation_closeout_plan.md | active |
| plans.webgl-audio-closeout | plans/webgl-audio-closeout.md | active |
| plans.audio-sfx-mcp-pipeline | plans/audio_sfx_mcp_pipeline_plan.md | active |
| plans.nova1492-content-residual | plans/nova1492-content-residual-plan.md | active |
| plans.uitk-page-routing-refactor | plans/uitk-page-routing-refactor.md | reference |
| plans.agent-workflow-skill-adoption | plans/agent_workflow_skill_adoption_plan.md | reference |
| plans.technical-debt-recurrence-prevention | plans/technical_debt_recurrence_prevention_plan.md | reference |
| plans.nova1492-assembly-profile-recovery | plans/nova1492_assembly_profile_recovery_plan.md | historical |
| ops.firebase-hosting | ops/firebase_hosting.md | reference |
| playtest.runtime-validation-checklist | playtest/runtime_validation_checklist.md | active |
| playtest.webgl-smoke-checklist | playtest/webgl_smoke_checklist.md | active |

## 먼저 볼 곳

| 상황 | 먼저 볼 문서 |
|---|---|
| 지금 뭐가 진행 중인지 확인 | [`plans/progress.md`](./plans/progress.md) |
| 응집도/결합도 상위 기준 확인 | [`ops/cohesion-coupling-policy.md`](./ops/cohesion-coupling-policy.md) |
| Plan Mode / Codex 운영 규칙 확인 | `rule-operations` route와 repo owner 문서 (`docs/index.md`로 current path를 해석한 뒤 읽기) |
| Skill route / 전역 skill 이름 확인 | [`ops/skill_routing_registry.md`](./ops/skill_routing_registry.md) |
| Skill trigger 기대값 확인 | [`ops/skill_trigger_matrix.md`](./ops/skill_trigger_matrix.md) |
| Codex 코딩 가드레일 확인 | [`ops/codex_coding_guardrails.md`](./ops/codex_coding_guardrails.md) |
| Unity UI/UX 작업 시작 규칙 확인 | [`ops/unity-ui-authoring-workflow.md`](./ops/unity-ui-authoring-workflow.md) |
| 게임 방향과 MVP 기준 확인 | [`design/game_design.md`](./design/game_design.md) |
| 세계관 / Nova1492 원전 기반 정서 기준 확인 | [`design/world_design.md`](./design/world_design.md) |
| Garage UI 레이아웃/토큰/Unity handoff 기준 확인 | [`design/ui_foundations.md`](./design/ui_foundations.md) |
| Stitch UI 시안 워크플로우 확인 | [`design/ui_reference_workflow.md`](./design/ui_reference_workflow.md) |
| Stitch 데이터 저장/갱신/Unity handoff 흐름 확인 | [`ops/stitch_data_workflow.md`](./ops/stitch_data_workflow.md) |
| Stitch JSON handoff contract 구조 확인 | [`ops/stitch_structured_handoff_contract.md`](./ops/stitch_structured_handoff_contract.md) |
| Stitch surface 실행 명령과 review capture 루프 확인 | [`../tools/stitch-unity/README.md`](../tools/stitch-unity/README.md) |
| GameScene 검증/closeout owner 확인 | [`plans/progress.md`](./plans/progress.md) |
| Account/Garage WebGL acceptance 확인 | [`playtest/webgl_smoke_checklist.md`](./playtest/webgl_smoke_checklist.md) |
| Unity MCP 실행 루틴 확인 | [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md) |
| WebGL 실기 절차 확인 | [`playtest/webgl_smoke_checklist.md`](./playtest/webgl_smoke_checklist.md) |
| Firebase 배포 절차 확인 | [`ops/firebase_hosting.md`](./ops/firebase_hosting.md) |
| 문서 역할/참조/리네임 관리 기준 확인 | [`ops/document_management_workflow.md`](./ops/document_management_workflow.md) |

## 폴더별 안내

### `design/`

- `active`: [`game_design.md`](./design/game_design.md) - 현재 게임 방향 SSOT
- `active`: [`world_design.md`](./design/world_design.md) - Nova1492 원전 기반 세계관과 정서 기준 SSOT
- `active`: [`ui_foundations.md`](./design/ui_foundations.md) - Garage UI 레이아웃/토큰/Unity 변환 기준
- `active`: [`ui_reference_workflow.md`](./design/ui_reference_workflow.md) - Stitch 기반 UI 시안 활용 기준
- `active`: [`unit_module_design.md`](./design/unit_module_design.md) - 유닛/모듈 설계 기준
- `active`: [`module_data_structure.md`](./design/module_data_structure.md) - 실제 구현 데이터 구조 기준

### `architecture/`

- `reference`: [`anti_pattern_analysis.md`](./architecture/anti_pattern_analysis.md) - Presentation/Domain anti-pattern 조사 기록과 후속 재발방지 참고

### `plans/`

- `active`: [`progress.md`](./plans/progress.md) - 공식 진행률 SSOT
- `active`: [`game_scene_flow_validation_closeout_plan.md`](./plans/game_scene_flow_validation_closeout_plan.md) - GameScene/BattleScene single-client flow closeout owner
- `active`: [`webgl-audio-closeout.md`](./plans/webgl-audio-closeout.md) - WebGL audio smoke owner
- `active`: [`audio_sfx_mcp_pipeline_plan.md`](./plans/audio_sfx_mcp_pipeline_plan.md) - direct Suno MCP SFX generation and Unity SoundCatalog pipeline owner
- `active`: [`nova1492-content-residual-plan.md`](./plans/nova1492-content-residual-plan.md) - Nova1492 content handoff owner
- `historical`: [`nova1492_assembly_profile_recovery_plan.md`](./plans/nova1492_assembly_profile_recovery_plan.md) - Nova1492 original slot/assembly profile reconstruction attempt record
- `reference`: [`non_stitch_ui_stitch_reimport_plan.md`](./plans/non_stitch_ui_stitch_reimport_plan.md) - Non-Stitch UI source/candidate handoff closeout and residual route
- `reference`: [`agent_workflow_skill_adoption_plan.md`](./plans/agent_workflow_skill_adoption_plan.md) - Matt Pocock skills audit and JG owner adoption route
- `reference`: [`technical_debt_recurrence_prevention_plan.md`](./plans/technical_debt_recurrence_prevention_plan.md) - runtime repair and dead-code retention recurrence reference
- `reference`: [`uitk-page-routing-refactor.md`](./plans/uitk-page-routing-refactor.md) - Lobby/Garage UITK Shell/Layout-style explicit page routing refactor closeout reference

### `playtest/`

- `active`: [`runtime_validation_checklist.md`](./playtest/runtime_validation_checklist.md) - 런타임 수동 검증 체크리스트와 GameScene multiplayer residual closeout checklist
- `active`: [`webgl_smoke_checklist.md`](./playtest/webgl_smoke_checklist.md) - WebGL 실기 체크리스트와 Account/Garage product smoke residual closeout checklist

### `ops/`

- `active`: [`cohesion-coupling-policy.md`](./ops/cohesion-coupling-policy.md) - 문서/코드/씬/프리팹/자동화 공통 응집도/결합도 상위 기준
- `active`: [`codex_coding_guardrails.md`](./ops/codex_coding_guardrails.md) - Codex 구현/리팩터/버그 수정/검증 작업의 일반 코딩 가드레일
- `active`: [`presentation_layer_guardrails.md`](./ops/presentation_layer_guardrails.md) - Presentation Layer 코딩 규칙과 안티패턴 방지 기준
- `active`: [`unity-ui-authoring-workflow.md`](./ops/unity-ui-authoring-workflow.md) - Unity UI/UX 작업 진입 SSOT
- `active`: [`stitch_data_workflow.md`](./ops/stitch_data_workflow.md) - Stitch working data와 Unity handoff 운영 기준
- `active`: [`stitch_structured_handoff_contract.md`](./ops/stitch_structured_handoff_contract.md) - Stitch 산출물을 JSON 번역 계약으로 고정하는 구조 SSOT
- `active`: [`document_management_workflow.md`](./ops/document_management_workflow.md) - 문서 운영 상위 원칙과 역할/참조/삭제 관리 기준
- `active`: [`plan_authoring_review_workflow.md`](./ops/plan_authoring_review_workflow.md) - plan closeout 원칙 적용 절차
- `active`: [`skill_routing_registry.md`](./ops/skill_routing_registry.md) - repo-local 및 external/global skill route 이름 registry
- `active`: [`skill_trigger_matrix.md`](./ops/skill_trigger_matrix.md) - skill trigger 기대값 fixture matrix
- `active`: [`acceptance_reporting_guardrails.md`](./ops/acceptance_reporting_guardrails.md) - mechanical/acceptance 분리와 success/blocked/mismatch 판정 기준
- `reference`: [`firebase_hosting.md`](./ops/firebase_hosting.md) - Firebase hosting 배포 절차

## Owner 요약

- 전역 진입점은 [`../AGENTS.md`](../AGENTS.md), `docs/` 내부 registry는 이 문서가 맡는다.
- 문서 역할 분리, 참조 규칙, entry 문서 기본값은 [`./ops/document_management_workflow.md`](./ops/document_management_workflow.md)를 따른다.
- 규칙 본문과 lane별 closeout 기준은 각 active owner 문서에서만 유지한다.
