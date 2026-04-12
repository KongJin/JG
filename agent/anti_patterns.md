# /agent/anti_patterns.md

## 하지 말아야 할 것

이 문서는 구조를 새로 정의하지 않는다. 구조 정의는 `architecture.md`를 따르고, 여기서는 구현 중 금지 패턴과 예외 판단만 다룬다.

다음 패턴은 금지한다:

* View 또는 InputHandler 내부에 비즈니스 로직 배치.
* Domain 내부에 네트워크 로직 배치.
* Domain 내부에 Unity API 사용.
* Shared 내부에 피처 전용 코드 배치.
* Bootstrap이 god class로 전환.
* 하나의 port가 무관한 여러 행위를 담당.
* architecture.md에 정의되지 않은 아키텍처 레이어 도입.

* 의존성 필드 규칙 — 의존성 필드 멤버 3가지 규칙 (`[SerializeField]`, 주입된 참조):
  1. **한 번에 수신**: constructor 또는 `Initialize()`에서만. 별도 setter 메서드 금지 (예: `SetXxx()`).
  2. **올바른 레이어에서 검증**: `[Required, SerializeField]` 필드는 Editor가 씬/프리팹 저장 시 검증하므로 런타임 null-check 불필요. `Initialize()` 파라미터 등 Inspector에서 검증할 수 없는 값만 런타임에 `Debug.LogError`로 검증한다.
  3. **초기화 후 신뢰**: 이벤트 핸들러, Update, 기타 런타임 메서드에서 재검증 금지. 런타임에 null이면 초기화 버그 — NullReferenceException이 표면화되도록 둔다.
* `[Required]` 범위 규칙 — `[Required]`는 Inspector에서 연결되는 **참조 의존성**에만 사용한다. `bool`, `int`, `float`, `enum`, `Color`, `Vector*`, `string` 같은 스칼라/config 값에는 붙이지 않는다. 스칼라 값 검증은 `Range`, `Min`, 도메인 검증, 전용 validator로 처리한다.
* 타입 enum 기반 조건 분기 — Factory + Strategy 패턴을 사용한다. switch는 명령 디스패치와 단순 값 매핑에만 허용.
* Strategy 패턴 파일 구조 — enum, interface, factory는 한 파일에. 각 구현체(Strategy 클래스)는 개별 파일.

