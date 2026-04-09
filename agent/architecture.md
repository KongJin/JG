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

* feature responsibility
* root `Setup` / `Bootstrap` entrypoints
* scene-owned or prefab-owned contract
* runtime-created objects
* allowed lookup exceptions
* cross-feature dependencies actually used
* late-join / reconnect notes when relevant
* feature-specific compile hazards
* same-name type alias policy
* scene-owned helper type ownership

Shared root `/Assets/Scripts/Shared/README.md`는 Shared에 둘 수 있는 공통 계약과 금지 대상을 로컬 관점에서 요약한다.

### Scene contract (code and scene assets)

scene contract 체크리스트의 소유자는 이 문서다.
각 feature는 아래 항목을 자기 피처의 `Setup`/`Bootstrap`, 씬/프리팹 직렬화 참조, 관련 코드 경로로 충족해야 하며, 항목 정의 자체를 다시 바꾸지 않는다.

Every scene-owned feature must make explicit:

* required GameObjects/components
* required serialized references
* runtime-created objects
* forbidden runtime-created replacements
* allowed runtime lookup exceptions
* initialization order
* late-join / reconnect behavior for networked objects

전역 항목 정의는 이 문서가 소유한다. feature README에는 자기 피처가 어떤 GameObject, serialized reference, runtime object를 요구하는지만 기록한다.

---

## Feature isolation

Each feature owns:

* Domain, Application, Presentation, Infrastructure
* Setup/Bootstrap class at feature root (**not** in a `Bootstrap/` folder)

Example:

```
Assets/Scripts/Features/Lobby/
  Domain/
  Application/
  Presentation/
  Infrastructure/
  LobbySetup.cs        # composition root
  LobbyBootstrap.cs    # scene-level wiring
```

Rules:

* Cross-feature dependency is allowed as long as **layer direction** is respected (same-or-inner layer only).
* Wiring/composition across a feature's layers must live in that feature's Setup/Bootstrap class.
* Feature 이름과 같은 short type name (`Unit`, `Player`, `Wave` 등)을 같은 feature 내부에서 타입으로 참조할 때는 alias 또는 fully-qualified name을 사용한다.

Keep concepts inside a feature unless they have an independent lifecycle (e.g. Room in Lobby until Room needs its own lifecycle).

### EventBus ownership

**One EventBus per scene.** Each scene Bootstrap/root (e.g. `GameSceneRoot`, `LobbyBootstrap`) creates `new EventBus()` once and injects it into every feature in that scene.

```
GameSceneRoot
  └─ _eventBus = new EventBus()
       ├─ PlayerSetup.Initialize(eventBus, ...)
       ├─ CombatBootstrap.Initialize(eventBus, ...)
       ├─ StatusSetup.Initialize(eventBus, ...)
       ├─ SkillSetup.Initialize(eventBus, ...)
       └─ WaveBootstrap.Initialize(eventBus, ...)
```

* Features do **not** create their own EventBus.
* Different scene ⇒ different EventBus (Lobby vs Game).
* Scene root는 보통 `EventBus` 구현체 하나를 생성하고, 필요한 곳에 `IEventPublisher` / `IEventSubscriber` 또는 `EventBus`를 명시적으로 주입한다.
* concrete type을 직접 받는 권한은 기본적으로 Bootstrap / composition root에만 있다.
* Application / Presentation / Infrastructure는 기본적으로 필요한 최소 계약만 받는다.
* Shared 계약명은 실제 선언된 인터페이스/클래스만 사용한다. 존재하지 않는 `IEventBus` 같은 phantom type을 새로 가정하지 않는다.

---

## Dependency direction

Allowed flow:

```
Presentation -> Application -> Domain
Infrastructure -> Application
Shared -> (no feature dependency)
```

Cross-feature dependency is **encouraged** when layer direction is respected. Do not add abstractions to Shared only to avoid cross-feature imports.

Each layer may depend on:

* **Domain:** other features' Domain.
* **Application:** Domain, Shared, other features' Application or Domain.
* **Presentation:** Application, Domain, Shared, other features' same-or-inner layers.
* **Infrastructure:** Application, Domain, Shared, other features' same-or-inner layers.

