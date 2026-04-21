# Progress Changelog

> 마지막 업데이트: 2026-04-21
> 상태: reference
> doc_id: plans.progress-changelog
> role: reference
> owner_scope: `plans.progress`에서 분리한 dated implementation log와 최근 변동 요약
> upstream: plans.progress
> artifacts: `artifacts/unity/`, `artifacts/webgl/`

이 문서는 [`progress.md`](./progress.md) 를 가볍게 유지하기 위해 분리한 dated change log다.
현재 상태와 우선순위는 `plans.progress`가 owner이고, 여기서는 최근 구현 변동을 날짜별로 짧게 회고만 남긴다.
더 세밀한 코드 레벨 이력은 git history와 관련 plan/SSOT 문서를 함께 본다.

## 2026-04-21

- scene registry를 scene-owned contract로 강제하는 guardrail을 추가하고, hidden runtime repair와 global registry lookup fallback을 제거했다.
- Battle HUD Set D와 battle result Set E를 prefab pack seed로 가져와 `prefab-first reset` 경로를 진전시켰다.
- Lobby/Garage shared 2-tab navigation baseline을 정리하고, shared nav contract를 `LobbyGarageNavBar` 기준으로 수렴시켰다.
- Stitch handoff 활성 경로를 `.stitch/contracts/*.json` contract-only로 전환하고, 관련 schema 및 lint guard를 추가했다.
- `jg-stitch-workflow`를 얇은 router skill로 줄여 owner doc routing 중심으로 재구성했다.

## 2026-04-20

- `LobbyPageRoot` baseline prefab reset seed를 accepted Stitch handoff 기준으로 다시 생성했다.
- Stitch handoff completeness checklist를 추가하고, 관련 entry/read-order를 새 owner 문서 기준으로 연결했다.
- Lobby 공용 prefab pack baseline과 Garage Set B MCP authoring/prefab extraction을 진행했다.
- Unity UI 작업의 단일 정책 본문을 `ops.unity-ui-authoring-workflow`로 고정하고, workflow policy check 스크립트를 추가했다.

## 2026-04-19

- `PresentationLayoutOwnershipValidator`와 관련 MCP route를 추가해 presentation layer의 geometry/layout authoring을 hard-stop으로 막기 시작했다.
- Unity MCP를 `완전 자동화`가 아니라 `진단 + 수동 자동화` bridge로 재정의하고 stable/manual route를 정리했다.

## 2026-04-17

- Garage save/load WebGL smoke를 성공시켰고, 익명 세션 지속성 복구를 진행했다.
- Lobby/Game scene Inspector wiring, WebGL smoke checklist, tech debt reduction plan을 실제 코드 상태 기준으로 재정리했다.
- Lobby/Garage UI 런타임 탐색 anti-pattern 제거와 계정 카드/설정 경로 복구를 이어갔다.

## 2026-04-15 ~ 2026-04-14

- Unity MCP Play/UI/diagnostic route와 helper를 확장해 smoke와 상태 점검 자동화를 보강했다.
- Garage UI Phase 1~4 개선으로 ThemeColors, result panel, slot/editor/selector 피드백, 탭 활성 시각 정리를 진행했다.

## 2026-04-12 ~ 2026-04-11

- Account feature 골격과 Google 로그인 경로를 코드에 연결했다.
- Lobby/Garage scene wiring과 WebGL 브리지, 계정 설정 UI를 추가했다.

## 2026-04-10 ~ 2026-04-08

- Phase 5~9 관련 네트워크 동기화, 소환 시스템, 로비 전환, 게임 종료 처리를 마감했다.
- rule harness, scene rename, compile fix, 문서 정리와 포트 ownership 정리를 진행했다.

## 참고

- 현재 상태와 다음 작업: [`progress.md`](./progress.md)
- GameScene 진입 상위 흐름: [`game_scene_entry_plan.md`](./game_scene_entry_plan.md)
- 계정/차고 복구 계획: [`account_system_plan.md`](./account_system_plan.md)
