# Unit Feature 분리 설계

이 문서는 `Garage` Feature에 있는 `Unit` 도메인을 독립적인 `Unit` Feature로 분리하는 설계안이다.
피처 경계의 최종 판단은 [architecture.md](../../agent/architecture.md)를 따른다.
게임 디자인 SSOT는 [`game_design.md`](./game_design.md)이며, 본 문서는 거기서 정의된 소환 흐름을 아키텍처로 풀어낸다.

---

## 배경

### 게임 디자인 결정

`game_design.md`에서 다음이 정의되었다:

- **소환 생명주기**: 유닛은 재소환 가능 (Clash Royale 식)
- **GarageRoster**: ID 조합만 저장 (`frameId`, `firepowerModuleId`, `mobilityModuleId`)
- **Unit 계산**: 게임 시작 시 한 번 → 전투 중 재사용 템플릿
- **앵커 반경**: 기동 모듈이 결정

```
GarageRoster (ID 조합)
    → 게임 시작 시 한 번 계산
        → Unit[] specs (전투 템플릿, 불변)
            → 소환 시마다 참조
                → new BattleEntity(spec) (가변 인스턴스)
```

### 문제의식

Unit 스펙은 **Garage가 조합하고, 전투 Feature들이 소비**하는 공유 템플릿이다.
현재 `Garage/Domain/Unit.cs`는 Garage 안에 갇혀 있어서 Player/Enemy/Combat가 소비하려면 `Features.Garage.Domain`을 참조해야 한다. 이는 도메인 경계를 모호하게 만든다.

### 목표

```
Before:  Garage → Unit (Garage 안에 Unit이 종속)
After:   Garage → Unit (Garage가 Unit을 참조, Unit은 독립)
         Player → Unit (전투 도메인도 Unit을 소비)
         Enemy  → Unit (적 유닛도 Unit 스펙 기반)
```

**의존성 방향**:
```
Garage ──→ Unit ←── Player
                  ←── Enemy
                  ←── Combat
```

Unit은 Garage를 **모른다**. Garage는 Unit을 **안다**. 단방향 의존성.

---

## 아키텍처 개요

### Feature 분리 전후

#### Before (현재)

```
Features/Garage/
├── Domain/
│   ├── Unit.cs                  ← 유닛 스펙 (이동 대상)
│   ├── UnitComposition.cs       ← 조합 검증 (이동 대상)
│   ├── CostCalculator.cs        ← 비용 계산 (이동 대상)
│   ├── PassiveEffect.cs         ← 패시브 효과 (이동 대상)
│   └── GarageRoster.cs          ← 편성 목록 (Garage 잔류)
├── Application/
│   ├── ComposeUnitUseCase.cs    ← 유닛 조합 (Unit 이동 시 수정)
│   └── Ports/
└── Infrastructure/
    ├── ModuleCatalog.cs         ← SO 카탈로그 (이동 대상)
    ├── UnitFrameData.cs         ← 프레임 SO (이동 대상)
    ├── FirepowerModuleData.cs   ← 화력 SO (이동 대상)
    ├── MobilityModuleData.cs    ← 기동 SO (이동 대상)
    └── PassiveTraitData.cs      ← 패시브 SO (이동 대상)
```

#### After (목표)

