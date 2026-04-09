# /agent/anti_patterns.md

## 하지 말아야 할 것

이 문서는 구조를 새로 정의하지 않는다. 구조 정의는 `architecture.md`를 따르고, 여기서는 구현 중 금지 패턴과 예외 판단만 다룬다.

다음 패턴은 금지한다:

* Put business logic inside View or InputHandler.
* Put networking logic inside Domain.
* Put Unity API usage inside Domain.
* Put feature-specific code inside Shared.
* Let Bootstrap become a god class.
* Make one port responsible for unrelated behaviors.
* Introduce architectural layers not defined in architecture.md.

* Dependency field discipline — three rules for dependency field members (`[SerializeField]`, injected references):
  1. **Receive all at once**: constructor or `Initialize()` only. No separate setter methods (e.g. `SetXxx()`).
  2. **Validate at the right layer**: `[Required, SerializeField]` 필드는 Editor가 씬/프리팹 저장 시 검증하므로 런타임 null-check 불필요. `Initialize()` 파라미터 등 Inspector에서 검증할 수 없는 값만 런타임에 `Debug.LogError`로 검증한다.
  3. **Trust after init**: do not re-check in event handlers, Update, or other runtime methods. If null at runtime, the bug is in initialization — let NullReferenceException surface it.
* `[Required]` scope discipline — `[Required]`는 Inspector에서 연결되는 **reference dependency**에만 사용한다. `bool`, `int`, `float`, `enum`, `Color`, `Vector*`, `string` 같은 scalar/config 값에는 붙이지 않는다. scalar 값 검증은 `Range`, `Min`, 도메인 검증, 전용 validator로 처리한다.
* Behavioral switch on type enums — use Factory + Strategy pattern instead. Switch is acceptable for command dispatch and simple value mapping.
* Strategy pattern file structure — enum, interface, factory go in one file. Each implementation (Strategy class) gets its own file.
* GetComponent for dependency acquisition — use `[Required, SerializeField]`로 선언하고 Inspector에서 연결한다. `GetComponent`/`FindObjectsByType`/`FindObjectOfType`/`Resources.FindObjectsOfTypeAll` 등 런타임 탐색은 의존성 획득이나 씬 wiring에 사용하지 않는다. `[Required]`는 씬/프리팹 저장 시 Editor가 자동 검증하므로 런타임 null 방어 코드도 불필요하다.
* Runtime fallback wiring — missing scene/prefab references must not be silently repaired with `GetComponent`, `AddComponent`, `CreateDefault`, or runtime UI creation. Fix the scene/prefab instead.
* Script edit during play mode expectation — Unity 플레이 중에 C# 스크립트나 `Assets/Editor/**` 브리지 코드를 수정하고 즉시 컴파일/적용될 것이라 가정하지 않는다. 이런 수정이 필요하면 반드시 `Play Stop -> 스크립트 수정 -> 컴파일 완료 확인 -> 테스트 재개` 순서를 따른다. MCP 호출·로그 확인 SOP는 `/docs/ops/unity_mcp.md`를 따른다.

