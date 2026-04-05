# /agent/architecture.md

이 문서는 **코드 구조·피처 경계·의존 방향·레이어 책임·네이밍·크로스 피처 포트·scene contract 체크리스트**의 단일 근거(SSOT)다.  
엔트리포인트는 `CLAUDE.md`이고, 각 feature `README.md`는 이 문서의 체크리스트를 자기 피처 값으로 채우는 **로컬 계약 문서**다. 전역 구조 규칙은 여기서만 정의한다.

시각 요약(Mermaid): [`docs/architecture-diagram.md`](../docs/architecture-diagram.md) — 표·본문은 **이 문서**와 같아야 한다.

---

## 폴더 구조

이 프로젝트는 **Feature-first Clean Architecture**를 따른다.

```
Assets/Scripts/Features/<FeatureName>/
  Domain/
  Application/
  Presentation/
  Infrastructure/
  <FeatureName>Setup.cs      # Bootstrap: composition root
  <FeatureName>Bootstrap.cs   # Bootstrap: scene-level wiring
Assets/Scripts/Shared/
```

각 feature는 독립적으로 성장해야 한다.

* feature 전용 코드를 Shared로 올리지 않는다.
* Shared에는 여러 feature가 함께 쓰는 공통 유틸만 둔다.
* Infrastructure는 Application port를 구현한다.
* Domain은 프레임워크 비의존을 유지한다.
* Bootstrap (`Setup`/`Bootstrap`)은 레이어 간 조립과 wiring만 담당한다.
* scene-owned feature는 자기 `README.md`에 scene contract를 기록한다.
* networked feature는 자기 `README.md`에 명시적 초기화 경로와 late-join 동작을 기록한다.
* 런타임 fallback 생성으로 누락된 scene/prefab setup을 대체하지 않는다.

개념이 독립 생명주기를 얻기 전에는 feature 안에 남긴다.

### Scene contract (README)

scene contract 체크리스트의 소유자는 이 문서다.
각 feature `README.md`는 아래 항목을 자기 피처 기준으로 채우고, 항목 정의 자체를 다시 바꾸지 않는다.

Every scene-owned feature must document:

* required GameObjects/components
* required serialized references
* runtime-created objects
* forbidden runtime-created replacements
* allowed runtime lookup exceptions
* initialization order
* late-join / reconnect behavior for networked objects

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
* `IEventPublisher` / `IEventSubscriber` split only if a real need appears; `IEventBus` is enough for now.

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

Rules: UseCases stay thin; business rules stay in Domain; UseCases publish via `IEventBus` — they do not call View directly.

### Presentation

User interaction and UI.

Allowed: View (`MonoBehaviour`), `InputHandler`.

Rules: No business logic; View subscribes to `IEventBus` and renders; `InputHandler` calls UseCases directly.

### Infrastructure

External systems: Photon, persistence, SDKs.

Rules: Implements Application ports; no business logic.

### Bootstrap

Composition root at feature root (`SkillSetup`, `GameSceneRoot`, etc.) — **not** inside a `Bootstrap/` folder.

Allowed: Object creation, dependency wiring, initialization order, subscribing (delegate handling to Application EventHandlers).

Not allowed: Event handling logic in Bootstrap, business logic, domain entity creation (use UseCase).

Rules: No business or rendering logic; **one** Setup or Bootstrap class at feature root (see also `anti_patterns.md`).

---

## Naming conventions

* **Entity:** no suffix — `Lobby`, `Room`, `RoomMember`
* **Use cases:** e.g. `CreateRoomUseCase`, `JoinRoomUseCase` (per feature, often consolidated as `LobbyUseCases`, `PlayerUseCases` — see existing features)
* **NetworkEventHandler:** `LobbyNetworkEventHandler`, `PlayerNetworkEventHandler`
* **Port interface:** `ILobbyRepository`, `IPlayerNetworkCommandPort` — use clear feature context, avoid overly generic names
* **Event:** past tense + `Event` — `LobbyUpdatedEvent`, `GameStartedEvent`
* **EventBus:** `IEventBus`, `EventBus` in `Shared/EventBus/`
* **Adapter:** `LobbyPhotonAdapter`, `ClockAdapter`
* **View:** `LobbyView`, `RoomListView`
* **InputHandler:** `LobbyInputHandler`

---

## asmdef (optional future)

There is **no** per-feature `asmdef` under `Assets/Scripts/Features/**` today; layers are enforced by convention and review. If assemblies are split later, update **this file** and `docs/architecture-diagram.md` together.