```
Features/Unit/                          ← 독립된 Unit Feature
├── Domain/
│   ├── Unit.cs                         ← 전투 유닛 스펙 엔티티
│   ├── UnitComposition.cs              ← 조합 검증 + 스탯 계산
│   ├── CostCalculator.cs               ← 소환 비용 공식
│   ├── PassiveEffect.cs                ← 패시브 효과 도메인
│   └── Module.cs                       ← 모듈 베이스 (신규)
├── Application/
│   ├── ComposeUnitUseCase.cs           ← 유닛 조합 계산
│   └── Ports/
│       └── IUnitCompositionPort.cs     ← 조합 요청 포트 (Garage가 정의)
├── Infrastructure/
│   ├── UnitCompositionProvider.cs      ← IUnitCompositionPort 구현 (Garage가 정의한 포트)
│   ├── ModuleCatalog.cs                ← SO 카탈로그
│   ├── UnitFrameData.cs                ← 프레임 SO
│   ├── FirepowerModuleData.cs          ← 화력 SO
│   ├── MobilityModuleData.cs           ← 기동 SO
│   └── PassiveTraitData.cs             ← 패시브 SO
├── UnitBootstrap.cs                    ← Composition root
├── UnitSetup.cs                        ← Scene-level wiring
└── README.md

Features/Garage/                        ← 편성 관리 전문
├── Domain/
│   └── GarageRoster.cs                 ← UnitLoadout[] 관리
├── Application/
│   ├── Ports/
│   │   ├── IGarageNetworkPort.cs
│   │   ├── IGaragePersistencePort.cs
│   │   └── IUnitCompositionPort.cs     ← Garage가 정의 (소비자)
│   ├── InitializeGarageUseCase.cs
│   ├── ValidateRosterUseCase.cs
│   ├── SaveRosterUseCase.cs
│   └── GarageEvents.cs
├── Infrastructure/
│   ├── GarageNetworkAdapter.cs
│   └── GarageJsonPersistence.cs
└── README.md
```

### 책임 분리

| Feature | 책임 | 모르는 것 |
|---|---|---|
| **Unit** | 유닛 스펙 정의, 조합 검증, 비용 계산 | 누가 조합하는지, 어디에 저장되는지 |
| **Garage** | 편성 목록 관리, 네트워크 동기화, 로컬 저장 | Unit의 내부 스탯 계산 방식 |

### 포트 소유권 규칙

`../../agent/architecture.md (line 130-131)` 규칙을 따른다:

> "Port interface는 소비자(A)의 Application/Ports에 정의. Implementation은 제공자(B)의 Infrastructure에 위치."

```
Garage/Application/Ports/IUnitCompositionPort.cs   ← Garage가 정의 (소비자)
Unit/Infrastructure/UnitCompositionProvider.cs      ← Unit이 구현 (제공자)
```

Garage는 "나한테 Unit 조합 기능이 필요해"라고 요청하고, Unit은 그 요청에 맞춰 구현을 제공한다.

자세한 것은 `game_design.md`의 "유닛 소환 생명주기" 섹션을 참조. 요약:

| 단계 | 데이터 | 위치 |
|---|---|---|
| Garage 편성 | `UnitLoadout` (ID 조합) | `garageRoster` CustomProperties |
| 게임 시작 | `Unit[] specs` (계산된 스펙) | GameScene 초기화 시 한 번 계산 |
| 소환 | `BattleEntity` (상태 인스턴스) | 전투 중 생성/파괴 반복 |

**GarageRoster 저장 데이터**: `frameId`, `firepowerModuleId`, `mobilityModuleId` 조합만 저장. 계산된 Unit 스펙은 저장하지 않는다.

---

## 도메인 모델 상세

### 1. Unit (전투 유닛 스펙)

```
위치: Features/Unit/Domain/Unit.cs
책임: 프레임 + 모듈 조합 결과로 생성된 전투 유닛의 스펙 정의
특징: 순수 C#, 불변 (Immutable)
```

```csharp
namespace Features.Unit.Domain
{
    /// <summary>
    /// 전투에서 실제 동작하는 유닛 스펙 엔티티.
    /// 프레임 + 모듈 조합 결과로 생성된다.
    /// 순수 C# — Unity/Photon 의존성 없음.
    /// </summary>
    public sealed class Unit
    {
        public DomainEntityId Id { get; }
        public string FrameId { get; }
        public string FirepowerModuleId { get; }
        public string MobilityModuleId { get; }

        // 조합 결과 스탯 (계산된 값)
        public float FinalHp { get; }
        public float FinalAttackDamage { get; }
        public float FinalAttackSpeed { get; }
        public float FinalRange { get; }
        public float FinalMoveRange { get; }
        public float FinalAnchorRange { get; }  // 앵커 반경 (기동 모듈이 결정)
        public int SummonCost { get; }
        public string PassiveTraitId { get; }
        public int PassiveTraitCostBonus { get; }

        public Unit(
            DomainEntityId id,
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            string passiveTraitId,
            int passiveTraitCostBonus,
            float finalHp,
            float finalAttackDamage,
            float finalAttackSpeed,
            float finalRange,
            float finalMoveRange,
            float finalAnchorRange,
            int summonCost)
        {
            Id = id;
            FrameId = frameId;
            FirepowerModuleId = firepowerModuleId;
            MobilityModuleId = mobilityModuleId;
            PassiveTraitId = passiveTraitId;
            PassiveTraitCostBonus = passiveTraitCostBonus;
            FinalHp = finalHp;
            FinalAttackDamage = finalAttackDamage;
            FinalAttackSpeed = finalAttackSpeed;
            FinalRange = finalRange;
            FinalMoveRange = finalMoveRange;
            FinalAnchorRange = finalAnchorRange;
            SummonCost = summonCost;
        }
    }
}
```