* 이중 상태 — 동일한 개념(체력, 위치 등)을 두 개의 도메인 엔티티가 독립적으로 관리해서는 안 된다. Single Source of Truth는 하나여야 한다. 예: Player.CurrentHp와 CombatTarget.CurrentHealth가 동시에 존재하면 필어긋남.
* 네트워크 공유 엔티티 로컬 ID — 네트워크로 공유되는 엔티티(플레이어 등)의 ID를 `DomainEntityId.New()`로 로컬 생성하면 안 된다. 반드시 네트워크 안정 소스(Photon ActorNumber, ViewID 등)에서 파생해야 한다. `DomainEntityId.New()`는 한 클라이언트 안에서만 존재하는 엔티티(투사체, 스킬 인스턴스 등)에만 사용한다.
* 이중 경로 데미지 — 단일 이벤트에 대해 데미지를 두 번 적용하지 않는다 (예: 피격탄 명중). 하나의 UseCase가 데미지를 계산; 다른 feature는 결과 이벤트로 반응.
* 제공자 대신 소비자 측 port — Feature A가 Feature B를 호출할 때, port 인터페이스는 A의 Application에 정의한다. 구현은 B에 — 외부 의존(Photon, DB, SDK)이 있으면 B의 Infrastructure, 순수 도메인 상태 조회/계산이면 B의 Application이 직접 구현해도 된다. (Dependency Inversion Principle)
* Bootstrap 내 선택/순환 로직 — Bootstrap은 wiring만 담당. 스킬 순환, 다음 타깃 선택 등은 전용 Application 레이어 클래스로 분리.
* Application 내 Unity 타입 — Sprite, GameObject, AudioClip, Color, Debug.Log/LogWarning/LogError, 기타 UnityEngine API를 Application 레이어 이벤트, port, UseCase에 넣지 않는다. Unity 타입이 필요하면 Presentation에 둔다. 로깅은 Bootstrap/Infrastructure에서 담당.
* 단일 호출 private 함수 — 호출 지점이 하나뿐인 private 메서드는 추출하지 않는다. 호출부에 직접 인라인한다. 읽는 사람이 한 함수 안에서 흐름을 따라갈 수 있어야 한다. 예외: (1) 콜백/이벤트 핸들러로 등록되는 함수, (2) 두 곳 이상에서 호출되는 함수.
* 타입명 충돌 — 한 feature 안에서 같은 short type name의 `MonoBehaviour`를 두 개 만들지 않는다. Presentation 컴포넌트 이름도 feature 전체에서 유일해야 한다.
* 네임스페이스-클래스명 충돌 — 피처 네임스페이스와 동일한 short name의 도메인 엔티티를 생성하지 않는다. 예: `Features.Account` 네임스페이스 아래 `Account` 클래스를 두면 C#이 `Account`를 타입이 아닌 네임스페이스로 해석하여 `CS0118` 컴파일 에러가 발생한다. 대신 `AccountProfile`, `AccountEntity` 등 접미사를 붙인다. (실제 사례: `Account.cs` → `AccountProfile.cs` 리네임)
* Feature short-type shadowing — feature namespace 이름과 같은 short type (`Unit`, `Player`, `Wave` 등)을 bare identifier로 사용해 namespace/type 충돌을 유발하지 않는다. alias 또는 fully-qualified name을 사용한다.
* 정적 이벤트 규칙 — 정적 이벤트는 엔진/네트워크 콜백을 Application 또는 Bootstrap으로 bridge할 때만 예외적으로 허용한다. gameplay event bus 대체제로 사용하지 않는다. 사용 시 `OnDestroy` 해제와 README 명시는 필수다.
* 운영 환경 런타임 UI 생성 — 운영용 UI는 scene 소유 또는 prefab 소유여야 한다. 런타임 UI 생성은 디버깅 도구 또는 일시적 마이그레이션에서만 허용한다.
* 완전 switch에서 암묵적 폴백 — enum을 switch로 분기할 때, default에서 폴백 값을 반환하지 않는다. 새 enum 값 추가 시 컴파일러가 경고하지 않으므로, default는 `throw new ArgumentOutOfRangeException()`으로 즉시 실패시킨다. `Debug.LogError` + 폴백 반환은 문제를 숨긴다. (실제 사례: SkillData.CreateDelivery에서 default가 SelfDelivery를 반환하여 새 DeliveryType 추가 시 silent corruption 가능성이 있었음)
* Phantom shared 계약 — Shared에 실제 선언되지 않은 계약 이름을 가정해서 쓰지 않는다. 예: `IEventBus`를 실제 선언 없이 새 공용 계약처럼 사용하는 것 금지. Shared 계약은 실제 선언 파일을 기준으로 참조한다.
* 심볼 이동 후 import 누락 — `RequiredAttribute`, `GarageRoster`, `StatusNetworkAdapter`, `Func<>` 같이 자주 이동하거나 namespace가 분명한 심볼은 사용 시 필요한 `using` 또는 fully-qualified name을 명시한다. IDE가 알아서 잡아줄 것이라 가정하지 않는다.
* 이벤트 계약 drift — 이벤트 producer/consumer/bridge가 서로 다른 필드 집합을 가정하는 상태를 허용하지 않는다. 이벤트 payload 변경 시 producer, consumer, bridge를 함께 검토한다. 예: `GameEndEvent`에 없는 `IsLocalPlayerDead`를 consumer가 계속 참조하는 상태.
* Concrete/interface drift — 한쪽은 interface, 한쪽은 concrete 구현체를 요구해 wiring이 깨지는 상태를 허용하지 않는다. Bootstrap만 concrete를 직접 조립하고, 그 밖의 레이어는 최소 계약을 받는다. 예: `EventBus` / `IEventPublisher` 가정이 엇갈려 `SummonPhotonAdapter` wiring이 깨지는 상태.
* Domain 내 scene 보조 타입 — `PlacementArea`처럼 Unity 의존 scene helper, 입력 판정 helper, 시각화 보조 타입을 Domain에 두지 않는다. 이런 타입은 Presentation 또는 scene 소유 계약 쪽에 둔다.
* Subscribe 반환값 가정 — `Subscribe()`의 반환형을 임의로 `IDisposable`처럼 취급하지 않는다. EventBus ownership 해제는 `EventBusSubscription.ForOwner(...)` 또는 명시적 cleanup으로 처리한다.
* 리팩터링 후 오래된 심볼 — 리팩터링 전 필드/메서드/타입명을 계속 참조하는 상태를 허용하지 않는다. 변경 후에는 반드시 컴파일을 실행하고 모든 참조가 수정되었음을 0 errors로 확인한다.
* 시그니처 변경 시 호출부 전수 조사 — public/protected 메서드 시그니처 변경 시 반드시 모든 호출부를 먼저 식별한다. 하위 호환이 필요하면 선택 인자나 오버로드로 처리하고, 모든 호출부를 한 번에 수정한다.

