# Player Feature

Player 피처는 플레이어 스폰, 이동, 생존 상태, 마나, 네트워크 복구를 담당한다.

## 먼저 읽을 규칙

- 전역 구조, 레이어, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- Bootstrap 책임, runtime lookup 예외, static event 예외: [anti_patterns.md](../../../../agent/anti_patterns.md)
- 게임 씬 전역 초기화 순서와 remote wiring 전제: [initialization_order.md](../../../../agent/initialization_order.md)
- Player `CustomProperties` 소유권과 동기화 채널: [state_ownership.md](../../../../agent/state_ownership.md)

## 이 피처의 책임

- 플레이어 스폰 (Photon Instantiate)
- 로컬 입력 → 이동/점프 처리
- 위치/회전 네트워크 동기화
- 로컬/원격 플레이어 분기 초기화
- Combat Feature의 데미지 파이프라인 참여 (ICombatTargetProvider 구현)
- 플레이어 HP 이벤트 발행 (PlayerHealthChangedEvent, PlayerDownedEvent, PlayerDiedEvent)
- 다운/구조 시스템: Alive→Downed→Dead 3상태, 10초 bleedout, 1.5초 채널링 구조, 50% HP/Mana 복귀 + 2초 무적
- 마나 리소스 관리 (ManaAdapter → IManaPort 구현)
- 마나 리젠 틱 (ManaRegenTicker, Presentation thin shell)
- 마나 UI (ManaBarView, 로컬 플레이어 전용 스크린 HUD)

## 로컬 계약

- `GameSceneRoot`는 로컬/원격 구분 없이 하나의 `ConnectPlayer()` 경로로 플레이어를 연결한다.
- remote player wiring은 Phase A 완료 후에만 시작하며, registry 등록이 hydrate보다 먼저 와야 한다.
- HP, Mana, LifeState는 Player가 쓰는 authoritative `CustomProperties`이며 late-join 복구도 이 경로를 따른다.

## 핵심 흐름

### 로컬 플레이어

```
PlayerInputHandler (InputSystem)
  → PlayerUseCases.Move(player, input, deltaTime)
    → Player.CalculateMovement (도메인 물리)
    → IPlayerMotorPort.Move (CharacterController 이동)
    → Player.ApplyMovement (상태 갱신)

  → PlayerUseCases.Jump(player)
    → Player.TryJump (지면 판정)
    → IPlayerNetworkCommandPort.SendJump (RPC)
```

### 원격 플레이어

```
PlayerNetworkAdapter.OnPhotonSerializeView (위치/회전 수신)
  → Update()에서 Lerp 보간

PlayerNetworkAdapter.RPC_Jump (점프 수신)
  → PlayerNetworkEventHandler → PlayerJumpedEvent 발행

PlayerNetworkAdapter.RPC_ApplyDamage (데미지 수신)
  → PlayerNetworkEventHandler → Combat.DamageReplicatedEvent 발행
    → CombatReplicationEventHandler → DamageAppliedEvent
      → PlayerDamageEventHandler → PlayerHealthChangedEvent / PlayerDownedEvent / PlayerDiedEvent

PlayerNetworkAdapter.RPC_PlayerRespawn (리스폰 수신)
  → PlayerNetworkEventHandler.HandleRemoteRespawned()
    → Player.Respawn() (도메인 상태 리셋)
    → PlayerRespawnedEvent 발행 → HUD 갱신

PlayerNetworkAdapter.RPC_Rescue (구조 수신, RpcTarget.All)
  → PlayerNetworkEventHandler.HandleRemoteRescued()
    → IPlayerLookupPort.Resolve(targetId)로 구조 대상 도메인 플레이어 lookup
    → Player.Rescue(hp, mana) (50% HP/Mana 복귀 + 2초 무적)
    → PlayerRescuedEvent + PlayerHealthChangedEvent + PlayerManaChangedEvent 발행

PlayerNetworkAdapter.OnPlayerPropertiesUpdate (LifeState CustomProperties 수신)
  → PlayerNetworkEventHandler.HandleLifeStateSynced()
    → IPlayerLookupPort.Resolve(targetId)로 도메인 플레이어 lookup
    → Downed: Player.ForceDowned() + PlayerDownedEvent 발행
    → Dead: Player.Die() + PlayerDiedEvent 발행
    → Alive: Player.Respawn() + PlayerRespawnedEvent + HP/Mana 이벤트 발행 (late-join 복구)

PlayerNetworkAdapter.RPC_RescueChannelStart / RPC_RescueChannelCancel
  → RescueChannelStartedEvent / RescueChannelCancelledEvent 발행
```