**설계 판단**:
- `Unit`은 **ID만 참조**한다. SO 데이터를 직접持有하지 않음
- 이는 Unit이 Infrastructure 레이어에 의존하지 않도록 하기 위함
- SO 데이터는 `UnitComposition`이 조합 시 사용, 결과는 `Unit` 스펙으로 직렬화

### 2. Module (모듈 도메인 — 신규)

```
위치: Features/Unit/Domain/Module.cs
책임: 모듈의 공통 구조 정의, 타입 안전 제공
```

Module은 **SO 데이터의 도메인 표현**이다. 실제 데이터는 Infrastructure의 `FirepowerModuleData` 등이 가지고, Domain은 타입 정의만 제공한다.

```csharp
namespace Features.Unit.Domain
{
    /// <summary>
    /// 모듈 타입 구분. SO 데이터 조회용 키.
    /// </summary>
    public enum ModuleType
    {
        Firepower,   // 상단: 화력
        Mobility,    // 하단: 기동
        Passive      // 중단: 고유 특성
    }

    /// <summary>
    /// 모듈 식별자. 타입 + ID 조합.
    /// </summary>
    public readonly struct ModuleId
    {
        public ModuleType Type { get; }
        public string Id { get; }

        public ModuleId(ModuleType type, string id)
        {
            Type = type;
            Id = id;
        }

        public override string ToString() => $"{Type}:{Id}";
    }

    /// <summary>
    /// 모듈 스탯 정의. 모든 모듈이 공통으로 가지는 수치.
    /// </summary>
    public readonly struct ModuleStats
    {
        public float HpBonus { get; }
        public float AttackDamage { get; }
        public float AttackSpeed { get; }
        public float Range { get; }
        public float MoveRange { get; }
        public float AnchorRange { get; }  // 앵커 반경
        public int CostBonus { get; }

        public ModuleStats(
            float hpBonus = 0f,
            float attackDamage = 0f,
            float attackSpeed = 0f,
            float range = 0f,
            float moveRange = 0f,
            float anchorRange = 0f,
            int costBonus = 0)
        {
            HpBonus = hpBonus;
            AttackDamage = attackDamage;
            AttackSpeed = attackSpeed;
            Range = range;
            MoveRange = moveRange;
            AnchorRange = anchorRange;
            CostBonus = costBonus;
        }
    }
}
```

**설계 판단**:
- Module을 별도의 클래스로 만들지 않고 `ModuleId` + `ModuleStats` 값 객체로 정의
- 실제 모듈 데이터(이름, 아이콘, 설명)는 SO가 관리
- Domain은 **타입 안전과 공통 구조**만 제공

### 3. UnitComposition (조합 검증 + 스탯 계산)

```
위치: Features/Unit/Domain/UnitComposition.cs
책임: 프레임 + 모듈 조합의 유효성 검증, 최종 스탯 계산
특징: static class, 순수 C#
```