---

## 리팩터링 교훈

아래 패턴은 리팩터링 과정에서 확인된 운영 규칙이며, 새 구현에도 그대로 따른다.

### 1. 타입 의존에 따른 Port 배치
**규칙:** Unity 타입을 사용하지 않는 port 인터페이스만 Application/Ports에 둔다. UnityEngine 타입(Sprite, GameObject, AudioClip, Color 등)을 참조하는 port는 Presentation에 둔다.
- ✅ `Application/Ports/IZoneEffectPort.cs` — `Float3`만 사용 (Shared), Application 배치 가능
- ✅ `Presentation/ISkillEffectPort.cs` — `GameObject`, `AudioClip` 사용 (Unity), Presentation 배치 필수
- ✅ `Presentation/ISkillIconPort.cs` — `Sprite` 사용 (Unity), Presentation 배치 필수
- ❌ `Application/Ports/ISkillEffectPort.cs` — Application 레이어에 Unity 타입 포함은 레이어 규칙 위반

**이유:** Application 레이어는 UnityEngine에 의존해서는 안 된다. Unity 타입 port를 Application으로 옮기면 이를 참조하는 모든 Application 클래스가 "감염"된다.

### 2. 이벤트 핸들러 → Application 레이어
**규칙:** 이벤트 처리 로직은 Bootstrap이 아닌 Application 레이어에 둔다.
- ❌ Bootstrap에서 `OnXxxEvent()` 메서드로 이벤트를 직접 구독·처리 — 잘못됨
- ✅ Application EventHandler가 생성자에서 EventBus를 직접 구독, Bootstrap은 생성 + `DisposableScope` 수명 관리만 — 올바름

**이유:** Bootstrap은 컴포넌트 연결만 담당해야 한다. 이벤트 처리는 Application에 속하는 비즈니스 로직을 포함한다.

**패턴:**
```
<Feature>Setup.cs / <Feature>Bootstrap.cs (wiring 전용)
  → Application EventHandler 생성 (생성자에 IEventSubscriber 주입)
  → EventHandler가 생성자에서 구독
  → Bootstrap이 소유권 추적: _disposables.Add(EventBusSubscription.ForOwner(eventBus, handler))
```

### 3. Bootstrap 책임
**Bootstrap이 해야 할 일:**
- 클래스 인스턴스화
- 의존성 주입 (constructor injection)
- 엔진/네트워크 정적 이벤트 구독 (Photon 콜백, `IPunInstantiateMagicCallback`, 정적 `Action` 이벤트)
- `DisposableScope`를 통한 EventHandler 구독 소유권 추적 (EventHandler가 생성자에서 EventBus를 직접 구독)
- 컴포넌트의 Initialize() 호출

