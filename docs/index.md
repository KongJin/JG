# Docs Index

> 마지막 업데이트: 2026-04-24
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: `docs/` 탐색 경로, 문서 상태 라벨, 현재 owner 문서 진입점
> upstream: `AGENTS.md`
> artifacts: none

이 문서는 `docs/` 탐색용 인덱스다. 루트 진입점은 [`../AGENTS.md`](../AGENTS.md), 진행 상황 SSOT는 [`plans/progress.md`](./plans/progress.md)다.
이 레포에서 사람 기준 current path 해석은 `docs/index.md`를 공식 registry로 삼고, `doc_id`는 stable owner identifier로 사용한다.

현재 검증 기본선은 다음으로 읽으면 된다.

1. `progress.md`
2. `ops/unity_ui_authoring_workflow.md`
3. `tools/unity-mcp/README.md`
4. relevant contract/test code

## Quick Start

- Unity UI / prefab / scene 작업: [`plans/progress.md`](./plans/progress.md) -> [`ops/unity_ui_authoring_workflow.md`](./ops/unity_ui_authoring_workflow.md) -> [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md) -> relevant contract/prefab
- Stitch / handoff 작업: [`design/ui_reference_workflow.md`](./design/ui_reference_workflow.md) -> [`ops/stitch_data_workflow.md`](./ops/stitch_data_workflow.md) -> [`ops/stitch_structured_handoff_contract.md`](./ops/stitch_structured_handoff_contract.md) -> [`../tools/stitch-unity/README.md`](../tools/stitch-unity/README.md)
  기준 흐름은 `source freeze -> execution contracts -> translation output`이다.
- Stitch -> Unity 한 장씩 번역: [`ops/stitch_to_unity_translation_guide.md`](./ops/stitch_to_unity_translation_guide.md) -> [`ops/stitch_data_workflow.md`](./ops/stitch_data_workflow.md) -> [`../tools/stitch-unity/README.md`](../tools/stitch-unity/README.md) -> [`ops/unity_ui_authoring_workflow.md`](./ops/unity_ui_authoring_workflow.md)
  실행 전에는 source에서 execution contract가 다시 준비되는지 먼저 본다.
- 문서 / workflow 정리: [`ops/cohesion_coupling_policy.md`](./ops/cohesion_coupling_policy.md) -> [`ops/document_management_workflow.md`](./ops/document_management_workflow.md) -> [`ops/plan_authoring_review_workflow.md`](./ops/plan_authoring_review_workflow.md) -> [`ops/acceptance_reporting_guardrails.md`](./ops/acceptance_reporting_guardrails.md) -> `docs/index.md` -> relevant owner doc
- GameScene 검증 작업: [`plans/progress.md`](./plans/progress.md) -> [`plans/game_scene_entry_plan.md`](./plans/game_scene_entry_plan.md) -> [`playtest/runtime_validation_checklist.md`](./playtest/runtime_validation_checklist.md) -> relevant smoke/test code

## 상태 규칙

- `active`: 현재 작업 기준으로 직접 참고하는 문서
- `draft`: 방향은 유효하지만 세부안은 아직 확정 전인 문서
- `paused`: 이유가 있어 실행을 멈춘 계획 문서
- `historical`: 당시 판단 기록은 남기되 현재 구현 기준으로 쓰지 않는 문서
- `reference`: 절차나 운영 방법처럼 필요할 때 다시 보는 문서

## 먼저 볼 곳

