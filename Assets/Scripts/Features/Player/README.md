# Player Feature

플레이어 캐릭터의 스폰, 이동, 점프 및 네트워크 위치 동기화를 담당한다.

## 책임

- 플레이어 스폰 (Photon Instantiate)
- 로컬 입력 → 이동/점프 처리
- 위치/회전 네트워크 동기화
- 로컬/원격 플레이어 분기 초기화
- Combat Feature의 데미지 파이프라인 참여 (ICombatTargetProvider 구현)
- 플레이어 HP 이벤트 발행 (PlayerHealthChangedEvent, PlayerDiedEvent)

## 이벤트 흐름

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
      → PlayerDamageEventHandler → PlayerHealthChangedEvent / PlayerDiedEvent

PlayerNetworkAdapter.RPC_PlayerRespawn (리스폰 수신)
  → PlayerNetworkEventHandler.HandleRemoteRespawned()
    → Player.Respawn() (도메인 상태 리셋)
    → PlayerRespawnedEvent 발행 → HUD 갱신
```

## 네트워크 동기화

| 데이터 | 방식 | 용도 |
|---|---|---|
| 위치, 회전 | `OnPhotonSerializeView` (연속 데이터) | 매 프레임 보간 |
| 점프 | `RPC` (이산 이벤트) | 점프 모션 트리거 |
| 데미지 | `RPC` (이산 이벤트) | 원격 데미지 적용 |
| 리스폰 | `RPC` (이산 이벤트) | 원격 HP 리셋 |

`PlayerNetworkAdapter`는 `IPunObservable` + `MonoBehaviourPun`을 구현하며,
`IPlayerNetworkCommandPort`(송신)와 `IPlayerNetworkCallbackPort`(수신)을 모두 담당한다.

## Bootstrap 구조

두 클래스가 협력한다 (피처 루트에 위치):

- **GameSceneBootstrap** (`GameSceneBootstrap.cs`, 씬 오브젝트): `PhotonNetwork.Instantiate`로 PlayerCharacter 프리팹 생성, 카메라를 플레이어에 부착하고 씬 공통 `SceneErrorPresenter`를 초기화한다.
- `GameSceneBootstrap`은 로컬 플레이어 생성 후 `PlayerCombatNetworkPortAdapter`를 만들어 Combat에 주입한다.
- 플레이어 식별자는 `PlayerNetworkAdapter.StablePlayerId`를 기준으로 로컬/원격 모두 동일하게 생성한다.
- `ConnectRemotePlayerDelayed`는 최대 30프레임 재시도로 원격 플레이어의 PhotonView를 탐색한다. Photon이 기존 플레이어의 네트워크 오브젝트를 인스턴스화하는 데 수 프레임이 소요될 수 있기 때문이다.
- **PlayerSetup** (`PlayerSetup.cs`, PlayerCharacter 프리팹): 스폰 후 `IsMine` 분기:
  - 로컬: PlayerNetworkEventHandler + PlayerUseCases + PlayerDamageEventHandler + InputHandler + View 초기화
  - 원격: PlayerNetworkEventHandler(remotePlayer) + PlayerDamageEventHandler + View 초기화, Input/Motor 비활성화

## 씬 공통 에러 UI

- `GameSceneBootstrap`은 시작 시 씬 `EventBus`를 만들고 `SceneErrorPresenter.Initialize(eventBus)`를 먼저 호출한다
- 룸에 연결되지 않은 상태로 게임 씬에 들어오면 `UiErrorRequestedEvent(Modal)`을 발행해 진행 불가 상태를 사용자에게 노출한다
- Inspector 미연결, null dependency 같은 프로그래밍 오류는 계속 `Debug.LogError`로만 남긴다

## 레이어 메모

- **Domain**: `Player`, `PlayerSpec` (Defense 필드 포함), `MovementRule`
- **Application**: `PlayerUseCases`, `PlayerNetworkEventHandler`, `PlayerDamageEventHandler`, `GameEndEventHandler`, 이벤트(`PlayerMovedEvent`, `PlayerJumpedEvent`, `PlayerHealthChangedEvent`, `PlayerDiedEvent`, `PlayerRespawnedEvent`, `PlayerSpawnedEvent`, `GameEndEvent`), 포트(`IPlayerMotorPort`, `IPlayerNetworkCommandPort`, `IPlayerNetworkCallbackPort`)
- **Infrastructure**: `PlayerMotorAdapter`, `PlayerNetworkAdapter`, `PlayerCombatTargetProvider` (Combat의 `ICombatTargetProvider` 구현)
- **Presentation**: `PlayerInputHandler`, `PlayerView`, `PlayerHealthHudView`
- **Bootstrap**: `GameSceneBootstrap` (씬 레벨, Photon Instantiate), `PlayerSetup` (프리팹 레벨, 로컬/원격 분기 초기화)

## 도메인 물리

`Player` 엔티티가 이동 계산을 도메인 레벨에서 수행한다:
- `MovementRule`: 속도 선택 (걷기/달리기), 수평 이동 델타, 중력 적용
- `PlayerSpec`: walkSpeed, sprintMultiplier, jumpForce, gravity, defense
- 실제 CharacterController 이동은 `PlayerMotorAdapter`(Infrastructure)가 담당

## Combat 연동

- `PlayerCombatTargetProvider` (Infrastructure): Player 도메인을 Combat의 `ICombatTargetProvider`에 연결
- `PlayerCombatNetworkPortAdapter` (Infrastructure): Player 네트워크 송신을 Combat의 `ICombatNetworkCommandPort`에 연결
- `PlayerDamageEventHandler` (Application): `DamageAppliedEvent` 구독 → `PlayerHealthChangedEvent`/`PlayerDiedEvent` 발행
- `GameEndEventHandler` (Application): `PlayerDiedEvent` 구독 → 승패 모달(`Victory!` / `Defeat!`) 발행
- `PlayerSetup`에서 `CombatTargetProvider`를 생성하고, `GameSceneBootstrap`에서 `CombatBootstrap.RegisterTarget()`으로 등록
- 로컬 플레이어가 죽으면 `PlayerInputHandler`가 입력을 비활성화한다

## 피처 간 의존

- **Skill**: `SkillSetup`과 `SkillNetworkAdapter`가 같은 PlayerCharacter 프리팹에 부착됨
- **Combat**: `ICombatTargetProvider` (구현), `DamageAppliedEvent` (구독)
- **Shared**: EventBus, Float3, DomainEntityId, IClockPort, `UiErrorRequestedEvent`