## 네트워크 동기화

| 데이터 | 방식 | 용도 |
|---|---|---|
| 위치, 회전 | `OnPhotonSerializeView` (연속 데이터) | 매 프레임 보간 |
| 점프 | `RPC` (이산 이벤트) | 점프 모션 트리거 |
| 데미지 | `RPC` (이산 이벤트) | 원격 데미지 적용 |
| 리스폰 | `RPC` (이산 이벤트) | 원격 HP 리셋 |
| 구조 | `RPC` (이산 이벤트, `RpcTarget.All`) | 로컬/원격 모두 동일 경로로 구조 적용 |
| 구조 채널링 시작/취소 | `RPC` (이산 이벤트) | 구조 진행 UI 동기화 |
| HP | `CustomProperties` (상태 동기화) | 원격 HP 바 표시, late-join 대응 |
| 마나 | `CustomProperties` (상태 동기화) | 프렌들리 파이어 판정용, late-join 대응 |
| LifeState | `CustomProperties` (상태 동기화) | 원격 다운/사망/생존 상태, late-join 대응 |

`PlayerNetworkAdapter`는 `IPunObservable` + `MonoBehaviourPunCallbacks`를 구��하며,
`IPlayerNetworkCommandPort`(송신)와 `IPlayerNetworkCallbackPort`(수신)을 모두 담당한다.
`OnPlayerPropertiesUpdate`를 통해 원격 플레이어의 HP/마나/LifeState CustomProperties 변경을 수신한다.

## Bootstrap 구조

두 클래스가 협력한다 (피처 루트에 위치):

