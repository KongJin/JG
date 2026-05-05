# 레이어 규칙

Clean Architecture 기반 레이어 정의와 책임.

---

## 레이어 구조

```
Domain (순수 비즈니스 로직)
  ↓
Application (UseCase, port, 이벤트)
  ↓
Infrastructure (외부 시스템: Photon, 영속성, SDK)
Presentation (사용자 상호작용, UI, InputHandler)
Bootstrap (composition root, wiring만)
```

---

## 레이어 정의

### Domain

순수 비즈니스 로직.

**허용:**
- Entity, ValueObject
- 도메인 규칙
- 도메인 상태
- 도메인 메서드

**금지:**
- Unity API (`UnityEngine.*`)
- Photon API
- 파일 IO
- 데이터베이스 접근
- UI 로직

---

### Application

UseCase, port, 이벤트.

**허용:**
- UseCase 클래스
- Repository 인터페이스
- 네트워크 port 인터페이스
- 도메인 이벤트 struct (예: `LobbyUpdatedEvent`)

**금지:**
- Unity API (`UnityEngine.Debug.Log*` 포함)
- Photon API
- `MonoBehaviour`
- 직접 파일 IO

**규칙:**
- UseCase는 얇게; 비즈니스 규칙은 Domain에
- UseCase는 `IEventPublisher` 또는 `EventBus`로 발행 — View를 직접 호출하지 않음
- 실제 존재하는 계약 타입만 사용

---

### Presentation

사용자 상호작용 및 UI.

**허용:**
- View (`MonoBehaviour`)
- `InputHandler`

**규칙:**
- 비즈니스 로직 금지
- View는 `IEventSubscriber` 또는 `EventBus`로 구독하고 렌더링
- `InputHandler`는 UseCase를 직접 호출
- `PlacementArea` 같은 scene 소유 배치 계약과 Unity 의존 helper는 여기에

---

### Infrastructure

외부 시스템: Photon, 영속성, SDK.

**규칙:**
- Application port를 구현
- 비즈니스 로직 금지

---

### Bootstrap

feature root의 composition root (`SkillSetup`, `GameSceneRoot` 등).

**허용:**
- 객체 생성
- 의존성 주입
- 초기화 순서
- 구독 (Application EventHandler 위임)

**금지:**
- Bootstrap 내 이벤트 처리 로직
- 비즈니스 로직
- 도메인 엔티티 생성 (UseCase 사용)

**규칙:**
- 비즈니스/렌더링 로직 금지
- feature root에 Setup 또는 Bootstrap 클래스 **하나**
- Bootstrap은 "지금 무엇을 해야 하나?"를 대답하기 시작하면 → Application으로 이동

---

## 의존 방향

허용된 흐름:

```
Presentation → Application → Domain
Infrastructure → Application
Shared → (피처 의존성 없음)
```

레이어 방향이 존중될 때 크로스 피처 의존성은 **권장**됩니다.

각 레이어의 의존 가능 대상:

| 레이어 | 의존 가능 대상 |
|--------|--------------|
| **Domain** | 다른 feature의 Domain |
| **Application** | Domain, Shared, 다른 feature의 Application 또는 Domain |
| **Presentation** | Application, Domain, Shared, 다른 feature의 같거나 내부 레이어 |
| **Infrastructure** | Application, Domain, Shared, 다른 feature의 같거나 내부 레이어 |

---

## EventBus 소유권

**씬당 하나의 EventBus.** 각 scene Bootstrap/root는 `new EventBus()`를 한 번 생성하고 해당 씬의 모든 feature에 주입합니다.

```
GameSceneRoot
  └─ _eventBus = new EventBus()
       ├─ PlayerSetup.Initialize(eventBus, ...)
       ├─ CombatBootstrap.Initialize(eventBus, ...)
       ├─ StatusSetup.Initialize(eventBus, ...)
       ├─ SkillSetup.Initialize(eventBus, ...)
       └─ WaveBootstrap.Initialize(eventBus, ...)
```

**규칙:**
- Feature는 스스로 EventBus를 생성하지 않음
- 다른 씬 ⇒ 다른 EventBus (Lobby vs Game)
- Scene root는 보통 `EventBus` 구현체 하나를 생성
- concrete type을 직접 받는 권한은 기본적으로 Bootstrap / composition root에만
- Application / Presentation / Infrastructure는 기본적으로 필요한 최소 계약만
- Shared 계약명은 실제 선언된 인터페이스/클래스만 사용

> **세부 규칙**: [`rule-patterns/event_rules.md`](../rule-patterns/event_rules.md#이벤트-소유권)

---

## Feature isolation

각 feature는 다음을 소유합니다:
- Domain, Application, Presentation, Infrastructure
- feature root에 위치한 Setup/Bootstrap 클래스 (`Bootstrap/` 폴더 내부 X)

**규칙:**
- 크로스 피처 의존성은 레이어 방향이 존중될 때 허용
- feature 레이어 간 조립/wiring은 해당 feature의 Setup/Bootstrap 클래스에 있어야 함
- 개념이 독립 생명주기를 얻기 전에는 feature 안에 남김
