# Presentation & Domain Layer Anti-Pattern Analysis

> 마지막 업데이트: 2026-05-04
> 상태: reference
> doc_id: architecture.anti-pattern-analysis
> role: reference
> owner_scope: Presentation/Domain anti-pattern 조사 기록과 후속 재발방지 참고
> upstream: docs.index, ops.codex-coding-guardrails, ops.presentation-layer-guardrails
> artifacts: none

이 문서는 2026-05-04 기준 조사 기록이다. 현재 구현 기준은 owner 문서인
[`../ops/codex_coding_guardrails.md`](../ops/codex_coding_guardrails.md)와
[`../ops/presentation_layer_guardrails.md`](../ops/presentation_layer_guardrails.md)를 따른다.

---

## Presentation Layer Anti-Patterns

### 1. Feature Envy / Anemic Presenter

**위치**: `GaragePageViewModelBuilders.cs`

```csharp
// 문제: Domain 로직이 Presenter에 포함됨
private static float Normalize(float value, float max)
{
    if (max <= 0f) return 0f;
    var normalized = value / max;
    if (normalized < 0f) return 0f;
    return normalized > 1f ? 1 : normalized;
}

// 문제: Business Logic이 Presenter에 있음
private static string BuildRoleLabel(
    GaragePanelCatalog.FirepowerOption firepower,
    GaragePanelCatalog.MobilityOption mobility)
{
    if (mobility == null) return "역할 산출 대기";
    if (mobility.MoveRange <= 3.1f)
    {
        if (firepower != null && firepower.Range >= 6f)
            return "고정 화력";
        return "전선 고정";
    }
    // ...
}
```

**문제점**:
- 역할(Role) 계산 로직이 Presenter에 있어 Domain 업무 로직 누출
- 데이터 정규화 로직이 Presentation에 있음

---

### 2. Primitive Obsession

**위치**: 전반적으로 발생

```csharp
// 문제: ID를 string으로 처리
public string FrameId { get; }
public string FirepowerModuleId { get; }
public string MobilityModuleId { get; }

// 문제: Magic Strings 하드코딩
if (combined.Contains("방어") || combined.Contains("defense") ||
    combined.Contains("guard") || combined.Contains("고정"))
```

**문제점**:
- ID 검증 로직이 중복됨 (`string.IsNullOrWhiteSpace` 반복)
- 다국어 지원 어려움
- 리팩토링 시 모든 문자열 검색 필요

---

### 3. Data Clumps / God Object

**위치**: `GarageSlotViewModel.cs`, `GaragePanelCatalog.cs`

```csharp
// 문제: 40개 이상의 프로퍼티
public class GarageSlotViewModel
{
    public string SlotLabel => Display.SlotLabel;
    public string Title => Display.Title;
    public string Summary => Display.Summary;
    public string StatusBadgeText => Display.StatusBadgeText;
    public bool HasCommittedLoadout => Display.HasCommittedLoadout;
    public bool HasDraftChanges => Display.HasDraftChanges;
    public bool IsEmpty => Display.IsEmpty;
    public bool IsSelected => Display.IsSelected;
    public bool ShowArrow => Display.ShowArrow;
    public string Callsign => Display.Callsign;
    public string RoleLabel => Display.RoleLabel;
    public string ServiceTagText => Display.ServiceTagText;
    // ... 20개 이상 추가 프로퍼티
}

// 문제: 60개 이상의 프로퍼티 (PartAlignment)
public class PartAlignment
{
    public float NormalizedScale { get; set; }
    public Vector3 PivotOffset { get; set; }
    // ... 40개 이상 추가
}
```

**문제점**:
- 단일 책임 원칙 위배
- 테스트 어려움
- 변경 파급 효과 큼

---

### 4. Large Constructor

**위치**: `GarageSlotViewModel.cs`

```csharp
// 문제: 20개 이상의 파라미터
public GarageSlotViewModel(
    string slotLabel,
    string title,
    string summary,
    string statusBadgeText,
    bool hasCommittedLoadout,
    bool hasDraftChanges,
    bool isEmpty,
    bool isSelected,
    bool showArrow = false,
    string callsign = null,
    string roleLabel = null,
    string serviceTagText = null,
    string loadoutKey = null,
    string frameId = null,
    string firepowerId = null,
    string mobilityId = null,
    GameObject framePreviewPrefab = null,
    // ... 5개 이상 추가
)
```

**문제점**:
- 파라미터 순서 오류 가능성
- 가독성 저하
- 옵셔널 파라미터 관리 어려움

---

### 5. Shotgun Surgery

**위치**: `GaragePageState.cs`

