# 진행 상황 (Game Scene Entry)

> **마지막 업데이트**: 2026-04-08

## Phase 진행률

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Bootstrap, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | 🟨 기본 구축 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (3슬롯) |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy → Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | 🟨 일부 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀 |

## 미완료 TODO

- UnitSlotView 드래그 앤 드롭 배치 (현재 클릭만)
- UnitSlotsContainer 로테이션 완성 (6개 중 3개 표시 전환)
- Phase 5 완성 — Late-join, BattleEntity 상태 동기화
- UnitCatalog에서 실제 스펙 조회 (`CreateTemporaryUnitSpec()` 대체)
- WaveEndView → 실제 Lobby 씬 전환 (현재 `PhotonNetwork.LeaveRoom()`만)
- Firebase Analytics 연동

## 최근 변경 사항

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
- **Agent 문서 5개 삭제**: game_design.md, state_ownership.md, initialization_order.md, firebase_hosting.md, webgl_optimization.md

## 전체 로드맵

자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
