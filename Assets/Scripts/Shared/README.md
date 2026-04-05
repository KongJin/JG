# Shared

Shared는 프로젝트 전역에서 재사용되는 공통 유틸리티와 계약만 둔다. 피처 전용 로직은 두지 않는다.

## 먼저 읽을 규칙

- 전역 구조와 Shared 경계: [architecture.md](../../../agent/architecture.md)
- Shared로 올리면 안 되는 패턴과 DDOL 예외: [anti_patterns.md](../../../agent/anti_patterns.md)
- 문서 소유권과 SSOT 운영 원칙: [work_principles.md](../../../agent/work_principles.md)

## Shared에 들어갈 수 있는 것

- 여러 feature가 함께 쓰는 순수 유틸리티
- 전역 계약 타입과 공통 이벤트 인프라
- 특정 feature 소유로 보기 어려운 공통 런타임 서비스
- 문서화된 예외를 가진 Shared 인프라 (`SoundPlayer.Instance` 같은 DDOL 서비스)

## Shared에 넣으면 안 되는 것

- 특정 feature 하나의 생명주기나 규칙에 묶인 코드
- cross-feature import를 피하려고 억지로 뽑은 추상화
- scene wiring 누락을 런타임에 복구하는 fallback 로직
- feature 전용 상태, 규칙, 화면 흐름

## 포함되는 공통 요소

- `EventBus` — 씬/피처 로컬 이벤트 발행·구독
- `Kernel` — `Result`, `DomainEntityId`, `EntityIdHolder` (MonoBehaviour, static registry로 `TryGet(id)` 위치 조회 제공) 같은 기본 타입
- `Math`, `Time` — 순수 유틸리티
- `Lifecycle` — EventBus/InputAction 같은 런타임 구독 해제 유틸
- `Attributes/RequiredAttribute` — Inspector 직렬화 필드 누락 검증용 어트리뷰트
- `Runtime/Pooling` — 공통 풀링, 반환, 수명 종료 유틸
- `ErrorHandling` / `Ui` — 공통 에러 표시 계약과 씬 공통 에러 프리젠터

## Inspector Validation

- Inspector에서 연결하는 직렬화 필드는 `[Required, SerializeField]`를 기본으로 사용한다.
- `RequiredFieldValidator`가 씬/프리팹 저장 시 `MonoBehaviour`의 필수 참조 누락을 검사한다.

## 공통 에러 UI

유저에게 보여줄 수 있는 실패는 `Shared/ErrorHandling/UiErrorMessage.cs`의
`UiErrorRequestedEvent`로 수렴한다.

### 분류

| 표시 타입 | 용도 |
|---|---|
| `Banner` | 유저가 계속 진행 가능한 recoverable 실패 |
| `Modal` | 현재 씬 진행이 막히는 fatal 실패 |

### 주요 타입

- `UiErrorMessage`
  - `Message`
  - `DisplayMode`
  - `SourceFeature`
  - `DurationSeconds`
  - `CanDismiss`
- `UiErrorRequestedEvent`
  - 씬 공통 에러 UI가 구독하는 이벤트
- `SceneErrorPresenter`
  - 같은 씬 `EventBus`를 구독해 Banner/Modal을 렌더링하는 `MonoBehaviour`
  - 모든 UI 참조는 `[Required, SerializeField]`로 프리팹/씬에서 연결 (런타임 생성 없음)
  - fatal modal은 씬에서 전체 화면 오버레이(`ErrorModalRoot`)로 배치하고, gameplay HUD 위에 렌더되도록 상위 정렬을 유지한다
  - dismiss 버튼 라벨은 씬 기본값으로 채워 둔다 (예: `OK`)
- `UiErrorResultBridge`
  - `Result` 실패를 `UiErrorRequestedEvent`로 바꿔 발행하는 작은 헬퍼

### 사용 규칙

- Presentation에서 `Result.Failure`를 즉시 사용자에게 보여줘야 할 때:
  - `UiErrorRequestedEvent(UiErrorMessage.Banner(...))` 발행
- Application/Infrastructure에서 비동기 실패를 사용자에게 알려야 할 때:
  - 같은 이벤트를 발행
- Inspector 미연결, null dependency, 불가능한 enum, 버그성 예외:
  - UI로 자동 노출하지 않고 `Debug.LogError/LogException`으로 남긴다

### 씬 조립

- 각 씬 Bootstrap이 자기 씬의 `SceneErrorPresenter.Initialize(eventBus)`를 호출한다
- 현재 구조는 씬별 `EventBus` 유지이며, 전역 `SceneContext`는 아직 도입하지 않았다

## Analytics

Firebase Analytics 연동. WebGL 빌드에서 JavaScript interop으로 전송한다.

### 주요 타입

- `IAnalyticsPort` — 세션, 플레이 루프, 이탈 지점, 핵심 행동 로깅 계약
- `FirebaseAnalyticsAdapter` — Firebase JS SDK 연동 구현체 (에디터에서는 Debug.Log 스텁)
- `AnalyticsParams` — 타입 안전 파라미터 빌더 (`int`, `float`, `string`만 허용)
- `RoundCounter` — 판 수 카운터 (로비 복귀 시 Reset, 게임 진입 시 Increment)

### 수집 항목

