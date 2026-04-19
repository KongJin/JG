# Docs Index

> 마지막 업데이트: 2026-04-18

이 문서는 `docs/` 탐색용 인덱스다. 루트 진입점은 [`../AGENTS.md`](../AGENTS.md), 진행 상황 SSOT는 [`plans/progress.md`](./plans/progress.md)다.

현재 검증 기본선은 다음으로 읽으면 된다.

1. `progress.md`
2. `tools/unity-mcp/README.md`
3. relevant contract/test code

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
| 게임 방향과 MVP 기준 확인 | [`design/game_design.md`](./design/game_design.md) |
| Garage UI 레이아웃/토큰/Unity handoff 기준 확인 | [`design/ui_foundations.md`](./design/ui_foundations.md) |
| 외부 UI 레퍼런스 워크플로우 확인 | [`design/ui_reference_workflow.md`](./design/ui_reference_workflow.md) |
| GameScene 진입 큰 흐름 확인 | [`plans/game_scene_entry_plan.md`](./plans/game_scene_entry_plan.md) |
| 계정/차고 복구 상태 확인 | [`plans/account_system_plan.md`](./plans/account_system_plan.md) |
| Unity MCP 검증 루틴 확인 | [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md) |
| WebGL 실기 절차 확인 | [`plans/webgl_smoke_checklist.md`](./plans/webgl_smoke_checklist.md) |
| Firebase 배포 절차 확인 | [`ops/firebase_hosting.md`](./ops/firebase_hosting.md) |

## 폴더별 안내

### `design/`

- `active`: [`game_design.md`](./design/game_design.md) - 현재 게임 방향 SSOT
- `active`: [`ui_foundations.md`](./design/ui_foundations.md) - Garage UI 레이아웃/토큰/Unity 변환 기준
- `active`: [`ui_reference_workflow.md`](./design/ui_reference_workflow.md) - Mobbin/Relume 기반 외부 레퍼런스 활용 기준
- `active`: [`unit_module_design.md`](./design/unit_module_design.md) - 유닛/모듈 설계 기준
- `active`: [`module_data_structure.md`](./design/module_data_structure.md) - 실제 구현 데이터 구조 기준
- `reference`: [`architecture-diagram.md`](./design/architecture-diagram.md) - 포트 소유권/의존성 개요
- `reference`: [`unit_feature_separation.md`](./design/unit_feature_separation.md) - 유닛 기능 분리 배경과 설계 메모

### `plans/`

- `active`: [`progress.md`](./plans/progress.md) - 공식 진행률 SSOT
- `active`: [`account_system_plan.md`](./plans/account_system_plan.md) - 계정/차고 복구 계획
- `active`: [`game_scene_entry_plan.md`](./plans/game_scene_entry_plan.md) - GameScene 진입 상위 흐름
- `active`: [`tech_debt_reduction_plan.md`](./plans/tech_debt_reduction_plan.md) - 현재 기술부채 실행 순서
- `active`: [`webgl_smoke_checklist.md`](./plans/webgl_smoke_checklist.md) - WebGL 실기 체크리스트
- `draft`: [`garage_ui_ux_improvement_plan.md`](./plans/garage_ui_ux_improvement_plan.md) - Garage UI backlog
- `active`: [`mcp_improvement_plan.md`](./plans/mcp_improvement_plan.md) - Unity MCP 역할/검증 레이어 정리
- `paused`: [`figma_ui_system_plan.md`](./plans/figma_ui_system_plan.md) - Figma 연동 보류 기록
- `historical`: [`codex_lobby_garage_panel_plan.md`](./plans/codex_lobby_garage_panel_plan.md) - 초창기 Garage 패널 계획

### `playtest/`

- `active`: [`runtime_validation_checklist.md`](./playtest/runtime_validation_checklist.md) - 런타임 수동 검증 기준
- `reference`: [`playtest_mvp_template.md`](./playtest/playtest_mvp_template.md) - 세션 기록 템플릿

### `ops/`

- `reference`: [`firebase_hosting.md`](./ops/firebase_hosting.md) - Firebase hosting 배포 절차

### `discussions/`

- `reference`: [`discussion_removed_features_rationale.md`](./discussions/discussion_removed_features_rationale.md) - 제외/폐기 근거 메모
- `reference`: [`discussion_game_fun_personas.md`](./discussions/discussion_game_fun_personas.md) - 재미 토론 보조 문서
- `historical`: [`discussion_unity.md`](./discussions/discussion_unity.md) - 규칙/도구 한계 검토 기록

## 코드와 문서의 경계

- 전역 진입점과 상위 링크는 [`../AGENTS.md`](../AGENTS.md)에서 시작한다.
- Unity MCP 실행 루틴과 canonical smoke 기준은 [`../tools/unity-mcp/README.md`](../tools/unity-mcp/README.md)를 우선한다.
- 세션 메모는 루트 [`../AGENTS.md`](../AGENTS.md)에 짧게만 남기고, 장기 기준은 `docs/` 쪽 SSOT로 승격한다.
