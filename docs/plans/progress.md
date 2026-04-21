# 진행 상황 (Game Scene Entry)

> 마지막 업데이트: 2026-04-21
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: 레포 전체 현재 상태, 현재 포커스, 다음 작업
> upstream: docs.index
> artifacts: `artifacts/unity/`, `artifacts/webgl/`

## 상태 주석

- 중요: Phase 0~9의 `완료` 표기는 주로 코드 경로 기준이다.
- 2026-04-18 확인 시 실제 `Assets/Scenes/GameScene.unity` 씬 에셋은 삭제된 상태였고, 플레이 가능한 전투 씬 완성도와 문서 표기가 어긋나 있었다.
- 현재는 새 hand-authored `GameScene.unity`와 최소 `BattleEntity.prefab`을 다시 만들었고, build settings 등록과 required-field audit까지 복구했다.
- `CodexLobbyScene -> GameScene` end-to-end summon smoke는 통과했다.
- click summon 기준 `GameScene` wave/core/victory loop는 다시 통과했고, 현재 남은 핵심 리스크는 placement drag/drop 자동화와 멀티플레이 동기화 smoke다.

## 현재 포커스

- `GameScene` placement drag/drop automation contract와 multiplayer sync smoke 마감
- 계정/Garage WebGL 실기 검증과 설정 동기화 마감
- Lobby/Garage 시각 polish와 상호작용 smoke 보강
- Stitch contract-first -> prefab-first reset 루프 정착

## Phase 진행률

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Setup, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | ✅ 완료 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (드래그+클릭), 로테이션 |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy -> Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | ✅ 완료 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster, BattleEntity late-join HP sync |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀, Firebase Analytics |
| Phase 7: 배치 시스템 완성 | ✅ 완료 | PlacementArea, PlacementAreaView, 드래그 피드백, 영역 검증, MaterialFactory, ErrorView |
| Phase 8: Energy 재생 증가 곡선 | ✅ 완료 | EnergyRegenCurve (시간 기반 60s->180s, 3->5/s), EnergyRegenCurveConfig, TickRegen wiring |
| Phase 9: 네트워크 완성 | ✅ 완료 | BattleEntityPhotonController (IPunObservable HP/pos/dead sync), BattleEntityDespawnAdapter, WaveEndView 통계 |
| Phase 10: 계정 시스템 | 🟨 복구 진행 중 | Firestore/Garage 저장·로드·삭제·재시도 핵심 경로는 연결됐고, 남은 과제는 WebGL 실기 검증과 설정 동기화, 계정 UX 마무리 |
| Phase 11: Google 로그인 | 🟨 실동작 검증 전 | Google linking 경로 코드는 존재하지만 UID 유지와 WebGL 실기 동작은 아직 검증되지 않음 |

## 미완료 TODO

- `GameScene` rebuild: placement area drag/drop, wave start, core victory/defeat loop 검증
- `GameScene` rebuild: 멀티플레이 smoke로 late-join, BattleEntity sync, Energy sync 확인
- Phase 9: 실제 멀티플레이어 smoke 테스트 (late-join, BattleEntity sync, Energy sync)
- Phase 10: Firebase Console 설정 (API Key, Project ID, Firestore DB 생성)
- Phase 10: 설정 Firestore 동기화 마무리 (저장 UI, language 소비 경로)
- Phase 10: WebGL 빌드 smoke 테스트
- Phase 10: Garage save/load WebGL 실기 확인 후속 1회 더 재현
- Phase 10: Garage 수동 저장 UX 2차 폴리시 (슬롯 카드/결과 패널/계정 카드 완성도)
- Phase 11: WebGL 빌드에서 Google 로그인 실기 테스트
- Phase 11: 익명->Google 계정 linking 시 UID 유지 확인
- Phase 11: Google 로그인 WebGL smoke 테스트

## 다음 작업

- `docs/index.md`에서 현재 owner 경로를 해석한 뒤 lane별 SSOT를 읽는다.
- Unity UI/UX 작업은 시작 전에 owner doc `ops.unity-ui-authoring-workflow`를 먼저 읽고, 종료 전 `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`로 route/evidence freshness를 확인한다.
- `GameScene` 쪽은 placement drag/drop automation contract와 multiplayer sync smoke를 우선 마감한다.
- Lobby/Garage 쪽은 mobile-first Garage 단일 구조의 시각 밀도와 Garage save dock first-screen visibility를 계속 sanity check 한다.
- 외부 디자인 시안은 `Stitch`를 기본 생성 도구로 두고, 실제 반영은 Unity MCP와 scene/prefab contract 기준으로 번역한다.

## 상세 이력

- dated change log와 이전 구현 메모는 [`progress_changelog.md`](./progress_changelog.md) 에서 본다.
- 자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
- 계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
