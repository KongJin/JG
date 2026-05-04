# Presentation Layer Guardrails

> 마지막 업데이트: 2026-05-04
> 상태: active
> doc_id: ops.presentation-layer-guardrails
> role: ssot
> owner_scope: Presentation Layer 코딩 규칙, 안티패턴 방지
> upstream: ops.codex-coding-guardrails
> artifacts: none

Presentation Layer 구현 시 다음 안티패턴을 방지한다.

## Memory Leak Prevention

**규칙**: 이벤트 핸들러를 등록하는 클래스는 반드시 해제 로직을 제공해야 한다.

- UI Element (`Button`, `TextField`, `Label.clicked`, etc.)에 이벤트를 등록하면 `IDisposable`을 구현한다.
- `OnDestroy`, `Dispose`, 또는 명시적 `Unbind()`에서 이벤트를 해제한다.
- 이벤트 해제 시 null 체크를 포함한다.
- `MonoBehaviour`의 `OnDestroy`에서 모든 `IDisposable` 종속성을 호출한다.

```csharp
// ✅ 올바른 예
internal sealed class SampleSurface : IDisposable
{
    private readonly Button _button;
    private EventCallback<ClickEvent> _clickCallback;
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_button != null && _clickCallback != null)
            _button.UnregisterCallback(_clickCallback);
        _clickCallback = null;
    }

    private void BindCallbacks()
    {
        if (_isDisposed) return;
        _clickCallback = evt => OnButtonClicked();
        _button.RegisterCallback(_clickCallback);
    }
}
```

```csharp
// ❌ 안티패턴: 이벤트 해제 없음
internal sealed class SampleSurface
{
    private readonly Button _button;

    private void BindCallbacks()
    {
        _button.clicked += OnButtonClicked;  // 해제되지 않음
    }
}
```

## Layer Dependency Rules

**규칙**: Presentation Layer는 상위 계층(Domain, Application, Shared)에만 의존한다.

- `Presentation → Domain`: ✅ 허용
- `Presentation → Application`: ✅ 허용 (가능하면 인터페이스 경유)
- `Presentation → Runtime`: ⚠️ 최소화 (UI 관련만 허용)
- `Presentation → Infrastructure`: ❌ 금지
- `Presentation → 다른 Feature.Presentation`: ⚠️ 최소화

**예외**: 디버깅/프리뷰 전용 클래스는 주석으로 명시해야 한다.

```csharp
/// <summary>
/// 프리뷰 씬에서 UI 테스트용 드라이버입니다.
/// NOTE: 디버깅 전용으로 Infrastructure에 직접 의존합니다.
/// 프로덕션 코드에서는 이 패턴을 따르지 마세요.
/// </summary>
public sealed class PreviewSceneDriver : MonoBehaviour
```

## God Object Prevention

**규칙**: 하나의 클래스는 하나의 명확한 책임만 가져야 한다.

- 클래스 필드 수가 15개를 넘으면 책임 분리를 고려한다.
- UI Element 바인딩, 렌더링 조율, 상태 관리는 별도 클래스로 분리한다.
- `RuntimeAdapter`는 Unity 생명주기 연결만 담당하고, 실제 로직은 Core 클래스에 위임한다.

**권장 구조**:
```
RuntimeAdapter (MonoBehaviour) → AdapterCore (POCO) → RenderCoordinator, ElementBindings
```

## MonoBehaviour Coupling Reduction

**규칙**: UI 로직은 POCO 클래스로 분리하고 `MonoBehaviour`는 연결만 담당한다.

- 인터페이스를 정의하여 테스트 가능성을 확보한다.
- Core 클래스는 `MonoBehaviour` 생명주기에 의존하지 않는다.
- UI Toolkit core/surface는 `VisualElement`, `Button`, `Transform` 같은 UI authoring 타입을 가질 수 있지만 scene lookup, document visibility, host clone, runtime repair는 맡지 않는다.
- `MonoBehaviour`는 생명주기 메서드(`OnEnable`, `OnDestroy`)만 구현한다.

```csharp
// ✅ 올바른 예
public interface IAdapter : IDisposable
{
    event Action Event;
    void Render(ViewModel vm);
}

internal sealed class AdapterCore : IAdapter { ... }  // POCO

public sealed class RuntimeAdapter : MonoBehaviour
{
    private IAdapter _core;
    private void OnDestroy() => _core?.Dispose();
}
```

## Initialization Guard

**규칙**: PageController의 반복 null 체크는 초기화 guard에서 한 번만 검증한다.

- `Initialize()`에서 serialized adapter와 injected dependency를 검증하고, 누락 시 fail-closed로 드러낸다.
- `CanRender()`는 guard 상태만 확인하며 dependency 목록을 매 렌더마다 재나열하지 않는다.
- guard가 ready가 된 뒤 생기는 missing dependency는 silent skip이 아니라 wiring/contract 오류로 다룬다.

