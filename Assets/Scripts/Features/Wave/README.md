# Wave Feature

웨이브 기반 PvE 한 판 루프를 담당한다.

## 현재 책임

- 웨이브 카운트다운, 시작, 클리어, 승리/패배 상태 관리
- 적 사망 / 플레이어 전멸 이벤트를 받아 웨이브 진행 전이
- Master에서만 적 스폰 수행
- 각 클라이언트에서 동일한 웨이브 상태를 로컬 이벤트로 재현해 HUD/결과 UI 갱신
- 기본 HUD / 결과 UI가 씬에 없으면 런타임에 자동 생성

## 데이터 흐름

### 웨이브 시작

```text
GameSceneBootstrap
  → WaveBootstrap.Initialize(eventBus, combatBootstrap)
    → WaveLoopUseCase 생성
    → WaveEventHandler 생성
    → 모든 클라이언트에서 StartCountdown()
      → WaveCountdownStartedEvent
```

### 웨이브 진행

```text
WaveBootstrap.Update()
  → Countdown tick
  → 카운트다운 종료 시 WaveLoopUseCase.BeginWave(enemyCount)
  → Master만 EnemySpawnAdapter.SpawnEnemy() 반복 호출
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

- `WaveBootstrap`은 `GameSceneBootstrap`과 같은 오브젝트에 붙어도 된다.
- `_waveTable`이 비어 있으면 `Resources/Wave/DefaultWaveTable.asset`을 자동 로드한다.
- `_spawnAdapter`, `_playerPositionQuery`가 비어 있으면 같은 오브젝트에 자동 추가한다.
- `_hudView`, `_endView`가 비어 있으면 기본 UI를 런타임 생성한다.

## 레이어 메모

- **Domain**: `WaveState`, `WaveProgress`
- **Application**: `WaveLoopUseCase`, `WaveEventHandler`, 웨이브 이벤트 5종, 포트 3종
- **Infrastructure**: `WaveTableData`, `EnemySpawnAdapter`, `AlivePlayerQueryAdapter`, `PlayerPositionQueryAdapter`
- **Presentation**: `WaveHudView`, `WaveEndView`
- **Bootstrap**: `WaveBootstrap`

## 피처 의존성

- **Enemy**: `EnemyDiedEvent`, `EnemySetup`
- **Player**: `PlayerDiedEvent`, 플레이어 Transform 등록
- **Combat**: `CombatBootstrap`
- **Shared**: `EventBus`, `DisposableScope`