- **GameSceneRoot** (`GameSceneRoot.cs`, 씬 루트 오브젝트): `PhotonNetwork.Instantiate`로 PlayerCharacter 프리팹 생성, `[Required, SerializeField]`로 연결된 `CameraFollower`를 플레이어에 부착하고 씬 공통 `SceneErrorPresenter`를 초기화한다.
- `GameSceneRoot`는 로컬/리모트 분기 없이 하나의 `ConnectPlayer()` 경로로 모든 플레이어를 연결한다 (Registry + HUD + CombatTarget + Wave 등록). HUD 프리팹에서 `GetComponent<PlayerHealthHudView>()`는 런타임 Instantiate 프리팹이라 Inspector wiring 불가 — Runtime Lookup Policy 허용 예외.
- 로컬 전용 씬 시스템(SkillSetup, BleedoutTicker, RescueChannelTicker, InvulnerabilityTicker, DownedOverlayView)은 `Start()`에서 로컬 플레이어 스폰 직후 1회 호출한다. **SoundPlayer**는 `JG_LobbyScene`에서 DDOL 단일 인스턴스로 생성되며, 게임 씬에서는 `SoundPlayer.Instance.Initialize(gameEventBus, localPlayerId)`로만 재바인딩한다(게임 씬에 `SoundPlayer` 직렬화 필드 없음).
- `JG_GameScene` 단독 실행은 지원 대상이 아니다. 이 경우 `SoundPlayer.Instance == null` 오류 로그만 남기고, 사운드 없이 나머지 초기화는 계속 진행한다.
- `DownedOverlayCanvas`는 **Screen Space - Overlay**, `Override Sorting=true`, `Sorting Order=600`으로 HUD 위에 렌더링한다. `OverlayRoot` / `RescueChannelGroup`는 `RectTransform` 기반 UI 컨테이너여야 하며, 기본 상태는 비활성이다(다운/구조 이벤트로만 표시).
- 로컬 플레이어가 완전히 사망(`PlayerDiedEvent`)하면 `DownedOverlayView`는 오버레이를 즉시 숨긴다. 다운 상태 UI와 웨이브 패배 화면이 동시에 겹치지 않게 하기 위함이다.
- Inspector 연결 필드는 `[Required, SerializeField]`로 선언해 씬/프리팹 저장 시 누락을 검증한다.
- `JG_GameScene` 하이어라키 계약:
  - 최상위 루트는 `GameSceneRoot`, `WorldRoot`, `UIRoot` 3개로 고정한다.
  - 루트 `GameSceneRoot` GO에는 씬 오케스트레이션용 `GameSceneRoot` + `PlayerSceneRegistry`만 둔다.
  - 자식 `WaveSystems` GO에는 `WaveBootstrap`, `EnemySpawnAdapter`, `PlayerPositionQueryAdapter`, `WaveFlowController`, `WaveNetworkAdapter`를 둔다.
  - 자식 `StatusSystems` GO에는 `StatusSetup`, `StatusTickController`를 둔다.
  - 자식 `PlayerSceneTickers` GO에는 `ManaRegenTicker`, `BleedoutTicker`, `RescueChannelTicker`, `InvulnerabilityTicker`를 둔다.
  - 자식 `CombatSystems` GO에는 `CombatBootstrap`, `DamageNumberSpawner`를 둔다.
  - 자식 `SkillWorldSystems` GO에는 `ProjectileSpawner`, `ZoneSetup`을 둔다.
  - `WorldRoot` 아래에는 `Main Camera`, `Directional Light`, `Plane`, `ObjectiveCore`, `CoreVisual`을 둔다.
  - `UIRoot` 아래에는 `Canvas`, `SkillBarCanvas`, `WaveHudCanvas`, `WaveEndCanvas`, `UpgradeSelectionCanvas`, `FFBannerCanvas`, `DownedOverlayCanvas`, `StartSkillSelectionCanvas`, `EventSystem`을 둔다.
  - `GameSceneRoot`의 직렬화 필드는 위 자식 GO의 컴포넌트를 직접 참조하며, 초기화 순서는 GO 위치가 아니라 `GameSceneRoot.Start()` 코드 순서를 따른다.
