# /agent/architecture.md

이 문서는 **코드 구조·피처 경계·의존 방향·레이어 책임·네이밍·크로스 피처 포트·scene contract 체크리스트**의 단일 근거(SSOT)다.  
엔트리포인트는 `../CLAUDE.md`이고, 전역 구조 규칙은 여기서만 정의한다. feature-local 계약은 feature root `/Assets/Scripts/Features/<FeatureName>/README.md`에 두되, wiring과 실제 씬/프리팹 사실은 `Setup`/`Bootstrap`, 씬/프리팹 직렬화 참조, 관련 코드 경로에서 드러나야 한다.

시각 요약(layer/port only): [architecture-diagram.md](../docs/design/architecture-diagram.md) — 피처 의존 그래프의 품질 게이트는 이 문서와 validator가 소유한다.

---

## 폴더 구조

이 프로젝트는 **Feature-first Clean Architecture**를 따른다.

```
Assets/Scripts/Features/<FeatureName>/
  Domain/
  Application/
  Presentation/
  Infrastructure/
  README.md                  # feature-local contract
  <FeatureName>Setup.cs      # Bootstrap: composition root
  <FeatureName>Bootstrap.cs   # Bootstrap: scene-level wiring
Assets/Scripts/Shared/
  README.md                  # shared-local contract
```

각 feature는 독립적으로 성장해야 한다.

* feature 전용 코드를 Shared로 올리지 않는다.
* Shared에는 여러 feature가 함께 쓰는 공통 유틸만 둔다.
* Infrastructure는 Application port를 구현한다.
* Domain은 프레임워크 비의존을 유지한다.
* Bootstrap (`Setup`/`Bootstrap`)은 레이어 간 조립과 wiring만 담당한다.
* scene-owned feature는 자기 `Setup`/`Bootstrap`, 씬/프리팹 직렬화 참조, 관련 코드에서 scene contract를 드러낸다.
* networked feature는 명시적 초기화 경로와 late-join 동작을 코드와 전역 규칙으로 추적 가능하게 유지한다.
* 런타임 fallback 생성으로 누락된 scene/prefab setup을 대체하지 않는다.
* 각 feature root는 `/Assets/Scripts/Features/<FeatureName>/README.md`를 갖고, 책임/로컬 계약/scene contract entry를 최신 상태로 유지한다.

개념이 독립 생명주기를 얻기 전에는 feature 안에 남긴다.

### Feature-local contract docs

각 feature root `/Assets/Scripts/Features/<FeatureName>/README.md`는 전역 구조를 다시 정의하지 않는다. 대신 아래 로컬 사실만 기록한다.

* feature 책임
* root `Setup` / `Bootstrap` 진입점
* scene 소유 또는 prefab 소유 계약
* 런타임 생성 객체
* 허용된 런타임 탐색 예외
* 실제로 사용되는 크로스 피처 의존성
* late-join / 재접속 관련 참고사항
* 피처별 컴파일 주의사항
* 동일 이름 타입 alias 정책
* scene 소유 helper 타입 소유권

Shared root `/Assets/Scripts/Shared/README.md`는 Shared에 둘 수 있는 공통 계약과 금지 대상을 로컬 관점에서 요약한다.

### Scene contract (code and scene assets)

scene contract 체크리스트의 소유자는 이 문서다.
각 feature는 아래 항목을 자기 피처의 `Setup`/`Bootstrap`, 씬/프리팹 직렬화 참조, 관련 코드 경로로 충족해야 하며, 항목 정의 자체를 다시 바꾸지 않는다.

모든 scene 소유 feature는 다음을 명시해야 한다:

* 필수 GameObjects/components
* 필수 직렬화 참조
* 런타임 생성 객체
* 금지된 런타임 생성 대체물
* 허용된 런타임 탐색 예외
* 초기화 순서
* 네트워크 객의 late-join / 재접속 동작