```csharp
namespace Features.Unit.Domain
{
    /// <summary>
    /// 프레임 + 모듈 조합의 유효성 검증 + 결과 계산.
    /// 조합 검증 규칙(unit_module_design.md 참조)을 적용한다.
    /// </summary>
    public static class UnitComposition
    {
        /// <summary>
        /// 조합이 유효한지 검사.
        /// 규칙: 이동범위 ≥ 4m OR 사거리 ≥ 6m OR (이동범위 ≥ 3m AND 사거리 ≥ 4m)
        /// </summary>
        public static bool Validate(
            ModuleStats frameBase,
            ModuleStats firepower,
            ModuleStats mobility,
            out string errorMessage)
        {
            errorMessage = null;

            float moveRange = mobility.MoveRange;
            float range = firepower.Range;

            bool isMeleeCapable = moveRange >= 4f;
            bool isRangedCapable = range >= 6f;
            bool isHybrid = moveRange >= 3f && range >= 4f;

            if (!isMeleeCapable && !isRangedCapable && !isHybrid)
            {
                errorMessage = $"금지조합: 이동범위({moveRange}m)와 사거리({range}m)이 모두 좁습니다.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 조합 결과 스탯 계산.
        /// </summary>
        public static ComposedStats Compose(
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            string passiveTraitId,
            int passiveTraitCostBonus,
            ModuleStats frameBase,
            ModuleStats firepower,
            ModuleStats mobility)
        {
            float hp = frameBase.HpBonus + mobility.HpBonus;
            float damage = firepower.AttackDamage;
            float attackSpeed = firepower.AttackSpeed;
            float range = firepower.Range;
            float moveRange = mobility.MoveRange;
            float anchorRange = mobility.AnchorRange;

            int summonCost = CostCalculator.Calculate(
                hp, damage, attackSpeed, range, moveRange, passiveTraitCostBonus);

            return new ComposedStats
            {
                FrameId = frameId,
                FirepowerModuleId = firepowerModuleId,
                MobilityModuleId = mobilityModuleId,
                PassiveTraitId = passiveTraitId,
                PassiveTraitCostBonus = passiveTraitCostBonus,
                FinalHp = hp,
                FinalAttackDamage = damage,
                FinalAttackSpeed = attackSpeed,
                FinalRange = range,
                FinalMoveRange = moveRange,
                FinalAnchorRange = anchorRange,
                SummonCost = summonCost
            };
        }
    }

    public readonly struct ComposedStats
    {
        public string FrameId { get; init; }
        public string FirepowerModuleId { get; init; }
        public string MobilityModuleId { get; init; }
        public string PassiveTraitId { get; init; }
        public int PassiveTraitCostBonus { get; init; }
        public float FinalHp { get; init; }
        public float FinalAttackDamage { get; init; }
        public float FinalAttackSpeed { get; init; }
        public float FinalRange { get; init; }
        public float FinalMoveRange { get; init; }
        public float FinalAnchorRange { get; init; }
        public int SummonCost { get; init; }

        public Unit ToUnit(DomainEntityId id) => new(
            id, FrameId, FirepowerModuleId, MobilityModuleId,
            PassiveTraitId, PassiveTraitCostBonus,
            FinalHp, FinalAttackDamage, FinalAttackSpeed,
            FinalRange, FinalMoveRange, FinalAnchorRange, SummonCost);
    }
}
```

### 4. CostCalculator (비용 계산)

```
위치: Features/Unit/Domain/CostCalculator.cs
책임: 스탯 기반 소환 비용 자동 계산
특징: static class, 공식만 관리
```

기존 `Garage/Domain/CostCalculator.cs`와 동일. 네임스페이스만 변경.

### 5. PassiveEffect (패시브 효과)

```
위치: Features/Unit/Domain/PassiveEffect.cs
책임: 고유 특성의 효과 타입 정의
특징: enum 또는 값 객체
```

기존 `Garage/Domain/PassiveEffect.cs`와 동일. 네임스페이스만 변경.

---

## Application 레이어

### ComposeUnitUseCase

```
위치: Features/Unit/Application/ComposeUnitUseCase.cs
책임: Garage로부터 모듈 ID들을 받아 Unit 스펙 생성
```

