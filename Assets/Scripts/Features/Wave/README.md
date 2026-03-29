# Wave Feature

웨이브 기반 PvE 한 판 루프를 담당한다.

## 현재 책임

- 웨이브 카운트��운, 시작, 클리어, 승리/패배 상태 관리
- 적 사망 / 플레이어 전멸 이벤트를 받아 웨이브 진행 전이
- Master에서만 적 스폰 수행
- 각 클라이언트에서 동일한 웨이브 상태를 로컬 이벤트로 재현해 HUD/결과 UI 갱신
- 비-Master 클라이언트에서 `EnemySetup.EnemyArrived` 콜백으로 원격 적 초기화 (Master는 `EnemySpawnAdapter`가 올바른 EnemyData로 명시적 초기화)

## 데이터 흐름

### 웨이브 시작

```text
GameSceneBootstrap
  → WaveBootstrap.Initialize(eventBus, combatBootstrap)
    → WaveLoopUseCase 생성
    → WaveEventHandler 생성
    → WaveFlowController.Initialize(waveLoop, waveTable, spawnAdapter)
      → StartCountdownForCurrentWave()
        → WaveCountdownStartedEvent
```

### 웨이브 진행

```text
WaveFlowController.Update()
  → Countdown 상태: WaveLoopUseCase.TickCountdown(deltaTime)
  → 카운트다운 종료 시 WaveLoopUseCase.BeginWave(enemyCount)
    → EnemySpawnAdapter.SpawnWaveEnemies(entry) — Master만 실제 스폰
  → Cleared 상태: 다음 웨이브 카운트다운 자동 시작
```

### 웨이브 종료

```text
EnemyDiedEvent
  → WaveEventHandler
    → WaveLoopUseCase.HandleEnemyDied()
      → WaveClearedEvent 또는 WaveVictoryEvent

PlayerDiedEvent
  → WaveEventHandler
    → AlivePlayerQueryAdapter.AnyPlayerAlive() == false
      → WaveDefeatEvent
```

## 네트워크 모델

| 행위 | 권한자 | 비고 |
|---|---|---|
| 카운트다운 / HUD | 모든 클라이언트 | 같은 WaveTableData와 EnemyDiedEvent를 기준으로 로컬 진행 |
| 적 스폰 | Master | `PhotonNetwork.Instantiate` |
| 적 AI / 이동 | Master | Enemy 피처에서 `IPunObservable` 동기화 |
| 승리 / 패배 화면 | 모든 클라이언트 | 로컬 Wave 이벤트 구독 |

## 씬 의존성

- `WaveBootstrap`과 `WaveFlowController`는 `GameSceneBootstrap`과 같은 오브젝트에 붙어 있다.
- 모든 Inspector 연결 필드(`_waveTable`, `_spawnAdapter`, `_playerPositionQuery`, `_hudView`, `_endView`, `_flowController`)는 `[Required, SerializeField]`로 선언해 저장 시점에 누락을 검증한다.
- 런타임 fallback(Resources.Load, GetComponent, AddComponent, CreateDefault)은 사용하지 않는다.

## 레이어 메모

- **Domain**: `WaveState`, `WaveProgress`
- **Application**: `WaveLoopUseCase`, `WaveEventHandler`, 웨이브 이벤트 5종, 포트 4종 (`IPlayerPositionQuery`, `IAlivePlayerQuery`, `IWaveTablePort`, `IWaveSpawnPort`)
- **Infrastructure**: `WaveTableData` (`IWaveTablePort` 구현), `EnemySpawnAdapter` (`IWaveSpawnPort` 구현, 일괄 스폰 코루틴 포함), `AlivePlayerQueryAdapter`, `PlayerPositionQueryAdapter`
- **Presentation**: `WaveFlowController` (포트 인터페이스로 Infrastructure 참조 없이 카운트다운 tick / 상태 전이 orchestration), `WaveHudView` (카운트다운 자체 표시), `WaveEndView`
- **Bootstrap**: `WaveBootstrap` (순수 조립 — 비즈니스 로직 없음)

## 피처 의존성

- **Enemy**: `EnemyDiedEvent`, `EnemySetup`, `EnemyData` (스폰 시 `SpawnEnemy(data, ...)` 파라미터로 전달)
- **Player**: `PlayerDiedEvent`, 플레이어 Transform 등록
- **Combat**: `CombatBootstrap`
- **Shared**: `EventBus`, `DisposableScope`
