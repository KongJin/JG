# Wave Feature

웨이브 기반 PvE 한 판 루프를 담당한다.

## 현재 책임

- 웨이브 카운트다운, 시작, 클리어, 승리/패배 상태 관리
- 적 사망 / 플레이어 전멸 이벤트를 받아 웨이브 진행 전이
- Master에서만 적 스폰 수행
- 각 클라이언트에서 동일한 웨이브 상태를 로컬 이벤트로 재현해 HUD/결과 UI 갱신
- 비-Master 클라이언트에서 `EnemySetup.EnemyArrived` 콜백으로 원격 적 초기화 (Master는 `EnemySpawnAdapter`가 올바른 EnemyData로 명시적 초기화)
- **웨이브 클리어 시 스킬 3지선다**: 보상 풀(초기 덱에 포함되지 않은 스킬)에서 아직 덱에 없는 스킬 3개를 후보로 제시하고, 선택한 스킬을 덱 버린 더미에 추가한다. 이후 덱 순환(DeckCycleHandler)에 의해 자연스럽게 핸드에 등장한다. 보상 풀이 소진되면(모든 스킬 획득) 선택을 건너뛰고 바로 다음 웨이브 카운트다운을 시작한다.
- **스킬 획득 결과 표시**: 선택 후 "스킬명 획득!" 형태로 2초간 요약 표시

## 데이터 흐름

### 웨이브 시작

```text
GameSceneBootstrap (SkillSetup.onComplete 콜백)
  → WaveBootstrap.Initialize(eventBus, combatBootstrap, localPlayerId, skillReward, iconPort)
    → WaveLoopUseCase 생성 (ISkillRewardPort 주입)
    → WaveEventHandler 생성
    → SkillRewardHandler 생성
    → WaveFlowController.Initialize(waveLoop, waveTable, spawnAdapter)
      (자동 카운트다운 없음 — GameStartEvent 대기)
    → Master: TryStartGame() — 전원 SkillsReady 확인 후 GameStartEvent 발행
      → WaveFlowController.StartFirstWave()
        → WaveCountdownStartedEvent
```

WaveBootstrap은 이제 `MonoBehaviourPunCallbacks`를 상속하여 `OnPlayerPropertiesUpdate(Photon.Realtime.Player, Hashtable)`를 수신한다. Master는 모든 플레이어의 `skillsReady` CustomProperty가 `true`인지 확인한 후 `GameStartEvent`를 발행하여 첫 웨이브 카운트다운을 시작한다.

비-Master 클라이언트는 `GameStartEvent`를 직접 수신하지 않고, Master가 시작한 카운트다운의 Wave 상태 동기화(`SyncWaveState`)를 통해 따라간다. late-join 시에는 `HydrateFromRoomProperties()`로 현재 상태를 복원한다.

### 웨이브 진행

```text
WaveFlowController.Update()
  → Countdown 상태: WaveLoopUseCase.TickCountdown(deltaTime)
  → 카운트다운 종료 시 WaveLoopUseCase.BeginWave(enemyCount)
    → EnemySpawnAdapter.SpawnWaveEnemies(entry) — Master만 실제 스폰
  → Cleared 상태: UpgradeSelection 진입 → SkillSelectionRequestedEvent
  → SkillSelectedEvent 수신 시: 다음 웨이브 카운트다운 시작
```

### 스킬 보상 선택

```text
WaveClearedEvent
  → WaveFlowController.Update() — Cleared 감지
    → WaveLoopUseCase.EnterUpgradeSelection()
      → ISkillRewardPort.DrawCandidates(3) — 보상 풀에서 덱에 없는 스킬 3개 추출 (0개면 선택 스킵)
      → SkillSelectionRequestedEvent(waveIndex, candidates)
        → UpgradeSelectionView: 3버튼 패널 표시 (ISkillIconPort로 아이콘 조회)

유저 선택 (스킬 1개)
  → SkillSelectedEvent(playerId, chosenSkillId, displayName)
    → SkillRewardHandler: ISkillRewardPort.AddToDeck(skillId) → Deck.AddToDiscardPile()
    → UpgradeResultView: "스킬명 획득!" 2초 표시
    → WaveFlowController.OnSkillSelected(): 다음 카운트다운 시작
```