```csharp
// 문제: 같은 로직이 여러 곳에 분산
public void SetEditingFrameId(string frameId)
{
    var slot = GetSelectedDraftSlot();
    DraftRoster.SetSlot(SelectedSlotIndex, new GarageRoster.UnitLoadout(
        frameId,
        slot.firepowerModuleId,
        slot.mobilityModuleId));
}

public void SetEditingFirepowerId(string firepowerId)
{
    var slot = GetSelectedDraftSlot();
    DraftRoster.SetSlot(SelectedSlotIndex, new GarageRoster.UnitLoadout(
        slot.frameId,
        firepowerId,
        slot.mobilityModuleId));
}

public void SetEditingMobilityId(string mobilityId)
{
    var slot = GetSelectedDraftSlot();
    DraftRoster.SetSlot(SelectedSlotIndex, new GarageRoster.UnitLoadout(
        slot.frameId,
        slot.firepowerModuleId,
        mobilityId));
}
```

**문제점**:
- 중복 코드
- 파라미터 추가 시 3곳 모두 수정 필요

---

### 6. State Leakage

**위치**: `GaragePageState.cs`

```csharp
// 문제: Domain Entity인 GarageRoster를 직접 노출
public GarageRoster CommittedRoster { get; private set; } = new GarageRoster();
public GarageRoster DraftRoster { get; private set; } = new GarageRoster();

// 외부에서 직접 조작 가능 (private setter지만 Clone으로 우회 가능)
public GarageRoster BuildSelectedSlotCommitRoster()
{
    var commitRoster = CommittedRoster.Clone();
    commitRoster.SetSlot(SelectedSlotIndex, GetSelectedDraftSlot());
    return commitRoster;
}
```

**문제점**:
- Presentation에서 Domain Entity를 직접 조작
- 불변성 보장 어려움

---

### 7. Magic Numbers

**위치**: `GarageUnitIdentityFormatter.cs`

```csharp
// 문제: Magic Numbers
if (mobility.MoveRange <= 3.1f)  // 3.1은 무엇?
{
    if (firepower != null && firepower.Range >= 6f)  // 6은 무엇?
        return "고정 화력";
}

if (mobility.MoveRange >= 6f)  // 또 6?
    return "침투 추적";
```

**문제점**:
- 의도가 명확하지 않음
- 상수로 정의되어야 함

---

### 8. Leaky Abstraction

**위치**: `GarageDraftEvaluation.cs`

```csharp
// 문제: 내부 구현 노출
public string ComposeError => WasComposeEvaluated
    ? ComposeResult.Error
    : "Garage catalog is unavailable.";  // 왜 이 메시지?
```

**문제점**:
- Evaluation 여부에 따른 동작이 외부에 노출
- 사용자가 내부 구현을 알아야 함

---

## Domain Layer Anti-Patterns

### 1. Anemic Domain Model

**위치**: `Unit.cs`

```csharp
// 문제: 데이터만 가지고 있음 (행동 없음)
public sealed class Unit
{
    public DomainEntityId Id { get; }
    public string FrameId { get; }
    public string DisplayName { get; }
    public float FinalHp { get; }
    public float FinalDefense { get; }
    public float FinalAttackDamage { get; }
    // ... 15개 이상 프로퍼티

    // 행동이 없음 - 모든 계산이 외부에서 수행됨
}
```

**문제점**:
- 도메인 로직이 Application 계층으로 빠짐
- 객체지향의 장점 활용 불가

**개선안**:
```csharp
public sealed class Unit
{
    // 행동 추가
    public bool CanAfford(int availableEnergy) => SummonCost <= availableEnergy;
    public bool IsInRange(float targetDistance) => targetDistance <= FinalRange;
    public float CalculateDamage(int targetDefense) => Math.Max(0, FinalAttackDamage - targetDefense);
}
```

---

### 2. Primitive Obsession

**위치**: `Unit.cs`, `StatusEffect.cs`

```csharp
// 문제: 개별 stat들이 개별 primitive 타입
public float FinalHp { get; }
public float FinalDefense { get; }
public float FinalAttackDamage { get; }
public float FinalAttackSpeed { get; }
public float FinalRange { get; }
```

**개선안**:
```csharp
public sealed class Stats
{
    public Health Health { get; }
    public Defense Defense { get; }
    public Attack Attack { get; }
    public Movement Movement { get; }
}

public readonly struct Health
{
    public float Current { get; }
    public float Max { get; }
    public float Percentage => Current / Max;
    public bool IsDead => Current <= 0;
}
```

---

### 3. Lack of Invariants

