# Wave Feature

Wave 피처는 웨이브 기반 PvE 한 판 루프, 목표 코어, 보상 선택, 웨이브 상태 동기화를 담당한다.

## 먼저 읽을 규칙

- 전역 구조, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- Bootstrap 책임, runtime lookup 예외, gameplay 루프 분리 규칙: [anti_patterns.md](../../../../agent/anti_patterns.md)
- 이 피처의 초기화 순서와 Skill 이후 Wave wiring 전제: 이 문서의 `## 로컬 계약`
- Room `difficultyPreset`, `waveIndex`, `waveState`, `countdownEnd` 소유권: 이 문서의 `## 네트워크 모델`

## 이 피처의 책임

- 웨이브 카운트다운, 시작, 클리어, 승리/패배 상태 관리
- 적 사망 / 플레이어 전멸 이벤트를 받아 웨이브 진행 전이
- Master에서만 적 스폰 수행
- **목표 코어**: 도메인 `ObjectiveCore` + `ObjectiveCoreIds.Default` (`objective-core`). `CoreObjectiveBootstrap`이 `CombatBootstrap.RegisterTarget`으로 코어를 등록하고 Enemy 소유 포트 `ICoreObjectiveQuery`를 구현해 위치를 노출한다. `WaveEventHandler`는 `DamageAppliedEvent`에서 코어가 파괴되면 `WaveLoopUseCase.EnterDefeatIfActive()` → `WaveDefeatEvent`
- **난이도 스폰 배율**: `DifficultySpawnScale`(Application 정적 매핑) + `RoomDifficultyReader`(Room `difficultyPreset` 읽기). `WaveBootstrap`은 `ResolveSpawnCountMultiplier()`로 배율만 주입한다(anti_patterns: Bootstrap에 산식 장황 금지). 선택적으로 같은 씬에 `RoomDifficultySpawnScaleProvider`(`IDifficultySpawnScale`)를 두고 Inspector에 연결하면 배율 조회가 그 컴포넌트로만 모인다(미연결 시 Bootstrap이 Reader+Scale로 폴백). 키 문자열은 `WaveRoomPropertyKeys.DifficultyPreset`이며, Lobby 상수와 동일해야 한다. 소유권 기준은 이 문서의 `## 네트워크 모델`.
- 각 클라이언트에서 동일한 웨이브 상태를 로컬 이벤트로 재현해 HUD/결과 UI 갱신
- 비-Master 클라이언트에서 `EnemySetup.EnemyArrived` 콜백으로 원격 적 초기화 (Master는 `EnemySpawnAdapter`가 올바른 EnemyData로 명시적 초기화)
- **웨이브 클리어 시 보상 선택 (새 스킬 1 + 강화 2)**: `DrawRewardCandidates(1, 2)`로 새 스킬 1개 + 덱 내 기존 스킬 강화 후보 2개를 `RewardCandidate[]`로 제시. 새 스킬은 덱 버린 더미에 추가되고, 강화는 `ISkillUpgradeCommandPort.TryUpgrade()`로 영구 업그레이드 레벨을 올린다. 후보가 0개면 선택을 건너뛴다.
- **선택 타이머 (10초 + 자동 건너뛰기)**: `SkillSelectionRequestedEvent.SelectionDuration`(기본 10초) 내 미선택 시 `WaveFlowController`가 `SkillSelectionSkippedEvent`를 발행해 **보상 없이** 다음 웨이브 카운트다운으로 진행한다. `UpgradeSelectionView`는 `WaveFlowController`의 `SelectionTimer.Remaining`을 읽어 카운트다운 텍스트를 표시한다 (자체 타이머 없음, 단일 소스). **건너뛰기** 버튼(`UpgradeSelectionView.skipButton`, `[Required]`)은 씬에서 반드시 연결하며, 클릭 시 동일 이벤트로 수동 스킵한다.
- **MVP ② 관측 로그**: `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서 `WaveFlowController`가 보상 후보 목록을, `UpgradeSelectionView`·자동선택 경로가 선택 결과를 `[MvpReward]` 접두로 로그한다(후보 순서·수동 시 경과 초).
- **보상 결과 표시**: 새 스킬 → "스킬명 획득!", 강화 → "스킬명 강화: 축 +1" 형태로 2초간 표시

## 로컬 계약

- Wave는 시작 스킬 선택이 끝난 뒤에만 초기화한다.
- `difficultyPreset`은 Lobby가 쓰고 Wave는 읽기만 한다.
- Countdown, Active, Victory, Defeat만 네트워크 동기화하고, Cleared/UpgradeSelection은 로컬 과도 상태로 처리한다.
- 비-Master 원격 적 초기화는 `EnemySetup.EnemyArrived` 경로만 사용한다.
- 적 AI용 질의 포트 `IPlayerPositionQuery`, `ICoreObjectiveQuery`는 Enemy가 소유하고, Wave는 `PlayerPositionQueryAdapter`, `CoreObjectiveBootstrap`으로 구현만 제공한다.

## 핵심 흐름

```text
GameSceneRoot (SkillSetup.onComplete 콜백)
  → WaveBootstrap.Initialize(...)
    → WaveLoopUseCase / WaveEventHandler / SkillRewardHandler 생성
    → WaveFlowController.Initialize(...)
    → Master: TryStartGame() — 전원 SkillsReady 확인
      → GameStartEvent
        → WaveFlowController.StartFirstWave()
          → WaveCountdownStartedEvent