```csharp
namespace Features.Unit.Application
{
    /// <summary>
    /// 모듈 ID들로 유닛을 조합한다.
    /// Unit 도메인의 UnitComposition을 사용하며, SO 데이터 조회는 Port를 통해 수행한다.
    /// </summary>
    public sealed class ComposeUnitUseCase
    {
        private readonly IUnitCompositionPort _port;

        public ComposeUnitUseCase(IUnitCompositionPort port)
        {
            _port = port;
        }

        public Result<Unit> Execute(
            DomainEntityId unitId,
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId)
        {
            var frameBase = _port.GetFrameBaseStats(frameId);
            var firepower = _port.GetFirepowerStats(firepowerModuleId);
            var mobility = _port.GetMobilityStats(mobilityModuleId);

            // 검증
            if (!UnitComposition.Validate(frameBase, firepower, mobility, out var error))
            {
                return Result.Fail<Unit>(error);
            }

            var passiveTraitId = _port.GetPassiveTraitId(frameId);
            var passiveCostBonus = _port.GetPassiveTraitCostBonus(frameId);

            // 조합
            var composed = UnitComposition.Compose(
                frameId, firepowerModuleId, mobilityModuleId,
                passiveTraitId, passiveCostBonus,
                frameBase, firepower, mobility);

            return Result.Ok(composed.ToUnit(unitId));
        }
    }
}
```

### IUnitCompositionPort (Garage가 정의)

```
위치: Features/Garage/Application/Ports/IUnitCompositionPort.cs
책임: Garage가 Unit 조합에 필요한 데이터 조회를 요청하는 포트
```

```csharp
namespace Features.Garage.Application.Ports
{
    /// <summary>
    /// Unit 조합에 필요한 모듈 스탯 조회 포트.
    /// Garage가 정의하고, Unit Infrastructure가 구현한다.
    /// ../../agent/architecture.md (line 130-131) 규칙 준수: 소비자가 포트 정의, 제공자가 구현.
    /// </summary>
    public interface IUnitCompositionPort
    {
        ModuleStats GetFrameBaseStats(string frameId);
        ModuleStats GetFirepowerStats(string moduleId);
        ModuleStats GetMobilityStats(string moduleId);
        string GetPassiveTraitId(string frameId);
        int GetPassiveTraitCostBonus(string frameId);
    }
}
```

**설계 판단**:
- `IUnitCompositionPort`는 **Garage가 정의** (소비자)
- `UnitCompositionProvider`는 **Unit Infrastructure가 구현** (제공자)
- 이는 `../../agent/architecture.md`의 크로스 피처 포트 규칙을 정확히 따름

---

## Infrastructure 레이어

### UnitCompositionProvider (Unit이 구현)

```
위치: Features/Unit/Infrastructure/UnitCompositionProvider.cs
책임: IUnitCompositionPort 구현, ModuleCatalog를 통해 SO 데이터 조회
```

```csharp
namespace Features.Unit.Infrastructure
{
    public sealed class UnitCompositionProvider : IUnitCompositionPort
    {
        private readonly ModuleCatalog _catalog;

        public UnitCompositionProvider(ModuleCatalog catalog)
        {
            _catalog = catalog;
        }

        public ModuleStats GetFrameBaseStats(string frameId)
        {
            var frame = _catalog.GetUnitFrame(frameId);
            return new ModuleStats(
                hpBonus: frame.BaseHp,
                moveRange: frame.BaseMoveRange,
                attackSpeed: frame.BaseAttackSpeed);
        }

        public ModuleStats GetFirepowerStats(string moduleId)
        {
            var m = _catalog.GetFirepowerModule(moduleId);
            return new ModuleStats(
                attackDamage: m.AttackDamage,
                attackSpeed: m.AttackSpeed,
                range: m.Range);
        }

        public ModuleStats GetMobilityStats(string moduleId)
        {
            var m = _catalog.GetMobilityModule(moduleId);
            return new ModuleStats(
                hpBonus: m.HpBonus,
                moveRange: m.MoveRange,
                anchorRange: m.AnchorRange);
        }

        public string GetPassiveTraitId(string frameId) =>
            _catalog.GetUnitFrame(frameId).PassiveTraitId;

        public int GetPassiveTraitCostBonus(string frameId) =>
            _catalog.GetUnitFrame(frameId).PassiveTraitCostBonus;
    }
}
```