### 웨이브 종료

```text
EnemyDiedEvent
  → WaveEventHandler
    → WaveLoopUseCase.HandleEnemyDied()
      → WaveClearedEvent 또는 WaveVictoryEvent

PlayerDiedEvent (Downed가 아닌 Dead 전이 시에만 발행)
  → WaveEventHandler
    → AlivePlayerQueryAdapter.AnyPlayerAlive() == false
      → WaveDefeatEvent
⚠️ Downed 상태는 Dead가 아니므로 패배 조건에 해당하지 않음.
   전원 Downed 상태에서도 bleedout 시간 동안 구조 기회가 남는다.
```

## 네트워크 모델

### 동기화 채널: Room CustomProperties

| 키 | 타입 | 용도 |
|---|---|---|
| `waveIndex` | int | 현재 웨이브 인덱스 |
| `waveState` | int (WaveState enum) | 현재 웨이브 상태 |

Master만 write, Non-Master는 read only. `state_ownership.md`에 소유권 등록됨.

### 동기화 시점 — 4개의 안정 상태만

Countdown, Active, Victory, Defeat만 동기화한다. Cleared/UpgradeSelection은 과도 상태이므로 동기화하지 않는다.
- Cleared: AdvanceWave() 타이밍 이슈 회피. 다음 Countdown 동기화로 자연스럽게 보정.
- UpgradeSelection: 스킬 덱이 개인별이므로 각 클라이언트 로컬 처리.

### 역할별 동작

| 행위 | 권한자 | 비고 |
|---|---|---|
| CustomProperties write | Master | `WaveNetworkAdapter.SyncWaveState()` |
| 카운트다운 tick / HUD | 모든 클라이언트 | 로컬 진행, Master의 Countdown 동기화로 drift 보정 |
| 적 스폰 | Master | `PhotonNetwork.Instantiate` (EnemySpawnAdapter가 IsMasterClient 체크) |
| 적 AI / 이동 | Master | Enemy 피처에서 `IPunObservable` 동기화 |
| 승리 / 패배 화면 | 모든 클라이언트 | 로컬 Wave 이벤트 구독 |
| ForceState 수신 | Non-Master | `WaveNetworkAdapter.OnRoomPropertiesUpdate()` → `WaveLoopUseCase.ForceState()` |

### Late-join

`WaveBootstrap.Initialize()` 마지막에 `WaveNetworkAdapter.HydrateFromRoomProperties()`를 호출한다. Room CustomProperties에서 현재 `waveIndex`/`waveState`를 읽어 `ForceState()`로 fast-forward한다. 기본값(0, Idle)이면 콜백을 생략하여 첫 게임 시작과 구분한다.

### Master 교체

`WaveNetworkAdapter`가 `MonoBehaviourPunCallbacks`이므로 `OnMasterClientSwitched()`를 Photon이 자동 호출한다. 현재 Room CustomProperties에서 상태를 읽어 `OnWaveStateSynced`로 발행하며, 새 Master의 `WaveFlowController`가 현재 state에 맞게 이어서 진행한다.

## 씬 의존성