**Bootstrap이 하지 말아야 할 일:**
- 비즈니스 로직 실행
- 도메인 엔티티 직접 생성
- 선택/의사결정 로직 포함
- 게임 이벤트 처리
* 초기화 bridging에 엄격히 필요하지 않은 런타임 게임플레이 레지stry/큐 소유
- `Update`, `LateUpdate`, 코루틴 루프로 게임플레이 플로우 구동

**명확한 경계:**
- Bootstrap이 "지금 무엇을 해야 하나?"를 대답하기 시작하면, 해당 로직을 Application으로 이동.
- Bootstrap이 생성된 객체의 런타임 상태를 일회성 초기화 이상으로 저장하면, 해당 상태 소유자를 Bootstrap 밖으로 이동.
- Bootstrap이 네트워크/게임플레이 규칙에 따라 분기가 필요하면, 해당 규칙은 Bootstrap 외부에.
- Bootstrap에 게임플레이 플로우를 구동하는 `Update()` 또는 코루틴이 있으면 (카운트다운 틱, 상태 전환, 스폰 루프), Bootstrap이 연결하는 Presentation MonoBehaviour로 분리. (실제 사례: WaveBootstrap에 Update/SpawnWaveEnemies 코루틴이 있었고 WaveFlowController + EnemySpawnAdapter로 분리)

### 3.1 네트워크 객체 초기화 계약
**규칙:** 모든 Photon 인스턴스화 객체 타입은 명시적인 초기화 계약을 하나 가져야 한다.

**필수 순서:**
1. Photon 객체 인스턴스화
2. Setup/Adapter가 런타임 전용 의존성 획득
3. Application EventHandler / UseCase 생성
4. Presentation 초기화
5. late-join / 재접속 동작을 명시적으로 처리

**규칙:**
- 동일 객체 타입에 대해 polling, scene scan, 정적 lookup, 콜백 기반 wiring을 혼용하지 않는다.
- 각 네트워크 feature는 하나의 arrival 메커니즘을 선택하고 README에 문서화.
- late-join / 재접속 동작은 명시적이어야 하며, 우발적이면 안 된다.
- 런타임 초기화 폴백은 권한 있는 setup 데이터를 덮어쓰면 안 된다.

### 4. MonoBehaviour를 가진 Infrastructure
**규칙:** Infrastructure는 Unity 통합이 필요할 때 MonoBehaviour를 가질 수 있다.
- ✅ `CombatTargetAdapter : MonoBehaviour` — scene 통합을 위해 SerializeField 필요
- ✅ `PlayerNetworkAdapter : MonoBehaviourPun` — Photon에 필요

가능하면 순수 C# adapter를 선호.

### 5. UseCase를 통한 도메인 엔티티 생성
**규칙:** Bootstrap은 도메인 엔티티를 직접 생성해서는 안 된다.
```
❌ Bootstrap이 Domain.Projectile을 직접 생성
✅ Bootstrap이 SpawnProjectileUseCase 호출 → UseCase가 Domain.Projectile 생성
```

### 6. 컴파일 클린 게이트
**규칙:** 정적 아키텍처 규칙을 통과해도 Unity 컴파일 에러가 있으면 `clean`이 아니다.

* 하네스 또는 사람이 `resolved`를 선언하기 전에 컴파일 상태를 확인한다.
* namespace drift, missing using, phantom contract, short-type shadowing, 이벤트 계약 drift, concrete/interface drift, scene 소유 helper 배치 drift는 모두 컴파일 클린을 깨는 구조 문제로 본다.
* `validation_gates.md`의 `compile-clean` 정의를 함께 따른다.

---

확실하지 않을 때:

Shared보다 현재 feature 안에 코드를 유지하는 것을 선호한다.