### ModuleCatalog

```
위치: Features/Unit/Infrastructure/ModuleCatalog.cs
책임: 모든 SO 모듈 데이터 조회
```

기존 `Garage/Infrastructure/ModuleCatalog.cs`와 동일. 네임스페이스만 변경.
`MobilityModuleData`에 `AnchorRange` 필드 추가 (`module_data_structure.md` 참조).

### SO 데이터들

```
위치: Features/Unit/Infrastructure/
- UnitFrameData.cs
- FirepowerModuleData.cs
- MobilityModuleData.cs      ← anchorRange 필드 추가
- PassiveTraitData.cs
```

기존 파일들과 동일. 네임스페이스만 `Features.Unit.Infrastructure`로 변경.

---

## Garage Feature 변경 사항

### Garage가 Unit을 사용하는 방식

```
Garage는 Unit을 "소비"하지 않는다.
Garage는 Unit을 "조립"한다.
```

| Garage의 역할 | Unit과의 관계 |
|---|---|
| 편성 목록 관리 (`GarageRoster`) | `UnitLoadout` (frameId, moduleId들) 저장 |
| 조합 요청 (`ComposeUnitUseCase`) | Unit Feature의 UseCase 호출 |
| 유효성 검증 | `UnitComposition.Validate` 호출 |

### GarageRoster (잔류)

```csharp
namespace Features.Garage.Domain
{
    /// <summary>
    /// 플레이어의 차고 편성. 3~5기의 유닛 로드아웃을 관리.
    /// Unit 스펙 자체는 저장하지 않는다. ID 조합만 저장.
    /// 네트워크 동기화는 ID 조합만 전달하며,
    /// 실제 Unit 스펙은 게임 시작 시 ComposeUnitUseCase로 복원한다.
    /// </summary>
    [Serializable]
    public sealed class GarageRoster
    {
        [Serializable]
        public struct UnitLoadout
        {
            public string frameId;
            public string firepowerModuleId;
            public string mobilityModuleId;
        }

        public List<UnitLoadout> loadout = new();

        public bool IsValid => loadout.Count >= 3 && loadout.Count <= 5;
    }
}
```

**설계 판단**:
- `GarageRoster`는 `Unit`을 직접持有하지 않음
- `UnitLoadout`은 **ID 조합**만 저장 (직렬화 용이, 네트워크 payload 최소화)
- 실제 `Unit` 인스턴스는 게임 시작 시 `ComposeUnitUseCase`를 통해 생성

### Garage에서 제거되는 것

| 항목 | 이동처 |
|---|---|
| `Unit.cs` | `Unit/Domain/` |
| `UnitComposition.cs` | `Unit/Domain/` |
| `CostCalculator.cs` | `Unit/Domain/` |
| `PassiveEffect.cs` | `Unit/Domain/` |
| `ComposeUnitUseCase.cs` | `Unit/Application/` |
| `ModuleCatalog.cs` | `Unit/Infrastructure/` |
| SO 데이터들 | `Unit/Infrastructure/` |

### Garage에 남는 것

| 항목 | 역할 |
|---|---|
| `GarageRoster.cs` | 편성 목록 |
| `GarageEvents.cs` | 편성 관련 이벤트 |
| `InitializeGarageUseCase.cs` | 편성 초기화/복구 |
| `ValidateRosterUseCase.cs` | 편성 유효성 |
| `SaveRosterUseCase.cs` | 편성 저장 |
| `IGarageNetworkPort.cs` | 네트워크 동기화 포트 |
| `IGaragePersistencePort.cs` | 저장 포트 |
| `IUnitCompositionPort.cs` | Unit 조합 요청 포트 (Garage가 정의) |
| `GarageNetworkAdapter.cs` | Photon 어댑터 |
| `GarageJsonPersistence.cs` | JSON 저장 |

---

## Composition Root

Unit Feature가 독립 Feature이므로 `UnitBootstrap.cs`와 `UnitSetup.cs`가 필요하다.

### UnitBootstrap

