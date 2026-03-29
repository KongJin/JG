# Zone Feature

지속/범위형 스킬의 시각 영역을 스폰하고, 영역 내 엔티티에 틱 데미지와 상태효과를 적용하는 피처다.

## 현재 책임

- Skill 피처가 발행한 `ZoneRequestedEvent`를 해석한다.
- Zone 도메인 엔티티를 생성한다.
- Zone 프리팹 스폰과 수명 관리를 Zone 피처 내부에서 수행한다.
- 영역 내 엔티티를 감지하여 틱 간격으로 `ZoneTickEvent`를 발행한다.
- `ZoneTickEvent`에 `StatusPayload`를 포함하여 Status 피처의 `StatusTriggerHandler`가 상태효과를 적용할 수 있게 한다.

## 핵심 흐름

```text
Skill ZoneRequestedEvent (SkillSpec with StatusPayload)
  -> ZoneEventHandler (EventBus 직접 구독)
    -> SpawnZoneUseCase (range, cooldown, baseDamage, statusPayload)
      -> IZoneEffectPort.SpawnZone()
        -> ZoneEffectAdapter
          -> ZoneView (시각 연출 + 수명)
          -> ZoneCollisionDetector (틱 충돌 감지)
               -> ZoneTickEvent 발행 (baseDamage, statusPayload 포함)
                    -> StatusTriggerHandler가 StatusApplyRequestedEvent 발행
```

## 레이어 메모

- **Domain**: `Zone`, `ZoneSpec` (Radius, Duration, AnchorType, HitType, BaseDamage, StatusPayload), `ZoneAnchorType`, `ZoneHitType`
- **Application**: `SpawnZoneUseCase`, `ZoneEventHandler`, `IZoneEffectPort` (Application/Ports), `ZoneSpawnedEvent`, `ZoneTickEvent`
- **Bootstrap**: `ZoneSetup`, `ZoneEffectAdapter` (피처 루트에 위치). `ZoneSetup.Initialize(eventBus)`에서 `ZoneEffectAdapter`에 eventBus를 전달한다. 이벤트 핸들링은 `ZoneEventHandler`가 EventBus를 직접 구독한다.
- **Presentation**: `ZoneView` (이펙트 표현과 수명 관리), `ZoneCollisionDetector` (OnTriggerStay 기반 틱 감지)

## ZoneCollisionDetector

- `ZoneEffect.prefab`의 `CapsuleCollider (isTrigger=true)`를 활용하여 `OnTriggerStay`로 영역 내 엔티티 감지
- `EntityIdHolder`에서 `DomainEntityId` 조회
- 틱 간격(StatusPayload.TickInterval 또는 기본 0.5초)마다 `ZoneTickEvent` 발행
- 풀 리셋 시 상태 초기화 (`IPoolResetHandler`)

## 현재 구현 기준 결정

- Zone은 별도 네트워크 어댑터를 두지 않는다.
- Skill 피처가 이미 RPC 이후 `ZoneRequestedEvent`를 각 클라이언트에서 발행하므로,
  Zone은 그 이벤트를 받아 로컬 시각 연출과 충돌 감지를 수행한다.
- 위치 계산(`position + direction * (range * 0.5)`)과 `ZoneSpec` 생성은 `SpawnZoneUseCase` 내부에서 수행한다.
- Inspector 연결 필드는 `[Required, SerializeField]`로 선언해 씬/프리팹 저장 시 누락을 검증한다.

## 피처 간 의존

- **Skill**: `ZoneRequestedEvent` (SkillSpec에 StatusPayload 포함)
- **Status**: `StatusPayload` (Domain VO), `StatusTriggerHandler`가 `ZoneTickEvent`를 구독
- **Shared**: `EventBus`, `Result`, `Float3`, `IClockPort`, `DomainEntityId`