**위치**: `Unit.cs`, `GarageRoster.cs`

```csharp
// 문제: 생성자에서 유효성 검증 없음
public Unit(
    DomainEntityId id,
    string frameId,
    string displayName,
    // ...
    int summonCost)
{
    Id = id;
    FrameId = frameId;  // null 허용?
    DisplayName = displayName;  // null 허용?
    SummonCost = summonCost;  // 음수 허용?
}
```

**문제점**:
- 유효하지 않은 상태의 객체 생성 가능
- 도메인 불변성 보장 불가

---

### 4. Feature Envy in Domain

**위치**: `StatusContainer.cs`

```csharp
// 문제: 외부에 의존하는 static 메서드 호출
public void Apply(StatusEffect effect)
{
    var policy = StatusRule.GetPolicy(effect.Type);  // 외부 의존
    // ...
}

public void Tick(float deltaTime)
{
    for (var i = 0; i < _effects.Count; i++)
    {
        _effects[i].Tick(deltaTime);  // 책임 전가
    }
}
```

---

### 5. Mutable Value Objects

**위치**: `StatusEffect.cs`

```csharp
// 문제: Value Object인데 mutable
public sealed class StatusEffect
{
    public float Duration { get; private set; }  // setter 있음
    public float Elapsed { get; private set; }  // setter 있음
    public float TimeSinceLastTick { get; private set; }  // setter 있음

    public void Refresh(float newDuration)
    {
        Duration = newDuration;
        Elapsed = 0f;
    }
}
```

**문제점**:
- Value Object는 불변이어야 함
- 참조 투명성 위배

**개선안**:
```csharp
public sealed class StatusEffect
{
    public float Duration { get; }  // readonly
    public float Elapsed { get; }  // readonly

    public StatusEffect WithRefreshedDuration(float newDuration) =>
        new StatusEffect(Type, Magnitude, newDuration, SourceId, TickInterval);
}
```

---

### 6. Code Smell: Dead Code

**위치**: `GarageRoster.cs`

```csharp
// 문제: 사용되지 않는 메서드
public GarageRoster Clone()
{
    Normalize();
    var cloned = new List<UnitLoadout>(MaxSlots);
    for (int i = 0; i < MaxSlots; i++)
        cloned.Add(GetSlot(i).Clone());

    return new GarageRoster(cloned);
}

// 사용되지 않는 클래스
public static class GarageLegacyPartIdMap
{
    // 전체 파일이 마이그레이션용으로 사용 후 제거 필요
}
```

---

### 7. Violation of SRP

**위치**: `GarageDraftEvaluation.cs`

```csharp
// 문제: Evaluation, Factory, Formatter 역할이 한 타입에 섞일 수 있음 (지속 관찰)
public sealed class GarageDraftEvaluation
{
    public bool CanSave => HasSelectedDraftChanges && RosterValidationResult.IsSuccess;
    public static GarageDraftEvaluation Create(/* ... */);
    // 평가 절차: 별도 Evaluator 클래스 대신 Evaluate(...) 단일 진입점으로 유지
    public static GarageDraftEvaluation Evaluate(/* state, catalog, use cases */);
}
```

---

### 8. Leaky Abstraction in Domain

**위치**: `SkillSpec.cs`

```csharp
// 문제: 내부 구현 세부사항 노출
public sealed class SkillSpec : Shared.Kernel.ValueObject
{
    private const int Precision = 4;  // 내부 세부사항이 공개됨

    public SkillSpec(float damage, float manaCost, /* ... */)
    {
        Damage = (float)System.Math.Round(damage, float.NaN);  // Precision 사용
        // ...
    }
}
```

---

## Summary

| Anti-Pattern | Severity | Files Affected |
|--------------|----------|-----------------|
| Anemic Domain Model | HIGH | `Unit.cs`, `StatusEffect.cs` |
| Primitive Obsession | MEDIUM | 전반적 |
| Data Clumps / God Object | HIGH | `GarageSlotViewModel.cs`, `PartAlignment.cs` |
| Feature Envy | MEDIUM | `GaragePageViewModelBuilders.cs`, `StatusContainer.cs` |
| Mutable Value Objects | HIGH | `StatusEffect.cs` |
| Lack of Invariants | HIGH | `Unit.cs`, `GarageRoster.cs` |
| Large Constructor | MEDIUM | `GarageSlotViewModel.cs` |
| Magic Numbers/Strings | LOW | 여러 파일 |
| Leaky Abstraction | LOW | `SkillSpec.cs`, `GarageDraftEvaluation.cs` |
| Shotgun Surgery | MEDIUM | `GaragePageState.cs` |