```
위치: Features/Unit/UnitBootstrap.cs
책임: Scene-level wiring, ModuleCatalog 연결
```

- `[Required, SerializeField]`로 `ModuleCatalog` 연결
- `Initialize(eventBus)` 호출받아 Infrastructure 어댑터 + UnitSetup 조립
- `OnDestroy()`에서 `Cleanup()` 호출

### UnitSetup

```
위치: Features/Unit/UnitSetup.cs
책임: Composition root (의존성 주입)
```

- `UnitCompositionProvider` 생성 (ModuleCatalog 기반)
- `ComposeUnitUseCase` 생성 + 포트 주입
- `DisposableScope`로 EventBus 구독 수명 관리

### 초기화 순서

```
1. LobbyBootstrap가 EventBus 생성
2. GarageBootstrap.Initialize(eventBus) 호출
3. UnitBootstrap.Initialize(eventBus) 호출
   - UnitCompositionProvider 생성
   - ComposeUnitUseCase 생성
4. InitializeGarageUseCase.Execute() → 이전 편성 복원
   - ComposeUnitUseCase로 UnitLoadout → Unit 스펙 복원
```

---

## 의존성 규칙

### Unit Feature (독립)

```
Unit → Shared.Kernel (DomainEntityId, Result)
Unit → 없음 (Garage, Player, Enemy, Combat 모두 모름)
```

### Garage Feature (Unit 의존)

```
Garage → Unit (Unit.Domain, Unit.Application, Unit.Infrastructure)
Garage → Shared.Kernel
Garage → Shared.EventBus
Garage → Shared.Networking
```

### 다른 Feature들의 Unit 소비 (미래)

```
Player → Unit.Domain (Unit 스펙 읽기 전용)
Enemy  → Unit.Domain (Unit 스펙 읽기 전용)
Combat → Unit.Domain (Unit 스펙 읽기 전용)
```

---

## 마이그레이션 순서

### Step 1: Unit Feature 스캐폴딩
1. `Features/Unit/` 폴더 구조 생성 (Domain/, Application/, Infrastructure/)
2. `UnitBootstrap.cs`, `UnitSetup.cs` 스켈레톤 생성
3. `../../Assets/Scripts/Features/Unit/README.md` 작성

### Step 2: Domain 이동
4. `Unit.cs` 이동 (네임스페이스 변경, `FinalAnchorRange` 추가)
5. `UnitComposition.cs` 이동 (`AnchorRange` 계산 추가)
6. `CostCalculator.cs` 이동
7. `PassiveEffect.cs` 이동

### Step 3: Module 도메인 추가
8. `Module.cs` 생성 (`ModuleType`, `ModuleId`, `ModuleStats` + `AnchorRange`)

### Step 4: Application 이동
9. `ComposeUnitUseCase.cs` 이동 (`IUnitCompositionPort` 사용으로 변경)

### Step 5: Infrastructure 이동
10. `ModuleCatalog.cs` 이동
11. SO 데이터들 이동 (`MobilityModuleData`에 `anchorRange` 필드 추가)
12. `UnitCompositionProvider.cs` 생성 (`IUnitCompositionPort` 구현)

### Step 6: Garage 리팩토링
13. `IUnitCompositionPort.cs` 생성 (Garage/Application/Ports에)
14. 모든 `using Features.Garage.Domain` → `using Features.Unit.Domain` 변경
15. `GarageCompositionProviders.cs` 제거 (UnitCompositionProvider로 대체)
16. Garage README 업데이트

### Step 7: 테스트
17. `Tests/Garage/` → `Tests/Unit/` 테스트 이동
18. 네임스페이스 업데이트
19. 빌드 + 테스트 실행

---

## 참고

- 게임 디자인 SSOT: [`game_design.md`](./game_design.md) — "유닛 소환 생명주기" 섹션
- 기존 유닛/모듈 설계: [`unit_module_design.md`](./unit_module_design.md)
- 데이터 구조 정의: [`module_data_structure.md`](./module_data_structure.md)
- 전역 아키텍처: [architecture.md](../../agent/architecture.md)
- 안티패턴: [anti_patterns.md](../../agent/anti_patterns.md)
