# Shared

프로젝트 전역에서 재사용되는 공통 유틸리티와 계약을 둔다.
피처 전용 로직은 두지 않는다.

## 포함되는 공통 요소

- `EventBus` — 씬/피처 로컬 이벤트 발행·구독
- `Kernel` — `Result`, `DomainEntityId` 같은 기본 타입
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

```csharp
// GameSceneBootstrap에서
_soundPlayer.Initialize(eventBus, localPlayerId.Value);
```

SoundPlayer GameObject에 `SoundCatalog`와 오디오 프리팹을 Inspector에서 연결한다.
오디오 프리팹에는 `AudioSource` + `PooledObject` + `LifetimeRelease` + `PooledAudioSource`를 붙인다.

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