| 항목 | 메서드 | 설명 |
|---|---|---|
| 세션 | `LogSessionStart/End` | 접속~종료 시간 |
| 플레이 루프 | `LogGameStart/End` | 게임 시작~종료, 플레이 시간, 라운드 번호 |
| 이탈 지점 | `LogDropOff` | 어디서 나갔는지 (context + 경과 시간) |
| 핵심 행동 | `LogAction` | 게임별 커스텀 행동 (스킬 사용, 사망 등) |

### 사용법

각 피처 Bootstrap에서 `FirebaseAnalyticsAdapter`를 생성:

```csharp
var analytics = new FirebaseAnalyticsAdapter();
analytics.LogSessionStart();
```

핵심 행동 로깅 시 `AnalyticsParams`를 사용:

```csharp
analytics.LogAction("skill_used",
    new AnalyticsParams()
        .Add("slot_index", 2)
        .Add("skill_id", "fireball")
        .Build());
```

## Sound

이벤트 기반 사운드 재생 시스템. 피처가 `SoundRequestEvent`를 발행하면 `SoundPlayer`가 재생한다.

### Application-safe 타입 (`Shared/Sound/`)

Unity 타입 없음. 어떤 피처의 Application 레이어에서도 사용 가능.

- `PlaybackPolicy` — 재생 정책 enum (`All`, `LocalOnly`, `OwnerExcluded`)
- `SoundRequest` — 재생 요청 데이터 (soundKey, position, policy, ownerId, cooldownHint)
- `SoundRequestEvent` — EventBus 발행용 래퍼 struct

### Runtime 컴포넌트 (`Shared/Runtime/Sound/`)

- `SoundEntry` — `[Serializable]` 사운드 데이터 (AudioClip, volume, spatialBlend, cooldown)
- `SoundCatalog` — ScriptableObject, string key → SoundEntry 매핑
- `PooledAudioSource` — 오디오 프리팹 컴포넌트, `[Required, SerializeField]`로 AudioSource/LifetimeRelease 참조
- `SoundPlayer` — MonoBehaviour, EventBus 구독 → 정책 필터 → 쿨다운 중복 방지 → 풀링 재생

### 사용법

```csharp
// 피처의 Presentation/Application에서
publisher.Publish(new SoundRequestEvent(new SoundRequest(
    "skill_fireball_cast",
    position,
    PlaybackPolicy.All,
    casterId.Value)));
```

### Sound Key 규약

| 패턴 | 용도 |
|---|---|
| `skill_{skillId}_cast` | 스킬 시전 |
| `combat_hit_{damageType}` | 피격 |
| `combat_death` | 사망 |
| `projectile_{skillId}_hit` | 투사체 적중 |
| `zone_{skillId}_spawn` | 존 생성 |

### 씬 조립

- **단일 인스턴스 (DDOL):** `SoundPlayer`는 `JG_LobbyScene` 루트에 두고 Inspector에서 `SoundCatalog`·오디오 프리팹을 연결한다. `Awake`에서 `DontDestroyOnLoad` + `SoundPlayer.Instance`로 유지한다. `LobbyBootstrap`이 `_view.Initialize` **이전**에 `Initialize(lobbyEventBus, SoundPlayer.LobbyOwnerId)`를 호출한다.
- **게임 씬:** `JG_GameScene`에는 `SoundPlayer` 오브젝트를 두지 않는다. 씬 루트 `GameSceneRoot`가 로컬 플레이어 준비 후 `SoundPlayer.Instance.Initialize(gameEventBus, localPlayerId)`로 **EventBus·ownerId 재바인딩**한다. Wave/Status/Ticker 일부 시스템이 자식 GO로 분리되어 있어도 재바인딩 책임은 계속 루트 `GameSceneRoot`에 있다. `Instance == null`이면 로비를 거치지 않았거나 씬 배선 누락이다 (`Debug.LogError`). `PooledAudioSource` 같은 pooled runtime 인스턴스도 `JG_GameScene` asset에 저장하지 않는다.
- **로비 사운드 규약:** 로비에서는 `PlaybackPolicy.All`만 사용한다. `LocalOnly` / `OwnerExcluded`는 게임 도메인 `PlayerId`가 바인딩된 뒤(게임 씬)부터 의미가 있다.

오디오 프리팹에는 `AudioSource` + `PooledObject` + `LifetimeRelease` + `PooledAudioSource`를 붙인다. `Initialize` 재호출 시 `PooledObject`가 있는 자식만 파괴해 풀 누적을 막는다.

## Lifecycle 유틸

- `DisposableScope`
  - 여러 `IDisposable` 정리 작업을 한 곳에 모은다
- `EventBusSubscription.ForOwner(...)`
  - dispose 시 `eventBus.UnsubscribeAll(owner)`를 수행한다
- `InputActionSubscription.BindPerformed(...)`
  - `Enable + performed 등록`과 `Disable + 해제`를 한 쌍으로 관리한다

## Pooling 유틸

- `GameObjectPool`
  - prefab 단위 재사용 풀
- `PooledObject`
  - 자신이 어떤 pool에 속하는지 알고 `Release()`로 반환한다
- `LifetimeRelease`
  - 일정 시간 후 pool 반환 또는 fallback `Destroy`를 수행한다
- `IPoolResetHandler`
  - rent/return 시 상태 초기화가 필요한 컴포넌트 계약