* Dual state — the same concept (health, position, etc.) must not be managed by two domain entities independently. Only one Source of Truth may exist. Example: Player.CurrentHp and CombatTarget.CurrentHealth existing simultaneously will inevitably diverge.
* Network-shared entity with local ID — 네트워크로 공유되는 엔티티(플레이어 등)의 ID를 `DomainEntityId.New()`로 로컬 생성하면 안 된다. 반드시 네트워크 안정 소스(Photon ActorNumber, ViewID 등)에서 파생해야 한다. `DomainEntityId.New()`는 한 클라이언트 안에서만 존재하는 엔티티(투사체, 스킬 인스턴스 등)에만 사용한다.
* Dual-path damage — do not apply damage twice for a single event (e.g. projectile hit). One UseCase calculates damage; other features react via result events.
* Port on provider instead of consumer — when feature A calls feature B, define the port interface in A's Application. Implementation goes in B — 외부 의존(Photon, DB, SDK)이 있으면 B의 Infrastructure, 순수 도메인 상태 조회/계산이면 B의 Application이 직접 구현해도 된다. (Dependency Inversion Principle)
* Bootstrap containing selection/cycling logic — Bootstrap only wires. Skill rotation, next-target selection, etc. must be extracted to a dedicated Application-layer class.
* Unity types in Application — do not put Sprite, GameObject, AudioClip, Color, Debug.Log/LogWarning/LogError, or any UnityEngine API in Application-layer events, ports, or use cases. If a port needs Unity types, it belongs in Presentation, not Application/Ports. Logging belongs in Bootstrap/Infrastructure.
* Single-reference private function — 호출 지점이 하나뿐인 private 메서드는 추출하지 않는다. 호출부에 직접 인라인한다. 읽는 사람이 한 함수 안에서 흐름을 따라갈 수 있어야 한다. 예외: (1) 콜백/이벤트 핸들러로 등록되는 함수, (2) 두 곳 이상에서 호출되는 함수.
* Type naming collision — one feature 안에서 같은 short type name의 `MonoBehaviour`를 두 개 만들지 않는다. Presentation 컴포넌트 이름도 feature 전체에서 유일해야 한다.
* Feature short-type shadowing — feature namespace 이름과 같은 short type (`Unit`, `Player`, `Wave` 등)을 bare identifier로 사용해 namespace/type 충돌을 유발하지 않는다. alias 또는 fully-qualified name을 사용한다.
* Static event discipline — static event는 엔진/네트워크 콜백을 Application or Bootstrap으로 bridge할 때만 예외적으로 허용한다. gameplay event bus 대체제로 사용하지 않는다. 사용 시 `OnDestroy` 해제와 README 명시는 필수다.
* Runtime UI creation in production — 운영용 UI는 scene-owned 또는 prefab-owned이어야 한다. runtime UI 생성은 debug tooling 또는 일시적 migration에서만 허용한다.
* Silent fallback in exhaustive switch — enum을 switch로 분기할 때, default에서 fallback 값을 반환하지 않는다. 새 enum 값 추가 시 컴파일러가 경고하지 않으므로, default는 `throw new ArgumentOutOfRangeException()`으로 즉시 실패시킨다. `Debug.LogError` + fallback 반환은 문제를 숨긴다. (실제 사례: SkillData.CreateDelivery에서 default가 SelfDelivery를 반환하여 새 DeliveryType 추가 시 silent corruption 가능성이 있었음)
* Phantom shared contract names — Shared에 실제 선언되지 않은 계약 이름을 가정해서 쓰지 않는다. 예: `IEventBus`를 실제 선언 없이 새 공용 계약처럼 사용하는 것 금지. Shared 계약은 실제 선언 파일을 기준으로 참조한다.
* Missing import after symbol move — `RequiredAttribute`, `GarageRoster`, `StatusNetworkAdapter`, `Func<>` 같이 자주 이동하거나 namespace가 분명한 심볼은 사용 시 필요한 `using` 또는 fully-qualified name을 명시한다. IDE가 알아서 잡아줄 것이라 가정하지 않는다.
* Event contract drift — 이벤트 producer/consumer/bridge가 서로 다른 필드 집합을 가정하는 상태를 허용하지 않는다. 이벤트 payload 변경 시 producer, consumer, bridge를 함께 검토한다. 예: `GameEndEvent`에 없는 `IsLocalPlayerDead`를 consumer가 계속 참조하는 상태.
* Concrete/interface drift — 한쪽은 interface, 한쪽은 concrete 구현체를 요구해 wiring이 깨지는 상태를 허용하지 않는다. Bootstrap만 concrete를 직접 조립하고, 그 밖의 레이어는 최소 계약을 받는다. 예: `EventBus` / `IEventPublisher` 가정이 엇갈려 `SummonPhotonAdapter` wiring이 깨지는 상태.
* Scene-owned helper in Domain — `PlacementArea`처럼 Unity 의존 scene helper, 입력 판정 helper, 시각화 보조 타입을 Domain에 두지 않는다. 이런 타입은 Presentation 또는 scene-owned contract 쪽에 둔다.
* Subscription return value assumption — `Subscribe()`의 반환형을 임의로 `IDisposable`처럼 취급하지 않는다. EventBus ownership 해제는 `EventBusSubscription.ForOwner(...)` 또는 명시적 cleanup으로 처리한다.
* Stale symbol after refactor — 리팩터링 전 필드/메서드/타입명을 계속 참조하는 상태를 허용하지 않는다. 예: 이동된 타입, 삭제된 이벤트 필드, 바뀐 adapter 이름을 그대로 쓰는 상태.

---

## 리팩터링 교훈

아래 패턴은 리팩터링 과정에서 확인된 운영 규칙이며, 새 구현에도 그대로 따른다.

### 1. Port Placement by Type Dependency
**Rule:** Port interfaces go in Application/Ports ONLY if they use no Unity types. Ports that reference UnityEngine types (Sprite, GameObject, AudioClip, Color, etc.) belong in Presentation.
- ✅ `Application/Ports/IZoneEffectPort.cs` — uses only `Float3` (Shared), OK in Application
- ✅ `Presentation/ISkillEffectPort.cs` — uses `GameObject`, `AudioClip` (Unity), must be in Presentation
- ✅ `Presentation/ISkillIconPort.cs` — uses `Sprite` (Unity), must be in Presentation
- ❌ `Application/Ports/ISkillEffectPort.cs` — Unity types in Application layer violates layer rules

**Why:** Application layer must not depend on UnityEngine. Moving a Unity-typed port to Application "infects" every Application class that references it.

### 2. Event Handlers → Application Layer
**Rule:** Event handling logic should be in Application layer, not Bootstrap.
- ❌ Bootstrap에서 `OnXxxEvent()` 메서드로 이벤트를 직접 구독·처리 — WRONG
- ✅ Application EventHandler가 생성자에서 EventBus를 직접 구독, Bootstrap은 생성 + `DisposableScope` 수명 관리만 — CORRECT

**Why:** Bootstrap should only wire components together. Event handling contains business logic that belongs in Application.

