# Wave Feature

웨이브 기반 PvE 한 판 루프, 목표 코어, 보상 선택, 웨이브 상태 동기화.

## 먼저 읽을 규칙

- 전역 구조, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- 크로스 피처 포트 소유권: [port_ownership.md](../../../../docs/design/port_ownership.md)

## 씬 계약 (JG_GameScene)

### 하이어라키 배치

- `WaveSystems` GO: `WaveBootstrap`, `EnemySpawnAdapter`, `PlayerPositionQueryAdapter`, `UnitPositionQueryAdapter`, `WaveFlowController`, `WaveNetworkAdapter`
- `WorldRoot/ObjectiveCore`: `CoreObjectiveBootstrap`

### 필수 Inspector 참조 (WaveBootstrap)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_waveTable` | `WaveTableData` | 웨이브별 적 스폰 데이터 |
| `_spawnAdapter` | `EnemySpawnAdapter` | 적 스폰 실행 |
| `_playerPositionQuery` | `PlayerPositionQueryAdapter` | 플레이어 위치 조회 |
| `_unitPositionQuery` | `UnitPositionQueryAdapter` | BattleEntity 위치 조회 (Phase 3) |
| `_hudView` | `WaveHudView` | 웨이브 카운트다운 HUD |
| `_endView` | `WaveEndView` | Victory/Defeat 패널 |
| `_flowController` | `WaveFlowController` | 웨이브 루프 제어 |
| `_networkAdapter` | `WaveNetworkAdapter` | 상태 네트워크 동기화 |
| `_coreHealthView` | `CoreHealthHudView` | 코어 HP 바 |

### 초기화 순서

```
1. GameSceneRoot: _combatBootstrap.Initialize()
2. _coreObjective.RegisterCombatTarget(_combatBootstrap)
3. _waveBootstrap.Initialize(eventBus, combatBootstrap, localPlayerId, coreObjectiveQuery)
4. Master: TryStartGame() → GameStartEvent → 첫 웨이브 카운트다운
```

### Late-join / Reconnect

- `WaveBootstrap.Initialize()` 마지막에 `HydrateFromRoomProperties()`로 현재 waveIndex/waveState/countdownEnd 복원
- Countdown 상태는 `countdownEnd - ServerTimestamp` 차이로 남은 시간 계산
- Victory/Defeat terminal state는 `WaveEndView`가 `WaveHydratedEvent`로 복원
