# Combat Feature

Combat 피처는 대상 방어력 기반 데미지 계산과 데미지 적용 이벤트 발행을 담당한다.

## 먼저 읽을 규칙

- 전역 구조, 레이어, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- Bootstrap 책임, EventHandler 위치, runtime lookup 예외: [anti_patterns.md](../../../../agent/anti_patterns.md)
- 이벤트 체인 방향과 직접 호출 판단: [event_rules.md](../../../../agent/event_rules.md)
- 이 피처의 scene wiring과 초기화 순서: 이 문서의 `## 씬 계약`

## 이 피처의 책임

- `DamageRule`로 최종 데미지를 계산한다.
- 타깃 포트를 통해 데미지를 적용한다.
- `DamageAppliedEvent`를 발행한다 (AttackerId, IsDowned 포함).
- `ICombatTargetProvider`를 통해 외부 피처(Player 등)가 데미지 파이프라인에 참여한다.
- `CombatTargetDamageResult`에 `IsDowned` 필드를 포함하여 다운 상태를 전파한다.

## 로컬 계약

- 실제 데미지 계산은 권한 있는 로컬 경로에서만 수행하고, 원격 클라이언트는 replication 경로만 재현한다.
- Friendly Fire 배율은 Combat가 적용하며, replicated path에서는 재계산하지 않는다.
- scene wiring과 직렬화 필드 계약은 아래 `## 씬 계약`을 따른다.

## 핵심 흐름

### 로컬 (공격자 클라이언트) — Projectile 경로

```text
ProjectileHitEvent (AllyDamageScale 포함)
  → CombatNetworkEventHandler (EventBus 직접 구독)
    → (owner == local authority 인 경우만) ApplyDamageUseCase.Execute(allyDamageScale)
      → ICombatTargetPort.GetDefense / ApplyDamage
      → Ally 관계 시 RelationshipRule 배율 × allyDamageScale 곱적용
      → ICombatNetworkCommandPort.SendDamage (RPC 전파)
      → DamageAppliedEvent 발행
        → PlayerDamageEventHandler → PlayerHealthChangedEvent / PlayerDownedEvent / PlayerDiedEvent
```

### 로컬 (공격자 클라이언트) — Zone 경로

```text
ZoneTickEvent (AllyDamageScale 포함)
  → ZoneDamageHandler (EventBus 직접 구독, DamageType.Magical 하드코딩)
    → (caster == local authority 인 경우만) ApplyDamageUseCase.Execute(allyDamageScale)
      → 동일한 데미지 파이프라인 (DamageRule, RelationshipRule, allyDamageScale)
      → DamageAppliedEvent 발행
```

### 원격 (피격자/관전자 클라이언트)

```text
PlayerNetworkAdapter.RPC_ApplyDamage
  → PlayerNetworkEventHandler.HandleRemoteDamaged()
    → DamageReplicatedEvent 발행
      → CombatReplicationEventHandler
        → ApplyDamageUseCase.ExecuteReplicated() (방어력 재계산 없이 적용)
          → DamageAppliedEvent 발행 (IsDowned 포함)
            → PlayerDamageEventHandler → PlayerHealthChangedEvent / PlayerDownedEvent / PlayerDiedEvent
```

### 단일 경로 원칙

- 사망은 별도 RPC 없이 데미지 replication 경로로 전달된다 (`SendDeath` 미사용).
- HP가 0이 되면 즉사가 아닌 **Downed** 상태로 전이한다 (`PlayerDownedEvent` 발행). Bleedout 만료 시 `PlayerDiedEvent` 발행.
- `PlayerDamageEventHandler`가 `_deathPublished` 플래그로 `PlayerDiedEvent` 중복 발행을 방지한다.
- 리스폰/구조 시 `PlayerRespawnedEvent`/`PlayerRescuedEvent`로 플래그가 리셋된다.
- 게임 종료 UI는 Player 피처의 `GameEndEventHandler`가 `PlayerDiedEvent`를 받아 처리한다.

**NOTE:** `CombatTestTargetLoop` (테스트용)은 삭제됨. 리스폰 기능이 필요하면 Application 레이어에 별도 핸들러를 만들어야 함.

## 레이어 메모

