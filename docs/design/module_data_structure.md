# Module Data Structure

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: design.module-data-structure
> role: ssot
> owner_scope: 유닛과 모듈 데이터의 실제 Unity C# 구조 기준
> upstream: design.unit-module-design, design.game-design
> artifacts: `Assets/Scripts/Features/Unit/`, `Assets/Scripts/Features/Garage/`

이 문서는 유닛/모듈 데이터의 Unity C# 구조를 정의한다.
`unit_module_design.md`의 하위 문서이며, 실제 구현 기준의 SSOT다.

---

## 아키텍처 개요

### 런타임 구조

```
┌─────────────────────────────────────────────────────┐
│  Garage UI Surface                                  │
│  - Stitch/UI Toolkit 후보 surface가 새 UX 기준       │
├─────────────────────────────────────────────────────┤
│  UnitComposition (Application)                      │
│  - 프레임 + 모듈 조합 결과 계산                      │
│  - CostCalculator (비용 공식)                        │
├─────────────────────────────────────────────────────┤
│  UnitFrame / Module (Domain)                        │
│  - 유닛 엔티티, 모듈 엔티티                          │
│  - 도메인 로직 (조합 규칙 검증)                      │
├─────────────────────────────────────────────────────┤
│  ScriptableObject Data (Infrastructure)             │
│  - UnitFrameData, FirepowerModuleData,               │
│    MobilityModuleData, PassiveTraitData              │
└─────────────────────────────────────────────────────┘
```

### 데이터 흐름

```
1. 차고에서 프레임 선택 → UnitFrameData 로드
2. 상/하단 모듈 선택 → FirepowerModuleData + MobilityModuleData 로드
3. UnitComposition이 조합 계산 → 최종 스탯 + 비용 산출
4. 전투에서 소환 시 → 계산된 스탯으로 Unit 인스턴스 생성
```

---

## ScriptableObject 데이터 정의

### 1. UnitFrameData (유닛 프레임)

```csharp
namespace Features.Garage.Domain
{
    [CreateAssetMenu(fileName = "NewUnitFrame", menuName = "Garage/UnitFrame")]
    public sealed class UnitFrameData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string frameId;      // "guardian_01"
        [SerializeField] private string displayName;  // "가디언"
        [SerializeField] private Sprite icon;

        [Header("Base Stats (프레임 기본 스탯)")]
        [SerializeField] private float baseHp;         // 300
        [SerializeField] private float baseMoveRange;  // 4.0m
        [SerializeField] private float baseAttackSpeed; // 1.0 (초당)

        [Header("Passive Trait")]
        [SerializeField] private PassiveTraitData passiveTrait; // 고유 특성 (고정)

        [Header("Visual")]
        [SerializeField] private GameObject unitPrefab; // 전투에서 생성될 프리팹

        public string FrameId => frameId;
        public string DisplayName => displayName;
        public float BaseHp => baseHp;
        public float BaseMoveRange => baseMoveRange;
        public float BaseAttackSpeed => baseAttackSpeed;
        public PassiveTraitData PassiveTrait => passiveTrait;
        public GameObject UnitPrefab => unitPrefab;
    }
}
```

### 2. FirepowerModuleData (상단 모듈)

```csharp
namespace Features.Garage.Domain
{
    [CreateAssetMenu(fileName = "NewFirepowerModule", menuName = "Garage/FirepowerModule")]
    public sealed class FirepowerModuleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string moduleId;     // "fire_single_01"
        [SerializeField] private string displayName;  // "단일탄"
        [SerializeField] private Sprite icon;

        [Header("Combat Stats")]
        [SerializeField] private float attackDamage;   // 공격력 보정 (절대값)
        [SerializeField] private float attackSpeed;    // 공격속도 (초당 횟수)
        [SerializeField] private float range;          // 사거리 (m)

        [Header("Description")]
        [SerializeField, TextArea] private string description;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public float AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float Range => range;
    }
}
```

### 3. MobilityModuleData (하단 모듈)