```

### DefaultWaveTable / 코어 HP (MVP 튜닝)

- `Resources/Wave/DefaultWaveTable.asset`: `spawnDelay`를 5~10초 수준으로 두어 **trickle spawn**(한 번에 몰리지 않게)한다. 웨이브 1~2는 플레이어 추적 적, 3~4는 `CoreHunterEnemy`, 5는 `CoreSiegeEnemy`. `countdownDuration`은 웨이브 간 짧은 휴식(3~5초). `../../../../docs/design/game_design.md`의 웨이브별 **전투 초**는 설계 목표이며, 구현은 **적 전멸(`RemainingEnemies`)** 로 클리어한다(별도 전투 타이머 없음).
- `CoreObjectiveBootstrap` 기본 `_maxHp`는 1500(씬 `JG_GameScene`의 ObjectiveCore와 동일). 코어 접촉 DPS가 높을 때 즉시 패배를 완화한다.

## 상세 흐름

### 웨이브 시작

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
        → UpgradeSelectionView: 3버튼 패널 표시 (NewSkill/Upgrade 구분, SelectionTimer.Remaining으로 카운트다운 표시)
        → WaveFlowController: 후보 캐시 + SelectionTimer 시작

유저 선택 → SkillSelectedEvent(playerId, skillId, displayName, candidateType, axis)
  또는 타이머 만료 / 건너뛰기 버튼 → SkillSelectionSkippedEvent(playerId, waveIndex) (덱·업그레이드 미적용)
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

DamageAppliedEvent
  → WaveEventHandler (대상이 objective core이고 IsDead)
    → WaveLoopUseCase.EnterDefeatIfActive()
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

Master만 write, Non-Master는 read only. `difficultyPreset`은 Lobby가 쓰고 Wave는 읽기만 하며, `waveIndex` / `waveState` / `countdownEnd`는 Wave만 쓴다.

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

`WaveBootstrap.Initialize()` 마지막에 `WaveNetworkAdapter.HydrateFromRoomProperties()`를 호출한다. Room CustomProperties에서 현재 `waveIndex`/`waveState`/`countdownEnd`를 읽어 `ForceState()`로 fast-forward한다. `ForceState()`는 상태 변경 후 `WaveHydratedEvent`를 발행하여, 이벤트 기반 소비자(`WaveHudView`, `WaveEndView`, `FriendlyFireScalingAdapter` 등)가 late-join 시에도 올바른 상태를 수신한다. `WaveEndView`는 Victory/Defeat 상태의 `WaveHydratedEvent`를 받아 종료 화면을 복원한다. `WaveHydratedEvent`는 `WaveNetworkEventHandler`가 구독하지 않으므로 네트워크 재동기화 순환이 발생하지 않는다. Countdown 상태에서는 `countdownEnd`와 `PhotonNetwork.ServerTimestamp`의 차이로 남은 시간을 계산하여 복원한다. 기본값(0, Idle)이면 콜백을 생략하여 첫 게임 시작과 구분한다. hydrate 결과가 Idle이 아니면 `_gameStarted = true`로 세팅하여 `GameStartEvent` 재발행을 방지한다.

### Master 교체

`WaveNetworkAdapter`가 `MonoBehaviourPunCallbacks`이므로 `OnMasterClientSwitched()`를 Photon이 자동 호출한다. 현재 Room CustomProperties에서 상태를 읽어 `OnWaveStateSynced`로 발행하며, 새 Master의 `WaveFlowController`가 현재 state에 맞게 이어서 진행한다. `WaveBootstrap`도 `OnMasterClientSwitched()`를 오버라이드하여, 게임 시작 전(pre-game) master가 교체된 경우 ready barrier를 재평가한다.

## 씬 의존성

- `WaveBootstrap`, `EnemySpawnAdapter`, `PlayerPositionQueryAdapter`, `WaveFlowController`, `WaveNetworkAdapter`는 `JG_GameScene`의 루트 `GameSceneRoot` 아래 자식 `WaveSystems` 오브젝트에 함께 배치한다. `GameSceneRoot._waveBootstrap`은 이 자식 GO의 `WaveBootstrap`을 참조한다.
- 모든 Inspector 연결 필드(`_waveTable`, `_spawnAdapter`, `_playerPositionQuery`, `_hudView`, `_endView`, `_flowController`, `_upgradeView`, `_upgradeResultView`, `_networkAdapter`, `_coreHealthView`)는 `[Required, SerializeField]`로 선언해 저장 시점에 누락을 검증한다. `WaveBootstrap.Initialize`는 Enemy 소유 포트 `ICoreObjectiveQuery`를 구현한 씬의 `CoreObjectiveBootstrap`을 받아 적 스폰/AI에 전달한다.
- 씬의 월드 루트 `WorldRoot` 아래에 **ObjectiveCore**(앵커 Transform, `EntityIdHolder`, `CoreObjectiveBootstrap`, 코어용 트리거 콜라이더 등)를 두고 `GameSceneRoot._coreObjective`에 연결한다(웨이브 사용 시 필수).
- 씬 중앙에는 플레이어가 방어 대상을 즉시 읽을 수 있도록 코어 시각 마커를 둔다. 현재 `JG_GameScene`은 `WorldRoot/CoreVisual` 큐브를 사용한다.
- `WaveHudCanvas`는 `UIRoot` 아래에 두는 `Screen Space - Overlay` HUD 캔버스여야 하며, `CanvasScaler.ScaleWithScreenSize` 기준 해상도 `1920x1080`, `sortingOrder=50`을 유지한다. `CoreHealthHudView`(코어 HP 바)를 이 캔버스 아래에 배치하고 `WaveBootstrap._coreHealthView`에 반드시 연결한다. `CoreHealthHudView` 내부의 `healthSlider`, `fillImage`, `hpText`도 모두 필수 Inspector 참조다.
- `WaveHudView`의 **선택** 필드 `firstWaveDeckHintText`: 웨이브1 카운트다운 동안만 덱 순환 안내 한 줄을 띄운다(MVP ①·온보딩). 비우면 기존과 동일하게 동작한다.
- `UpgradeSelectionCanvas`는 `UIRoot` 아래에 두는 `Screen Space - Overlay` HUD 캔버스여야 한다. `UpgradeSelectionView`가 여는 보상 패널(최대 3 후보 슬롯 + **필수** `SkipButton` → `skipButton` 참조)과 `UpgradeResultView` 요약 표시는 이 캔버스 아래에서 렌더되므로 world-space로 바꾸면 Scene 뷰에는 보여도 Game 뷰 HUD로 보장되지 않는다.
- 동일 씬의 `UIRoot` 아래에 `StartSkillSelectionCanvas`가 있으면 `sortingOrder`가 낮은 쪽이 먼저 그려지므로, **웨이브 보상 Panel은 씬에서 기본 비활성**으로 두고 `UpgradeSelectionView`가 `SkillSelectionRequestedEvent` 때만 연다. 그렇지 않으면 `WaveBootstrap.Initialize` 이전에 Panel이 상위 캔버스로 시작 스킬 UI를 가릴 수 있다. **`UpgradeResultView/ResultPanel`** 도 동일하게 씬에서 기본 비활성으로 저장한다(`UpgradeResultView`는 `Awake()`에서 `panel`을 끄는 가드로 에디터 저장 실수·초기 1프레임 노출을 완화).
- `UpgradeSelectionCanvas`는 `CanvasScaler.ScaleWithScreenSize` 기준 해상도 `1920x1080`, `sortingOrder=200`을 유지해 다른 HUD 위에서 안정적으로 표시한다.
- `WaveEndCanvas`도 `UIRoot` 아래에 두는 `Screen Space - Overlay` HUD 캔버스여야 하며, `CanvasScaler.ScaleWithScreenSize` 기준 해상도 `1920x1080`, `sortingOrder=300`으로 승패 화면이 다른 HUD 위에 뜨도록 유지한다.
- `WaveEndView/Panel`은 전체 화면을 덮는 반투명 배경 + 중앙 `ResultText` 구조를 유지한다. 흰 100x100 임시 패널처럼 보이지 않도록 배경 색과 텍스트 크기를 씬에서 관리한다.
- 런타임 fallback(Resources.Load, GetComponent, AddComponent, CreateDefault)은 사용하지 않는다.

## 레이어 메모

- **Domain**: `WaveState` (UpgradeSelection 포함), `WaveProgress` (하단에 `ObjectiveCoreIds`, `ObjectiveCore` 타입 동일 파일)
- **Application**: `WaveLoopUseCase` (패배: `EnterDefeatIfActive`, `HandleAllPlayersDead`; `SpawnObjectiveCoreUseCase` 동일 파일), `EnterUpgradeSelection()` / `DrawRewardCandidates(1,2)` (`bool`, 후보 0이면 `false`), `ForceState()` (late-join/동기화), `WaveEventHandler` (`DamageAppliedEvent`로 코어 사망 시 패배), `WaveNetworkEventHandler`, `SkillRewardHandler`, `SelectionTimer`, 웨이브 이벤트 5종 + `WaveHydratedEvent` + 스킬 보상 이벤트 `SkillSelectionRequestedEvent` / `SkillSelectedEvent` / `SkillSelectionSkippedEvent` + `GameStartEvent`, 포트 (`IAlivePlayerQuery`, `IWaveTablePort`, `IWaveSpawnPort`, `ISkillRewardPort`, `IWaveNetworkCommandPort`, `IWaveNetworkCallbackPort`), `RewardCandidate`/`CandidateType`
- **Infrastructure**: `WaveTableData` (`IWaveTablePort` 구현), `EnemySpawnAdapter` (`IWaveSpawnPort` 구현, 일괄 스폰 코루틴 포함), `AlivePlayerQueryAdapter`, `PlayerPositionQueryAdapter` (하단에 `ObjectiveCoreCombatTargetProvider` 동일 파일), `WaveNetworkAdapter` (`IWaveNetworkCommandPort` + `IWaveNetworkCallbackPort` 구현, Room CustomProperties 기반)
- **Presentation**: `WaveFlowController` (UpgradeSelection 상태에서 `SelectionTimer` 틱 + 만료 시 `SkillSelectionSkippedEvent`, 보상 후보 0개면 선택 UI 스킵, `GameStartEvent` 구독으로 첫 웨이브 시작; 에디터/개발 빌드 `[MvpReward]` 로그), `WaveHudView` (카운트다운 자체 표시, 선택적 `firstWaveDeckHintText`, `WaveHydratedEvent` 구독으로 late-join HUD 복원), `CoreHealthHudView` (코어 HP 바, `DamageAppliedEvent` 구독 + coreId 필터, 씬 소유 HUD), `WaveEndView` (`WaveVictoryEvent`/`WaveDefeatEvent` + `WaveHydratedEvent` 구독으로 late-join terminal state 복원), `UpgradeSelectionView` (새 스킬/강화 구분 UI, `[NEW]`/`[강화]` 라벨, `SelectionTimer.Remaining` 읽기로 카운트다운 표시, ISkillIconPort로 아이콘 조회, `SkillSelectedEvent`/`SkillSelectionSkippedEvent`로 패널 닫기, `[Required]` `skipButton`으로 수동 스킵), `UpgradeResultView` (NewSkill→"획득!", Upgrade→"강화: 축 +1" 2초 표시)
- **Bootstrap**: `WaveBootstrap` (`MonoBehaviourPunCallbacks` — 조립 + Master SkillsReady 확인 → GameStartEvent 발행), `CoreObjectiveBootstrap` (독립 씬 스크립트 — `RegisterCombatTarget`, Enemy 소유 포트 `ICoreObjectiveQuery` 구현)

## 피처 의존성

- **Enemy**: `EnemyDiedEvent`, `EnemySetup`, `EnemyData` (스폰 시 `SpawnEnemy(data, ...)` 파라미터로 전달)
- **Player**: `PlayerDiedEvent`, 플레이어 Transform 등록
- **Combat**: `CombatBootstrap`
- **Skill**: `ISkillRewardPort` (Wave/Application/Ports에 정의, Skill/Application/SkillRewardAdapter가 구현), `ISkillIconPort` (Skill/Presentation에 정의, SkillIconAdapter가 구현), `ISkillUpgradeCommandPort` (Skill/Application/Ports에 정의, SkillUpgradeAdapter가 구현 — 보상 강화 적용용), `GrowthAxis` (Skill/Domain), `SkillNetworkAdapter.IsPlayerSkillsReady()` (Master 준비 배리어 확인용)
- **Shared**: `EventBus`, `DisposableScope`, `DomainEntityId`