- `WaveBootstrap`은 optional 의존성이므로 `[SerializeField]`만 사용하고 `[Required]`를 붙이지 않는다. null이면 Wave 관련 로직을 스킵한다. **웨이브를 켠 경우** 씬에 `CoreObjectiveBootstrap`이 있어야 하며 `GameSceneRoot._coreObjective`에 연결한다. `CombatBootstrap.Initialize` 직후 `RegisterCombatTarget`(코어)을 호출한 뒤, 스킬 선택 완료 콜백에서 `WaveBootstrap.Initialize(..., coreObjectiveQuery)`를 호출한다.
- `DamageNumberSpawner`은 optional 의존성이므로 `[SerializeField]`만 사용한다. null이면 대미지 숫자 표시를 스킵한다. `CombatBootstrap.Initialize` 직후 `DamageNumberSpawner.Initialize(eventBus)`를 호출한다.
- 플레이어 식별자는 `PlayerNetworkAdapter.StablePlayerId`를 기준으로 로컬/원격 모두 동일하게 생성한다.
- `GameSceneRoot`는 `Awake()`에서 `PlayerSetup.RemoteArrived`를 구독하고, `Start()`에서 Combat/Skill/Zone/Wave 초기화가 끝난 뒤 대기 중인 원격 플레이어 연결을 drain한다.
- `PlayerSceneRegistry` (`PlayerSceneRegistry.cs`, Player feature 루트의 bootstrap 보조 MonoBehaviour)는 씬에 연결된 `PlayerSetup`을 추적해 HUD/Combat/Wave 중복 등록을 막는다.
- 원격 플레이어 연결은 `IPunInstantiateMagicCallback.OnPhotonInstantiate()` 기반이다. Photon이 원격 오브젝트를 실제로 생성한 시점에 `PlayerSetup.RemoteArrived`가 발행되며, 폴링/코루틴/씬 스캔은 사용하지 않는다.
- `PlayerSetup.RemoteArrived`는 static event이므로 `GameSceneRoot.OnDestroy()`에서 반드시 구독을 해제한다.
- **PlayerSetup** (`PlayerSetup.cs`, PlayerCharacter 프리팹): 스폰 후 `IsMine` 분기:
  - 로컬: PlayerNetworkEventHandler(IPlayerLookupPort) + PlayerUseCases(IPlayerLookupPort) (이벤트 구독으로 SyncLifeState 전송) + PlayerDamageEventHandler + BleedoutTracker + RescueChannelTracker + InvulnerabilityTracker + CombatNetworkPort + InputHandler (RescueChannelTracker 주입) + View 초기화
  - 원격: `PlayerUseCases.SpawnRemote()` (static factory)로 도메인 플레이어 생성 + PlayerNetworkEventHandler(IPlayerLookupPort) + PlayerDamageEventHandler + View 초기화, Input/Motor 비활성화. `HydrateFromProperties()`는 `GameSceneRoot.ConnectPlayer()`에서 레지스트리 등록 이후 호출한다 (IPlayerLookupPort.Resolve가 도메인 플레이어를 찾을 수 있도록). `PlayerView`는 리모트일 때 이벤트 구독만 스킵하고 컴포넌트 비활성화는 `PlayerSetup`이 담당한다.
  - `PlayerView._visualRoot`는 `PlayerCharacter.prefab`의 `Cube` 자식에 연결되며, 로컬 플레이어가 Downed일 때만 비활성화되어 다운드 오버레이 중앙을 가리지 않는다. 구조/리스폰 시 다시 활성화된다.
  - `PlayerHealthHudView`는 `ConnectPlayer()`에서 로컬/원격 모두 생성한다. 로컬 플레이어도 접촉 피해 변화를 즉시 읽을 수 있게 월드 HP HUD를 사용하며, 다운 상태에서는 `PlayerDownedEvent`로 HUD를 숨겨 다운드 오버레이와 겹치지 않게 한다.
  - `StatusNetworkAdapter` 프로퍼티를 노출하여 `GameSceneRoot`가 원격 플레이어의 Status RPC 콜백을 등록할 수 있다.
  - `DomainPlayer`, `BleedoutTrackerInstance`, `RescueChannelTrackerInstance`, `InvulnerabilityTrackerInstance` 프로퍼티를 노출하여 씬 레벨 틱커/UI 초기화에 전달한다.
  - `PlayerSetup`은 `DisposableScope`으로 `PlayerUseCases`, `PlayerDamageEventHandler`, `BleedoutTracker`, `RescueChannelTracker`, `InvulnerabilityTracker`의 EventBus 구독 수명을 관리한다. 원격 플레이어도 `DisposableScope`으로 `PlayerDamageEventHandler` 구독을 관리한다.

## 씬 공통 에러 UI

- `GameSceneRoot`는 시작 시 씬 `EventBus`를 만들고 `SceneErrorPresenter.Initialize(eventBus)`를 먼저 호출한다
- 룸에 연결되지 않은 상태로 게임 씬에 들어오면 `UiErrorRequestedEvent(Modal)`을 발행해 진행 불가 상태를 사용자에게 노출한다
- Inspector 미연결, null dependency 같은 프로그래밍 오류는 계속 `Debug.LogError`로만 남긴다

## 레이어 메모

