# /agent/feature_rules.md

## Feature Isolation

Each feature owns:

* Domain
* Application
* Presentation
* Infrastructure
* Setup/Bootstrap class (at feature root, NOT in Bootstrap folder)

Example:

Assets/Scripts_/Features/
Lobby/
Domain/
Application/
Presentation/
Infrastructure/
LobbySetup.cs        # Bootstrap: composition root
LobbyBootstrap.cs    # Bootstrap: scene-level wiring

Rules:

* Cross-feature dependency is allowed as long as layer direction is respected (same-or-inner layer only).
* Wiring/composition across a feature's layers must live in that feature's Setup/Bootstrap class.

Keep concepts inside a feature unless they have an independent lifecycle.

Example:

Room belongs to Lobby if its lifecycle depends on Lobby.

If Room gains independent lifecycle, it may become its own feature later.

---

## EventBus 소유 모델

**씬 단일 EventBus.** 각 씬의 Bootstrap(예: `GameSceneBootstrap`, `LobbyBootstrap`)이 `new EventBus()`를 하나 생성하고, 해당 씬의 모든 피처에 주입한다.

```
GameSceneBootstrap
  └─ _eventBus = new EventBus()
       ├─ PlayerSetup.Initialize(eventBus, ...)
       ├─ CombatBootstrap.Initialize(eventBus, ...)
       ├─ StatusSetup.Initialize(eventBus, ...)
       ├─ SkillSetup.Initialize(eventBus, ...)
       └─ WaveBootstrap.Initialize(eventBus, ...)
```

규칙:

* 피처는 자체 EventBus를 생성하지 않는다. 씬 Bootstrap에서 주입받는다.
* 씬이 다르면 EventBus도 다르다 (Lobby 씬과 Game 씬은 별개).
* IEventPublisher / IEventSubscriber 분리(ISP)는 실제 필요가 생기면 도입한다. 현재는 `IEventBus` 단일 인터페이스로 충분하다.