## Runtime Adapter Boundary

**규칙**: `RuntimeAdapter`는 Unity 생명주기와 core binding만 맡는다.

- `UIDocument` 표시/숨김, host clone, shell embed 같은 document 관리는 별도 `*DocumentHost`/host owner로 분리한다.
- 기존 public compatibility 메서드는 남기더라도 document host로 얇게 위임하고 새 로직을 추가하지 않는다.
- 다른 feature shell이 Garage/Lobby 같은 concrete Presentation 타입을 직접 조작해야 하면 host seam을 먼저 둔다.

## Async Operation State

**규칙**: Presentation 비동기 작업은 UI 상태와 취소/late result 처리를 함께 가진다.

- fire-and-forget `Task`는 operation handle 또는 동등한 상태 객체로 `loading/saving`, operation name, cancellation requested를 추적한다.
- 작업 시작/종료 시 렌더 상태를 갱신하고, 취소 후 도착한 결과는 commit/render success로 승격하지 않는다.
- `OnDestroy`는 진행 중인 operation을 취소하고 UI event 구독을 해제한다.

## Event Handler Management

**규칙**: 이벤트 핸들러는 람다 대신 명시적 메서드 또는 캡처된 `EventCallback`을 사용한다.

- 람다를 사용하면 이벤트 해제가 불가능하다.
- 명시적 메서드를 사용하거나 `EventCallback<T>`을 필드에 저장한다.
- `+=` 등록 시 `-=` 해제 로직을 동시에 작성한다.

```csharp
// ✅ 올바른 예
private EventCallback<ClickEvent> _clickCallback;

private void Bind()
{
    _clickCallback = evt => OnClicked();
    _button.RegisterCallback(_clickCallback);
}

private void Unbind()
{
    _button.UnregisterCallback(_clickCallback);
}
```

```csharp
// ❌ 안티패턴: 람다로 인해 해제 불가
_button.clicked += () => DoSomething();  // 해제할 방법 없음
```

## Surface Pattern

**규칙**: 이벤트를 등록하는 surface는 공통 dispose 패턴을 사용한다.

- `BindCallbacks()`가 있는 UITK surface는 `Dispose()` 대칭 경로를 가져야 한다.
- 같은 feature 안에 surface가 둘 이상이면 `BaseSurface` 또는 동일한 작은 lifecycle helper로 `_isDisposed`, `Dispose`, `UnbindCallbacks` 중복을 줄인다.
- 공통화는 lifecycle에 한정하고, 서로 다른 render 책임을 한 base class에 합치지 않는다.

## ViewModel Factory Complexity

**규칙**: ViewModel factory는 orchestration을 맡고 option 생성, filtering, stat scaling, text formatting은 helper owner로 분리한다.

- `Build()` 안에서 catalog 순회, compatibility filtering, stat percent 계산이 함께 늘어나면 `*OptionBuilder`, `*TextFormatter`, `*StatBuilder`로 분리한다.
- factory는 selected state와 final ViewModel 조립 흐름을 읽을 수 있는 수준으로 유지한다.
- 중복 제거보다 failure owner 분리가 우선이다.

## Theme Tokens

**규칙**: Presentation code에 색상 literal을 직접 두지 않는다.

- 공통 UI 색상은 `Shared.Ui` theme token을 사용한다.
- feature 전용 preview/background 색상은 feature theme owner에 둔다.
- 예외적으로 임시 debug 색상을 쓸 때는 debug/preview 전용 클래스임을 주석으로 명시한다.

## Primitive Obsession Prevention

**규칙**: 도메인 개념은 원시 타입 대신 값 객체(Value Object)를 사용한다.

- UI 상태, 식별자, 선택 항목은 전용 타입으로 감싼다.
- 관련된 여러 원시값은 하나의 값 객체로 묶는다.

```csharp
// ✅ 올바른 예
public readonly struct SlotIndex
{
    private readonly int _value;
    public SlotIndex(int value) => _value = Mathf.Clamp(value, 0, MaxSlots - 1);
    public int Value => _value;
}

// ❌ 안티패턴: 원시 타입 그대로 사용
public int SelectedSlotIndex;  // 의미가 불명확
```

## Historical Context

**발견 날짜**: 2026-05-04
**수정된 파일**:
- `GarageSetBPartListSurface.cs` - IDisposable 추가, 이벤트 해제
- `GarageSetBSlotSurface.cs` - IDisposable 추가
- `GarageSetBUitkRuntimeAdapter.cs` - God Object 분해 (301줄 → 130줄)
- 새 파일: `GarageSetBUitkElementBindings.cs`, `GarageSetBUitkRenderCoordinator.cs`
- 새 파일: `IGarageSetBUitkAdapter.cs`, `GarageSetBUitkAdapterCore.cs`