```csharp
namespace Features.Garage.Domain
{
    [CreateAssetMenu(fileName = "NewMobilityModule", menuName = "Garage/MobilityModule")]
    public sealed class MobilityModuleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string moduleId;     // "mob_armor_01"
        [SerializeField] private string displayName;  // "중장갑"
        [SerializeField] private Sprite icon;

        [Header("Defense Stats")]
        [SerializeField] private float hpBonus;        // HP 보정값 (+500)
        [SerializeField] private float moveRange;      // 이동범위 (m) (절대값)

        [Header("Anchor")]
        [SerializeField] private float anchorRange;    // 앵커 반경 (m) (전투 중 교전 가능 범위)

        [Header("Description")]
        [SerializeField, TextArea] private string description;

        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public float HpBonus => hpBonus;
        public float MoveRange => moveRange;
        public float AnchorRange => anchorRange;
    }
}
```

### 4. PassiveTraitData (중단 - 고유 특성)

```csharp
namespace Features.Garage.Domain
{
    [CreateAssetMenu(fileName = "NewPassiveTrait", menuName = "Garage/PassiveTrait")]
    public sealed class PassiveTraitData : ScriptableObject
    {
        public enum TraitStrength
        {
            Weak = 2,    // 보정값 +2
            Medium = 5,  // 보정값 +5
            Strong = 10  // 보정값 +10
        }

        [Header("Identity")]
        [SerializeField] private string traitId;
        [SerializeField] private string displayName;

        [Header("Cost")]
        [SerializeField] private TraitStrength strength;

        [Header("Description")]
        [SerializeField, TextArea] private string description;

        public string TraitId => traitId;
        public string DisplayName => displayName;
        public int CostBonus => (int)strength;
        public string Description => description;
    }
}
```

---

## 도메인 모델

### 1. Unit (전투 중 유닛 엔티티)

```csharp
namespace Features.Garage.Domain
{
    /// <summary>
    /// 전투에서 실제 동작하는 유닛 엔티티.
    /// 프레임 + 모듈 조합 결과로 생성됨.
    /// </summary>
    public sealed class Unit
    {
        public DomainEntityId Id { get; }
        public UnitFrameData FrameData { get; }
        public FirepowerModuleData FirepowerModule { get; }
        public MobilityModuleData MobilityModule { get; }

        // 조합 결과 스탯 (계산된 값)
        public float FinalHp { get; }
        public float FinalAttackDamage { get; }
        public float FinalAttackSpeed { get; }
        public float FinalRange { get; }
        public float FinalMoveRange { get; }
        public int SummonCost { get; }

        public Unit(
            DomainEntityId id,
            UnitFrameData frame,
            FirepowerModuleData firepower,
            MobilityModuleData mobility)
        {
            Id = id;
            FrameData = frame;
            FirepowerModule = firepower;
            MobilityModule = mobility;

            // 조합 결과 계산
            FinalHp = frame.BaseHp + mobility.HpBonus;
            FinalAttackDamage = firepower.AttackDamage;
            FinalAttackSpeed = firepower.AttackSpeed * frame.BaseAttackSpeed;
            FinalRange = firepower.Range;
            FinalMoveRange = mobility.MoveRange;
            SummonCost = CostCalculator.Calculate(this);
        }
    }
}
```

### 2. UnitComposition (조합 계산기)

