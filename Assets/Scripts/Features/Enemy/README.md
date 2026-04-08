# Enemy Feature

Enemy 피처는 적 엔티티의 생성, AI, 전투 통합, 접촉 데미지를 담당한다.

## 먼저 읽을 규칙

- 전역 구조, 레이어, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- Bootstrap 책임, runtime lookup 예외, static event 예외: [anti_patterns.md](../../../../agent/anti_patterns.md)
- 이 피처의 초기화 순서와 late-join 전제: 이 문서의 `## 로컬 계약`

## 이 피처의 책임

- 적 도메인 엔티티 생성 및 HP 관리
- Master 클라이언트에서 이동 목표 결정: `EnemySpec.TargetMode` + `AggroRadius`에 따라 **플레이어/BattleEntity/코어** 우선순위 (`EnemyMoveTargetResolver` in `SpawnEnemyUseCase.cs`, Enemy 소유 포트 `ICoreObjectiveQuery` + `IPlayerPositionQuery` 주입). Phase 3 이후 `HostilePositionQuery`(Wave)가 Player + BattleEntity 통합 조회를 제공하여 적이 유닛도 타겟팅.
- `EnemySpec.StopDistance`(`EnemyData.stopDistance`): **코어를 추적 중일 때만** 적용되는 XZ 정지 거리. 코어는 트리거 콜라이더만 있으므로 물리 막힘 없이도 적이 셸 근처에서 멈추게 한다. 플레이어를 추적 중일 때는 접촉 피해를 위해 기존처럼 거의 겹칠 때까지 전진한다. `0`이면 레거시 정지(3D 거리 거의 0)만 사용한다.
- 기존 Combat 시스템에 `RegisterTarget()`으로 통합 — 플레이어 투사체로 처치 가능
- 접촉 데미지: Master가 `OnTriggerStay`로 감지 → `CombatBootstrap.ApplyDamage()` 호출
- 위치 동기화: `IPunObservable`로 Master→클라이언트 위치 전송

## 로컬 계약

- Master는 `EnemyData`로 명시적 초기화하고, 비-Master는 `EnemySetup.EnemyArrived` 경로로만 초기화한다.
- 원격 적 스펙의 기준값은 Photon `InstantiationData[0]`에 담긴 Resources 경로다.
- `EnemySetup.ResolveDataFromInstantiation()`의 같은 GameObject `PhotonView` 조회는 문서화된 허용 예외다.

## 핵심 흐름

### 스폰 (Master)

```text
WaveBootstrap.SpawnWaveEnemies()
  → EnemySpawnAdapter.SpawnEnemy()
    → PhotonNetwork.Instantiate("EnemyCharacter")
      → EnemySetup.Initialize(eventBus, combatBootstrap, data, playerQuery, coreObjectiveQuery)
        → SpawnEnemyUseCase.Execute() → Enemy 도메인 생성 → EnemySpawnedEvent
        → combatBootstrap.RegisterTarget(enemyId, provider)
        → EnemyDamageEventHandler 생성 (DamageAppliedEvent 구독)
        → Master: EnemyAiAdapter + EnemyContactDamageDetector 초기화

원격 클라이언트에서는 `EnemySetup`이 `IPunInstantiateMagicCallback`을 구현하여
Photon Instantiate 시점에 `EnemySetup.EnemyArrived` static event를 발행한다.
`WaveBootstrap`이 이 이벤트를 구독해 비-Master에서 `EnemySetup.Initialize(eventBus, combat, playerQuery, coreObjectiveQuery)`를 호출한다. 이때 스펙은 **Photon `InstantiationData[0]`**에 실은 `EnemyData.ResourcesLoadPath`(Resources 경로 문자열)로 `Resources.Load` 하며, 없으면 `Enemy/BasicEnemy`로 폴백한다. Master는 `EnemySpawnAdapter`가 명시적 `EnemyData`로 초기화한다.

- **GetComponent 예외:** `EnemySetup.ResolveDataFromInstantiation()`이 **같은 GameObject**의 `PhotonView.InstantiationData`만 읽는다. [anti_patterns.md](../../../../agent/anti_patterns.md)의 Runtime Lookup Policy 예외(한 번·로컬·문서화)에 해당한다.
```

### 피격 (기존 Combat 경로 재사용)

```text
ProjectileHitEvent
  → CombatBootstrap → ApplyDamageUseCase → DamageAppliedEvent
    → EnemyDamageEventHandler (enemyId 필터)
      → Enemy.TakeDamage()
      → EnemyHealthChangedEvent 발행
      → (사망 시) EnemyDiedEvent 발행
        → EnemySetup: Master가 PhotonNetwork.Destroy 호출
```

### 접촉 데미지 (Master만)

