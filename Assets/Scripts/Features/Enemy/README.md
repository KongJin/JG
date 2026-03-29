# Enemy Feature

적(Enemy) 엔티티의 생성, AI, 전투 통합, 접촉 데미지를 담당한다.

## 현재 책임

- 적 도메인 엔티티 생성 및 HP 관리
- Master 클라이언트에서 가장 가까운 플레이어를 향해 이동 (AI)
- 기존 Combat 시스템에 `RegisterTarget()`으로 통합 — 플레이어 투사체로 처치 가능
- 접촉 데미지: Master가 `OnTriggerStay`로 감지 → `CombatBootstrap.ApplyDamage()` 호출
- 위치 동기화: `IPunObservable`로 Master→클라이언트 위치 전송

## 데이터 흐름

### 스폰 (Master)

```text
WaveBootstrap.SpawnWaveEnemies()
  → EnemySpawnAdapter.SpawnEnemy()
    → PhotonNetwork.Instantiate("EnemyCharacter")
      → EnemySetup.Initialize(eventBus, combatBootstrap, data, playerQuery)
        → SpawnEnemyUseCase.Execute() → Enemy 도메인 생성 → EnemySpawnedEvent
        → combatBootstrap.RegisterTarget(enemyId, provider)
        → EnemyDamageEventHandler 생성 (DamageAppliedEvent 구독)
        → Master: EnemyAiAdapter + EnemyContactDamageDetector 초기화

원격 클라이언트에서는 `WaveBootstrap.InitializePendingEnemies()`가 아직 초기화되지 않은 `EnemySetup`을 찾아
프리팹의 `_defaultData` 기준으로 같은 적 엔티티를 로컬에 조립한다.
```

### 피격 (기존 Combat 경로 재사용)

```text
ProjectileHitEvent
  → CombatBootstrap → ApplyDamageUseCase → DamageAppliedEvent
    → EnemyDamageEventHandler (enemyId 필터)
      → Enemy.TakeDamage()
      → EnemyHealthChangedEvent 발행
      → (사망 시) EnemyDiedEvent 발행
        → EnemyView: Master가 PhotonNetwork.Destroy 호출
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

- **Domain**: `Enemy` (Entity), `EnemySpec` (readonly struct)
- **Application**: `SpawnEnemyUseCase`, `EnemyDamageEventHandler`, 이벤트 3개
- **Infrastructure**: `EnemyData` (ScriptableObject), `EnemyCombatTargetProvider`, `EnemyNetworkAdapter`, `EnemyAiAdapter`
- **Presentation**: `EnemyView` (피격 플래시, 사망 처리), `EnemyContactDamageDetector`
- **Bootstrap**: `EnemySetup` (프리팹 컴포지션 루트)

## 초기화 메모

- `EnemySetup`은 한 번만 초기화된다 (`IsInitialized`)
- Master는 `EnemySpawnAdapter`가 명시적으로 초기화
- 비Master는 `WaveBootstrap`이 지연 초기화
- 프리팹에는 `_defaultData`가 반드시 연결돼 있어야 한다

## 프리팹 요구사항

`Assets/Resources/EnemyCharacter.prefab`:
- PhotonView, EnemySetup, EnemyNetworkAdapter, EnemyAiAdapter
- EnemyView, EnemyContactDamageDetector, EntityIdHolder
- Collider (trigger) + Rigidbody (kinematic)
- 임시 메시 (큐브/캡슐)

## 피처 의존성

- **Combat**: `CombatBootstrap.RegisterTarget()`, `ApplyDamage()`, `ICombatTargetProvider`
- **Wave**: `IPlayerPositionQuery` (AI 이동 대상 조회)
- **Shared**: `Entity`, `DomainEntityId`, `EntityIdHolder`, `EventBus`
