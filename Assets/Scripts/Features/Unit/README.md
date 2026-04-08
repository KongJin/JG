# Unit Feature

유닛 스펙 정의, 조합 검증, 비용 계산, 전장 소환 및 BattleEntity 생명주기.

## 먼저 읽을 규칙

- 전역 구조, 레이어, 포트 위치: [architecture.md](../../../../agent/architecture.md)
- 포트 소유권 패턴: [architecture-diagram.md](../../../../docs/design/architecture-diagram.md#포트-소유권-패턴)
- 게임 디자인 SSOT: [game_design.md](../../../../docs/design/game_design.md)

## 씬 계약 (GameScene)

### 필수 Inspector 참조 (UnitBootstrap)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_moduleCatalog` | `ModuleCatalog` | 유닛/모듈 SO 카탈로그 |
| `_summonAdapter` | `SummonPhotonAdapter` | BattleEntity Photon instantiate |

### 필수 Inspector 참조 (BattleEntityPrefabSetup — BattleEntity 프리팹)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_view` | `BattleEntityView` | 시각 표현 |
| `_photonController` | `BattleEntityPhotonController` | Owner 기반 RPC 동기화 |
| `_entityIdHolder` | `EntityIdHolder` | DomainEntityId 식별 |

### 런타임 생성 오브젝트

- `BattleEntity` — `SummonPhotonAdapter.SpawnBattleEntity()`가 `PhotonNetwork.Instantiate`로 생성
- `BattleEntityPrefabSetup`가 `OnPhotonInstantiate` → `Initialize()`로 도메인 객체 생성, Combat 등록, 위치 등록

### 초기화 순서

```
1. Lobby: UnitBootstrap.Initialize(eventBus)
2. GameScene: UnitBootstrap.InitializeBattleEntity(eventBus, energyPort, combatBootstrap, unitPositionQuery)
3. SummonPhotonAdapter.Initialize(eventBus, combatBootstrap, unitPositionQuery)
4. BattleEntitySetup.Initialize() — SummonUnitUseCase + UnitDeathEventHandler 조립
```

### Late-join / Reconnect

- late-join 시 GarageRoster 복원 → `ComputePlayerUnitSpecsUseCase`로 Unit[] specs 재계산
- 기존 BattleEntity들은 `BattleEntityPhotonController.OnPhotonSerializeView`로 HP/사망 상태 수신 → `SetHpFromNetwork()` 적용