- **Domain**: `Player` (마나 필드 포함: `MaxMana`, `CurrentMana`, `SpendMana()`, `RegenMana()`; LifeState: `Alive`/`Downed`/`Dead`, `TakeDamage()` → Downed 전이, `TickBleedout()`, `ForceDowned()` (late-join 상태 복원용), `Rescue()`, `TickInvulnerability()` (시간 경과 후 무적 해제), `Hydrate(float hp, float mana)` (late-join/CustomProperties 상태 복원용, HP/Mana를 도메인에 직접 반영), `Die()`), `PlayerSpec` (Defense, MaxMana, ManaRegenPerSecond 필드 포함), `MovementRule`, `LifeState`, `BleedoutRule` (Duration=10s), `RescueRule` (ChannelDuration=1.5s, HpPercent=50%, ManaPercent=50%, InvulnerabilityDuration=2s, MaxRange=3f)
- **Application**: `PlayerUseCases` (구조 메서드: `CompleteRescue()`는 rescuer alive + target downed 재검증 후 RPC 전송, `SpawnRemote()` static factory로 원격 플레이어 도메인 생성 통합, `FindRescueTarget()` 쿼리 포함, 이벤트 구독으로 SyncLifeState 네트워크 전송), `PlayerNetworkEventHandler` (Health/Mana CustomProperties 수신 시 `Player.Hydrate()`로 도메인 상태도 갱신, LifeState CustomProperties 수신 처리 — `LifeState.Alive`는 `Player.Respawn()` + 이벤트 발행으로 late-join 클라이언트의 다운/사망 상태 복구, `HandleRemoteRescued`는 target.IsDowned 가드 포함, `IPlayerLookupPort`로 targetId 기준 도메인 플레이어 lookup), `PlayerDamageEventHandler` (PlayerDownedEvent/PlayerDiedEvent 분기 발행, downed/death dedupe 플래그), `BleedoutTracker` (Downed 상태 틱, bleedout 만료 시 PlayerDiedEvent 발행), `RescueChannelTracker` (채널링 진행/완료/취소 + 이벤트 기반 자동 취소: rescuer downed/died 또는 target died/rescued 시 ForceCancel → 네트워크 취소 전송 + RescueChannelCancelledEvent 발행), `InvulnerabilityTracker` (Rescue 후 무적 시간 틱, 만료 시 자동 해제), `GameEndEventHandler`, `ManaAdapter` (`IManaPort` 구현, 마나 차감/리젠/네트워크 동기화), 이벤트(`PlayerMovedEvent`, `PlayerJumpedEvent`, `PlayerHealthChangedEvent`, `PlayerDiedEvent`, `PlayerDownedEvent`, `PlayerRescuedEvent`, `RescueChannelStartedEvent`, `RescueChannelCancelledEvent`, `PlayerRespawnedEvent`, `PlayerManaChangedEvent`, `GameEndEvent`), 포트(`IPlayerMotorPort`, `IPlayerNetworkCommandPort` (SyncLifeState, SendRescue, SendRescueChannelStart/Cancel 추가), `IPlayerNetworkCallbackPort` (OnLifeStateSynced, OnRemoteRescued, OnRemoteRescueChannelStarted/Cancelled 추가), `IPlayerLookupPort` (플레이어 ID → Domain.Player + 위치 조회), `ISpeedModifierPort`)
- **Infrastructure**: `PlayerMotorAdapter`, `PlayerNetworkAdapter` (`MonoBehaviourPunCallbacks`, HP/마나/LifeState CustomProperties 송수신, Rescue/RescueChannel RPC, `HydrateFromProperties()` late-join 상태 복원 (LifeState enum 범위 검증 포함), RPC 수신 시 파라미터 검증 — null/empty string, NaN/음수 damage, enum 범위 체크, optional attacker/killer ID는 `default(DomainEntityId)`로 전달), `PlayerCombatTargetProvider` (Combat의 `ICombatTargetProvider` 구현, Downed 상태에서 추가 피해 무시), `EntityAffiliationAdapter` (Combat의 `IEntityAffiliationPort` 구현, ID 프리픽스 기반 소속 판정), `PlayerLookupAdapter` (`IPlayerLookupPort` 구현, PlayerSceneRegistry 래핑)
- **Presentation**: `PlayerInputHandler` (Interact 입력으로 `PlayerUseCases.FindRescueTarget()` 호출하여 구조 시작/취소, `PlayerRespawnedEvent` 구독으로 respawn 시 입력 복구), `PlayerView` (리모트 컴포넌트 비활성화는 PlayerSetup이 담당, View는 이벤트 구독 분기만 수행), `PlayerHealthHudView`, `ManaBarView` (로컬 전용 스크린 HUD), `ManaRegenTicker` (thin Update shell → ManaAdapter.TickRegen), `BleedoutTicker` (thin Update shell → BleedoutTracker.Tick), `RescueChannelTicker` (thin Update shell → RescueChannelTracker.Tick → 완료 시 CompleteRescue), `InvulnerabilityTicker` (thin Update shell → InvulnerabilityTracker.Tick), `DownedOverlayView` (다운 시 bleedout 타이머바 + 구조 채널링 진행 바, BleedoutTracker.Elapsed / RescueChannelTracker.Elapsed를 직접 읽어 dual state 방지), `CameraFollower`
- **Bootstrap**: `GameSceneRoot` (씬 레벨, Photon Instantiate + 씬 wiring, `DisposableScope`으로 `GameAnalyticsEventHandler`/`GameEndEventHandler` EventBus 구독 수명 관리), `PlayerSetup` (프리팹 레벨, 로컬/원격 분기 초기화), `PlayerSceneRegistry` (씬 등록 보조)

