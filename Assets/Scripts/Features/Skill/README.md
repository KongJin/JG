# Skill Feature

스킬 입력, 마나 검증, 네트워크 RPC 전송, RPC 수신 후 연출 이벤트 발행을 담당한다.

## 현재 책임

- 스킬바 3슬롯 UI 초기화와 아이콘 표시
- 슬롯 입력 수신 (`RMB`, `Q`, `E`)
- 덱 순환: 카탈로그 전체 스킬로 덱 구성 → 초기 핸드 3장 드로우 → 시전 시 자동 버리기/드로우
- 웨이브 보상 스킬 추가: `SkillRewardAdapter`가 `ISkillRewardPort`를 구현하여 덱에 없는 스킬 후보를 제공하고, 선택된 스킬을 덱 버린 더미에 추가
- 시전 시 마나 검증 후 `SkillCastNetworkData` 전송
- RPC 수신 후 `ProjectileRequestedEvent`, `ZoneRequestedEvent`, `TargetedRequestedEvent`, `SelfRequestedEvent`, `SkillCastedEvent` 발행
- 스킬별 이펙트 매핑 (`SkillPresentationData` SO 기반)
- 시전 사운드는 `SoundRequestEvent`를 발행하여 `Shared/Runtime/Sound/SoundPlayer`에 위임

## 핵심 흐름

```text
SlotInputHandler
  -> CastSkillUseCase
    -> SkillNetworkAdapter.SendSkillCasted(RPC All)
      -> SkillNetworkAdapter.RPC_SkillCasted
        -> SkillNetworkEventHandler
          -> Requested 이벤트 발행 (SkillId 포함)
          -> SkillCastedEvent 발행
```

현재 구현은 "로컬에서도 RPC를 한 번 타고 돌아온 결과를 이벤트로 해석한다"는 방식이다.
즉 연출은 `SkillNetworkEventHandler`가 발행한 이벤트를 기준으로 움직인다.

## 데이터 드리븐 구조

### ScriptableObject

- `SkillData` (SO) — 스킬 하나의 게임플레이 설정
  - Identity: `skillId` (고정 문자열, 모든 클라이언트에서 동일)
  - Presentation: `SkillPresentationData` SO 참조
  - Spec: `damage`, `manaCost`, `range`, `duration`, `projectileCount`
  - Delivery: `deliveryType` enum + Projectile 전용 필드 (`trajectoryType`, `hitType`, `speed`, `radius`)
  - Status Effect: `hasStatusEffect`, `statusType`, `statusMagnitude`, `statusDuration`, `statusTickInterval`
  - `ToDomain()` 메서드로 Domain `Skill` 엔티티 생성 (StatusPayload 포함)

- `SkillPresentationData` (SO) — 스킬 하나의 연출/UI 리소스
  - UI: `displayName`, `description`, `icon`
  - Effects: `castEffectPrefab`, `castSound`
  - 밸런스 조정과 연출 수정이 분리된다

- `SkillCatalogData` (SO) — `SkillData[]` 목록
  - Inspector에서 스킬 목록을 관리한다
  - 스킬 추가/수정 시 코드 변경 불필요

### 런타임 레지스트리

- `SkillCatalog` (클래스) — `SkillCatalogData`를 받아 런타임 딕셔너리 구성
  - `Get(skillId)` → Domain `Skill` 반환
  - `GetData(skillId)` → `SkillData` SO 반환
  - `GetPresentationData(skillId)` → `SkillPresentationData` SO 반환 (아이콘/이펙트/사운드 조회용)
  - 중복 ID 경고 처리

## 주요 클래스

### Bootstrap

- `SkillSetup` (피처 루트에 위치)
  - SkillBarCanvas 프리팹에 부착되는 조립용 컴포넌트
  - Inspector 연결 필드는 `[Required, SerializeField]`로 선언한다
  - `SkillCatalogData`를 `[Required, SerializeField]`로 Inspector에서 연결한다
  - `Initialize(EventBus, Transform, Camera, CasterId, IManaPort)`에서 `SkillCatalog` 생성 → `BarView`, `SkillCastEffectSpawner`, `SkillNetworkEventHandler` 초기화
  - 카탈로그 전체 스킬 ID로 `Deck`을 구성하고 초기 핸드를 드로우하여 3슬롯에 장착
  - `DeckCycleHandler`를 생성하여 시전 시 자동 덱 순환 (버리기 → 드로우 → 재장착)
  - `SwapSkill(slotIndex, skillId)`: 런타임 스킬 교체 API
  - `Deck`, `Bar`, `Catalog` 프로퍼티를 노출하여 외부(GameSceneBootstrap)에서 `SkillRewardAdapter` 생성에 사용

