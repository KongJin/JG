# /agent/anti_patterns.md

## Anti Patterns

Never do these:

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

* Dual state — the same concept (health, position, etc.) must not be managed by two domain entities independently. Only one Source of Truth may exist. Example: Player.CurrentHp and CombatTarget.CurrentHealth existing simultaneously will inevitably diverge.
* Network-shared entity with local ID — 네트워크로 공유되는 엔티티(플레이어 등)의 ID를 `DomainEntityId.New()`로 로컬 생성하면 안 된다. 반드시 네트워크 안정 소스(Photon ActorNumber, ViewID 등)에서 파생해야 한다. `DomainEntityId.New()`는 한 클라이언트 안에서만 존재하는 엔티티(투사체, 스킬 인스턴스 등)에만 사용한다.
* Dual-path damage — do not apply damage twice for a single event (e.g. projectile hit). One UseCase calculates damage; other features react via result events.
* Port on provider instead of consumer — when feature A calls feature B, define the port interface in A's Application. Implementation goes in B's Infrastructure. (Dependency Inversion Principle)
* Bootstrap containing selection/cycling logic — Bootstrap only wires. Skill rotation, next-target selection, etc. must be extracted to a dedicated Application-layer class.
* Unity types in Application — do not put Sprite, GameObject, AudioClip, Color, Debug.Log/LogWarning/LogError, or any UnityEngine API in Application-layer events, ports, or use cases. If a port needs Unity types, it belongs in Presentation, not Application/Ports. Logging belongs in Bootstrap/Infrastructure.
* Single-reference private function — 호출 지점이 하나뿐인 private 메서드는 추출하지 않는다. 호출부에 직접 인라인한다. 읽는 사람이 한 함수 안에서 흐름을 따라갈 수 있어야 한다. 예외: (1) 콜백/이벤트 핸들러로 등록되는 함수, (2) 두 곳 이상에서 호출되는 함수.
* Type naming collision — one feature 안에서 같은 short type name의 `MonoBehaviour`를 두 개 만들지 않는다. Presentation 컴포넌트 이름도 feature 전체에서 유일해야 한다.
* Static event discipline — static event는 엔진/네트워크 콜백을 Application or Bootstrap으로 bridge할 때만 예외적으로 허용한다. gameplay event bus 대체제로 사용하지 않는다. 사용 시 `OnDestroy` 해제와 README 명시는 필수다.
* Runtime UI creation in production — 운영용 UI는 scene-owned 또는 prefab-owned이어야 한다. runtime UI 생성은 debug tooling 또는 일시적 migration에서만 허용한다.

---

## Established Patterns (Lessons Learned)

These patterns were discovered through refactoring and should be followed:

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
- documented in the feature README
- not used as fallback for missing scene references

### 7. Scene Contract
**Rule:** Scene-owned features must declare their scene contract in the feature README.

Must list:
- required GameObjects/components
- required serialized references
- runtime-created objects
- forbidden runtime-created replacements
- allowed lookup exceptions
- initialization order
- late-join / reconnect behavior (if networked)

---

When unsure:

Prefer keeping code inside the current feature rather than Shared.
