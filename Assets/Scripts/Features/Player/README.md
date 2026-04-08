# Player Feature

플레이어 스폰, 위치 동기화, 생존 상태, Energy 리소스 관리.

## 먼저 읽을 규칙

- 전역 구조, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- 크로스 피처 포트 소유권: [port_ownership.md](../../../../docs/design/port_ownership.md)
- 게임 디자인 SSOT: [game_design.md](../../../../docs/design/game_design.md)

> **기획 참고**: 플레이어는 직접 조작 아바타가 아닌 **차고 소유자, 전투 중 소환자, 빌드 책임자**로 정의됨. 기존 이동/점프 코드는 잔존하지만 신규 게임 루프에서 비활성 상태.

## 씬 계약 (JG_GameScene)

### 필수 Inspector 참조 (GameSceneRoot — Player 관련)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_playerPrefabName` | `string` | PlayerCharacter 프리팹 이름 |
| `_spawnRadius` | `float` | 스폰 반경 |
| `_cameraFollower` | `CameraFollower` | 카메라 추적 |
| `_healthHudPrefab` | `GameObject` | 월드 HP HUD 프리팹 |
| `_hudCanvas` | `Canvas` | HUD 캔버스 |
| `_energyRegenTicker` | `EnergyRegenTicker` | Energy 재생 틱 |
| `_energyBarView` | `EnergyBarView` | Energy UI 바 |
| `_localPlayerSetup` | `PlayerSetup` | 로컬 플레이어 셋업 |

### 하이어라키 배치

- `PlayerSceneTickers` GO: `EnergyRegenTicker`, `BleedoutTicker`, `RescueChannelTicker`, `InvulnerabilityTicker`
- `CombatSystems` GO: `CombatBootstrap`, `DamageNumberSpawner`

### 런타임 생성 오브젝트

- `PlayerCharacter` — `PhotonNetwork.Instantiate`로 생성 (로컬/원격 구분 없이 하나의 `ConnectPlayer()` 경로)
- `PlayerHealthHudView` — `ConnectPlayer()`에서 로컬/원격 모두 `Instantiate`

### 초기화 순서

```
1. EventBus 생성
2. PlayerCharacter PhotonNetwork.Instantiate
3. Status 초기화 (PlayerSetup 이전 — SpeedModifier 필요)
4. PlayerSetup.InitializeLocal() 또는 InitializeRemote()
5. ConnectPlayer() — Registry 등록 + HUD + CombatTarget + Wave 등록
6. Energy 시스템 초기화
7. 원격 플레이어 drain
```

### Late-join / Reconnect

- 원격 플레이어: `PlayerSetup.RemoteArrived` static event → `ConnectPlayer()` → `HydrateFromProperties()`
- HP/Energy/LifeState는 CustomProperties에서 복원, `Player.Hydrate()`로 도메인 상태 반영