전역 항목 정의는 이 문서가 소유한다. feature README에는 자기 피처가 어떤 GameObject, serialized reference, runtime object를 요구하는지만 기록한다.

---

## Feature isolation

각 feature는 다음을 소유한다:

* Domain, Application, Presentation, Infrastructure
* feature root에 위치한 Setup/Bootstrap 클래스 (`Bootstrap/` 폴더 내부가 아님)

예시:

```
Assets/Scripts/Features/Lobby/
  Domain/
  Application/
  Presentation/
  Infrastructure/
  LobbySetup.cs        # composition root
  LobbyBootstrap.cs    # scene-level wiring
```

규칙:

* 크로스 피처 의존성은 **레이어 방향**이 존중될 때 허용된다 (같거나 내부 레이어만).
* feature 레이어 간 조립/wiring은 해당 feature의 Setup/Bootstrap 클래스에 있어야 한다.
* Feature 이름과 같은 short type name (`Unit`, `Player`, `Wave` 등)을 같은 feature 내부에서 타입으로 참조할 때는 alias 또는 fully-qualified name을 사용한다.

개념이 독립 생명주기를 얻기 전에는 feature 안에 남긴다 (예: Lobby의 Room이 독립 생명주기가 필요해질 때까지).

### EventBus 소유권

**씬당 하나의 EventBus.** 각 scene Bootstrap/root (예: `GameSceneRoot`, `LobbyBootstrap`)는 `new EventBus()`를 한 번 생성하고 해당 씬의 모든 feature에 주입한다.

```
GameSceneRoot
  └─ _eventBus = new EventBus()
       ├─ PlayerSetup.Initialize(eventBus, ...)
       ├─ CombatBootstrap.Initialize(eventBus, ...)
       ├─ StatusSetup.Initialize(eventBus, ...)
       ├─ SkillSetup.Initialize(eventBus, ...)
       └─ WaveBootstrap.Initialize(eventBus, ...)
```

* Feature는 스스로 EventBus를 생성하지 **않는다**.
* 다른 씬 ⇒ 다른 EventBus (Lobby vs Game).
* Scene root는 보통 `EventBus` 구현체 하나를 생성하고, 필요한 곳에 `IEventPublisher` / `IEventSubscriber` 또는 `EventBus`를 명시적으로 주입한다.
* concrete type을 직접 받는 권한은 기본적으로 Bootstrap / composition root에만 있다.
* Application / Presentation / Infrastructure는 기본적으로 필요한 최소 계약만 받는다.
* Shared 계약명은 실제 선언된 인터페이스/클래스만 사용한다. 존재하지 않는 `IEventBus` 같은 phantom type을 새로 가정하지 않는다.

---

## 의존 방향

허용된 흐름:

```
Presentation -> Application -> Domain
Infrastructure -> Application
Shared -> (피처 의존성 없음)
```

레이어 방향이 존중될 때 크로스 피처 의존성은 **권장**된다. 크로스 피처 import를 피하기 위해 Shared에 추상화를 추가하지 않는다.

각 레이어의 의존 가능 대상:

* **Domain:** 다른 feature의 Domain.
* **Application:** Domain, Shared, 다른 feature의 Application 또는 Domain.
* **Presentation:** Application, Domain, Shared, 다른 feature의 같거나 내부 레이어.
* **Infrastructure:** Application, Domain, Shared, 다른 feature의 같거나 내부 레이어.

사용 금지:

* Domain에서 Unity API
* Domain에서 Photon API
* Domain에서 데이터베이스 로직

### Feature dependency graph

피처 간 의존성은 수동 diagram이나 수기 표를 SSOT로 두지 않는다.