```csharp
namespace Features.Garage.Domain
{
    /// <summary>
    /// 프레임 + 모듈 조합의 유효성 검증 + 결과 계산.
    /// </summary>
    public static class UnitComposition
    {
        /// <summary>
        /// 조합이 유효한지 검사.
        /// </summary>
        public static bool Validate(
            UnitFrameData frame,
            FirepowerModuleData firepower,
            MobilityModuleData mobility,
            out string errorMessage)
        {
            errorMessage = null;

            // 규칙 1: 이동범위 ≥ 사거리 - α (근접 유닛 조건)
            // α = 1.5m (여유치)
            float moveRange = mobility.MoveRange;
            float range = firepower.Range;

            if (moveRange < 4f && range < 6f)
            {
                // 하이브리드 조건 체크
                if (moveRange < 3f || range < 4f)
                {
                    errorMessage = $"이동범위({moveRange}m)와 사거리({range}m)이 모두 좁습니다. 근접 유닛은 이동범위 ≥ 4m 또는 사거리 ≥ 6m이 필요합니다.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 조합 결과 스탯 계산.
        /// </summary>
        public static ComposedStats Compose(
            UnitFrameData frame,
            FirepowerModuleData firepower,
            MobilityModuleData mobility)
        {
            return new ComposedStats
            {
                Hp = frame.BaseHp + mobility.HpBonus,
                AttackDamage = firepower.AttackDamage,
                AttackSpeed = firepower.AttackSpeed * frame.BaseAttackSpeed,
                Range = firepower.Range,
                MoveRange = mobility.MoveRange,
                PassiveTraitCost = frame.PassiveTrait.CostBonus
            };
        }
    }

    public struct ComposedStats
    {
        public float Hp;
        public float AttackDamage;
        public float AttackSpeed;
        public float Range;
        public float MoveRange;
        public int PassiveTraitCost;
    }
}
```

### 3. CostCalculator (비용 계산기)

```csharp
namespace Features.Garage.Domain
{
    /// <summary>
    /// 스탯 기반 소환 비용 자동 계산.
    /// 가중치 합산 + 분산 페널티 적용.
    /// </summary>
    public static class CostCalculator
    {
        // 가중치 상수
        private const float WeightHp = 0.02f;
        private const float WeightAttackDamage = 0.5f;
        private const float WeightAttackSpeed = 3.0f;
        private const float WeightRange = 2.0f;
        private const float WeightMoveRange = 1.5f;

        // 분산 페널티 계수
        private const float DispersionPenaltyFactor = 0.3f;

        // 비용 범위
        private const int MinCost = 15;
        private const int MaxCost = 80;

        public static int Calculate(Unit unit)
        {
            return Calculate(
                unit.FinalHp,
                unit.FinalAttackDamage,
                unit.FinalAttackSpeed,
                unit.FinalRange,
                unit.FinalMoveRange,
                unit.FrameData.PassiveTrait.CostBonus);
        }

        public static int Calculate(ComposedStats stats)
        {
            return Calculate(
                stats.Hp,
                stats.AttackDamage,
                stats.AttackSpeed,
                stats.Range,
                stats.MoveRange,
                stats.PassiveTraitCost);
        }

        public static int Calculate(
            float hp,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveRange,
            int passiveTraitCost)
        {
            // 1. 가중치 적용된 값 계산
            float weightedHp = hp * WeightHp;
            float weightedDamage = attackDamage * WeightAttackDamage;
            float weightedSpeed = attackSpeed * WeightAttackSpeed;
            float weightedRange = range * WeightRange;
            float weightedMoveRange = moveRange * WeightMoveRange;

            // 2. 기본 비용
            float baseCost = weightedHp + weightedDamage + weightedSpeed + weightedRange + weightedMoveRange;

            // 3. 분산 페널티 (표준편차 기반)
            float[] values = { weightedHp, weightedDamage, weightedSpeed, weightedRange, weightedMoveRange };
            float dispersionPenalty = CalculateStandardDeviation(values) * DispersionPenaltyFactor;

            // 4. 최종 비용
            int finalCost = Mathf.RoundToInt(baseCost + dispersionPenalty + passiveTraitCost);

            // 5. 범위 제한
            return Mathf.Clamp(finalCost, MinCost, MaxCost);
        }

        /// <summary>
        /// 배열의 표준편차 계산.
        /// </summary>
        private static float CalculateStandardDeviation(float[] values)
        {
            if (values.Length == 0) return 0f;

            // 평균
            float mean = 0f;
            foreach (var v in values) mean += v;
            mean /= values.Length;

            // 분산
            float variance = 0f;
            foreach (var v in values)
            {
                float diff = v - mean;
                variance += diff * diff;
            }
            variance /= values.Length;

            // 표준편차
            return Mathf.Sqrt(variance);
        }
    }
}
```

---

## 데이터 카탈로그 구조

### 파일 경로 규칙

