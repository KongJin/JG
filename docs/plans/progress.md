# 진행 상황 (Game Scene Entry)

> **마지막 업데이트**: 2026-04-11

## Phase 진행률

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Bootstrap, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | ✅ 완료 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (드래그+클릭), 로테이션 |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy → Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | ✅ 완료 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster, BattleEntity late-join HP sync |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀, Firebase Analytics |
| Phase 7: 배치 시스템 완성 | ✅ 완료 | PlacementArea, PlacementAreaView, 드래그 피드백, 영역 검증, MaterialFactory, ErrorView |
| Phase 8: Energy 재생 증가 곡선 | ✅ 완료 | EnergyRegenCurve (시간 기반 60s→180s, 3→5/s), EnergyRegenCurveConfig, TickRegen wiring |
| Phase 9: 네트워크 완성 | 🟨 대부분 완료 | BattleEntityPhotonController (IPunObservable HP/pos/dead sync), BattleEntityDespawnAdapter, WaveEndView 통계, GameEndAnalytics |

## 미완료 TODO

- Phase 9: 명시적 `[PunRPC]` 기반 BattleEntity remote 사망 동기화 (현재 PUN 자동 전파에 의존)
- Unity Inspector wiring 검증: `PlacementAreaView`, `DragGhostPrefab` 등 직렬화 참조 할당 확인
- 실제 멀티플레이어 smoke 테스트: late-join, BattleEntity sync, Energy sync

## 다음 작업 메모

- `CodexLobbyScene`에 `Garage`를 별도 씬이 아닌 상단 탭 페이지로 구현했다.
- 현재 범위는 `Garage UI + 자동 저장 + Ready 연동`이며, 기준 편성은 `6편성 / 최소 3기 Ready`다.
- `Clear`로 최소 편성 수를 깨는 경우에만 Ready를 해제하고, 무효 조합은 슬롯에 확정 저장하지 않는다.
- 1차 UI 분리 작업은 워킹트리 기준으로 `GaragePageController + GarageRosterListView + GarageUnitEditorView + GarageResultPanelView + viewmodel/presenter/state` 구조까지 반영됐다.
- `GarageSetup`, `CodexLobbyScene`, `CodexLobbyGarageAugmenter`, `Garage/ Lobby README`도 새 scene contract 기준으로 함께 갱신 중이다.
- 현재 compile gate의 직접 블로커는 `GaragePageController` / `GarageDraftEvaluation`에서 `Result<Unit>`를 nullable처럼 다루는 코드다.
- 전체 `dotnet build ProjectSD.slnx`는 위 Garage compile error 외에도 Photon demo csproj의 누락 소스 파일 때문에 계속 실패한다.
- 다음 확인 포인트는 2차 리팩터링에서 compile blocker를 먼저 제거하고, 실제 플레이 테스트에서 Garage 탭 전환, 자동 저장, Ready auto-cancel 동작을 함께 검증하는 것이다.
- 상세 계획은 [`codex_lobby_garage_panel_plan.md`](./codex_lobby_garage_panel_plan.md) 참고.
- 해당 리팩터링 상세는 [`codex_lobby_garage_ui_refactor_plan.md`](./codex_lobby_garage_ui_refactor_plan.md) 참고.
- 2차 리팩터링의 핵심은 새 Presentation 구조를 안정화하고, scene wiring / 탭 복귀 / Ready interlock 검증을 마친 뒤 SSOT 문서를 마감 상태로 정리하는 것이다.

### 최근 변경 사항

### 2026-04-11

- working tree: Garage UI Presentation을 `GaragePageController + GarageRosterListView + GarageUnitEditorView + GarageResultPanelView + presenter/state/viewmodel` 구조로 분리
- working tree: `GarageSetup`, `LobbyView`, `CodexLobbyScene`, `CodexLobbyGarageAugmenter`, `Garage/ Lobby README`를 새 3분할 Garage scene contract에 맞춰 갱신 중
- done: `GaragePageController`, `GarageDraftEvaluation`의 `Result<Unit>` nullable 가정 제거, `Assembly-CSharp.csproj` compile 통과
- done: Garage editor가 committed slot과 unsaved draft를 더 분명히 드러내도록 subtitle/clear-state 정리
- done: `RoomDetailView`가 재초기화 중복 버튼 구독과 stale local ready 상태를 남기지 않도록 보강
- next: 실제 Inspector wiring 확인, Garage 탭 왕복 smoke test, Ready interlock 플레이 검증 필요

### 2026-04-10

| 커밋 | 내용 |
|---|---|
| `b01947f` | chore: gate rule harness on feature dependency dag |
| `1ccab5e` | chore: expand rule harness code fix recipes |
| `ec3be02` | chore: strengthen rule harness feedback loop |
| `4dcd9e4` | fix: rename scenes (JG_ prefix removed), fix compile errors, update validator |
| `54447f2` | docs(rules): unify Korean/English mixed language in agent/*.md |
| `b897879` | fix(zone): move ZoneStatusPayload to Domain layer, break layer violation |
| `d295479` | fix(zone): break Status->Player->Zone->Status cycle with ZoneStatusPayload |
| `9445434` | feat(phase9): complete network sync — stats UI and drag ghost prefab |

### 2026-04-09

| 커밋 | 내용 |
|---|---|
| `2540305` | feat(analytics): add LogGameResult for Firebase game outcome tracking |
| `733a376` | feat(phase5): BattleEntity late-join state sync via instantiation data |
| `d53e90a` | feat(unit): replace CreateTemporaryUnitSpec with real UnitSpec, add Lobby scene transition |
| `9655638` | feat(unit): fix compilation errors, add drag-drop and rotation support |

### 2026-04-08

| 커밋 | 내용 |
|---|---|
| `7e72929` | docs: update QWEN.md with Phase 6 completion |
| `9548e5c` | docs: add game scene entry progress to CLAUDE.md (SSOT) |
| `4a48f7f` | feat(phase6): add game end handling with stats analytics and lobby return |
| `7759b38` | chore: add ProjectileSetup.meta, new test directories, harness memory, .qwen |
| `e869c85` | refactor(lobby): minor cleanup, remove obsolete agent docs |
| `a344a33` | feat(unit): add summon system, unit slot UI, ComputePlayerUnitSpecsUseCase |
| `6cb2131` | refactor(player): replace Mana with Energy, add IPlayerSpecProvider, split InitializeLocal/Remote |
| `1949a98` | refactor(garage): add GetLocalPlayerRoster, expand unit slots to 6 |
| `754cde4` | docs: consolidate port ownership into architecture-diagram.md |

### 주요 변경 요약
- **포트 소유권**: `port_ownership.md` → `architecture-diagram.md` 통합 (Mermaid 의존성 그래프)
- **Player**: Mana → Energy 변경, IPlayerSpecProvider 도입, InitializeLocal/Remote 분리
- **Garage**: 슬롯 5→6 확장, GetLocalPlayerRoster, RestoreGarageRosterUseCase
- **Unit**: 소환 시스템, 슬롯 UI, ComputePlayerUnitSpecsUseCase
- **Wave**: Victory/Defeat 통계, Lobby 복귀 버튼
- **문서 정리**: obsolete agent 문서 일부 제거와 규칙 문서 참조 정리

## 전체 로드맵

자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
