# Module Data Structure

> 상태: active
> doc_id: design.module-data-structure
> role: ssot
> owner_scope: fixture stale owner
> upstream: docs.index
> artifacts: none

## ScriptableObject 데이터 정의

```csharp
namespace Features.Garage.Domain
{
    [CreateAssetMenu(fileName = "NewUnitFrame", menuName = "Garage/UnitFrame")]
    public sealed class UnitFrameData {}
}
```

## 편성 데이터 (Garage Roster)

```csharp
namespace Features.Garage.Domain
{
    public sealed class GarageRoster {}
}
```