## 도메인 물리

`Player` 엔티티가 이동 계산을 도메인 레벨에서 수행한다:
- `MovementRule`: 속도 선택 (걷기/달리기), 수평 이동 델타, 중력 적용
- `PlayerSpec`: walkSpeed, sprintMultiplier, jumpForce, gravity, defense
- 실제 CharacterController 이동은 `PlayerMotorAdapter`(Infrastructure)가 담당

## Combat 연동

- `PlayerCombatTargetProvider` (Infrastructure): Player 도메인을 Combat의 `ICombatTargetProvider`에 연결
- `PlayerCombatNetworkPortAdapter` (Infrastructure): Player 네트워크 송신을 Combat의 `ICombatNetworkCommandPort`에 연결
- `PlayerDamageEventHandler` (Application): `DamageAppliedEvent` 구독 → `PlayerHealthChangedEvent`/`PlayerDownedEvent`/`PlayerDiedEvent` 발행
- `GameEndEventHandler` (Application): `PlayerDiedEvent` 구독 → 승패 모달(`Victory!` / `Defeat!`) 발행
- `PlayerSetup`에서 `CombatTargetProvider`와 `CombatNetworkPort`를 생성하고, `GameSceneRoot`에서 `CombatBootstrap.RegisterTarget()`/`Initialize()`에 전달
- 로컬 플레이어가 다운되면 `PlayerInputHandler`가 입력을 비활성화한다 (구조 시 재활성화)

## 피처 간 의존

- **Skill**: `SkillSetup`과 `SkillNetworkAdapter`가 같은 PlayerCharacter 프리팹��� 부착됨. `IManaPort` 구현체(`ManaAdapter`)를 `PlayerSetup`에서 생성하여 `SkillSetup`에 주입. `ManaAdapter`는 외부 SDK 직접 사용 없이 순수 도메인 상태 조회/계산이므로 Application 레벨에 배치 (anti_patterns.md 예외 적용)
- **Combat**: `ICombatTargetProvider` (구현), `IEntityAffiliationPort` (구현), `DamageAppliedEvent` (구독)
- **Status**: `ISpeedModifierPort`를 통해 Haste/Slow 이동속도 수정 (선택적 의존, null이면 기본 속도 사용)
- **Shared**: EventBus, Float3, DomainEntityId, IClockPort, `UiErrorRequestedEvent`