- `SkillIconAdapter` — `ISkillIconPort` 구현, `SkillCatalog.GetPresentationData()`에서 아이콘 조회
- `SkillEffectAdapter` — `ISkillEffectPort` 구현, `SkillCatalog.GetPresentationData()`에서 이펙트 프리팹 조회

### Application

- `CastSkillUseCase`
  - `ManaRule`로 시전 가능 여부 검사 (마나 부족 시 실패)
  - `IManaPort.TrySpendMana()`로 마나 차감
  - Extend 상태효과 적용 시 마나 비용 감소: `manaCost *= 1f / (1f + extendMag)`
  - Count 상태효과 적용 시 `GrowthRule.CalculateCount()`로 발사/스폰 수 증가
  - `Delivery.Deliver()` 결과에서 `DeliveryType`을 직접 가져온다
  - `SkillCastNetworkData`를 만들어 `ISkillNetworkCommandPort`로 전송

- `EquipSkillUseCase`
  - 스킬바 슬롯에 스킬을 장착한다
  - `SkillEquippedEvent`를 발행한다

- `DeckCycleHandler`
  - `SkillCastedEvent` 구독 → 시전된 스킬을 덱에 버리기 → 다음 스킬 드로우 → 같은 슬롯에 재장착
  - 로컬 시전자만 처리 (`CasterId` 필터링)
  - Bootstrap에서 `Func<string, Skill>` 형태의 lookup을 주입받아 Application→Infrastructure 의존을 피한다

- `SkillNetworkEventHandler`
  - `SkillCastNetworkData`를 받아 이벤트 버스로 변환한다
  - `DeliveryType` enum에 따라 아래 이벤트를 발행한다
    - `ProjectileRequestedEvent` — count > 1이면 팬 스프레드로 다중 발사
    - `ZoneRequestedEvent` (SkillId 포함) — count > 1이면 링 배치로 다중 스폰 (반경 = range * 0.3f)
    - `TargetedRequestedEvent` (SkillId 포함)
    - `SelfRequestedEvent` (SkillId 포함)
  - 마지막에 `SkillCastedEvent`를 발행한다

- `Ports/ISkillNetworkCommandPort`, `ISkillNetworkCallbackPort` — 네트워크 송수신 포트
- `Ports/IManaPort` — 마나 조회/차감 포트 (구현은 Player/Application/ManaAdapter)

- `SkillRewardAdapter`
  - `ISkillRewardPort` (Wave/Application/Ports에 정의) 구현
  - `DrawCandidates(count)`: 카탈로그에서 현재 덱(DrawPile + DiscardPile + SkillBar)에 없는 스킬을 셔플하여 count개 반환
  - `AddToDeck(skillId)`: `Deck.AddToDiscardPile()`로 스킬 추가

### Domain

- `Deck` — 뽑기/버리기 덱. 시전한 스킬은 버린 더미로, 뽑을 더미가 비면 셔플하여 재사용. `AddToDiscardPile()`로 웨이브 보상 스킬을 런타임에 추가 가능. `DrawPileIds`/`DiscardPileIds`로 현재 덱 내용물 조회 가능
- `SkillBar` — 3슬롯 스킬바. `Equip(slotIndex, skill)`, `GetSkill(slotIndex)`
- `SkillSpec` — 스킬 스펙 VO: `Damage`, `ManaCost`, `Range`, `Duration`, `ProjectileCount`, `StatusPayload`
- `ManaRule` — 시전 가능 여부 검사: 마나가 충분한지 확인

### Infrastructure

- `SkillNetworkAdapter` (`MonoBehaviourPun`) — RPC 송수신

### Presentation

- `ISkillIconPort` — 스킬 아이콘(Sprite) 조회 포트 (Unity 타입이므로 Presentation에 배치)
- `ISkillEffectPort` — 스킬 이펙트(GameObject) 조회 포트

- `SlotInputHandler`
  - 입력 액션을 바인딩하고 시전 요청을 보낸다
  - `Initialize()`에서 전달받은 플레이어 트랜스폼/카메라를 사용해 시전 origin과 조준 방향을 계산한다
  - `Result.Failure`는 `UiErrorRequestedEvent(Banner)`로 씬 공통 에러 UI에 전달한다
  - `TargetedDelivery`는 마우스 레이캐스트로 유효 타겟을 찾지 못하면 시전되지 않는다

