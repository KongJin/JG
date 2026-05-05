# Unity 코딩 규칙

스크립트, 의존성, 탐색 정책, 이벤트, UI 생성에 대한 규칙.

---

## 4. AssetDatabase / Editor 스크립트

**규칙:** `Assets/Editor/` 브리지 코드는 최소한으로 유지.

- Editor 전용 코드는 `#if UNITY_EDITOR` 가드로 감싼다
- AssetDatabase 조작 후 `AssetDatabase.Refresh()` 호출
- Editor 스크립트 변경은 런타임 빌드에 영향 없음을 확인

---

## 7. 프리팹 인스턴스화 후 초기화

**규칙:** `PhotonNetwork.Instantiate` 또는 `Instantiate`로 생성된 객체는 명시적인 `Initialize()` 호출로 의존성을 주입한다.

- ✅ `setup.Initialize(eventBus, ...)` — 명시적 주입
- ❌ `FindObjectOfType`로 의존성 탐색 — 런타임 탐색 금지
- ❌ 정적 이벤트로 암묵적 초기화 — 우발적 타이밍 의존

---

## 8. 런타임 탐색 정책

**규칙:** 런타임 탐색은 scene/prefab 계약을 대체할 수 없다.

**기본 금지:**
- 의존성 획득을 위한 `GetComponent`
- 누락된 의존성 복구를 위한 `AddComponent`
- `FindObjectOfType` / `FindObjectsByType`
- `Resources.FindObjectsOfTypeAll`
- 런타임 객체 발견을 위한 scene scan

**허용 예외 (모든 조건을 만족해야 함):**
- 동일 GameObject 내부 helper 탐색만
- 일회성 획득만

**helper 정의:**
- `transform.GetComponentsInChildren<IHelper>()` — 자식 GameObject 포함
- `GetComponentInChildren<IHelper>()` — 첫 번째 일치 항목

**허용 예시:**
```csharp
// ✅ 허용: 동일 GameObject 내부 helper 탐색
var helpers = transform.GetComponentsInChildren<IHelper>();
foreach (var helper in helpers)
{
    helper.Process(); // 일회성 사용
}
```

**금지 예시:**
```csharp
// ❌ 금지: 의존성 획득을 위한 GetComponent
private EventBus _eventBus;

void Awake()
{
    _eventBus = GetComponent<EventBus>(); // scene 계약 대신 런타임 탐색
}
```
- Unity/Photon이 해당 사례를 inspector에서 연결할 수 없어서 필요한 경우
- 사용 사이트에 짧은 주석으로 정당화 또는 예외가 지속되면 전역 규칙 문서에 기록
- 누락된 scene 참조의 폴백으로 사용 금지

