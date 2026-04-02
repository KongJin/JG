# Wave Feature

웨이브 기반 PvE 한 판 루프를 담당한다.

## 현재 책임

- 웨이브 카운트다운, 시작, 클리어, 승리/패배 상태 관리
- 적 사망 / 플레이어 전멸 이벤트를 받아 웨이브 진행 전이
- Master에서만 적 스폰 수행
- 각 클라이언트에서 동일한 웨이브 상태를 로컬 이벤트로 재현해 HUD/결과 UI 갱신
- 비-Master 클라이언트에서 `EnemySetup.EnemyArrived` 콜백으로 원격 적 초기화 (Master는 `EnemySpawnAdapter`가 올바른 EnemyData로 명시적 초기화)
- **웨이브 클리어 시 보상 선택 (새 스킬 1 + 강화 2)**: `DrawRewardCandidates(1, 2)`로 새 스킬 1개 + 덱 내 기존 스킬 강화 후보 2개를 `RewardCandidate[]`로 제시. 새 스킬은 덱 버린 더미에 추가되고, 강화는 `ISkillUpgradeCommandPort.TryUpgrade()`로 영구 업그레이드 레벨을 올린다. 후보가 0개면 선택을 건너뛴다.
- **선택 타이머 (10초 + 자동선택)**: `SkillSelectionRequestedEvent.SelectionDuration`(기본 10초) 내 미선택 시 `WaveFlowController`가 첫 번째 후보를 자동선택. `UpgradeSelectionView`가 카운트다운 텍스트 표시.
- **보상 결과 표시**: 새 스킬 → "스킬명 획득!", 강화 → "스킬명 강화: 축 +1" 형태로 2초간 표시

## 데이터 흐름

### 웨이브 시작

