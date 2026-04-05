# /agent/initialization_order.md

## 피처 초기화 순서 규칙

이 문서는 **피처 간 전역 초기화 순서**의 SSOT다.
feature `README.md`는 이 문서의 순서를 다시 길게 복붙하지 말고, 자기 피처에서 필요한 로컬 전제와 예외만 적는다.

### 원칙

피처 간 초기화 순서는 **Bootstrap 코드 순서에 암묵적으로 의존하면 안 된다.**
의존 관계가 있으면 이 문서와 해당 피처 README에 명시한다.

---

### 현재 LobbyScene 초기화 순서 (LobbyBootstrap)

`EventBus`는 필드 초기화로 한 인스턴스가 존재한다. `SoundPlayer`는 **로비 씬 루트**에 두고 `Awake`에서 `DontDestroyOnLoad` + 단일 `Instance`로 등록된다.

```
1. SceneErrorPresenter.Initialize(eventBus)
2. EventBus Subscribe (SceneLoadRequestedEvent 등)
3. Analytics / Repository / LobbyNetworkEventHandler / LobbyUseCases
4. SoundPlayer.Initialize(eventBus, SoundPlayer.LobbyOwnerId)  ← LobbyView.Initialize 이전
5. LobbyView.Initialize
6. LobbyUpdatedEvent 발행
```

로비에서 발행하는 `SoundRequestEvent`는 규약상 **`PlaybackPolicy.All`만** 사용한다 (`LocalOnly` / `OwnerExcluded`는 게임 씬 재바인딩 이후).

---

### 현재 GameScene 초기화 순서 (GameSceneRoot)

초기화는 두 단계로 나뉜다:

#### Phase A (동기 — 선택 전)

```
1. Analytics         — FirebaseAnalyticsAdapter, GameAnalyticsEventHandler
2. Player Lookup     — PlayerLookupAdapter, SceneErrorPresenter
3. Local Player Spawn — PhotonNetwork.Instantiate
4. Status            — StatusSetup.Initialize  ← Player보다 먼저 (SpeedModifier 의존)
5. Player            — PlayerSetup.Initialize (Status.SpeedModifier 주입)
6. Combat            — CombatBootstrap.Initialize (localPlayer 주입)
6b. Objective core   — `CoreObjectiveBootstrap.RegisterCombatTarget` (웨이브 사용 시 `_coreObjective` 필수; `CombatBootstrap` 이후)
7. UI & Subsystems   — ManaRegenTicker, BleedoutTicker, RescueChannelTicker,
                       InvulnerabilityTicker, SoundPlayer.Instance.Initialize (DDOL, 로비에서 생성됨)
8. ProjectileSpawner — EventBus만 필요, 선택 전 초기화 (원격 스킬 이벤트 수신)
9. ZoneSetup         — EventBus만 필요, 선택 전 초기화 (원격 스킬 이벤트 수신)
10. Remote Players   — _remotePlayerWiringReady = true, 큐 처리
11. SkillSetup.InitializePreSelection — 카탈로그, UI, 네트워크 핸들러, 선택 UI 표시
```

Phase A 완료 시점: 모든 동기 초기화 끝남. Remote player wiring 가능.

#### Phase B (선택 후 콜백)

```
12. SkillSetup.InitializePostSelection — 덱 구성, 스킬 장착, SkillsReady 동기화
13. Wave (Optional)  — WaveBootstrap.Initialize (PvE 모드만, SkillReward + `ICoreObjectiveQuery` 필요)
14. Master: 전원 SkillsReady 확인 → GameStartEvent → 카운트다운 시작
```

Phase B는 `SkillSetup.InitializePreSelection`의 `onComplete` 콜백에서 시작된다.
플레이어가 시작 스킬 2개를 선택하면 `StartSkillSelectedEvent` → `StartSkillSelectionHandler` → `InitializePostSelection` → `onComplete` 순으로 호출된다.
`JG_GameScene`에서 Wave/Status/Ticker/Combat/SkillWorld 컴포넌트가 자식 GO로 분리되고, 최상위가 `WorldRoot` / `UIRoot`로 묶여 있어도 초기화 순서는 하이어라키 위치가 아니라 위 `GameSceneRoot.Start()` / 콜백 호출 순서를 따른다.

---

### 의존 관계 맵

| 피처 | 선행 조건 | 이유 |
|------|-----------|------|
| Status | (없음) | SpeedModifier는 독립 생성 가능 |
| Player | Status | SpeedModifier를 PlayerSetup에 주입 |
| Combat | Player | localPlayer를 CombatBootstrap에 주입 |
| Skill (PreSelection) | Combat | 네트워크 핸들러 초기화 필요 |
| Skill (PostSelection) | Skill (PreSelection) | 스킬 선택 완료 후 덱 구성 |
| Wave | Skill (PostSelection) + Combat + Core | SkillReward, SkillIcon 포트 필요; 적 AI/스폰에 `ICoreObjectiveQuery`; 코어는 Combat에 먼저 등록됨 |
| ProjectileSpawner | EventBus | 선택 전 초기화 (원격 스킬 이벤트 수신) |
| ZoneSetup | EventBus | 선택 전 초기화 (원격 스킬 이벤트 수신) |

---

### 새 피처 추가 시 체크리스트

1. **의존하는 피처를 식별한다.** 이 피처가 초기화 시점에 다른 피처의 인스턴스나 포트가 필요한가?
2. **위 의존 관계 맵에 추가한다.** 선행 조건과 이유를 명시한다.
3. **GameSceneRoot 코드 순서를 맞춘다.** 선행 피처 초기화 이후에 배치한다.
4. **피처 README에 초기화 순서 요구사항을 기록한다.**

---

### Remote Player 초기화

리모트 플레이어는 Phase A 완료 후(`_remotePlayerWiringReady = true`) 처리된다.
Phase B(스킬 선택) 이전에 wiring이 가능하므로, Status RPC 유실 없이 원격 플레이어가 연결된다.

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
- Remote player 처리를 Phase A 완료 전에 시작하는 것
