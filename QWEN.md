## Qwen Added Memories

### Phase 6 완료 (게임 종료)
**이전 세션에서 완료됨**: Phase 0, 1, 2, 3, 4 (컴파일 수정, Unit/Garage 통합, 소환 시스템, Wave/Enemy 연결, 재소환)
**Phase 5 진행 중**: 네트워크 동기화 일부 구현 (Energy 변경, IPlayerSpecProvider, GetLocalPlayerRoster)
**Phase 6 완료됨**: 게임 종료

#### Phase 6 완료 요약:
1. **GameEndEvent 재설계** — `IsVictory`, `ReachedWave`, `PlayTimeSeconds`, `SummonCount`, `UnitKillCount` 필드
2. **GameEndEventHandler** — `GameEndEvent` 구독으로 변경 (기존 `PlayerDiedEvent` 구독 제거)
3. **GameEndAnalytics** 신규 — 소환/처치 카운팅, 플레이 시간 측정, 게임 종료 시 통계 로그
4. **WaveEndView 개선** — 통계 텍스트, Lobby 복귀 버튼 (`PhotonNetwork.LeaveRoom()`)
5. **GameSceneRoot 연결** — `GameEndEventHandler` + `GameEndAnalytics` (PvE/PvP 모두)

#### 신규 파일:
- `Assets/Scripts/Features/Player/Application/GameEndAnalytics.cs`

#### 수정된 파일:
- `Assets/Scripts/Features/Player/Application/Events/GameEndEvent.cs` — 통계 필드로 재설계
- `Assets/Scripts/Features/Player/Application/GameEndEventHandler.cs` — GameEndEvent 구독으로 변경
- `Assets/Scripts/Features/Wave/Presentation/WaveEndView.cs` — 통계 + Lobby 복귀 버튼 추가
- `Assets/Scripts/Features/Player/GameSceneRoot.cs` — GameEnd 핸들러/애널리틱스 연결

#### 문서 정리:
- `docs/design/port_ownership.md` → `docs/design/architecture-diagram.md` 통합 (Mermaid 의존성 그래프)
- 6개 Feature README 링크 업데이트

#### 전체 진행률:
| Phase | 상태 |
|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 |
| Phase 1: GameScene 초기화 | ✅ 완료 |
| Phase 2: 소환 시스템 | 🟨 기본 구축 |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 |
| Phase 4: 재소환 시스템 | ✅ 완료 |
| Phase 5: 네트워크 동기화 | 🟨 일부 (Energy, SpecProvider, Roster 복원) |
| Phase 6: 게임 종료 | ✅ 완료 |

#### 미완료/향후 작업 (TODO):
- UnitSlotView 배치 영역 선택 (드래그 앤 드롭) — 현재 클릭 소환만
- UnitSlotsContainer 로테이션 완성 (6개 중 3개 표시 전환)
- 네트워크 재소환 (Phase 5) — Late-join, BattleEntity 상태 동기화
- `CreateTemporaryUnitSpec()` — UnitCatalog에서 실제 스펙 조회 필요
- WaveEndView → 실제 Lobby 씬 전환 (현재 `PhotonNetwork.LeaveRoom()`만)
- Firebase Analytics 연동

**다음 세션 시작점**: Phase 5 완성 — BattleEntity 상태 동기화, Late-join 처리

시작 시 참조 문서:
- `docs/plans/game_scene_entry_plan.md` — 전체 로드맵
- `docs/design/architecture-diagram.md` — 아키텍처 + 포트 소유권 패턴
- `docs/design/game_design.md` — 게임 디자인 SSOT
