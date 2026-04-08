# Combat Feature

대상 방어력 기반 데미지 계산과 데미지 적용 이벤트 발행.

## 먼저 읽을 규칙

- 전역 구조, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- 크로스 피처 포트 소유권: [port_ownership.md](../../../../docs/design/port_ownership.md)
- 이벤트 체인 방향: [event_rules.md](../../../../agent/event_rules.md)

## 씬 계약 (JG_GameScene)

### 필수 Inspector 참조 (CombatBootstrap)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_targetAdapter` | `CombatTargetAdapter` | 데미지 타깃 관리 |
| `_targetViews` | `CombatTargetView[]` | 피격 시각 피드백 |
| `_friendlyFireFeedbackView` | `FriendlyFireFeedbackView` | 아군 피격 경고 (선택) |

### DamageNumberSpawner

| 필드 | 타입 | 용도 |
|---|---|---|
| `damageNumberPrefab` | `GameObject` | 대미지 숫자 프리팹 |

`GameSceneRoot/CombatSystems` 아래 배치, 선택 연결.

### 초기화 순서

```
1. CombatBootstrap.Initialize(eventBus, networkPort, localAuthorityId, affiliationPort, ffScaling?)
2. GameSceneRoot: _coreObjective.RegisterCombatTarget(_combatBootstrap)
3. GameSceneRoot: ConnectPlayer() → _combatBootstrap.RegisterTarget(playerId, provider)
4. GameSceneRoot: _damageNumberSpawner.Initialize(eventBus)
```

### 등록 대상

| 대상 | 제공자 | 등록 시점 |
|---|---|---|
| Player | PlayerSetup.CombatTargetProvider | ConnectPlayer() |
| Enemy | EnemySetup | EnemySpawnAdapter.SpawnEnemy() |
| ObjectiveCore | CoreObjectiveBootstrap | WaveBootstrap.Initialize() |
| BattleEntity | BattleEntityPrefabSetup | SummonPhotonAdapter.SpawnBattleEntity() |

### Late-join / Reconnect

- `EntityAffiliationAdapter`: ID 프리픽스 기반 순수 계산 — 상태 의존 없음
- `FriendlyFireScalingAdapter`: `WaveHydratedEvent` 구독으로 late-join 시 올바른 웨이브 인덱스 수신