| 상황 | 먼저 볼 문서 |
|---|---|
| 지금 뭐가 진행 중인지 확인 | [`plans/progress.md`](./plans/progress.md) |
| 응집도/결합도 상위 기준 확인 | [`ops/cohesion_coupling_policy.md`](./ops/cohesion_coupling_policy.md) |
| Plan Mode / Codex 운영 규칙 확인 | `rule-operations` owner 문서 (`docs/index.md`로 current path를 해석한 뒤 읽기) |
| 계획 문서 작성/재리뷰 기준 확인 | [`ops/plan_authoring_review_workflow.md`](./ops/plan_authoring_review_workflow.md) |
| acceptance / reporting 기준 확인 | [`ops/acceptance_reporting_guardrails.md`](./ops/acceptance_reporting_guardrails.md) |
| Unity UI/UX 작업 시작 규칙 확인 | [`ops/unity_ui_authoring_workflow.md`](./ops/unity_ui_authoring_workflow.md) |
| 게임 방향과 MVP 기준 확인 | [`design/game_design.md`](./design/game_design.md) |
| Garage UI 레이아웃/토큰/Unity handoff 기준 확인 | [`design/ui_foundations.md`](./design/ui_foundations.md) |
| Stitch UI 시안 워크플로우 확인 | [`design/ui_reference_workflow.md`](./design/ui_reference_workflow.md) |
| Stitch 데이터 저장/갱신/Unity handoff 흐름 확인 | [`ops/stitch_data_workflow.md`](./ops/stitch_data_workflow.md) |
| Stitch JSON handoff contract 구조 확인 | [`ops/stitch_structured_handoff_contract.md`](./ops/stitch_structured_handoff_contract.md) |
| Stitch screen을 Unity로 한 장씩 옮기는 실무 가이드 확인 | [`ops/stitch_to_unity_translation_guide.md`](./ops/stitch_to_unity_translation_guide.md) |
| Stitch surface 실행 명령과 review capture 루프 확인 | [`../tools/stitch-unity/README.md`](../tools/stitch-unity/README.md) |
| Stitch 전면 개편 실행 계획 확인 | [`plans/stitch_ui_ux_overhaul_plan.md`](./plans/stitch_ui_ux_overhaul_plan.md) |
| Stitch 테스트 프롬프트 확인 | [`design/stitch_test_brief.md`](./design/stitch_test_brief.md) |
| GameScene 진입 큰 흐름 확인 | [`plans/game_scene_entry_plan.md`](./plans/game_scene_entry_plan.md) |
| GameScene 전투 HUD / 소환 UX 개선 계획 확인 | [`plans/game_scene_ui_ux_improvement_plan.md`](./plans/game_scene_ui_ux_improvement_plan.md) |
| 계정/차고 복구 상태 확인 | [`plans/account_system_plan.md`](./plans/account_system_plan.md) |
| Unity MCP 실행 루틴 확인 | [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md) |
| WebGL 실기 절차 확인 | [`plans/webgl_smoke_checklist.md`](./plans/webgl_smoke_checklist.md) |
| Firebase 배포 절차 확인 | [`ops/firebase_hosting.md`](./ops/firebase_hosting.md) |
| 문서 역할/참조/리네임 관리 기준 확인 | [`ops/document_management_workflow.md`](./ops/document_management_workflow.md) |

## 폴더별 안내

### `design/`

- `active`: [`game_design.md`](./design/game_design.md) - 현재 게임 방향 SSOT
- `active`: [`ui_foundations.md`](./design/ui_foundations.md) - Garage UI 레이아웃/토큰/Unity 변환 기준
- `active`: [`ui_reference_workflow.md`](./design/ui_reference_workflow.md) - Stitch 기반 UI 시안 활용 기준
- `active`: [`stitch_test_brief.md`](./design/stitch_test_brief.md) - Stitch 붙여넣기용 테스트 브리프
- `active`: [`unit_module_design.md`](./design/unit_module_design.md) - 유닛/모듈 설계 기준
- `active`: [`module_data_structure.md`](./design/module_data_structure.md) - 실제 구현 데이터 구조 기준
- `reference`: [`architecture-diagram.md`](./design/architecture-diagram.md) - 포트 소유권/의존성 개요
- `reference`: [`unit_feature_separation.md`](./design/unit_feature_separation.md) - 유닛 기능 분리 배경과 설계 메모

### `plans/`

- `active`: [`progress.md`](./plans/progress.md) - 공식 진행률 SSOT
- `reference`: [`progress_changelog.md`](./plans/progress_changelog.md) - 진행 상황에서 분리한 dated change log
- `active`: [`account_system_plan.md`](./plans/account_system_plan.md) - 계정/차고 복구 계획
- `active`: [`game_scene_entry_plan.md`](./plans/game_scene_entry_plan.md) - GameScene 진입 상위 흐름
- `active`: [`stitch_ui_ux_overhaul_plan.md`](./plans/stitch_ui_ux_overhaul_plan.md) - active surface와 reset 우선순위 기준
- `draft`: [`game_scene_ui_ux_improvement_plan.md`](./plans/game_scene_ui_ux_improvement_plan.md) - GameScene 전투 HUD/소환 UX 재설계 계획
- `active`: [`tech_debt_reduction_plan.md`](./plans/tech_debt_reduction_plan.md) - 현재 기술부채 실행 순서
- `active`: [`webgl_smoke_checklist.md`](./plans/webgl_smoke_checklist.md) - WebGL 실기 체크리스트
- `draft`: [`garage_ui_ux_improvement_plan.md`](./plans/garage_ui_ux_improvement_plan.md) - Garage Stitch-to-Unity recovery plan
- `active`: [`mcp_improvement_plan.md`](./plans/mcp_improvement_plan.md) - Unity MCP 역할/검증 레이어 정리
- `reference`: [`implementation_plan_mvp_fun.md`](./plans/implementation_plan_mvp_fun.md) - MVP 재미 검증 압축 실행 체크리스트
- `historical`: [`codex_lobby_garage_panel_plan.md`](./plans/codex_lobby_garage_panel_plan.md) - 초창기 Garage 패널 계획