```text
EnemyContactDamageDetector.OnTriggerStay()
  → EntityIdHolder로 대상 식별
  → "enemy-" prefix로 적끼리 충돌 필터
  → CombatBootstrap.ApplyDamage(targetId, damage, Physical, enemyId)
```

## 네트워크 권한 모델

| 행위 | 권한자 | 동기화 |
|---|---|---|
| 스폰 | Master | `PhotonNetwork.Instantiate` |
| AI 이동 | Master | `IPunObservable` 위치 동기화 |
| 투사체 데미지 | 투사체 소유자 | 기존 Combat RPC 경로 |
| 접촉 데미지 | Master | `CombatBootstrap.ApplyDamage` → RPC |
| 소멸 | Master | `PhotonNetwork.Destroy` |

## 레이어 메모

- **Domain**: `Enemy` (Entity), `EnemySpec` (readonly struct; `EnemyTargetMode` enum 동일 파일)
- **Application**: `SpawnEnemyUseCase`, `EnemyMoveTargetResolver` (같은 파일), `EnemyDamageEventHandler`, 이벤트 3개
- **Infrastructure**: `EnemyData` (ScriptableObject), `EnemyCombatTargetProvider`, `EnemyNetworkAdapter`, `EnemyAiAdapter`
- **Presentation**: `EnemyView` (피격 플래시), `EnemyHealthBarView` (World-Space Canvas 기반 체력바, Billboard), `EnemyContactDamageDetector`
- **Bootstrap**: `EnemySetup` (프리팹 컴포지션 루트)

## 초기화 메모

- `EnemySetup`은 한 번만 초기화된다 (`IsInitialized`)
- `EnemySetup`은 `IPunInstantiateMagicCallback`을 구현해 Photon Instantiate 시점에 `EnemyArrived` static event를 발행한다
- Master는 `EnemySpawnAdapter`가 올바른 EnemyData로 명시적 초기화 (`EnemyArrived` 콜백에서는 `IsMasterClient` 가드로 스킵)
- 비-Master는 `WaveBootstrap`이 `EnemyArrived` 구독으로 `LoadDefaultData()` 기반 초기화 (폴링/FindObjectsByType 사용하지 않음)
- 원격 클라이언트 기본 적 데이터 로드는 `Resources/Enemy/BasicEnemy.asset` 폴백을 유지하되, 실제 스폰 타입은 `InstantiationData[0]`의 Resources 경로(`Enemy/BasicEnemy`, `Enemy/FastEnemy`, `Enemy/CoreHunterEnemy`, `Enemy/CoreSiegeEnemy` 등)로 결정된다
- Inspector 연결 필드는 `[Required, SerializeField]`로 선언해 저장 시점에 누락을 검증한다

## 프리팹 요구사항

`Assets/Resources/EnemyCharacter.prefab`:
- PhotonView, EnemySetup, EnemyNetworkAdapter, EnemyAiAdapter
- EnemyView, EnemyContactDamageDetector, EntityIdHolder
- `EnemySetup._healthBarPrefab` → `Assets/Resources/EnemyHealthBar.prefab` 에셋 참조
- Collider (trigger) + Rigidbody (kinematic)
- 임시 메시 (큐브/캡슐)

`Assets/Resources/EnemyHealthBar.prefab`:
- World-Space Canvas + Slider + Fill Image + `EnemyHealthBarView`
- `EnemySetup.SpawnHealthBar()`가 Initialize 시 Instantiate → 적 Transform 자식으로 부착
- GetComponent 예외: Instantiate한 프리팹에서 `EnemyHealthBarView`를 가져온다. 같은 GameObject, 1회, Photon Instantiate가 아닌 로컬 Instantiate이므로 Inspector 연결 불가 — Runtime Lookup Policy 허용 예외에 해당

`Assets/Resources/EnemyCharacterCore.prefab`:
- 코어 공성 적 시각용 대형 프리팹(선택). 컴포지션 계약은 `EnemyCharacter.prefab`과 동일.
- `CoreSiegeEnemy.asset`은 Photon `Resources` Instantiate 안정을 위해 **`prefabName: EnemyCharacter`** 를 사용한다(데이터 기본값). 시각만 바꿀 때는 에셋에서 `EnemyCharacterCore`로 전환 가능.

## 피처 의존성

- **Combat**: `CombatBootstrap.RegisterTarget()`, `ApplyDamage()`, `ICombatTargetProvider`
- **Wave**: `CoreObjectiveBootstrap`, `HostilePositionQuery` (Phase 3 신규 — Player + BattleEntity 통합 조회)가 Enemy 소유 포트 `IPlayerPositionQuery`, `ICoreObjectiveQuery`를 구현해 AI 이동 목표를 제공
- **Shared**: `Entity`, `DomainEntityId`, `EntityIdHolder`, `EventBus`