```
Assets/Data/Garage/
├── Frames/
│   ├── SO_Frame_Guardian.asset
│   ├── SO_Frame_Hunter.asset
│   ├── SO_Frame_Artist.asset
│   └── SO_Frame_Medic.asset
├── Modules/
│   ├── Firepower/
│   │   ├── SO_Fire_Single.asset      (단일탄)
│   │   ├── SO_Fire_AoE.asset         (광유탄)
│   │   └── SO_Fire_Rapid.asset       (연사)
│   └── Mobility/
│       ├── SO_Mob_Armor.asset        (중장갑)
│       ├── SO_Mob_Light.asset        (경량)
│       └── SO_Mob_Fixed.asset        (고정포대)
└── Traits/
    ├── SO_Trait_IronWall.asset       (철벽)
    ├── SO_Trait_Pursuit.asset        (추격 본능)
    ├── SO_Trait_FocusFire.asset      (집중 포격)
    └── SO_Trait_EmergencyRepair.asset (긴급 수리)
```

### 모듈 카탈로그 ScriptableObject

```csharp
namespace Features.Garage.Infrastructure
{
    [CreateAssetMenu(fileName = "ModuleCatalog", menuName = "Garage/ModuleCatalog")]
    public sealed class ModuleCatalog : ScriptableObject
    {
        [Header("Firepower Modules")]
        [SerializeField] private List<FirepowerModuleData> firepowerModules;

        [Header("Mobility Modules")]
        [SerializeField] private List<MobilityModuleData> mobilityModules;

        [Header("Unit Frames")]
        [SerializeField] private List<UnitFrameData> unitFrames;

        public IReadOnlyList<FirepowerModuleData> FirepowerModules => firepowerModules;
        public IReadOnlyList<MobilityModuleData> MobilityModules => mobilityModules;
        public IReadOnlyList<UnitFrameData> UnitFrames => unitFrames;

        public FirepowerModuleData GetFirepowerModule(string id) =>
            firepowerModules.Find(m => m.ModuleId == id);

        public MobilityModuleData GetMobilityModule(string id) =>
            mobilityModules.Find(m => m.ModuleId == id);

        public UnitFrameData GetUnitFrame(string id) =>
            unitFrames.Find(f => f.FrameId == id);
    }
}
```

---

## 편성 데이터 (Garage Roster)

```csharp
namespace Features.Garage.Domain
{
    /// <summary>
    /// 플레이어의 차고 편성. 3~6기 유닛 + 각각의 모듈 조합 저장.
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

---

## 저장/불러오기

### GarageSaveService

```csharp
namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// 편성 데이터를 JSON으로 저장/불러오기.
    /// </summary>
    public class GarageSaveService
    {
        private const string FileName = "garage_roster.json";

        public void Save(GarageRoster roster)
        {
            string json = JsonUtility.ToJson(new GarageRosterWrapper(roster), true);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, FileName), json);
        }

        public GarageRoster Load()
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path))
                return new GarageRoster(); // 기본값

            string json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<GarageRosterWrapper>(json);
            return wrapper.roster;
        }
    }

    [Serializable]
    public class GarageRosterWrapper
    {
        public GarageRoster roster;
        public GarageRosterWrapper(GarageRoster r) => roster = r;
        public GarageRosterWrapper() { }
    }
}
```

---

## 기존 SkillData와의 관계

### 마이그레이션 전략

기존 `SkillData`는 점진적으로 제거한다.

| 기존 | 신규 |
|---|---|
| `SkillData` (ScriptableObject) | `UnitFrameData` + `FirepowerModuleData` + `MobilityModuleData` |
| `manaCost` | `CostCalculator` 자동 계산 |
| `Deck` | `GarageRoster` (3~6기 편성) |
| `InitializeDeckUseCase` | `InitializeGarageUseCase` |

### 과도기 허용 상태

- 기존 `SkillData` 자산은 당분간 legacy visual 참조용으로 유지
- 신규 구현은 Unit/Module 데이터 구조만 사용
- 완전 제거는 Phase 5에서 진행