* 피처 의존 그래프는 코드에서 자동 추출한다.
* composition root(`*Setup`, `*Bootstrap`, scene root)처럼 조립 책임만 가진 feature root 파일은 DAG 품질 게이트에서 제외한다.
* consumer-owned `Application/Ports` 참조는 승인된 DIP seam으로 보고, DAG edge로 세지 않는다.
* analytics/reporting 전용 observer 코드는 gameplay DAG 품질 게이트에서 제외한다.
* 새 피처 의존성 자체는 허용한다.
* 단, 전체 그래프는 DAG여야 하며 `A -> B -> A`, `A -> B -> C -> A` 같은 cycle은 구조 위반이다.
* 자동 추출 산출물은 `Temp/LayerDependencyValidator/feature-dependencies.json` 이다.
* feature README의 `Cross-feature Dependencies` 섹션은 설명용이며, 품질 게이트 입력값으로 쓰지 않는다.

### 크로스 피처 포트 배치

Feature A가 Feature B의 기능을 사용할 때:

1. **Port 인터페이스**는 **소비자 (A)**의 `Application/Ports/`에 정의한다.
2. **구현**은 **제공자 (B)**의 `Infrastructure/`에 둔다.
3. **Bootstrap**이 구현체를 생성하고 소비자에 주입한다.

```
Combat/Application/Ports/ICombatTargetProvider.cs   ← Combat 정의 (소비자)
Player/Infrastructure/PlayerCombatTargetProvider.cs  ← Player 구현 (제공자)
```

**금지:** 제공자의 Application에 port를 정의하고 소비자가 import하는 것.

---

## 레이어 규칙

### Domain

순수 비즈니스 로직.

허용: Entity, ValueObject, 도메인 규칙, 도메인 상태, 도메인 메서드.

금지: Unity API, Photon API, 파일 IO, 데이터베이스 접근, UI 로직.

### Application

UseCase, port, 이벤트.

허용: UseCase 클래스, repository 인터페이스, 네트워크 port 인터페이스, 도메인 이벤트 struct (예: `LobbyUpdatedEvent`).

금지: Unity API (`UnityEngine.Debug.Log*` 포함), Photon API, `MonoBehaviour`, 직접 파일 IO.

규칙: UseCase는 얇게; 비즈니스 규칙은 Domain에; UseCase는 `IEventPublisher` 또는 `EventBus`로 발행 — View를 직접 호출하지 않는다.
규칙: Application은 실제 존재하는 계약 타입만 사용한다. Unity/Shared 타입 이동 후 namespace drift가 생기지 않도록 필요한 `using` 또는 fully-qualified name을 명시한다.

### Presentation

사용자 상호작용 및 UI.

허용: View (`MonoBehaviour`), `InputHandler`.

규칙: 비즈니스 로직 금지; View는 `IEventSubscriber` 또는 `EventBus`로 구독하고 렌더링; `InputHandler`는 UseCase를 직접 호출.
규칙: `PlacementArea` 같은 scene 소유 배치 계약과 Unity 의존 helper는 Presentation 또는 scene 소유 계약에 둔다. Domain에 두지 않는다.

### Infrastructure

외부 시스템: Photon, 영속성, SDK.

규칙: Application port를 구현; 비즈니스 로직 금지.

### Bootstrap

feature root의 composition root (`SkillSetup`, `GameSceneRoot` 등) — `Bootstrap/` 폴더 내부 **아님**.

허용: 객체 생성, 의존성 주입, 초기화 순서, 구독 (Application EventHandler 위임).

금지: Bootstrap 내 이벤트 처리 로직, 비즈니스 로직, 도메인 엔티티 생성 (UseCase 사용).

규칙: 비즈니스/렌더링 로직 금지; feature root에 Setup 또는 Bootstrap 클래스 **하나** (`./anti_patterns.md` 참조).

---

## 레이어 배치 운영 패턴

architecture.md의 레이어 규칙을 코드에 적용하는 구체적 패턴이다.
세부 금지 패턴은 [`anti_patterns.md`](anti_patterns.md)를 따른다.

### 타입 의존에 따른 Port 배치

**규칙:** Unity 타입을 사용하지 않는 port 인터페이스만 Application/Ports에 둔다. UnityEngine 타입(Sprite, GameObject, AudioClip, Color 등)을 참조하는 port는 Presentation에 둔다.

