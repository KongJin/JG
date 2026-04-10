# 게임 씬 진입 계획 (Game Scene Entry Plan)

> **마지막 업데이트**: 2026-04-11
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 GameScene 진입 작업의 큰 흐름만 유지한다. 상세 구현 이력과 완료 판단은 `progress.md`를 기준으로 본다.

---

## 핵심 결정

| 항목 | 현재 기준 |
|---|---|
| 전투 진입 흐름 | `LobbyScene` → `GameScene` |
| 편성 데이터 | `garageRoster`를 Room CustomProperties로 동기화 |
| 유닛 스펙 계산 | 게임 시작 시 각 클라이언트가 자체 계산 |
| 전투 리소스 | `Mana`가 아니라 `Energy` |
| 슬롯 구조 | 3개 표시 슬롯 + 6개 로테이션 |
| 배치 방식 | 고정 배치 영역 + 클릭/드래그 입력 |
| 전투 시작 | Skill 선택 조건 없이 바로 시작 |
| 네트워크 원칙 | BattleEntity는 owner 중심 동기화, late-join 복구 유지 |

---

## 완료된 Phase 요약

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0 | ✅ 완료 | Lobby → Game 진입 전 `garageRoster` 직렬화와 동기화 경로 정리 |
| Phase 1 | ✅ 완료 | EventBus, Unit/Garage bootstrap, Unit 스펙 계산 파이프라인 연결 |
| Phase 2 | ✅ 완료 | 소환 시스템, Energy, Unit 슬롯 UI, 로테이션 구현 |
| Phase 3 | ✅ 완료 | Wave 즉시 시작, Enemy → Unit 타겟팅, Combat 등록 연결 |
| Phase 4 | ✅ 완료 | 재소환 이벤트와 UI 피드백 정리 |
| Phase 5 | ✅ 완료 | late-join 복구와 기본 네트워크 동기화 경로 연결 |
| Phase 6 | ✅ 완료 | 승패 처리, 결과 화면, Analytics 연결 |
| Phase 7 | ✅ 완료 | 배치 영역 시각화, 드래그 피드백, 배치 검증 보강 |
| Phase 8 | ✅ 완료 | 시간 기반 Energy 재생 증가 곡선 적용 |

Phase 0~8의 세부 커밋과 작업 로그는 [`progress.md`](./progress.md)에 누적한다.

---

## 현재 집중 영역: Phase 9

Phase 9는 네트워크 완성 단계다. 현재 활성 TODO는 `progress.md`와 동일하게 아래 세 가지다.

1. 명시적 `[PunRPC]` 기반 BattleEntity remote 사망 동기화 보강
2. `PlacementAreaView`, `DragGhostPrefab` 등 Inspector 직렬화 참조 최종 검증
3. 실제 멀티플레이어 smoke 테스트로 late-join, BattleEntity sync, Energy sync 확인

---

## 유지 원칙

- GameScene 진입 관련 상세 진행률, 최근 커밋, 남은 TODO는 항상 [`progress.md`](./progress.md)에 먼저 반영한다.
- 이 문서에는 이미 끝난 Phase의 세부 구현 순서나 오래된 “다음 액션” 체크리스트를 다시 복제하지 않는다.
- 설계 기준은 [`../design/game_design.md`](../design/game_design.md), 전역 구조 규칙은 [`../../agent/architecture.md`](../../agent/architecture.md)를 따른다.
