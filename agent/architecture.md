# /agent/architecture.md

**코드 구조·피처 경계·의존 방향·레이어 책임·네이밍·크로스 피처 포트**의 단일 근거(SSOT)다.  
(기존 `dependency_rules.md`, `layer_rules.md`, `feature_rules.md`, `naming_rules.md`를 이 파일로 통합했다.)

시각 요약(Mermaid): [`docs/architecture-diagram.md`](../docs/architecture-diagram.md) — 표·본문은 **이 문서**와 같아야 한다.

---

## Folder structure

The project follows **Feature-first Clean Architecture**.

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

Each feature must be self-contained.

* Do not move feature-specific code into Shared.
* Shared must only contain reusable cross-feature utilities.
* Infrastructure implements Application ports.
* Domain must stay framework-independent.
* Bootstrap (Setup/Bootstrap classes at feature root) handles composition and wiring between layers.
* Scene-owned features must define a scene contract in their **feature `README.md`**.
* Networked features must define one explicit initialization path and late-join behavior in their **feature `README.md`**.
* Runtime fallback creation must not replace missing scene/prefab setup.

Features should grow independently. Only split a feature when a concept gains an independent lifecycle.

### Scene contract (README)

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

**One EventBus per scene.** Each scene Bootstrap (e.g. `GameSceneBootstrap`, `LobbyBootstrap`) creates `new EventBus()` once and injects it into every feature in that scene.

```
GameSceneBootstrap
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

Composition root at feature root (`SkillSetup`, `GameSceneBootstrap`, etc.) — **not** inside a `Bootstrap/` folder.

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