- ✅ `Application/Ports/IZoneEffectPort.cs` — `Float3`만 사용 (Shared), Application 배치 가능
- ✅ `Presentation/ISkillEffectPort.cs` — `GameObject`, `AudioClip` 사용 (Unity), Presentation 배치 필수
- ✅ `Presentation/ISkillIconPort.cs` — `Sprite` 사용 (Unity), Presentation 배치 필수
- ❌ `Application/Ports/ISkillEffectPort.cs` — Application 레이어에 Unity 타입 포함은 레이어 규칙 위반

**이유:** Application 레이어는 UnityEngine에 의존해서는 안 된다. Unity 타입 port를 Application으로 옮기면 이를 참조하는 모든 Application 클래스가 "감염"된다.

### 이벤트 핸들러 → Application 레이어

**규칙:** 이벤트 처리 로직은 Bootstrap이 아닌 Application 레이어에 둔다.

- ❌ Bootstrap에서 `OnXxxEvent()` 메서드로 이벤트를 직접 구독·처리 — 잘못됨
- ✅ Application EventHandler가 생성자에서 EventBus를 직접 구독, Bootstrap은 생성 + `DisposableScope` 수명 관리만 — 올바름

**패턴:**
```
<Feature>Setup.cs / <Feature>Bootstrap.cs (wiring 전용)
  → Application EventHandler 생성 (생성자에 IEventSubscriber 주입)
  → EventHandler가 생성자에서 구독
  → Bootstrap이 소유권 추적: _disposables.Add(EventBusSubscription.ForOwner(eventBus, handler))
```

### UseCase를 통한 도메인 엔티티 생성

Bootstrap은 도메인 엔티티를 직접 생성해서는 안 된다.

```
❌ Bootstrap이 Domain.Projectile을 직접 생성
✅ Bootstrap이 SpawnProjectileUseCase 호출 → UseCase가 Domain.Projectile 생성
```

---

## 명명 규칙

* **Entity:** 접미사 없음 — `Lobby`, `Room`, `RoomMember`
* **UseCase:** 예: `CreateRoomUseCase`, `JoinRoomUseCase` (피처별, 종종 `LobbyUseCases`, `PlayerUseCases`로 통합 — 기존 피처 참조)
* **NetworkEventHandler:** `LobbyNetworkEventHandler`, `PlayerNetworkEventHandler`
* **Port 인터페이스:** `ILobbyRepository`, `IPlayerNetworkCommandPort` — 명확한 피처 컨텍스트 사용, 지나치게 일반적인 이름 금지
* **Event:** 과거 시제 + `Event` — `LobbyUpdatedEvent`, `GameStartedEvent`
* **EventBus:** `IEventPublisher`, `IEventSubscriber`, `EventBus`는 `Shared/EventBus/`에
* **Adapter:** `LobbyPhotonAdapter`, `ClockAdapter`
* **View:** `LobbyView`, `RoomListView`
* **InputHandler:** `LobbyInputHandler`

### Type naming safety

feature 이름이 namespace와 type short name을 동시에 차지하는 경우가 있다.

예:

* `Features.Unit` namespace
* `Features.Unit.Domain.Unit` type

이 경우 아래를 따른다.

* same feature 안에서는 `Unit`, `Player`, `Wave` 같은 이름을 bare identifier로 타입처럼 쓰지 않는다.
* alias를 필수 기본값으로 본다.
* 예: `using UnitSpec = Features.Unit.Domain.Unit;`
* alias가 없으면 fully-qualified name을 쓴다.

이 규칙은 compile-clean을 위한 구조 규칙이며, `validation_gates.md`의 `compile-clean` 정의와 함께 해석한다.

---

## asmdef (optional future)

There is **no** per-feature `asmdef` under `Assets/Scripts/Features/**` today; layers are enforced by convention and review. If assemblies are split later, update **this file** and `../docs/design/architecture-diagram.md` together.