- **Domain**: `DamageType`, `DamageRule`, `CombatTarget`, `RelationshipType`, `RelationshipRule`
- **Application**: `ApplyDamageUseCase` (string→DomainEntityId 변환 오버로드 포함, 옵셔널 `IFriendlyFireScalingPort` 주입), `CombatNetworkEventHandler`, `CombatReplicationEventHandler`, `ZoneDamageHandler` (ZoneTickEvent 구독 → ApplyDamageUseCase 경유), `FriendlyFireScalingAdapter` (`IFriendlyFireScalingPort` 구현, `WaveCountdownStartedEvent` + `WaveStartedEvent` + `WaveHydratedEvent` 구독하여 웨이브별 FF 배율 제공 — 카운트다운 시점부터 동기화, late-join/master-switch 포함), `ICombatTargetPort`, `ICombatTargetProvider` (`CombatTargetDamageResult.IsDowned` 포함), `ICombatNetworkCommandPort`, `IEntityAffiliationPort`, `IFriendlyFireScalingPort`, `DamageAppliedEvent` (`IsDowned` 포함), `DamageReplicatedEvent`, `FriendlyFireAppliedEvent`
- **Infrastructure**: `CombatTargetAdapter` (ICombatTargetPort 구현, ICombatTargetProvider 기반 딕셔너리)
- **Presentation**: `CombatTargetView` (데미지 반응/피격 피드백), `FriendlyFireFeedbackView` (아군 피격 경고/피드백), `DamageNumberSpawner` (DamageAppliedEvent 구독 → EntityIdHolder 위치에 월드 플로팅 텍스트 스폰), `DamageNumberView` (개별 플로팅 숫자 애니메이션 + 자동 파괴)
- **Bootstrap**: `CombatBootstrap` (조립, `RegisterTarget` API) - 피처 루트에 위치. 이벤트 핸들링은 `CombatNetworkEventHandler`/`CombatReplicationEventHandler`가 EventBus를 직접 구독한다.

## 프렌들리 파이어 (Friendly Fire)

- `RelationshipType` (Self/Ally/Enemy) + `RelationshipRule`로 데미지 배율을 결정한다.
  - Self = 0× (자기 피해 차단), Ally = 0.5×, Enemy = 1×
- `IEntityAffiliationPort`가 공격자-피격자 관계를 조회한다. 구현체는 Player/Infrastructure의 `EntityAffiliationAdapter`.
  - 같은 ID → Self, 둘 다 "player-" 프리픽스 → Ally, 그 외 → Enemy
- `ApplyDamageUseCase.Execute(allyDamageScale)`에서 관계 배율을 적용한 후 데미지를 전파한다. Ally 관계일 때 `baseMultiplier * allyDamageScale`을 곱적용한다.
  - `baseMultiplier`: `IFriendlyFireScalingPort`가 주입되면 웨이브별 동적 배율 사용 (wave < 3: 0.5, wave ≥ 3: 0.375 — 0-based index, 플레이어 웨이브 4부터 감소), 없으면 `RelationshipRule` 폴백 (0.5)
  - Ally 관계일 때 `FriendlyFireAppliedEvent` 추가 발행
- `ExecuteReplicated()`에서는 이미 계산된 데미지이므로 배율 재적용 안 함. 관계 조회하여 `FriendlyFireAppliedEvent`만 발행.
- `FriendlyFireFeedbackView` (Presentation): 로컬 플레이어가 공격자/피격자일 때 피드백 표시.

## 씬 계약

### Required Serialized References (CombatBootstrap)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_targetAdapter` | `CombatTargetAdapter` | 데미지 타깃 관리 |
| `_targetViews` | `CombatTargetView[]` | 피격 시각 피드백 |
| `_friendlyFireFeedbackView` | `FriendlyFireFeedbackView` | 아군 피격 경고/피드백 (선택) |

### Required Serialized References (FriendlyFireFeedbackView)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_bannerPanel` | `GameObject` | 배너 패널 (SetActive로 표시/숨김) |
| `_bannerText` | `Text` | 배너 메시지 텍스트 |

FriendlyFireFeedbackView는 `JG_GameScene`의 `UIRoot/FFBannerCanvas`에 배치한다(ScreenSpace-Overlay, sortingOrder=100). 내부에 Panel + Text 자식 구조.

### Required Serialized References (DamageNumberSpawner)

| 필드 | 타입 | 용도 |
|---|---|---|
| `damageNumberPrefab` | `GameObject` | 대미지 숫자 프리팹 (DamageNumberView + World-Space Canvas + CanvasGroup) |

`DamageNumberSpawner`는 `JG_GameScene`의 `GameSceneRoot/CombatSystems` 아래에 두고, `GameSceneRoot`에서 선택 연결한다. `Initialize(eventBus)` 호출 후 `DamageAppliedEvent`를 구독하여 `EntityIdHolder.TryGet(targetId)` 위치에 프리팹을 Instantiate한다.

### Runtime-created objects
- `DamageNumberView` 인스턴스: `DamageNumberSpawner`가 `DamageAppliedEvent`마다 프리팹에서 Instantiate. lifetime(0.8초) 후 자동 Destroy.