### `playtest/`

- `active`: [`runtime_validation_checklist.md`](./playtest/runtime_validation_checklist.md) - 런타임 수동 검증 기준
- `reference`: [`playtest_mvp_template.md`](./playtest/playtest_mvp_template.md) - 세션 기록 템플릿

### `ops/`

- `active`: [`cohesion_coupling_policy.md`](./ops/cohesion_coupling_policy.md) - 문서/코드/씬/프리팹/자동화 공통 응집도/결합도 상위 기준
- `active`: [`unity_ui_authoring_workflow.md`](./ops/unity_ui_authoring_workflow.md) - Unity UI/UX 작업 진입 SSOT
- `active`: [`stitch_data_workflow.md`](./ops/stitch_data_workflow.md) - Stitch working data와 Unity handoff 운영 기준
- `active`: [`stitch_structured_handoff_contract.md`](./ops/stitch_structured_handoff_contract.md) - Stitch 산출물을 JSON 번역 계약으로 고정하는 구조 SSOT
- `reference`: [`stitch_to_unity_translation_guide.md`](./ops/stitch_to_unity_translation_guide.md) - accepted Stitch screen을 Unity prefab/scene contract로 옮기는 실무 가이드
- `reference`: [`stitch_handoff_completeness_checklist.md`](./ops/stitch_handoff_completeness_checklist.md) - Stitch handoff completeness 빠른 점검표
- `active`: [`document_management_workflow.md`](./ops/document_management_workflow.md) - 문서 역할/참조/리네임/삭제 관리 기준
- `active`: [`plan_authoring_review_workflow.md`](./ops/plan_authoring_review_workflow.md) - 계획 문서 작성 후 반복 재리뷰와 closeout 기준
- `active`: [`acceptance_reporting_guardrails.md`](./ops/acceptance_reporting_guardrails.md) - acceptance lock, blocked/mismatch/success 판정, reporting guardrail 기준
- `reference`: [`firebase_hosting.md`](./ops/firebase_hosting.md) - Firebase hosting 배포 절차

### `discussions/`

- `reference`: [`discussion_removed_features_rationale.md`](./discussions/discussion_removed_features_rationale.md) - 제외/폐기 근거 메모
- `reference`: [`discussion_game_fun_personas.md`](./discussions/discussion_game_fun_personas.md) - 재미 토론 보조 문서
- `historical`: [`discussion_unity.md`](./discussions/discussion_unity.md) - 규칙/도구 한계 검토 기록

### root `docs/`

- `historical`: [`tech_debt_review.md`](./tech_debt_review.md) - 당시 기술부채 심각도 평가 기록

## 코드와 문서의 경계

- 전역 진입점과 상위 링크는 [`../AGENTS.md`](../AGENTS.md)에서 시작한다.
- 응집도/결합도 정의와 hard-fail/review 경계는 [`./ops/cohesion_coupling_policy.md`](./ops/cohesion_coupling_policy.md)를 상위 owner로 본다.
- Plan Mode 또는 규칙/운영/Codex 절차 작업은 `rule-operations` owner 문서를 먼저 보고, 그 lane에서는 mutation 없이 inspection/reference만 수행한다.
- 계획 문서 작성 후 반복 재리뷰와 clean closeout 기준은 [`./ops/plan_authoring_review_workflow.md`](./ops/plan_authoring_review_workflow.md)에만 둔다.
- acceptance 입력 우선순위와 closeout/reporting guardrail 기준은 [`./ops/acceptance_reporting_guardrails.md`](./ops/acceptance_reporting_guardrails.md)에만 둔다.
- Unity UI/UX authoring 정책 본문은 [`./ops/unity_ui_authoring_workflow.md`](./ops/unity_ui_authoring_workflow.md)에만 둔다.
- Unity MCP 실행 루틴과 canonical smoke 기준은 [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md)를 reference로 본다.
- 세션 메모는 루트 [`../AGENTS.md`](../AGENTS.md)에 짧게만 남기고, 장기 기준은 `docs/` 쪽 SSOT로 승격한다.