**Pattern:**
```
<Feature>Setup.cs / <Feature>Bootstrap.cs (wiring only)
  → Creates Application EventHandler (constructor receives IEventSubscriber)
  → EventHandler subscribes in its own constructor
  → Bootstrap tracks ownership: _disposables.Add(EventBusSubscription.ForOwner(eventBus, handler))
```

### 3. Bootstrap Responsibilities
**Bootstrap SHOULD:**
- Instantiate classes
- Inject dependencies (constructor injection)
- Subscribe to engine/network static events (Photon callbacks, `IPunInstantiateMagicCallback`, static `Action` events)
- Track EventHandler subscription ownership via `DisposableScope` (EventHandler가 생성자에서 EventBus를 직접 구독한다)
- Call Initialize() on components

**Bootstrap SHOULD NOT:**
- Execute business logic
- Create domain entities directly
- Contain selection/decision logic
- Handle game events
- Own runtime gameplay registries/queues unless strictly required for initialization bridging
- Drive gameplay flow via `Update`, `LateUpdate`, or coroutine loops

**Hard boundary:**
- If Bootstrap starts answering "what should happen now?", move that logic to Application.
- If Bootstrap stores runtime state for spawned objects beyond one-time initialization, move that state owner out of Bootstrap.
- If Bootstrap needs branching on network/gameplay rules, the rule belongs outside Bootstrap.
- If Bootstrap has `Update()` or coroutine that drives gameplay flow (countdown tick, state transition, spawn loop), extract to a Presentation MonoBehaviour that Bootstrap wires. (실제 사례: WaveBootstrap에 Update/SpawnWaveEnemies 코루틴이 있었고 WaveFlowController + EnemySpawnAdapter로 분리함)

### 3.1 Networked Object Initialization Contract
**Rule:** Every Photon-instantiated object type must have one explicit initialization contract.

**Required order:**
1. Photon object is instantiated
2. Setup/Adapter acquires runtime-only dependencies
3. Application EventHandler / UseCases are created
4. Presentation is initialized
5. Late-join / reconnect behavior is explicitly handled

**Rules:**
- Do not mix polling, scene scan, static lookup, and callback-based wiring for the same object type.
- Each networked feature must choose one arrival mechanism and document it in its README.
- Late join / reconnect behavior must be explicit, not accidental.
- Runtime initialization fallback must not overwrite authoritative setup data.

### 4. Infrastructure with MonoBehaviour
**Rule:** Infrastructure CAN have MonoBehaviour when needed for Unity integration.
- ✅ `CombatTargetAdapter : MonoBehaviour` — needs SerializeField for scene integration
- ✅ `PlayerNetworkAdapter : MonoBehaviourPun` — required for Photon

Pure C# adapters are preferred when possible.

### 5. Domain Entity Creation via UseCase
**Rule:** Bootstrap must not create domain entities directly.
```
❌ Bootstrap creates Domain.Projectile directly
✅ Bootstrap calls SpawnProjectileUseCase → UseCase creates Domain.Projectile
```

### 6. Runtime Lookup Policy
**Rule:** Runtime lookup is not a substitute for scene/prefab contracts.

**Forbidden by default:**
- `GetComponent` for dependency acquisition
- `AddComponent` to repair missing dependencies
- `FindObjectOfType` / `FindObjectsByType`
- `Resources.FindObjectsOfTypeAll`
- scene scans to discover required runtime objects

**Allowed exceptions (all conditions must be true):**
- same-GameObject local helper lookup only
- one-time acquisition only
- required because Unity/Photon cannot inspector-wire that specific case
- justified at the use site with a short comment or in the current global rule docs when the exception becomes durable
- not used as fallback for missing scene references

**DDOL singleton (Shared infrastructure only):**
- Default: do not introduce new static `Instance` + `DontDestroyOnLoad` patterns in feature code.
- Allowed only for `Shared/**` infrastructure when a single process-wide service is required. Example: `SoundPlayer.Instance` — one audio root, created from the lobby scene, `Initialize` rebinds per-scene `EventBus`. The first scene wires it with `[Required, SerializeField]`; later scenes use `Instance` only for rebind, not scene `Find*`.
- Forbidden: arbitrary DDOL singletons in feature Presentation/Infrastructure.

### 7. Scene Contract
**Rule:** Scene-owned features must keep their scene contract explicit in `Setup`/`Bootstrap`, serialized scene/prefab references, and related code paths.

The required scene-contract checklist is owned by `architecture.md`. Do not redefine that checklist differently in local docs; keep the actual code/scene wiring current and point back to the architecture rule when needed.

### 8. Compile-clean gate
**Rule:** 정적 아키텍처 규칙을 통과해도 Unity compile error가 있으면 `clean`이 아니다.

* 하네스 또는 사람이 `resolved`를 선언하기 전에 compile 상태를 확인한다.
* namespace drift, missing using, phantom contract, short-type shadowing, event contract drift, concrete/interface drift, scene-owned helper placement drift는 모두 compile-clean을 깨는 구조 문제로 본다.
* `validation_gates.md`의 `compile-clean` 정의를 함께 따른다.

---

When unsure:

Prefer keeping code inside the current feature rather than Shared.
