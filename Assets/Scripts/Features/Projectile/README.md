# Projectile Feature

투사체의 생성, 궤적 계산, 충돌 판정, 시각 연출을 담당한다.

## 책임

- 투사체 스폰 (프리팹 인스턴스화)
- 매 프레임 궤적 전략에 따른 이동
- 충돌 시 Hit 전략에 따른 판정 (파괴/관통/반사/연쇄)
- 시각 이펙트 (색상, 수명 관리)

## 이벤트 흐름

```
Skill Feature → ProjectileRequestedEvent(ownerId, spec, position, direction)
  → ProjectileSpawner (Bootstrap)
    → 프리팹 선택 (TrajectoryType 기반)
    → Instantiate at event position/direction
    → ProjectilePhysicsAdapter.Spawn(projectile, trajectory, hitResolver)
    → ProjectileView.SetColor

Update Loop (ProjectilePhysicsAdapter):
  → trajectory.NextPosition(input) 매 프레임 호출
  → OnTriggerEnter → hitResolver.Resolve → IHitResult.Apply
    → ProjectileHitEvent 발행
    → 조건 충족 시 GameObject 파괴
```

## 도메인 전략 (다형성)

### 궤적 (TrajectoryType → ITrajectory)

| 타입 | 동작 |
|---|---|
| Linear | 직선 이동 |
| Parabolic | 포물선 (중력) |
| Homing | 타겟 추적 (turnRate 5) |
| Orbit | 타겟 주위 공전 (반경 3) |
| Boomerang | 전진 후 복귀 (1초 전환) |

### 충돌 (HitType → IHitResolver)

| 타입 | 동작 |
|---|---|
| Single | 첫 충돌에 파괴 |
| Piercing | 관통 계속 |
| Bounce | 최대 3회 반사 |
| Chain | 최대 3회 연쇄 |

`TrajectoryFactory`, `HitResolverFactory`가 enum → 전략 인스턴스를 생성한다.

## 네트워크 동기화

현재 투사체 자체는 네트워크 동기화하지 않는다.
Skill Feature의 RPC로 원격 `ProjectileRequestedEvent`가 발행되면
각 클라이언트가 독립적으로 로컬 투사체를 스폰한다 (시뮬레이션 동기화 방식).

## 레이어 메모

- **Domain**: `Projectile`, `ProjectileSpec`, `TrajectoryInput`, `TrajectoryType`/`HitType` enum, `ITrajectory`/`IHitResolver`/`IHitResult` 인터페이스, `TrajectoryFactory`, `HitResolverFactory`, 각 전략 구현체
- **Application**: `SpawnProjectileUseCase`, `IProjectilePhysicsPort` (Application/Ports), `ProjectileRequestedEvent`, `ProjectileHitEvent`, `ProjectileSpawnedEvent`
- **Infrastructure**: `ProjectilePhysicsAdapter` (MonoBehaviour, 물리 이동/충돌 처리)
- **Presentation**: `ProjectileView` (색상, 수명 관리)
- **Bootstrap**: `ProjectileSpawner` (이벤트 구독 → 프리팹 스폰 → UseCase 실행) - 피처 루트에 위치

## Bootstrap

- **ProjectileSpawner** (MonoBehaviour): `ProjectileRequestedEvent` 구독 → 투사체 스폰
  - `[SerializeField] ProjectilePhysicsAdapter _projectilePrefab`과 `GameObjectPool`로 프리팹 기반 풀링
  - `Initialize()`에서 `SpawnProjectileUseCase`를 한 번 생성
  - 이벤트마다 pool에서 Rent → `ProjectilePhysicsAdapter.Initialize/Spawn` 호출

## 프리팹 설정 주의

- **SphereCollider**: `IsTrigger = true` 필수. `OnTriggerEnter`가 동작하려면 최소 하나의 콜라이더가 Trigger여야 한다.
- **Rigidbody**: `IsKinematic = true`, `UseGravity = false` 필수. 궤적이 코드(`ITrajectory`)로 매 프레임 제어되므로 물리 시뮬레이션이 개입하면 안 된다.

## 피처 간 의존

- **Skill Feature에 의해 트리거됨**: `ProjectileRequestedEvent`로 연결
- **Combat**: `DamageType` (Domain)
- **Shared**: EventBus, Float3, DomainEntityId, IClockPort