> **Bootstrap 위치 규칙**: [`bootstrap.md`](../rule-architecture/bootstrap.md#위치)

**DDOL singleton (Shared 인프라 전용):**

**기준:**
- 폴더 경로: `Assets/Scripts/Shared/**` 인프라에만 허용
- 조건: "프로세스 전체 서비스" + "단일 인스턴스 필수"인 경우

**허용 예시:**
```csharp
// Assets/Scripts/Shared/Infrastructure/SoundPlayer.cs
// ✅ 허용: 오디오 시스템은 프로세스 전체 하나만 필요
public class SoundPlayer : MonoBehaviour
{
    public static SoundPlayer Instance { get; private set; }

    [Required, SerializeField] private EventBus _initialEventBus;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(EventBus sceneEventBus)
    {
        // 첫 scene: [Required]로 연결된 버스 사용
        // 이후 scene: EventBus를 재바인딩
    }
}
```

**금지 예시:**
```csharp
// Assets/Scripts/Features/Unit/Presentation/GameManager.cs
// ❌ 금지: feature 코드에 임의 DDOL singleton
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    // ...
}
```

---

## 9. Scene 계약

**규칙:** scene 소유 feature는 `Setup`/`Bootstrap`, 직렬화 scene/prefab 참조, 관련 코드 경로에서 scene 계약을 명시적으로 유지해야 한다.

필요한 scene 계약 체크리스트는 repo owner docs를 우선하고, fallback으로 `rule-architecture`가 소유한다. 로컬 문서에서 이 체크리스트를 재정의하지 않는다. 실제 코드/scene 연결을 최신 상태로 유지하고 필요시 architecture 규칙을 참조.

---

확실하지 않을 때:

Shared보다 현재 feature 안에 코드를 유지하는 것을 선호한다.

---

## 10. 의존성 필드 규칙

**의존성 필드 멤버 3가지 규칙** (`[SerializeField]`, 주입된 참조):

1. **한 번에 수신**: constructor 또는 `Initialize()`에서만. 별도 setter 메서드 금지 (예: `SetXxx()`).
2. **올바른 레이어에서 검증**: `[Required, SerializeField]` 필드는 Editor가 씬/프리팹 저장 시 검증하므로 런타임 null-check 불필요. `Initialize()` 파라미터 등 Inspector에서 검증할 수 없는 값만 런타임에 `Debug.LogError`로 검증한다.
3. **초기화 후 신뢰**: 이벤트 핸들러, Update, 기타 런타임 메서드에서 재검증 금지. 런타임에 null이면 초기화 버그 — NullReferenceException이 표면화되도록 둔다.

**`[Required]` 범위 규칙:**
`[Required]`는 Inspector에서 연결되는 **참조 의존성**에만 사용한다. `bool`, `int`, `float`, `enum`, `Color`, `Vector*`, `string` 같은 스칼라/config 값에는 붙이지 않는다. 스칼라 값 검증은 `Range`, `Min`, 도메인 검증, 전용 validator로 처리한다.

---

## 11. 타입 enum 기반 조건 분기

enum 기반 switch로 타입별 행동을 분기하지 않는다. Factory + Strategy 패턴을 사용한다.

**허용:** 명령 디스패치와 단순 값 매핑은 switch 허용.

**허용 예시:**

**1. 명령 디스패치 (Command Pattern)**
```csharp
// ✅ 허용: 명령 디스패치
public enum CommandType { Move, Attack, Defend }

public void ExecuteCommand(CommandType type)
{
    switch (type)
    {
        case CommandType.Move: ExecuteMove(); break;
        case CommandType.Attack: ExecuteAttack(); break;
        case CommandType.Defend: ExecuteDefend(); break;
    }
}
```

**2. 단순 값 매핑**
```csharp
// ✅ 허용: 단순 값 매핑
public enum Theme { Light, Dark, System }

public Color GetThemeColor(Theme theme)
{
    switch (theme)
    {
        case Theme.Light: return Color.white;
        case Theme.Dark: return Color.black;
        case Theme.System: return Color.gray;
    }
}
```

**금지 예시 (Strategy 패턴 사용 필요):**

**상태/행동/능력 분기**
```csharp
// ❌ 금지: 상태별 행동은 Strategy 패턴 사용
public enum UnitState { Idle, Moving, Attacking }

public void Update()
{
    switch (_state)  // ❌
    {
        case UnitState.Idle: /* ... */
        case UnitState.Moving: /* ... */
        case UnitState.Attacking: /* ... */
    }
}

// ✅ 올바름: Strategy 패턴
public interface IUnitStateStrategy { void Update(Unit unit); }
public class IdleStateStrategy : IUnitStateStrategy { /* ... */ }
public class MovingStateStrategy : IUnitStateStrategy { /* ... */ }
public class AttackingStateStrategy : IUnitStateStrategy { /* ... */ }
```

**Strategy 패턴 파일 구조:**
- `UnitState.cs` — enum, interface, factory를 한 파일에
- `IdleStateStrategy.cs` — 개별 파일
- `MovingStateStrategy.cs` — 개별 파일
- `AttackingStateStrategy.cs` — 개별 파일

---

## 12. 정적 이벤트 규칙

정적 이벤트는 엔진/네트워크 콜백을 Application 또는 Bootstrap으로 bridge할 때만 예외적으로 허용한다. gameplay event bus 대체제로 사용하지 않는다. 사용 시 `OnDestroy` 해제와 README 명시는 필수다.

---

## 13. 운영 환경 런타임 UI 생성

운영용 UI는 scene 소유 또는 prefab 소유여야 한다. 런타임 UI 생성은 디버깅 도구 또는 일시적 마이그레이션에서만 허용한다.


---

## 14. Editor 코드에서 제외되는 규칙

### 제외되는 영역
- `Assets/Editor/**` — Editor 전용 코드
- `Assets/FromStore/**` — 외부 라이브러리 (Photon, DOTween 등)
- `Assets/Editor/UnityMcp/**` — MCP 브리지

### 제외되는 규칙
`#if UNITY_EDITOR` 가드로 감싸진 코드는 다음 규칙에서 제외된다:
- 런타임 탐색 금지 (GetComponent, FindObjectOfType 등)
- 정적 이벤트 규칙
- 의존성 필드 규칙 (일부)

### 예외 상황 처리 가이드
- **외부 라이브러리**: Photon, DOTween 등 엔진/네트워크 콜백은 규칙 적용 범위 외
- **Editor 전용 코드**: `#if UNITY_EDITOR` 가드로 감싸면 런타임 동작에 영향 없음
- **Shared 인프라**: `Assets/Scripts/Shared/**` 인프라 코드는 일부 규칙 제한적 적용

> **참고:** 적용 영역 상세는 [SKILL.md](SKILL.md#적용-영역) 참조