Never reference:

* Unity API in Domain
* Photon API in Domain
* Database logic in Domain

### Feature dependency graph

피처 간 의존성은 수동 diagram이나 수기 표를 SSOT로 두지 않는다.

* 피처 의존 그래프는 코드에서 자동 추출한다.
* consumer-owned `Application/Ports` 참조는 port ownership 규칙에 따라 semantic edge 방향으로 해석한다.
* 새 피처 의존성 자체는 허용한다.
* 단, 전체 그래프는 DAG여야 하며 `A -> B -> A`, `A -> B -> C -> A` 같은 cycle은 구조 위반이다.
* 자동 추출 산출물은 `Temp/LayerDependencyValidator/feature-dependencies.json` 이다.
* feature README의 `Cross-feature Dependencies` 섹션은 설명용이며, 품질 게이트 입력값으로 쓰지 않는다.

### Cross-feature port placement

When feature A uses functionality from feature B:

1. **Port interface** is defined in the **consumer (A)**'s `Application/Ports/`.
2. **Implementation** lives in the **provider (B)**'s `Infrastructure/`.
3. **Bootstrap** creates the implementation and injects it into the consumer.

```
Combat/Application/Ports/ICombatTargetProvider.cs   ← Combat defines (consumer)
Player/Infrastructure/PlayerCombatTargetProvider.cs  ← Player implements (provider)
```

**Forbidden:** defining the port in the provider's Application and having the consumer import it.

---

## Layer rules

### Domain

Pure business logic.

Allowed: Entities, ValueObjects, domain rules, domain state, domain methods.

Not allowed: Unity API, Photon API, file IO, database access, UI logic.

### Application

Use cases, ports, events.

Allowed: UseCase classes, repository interfaces, network port interfaces, domain event structs (e.g. `LobbyUpdatedEvent`).

Not allowed: Unity API (including `UnityEngine.Debug.Log*`), Photon API, `MonoBehaviour`, direct file IO.

Rules: UseCases stay thin; business rules stay in Domain; UseCases publish via `IEventPublisher` or `EventBus` — they do not call View directly.
Rules: Application은 실제 존재하는 계약 타입만 사용한다. Unity/Shared 타입 이동 후 namespace drift가 생기지 않도록 필요한 `using` 또는 fully-qualified name을 명시한다.

### Presentation

User interaction and UI.

Allowed: View (`MonoBehaviour`), `InputHandler`.

Rules: No business logic; View subscribes via `IEventSubscriber` or `EventBus` and renders; `InputHandler` calls UseCases directly.
Rules: `PlacementArea` 같은 scene-owned placement contract와 Unity 의존 helper는 Presentation 또는 scene-owned contract에 둔다. Domain에 두지 않는다.

### Infrastructure

External systems: Photon, persistence, SDKs.

Rules: Implements Application ports; no business logic.

### Bootstrap

Composition root at feature root (`SkillSetup`, `GameSceneRoot`, etc.) — **not** inside a `Bootstrap/` folder.

Allowed: Object creation, dependency wiring, initialization order, subscribing (delegate handling to Application EventHandlers).

Not allowed: Event handling logic in Bootstrap, business logic, domain entity creation (use UseCase).

Rules: No business or rendering logic; **one** Setup or Bootstrap class at feature root (see also `./anti_patterns.md`).

---

## Naming conventions

* **Entity:** no suffix — `Lobby`, `Room`, `RoomMember`
* **Use cases:** e.g. `CreateRoomUseCase`, `JoinRoomUseCase` (per feature, often consolidated as `LobbyUseCases`, `PlayerUseCases` — see existing features)
* **NetworkEventHandler:** `LobbyNetworkEventHandler`, `PlayerNetworkEventHandler`
* **Port interface:** `ILobbyRepository`, `IPlayerNetworkCommandPort` — use clear feature context, avoid overly generic names
* **Event:** past tense + `Event` — `LobbyUpdatedEvent`, `GameStartedEvent`
* **EventBus:** `IEventPublisher`, `IEventSubscriber`, `EventBus` in `Shared/EventBus/`
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
