# /agent/anti_patterns.md

> 마지막 수정: 2026-04-12

이 문서는 구조를 새로 정의하지 않는다. 구조 정의는 `architecture.md`를 따르고, 여기서는 구현 중 금지 패턴과 예외 판단만 다룬다.

레이어·폴더·의존 기본 규칙은 [`architecture.md`](architecture.md)를 따른다. 이 문서는 **실제 코드 작성 시 실수하기 쉬운 패턴**만 모은다.

---

## 태그 범례

| 태그 | 의미 |
|---|---|
| `#arch` | 아키텍처 위반 |
| `#layer` | 레이어 배치 위반 |
| `#compile` | 컴파일/정적 안전 |
| `#code-quality` | 코드 품질 |
| `#unity` | Unity 고유 |

---

## 금지 패턴

### #arch — Bootstrap 책임 위반

**Bootstrap이 god class로 전환.**

Bootstrap은 wiring만 담당한다. 아래는 모두 Bootstrap 외부로 분리:
- 비즈니스 로직 실행
- 도메인 엔티티 직접 생성
- 선택/의사결정 로직
- 게임 이벤트 처리
- `Update`, `LateUpdate`, 코루틴 루프로 게임플레이 플로우 구동

**명확한 경계:**
- Bootstrap이 "지금 무엇을 해야 하나?"를 대답하기 시작하면 → Application으로 이동.
- Bootstrap이 생성된 객체의 런타임 상태를 일회성 초기화 이상으로 저장하면 → 상태 소유자를 Bootstrap 밖으로 이동.
- Bootstrap에 게임플레이 플로우를 구동하는 `Update()` 또는 코루틴이 있으면 → Presentation MonoBehaviour로 분리. (실제 사례: WaveBootstrap → WaveFlowController + EnemySpawnAdapter)

### #arch — Bootstrap 내 선택/순환 로직

Bootstrap은 wiring만 담당. 스킬 순환, 다음 타깃 선택 등은 전용 Application 레이어 클래스로 분리.

### #arch — 네트워크 객체 초기화 계약

모든 Photon 인스턴스화 객체 타입은 명시적인 초기화 계약을 하나 가져야 한다.

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

### #code-quality — 단일 호출 private 함수

호출 지점이 하나뿐인 private 메서드는 추출하지 않는다. 호출부에 직접 인라인한다. 읽는 사람이 한 함수 안에서 흐름을 따라갈 수 있어야 한다.

예외: (1) 콜백/이벤트 핸들러로 등록되는 함수, (2) 두 곳 이상에서 호출되는 함수.

### #compile — 타입명 충돌

한 feature 안에서 같은 short type name의 `MonoBehaviour`를 두 개 만들지 않는다. Presentation 컴포넌트 이름도 feature 전체에서 유일해야 한다.

### #compile — 네임스페이스-클래스명 충돌

피처 네임스페이스와 동일한 short name의 도메인 엔티티를 생성하지 않는다. 예: `Features.Account` 네임스페이스 아래 `Account` 클래스를 두면 C#이 `Account`를 타입이 아닌 네임스페이스로 해석하여 `CS0118` 컴파일 에러가 발생한다. 대신 `AccountProfile`, `AccountEntity` 등 접미사를 붙인다. (실제 사례: `Account.cs` → `AccountProfile.cs` 리네임)

### #compile — Feature short-type shadowing

feature namespace 이름과 같은 short type (`Unit`, `Player`, `Wave` 등)을 bare identifier로 사용해 namespace/type 충돌을 유발하지 않는다. alias 또는 fully-qualified name을 사용한다.

### #code-quality — 완전 switch에서 암묵적 폴백

enum을 switch로 분기할 때, default에서 폴백 값을 반환하지 않는다. 새 enum 값 추가 시 컴파일러가 경고하지 않으므로, default는 `throw new ArgumentOutOfRangeException()`으로 즉시 실패시킨다. `Debug.LogError` + 폴백 반환은 문제를 숨긴다. (실제 사례: SkillData.CreateDelivery에서 default가 SelfDelivery를 반환하여 새 DeliveryType 추가 시 silent corruption 가능성이 있었음)

### #compile — Concrete/interface drift

한쪽은 interface, 한쪽은 concrete 구현체를 요구해 wiring이 깨지는 상태를 허용하지 않는다. Bootstrap만 concrete를 직접 조립하고, 그 밖의 레이어는 최소 계약을 받는다. 예: `EventBus` / `IEventPublisher` 가정이 엇갈려 `SummonPhotonAdapter` wiring이 깨지는 상태.

### #unity — Subscribe 반환값 가정

`Subscribe()`의 반환형을 임의로 `IDisposable`처럼 취급하지 않는다. EventBus ownership 해제는 `EventBusSubscription.ForOwner(...)` 또는 명시적 cleanup으로 처리한다.

### #compile — 리팩터링 후 오래된 심볼

리팩터링 전 필드/메서드/타입명을 계속 참조하는 상태를 허용하지 않는다. 변경 후에는 반드시 컴파일을 실행하고 모든 참조가 수정되었음을 0 errors로 확인한다.

### #compile — 시그니처 변경 시 호출부 전수 조사

public/protected 메서드 시그니처 변경 시 반드시 모든 호출부를 먼저 식별한다. 하위 호환이 필요하면 선택 인자나 오버로드로 처리하고, 모든 호출부를 한 번에 수정한다.

---

## 레이어 운영 패턴

architecture.md의 규칙을 코드에 적용하는 구체적 패턴이다.

### UseCase를 통한 도메인 엔티티 생성

Bootstrap은 도메인 엔티티를 직접 생성해서는 안 된다.

```
❌ Bootstrap이 Domain.Projectile을 직접 생성
✅ Bootstrap이 SpawnProjectileUseCase 호출 → UseCase가 Domain.Projectile 생성
```

### MonoBehaviour를 가진 Infrastructure

Infrastructure는 Unity 통합이 필요할 때 MonoBehaviour를 가질 수 있다.

- ✅ `CombatTargetAdapter : MonoBehaviour` — scene 통합을 위해 SerializeField 필요
- ✅ `PlayerNetworkAdapter : MonoBehaviourPun` — Photon에 필요

가능하면 순수 C# adapter를 선호.

---

확실하지 않을 때:

Shared보다 현재 feature 안에 코드를 유지하는 것을 선호한다.