```text
GameSceneBootstrap (SkillSetup.onComplete 콜백)
  → WaveBootstrap.Initialize(eventBus, combatBootstrap, localPlayerId, skillReward, iconPort, upgradeCommand)
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
      → ISkillRewardPort.DrawRewardCandidates(1, 2) — 새 스킬 1 + 강화 2 추출 (0개면 선택 스킵)
      → SkillSelectionRequestedEvent(waveIndex, candidates, selectionDuration=10)
        → UpgradeSelectionView: 3버튼 패널 표시 (NewSkill/Upgrade 구분, 카운트다운 텍스트)
        → WaveFlowController: 후보 캐시 + SelectionTimer 시작

유저 선택 또는 타이머 만료 (첫 번째 후보 자동선택)
  → SkillSelectedEvent(playerId, skillId, displayName, candidateType, axis)
    → UpgradeSelectionView.OnSkillSelected(): 패널 닫기 + candidates null (이중 클릭 방지)
    → SkillRewardHandler (멱등성 가드: 선택 라운드당 1회만 적용):
      - NewSkill → AddToDeck(skillId) → Deck.AddToDiscardPile()
      - Upgrade → ISkillUpgradeCommandPort.TryUpgrade(skillId, axis)
    → UpgradeResultView: "스킬명 획득!" 또는 "스킬명 강화: 축 +1" 2초 표시
    → WaveFlowController.OnSkillSelected(): 타이머 정지, 다음 카운트다운 시작
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
| `countdownEnd` | int | 카운트다운 종료 시각 (`PhotonNetwork.ServerTimestamp` ms 기준). Countdown 상태에서만 유의미 |

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

`WaveBootstrap.Initialize()` 마지막에 `WaveNetworkAdapter.HydrateFromRoomProperties()`를 호출한다. Room CustomProperties에서 현재 `waveIndex`/`waveState`/`countdownEnd`를 읽어 `ForceState()`로 fast-forward한다. `ForceState()`는 상태 변경 후 `WaveHydratedEvent`를 발행하여, 이벤트 기반 소비자(`WaveHudView`, `FriendlyFireScalingAdapter` 등)가 late-join 시에도 올바른 상태를 수신한다. `WaveHydratedEvent`는 `WaveNetworkEventHandler`가 구독하지 않으므로 네트워크 재동기화 순환이 발생하지 않는다. Countdown 상태에서는 `countdownEnd`와 `PhotonNetwork.ServerTimestamp`의 차이로 남은 시간을 계산하여 복원한다. 기본값(0, Idle)이면 콜백을 생략하여 첫 게임 시작과 구분한다. hydrate 결과가 Idle이 아니면 `_gameStarted = true`로 세팅하여 `GameStartEvent` 재발행을 방지한다.

### Master 교체

`WaveNetworkAdapter`가 `MonoBehaviourPunCallbacks`이므로 `OnMasterClientSwitched()`를 Photon이 자동 호출한다. 현재 Room CustomProperties에서 상태를 읽어 `OnWaveStateSynced`로 발행하며, 새 Master의 `WaveFlowController`가 현재 state에 맞게 이어서 진행한다. `WaveBootstrap`도 `OnMasterClientSwitched()`를 오버라이드하여, 게임 시작 전(pre-game) master가 교체된 경우 ready barrier를 재평가한다.

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
- **Application**: `WaveLoopUseCase` (`EnterUpgradeSelection()`은 `DrawRewardCandidates(1,2)` 사용, `bool` 반환 — 후보 0개면 `false`, `ForceState()` — late-join/네트워크 동기화용), `WaveEventHandler`, `WaveNetworkEventHandler`, `SkillRewardHandler` (`CandidateType`별 분기: NewSkill→AddToDeck, Upgrade→TryUpgrade, 선택 라운드당 1회 멱등성 가드), `SelectionTimer` (순수 C# — 시작/틱/만료), 웨이브 이벤트 5종 + `WaveHydratedEvent` (ForceState 후 hydration 알림) + 스킬 보상 이벤트 2종 (`SkillSelectionRequestedEvent` — `RewardCandidate[]` + `SelectionDuration`, `SkillSelectedEvent` — `CandidateType` + `GrowthAxis`) + `GameStartEvent` (room-wide 준비 완료), 포트 7종 + `RewardCandidate`/`CandidateType` (`IPlayerPositionQuery`, `IAlivePlayerQuery`, `IWaveTablePort`, `IWaveSpawnPort`, `ISkillRewardPort`, `IWaveNetworkCommandPort`, `IWaveNetworkCallbackPort`)
- **Infrastructure**: `WaveTableData` (`IWaveTablePort` 구현), `EnemySpawnAdapter` (`IWaveSpawnPort` 구현, 일괄 스폰 코루틴 포함), `AlivePlayerQueryAdapter`, `PlayerPositionQueryAdapter`, `WaveNetworkAdapter` (`IWaveNetworkCommandPort` + `IWaveNetworkCallbackPort` 구현, Room CustomProperties 기반)
- **Presentation**: `WaveFlowController` (UpgradeSelection 상태에서 `SelectionTimer` 틱 + 만료 시 자동선택, 보상 풀 소진 시 스킵, `GameStartEvent` 구독으로 첫 웨이브 시작), `WaveHudView` (카운트다운 자체 표시, `WaveHydratedEvent` 구독으로 late-join HUD 복원), `WaveEndView`, `UpgradeSelectionView` (새 스킬/강화 구분 UI, `[NEW]`/`[강화]` 라벨, 카운트다운 텍스트, ISkillIconPort로 아이콘 조회, `SkillSelectedEvent` 구독으로 자동선택 시 패널 닫기), `UpgradeResultView` (NewSkill→"획득!", Upgrade→"강화: 축 +1" 2초 표시)
- **Bootstrap**: `WaveBootstrap` (`MonoBehaviourPunCallbacks` — 조립 + Master SkillsReady 확인 → GameStartEvent 발행)

## 피처 의존성

- **Enemy**: `EnemyDiedEvent`, `EnemySetup`, `EnemyData` (스폰 시 `SpawnEnemy(data, ...)` 파라미터로 전달)
- **Player**: `PlayerDiedEvent`, 플레이어 Transform 등록
- **Combat**: `CombatBootstrap`
- **Skill**: `ISkillRewardPort` (Wave/Application/Ports에 정의, Skill/Application/SkillRewardAdapter가 구현), `ISkillIconPort` (Skill/Presentation에 정의, SkillIconAdapter가 구현), `ISkillUpgradeCommandPort` (Skill/Application/Ports에 정의, SkillUpgradeAdapter가 구현 — 보상 강화 적용용), `GrowthAxis` (Skill/Domain), `SkillNetworkAdapter.IsPlayerSkillsReady()` (Master 준비 배리어 확인용)
- **Shared**: `EventBus`, `DisposableScope`, `DomainEntityId`
