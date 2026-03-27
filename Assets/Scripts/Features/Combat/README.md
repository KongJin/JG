# Combat Feature

대상 방어력 기반 데미지 계산과 데미지 적용 이벤트 발행을 담당한다.

## 현재 책임

- `DamageRule`로 최종 데미지를 계산한다.
- 타깃 포트를 통해 데미지를 적용한다.
- `DamageAppliedEvent`를 발행한다 (AttackerId 포함).
- `ICombatTargetProvider`를 통해 외부 피처(Player 등)가 데미지 파이프라인에 참여한다.

## 데이터 흐름

### 로컬 (공격자 클라이언트)

```text
ProjectileHitEvent
  → CombatBootstrap.OnProjectileHit()
    → CombatNetworkEventHandler.HandleProjectileHit()
      → (owner == local authority 인 경우만) ApplyDamageUseCase.Execute()
        → ICombatTargetPort.GetDefense / ApplyDamage
        → ICombatNetworkCommandPort.SendDamage (RPC 전파)
        → DamageAppliedEvent 발행
          → PlayerDamageEventHandler → PlayerHealthChangedEvent / PlayerDiedEvent
```

### 원격 (피격자/관전자 클라이언트)

```text
PlayerNetworkAdapter.RPC_ApplyDamage
  → PlayerNetworkEventHandler.HandleRemoteDamaged()
    → DamageReplicatedEvent 발행
      → CombatReplicationEventHandler
        → ApplyDamageUseCase.ExecuteReplicated() (방어력 재계산 없이 적용)
          → DamageAppliedEvent 발행
            → PlayerDamageEventHandler → PlayerHealthChangedEvent / PlayerDiedEvent
```

### 단일 경로 원칙

- 사망은 별도 RPC 없이 데미지 replication 경로로 전달된다 (`SendDeath` 미사용).
- `PlayerDamageEventHandler`가 `_deathPublished` 플래그로 `PlayerDiedEvent` 중복 발행을 방지한다.
- 리스폰 시 `PlayerRespawnedEvent`로 플래그가 리셋된다.

**NOTE:** `CombatTestTargetLoop` (테스트용)은 삭제됨. 리스폰 기능이 필요하면 Application 레이어에 별도 핸들러를 만들어야 함.

## 레이어 메모

- **Domain**: `DamageType`, `DamageRule`, `CombatTarget`
- **Application**: `ApplyDamageUseCase`, `CombatNetworkEventHandler`, `CombatReplicationEventHandler`, `ICombatTargetPort`, `ICombatTargetProvider`, `ICombatNetworkCommandPort`, `DamageAppliedEvent`, `DamageReplicatedEvent`
- **Infrastructure**: `CombatTargetAdapter` (ICombatTargetPort 구현, ICombatTargetProvider 기반 딕셔너리)
- **Presentation**: `CombatTargetView` (데미지 반응/피격 피드백)
- **Bootstrap**: `CombatBootstrap` (조립, 이벤트 구독, `RegisterTarget` API) - 피처 루트에 위치

## 현재 구현 기준 결정

- 타깃 식별은 `EntityIdHolder`를 사용해 런타임 `DomainEntityId`를 공유한다.
- `ICombatTargetProvider`: Combat이 소유하는 인터페이스. 외부 피처가 구현하면 데미지 파이프라인에 참여 가능.
  - `CombatBootstrap.RegisterTarget(id, provider)`로 등록
  - 기존 Inspector CombatTarget은 내부 `CombatTargetWrapper`로 래핑

## 피처 간 의존

- **Projectile**: `ProjectileHitEvent` (트리거)
- **Player**: `PlayerCombatTargetProvider`가 `ICombatTargetProvider` 구현
- **Player**: `PlayerCombatNetworkPortAdapter`가 `ICombatNetworkCommandPort` 구현
- **Shared**: `EventBus`, `Result`, `EntityIdHolder`, `DomainEntityId`
