# /agent/initialization_order.md

## 피처 초기화 순서 규칙

### 원칙

피처 간 초기화 순서는 **Bootstrap 코드 순서에 암묵적으로 의존하면 안 된다.**
의존 관계가 있으면 이 문서와 해당 피처 README에 명시한다.

---

### 현재 GameScene 초기화 순서 (GameSceneBootstrap)

```
1. Analytics         — FirebaseAnalyticsAdapter, GameAnalyticsEventHandler
2. Player Lookup     — PlayerLookupAdapter, SceneErrorPresenter
3. Local Player Spawn — PhotonNetwork.Instantiate
4. Status            — StatusSetup.Initialize  ← Player보다 먼저 (SpeedModifier 의존)
5. Player            — PlayerSetup.Initialize (Status.SpeedModifier 주입)
6. Combat            — CombatBootstrap.Initialize (localPlayer 주입)
7. UI & Subsystems   — ManaRegenTicker, BleedoutTicker, RescueChannelTicker,
                       InvulnerabilityTicker, SoundPlayer, SkillSetup,
                       ProjectileSpawner, ZoneSetup
8. Wave (Optional)   — WaveBootstrap.Initialize (PvE 모드만)
9. Remote Players    — _remotePlayerWiringReady = true, 큐 처리
```

---

### 의존 관계 맵

| 피처 | 선행 조건 | 이유 |
|------|-----------|------|
| Status | (없음) | SpeedModifier는 독립 생성 가능 |
| Player | Status | SpeedModifier를 PlayerSetup에 주입 |
| Combat | Player | localPlayer를 CombatBootstrap에 주입 |
| Skill | Combat | 타겟팅 시스템 필요 |
| Wave | Player, Combat | 플레이어 등록 + 전투 시스템 필요 |

---

### 새 피처 추가 시 체크리스트

1. **의존하는 피처를 식별한다.** 이 피처가 초기화 시점에 다른 피처의 인스턴스나 포트가 필요한가?
2. **위 의존 관계 맵에 추가한다.** 선행 조건과 이유를 명시한다.
3. **GameSceneBootstrap 코드 순서를 맞춘다.** 선행 피처 초기화 이후에 배치한다.
4. **피처 README에 초기화 순서 요구사항을 기록한다.**

---

### Remote Player 초기화

리모트 플레이어는 로컬 초기화 완료 후(`_remotePlayerWiringReady = true`) 처리된다.

순서:
1. `ConnectPlayer(setup)` 호출
2. `PlayerSceneRegistry.TryRegister(setup)` — lookup 가능 상태
3. HUD, 타겟 등록
4. `HydrateFromProperties()` — CustomProperties에서 상태 복구

**핵심:** Registry 등록이 Hydrate보다 먼저여야 한다. `PlayerNetworkEventHandler`가 `IPlayerLookupPort.Resolve()`로 도메인 객체를 찾기 때문이다.

---

### 금지 사항

- Bootstrap 초기화 순서를 코드 순서만으로 보장하고 문서화하지 않는 것
- 초기화 시점에 아직 생성되지 않은 피처의 인스턴스를 참조하는 것
- Remote player 처리를 local 초기화 완료 전에 시작하는 것