- `BarView` — `SkillEquippedEvent` 구독, 슬롯 아이콘 표시
- `SlotView` — 개별 슬롯 UI 렌더링 (쿨다운 오버레이 제거됨)
- `SkillCastEffectSpawner` — 이벤트의 SkillId로 이펙트 프리팹 스폰 + `SoundRequestEvent` 발행
- `SelfCastEffect`, `TargetedCastEffect` — 캐스트 이펙트 연출

## 네트워크 데이터

`SkillCastNetworkData`는 다음 정보를 담는다.

- `SkillId`, `CasterId`, `SlotIndex`
- `Damage`, `Duration`, `Range`, `ProjectileCount`
- `DeliveryType` (enum)
- `TrajectoryType`, `HitType`
- `Speed`, `Radius`
- `Position` (Float3), `Direction` (Float3)
- `TargetPosition` (Float3, targeted 연출/판정용)
- `StatusPayload` (HasEffect, Type, Magnitude, Duration, TickInterval)

Position/Direction/TargetPosition에 `Float3`를 사용해 XYZ를 묶었다.
RPC 전송 시 Infrastructure에서 개별 float로 분해하고, 수신 시 다시 `Float3`로 조립한다.

## JG_GameScene 기준 조립 상태

`JG_GameScene`에는 다음이 배치되어 있다.

- `SkillBarCanvas` 프리팹 인스턴스
- 씬 오브젝트 `GameSceneBootstrap`
- `GameSceneBootstrap._skillSetup` 필드에 `SkillSetup` 참조
- `GameSceneBootstrap._projectileSpawner` 필드에 `ProjectileSpawner` 참조
- `_playerPrefabName = PlayerCharacter`

코드 기준 실제 연결은 아래와 같다.

- `GameSceneBootstrap.Start()`
  - 플레이어를 `PhotonNetwork.Instantiate()`로 생성
  - `ConnectLocalPlayer()`: 생성된 플레이어에서 `PlayerSetup.Initialize(eventBus)` 후 씬 등록
  - `_skillSetup.Initialize(_eventBus, player.transform, camera, playerId)`: 스킬 시스템 초기화 및 플레이어 트랜스폼/카메라/시전자 ID 전달
  - `_projectileSpawner.Initialize(_eventBus, _eventBus)`: 투사체 스포너 초기화
  - 원격 플레이어는 `PlayerSetup.RemoteArrived` 콜백으로 도착 시점에 연결되며 폴링은 사용하지 않음

## 현재 코드 기준 주의점

- `GameSceneBootstrap`이 `SkillSetup.Initialize(_eventBus, player.transform, camera, playerId)`를 호출하므로 스킬 시스템은 정상 초기화된다
- `SkillSetup.Initialize()`가 호출되지 않으면 입력 바인딩이 없어 스킬 사용 불가
- Inspector에서 `SkillCatalogData`를 연결해야 한다 — 없으면 초기화 중단

## 스킬 추가 방법

1. Unity에서 `Create > Skill > SkillPresentationData`로 연출 SO 생성
2. Inspector에서 displayName, icon, castEffectPrefab 등 연출 리소스 설정
3. Unity에서 `Create > Skill > SkillData`로 게임플레이 SO 생성
4. Inspector에서 ID, 스펙, 딜리버리, 상태이상 설정 + Presentation 필드에 위 SO 연결
5. `SkillCatalogData`의 `Skills` 배열에 추가
6. 코드 변경 불필요

## 피처 간 의존

- `Projectile`: `ProjectileRequestedEvent`, `ProjectileSpec`
- `Zone`: `ZoneRequestedEvent` (Skill이 발행, Zone이 구독)
- `Player`: `IManaPort` 구현체(`ManaAdapter`)를 Bootstrap에서 주입받음
- `Status`: `IStatusQueryPort`를 통해 Expand/Extend/Multiply/Count 상태 조회 → 스킬 발동 시 범위/데미지/발사수 수정, Extend는 마나 비용 감소 (선택적 의존, null이면 기본값 사용)
- `Shared`: `EventBus`, `DomainEntityId`, `Result`, `Float3`, `UiErrorRequestedEvent`, `SoundRequestEvent`

## 현재 문서 범위

이 문서는 현재 코드 구현을 기준으로 작성되었다.
설계 의도나 이후 리팩터링 방향이 아니라, 지금 실제로 존재하는 조립 경로와 책임만 기록한다.
