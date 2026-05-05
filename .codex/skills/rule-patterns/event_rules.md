# Event Rules

## EventBus 동작 모델

현재 `EventBus.Publish<T>`는 **동기(synchronous)** 실행이다.
핸들러는 **구독 순서(FIFO)** 대로 같은 프레임 안에서 즉시 실행된다.
핸들러 하나가 예외를 던져도 나머지 핸들러는 계속 실행된다(snapshot 기반).

---

## 이벤트 체인 규칙

### 1. 단방향만 허용

이벤트 체인은 **인과(cause → effect)** 방향으로만 흐른다.

```
허용: A → B → C (한 방향)
금지: A → B → A (순환)
```

핸들러가 새 이벤트를 Publish하면 그 이벤트의 핸들러도 **같은 콜스택 안에서 즉시** 실행된다.
순환이 발생하면 스택 오버플로우가 난다 — 컴파일러가 잡아주지 않으므로 작성자가 책임진다.

### 2. 체인 깊이 제한

이벤트 체인은 **최대 3단계**를 권장한다.

```
좋음: ProjectileHitEvent → DamageAppliedEvent → PlayerDownedEvent
나쁨: A → B → C → D → E (추적 불가)
```

3단계를 넘기면 중간 단계를 UseCase 직접 호출로 대체하여 깊이를 줄인다.

### 3. 순환 방지 검증법

새 이벤트 구독을 추가할 때, 아래를 확인한다:

1. 이 핸들러가 Publish하는 이벤트 목록을 적는다.
2. 그 이벤트의 핸들러가 다시 원래 이벤트를 Publish하는지 추적한다.
3. 순환이 발견되면 한쪽을 **직접 호출(UseCase inject)**로 바꾼다.

---

## 이벤트 vs 직접 호출 판단 기준

| 상황 | 선택 | 이유 |
|------|------|------|
| 발행자가 반응자를 몰라야 할 때 | 이벤트 | 느슨한 결합 |
| 반응이 여러 피처에서 일어날 때 | 이벤트 | 1:N 브로드캐스트 |
| 정확히 하나의 결과가 필요할 때 | 직접 호출 | 명시적 의존, 디버깅 쉬움 |
| 호출 순서가 중요할 때 | 직접 호출 | 순서 보장 |
| 체인 깊이가 3을 넘길 때 | 직접 호출로 대체 | 추적성 확보 |

---

## 이벤트 소유권

> **씬당 하나의 EventBus**: 상세 규칙은 [`rule-architecture/layers.md`](../rule-architecture/layers.md#eventbus-소유권)를 따른다.

- 이벤트 struct는 **발행하는 피처의 Application** 에 둔다.
- 다른 피처가 같은 상태 변화를 표현해야 하면, 원본 이벤트를 구독한다. 별도 이벤트를 만들지 않는다.
- 예: `PlayerDownedEvent`는 Player/Application 소유. Combat이 다운을 알아야 하면 이 이벤트를 구독한다.

---

## 현재 확인된 이벤트 체인 (참조용)

```
1. Skill Cast → Delivery → Hit → Damage → Player State
   SkillCastedEvent → ProjectileRequestedEvent → ProjectileHitEvent
   → DamageAppliedEvent → PlayerHealthChangedEvent / PlayerDownedEvent

2. Status Application
   ProjectileHitEvent / ZoneTickEvent → StatusApplyRequestedEvent
   → StatusAppliedEvent

3. Player Lifecycle
   PlayerDownedEvent → (BleedoutTracker ticks) → PlayerDiedEvent
   PlayerDownedEvent → (RescueChannelTracker) → PlayerRescuedEvent
```

이 체인들은 모두 단방향이며 순환이 없다. 새 피처 추가 시 이 체인에 합류하는지 확인한다.

---

## 이벤트 계약 drift 방지

이벤트 payload 변경 시 **producer, consumer, bridge를 함께 검토**한다.

**금지:**
- 이벤트 struct에 필드를 추가/삭제했는데 consumer가 이전 필드를 계속 참조하는 상태
- consumer가 더 이상 존재하지 않는 필드를 가정하는 상태 (예: `GameEndEvent`에서 `IsLocalPlayerDead` 제거 후 consumer가 계속 참조)
- bridge(이벤트 → 다른 시스템 변환)가 원본 이벤트와 다른 필드 집합을 가정하는 상태

**검증 방법:**
1. 이벤트 struct를 수정하면 모든 `Subscribe<T>` 호출부를 식별
2. 각 핸들러가 사용하는 필드가 존재하는지 확인
3. bridge 코드가 원본 이벤트를 그대로 전달하는지, 변환하는지 확인