### 초기화 순서

1. `GameSceneRoot`가 `FriendlyFireScalingAdapter` 생성 (Wave 모드일 때) → `CombatBootstrap.Initialize(eventBus, networkPort, localAuthorityId, affiliation, ffScaling)` 호출
2. `CombatBootstrap`이 내부에서:
   - `CombatTargetAdapter.Initialize()`
   - `ApplyDamageUseCase` 생성 (`IEntityAffiliationPort` 필수 주입)
   - `CombatNetworkEventHandler` 생성 (EventBus 직접 구독)
   - `CombatReplicationEventHandler` 생성 (EventBus 직접 구독)
   - `ZoneDamageHandler` 생성 (EventBus 직접 구독, localAuthorityId로 권한 필터)
   - `CombatTargetView[]` 초기화
   - `FriendlyFireFeedbackView.Initialize(subscriber, publisher, localPlayerId)` 호출
3. 이후 `GameSceneRoot`가 `CombatBootstrap.RegisterTarget()`으로 플레이어/적 등록

### Late-join / Reconnect 동작

- **관계 판정**: `EntityAffiliationAdapter`는 ID 프리픽스 기반 순수 계산이므로 상태 의존 없음. late-join에 영향 없음.
- **FF 배율**: `FriendlyFireScalingAdapter`가 `WaveHydratedEvent`도 구독하므로, late-join/master-switch 시 `ForceState()`를 통해 올바른 웨이브 인덱스를 수신한다. `ApplyDamageUseCase.Execute()`에서 매 호출마다 관계를 조회하므로 동기화 이슈 없음.
- **FF 이벤트 재생**: `FriendlyFireAppliedEvent`는 로컬 UI 피드백 전용. 네트워크로 전파되지 않으며, late-join 클라이언트에 과거 FF 이벤트를 재생하지 않음 (의도적 — 과거 피드백은 의미 없음).
- **Replicated path**: `ExecuteReplicated()`는 이미 계산된 최종 데미지를 받으므로 FF 배율을 재적용하지 않음. `FriendlyFireAppliedEvent`만 로컬 피드백용으로 발행.
- **Host/Client 비대칭**: 없음. `FriendlyFireScalingAdapter`가 `WaveCountdownStartedEvent`를 구독하여 카운트다운 시작 시점부터 Master/Client 모두 동일한 웨이브 인덱스를 사용한다. 공격자 클라이언트가 `Execute()`로 배율 적용 + RPC 전파, 피격자 클라이언트가 `ExecuteReplicated()`로 수신. 양쪽 모두 로컬 `FriendlyFireAppliedEvent`를 독립적으로 발행.

## 현재 구현 기준 결정

- 타깃 식별은 `EntityIdHolder`를 사용해 런타임 `DomainEntityId`를 공유한다.
- Player는 `StablePlayerId`를 사용해 모든 클라이언트에서 동일한 타깃 ID를 유지한다.
- Inspector에서 연결하는 직렬화 필드는 `[Required, SerializeField]`로 선언해 씬/프리팹 저장 시 누락을 검증한다.
- `ICombatTargetProvider`: Combat이 소유하는 인터페이스. 외부 피처가 구현하면 데미지 파이프라인에 참여 가능.
  - `CombatBootstrap.RegisterTarget(id, provider)`로 등록
  - 기존 Inspector CombatTarget은 내부 `CombatTargetWrapper`로 래핑
  - Phase 3 이후 **Player, Enemy, ObjectiveCore, BattleEntity** 모두 `ICombatTargetProvider`로 등록됨

## 피처 간 의존

- **Projectile**: `ProjectileHitEvent` (트리거)
- **Zone**: `ZoneTickEvent` (Zone 데미지 트리거, AllyDamageScale 포함. DamageType은 ZoneDamageHandler가 Magical 하드코딩)
- **Player**: `PlayerCombatTargetProvider`가 `ICombatTargetProvider` 구현
- **Player**: `EntityAffiliationAdapter`가 `IEntityAffiliationPort` 구현 (ID 프리픽스 기반 소속 판정 — Phase 3에서 BattleEntity(`battle-`), Enemy(`enemy-`), Core(`objective-core`) 관계 판정 확장)
- **Wave**: `WaveCountdownStartedEvent`, `WaveStartedEvent`, `WaveHydratedEvent` (`FriendlyFireScalingAdapter`가 웨이브 인덱스 추적에 사용 — 카운트다운 시점부터 동기화 + late-join/master-switch)
- **Shared**: `EventBus`, `Result`, `EntityIdHolder` (static registry: `TryGet(id)` — 대미지 숫자 위치 조회), `DomainEntityId`, `SoundRequestEvent`