- `WaveBootstrap`과 `WaveFlowController`는 `GameSceneBootstrap`과 같은 오브젝트에 붙어 있다.
- 모든 Inspector 연결 필드(`_waveTable`, `_spawnAdapter`, `_playerPositionQuery`, `_hudView`, `_endView`, `_flowController`, `_upgradeView`, `_upgradeResultView`, `_networkAdapter`)는 `[Required, SerializeField]`로 선언해 저장 시점에 누락을 검증한다.
- `WaveHudCanvas`는 `Screen Space - Overlay` HUD 캔버스여야 하며, `CanvasScaler.ScaleWithScreenSize` 기준 해상도 `1920x1080`, `sortingOrder=50`을 유지한다.
- `UpgradeSelectionCanvas`는 `Screen Space - Overlay` HUD 캔버스여야 한다. `UpgradeSelectionView`가 여는 3지선다 패널과 `UpgradeResultView` 요약 표시는 이 캔버스 아래에서 렌더되므로 world-space로 바꾸면 Scene 뷰에는 보여도 Game 뷰 HUD로 보장되지 않는다.
- `UpgradeSelectionCanvas`는 `CanvasScaler.ScaleWithScreenSize` 기준 해상도 `1920x1080`, `sortingOrder=200`을 유지해 다른 HUD 위에서 안정적으로 표시한다.
- `WaveEndCanvas`도 `Screen Space - Overlay` HUD 캔버스여야 하며, `CanvasScaler.ScaleWithScreenSize` 기준 해상도 `1920x1080`, `sortingOrder=300`으로 승패 화면이 다른 HUD 위에 뜨도록 유지한다.
- 런타임 fallback(Resources.Load, GetComponent, AddComponent, CreateDefault)은 사용하지 않는다.

## 레이어 메모

- **Domain**: `WaveState` (UpgradeSelection 포함), `WaveProgress`
- **Application**: `WaveLoopUseCase` (`EnterUpgradeSelection()`은 `bool` 반환 — 보상 풀 소진 시 `false`, `ForceState()` — late-join/네트워크 동기화용), `WaveEventHandler`, `WaveNetworkEventHandler`, `SkillRewardHandler`, 웨이브 이벤트 5종 + 스킬 보상 이벤트 2종 (`SkillSelectionRequestedEvent`, `SkillSelectedEvent`) + `GameStartEvent` (room-wide 준비 완료), 포트 7종 (`IPlayerPositionQuery`, `IAlivePlayerQuery`, `IWaveTablePort`, `IWaveSpawnPort`, `ISkillRewardPort`, `IWaveNetworkCommandPort`, `IWaveNetworkCallbackPort`)
- **Infrastructure**: `WaveTableData` (`IWaveTablePort` 구현), `EnemySpawnAdapter` (`IWaveSpawnPort` 구현, 일괄 스폰 코루틴 포함), `AlivePlayerQueryAdapter`, `PlayerPositionQueryAdapter`, `WaveNetworkAdapter` (`IWaveNetworkCommandPort` + `IWaveNetworkCallbackPort` 구현, Room CustomProperties 기반)
- **Presentation**: `WaveFlowController` (UpgradeSelection 상태 처리 포함, 보상 풀 소진 시 스킵, `GameStartEvent` 구독으로 첫 웨이브 시작), `WaveHudView` (카운트다운 자체 표시), `WaveEndView`, `UpgradeSelectionView` (스킬 3지선다 UI, `[Required, SerializeField]`로 Text/Image 참조, ISkillIconPort로 아이콘 조회), `UpgradeResultView` (스킬 획득 결과 2초 표시)
- **Bootstrap**: `WaveBootstrap` (`MonoBehaviourPunCallbacks` — 조립 + Master SkillsReady 확인 → GameStartEvent 발행)

## 피처 의존성

- **Enemy**: `EnemyDiedEvent`, `EnemySetup`, `EnemyData` (스폰 시 `SpawnEnemy(data, ...)` 파라미터로 전달)
- **Player**: `PlayerDiedEvent`, 플레이어 Transform 등록
- **Combat**: `CombatBootstrap`
- **Skill**: `ISkillRewardPort` (Wave/Application/Ports에 정의, Skill/Application/SkillRewardAdapter가 구현), `ISkillIconPort` (Skill/Presentation에 정의, SkillIconAdapter가 구현), `SkillNetworkAdapter.IsPlayerSkillsReady()` (Master 준비 배리어 확인용)
- **Shared**: `EventBus`, `DisposableScope`, `DomainEntityId`
